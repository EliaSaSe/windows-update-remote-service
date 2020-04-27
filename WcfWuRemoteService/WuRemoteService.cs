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
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using System.ServiceModel;
using System.Threading.Tasks;
using WcfWuRemoteService.Helper;
using WindowsUpdateApiController;
using WindowsUpdateApiController.Exceptions;
using WuDataContract.DTO;
using WuDataContract.Enums;
using WuDataContract.Faults;
using WuDataContract.Interface;
using Client = WuDataContract.Interface.IWuRemoteServiceCallback;

namespace WcfWuRemoteService
{
    /// <summary>
    /// Wrapper for <see cref="WindowsUpdateApiController.IWuApiController"/> to host it as web service.
    /// </summary>
    /// This class is not thread safe, do not change the ConcurrencyMode, must be ConcurrencyMode.Reentrant.
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single, ConcurrencyMode = ConcurrencyMode.Reentrant, UseSynchronizationContext = false)]
    public class WuRemoteService : IWuRemoteService, IDisposable
    {
        private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        readonly IWuApiConfigProvider ConfigProvider;
        readonly OperationContextProvider OpContext;
        readonly WuApiControllerFactory ControllerFactory;

        #region Properties

        private IWuApiController _controller = null;
        /// <summary>
        /// The windows update api controller.
        /// </summary>
        private IWuApiController Controller
        {
            get { return _controller; }
            set
            {
                if (value == null) throw new ArgumentNullException(nameof(value));
                if (_controller != null)
                {
                    _controller.OnStateChanged -= OnStateChangedHandler;
                    _controller.OnAsyncOperationCompleted -= OnAsyncOperationCompletedHandler;
                    _controller.OnProgressChanged -= OnProgressChangedHandler;
                    (_controller as IDisposable)?.Dispose();
                }
                _controller = value;
                _controller.OnStateChanged += OnStateChangedHandler;
                _controller.OnAsyncOperationCompleted += OnAsyncOperationCompletedHandler;
                _controller.OnProgressChanged += OnProgressChangedHandler;
            }
        }

        private List<Client> _callbacks = null;
        private readonly object CallbackLock = new object();
        /// <summary>
        /// Holds client callbacks to push events to them. 
        /// </summary>
        private List<Client> Callbacks
        {
            get
            {
                if (_callbacks == null) _callbacks = new List<Client>();
                // Remove callbacks for clients which are not longer connected
                _callbacks = _callbacks.Where(c => OpContext.GetCommunicationState(c) != CommunicationState.Closed && OpContext.GetCommunicationState(c) != CommunicationState.Faulted).ToList();
                return _callbacks;
            }
        }

        #endregion

        #region Construct and dispose

        internal WuRemoteService(WuApiControllerFactory controllerFactory, OperationContextProvider opContext, IWuApiConfigProvider configProvider)
        {
            if (controllerFactory == null) throw new ArgumentNullException(nameof(controllerFactory));
            if (opContext == null) throw new ArgumentNullException(nameof(opContext));
            if (configProvider == null) throw new ArgumentNullException(nameof(configProvider));
            ControllerFactory = controllerFactory;
            OpContext = opContext;
            ConfigProvider = configProvider;
            Controller = GetNewController();
        }

        private IWuApiController GetNewController()
        {
            IWuApiController controller = ControllerFactory.GetController();
            controller.AutoSelectUpdates = ConfigProvider.AutoSelectUpdates;
            controller.AutoAcceptEulas = ConfigProvider.AutoAcceptEulas;
            return controller;
        }

        public void Dispose()
        {
            Log.Debug("Disposing.");
            (Controller as IDisposable)?.Dispose();
            try
            {
                ConfigProvider.AutoSelectUpdates = Controller.AutoSelectUpdates;
                ConfigProvider.AutoAcceptEulas = Controller.AutoAcceptEulas;
                ConfigProvider.Save();
            }
            catch (Exception e)
            {
                Log.Error($"Could not write configuration changes.", e);
            }
            ConfigProvider?.Dispose();
        }

        #endregion

        #region Default request handling

        private FaultException GetFault(Exception e)
        {
            if (e == null) throw new ArgumentNullException(nameof(e));
            if (e is PreConditionNotFulfilledException) return PreConditionNotFulfilledFault.GetFault(e);
            if (e is InvalidStateTransitionException) return InvalidStateTransitionFault.GetFault(e);
            if (e is COMException) return ApiFault.GetFault(e as COMException);
            if (e is UpdateNotFoundException) return UpdateNotFoundFault.GetFault(e, ((UpdateNotFoundException)e).UpdateId);
            //if (e is ArgumentOutOfRangeException) return BadArgumentFault.GetFault(e as ArgumentOutOfRangeException);
            if (e is ArgumentException) return BadArgumentFault.GetFault(e as ArgumentException);

            // Other exceptions should never happen. Do not wrap them in to faults.
            Log.Error("An unexpected expection was thrown.", e);
            ExceptionDispatchInfo.Capture(e).Throw(); // Keep the stack trace and rethrow
            throw new NotImplementedException($"Avoid compiler error. Should never be reached, because {nameof(ExceptionDispatchInfo)} rethrows the exception. But the compiler does not know that.");
        }

        private T ProcessRequest<T>(Func<T> operation)
        {
            try { return operation(); }
            catch (Exception e) { throw GetFault(e); }
        }

        private T ProcessSettingChange<T>(T newValue, Func<T> GetProp, Action<T> SetProp)
        {
            if ((newValue == null && GetProp() != null) || !newValue.Equals(GetProp()))
            {
                try { SetProp(newValue); }
                catch (Exception e) { throw GetFault(e); }
            }
            return GetProp();
        }

        #endregion

        #region Callback event handling

        private WuApiController.StateChangedHandler OnStateChangedHandler => (s, e) => Broadcast((Client client) => { client.OnStateChanged(e.NewState, e.OldState); });
        private WuApiController.AsyncOperationCompletedHandler OnAsyncOperationCompletedHandler => (s, e) => Broadcast((Client client) => { client.OnAsyncOperationCompleted(e.Operation, e.Result); });
        private WuApiController.ProgressChangedHandler OnProgressChangedHandler => (s, e) => Broadcast((Client client) => { client.OnProgressChanged(e.Progress, e.StateId); });

        /// <summary>
        /// Iterates through every connected client which is registered for callback and invokes <paramref name="messageSender"/> with the client as parameter.
        /// The callbacks are called asyncron without waiting for the result. The method may return before all callbacks are delivered to all clients.
        /// </summary>
        private void Broadcast(Action<Client> messageSender, [System.Runtime.CompilerServices.CallerMemberName] string callerName = "")
        {
            Debug.Assert(messageSender != null);
            if (String.IsNullOrWhiteSpace(callerName)) callerName = "<unkown caller>";
            Action<Client> send = delegate (Client clientCallback)
            {
                try
                {
                    Debug.Assert(clientCallback != null);
                    var comState = OpContext.GetCommunicationState(clientCallback);
                    if (comState == CommunicationState.Opened || comState == CommunicationState.Created)
                    {
                        messageSender(clientCallback);
                    }
                }
                catch (Exception ex)
                {
                    Log.Warn($"{callerName}: Failed to send callback.", ex);
                }
            };
            try
            {
                IList<Client> callbackList;
                lock (CallbackLock)
                {
                    callbackList = Callbacks.ToList();
                }
                if (!callbackList.Any())
                {
                    Log.Debug($"{callerName}: Skipping callback to clients, because no client is registered for callbacks.");
                }
                Log.Debug($"{callerName}: Begin to send callback to {callbackList.Count} connected client(s).");
                foreach (var callback in callbackList)
                {
                    Task.Run(() => send(callback));
                }
            }
            catch (Exception ex2)
            {
                Log.Error($"{callerName}: Callback broadcast failed.", ex2);
                throw;
            }
        }

        /// <summary>
        /// Allows clients to register for callback events.
        /// Registration may be lost after a dis- and reconnect.
        /// </summary>
        [AdministratorPrincipalRequired(SecurityAction.Demand)]
        public void RegisterForCallback()
        {
            try
            {
                Client clientCallback = OpContext.GetCallbackChannel();
                Debug.Assert(clientCallback != null);
                lock (CallbackLock)
                {
                    if (!Callbacks.Contains(clientCallback))
                    {
                        Log.Debug("Callback registration.");
                        Callbacks.Add(clientCallback);
                    }
                }
            }
            catch (Exception e) { throw GetFault(e); }
        }

        /// <summary>
        /// Informs connected clients that the service is shutting down.
        /// </summary>
        internal void SendShutdownSignal() => Broadcast((Client client) => { client.OnServiceShutdown(); });

        #endregion

        #region Aborts

        [AdministratorPrincipalRequired(SecurityAction.Demand)]
        public WuStateId AbortDownloadUpdates() => ProcessRequest(() => Controller.AbortDownloadUpdates());

        [AdministratorPrincipalRequired(SecurityAction.Demand)]
        public WuStateId AbortInstallUpdates() => ProcessRequest(() => Controller.AbortInstallUpdates());

        [AdministratorPrincipalRequired(SecurityAction.Demand)]
        public WuStateId AbortSearchUpdates() => ProcessRequest(() => Controller.AbortSearchUpdates());

        #endregion

        #region Begins

        [AdministratorPrincipalRequired(SecurityAction.Demand)]
        public WuStateId BeginDownloadUpdates() => ProcessRequest(() => Controller.BeginDownloadUpdates(ConfigProvider.DownloadTimeout));

        [AdministratorPrincipalRequired(SecurityAction.Demand)]
        public WuStateId BeginInstallUpdates() => ProcessRequest(() => Controller.BeginInstallUpdates(ConfigProvider.InstallTimeout));

        [AdministratorPrincipalRequired(SecurityAction.Demand)]
        public WuStateId BeginSearchUpdates() => ProcessRequest(() => Controller.BeginSearchUpdates(ConfigProvider.SearchTimeout));

        #endregion

        #region Settings

        [AdministratorPrincipalRequired(SecurityAction.Demand)]
        public bool SetAutoAcceptEulas(bool value) => ProcessSettingChange(value, () => Controller.AutoAcceptEulas, (value2) => Controller.AutoAcceptEulas = value2);

        [AdministratorPrincipalRequired(SecurityAction.Demand)]
        public bool SetAutoSelectUpdates(bool value) => ProcessSettingChange(value, () => Controller.AutoSelectUpdates, (value2) => Controller.AutoSelectUpdates = value2);

        [AdministratorPrincipalRequired(SecurityAction.Demand)]
        public WuSettings GetSettings() => ProcessRequest(() =>
        {
            return new WuSettings(ConfigProvider.SearchTimeout, 
                ConfigProvider.DownloadTimeout, 
                ConfigProvider.InstallTimeout, 
                Controller.AutoAcceptEulas, 
                Controller.AutoSelectUpdates);
        });

        #region Timeout values

        private int ThrowIfInvalidTimeout(int timeout)
        {
            if (timeout <= 0) throw BadArgumentFault.GetFault(new ArgumentOutOfRangeException(nameof(timeout), "Must be greater than zero."));
            if (timeout > (int.MaxValue / 1000)) throw BadArgumentFault.GetFault(new ArgumentOutOfRangeException(nameof(timeout), $"The max allowed value is {(int.MaxValue / 1000)}."));
            return timeout;
        }

        [AdministratorPrincipalRequired(SecurityAction.Demand)]
        public int SetSearchTimeout(int timeout) => ProcessSettingChange(ThrowIfInvalidTimeout(timeout), () => ConfigProvider.SearchTimeout, (value) => ConfigProvider.SearchTimeout = value);

        [AdministratorPrincipalRequired(SecurityAction.Demand)]
        public int SetInstallTimeout(int timeout) => ProcessSettingChange(ThrowIfInvalidTimeout(timeout), () => ConfigProvider.InstallTimeout, (value) => ConfigProvider.InstallTimeout = value);

        [AdministratorPrincipalRequired(SecurityAction.Demand)]
        public int SetDownloadTimeout(int timeout) => ProcessSettingChange(ThrowIfInvalidTimeout(timeout), () => ConfigProvider.DownloadTimeout, (value) => ConfigProvider.DownloadTimeout = value);

        #endregion
        #endregion

        #region Service control

        [AdministratorPrincipalRequired(SecurityAction.Demand)]
        public ProgressDescription GetCurrentProgress() => ProcessRequest(() => Controller.CurrentProgress);

        [AdministratorPrincipalRequired(SecurityAction.Demand)]
        public StateDescription GetWuStatus() => ProcessRequest(() => Controller.GetWuStatus());

        [AdministratorPrincipalRequired(SecurityAction.Demand)]
        public WuStateId ResetService() => ProcessRequest(() =>
        {
            Log.Warn("Resetting the service.");
            Controller = GetNewController();
            return Controller.GetWuStatus().StateId;
        });

        [AdministratorPrincipalRequired(SecurityAction.Demand)]
        public string GetFQDN() => ProcessRequest(() => Controller.GetEnviroment().FQDN);

        [AdministratorPrincipalRequired(SecurityAction.Demand)]
        public VersionInfo[] GetServiceVersion() => ProcessRequest(() =>
        {
            var contrlAssembly = Controller.GetType().Assembly.GetName();
            var serviceAssembly = GetType().Assembly.GetName();
            var contractAssembly = typeof(IWuRemoteService).Assembly.GetName();
            return new VersionInfo[] { contrlAssembly, serviceAssembly, VersionInfo.FromAssembly(contractAssembly, true) };
        });

        [AdministratorPrincipalRequired(SecurityAction.Demand)]
        public WuStateId RebootHost() => ProcessRequest(() => Controller.Reboot());

        #endregion

        #region Update selection

        [AdministratorPrincipalRequired(SecurityAction.Demand)]
        public UpdateDescription[] GetAvailableUpdates() => ProcessRequest(() => Controller.GetAvailableUpdates().ToArray());

        [AdministratorPrincipalRequired(SecurityAction.Demand)]
        public bool AcceptEula(string updateId) => ProcessRequest(() => { Controller.AcceptEula(updateId); return true; });

        [AdministratorPrincipalRequired(SecurityAction.Demand)]
        public bool SelectUpdate(string updateId) => ProcessRequest(() => { Controller.SelectUpdate(updateId); return true; });

        [AdministratorPrincipalRequired(SecurityAction.Demand)]
        public bool UnselectUpdate(string updateId) => ProcessRequest(() => { Controller.UnselectUpdate(updateId); return true; });

        [AdministratorPrincipalRequired(SecurityAction.Demand)]
        public int SelectAllUpdates() => ProcessRequest(() =>
        {
            var updates = Controller.GetAvailableUpdates().Where(u => !u.SelectedForInstallation);
            updates.ToList().ForEach((u) => Controller.SelectUpdate(u));
            return updates.Count();

        });

        [AdministratorPrincipalRequired(SecurityAction.Demand)]
        public int UnselectAllUpdates() => ProcessRequest(() =>
        {
            var updates = Controller.GetAvailableUpdates().Where(u => u.SelectedForInstallation);
            updates.ToList().ForEach((u) => Controller.UnselectUpdate(u));
            return updates.Count();
        });

        #endregion
    }
}