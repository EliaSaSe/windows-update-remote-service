using System;
using System.ServiceModel;
using System.ServiceModel.Channels;
using WuDataContract.Interface;
using static WcfWuRemoteClient.Models.WuEndpoint;

namespace WcfWuRemoteClient.Models
{
    public class WuRemoteServiceFactory
    {
        protected static readonly log4net.ILog Log = log4net.LogManager.GetLogger(
            System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        virtual internal IWuRemoteService GetInstance(Binding binding, EndpointAddress remoteAddress, CallbackReceiver callback)
        {
            if (binding == null) throw new ArgumentNullException(nameof(binding));
            if (remoteAddress == null) throw new ArgumentNullException(nameof(remoteAddress));
            if (callback == null) throw new ArgumentNullException(nameof(callback));

            IWuRemoteService service;
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
    }
}
