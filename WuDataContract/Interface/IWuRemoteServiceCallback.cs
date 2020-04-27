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
using System.ServiceModel;
using WuDataContract.DTO;
using WuDataContract.Enums;

namespace WuDataContract.Interface
{
    /// <summary>
    /// Callback-Interface for <see cref="IWuRemoteService"/> to notify clients about changes in the service.
    /// A specific order of event firing is never guaranteed.
    /// </summary>
    [ServiceContract]
    public interface IWuRemoteServiceCallback
    {
        /// <summary>
        /// Fired when the internal state of the <see cref="IWuRemoteService"/> has changed.
        /// Call <see cref="IWuRemoteService.GetWuStatus"/> for more details.
        /// </summary>
        [OperationContract(IsOneWay = true)]
        void OnStateChanged(WuStateId newState, WuStateId oldState);

        /// <summary>
        /// Fired when the <see cref="IWuRemoteService"/> completes an async operation (successful or unsuccesful).
        /// Call <see cref="IWuRemoteService.GetWuStatus"/> for more details.
        /// </summary>
        /// <param name="operation">Id of the completed operation</param>
        [OperationContract(IsOneWay = true)]
        void OnAsyncOperationCompleted(AsyncOperation operation, WuStateId newState);

        /// <summary>
        /// Fired when the progress of an async operation changed.
        /// </summary>
        [OperationContract(IsOneWay = true)]
        void OnProgressChanged(ProgressDescription progress, WuStateId currentState);

        /// <summary>
        /// Fired when the service is shutting down. Clients should close connections to this service as soon as possible.
        /// </summary>
        [OperationContract(IsOneWay = true, IsTerminating = true)]
        void OnServiceShutdown();
    }
}
