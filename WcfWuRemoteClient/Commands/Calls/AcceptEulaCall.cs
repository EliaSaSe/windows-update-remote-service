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
using WcfWuRemoteClient.Models;
using WuDataContract.DTO;

namespace WcfWuRemoteClient.Commands.Calls
{
    /// <summary>
    /// Accepts the eula of an update.
    /// </summary>
    class AcceptEulaCall : WuRemoteCall
    {
        public AcceptEulaCall() : base("Accept EULA"){}

        public override bool CanExecute(IWuEndpoint endpoint) => (endpoint != null);

        protected override WuRemoteCallResult CallInternal(IWuEndpoint endpoint, object param)
        {
            UpdateDescription update = param as UpdateDescription;
            if (update == null) throw new ArgumentException($"A {nameof(UpdateDescription)} parameter which includes an update id is required.", nameof(param));
            if (ReconnectIfDisconnected(endpoint))
            {
                if (endpoint.Service.AcceptEula(update.ID))
                {
                    return WuRemoteCallResult.SuccessResult(endpoint, this, $"The EULA of update {update.Title} was accepted.");
                }
                return new WuRemoteCallResult(endpoint, this, false, null, $"The EULA of update {update.Title} could not be accepted.");             
            }
            return WuRemoteCallResult.EndpointNotAvailableResult(endpoint, this);
        }
    }
}
