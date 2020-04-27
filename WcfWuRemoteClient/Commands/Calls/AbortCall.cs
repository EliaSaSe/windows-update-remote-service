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
using WcfWuRemoteClient.Models;
using WuDataContract.Enums;

namespace WcfWuRemoteClient.Commands.Calls
{
    /// <summary>
    /// Aborts a running async operation on the endpoint.
    /// </summary>
    class AbortCall : WuRemoteCall
    {
        public AbortCall() : base("Abort Operation") { }

        public override bool CanExecute(IWuEndpoint endpoint)
        {
            return (endpoint != null &&
                (endpoint.State.StateId == WuStateId.Searching
                || endpoint.State.StateId == WuStateId.Downloading
                || endpoint.State.StateId == WuStateId.Installing));
        }

        protected override WuRemoteCallResult CallInternal(IWuEndpoint endpoint, object param)
        {
            if (ReconnectIfDisconnected(endpoint))
            {
                switch (endpoint.State.StateId)
                {
                    case WuStateId.Searching:
                        endpoint.Service.AbortSearchUpdates();
                        break;
                    case WuStateId.Downloading:
                        endpoint.Service.AbortDownloadUpdates();
                        break;
                    case WuStateId.Installing:
                        endpoint.Service.AbortInstallUpdates();
                        break;
                    default:
                        return new WuRemoteCallResult(endpoint, this, false, null, "The remote service is not searching, downloading or installing updates, can not abort the current state.");
                }
                return WuRemoteCallResult.SuccessResult(endpoint, this);
            }
            return WuRemoteCallResult.EndpointNotAvailableResult(endpoint, this);
        }
    }
}
