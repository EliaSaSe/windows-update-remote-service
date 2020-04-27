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
    /// (Un-)Selects updates for download/installation.
    /// </summary>
    class SetUpdateSelectionCall : WuRemoteCall
    {
        public SetUpdateSelectionCall() : base("Select update"){}

        public override bool CanExecute(IWuEndpoint endpoint) => (endpoint != null);

        protected override WuRemoteCallResult CallInternal(IWuEndpoint endpoint, object param)
        {
            SelectionParameter parameter;
            if (param is SelectionParameter)
            {
                parameter = (SelectionParameter)param;
            }
            else
            {
                throw new ArgumentException($"A {nameof(SelectionParameter)} parameter is required.", nameof(param));
            }
            if (ReconnectIfDisconnected(endpoint))
            {
                if (parameter.Value && endpoint.Service.SelectUpdate(parameter.Update.ID)) // try to select update
                {
                    return WuRemoteCallResult.SuccessResult(endpoint, this, $"Update {parameter.Update.Title} selected.");
                }
                if (!parameter.Value && endpoint.Service.UnselectUpdate(parameter.Update.ID)) // try to unselect update
                {
                    return WuRemoteCallResult.SuccessResult(endpoint, this, $"Update {parameter.Update.Title} unselected.");
                }
                return new WuRemoteCallResult(endpoint, this, false, null, $"Update {parameter.Update.Title} could not be {(parameter.Value?"selected":"unselected")}.");
            }
            return WuRemoteCallResult.EndpointNotAvailableResult(endpoint, this);
        }

        public struct SelectionParameter
        {
            public readonly bool Value;
            public readonly UpdateDescription Update;

            public SelectionParameter(UpdateDescription update, bool value)
            {
                if (update == null) throw new ArgumentNullException(nameof(update));

                Value = value;
                Update = update;
            }

            public override string ToString()
            {
                return $"{((Value) ? "Select" : "Unselect")} update: {Update.Title}";
            }
        }

    }
}
