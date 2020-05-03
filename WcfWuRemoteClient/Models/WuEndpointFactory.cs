using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Channels;
using WuDataContract.DTO;
using WuDataContract.Interface;

namespace WcfWuRemoteClient.Models
{
    public class WuEndpointFactory
    {
        protected WuRemoteServiceFactory _wuRemoteServiceFactory;
        protected static readonly log4net.ILog Log = log4net.LogManager.GetLogger(
            System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public WuEndpointFactory()
        {
            _wuRemoteServiceFactory = new WuRemoteServiceFactory();
        }

        public WuEndpointFactory(WuRemoteServiceFactory wuRemoteServiceFactory = null)
        {
            _wuRemoteServiceFactory = wuRemoteServiceFactory ?? new WuRemoteServiceFactory();
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
        virtual public bool TryCreateWuEndpoint(Binding binding, EndpointAddress remoteAddress, out IWuEndpoint endpoint, out Exception exception)
        {
            if (binding == null) throw new ArgumentNullException(nameof(binding));
            if (remoteAddress == null) throw new ArgumentNullException(nameof(remoteAddress));

            endpoint = null;
            try
            {
                endpoint = new WuEndpoint(_wuRemoteServiceFactory, binding, remoteAddress);

                try
                {
                    endpoint.Service.GetFQDN(); // Call arbitrary to verfiy that the service does not deny the usage.
                    (endpoint as WuEndpoint).EagerLoad();
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
    }
}
