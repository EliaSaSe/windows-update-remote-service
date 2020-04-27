/*
    Windows Update Remote Service
    Copyright(C) 2016-2020  Elia Seikritt

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU Lesser General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.See the
    GNU General Public License for more details.

    You should have received a copy of the GNU Lesser General Public License
    along with this program.If not, see<https://www.gnu.org/licenses/>.
*/
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Threading.Tasks;
using WcfWuRemoteClient.Converter;
using WuDataContract.DTO;
using WuDataContract.Enums;
using WuDataContract.Interface;

namespace WcfWuRemoteClient.Models
{
    /// <summary>
    /// Represents an endpoint which runs a <see cref="IWuRemoteService"/> service.
    /// Allows to communicate with the service.
    /// </summary>
    public class WuEndpoint : IWuEndpoint
    {
        private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        CallbackReceiver _callbackReceiver;
        Binding _binding;
        EndpointAddress _address;

        #region Properties

        /// <summary>
        /// Loads a property lazy if required or returns the current value.
        /// </summary>
        /// <typeparam name="T">The type of the property.</typeparam>
        /// <param name="currentValue">The current value of the property.</param>
        /// <param name="loadValue">Method to lazy load the property.</param>
        /// <param name="lazyLoadRequired">
        /// Method to determine whether <paramref name="loadValue"/> must be called to load the property.
        /// If this parameter is null, <paramref name="loadValue"/> will be called when <paramref name="currentValue"/> is null.
        /// </param>
        /// <param name="throwOnException">
        /// If false, all exceptions of <paramref name="loadValue"/> are catched an <paramref name="currentValue"/> will be returned.
        /// If true, exceptions of <paramref name="loadValue"/> will not be catched.
        /// </param>
        /// <returns>Returns <paramref name="currentValue"/> or the result of <paramref name="loadValue"/></returns>
        private T LazyLoad<T>(T currentValue, Func<T> loadValue, Func<T, bool> lazyLoadRequired = null, bool throwOnException = true)
        {
            if (loadValue == null) throw new ArgumentNullException(nameof(loadValue));
            if ((lazyLoadRequired != null && lazyLoadRequired(currentValue)) || (lazyLoadRequired == null && currentValue == null))
            {
                try
                {
                    return loadValue();
                }
                catch (Exception e)
                {
                    Log.Error($"Failed to lazy load a property of type {nameof(T)}.", e);
                    if (throwOnException) throw;
                }
            }
            return currentValue;
        }

        UpdateDescription[] _updates = null;
        readonly object UpdatesLock = new object();

        /// <summary>
        /// Updates available on the endpoint.
        /// </summary>
        public UpdateDescription[] Updates
        {
            get
            {
                lock (UpdatesLock)
                {
                    _updates = LazyLoad(_updates, () => { return Service.GetAvailableUpdates(); }, null, false);
                    Debug.Assert(_updates != null);
                    UpdateDescription[] resultSet = (_updates != null) ? new UpdateDescription[_updates.Length] : new UpdateDescription[0];
                    _updates?.CopyTo(resultSet, 0);
                    return resultSet;
                }
            }

            private set
            {
                lock (UpdatesLock)
                {
                    _updates = value;
                }
                OnPropertyChanged(nameof(Updates));
            }
        }

        string _fqdn = null;
        readonly object FqdnLock = new object();

        /// <summary>
        /// The FQDN of the remote host. Can be null if the FQDN can not be received.
        /// </summary>
        public string FQDN
        {
            get
            {
                lock (FqdnLock)
                {
                    _fqdn = LazyLoad(_fqdn, () => { return Service.GetFQDN(); }, null, false);
                }
                Debug.Assert(!String.IsNullOrWhiteSpace(_fqdn));
                return _fqdn;
            }
            private set
            {
                Debug.Assert(!String.IsNullOrWhiteSpace(value));
                lock (FqdnLock)
                {
                    _fqdn = value;
                }
                OnPropertyChanged(nameof(FQDN));
            }
        }

        VersionInfo[] _version = null;
        readonly object VersionLock = new object();

