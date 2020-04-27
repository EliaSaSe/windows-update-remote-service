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
using log4net;
using System;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;
using System.Threading;
using WcfWuRemoteService.Configuration;
using WcfWuRemoteService.Helper;

namespace WcfWuRemoteService.WindowsService
{
    /// <summary>
    /// This class is responsible for setup a <see cref="WuRemoteService"/> as self hosted wcf service and to start and stop them.
    /// Public members are thread safe. This class will terminate the process on fatal errors by calling <see cref="Environment.Exit(int)"/>.
    /// Exit-Codes:
    /// 1 - Failed to start the self hosted wcf service.
    /// 2 - Self hosted wcf service faulted to many times.
    /// </summary>
    class ServiceWorker
    {
        /// <summary>
        /// The name of the service.
        /// </summary>
        public readonly string ServiceName;

        private static readonly ILog Log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        ServiceHost _hosting;
        WuRemoteService _hostedService;
        int _faultedCount = 0, _faultedCountMax = 10; // WCF service faulted count. // ToDo: could be made configurable
        object _startstoplock = new object(), _firewallLock = new object();

        /// <param name="servicename">Name of the service.</param>
        public ServiceWorker(string servicename)
        {
            if (String.IsNullOrWhiteSpace(servicename)) throw new ArgumentNullException(nameof(servicename));
            ServiceName = servicename;
        }

        /// <summary>
        /// Starts the wcf hosting of <see cref="WuRemoteService"/>.
        /// </summary>
        public void Start()
        {
            lock (_startstoplock)
            {
                Log.Info("Starting service host.");
                StartInternal();
                Debug.Assert(_hosting != null);
                if (_hosting.State != CommunicationState.Opened)
                {
                    Log.Fatal("Could not start service host, aborting.");
                    Environment.Exit(1);
                }
            }
            if (GetCreateFWRuleSetting())
            {
                lock (_firewallLock)
                {
                    try
                    {
                        Log.Info($"Creating firewall rule for service {ServiceName}.");
                        (new WindowsFirewall(ServiceName)).OpenFirewall();
                    }
                    catch (Exception e)
                    {
                        Log.Error("Could not create firewall rule.", e);
                    }
                }
            }
        }

        /// <summary>
        /// Stops the wcf hosting of <see cref="WuRemoteService"/>.
        /// </summary>
        public void Stop()
        {
            lock (_startstoplock)
            {
                Log.Info("Stoping service host.");
                Debug.Assert(_hosting != null);
                try
                {
                    Debug.Assert(_hostedService != null);
                    _hostedService?.SendShutdownSignal();
                    _hosting?.Close();
                }
                catch (Exception e)
                {
                    Log.Error("Could not close service host properly.", e);
                    _hosting?.Abort();
                }
                finally
                {
                    _hostedService = null;
                }
            }
            if (GetCreateFWRuleSetting())
            {
                lock (_firewallLock)
                {
                    try
                    {
                        Log.Info("Removing firewall rule for service " + ServiceName);
                        (new WindowsFirewall(ServiceName)).CloseFirewall();
                    }
                    catch (Exception e)
                    {
                        Log.Error("Could not remove firewall rule.", e);
                    }
                }
            }
        }

        /// <summary>
        /// Setup of the hosted wcf service.
        /// </summary>
        private void StartInternal()
        {
            if (_hosting == null || _hosting.State == CommunicationState.Closed || _hosting.State == CommunicationState.Faulted)
            {
                if (_hosting != null)
                {
                    _hosting.Abort();
                    _hosting = null;
                }

                Log.Info("Creating new service instance.");
                _hostedService = new WuRemoteService(new WuApiControllerFactory(), new OperationContextProvider(), new WuApiConfigProvider());
                _hosting = new ServiceHost(_hostedService);
                _hosting.Closed += (s, e) => { Log.Debug("Service host closed."); };
                _hosting.Closing += (s, e) => { Log.Debug("Service host closing."); };
                _hosting.Opened += (s, e) => { Log.Info("Service host opened."); };
                _hosting.Opening += (s, e) => { Log.Debug("Service host opening."); };
                _hosting.UnknownMessageReceived += (s, e) => { Log.Warn("Unkown message received."); };
                _hosting.Faulted += OnServiceFaulted;

                foreach (var endpoint in _hosting.Description.Endpoints)
                {
                    endpoint.Behaviors.Add(new ClientTrackerEndpointBehavior());
                };

            }
            Debug.Assert(_hosting != null);

            if (_hosting.State == CommunicationState.Created)
            {
                Log.Info($"Endpoint: " + String.Join(", ", _hosting.Description.Endpoints.Select(e => e.Address.Uri)));
                try
                {
                    _hosting.Open();
                }
                catch (Exception e)
                {
                    ///Eventhandler <see cref="OnServiceFaulted(object, EventArgs)" /> should be invoked and handle the situation.
                    Log.Error("Failed to open service host.", e);
                }
            }
        }

