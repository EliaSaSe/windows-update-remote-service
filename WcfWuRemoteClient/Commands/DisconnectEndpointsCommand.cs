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
using WcfWuRemoteClient.Models;

namespace WcfWuRemoteClient.Commands
{
    /// <summary>
    /// Endpoint disconnect command.
    /// </summary>
    class DisconnectEndpointsCommand : ICommand
    {
        readonly Func<IEnumerable<IWuEndpoint>> WuEndpointSelector;
        readonly WuEndpointCollection em;

        public DisconnectEndpointsCommand(WuEndpointCollection endpointMgr, Func<IEnumerable<IWuEndpoint>> wuEndpointSelector)
        {
            if (wuEndpointSelector == null) throw new ArgumentNullException(nameof(wuEndpointSelector));
            if (endpointMgr == null) throw new ArgumentNullException(nameof(endpointMgr));
            WuEndpointSelector = wuEndpointSelector;
            em = endpointMgr;
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public bool CanExecute(object parameter) => (WuEndpointSelector()?.Any() == true);

        public void Execute(object parameter)
        {
            var endpoints = WuEndpointSelector();
            if (endpoints != null) em.RemoveAndDisposeRange(endpoints);
        }

    }
}
