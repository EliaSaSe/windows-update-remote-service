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
using System.Windows;
using WcfWuRemoteClient.InteractionServices;
using WcfWuRemoteClient.Models;
using WcfWuRemoteClient.ViewModels;

namespace WcfWuRemoteClient.Views
{
    /// <summary>
    /// Interaktionslogik für UpdateOverview.xaml
    /// </summary>
    public partial class UpdateOverviewWindow : Window
    {
        WeakReference<IWuEndpoint> _endpoint;
        object _loadingLock = new object();
        bool _isLoading = false;
        readonly IModalService ModalService = new WpfModalService();
        readonly UpdateOverviewViewModel Model;


        internal UpdateOverviewWindow(IWuEndpoint endpoint)
        {
            if (endpoint == null)
                throw new ArgumentNullException(nameof(endpoint));

            _endpoint = new WeakReference<IWuEndpoint>(endpoint);

            InitializeComponent();

            Model = new UpdateOverviewViewModel(ModalService, endpoint);

            HostNameLabel.Content = endpoint.FQDN;
            this.Title = $"{endpoint.FQDN}: {this.Title}";
            Refresh();
        }

        async void Refresh()
        {
            lock (_loadingLock)
            {
                if (_isLoading)
                {
                    return;
                }
                _isLoading = true;
            }

            UpdateGrid.IsEnabled = false;
            Loading.BeginLoadingIndication();
            try
            {
                DataContext = null;
                await Model.RefreshAsync();
                DataContext = Model;
            }
            finally
            {
                lock (_loadingLock)
                {
                    _isLoading = false;
                }
                UpdateGrid.IsEnabled = true;
                Loading.StopLoadingIndication();
            }
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            Refresh();
        }

    }
}
