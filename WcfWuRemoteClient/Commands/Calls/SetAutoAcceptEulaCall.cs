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

namespace WcfWuRemoteClient.Commands.Calls
{
    /// <summary>
    /// Configures the auto accept behavior for eulas. 
    /// </summary>
    class SetAutoAcceptEulaCall : WuRemoteCall
    {
        public SetAutoAcceptEulaCall() : base("Set auto accept eulas setting") { }

        public override bool CanExecute(IWuEndpoint endpoint) => (endpoint != null);

        protected override WuRemoteCallResult CallInternal(IWuEndpoint endpoint, object param)
        {
            bool value = (param is bool) ? (bool)param : true;
            if (ReconnectIfDisconnected(endpoint))
            {
                endpoint.Service.SetAutoAcceptEulas(value);
                endpoint.RefreshSettings();
                return WuRemoteCallResult.SuccessResult(endpoint, this);
            }
            return WuRemoteCallResult.EndpointNotAvailableResult(endpoint, this);
        }
    }

}
