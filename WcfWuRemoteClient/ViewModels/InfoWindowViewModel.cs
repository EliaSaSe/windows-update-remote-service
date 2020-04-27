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
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using WcfWuRemoteClient.InteractionServices;
using WcfWuRemoteClient.Models;
using WuDataContract.Interface;

namespace WcfWuRemoteClient.ViewModels
{
    internal class InfoWindowViewModel : INotifyPropertyChanged
    {
        readonly IModalService ModalService;
        readonly WuEndpointCollection EndpointCollection;
        volatile DataTable _versionsInUse = null;
        private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public InfoWindowViewModel(IModalService modalService, WuEndpointCollection endpointCollection)
        {
            if (endpointCollection == null) throw new ArgumentNullException(nameof(endpointCollection));
            if (modalService == null) throw new ArgumentNullException(nameof(modalService));
            ModalService = modalService;
            EndpointCollection = endpointCollection;
        }

        public async void LoadDataAsync()
        {
            try
            {
                VersionsInUse = await BuildVersionTableAsync();
            }
            catch (Exception e)
            {
                Log.Error("Could not receive version information from connected endpoints.", e);
                ModalService.ShowMessageBox("Could not receive version information from connected endpoints.", "Failed to display version data", MessageType.Error);
            }
        }

        public DataTable VersionsInUse
        {
            get { return _versionsInUse; }
            private set
            {
                _versionsInUse = value;
                OnPropertyChanged(nameof(VersionsInUse));
            }
        }

        public string ClientContractVersion
        {
            get
            {
                var assembly = typeof(IWuRemoteService).Assembly.GetName();
                return $"{assembly.Name} {assembly.Version.Major}.{assembly.Version.Minor}.{assembly.Version.Build}.{assembly.Version.Revision}";
            }
        }

        public string ClientVersion
        {
            get
            {
                var assembly = this.GetType().Assembly.GetName();
                return $"{assembly.Name} {assembly.Version.Major}.{assembly.Version.Minor}.{assembly.Version.Build}.{assembly.Version.Revision}";
            }
        }

        private Task<DataTable> BuildVersionTableAsync()
        {
            return Task.Run(() =>
            {
                var data = new DataTable();
                data.Columns.Add("Host");
                var endpoints = EndpointCollection.ToList();

                if (!endpoints.Any()) return null;

                endpoints.Where(e => e.ServiceVersion != null).SelectMany(e => e.ServiceVersion).Select(v => v.ComponentName)
                .Distinct().ToList().ForEach(c => data.Columns.Add(c));

                foreach (var endpoint in endpoints)
                {
                    var row = data.NewRow();
                    row[0] = endpoint.FQDN;
                    if (endpoint.ServiceVersion != null)
                    {
                        foreach (var ver in endpoint.ServiceVersion)
                        {
                            row[row.Table.Columns[ver.ComponentName]] = $"{ver.Major}.{ver.Minor}.{ver.Build}.{ver.Revision}";
                        }
                    }
                    data.Rows.Add(row);
                }
                return data;
            });
        }

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        protected internal void OnPropertyChanged(string propertyname) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyname));
        #endregion
    }
}
