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
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using WcfWuRemoteClient.Commands;
using WcfWuRemoteClient.Commands.Calls;
using WcfWuRemoteClient.InteractionServices;
using WcfWuRemoteClient.Models;
using WcfWuRemoteClient.ViewModels;

namespace WcfWuRemoteClient.Views
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        MainWindowViewModel Model;

        public MainWindow()
        {
            InitializeComponent();
            Language = XmlLanguage.GetLanguage(Thread.CurrentThread.CurrentCulture.Name);

            Model = new MainWindowViewModel(() => DataGridEndpoints.SelectedItems.OfType<IWuEndpoint>());
            DataContext = Model;
        }

        AddEndpointWindow _addWindow;
        private void AddEndpoint_Click(object sender, RoutedEventArgs e)
        {
            if (_addWindow == null || !_addWindow.IsVisible)
            {
                _addWindow?.Close();
                _addWindow = new AddEndpointWindow(new WpfModalService(), Model.Endpoints, new WuEndpointFactory());
            }
            _addWindow.Show();
            _addWindow.Activate();
        }

        private void ClearCommandHistory_Click(object sender, RoutedEventArgs e) => Model.ClearHistory();

        private void FilterCommandHistory_Click(object sender, RoutedEventArgs e) => Model.FilterCommandHistory = !Model.FilterCommandHistory;


        private void EndpointRow_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            DataGridRow row = sender as DataGridRow;
            if (row.Item is IWuEndpoint)
            {
                var window = new UpdateOverviewWindow(row.Item as IWuEndpoint);
                window.Show();
                window.Activate();
            }
        }

        private void RefreshBinding_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            (new WuEndpointCommand(new RefreshCall(), () => DataGridEndpoints.Items.OfType<IWuEndpoint>())).Execute(null);
        }

        private void RefreshBinding_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = (DataGridEndpoints.Items.OfType<IWuEndpoint>().Any()) ? true : false;
        }

        private void DataGridEndpoints_SelectionChanged(object sender, SelectionChangedEventArgs e) => CommandManager.InvalidateRequerySuggested();


        InfoWindow _aboutWindow = null;
        private void About_Click(object sender, RoutedEventArgs e)
        {
            if (_aboutWindow == null || !_aboutWindow.IsVisible)
            {
                _aboutWindow?.Close();
                _aboutWindow = new InfoWindow(new WpfModalService(), Model.Endpoints);
            }
            _aboutWindow.Show();
            _aboutWindow.Activate();
        }
    }
}
