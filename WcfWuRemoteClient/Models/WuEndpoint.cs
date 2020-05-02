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
        readonly Binding _binding;
        readonly EndpointAddress _address;
        readonly WuRemoteServiceFactory _serviceFactory;

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
                    if (_service == null)
                    {
                        Connect();
                    }
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
            Log.Debug($"{_fqdn}: {nameof(OnPropertyChanged)}-Event for {propertyName}.");
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        #region Construct and Dispose

        internal WuEndpoint(
            WuRemoteServiceFactory serviceFactory,
            Binding binding,
            EndpointAddress address)
        {
            Log.Debug($"Creating instance of {nameof(WuEndpoint)} for {nameof(EndpointAddress)} {address?.Uri}");

            _serviceFactory = serviceFactory ?? throw new ArgumentNullException(nameof(serviceFactory));
            _binding = binding ?? throw new ArgumentNullException(nameof(binding));
            _address = address ?? throw new ArgumentNullException(nameof(address));

            IsDisposed = false;
        }

        /// <summary>
        /// Does some expensive remote calls which are separated from the main constructor.
        /// The remote calls will fill some properties with initial values. 
        /// The instances of this class are operational without calling this method.
        /// <see cref="EagerLoad"/> can be called multiple times.
        /// </summary>
        internal void EagerLoad()
        {
            Log.Debug("Eager load properties.");
            // Call property getter to eager load value.
            _ = FQDN;
            _ = ServiceVersion;
            _ = State;
            _ = Settings;
            _ = Updates;
        }

        public void Dispose()
        {
            lock (ServiceLock)
            {
                if (Log.IsDebugEnabled) { Log.Debug($"Disposing endpoint {_fqdn}"); }
                try
                {
                    ((IChannel)Service)?.Close();
                }
                catch (Exception e)
                {
                    Log.Warn(e);
                }
                finally
                {
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

        private void Connect()
        {

            IWuRemoteService service = null;
            var callbackReceiver = new CallbackReceiver();

            lock (ServiceLock)
            {
                ThrowIfDisposed();
                service = _serviceFactory.GetInstance(_binding, _address, callbackReceiver);
                if (!(service is IChannel)) throw new InvalidOperationException(
                    $"{nameof(service)} must implement interface {nameof(IChannel)}.");

                if (service != null)
                {
                    var channel = ((IChannel)service);
                    channel.Faulted += (s, e) => { OnPropertyChanged("ConnectionState"); };
                    channel.Closed += (s, e) => { OnPropertyChanged("ConnectionState"); };
                    channel.Closing += (s, e) => { OnPropertyChanged("ConnectionState"); };
                    channel.Opened += (s, e) => { OnPropertyChanged("ConnectionState"); };
                    channel.Opening += (s, e) => { OnPropertyChanged("ConnectionState"); };

                    _callbackReceiver = callbackReceiver;
                    _callbackReceiver.Endpoint = this;
                    Service = service;
                }
            }
            OnPropertyChanged("ConnectionState");
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
            Connect();
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
