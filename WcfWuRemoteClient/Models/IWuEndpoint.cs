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
using System.ServiceModel;
using System.Threading.Tasks;
using WuDataContract.DTO;
using WuDataContract.Interface;

namespace WcfWuRemoteClient.Models
{
    /// <summary>
    /// Represents an endpoint to communicate with a <see cref="IWuRemoteService"/> service.
    /// </summary>
    public interface IWuEndpoint : IDisposable, INotifyPropertyChanged
    {
        CommunicationState? ConnectionState { get; }
        string FQDN { get; }
        VersionInfo[] ServiceVersion { get; }
        bool IsDisposed { get; }
        WuSettings Settings { get; }
        StateDescription State { get; }
        IWuRemoteService Service { get; }
        UpdateDescription[] Updates { get; }
        void Disconnect();
        void Reconnect();
        void RefreshSettings();
        Task RefreshSettingsAsync();
        void RefreshState();
        Task RefreshStateAsync();
        void RefreshUpdates();
        Task RefreshUpdatesAsync();
    }
}