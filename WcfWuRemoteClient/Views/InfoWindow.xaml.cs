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
    public partial class InfoWindow : Window
    {
        readonly InfoWindowViewModel Model;


        public InfoWindow(IModalService modalService, WuEndpointCollection endpointCollection)
        {
            if (modalService == null) throw new ArgumentNullException(nameof(modalService));
            if (endpointCollection == null) throw new ArgumentNullException(nameof(endpointCollection));
            Model = new InfoWindowViewModel(modalService, endpointCollection);
            DataContext = Model;
            InitializeComponent();

            Model.LoadDataAsync();
        }
    }
}