        /// <summary>
        /// Tries to restart a faulted wcf service.
        /// </summary>
        private void OnServiceFaulted(object sender, EventArgs args)
        {
            Log.Error("Service host is now in faulted state.");
            _faultedCount++;
            if (_faultedCount <= _faultedCountMax)
            {
                Thread.Sleep(1000 * _faultedCount);
                Log.Warn("Restart service host to recover from faulted state.");
                Start();
            }
            else
            {
                Log.Fatal($"Service host faulted to many times ({_faultedCountMax}), aborting."); // something is wrong with the hosted service?
                Environment.Exit(2);
            }
        }

        /// <summary>
        /// Reads the configuration of <see cref="WuServiceConfigSection.CreateFirewallRuleValue"/>.
        /// </summary>
        /// <returns>When true, the service is allowed to modify the windows firewall rules.</returns>
        private bool GetCreateFWRuleSetting()
        {
            WuServiceConfigSection section = ConfigurationManager.GetSection(WuServiceConfigSection.SectionName) as WuServiceConfigSection;
            if (section != null)
            {
                Log.Debug($"The configuration section '{WuServiceConfigSection.SectionName}' will be used to set settings.");
                return section.CreateFirewallRuleValue;
            }
            Log.Warn($"The configuration section '{WuServiceConfigSection.SectionName}' could not be found, using default settings.");
            return false; // the default value
        }

        /// <summary>
        /// Helperclass for logging.
        /// </summary>
        class ClientTrackerEndpointBehavior : IEndpointBehavior
        {
            public void AddBindingParameters(ServiceEndpoint endpoint, BindingParameterCollection bindingParameters) { }

            public void ApplyClientBehavior(ServiceEndpoint endpoint, ClientRuntime clientRuntime) { }

            public void ApplyDispatchBehavior(ServiceEndpoint endpoint, EndpointDispatcher endpointDispatcher)
            {
                endpointDispatcher.ChannelDispatcher.ChannelInitializers.Add(new ClientTrackerChannelInitializer());
                endpointDispatcher.DispatchRuntime.MessageInspectors.Add(new ClientTrackerMessageInspector());
            }

            public void Validate(ServiceEndpoint endpoint) { }
        }

        /// <summary>
        /// Helperclass for logging.
        /// </summary>
        class ClientTrackerChannelInitializer : IChannelInitializer
        {
            private static readonly ILog Log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

            public void Initialize(IClientChannel channel)
            {
                using (NDC.Push(channel.SessionId))
                {
                    Log.Info($"A client connected (session: {channel.SessionId}).");
                }

                channel.Closed += ClientDisconnected;
                channel.Faulted += ClientDisconnected;
            }

            private void ClientDisconnected(object sender, EventArgs e)
            {
                var channel = (IClientChannel)sender;
                using (NDC.Push(channel.SessionId))
                {
                    Log.Info($"A client disconnected (session: {channel.SessionId}).");
                }
            }
        }

        /// <summary>
        /// Helperclass for logging.
        /// </summary>
        class ClientTrackerMessageInspector : IDispatchMessageInspector
        {
            private static readonly ILog Log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

            public object AfterReceiveRequest(ref Message request, IClientChannel channel, InstanceContext instanceContext)
            {
                //NDC.Push(channel.SessionId);
                using (NDC.Push(channel.SessionId))
                {
                    var context = OperationContext.Current;
                    var user = context?.ServiceSecurityContext?.WindowsIdentity.Name;
                    var remoteEndpointMsgProp = context?.IncomingMessageProperties[RemoteEndpointMessageProperty.Name] as RemoteEndpointMessageProperty;
                    Log.Info($"{remoteEndpointMsgProp?.Address}:{user}:{request.Headers.Action}");
                }
                return null;
            }

            public void BeforeSendReply(ref Message reply, object correlationState)
            {
                //NDC.Pop();
            }
        }
    }


}
