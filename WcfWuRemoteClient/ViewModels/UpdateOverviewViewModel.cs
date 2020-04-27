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
using System.Threading.Tasks;
using WcfWuRemoteClient.Commands.Calls;
using WcfWuRemoteClient.InteractionServices;
using WcfWuRemoteClient.Models;

namespace WcfWuRemoteClient.ViewModels
{
    class UpdateOverviewViewModel : WuEndpointBasedViewModel
    {
        object _dataLock = new object();
        bool _autoSelectUpdates, _autoAcceptEula;

        public UpdateOverviewViewModel(IModalService modalService, IWuEndpoint endpoint) : base(modalService, endpoint) { }

        public string Hostname { private set; get; }
        public IEnumerable<UpdateDescriptionViewModel> Updates { private set; get; }
        public bool AutoSelectUpdates
        {
            get
            {
                lock (_dataLock)
                {
                    return _autoSelectUpdates;
                }
            }
            set
            {
                lock (_dataLock)
                {
                    if (_autoSelectUpdates == value) return;
                    _autoSelectUpdates = value;
                }

                ExecuteRemoteCallAsync(
                    new SetAutoSelectCall(),
                    value,
                    (result) =>
                    {
                        lock (_dataLock)
                        {
                            _autoSelectUpdates = (result.Success) ? value : !value;
                        }
                        OnPropertyChanged(nameof(AutoSelectUpdates));
                        if (!result.Success) ModalService.ShowMessageBox($"Could not change setting: {result.Message}.", $"Auto select: {Hostname}", MessageType.Error);
                    }
                 );
            }
        }
        public bool AutoAcceptEulas
        {
            get
            {
                lock (_dataLock)
                {
                    return _autoAcceptEula;
                }
            }
            set
            {
                lock (_dataLock)
                {
                    if (_autoAcceptEula == value) return;
                    _autoAcceptEula = value;
                }

                ExecuteRemoteCallAsync(
                    new SetAutoAcceptEulaCall(),
                    value,
                    (result) =>
                    {
                        lock (_dataLock)
                        {
                            _autoAcceptEula = (result.Success) ? value : !value;
                        }
                        OnPropertyChanged(nameof(AutoAcceptEulas));
                        if (!result.Success) ModalService.ShowMessageBox($"Could not change setting: {result.Message}", $"Auto accept eulas: {Hostname}", MessageType.Error);
                    }
                 );
            }
        }

        async public Task RefreshAsync()
        {
            IWuEndpoint endpoint;
            if (TryGetEndpoint(out endpoint))
            {
                try
                {
                    await endpoint.RefreshSettingsAsync();
                    var updates = await UpdateDescriptionViewModel.GetAvailableUpdatesAsync(endpoint, ModalService);
                    lock (_dataLock)
                    {
                        _autoSelectUpdates = endpoint.Settings.AutoSelectUpdates;
                        _autoAcceptEula = endpoint.Settings.AutoAcceptEulas;
                        Updates = updates;
                        Hostname = endpoint.FQDN;
                    }
                    OnPropertyChanged(nameof(Hostname));
                    OnPropertyChanged(nameof(Updates));
                    OnPropertyChanged(nameof(AutoSelectUpdates));
                    OnPropertyChanged(nameof(AutoAcceptEulas));
                }
                catch (Exception e)
                {
                    ModalService.ShowMessageBox("Could not retrieve data: " + e.Message, Hostname, MessageType.Error);
                }
            }
            else
            {
                ModalService.ShowMessageBox("The service is not longer available.", Hostname, MessageType.Error);
            }
        }
    }
}