        /// <summary>
        /// The version of the remote host. Can be null if the version can not be received.
        /// </summary>
        public VersionInfo[] ServiceVersion
        {
            get
            {
                lock (VersionLock)
                {
                    _version = LazyLoad(_version, () => { lock (VersionLock) { return Service.GetServiceVersion(); } }, null, false);
                }
                Debug.Assert(!String.IsNullOrWhiteSpace(_fqdn));
                return _version;
            }
            private set
            {
                Debug.Assert(value != null);
                lock (VersionLock)
                {
                    _version = value;
                }
                OnPropertyChanged(nameof(ServiceVersion));
            }
        }

        /// <summary>
        /// True when <see cref="Dispose"/> was called. Properties and methods may throw <see cref="ObjectDisposedException"/> if the instance is disposed.
        /// </summary>
        public bool IsDisposed { get; private set; }

        WuSettings _settings;
        readonly object SettingsLock = new object();
        /// <summary>
        /// Settings of the endpoint.
        /// </summary>
        public WuSettings Settings
        {
            get
            {
                lock (SettingsLock)
                {
                    _settings = LazyLoad(_settings, () => { return Service.GetSettings(); }, null, false);
                    Debug.Assert(_settings != null);
                }
                return _settings;
            }
            private set
            {
                lock (SettingsLock)
                {
                    _settings = value;
                }
                OnPropertyChanged(nameof(Settings));
            }
        }

        readonly object StateLock = new object();
        StateDescription _state;
        /// <summary>
        /// Windows Update state of the endpoint.
        /// </summary>
        public StateDescription State
        {
            get
            {
                lock (StateLock)
                {
                    _state = LazyLoad(_state, () => { return Service.GetWuStatus(); }, null, false);
                    Debug.Assert(_state != null);
                }
                return _state;
            }
            private set
            {
                lock (StateLock)
                {
                    _state = value;
                }
                OnPropertyChanged(nameof(State));
            }
        }

        public CommunicationState? ConnectionState
        {
            get
            {
                var service = Service;
                if (service != null) return ((IChannel)service).State;
                return null;
            }
        }

