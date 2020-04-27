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
    /// Reboots the endpoint.
    /// </summary>
    class RebootCall : WuRemoteCall
    {
        public RebootCall() : base("Reboot") { }

        public override bool CanExecute(IWuEndpoint endpoint)
        {
            return (endpoint != null &&
                (endpoint.State.StateId == WuStateId.Ready
                || endpoint.State.StateId == WuStateId.RebootRequired));
        }

        protected override WuRemoteCallResult CallInternal(IWuEndpoint endpoint, object param)
        {
            if (ReconnectIfDisconnected(endpoint))
            {
                endpoint.Service.RebootHost();
                return WuRemoteCallResult.SuccessResult(endpoint, this);
            }
            return WuRemoteCallResult.EndpointNotAvailableResult(endpoint, this);
        }
    }
}
