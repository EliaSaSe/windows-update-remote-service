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
using System.Threading.Tasks;
using WcfWuRemoteClient.Commands.Calls;
using WcfWuRemoteClient.InteractionServices;
using WcfWuRemoteClient.Models;

namespace WcfWuRemoteClient.ViewModels
{
    abstract class WuEndpointBasedViewModel : INotifyPropertyChanged
    {
        readonly WeakReference<IWuEndpoint> WeakRefEndpoint;
        readonly protected IModalService ModalService;
        private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public WuEndpointBasedViewModel(IModalService modalService, IWuEndpoint endpoint)
        {
            if (endpoint == null) throw new ArgumentNullException(nameof(endpoint));
            if (modalService == null) throw new ArgumentNullException(nameof(modalService));
            WeakRefEndpoint = new WeakReference<IWuEndpoint>(endpoint);
            ModalService = modalService;
        }

        /// <summary>
        /// Allows to get the <see cref="IWuEndpoint"/> for this View Model.
        /// </summary>
        /// <param name="endpoint">The endpoint for the view model.</param>
        /// <returns>True if the endpoint is available, false if the endpoint is disposed or not longer available.</returns>
        protected bool TryGetEndpoint(out IWuEndpoint endpoint)
        {
            IWuEndpoint e;
            if (WeakRefEndpoint.TryGetTarget(out e) && !e.IsDisposed)
            {
                endpoint = e;
                return true;
            }
            endpoint = null;
            return false;
        }

        /// <summary>
        /// Executes a <see cref="WuRemoteCall"/> on the <see cref="IWuEndpoint"/> for this view model.
        /// </summary>
        /// <param name="call">Call to execute.</param>
        /// <param name="param">Call parameter.</param>
        protected async Task<WuRemoteCallResult> ExecuteRemoteCallAsync(WuRemoteCall call, object param)
        {
            if (call == null) throw new ArgumentNullException(nameof(call));

            IWuEndpoint endpoint;
            if (TryGetEndpoint(out endpoint))
            {
                try
                {
                    Log.Debug($"Execute remote call async: {call.GetType().Name}, Param: {param?.ToString()}");
                    return await call.CallAsync(endpoint, param);
                }
                catch (Exception e)
                {
                    Log.Error($"Execute remote call async failed: {call.GetType().Name}, Param: {param?.ToString()}", e);
                    return new WuRemoteCallResult(endpoint, call, false, e, null);
                }
            }
            else
            {
                return new WuRemoteCallResult(endpoint, call, false, null, "The remote service is not longer available.");
            }
        }

        /// <summary>
        /// Executes a <see cref="WuRemoteCall"/> on the <see cref="IWuEndpoint"/> for this view model.
        /// </summary>
        /// <param name="call">Call to execute.</param>
        /// <param name="param">Call parameter.</param>
        /// <param name="onCompletion">Called when the async operation completes.</param>
        protected async void ExecuteRemoteCallAsync(WuRemoteCall call, object param, Action<WuRemoteCallResult> onCompletion)
        {
            if (call == null) throw new ArgumentNullException(nameof(call));
            var result = await ExecuteRemoteCallAsync(call, param);
            onCompletion?.Invoke(result);
        }

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        protected internal void OnPropertyChanged(string propertyname) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyname));
        #endregion
    }
}