        IWuRemoteService _service;
        readonly object ServiceLock = new object();
        public IWuRemoteService Service
        {
            get
            {
                IWuRemoteService service;
                lock (ServiceLock)
                {
                    ThrowIfDisposed();
                    service = _service;
                }
                return service;
            }
            private set
            {
                lock (ServiceLock)
                {
                    ThrowIfDisposed();
                    _service = value;
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            Log.Debug($"{FQDN}: {nameof(OnPropertyChanged)}-Event for {propertyName}.");
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        #region Construct and Dispose

        internal WuEndpoint(IWuRemoteService service, CallbackReceiver callbackReceiver, Binding binding, EndpointAddress address)
        {
            if (service == null) throw new ArgumentNullException(nameof(service));
            if (callbackReceiver == null) throw new ArgumentNullException(nameof(callbackReceiver));
            if (binding == null) throw new ArgumentNullException(nameof(binding));
            if (address == null) throw new ArgumentNullException(nameof(address));
            if (!(service is IChannel)) throw new ArgumentException($"{nameof(service)} must implement interface {nameof(IChannel)}.", nameof(service));

            Log.Debug($"Creating instance of {nameof(WuEndpoint)} for {nameof(EndpointAddress)} {address.Uri}");

            Service = service;
            _callbackReceiver = callbackReceiver;
            _callbackReceiver.Endpoint = this;
            _binding = binding;
            _address = address;

            var channel = ((IChannel)Service);
            channel.Faulted += (s, e) => { OnPropertyChanged("ConnectionState"); };
            channel.Closed += (s, e) => { OnPropertyChanged("ConnectionState"); };
            channel.Closing += (s, e) => { OnPropertyChanged("ConnectionState"); };
            channel.Opened += (s, e) => { OnPropertyChanged("ConnectionState"); };
            channel.Opening += (s, e) => { OnPropertyChanged("ConnectionState"); };

            IsDisposed = false;
        }

        /// <summary>
        /// Does some expensive remote calls which are separated from the main constructor.
        /// The remote calls will fill some properties with initial values. 
        /// The instances of this class are operational without calling this method.
        /// <see cref="EagerLoad"/> can be called multiple times.
        /// </summary>
        private void EagerLoad()
        {
            Log.Debug("Eager load properties.");
            // Call property getter to eager load value.
            var fqdn = FQDN;
            var version = ServiceVersion;
            var state = State;
            var settings = Settings;
            var updates = Updates;
        }

        private static IWuRemoteService CreateChannel(Binding binding, EndpointAddress remoteAddress, CallbackReceiver callback)
        {
            if (binding == null) throw new ArgumentNullException(nameof(binding));
            if (remoteAddress == null) throw new ArgumentNullException(nameof(remoteAddress));
            if (callback == null) throw new ArgumentNullException(nameof(callback));

            IWuRemoteService service = null;
            DuplexChannelFactory<IWuRemoteService> channelFactory = null;

            try
            {
                Log.Debug($"Creating channel for {remoteAddress.Uri}");
                channelFactory = new DuplexChannelFactory<IWuRemoteService>(callback, binding, remoteAddress);
                service = channelFactory.CreateChannel();
                ((IChannel)service).Open();
                Log.Debug($"{remoteAddress.Uri}: Register for callbacks.");
                service.RegisterForCallback();
            }
            catch (EndpointNotFoundException e)
            {
                channelFactory?.Abort();
                Log.Warn($"Could not create channel for {remoteAddress.Uri}", e);
                throw new EndpointNotFoundException($"Could not connect to the remote host. Verify that the serivce is installed on the remote host and is not blocked by the firewall. {((e.InnerException != null) ? e.InnerException.Message : e.Message) }", e);
            }
            catch (Exception e)
            {
                Log.Warn($"Could not create channel for {remoteAddress.Uri}", e);
                channelFactory?.Abort();
                throw;
            }

            return service;
        }

        /// <summary>
        /// Tries to connect to the endpoint.
        /// </summary>
        /// <param name="binding">Bindingparameter of the endpoint.</param>
        /// <param name="remoteAddress">Address to connect to.</param>
        /// <param name="endpoint">Contains the endpoint when the connect was successfull.</param>
        /// <param name="exception">Contains an exception when the connect was not successfull or the connect was succssfull, but a specific circumstance should be handled.
        /// Contains <see cref="EndpointNotSupportedException"/> when the service contract of the endpoint seems not to be compatible.
        /// Contains <see cref="EndpointNeedsUpgradeException"/> when the endpoint is using an older service contract than the client.
        /// Contains <see cref="CommunicationException"/> when the connect try failed.
        /// </param>
        /// <returns>True, when the connect was successfull. False if not, then <paramref name="exception"/> contains more details.</returns>
        public static bool TryCreateWuEndpoint(Binding binding, EndpointAddress remoteAddress, out WuEndpoint endpoint, out Exception exception)
        {
            if (binding == null) throw new ArgumentNullException(nameof(binding));
            if (remoteAddress == null) throw new ArgumentNullException(nameof(remoteAddress));

            IWuRemoteService service = null;
            var callbackReceiver = new CallbackReceiver();

            try
            {
                service = CreateChannel(binding, remoteAddress, callbackReceiver);
            }
            catch (Exception e)
            {
                Log.Warn($"Could not connect to endpoint {remoteAddress.Uri}.", e);
                exception = e;
                endpoint = null;
                return false;
            }

            endpoint = null;
            try
            {
                endpoint = new WuEndpoint(service, callbackReceiver, binding, remoteAddress);

                try
                {
                    endpoint.Service.GetFQDN(); // Call arbitrary to verfiy that the service does not deny the usage.
                    endpoint.EagerLoad();
                }
                catch (System.ServiceModel.Security.SecurityAccessDeniedException e)
                {
                    Log.Info($"Access denied: {remoteAddress.Uri}", e);
                    throw new System.ServiceModel.Security.SecurityAccessDeniedException("Access denied. You must be a member of the local administrator group of the remote host to use the service.", e);
                }

                var contractAssembly = typeof(IWuRemoteService).Assembly.GetName();
                var clientContractVersion = (VersionInfo)contractAssembly;
                var minimumSupportedContractVersion = new VersionInfo(contractAssembly.Name, 1, 0, 0, 0);
                var remoteContractVersion = endpoint.ServiceVersion?.FirstOrDefault(vi => vi.ComponentName.Equals(clientContractVersion.ComponentName) && vi.IsContract);

                Log.Info($"Comparing service contract version between this application ({clientContractVersion}) and {remoteAddress.Uri} ({remoteContractVersion}).");

                if (remoteContractVersion == null)
                {
                    Log.Info($"Endpoint {remoteAddress.Uri} does not support contract {contractAssembly.Name}.");
                    throw new EndpointNotSupportedException($"The endpoint {endpoint.FQDN} is using an unkown service contract. Expected was '{minimumSupportedContractVersion.ToString()}' until '{clientContractVersion.ToString()}'");
                }
                if (remoteContractVersion.HasHigherVersionThan(clientContractVersion, true))
                {
                    Log.Info($"Endpoint {remoteAddress.Uri} is using a newer service contract {(remoteContractVersion)} than this application.");
                    throw new EndpointNotSupportedException($"The endpoint {endpoint.FQDN} is using a newer service contract ({remoteContractVersion.ToString()}) than this client supports. Supported is '{minimumSupportedContractVersion.Major}.{minimumSupportedContractVersion.Minor}.*.*.");
                }
                if (remoteContractVersion.HasLowerVersionThan(clientContractVersion, true))
                {
                    Log.Info($"Endpoint {remoteAddress.Uri} needs upgrade ({remoteContractVersion}).");
                    exception = new EndpointNeedsUpgradeException(endpoint);
                    return true;
                }
                exception = null;
                return true;
            }
            catch (Exception e)
            {
                Log.Warn($"Could not connect to endpoint {remoteAddress.Uri}.", e);
                endpoint?.Dispose();
                exception = e;
                endpoint = null;
                return false;
            }
        }

        public void Dispose()
        {
            lock (ServiceLock)
            {
                if (Log.IsDebugEnabled) { Log.Debug($"Disposing endpoint {FQDN}"); }
                try
                {
                    ((IChannel)Service)?.Close();
                }
                catch (Exception e)
                {
                    Log.Warn(e);
                }
                finally {
                    _callbackReceiver = null;
                    Service = null;
                    IsDisposed = true;
                }
            }
        }

        private void ThrowIfDisposed()
        {
            if (IsDisposed) throw new ObjectDisposedException(FQDN);
        }

        #endregion

        #region Refresh State

        /// <summary>
        /// Requests the current Windows Update state the endpoint.
        /// The result will be stored in <see cref="State"/>.
        /// Throws exceptions if the request fails.
        /// </summary>
        public void RefreshState()
        {
            try
            {
                Log.Debug($"{FQDN}: Refreshing state.");
                State = Service.GetWuStatus();
            }
            catch (Exception e)
            {
                Log.Error($"Failed to refresh state of {FQDN}.", e);
                throw;
            }
        }

        /// <summary>
        /// Requests the actual settings of the endpoint.
        /// The result will be stored in <see cref="Settings"/>.
        /// Throws exceptions if the request fails.
        /// </summary>
        public void RefreshSettings()
        {
            try
            {
                Log.Debug($"{FQDN}: Refreshing settings.");
                Settings = Service.GetSettings();
            }
            catch (Exception e)
            {
                Log.Error($"Failed to refresh stettings of {FQDN}.", e);
                throw;
            }
        }

        /// <summary>
        /// Requests the available updates from the endpoint.
        /// The result will be stored in <see cref="Updates"/>.
        /// Throws exceptions if the request fails.
        /// </summary>
        public void RefreshUpdates()
        {
            try
            {
                Log.Debug($"{FQDN}: Refreshing available updates.");
                Updates = Service.GetAvailableUpdates();
            }
            catch (Exception e)
            {
                Log.Error($"Failed to load update list from {FQDN}.", e);
                throw;
            }
        }

        public async Task RefreshStateAsync() => await Task.Run(() => { RefreshState(); });

        public async Task RefreshSettingsAsync() => await Task.Run(() => { RefreshSettings(); });

        public async Task RefreshUpdatesAsync() => await Task.Run(() => { RefreshUpdates(); });

        #endregion

        #region Connection

        /// <summary>
        /// Closes the current channel. Does nothing if the current channel is already disconnected.
        /// </summary>
        public void Disconnect()
        {
            var channel = (IChannel)Service;
            if (channel.State == CommunicationState.Closed || channel.State == CommunicationState.Faulted) return;
            lock (ServiceLock)
            {
                ThrowIfDisposed();
                try
                {
                    channel.Close();
                }
                catch (CommunicationObjectFaultedException e)
                {
                    Log.Warn($"Failed to close connection to {FQDN}. Maybe the connection is already closed or faulted.", e);
                }
                Log.Info($"Closed connection to {FQDN}");
            }
        }

        /// <summary>
        /// Recreates a channel to the endpoint if the current channel is not longer connected.
        /// Does nothing if the current channel is still open. Throws expections when the reconnect fails.
        /// </summary>
        public void Reconnect()
        {
            var channel = (IChannel)Service;
            if (channel.State == CommunicationState.Opened || channel.State == CommunicationState.Created) return;
            Log.Info($"Reconnecting to {FQDN}.");

            IWuRemoteService service = null;
            var callbackReceiver = new CallbackReceiver();

            lock (ServiceLock)
            {
                ThrowIfDisposed();
                try
                {
                    service = CreateChannel(_binding, _address, callbackReceiver);
                }
                catch (Exception e)
                {
                    Log.Error($"Reconnecting to {FQDN} failed.", e);
                    throw;
                }

                if (service != null)
                {
                    _callbackReceiver = callbackReceiver;
                    _callbackReceiver.Endpoint = this;
                    Service = service;
                }
            }
            OnPropertyChanged("ConnectionState");
        }

        #endregion

        /// <summary>
        /// Controls the <see cref="IWuEndpoint"/> when the remote hosts sends a callback.
        /// </summary>
        internal class CallbackReceiver : IWuRemoteServiceCallback
        {
            private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
            private readonly object EndpointLock = new object();
            WuEndpoint _endpoint;

            /// <summary>
            /// Endpoint to control.
            /// </summary>
            public WuEndpoint Endpoint
            {
                get { return _endpoint; }
                set
                {
                    lock (EndpointLock)
                    {
                        _endpoint = value;
                    }
                }
            }

            public void OnAsyncOperationCompleted(WuDataContract.Enums.AsyncOperation operation, WuStateId newState)
            {
                lock (EndpointLock)
                {
                    if (Endpoint == null) return;
                    Log.Debug($"Async operation completed callback from {Endpoint.FQDN}. Operation: {operation}. New state: {newState}.");
#pragma warning disable 4014
                    Endpoint.RefreshUpdatesAsync(); // do not wait for the result
#pragma warning restore 4014
                }
            }

            public void OnProgressChanged(ProgressDescription progress, WuStateId currentState)
            {
                lock (EndpointLock)
                {
                    if (Endpoint == null) return;
                    Log.Debug($"Progress changed callback from {Endpoint.FQDN}. Percent: {progress.Percent} Indeterminate: {progress.IsIndeterminate}.");
                    lock (Endpoint.StateLock)
                    {
                        if (progress.CurrentUpdate != null)
                        {
                            Endpoint.State.Description = progress.CurrentUpdate.Title;
                            if (currentState == WuStateId.Downloading)
                            {
                                Endpoint.State.Description += $" ({BytesToHumanReadableConverter.GetBytesReadable(progress.CurrentUpdate.MaxByteSize)})";
                            }
                            Endpoint.State.Progress = progress;
                        }
                    }
                    Endpoint.OnPropertyChanged("State");
                }
            }

            public void OnStateChanged(WuStateId newState, WuStateId oldState)
            {
                lock (EndpointLock)
                {
                    if (Endpoint == null) return;
                    Log.Debug($"State changed callback from {Endpoint.FQDN}. new: {newState} old: {oldState}.");
#pragma warning disable 4014
                    if (!Endpoint.IsDisposed) Endpoint.RefreshStateAsync(); // do not wait for the result
#pragma warning restore 4014
                }
            }

            public void OnServiceShutdown()
            {
                lock (EndpointLock)
                {
                    if (Endpoint == null) return;
                    Log.Info("Service shutdown callback from " + Endpoint.FQDN);
                    Endpoint.Disconnect();
                    Endpoint.State = new StateDescription(
                        WuStateId.Stopped,
                        "Service stopped",
                        (Endpoint.State.StateId == WuStateId.RestartSentToOS) ? "Reboot" : "Unexpected service shutdown signal received",
                        Endpoint.State.InstallerStatus,
                        Endpoint.State.Enviroment,
                        null
                    );
                    Log.Debug($"State updated to {WuStateId.Stopped}.");
                }
            }
        }

    }
}
