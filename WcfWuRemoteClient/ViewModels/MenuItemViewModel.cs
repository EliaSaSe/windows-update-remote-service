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
using System.Collections.ObjectModel;
using System.Windows.Input;
using WcfWuRemoteClient.Commands;

namespace WcfWuRemoteClient.ViewModels
{
    class MenuItemViewModel
    {
        private readonly ICommand _command;

        public MenuItemViewModel(WuEndpointCommand command)
        {
            if (command == null) throw new ArgumentNullException(nameof(command));

            _command = command;
            Header = command.Name;
        }

        public MenuItemViewModel(ICommand command, string header)
        {
            if (command == null) throw new ArgumentNullException(nameof(command));
            if(String.IsNullOrWhiteSpace(header)) throw new ArgumentNullException(nameof(header));

            _command = command;
            Header = header;
        }

        public MenuItemViewModel(string header, ObservableCollection<MenuItemViewModel> submenuItems)
        {
            if (String.IsNullOrEmpty(header)) throw new ArgumentNullException(nameof(header));
            if (submenuItems == null) throw new ArgumentNullException(nameof(submenuItems));

            Header = header;
            MenuItems = submenuItems;
        }

        public string Header { get; private set; }

        public ObservableCollection<MenuItemViewModel> MenuItems { get; private set; }

        public ICommand Command => _command;

        private void Execute() => _command?.Execute(null);
    }
}
