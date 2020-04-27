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
using WuDataContract.DTO;

namespace WcfWuRemoteClient.ViewModels
{
    class UpdateDescriptionViewModel : WuEndpointBasedViewModel
    {
        readonly UpdateDescription Model;
        volatile bool _eulaAccepted, _selected;
        readonly object _eulaAcceptedLock = new object(), _selectedLock = new object();
        private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public UpdateDescriptionViewModel(IModalService modalService, UpdateDescription model, IWuEndpoint endpoint) : base(modalService, endpoint)
        {
            if (model == null) throw new ArgumentNullException(nameof(model));
            Model = model;
            _eulaAccepted = model.EulaAccepted;
            _selected = model.SelectedForInstallation;
        }

        public bool IsImportant { get { return Model.IsImportant; } }
        public string ID { get { return Model.ID; } }
        public string Title { get { return Model.Title; } }
        public string Description { get { return Model.Description; } }
        public long MaxByteSize { get { return Model.MaxByteSize; } }
        public long MinByteSize { get { return Model.MinByteSize; } }
        public bool IsInstalled { get { return Model.IsInstalled; } }
        public bool IsDownloaded { get { return Model.IsDownloaded; } }
        public bool EulaAccepted
        {
            get { lock (_eulaAcceptedLock) { return _eulaAccepted; } }
            set
            {
                if (value == false) throw new InvalidOperationException("Can not unaccept EULAs.");
                lock (_eulaAcceptedLock)
                {
                    if (_eulaAccepted == value) return;
                    _eulaAccepted = value;
                }

                ExecuteRemoteCallAsync(
                    new AcceptEulaCall(),
                    Model,
                    (result) =>
                    {
                        lock (_eulaAcceptedLock)
                        {
                            _eulaAccepted = (result.Success) ? value : !value;
                        }
                        OnPropertyChanged(nameof(EulaAccepted));
                        if (!result.Success) ModalService.ShowMessageBox($"Could not accept the EULA: {result.Message}.", $"Accept EULA of {Model.Title}", MessageType.Error);
                    }
                 );
            }
        }
        public bool Selected
        {
            get { lock (_selectedLock) { return _selected; } }
            set
            {
                lock (_selectedLock)
                {
                    if (_selected == value) return;
                    _selected = value;
                }

                ExecuteRemoteCallAsync(
                    new SetUpdateSelectionCall(),
                    new SetUpdateSelectionCall.SelectionParameter(Model, value),
                    (result) =>
                    {
                        lock (_selectedLock)
                        {
                            _selected = (result.Success) ? value : !value;
                        }
                        OnPropertyChanged(nameof(Selected));
                        if (!result.Success) ModalService.ShowMessageBox($"Could not select update: {result.Message}", $"Select update {Model.Title}", MessageType.Error);
                    }
                 );
            }
        }

        public async static Task<IEnumerable<UpdateDescriptionViewModel>> GetAvailableUpdatesAsync(IWuEndpoint endpoint, IModalService modalService)
        {
            if (endpoint == null) throw new ArgumentNullException(nameof(endpoint));
            if (modalService == null) throw new ArgumentNullException(nameof(modalService));

            return await Task.Run(async () =>
            {
                await endpoint.RefreshUpdatesAsync();
                var updates = endpoint.Updates;
                IList<UpdateDescriptionViewModel> result = new List<UpdateDescriptionViewModel>();
                foreach (var u in updates)
                {
                    result.Add(new UpdateDescriptionViewModel(modalService, u, endpoint));
                }
                return result;
            });
        }
    }
}
