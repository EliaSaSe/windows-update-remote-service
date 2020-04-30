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
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows.Input;
using WcfWuRemoteClient.Commands;
using WcfWuRemoteClient.Commands.Calls;
using WcfWuRemoteClient.Models;

namespace WcfWuRemoteClient.ViewModels
{
    /// <summary>
    /// ViewModel for the main window.
    /// </summary>
    class MainWindowViewModel : INotifyPropertyChanged
    {
        bool _filterCommandHistory = true;
        private readonly Func<IEnumerable<IWuEndpoint>> GetSelectedEndpoints;
        ObservableCollection<MenuItemViewModel> _contextMenuItems;
        ObservableCollection<MenuItemViewModel> _commandButtonItems;
        readonly WuEndpointCollection _endpointCollection = new WuEndpointCollection();
        private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        internal MainWindowViewModel(Func<IEnumerable<IWuEndpoint>> getSelectedEndpoints)
        {
            if (getSelectedEndpoints == null) throw new ArgumentNullException(nameof(getSelectedEndpoints));

            GetSelectedEndpoints = getSelectedEndpoints;
            _contextMenuItems = SetupContextMenu(GetSelectedEndpoints);
            _commandButtonItems = SetupCommandButtons(GetSelectedEndpoints);
            Endpoints = _endpointCollection;
            CommandHistory = WuRemoteCall.CallHistory;
            WuRemoteCall.CallHistory.CollectionChanged += CommandHistoryChanged;
            WuRemoteCall.CallHistory.CollectionChanged += EndpointCollectionChanged;
        }

        /// <summary>
        /// Enables or disables the view filter for the command history.
        /// If enabled, only running and failed commands shall be displayed.
        /// </summary>
        public bool FilterCommandHistory
        {
            get { return _filterCommandHistory; }
            set
            {
                if (value != _filterCommandHistory)
                {
                    _filterCommandHistory = value;
                    OnPropertyChanged(nameof(FilterCommandHistory));
                    OnPropertyChanged(nameof(FilterCommandHistoryIcon));
                    OnPropertyChanged(nameof(CommandHistoryFiltered));
                }
            }
        }

        /// <summary>
        /// Path to an icon which represents the current <see cref="FilterCommandHistory"/> state.
        /// </summary>
        public string FilterCommandHistoryIcon => (FilterCommandHistory) ? "/Images/DeleteFilter.png" : "/Images/Filter.png";

        /// <summary>
        /// List of managed endpoints.
        /// </summary>
        public WuEndpointCollection Endpoints { get; private set; }

        /// <summary>
        /// Complete list of the command history.
        /// </summary>
        public IReadOnlyCollection<WuRemoteCallContext> CommandHistory { get; private set; }

        /// <summary>
        /// Depends on <see cref="FilterCommandHistory"/>, the filtered command history or the full command history.
        /// </summary>
        public IReadOnlyCollection<WuRemoteCallContext> CommandHistoryFiltered
        {
            get
            {
                if (CommandHistory != null && FilterCommandHistory)
                {
                    return new ReadOnlyCollection<WuRemoteCallContext>(CommandHistory.Where(c => !(c.Result != null && c.Result.Success)).ToList());
                }
                return CommandHistory;
            }
        }

        public ObservableCollection<MenuItemViewModel> ContextMenuItems => _contextMenuItems;
        public ObservableCollection<MenuItemViewModel> CommadButtonItems => _commandButtonItems;

        private void CommandHistoryChanged(object sender, NotifyCollectionChangedEventArgs e) => OnPropertyChanged(nameof(CommandHistoryFiltered));

        private ObservableCollection<MenuItemViewModel> SetupContextMenu(Func<IEnumerable<IWuEndpoint>> endpointSelector)
        {
            Debug.Assert(endpointSelector != null);
            return new ObservableCollection<MenuItemViewModel>
            {
                new MenuItemViewModel(new WuEndpointCommand(new BeginSearchCall(), endpointSelector)),
                new MenuItemViewModel(new OpenUpdateOverviewCommand(endpointSelector), "Select Updates"),
                new MenuItemViewModel(new WuEndpointCommand(new BeginDownloadCall(),endpointSelector)),
                new MenuItemViewModel(new WuEndpointCommand(new BeginInstallationCall(),endpointSelector)),
                new MenuItemViewModel(new WuEndpointCommand(new AbortCall(),endpointSelector)),
                new MenuItemViewModel(new WuEndpointCommand(new RefreshCall(),endpointSelector)),
                new MenuItemViewModel(new WuEndpointCommand(new RebootCall(),endpointSelector)),
                new MenuItemViewModel("Connection",
                    new ObservableCollection<MenuItemViewModel>
                        {
                            new MenuItemViewModel(new WuEndpointCommand(new ReconnectCall(),endpointSelector)),
                            new MenuItemViewModel(new WuEndpointCommand(new ResetCall(),endpointSelector)),
                            new MenuItemViewModel(new DisconnectEndpointsCommand(_endpointCollection, endpointSelector),"Disconnect"),
                        }
                )
            };
        }

        private ObservableCollection<MenuItemViewModel> SetupCommandButtons(Func<IEnumerable<IWuEndpoint>> endpointSelector)
        {
            Debug.Assert(endpointSelector != null);
            return new ObservableCollection<MenuItemViewModel>
            {
                new MenuItemViewModel(new WuEndpointCommand(new BeginSearchCall(), endpointSelector)),
                new MenuItemViewModel(new OpenUpdateOverviewCommand(endpointSelector), "Select Updates"),
                new MenuItemViewModel(new WuEndpointCommand(new BeginDownloadCall(),endpointSelector)),
                new MenuItemViewModel(new WuEndpointCommand(new BeginInstallationCall(),endpointSelector)),
                new MenuItemViewModel(new WuEndpointCommand(new AbortCall(),endpointSelector)),
                new MenuItemViewModel(new WuEndpointCommand(new RefreshCall(),endpointSelector)),
                new MenuItemViewModel(new WuEndpointCommand(new RebootCall(),endpointSelector)),
                new MenuItemViewModel(new DisconnectEndpointsCommand(_endpointCollection, endpointSelector),"Disconnect")
            };
        }

        public void ClearHistory() => WuRemoteCall.CallHistory.Clear();

        /// <summary>
        /// Adds an event handler to each new <see cref="IWuEndpoint"/> to watch state changes.
        /// Required to raise <see cref="WuEndpointCommand.CanExecuteChanged"/> events to revalidate buttons and command execution rules. 
        /// </summary>
        private void EndpointCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            Log.Debug($"{nameof(EndpointCollectionChanged)}-Eventhandler activation. Change: {e.Action.ToString()}.");
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                foreach (var item in e.NewItems)
                {
                    IWuEndpoint endpoint = item as IWuEndpoint;
                    if (endpoint != null)
                    {
                        Log.Debug($"Register {nameof(endpoint.PropertyChanged)}-Eventhandler for {endpoint.FQDN}");
                        endpoint.PropertyChanged += (s, args) =>
                        {                            
                            if (args.PropertyName.Equals(nameof(IWuEndpoint.State)))
                            {
                                Log.Debug($"Revalidate buttons and command execution rules because the state of endpoint {endpoint.FQDN} changed.");
                                CommandManager.InvalidateRequerySuggested();
                            }
                        };
                    }
                }
            }
        }

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        protected internal void OnPropertyChanged(string propertyname) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyname));
        #endregion
    }
}
