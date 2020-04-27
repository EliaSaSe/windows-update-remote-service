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
using System.Threading.Tasks;
using WcfWuRemoteClient.Models;

namespace WcfWuRemoteClient.Commands.Calls
{
    /// <summary>
    /// Baseclass for calls that can be executed on a <see cref="IWuEndpoint"/>.
    /// </summary>
    abstract class WuRemoteCall
    {

        public readonly string Name;
        private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public static readonly WuRemoteCallHistory CallHistory = new WuRemoteCallHistory();

        public WuRemoteCall(string name)
        {
            if (String.IsNullOrWhiteSpace(name)) throw new ArgumentNullException(nameof(name));
            Name = name;
        }

        abstract protected WuRemoteCallResult CallInternal(IWuEndpoint endpoint, object param);

        /// <summary>
        /// Determines if the given <see cref="IWuEndpoint"/> is in a state that allows to successfully execute the call.
        /// </summary>
        /// <returns>True if the <see cref="IWuEndpoint"/> should be able to execute the call successfully.</returns>
        abstract public bool CanExecute(IWuEndpoint endpoint);

        private WuRemoteCallResult Call(IWuEndpoint endpoint, object param)
        {
            if (endpoint == null) throw new ArgumentNullException(nameof(endpoint));

            Log.Info($"Execute remote call {GetType().Name} on endpoint {endpoint.FQDN}, param: {param?.ToString()}");
            WuRemoteCallResult result = null;
            try
            {
                if (endpoint.IsDisposed) return new WuRemoteCallResult(endpoint, this, false, null, "Host connection is disposed, please reconnect to this host.");
                result = CallInternal(endpoint, param);
            }
            catch (Exception e)
            {
                Log.Error($"Execute remote call {GetType().Name} on endpoint {endpoint.FQDN} failed.", e);
                return new WuRemoteCallResult(endpoint, this, false, e, e.Message);
            }
            if (result == null)
            {
                return new WuRemoteCallResult(endpoint, this, true, null, null);
            }
            return result;
        }

        /// <summary>
        /// Executes the call on the given endpoint.
        /// </summary>
        /// <param name="endpoint">The target endpoint.</param>
        /// <param name="param">Call parameter.</param>
        public Task<WuRemoteCallResult> CallAsync(IWuEndpoint endpoint, object param)
        {
            if (endpoint == null) throw new ArgumentNullException(nameof(endpoint));

            var task = new Task<WuRemoteCallResult>(() => Call(endpoint, param));
            CallHistory.Add(this, endpoint, task);
            task.Start();
            return task;
        }

        /// <summary>
        /// Tries to reconnect to the given endpoint.
        /// If the endpoint is already connected, a reconnect will not be executed.
        /// </summary>
        /// <param name="e">Endpoint to reconnect.</param>
        /// <returns>True, if the endpoint is connected, false if the reconnect failed.</returns>
        protected bool ReconnectIfDisconnected(IWuEndpoint e)
        {
            if (e == null) throw new ArgumentNullException(nameof(e));

            if (e.ConnectionState != System.ServiceModel.CommunicationState.Opened)
            {
                var reconnect = new ReconnectCall();
                var result = reconnect.Call(e, null);
                return result.Success;           
            }
            return true;
        }
    }
}
