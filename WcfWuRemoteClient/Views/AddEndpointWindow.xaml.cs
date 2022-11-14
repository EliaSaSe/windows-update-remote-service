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
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using WcfWuRemoteClient.InteractionServices;
using WcfWuRemoteClient.Models;
using WcfWuRemoteClient.ViewModels;

namespace WcfWuRemoteClient.Views
{
    public partial class AddEndpointWindow : Window
    {
        readonly IModalService _modalService = new WpfModalService();
        readonly WuEndpointCollection _endpointCollection;
        readonly WuEndpointFactory _wuEndpointFactory;

        public AddEndpointWindow(IModalService modalService, WuEndpointCollection endpointCollection,
            WuEndpointFactory endpointFactory)
        {
            _modalService = modalService ?? throw new ArgumentNullException(nameof(modalService));
            _endpointCollection = endpointCollection ?? throw new ArgumentNullException(nameof(endpointCollection));
            _wuEndpointFactory = endpointFactory;

            InitializeComponent();

            TextBoxUrlInput.Text =
                $"{AddHostViewModel.DefaultScheme}://localhost:{AddHostViewModel.DefaultPort}/{AddHostViewModel.DefaultPath}";
        }

        private void BeginLoadingIndication()
        {
            ButtonAddEndpoints.IsEnabled = false;
            TextBoxUrlInput.IsEnabled = false;
            this.LoadingIndicator.BeginLoadingIndication();
        }

        private void StopLoadingIndication()
        {
            ButtonAddEndpoints.IsEnabled = true;
            TextBoxUrlInput.IsEnabled = true;
            this.LoadingIndicator.StopLoadingIndication();
        }

        private async void ButtonAddEndpoints_Click(object sender, RoutedEventArgs e)
        {
            var inputText = TextBoxUrlInput.Text;
            await AddEndpoints(inputText);
        }

        private async Task AddEndpoints(string inputText)
        {
            if (String.IsNullOrWhiteSpace(inputText)) return;
            BeginLoadingIndication();
            var result = await AddHostViewModel.ConnectToHosts(_wuEndpointFactory, _endpointCollection, inputText);
            StopLoadingIndication();
            
            if (result.Any(a => a.Success && a.Exception is EndpointNeedsUpgradeException))
            {
                string text =
                    "The following hosts are outdated and should be upgraded:"
                    + Environment.NewLine
                    + String.Join(Environment.NewLine,
                        result.Where(a => a.Success && a.Exception is EndpointNeedsUpgradeException)
                            .Select(a => a.Endpoint.FQDN));
                _modalService.ShowMessageBox(text, "Outdated hosts.", MessageType.Info);
            }

            if (result.Any(a => !a.Success))
            {
                TextBoxUrlInput.Text =
                    String.Join(Environment.NewLine, result.Where(a => !a.Success).Select(a => a.Url));
                _modalService.ShowMessageBox(
                    String.Join(Environment.NewLine + Environment.NewLine,
                        result.Where(a => !a.Success).Select(a => a.Url + ": " + a.Exception.Message)),
                    "Connect to hosts", MessageType.Warning);
            }
            else
            {
                Close();
            }

        }

        private async void ButtonAddEndpointsFromFile_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog();
            dialog.DefaultExt = ".txt"; // Default file extension
            dialog.Filter = "Text documents (.txt)|*.txt"; // Filter files by extension
            var dialogResult = dialog.ShowDialog();

            if (dialogResult is true)
            {
                var inputText = File.ReadAllText(dialog.FileName);
                await AddEndpoints(inputText);
            }
        }
    }
}