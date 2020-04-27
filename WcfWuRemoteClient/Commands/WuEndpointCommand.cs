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
using System.Linq;
using System.Windows.Input;
using WcfWuRemoteClient.Commands.Calls;
using WcfWuRemoteClient.Models;

namespace WcfWuRemoteClient.Commands
{
    /// <summary>
    /// General class for commands which can be executed on <see cref="IWuEndpoint"/> objects.
    /// This class needs a <see cref="WuRemoteCall"/> object to execute a specific command.
    /// </summary>
    class WuEndpointCommand : ICommand
    {
        readonly WuRemoteCall RemoteCall;
        readonly Func<IEnumerable<IWuEndpoint>> WuEndpointSelector;

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public string Name { get { return RemoteCall.Name; } }

        public WuEndpointCommand(WuRemoteCall remoteCall, Func<IEnumerable<IWuEndpoint>> wuEndpointSelector)
        {
            if (remoteCall == null) throw new ArgumentNullException(nameof(remoteCall));
            if (wuEndpointSelector == null) throw new ArgumentNullException(nameof(wuEndpointSelector));

            RemoteCall = remoteCall;
            WuEndpointSelector = wuEndpointSelector;
        }

        public bool CanExecute(object param) => WuEndpointSelector().All(e => RemoteCall.CanExecute(e));

        public void Execute(object param)
        {
            var endpoints = WuEndpointSelector();
            if (endpoints != null)
            {
                foreach (var e in endpoints)
                {
                    RemoteCall.CallAsync(e, param);
                }
            }
        }
    }
}
