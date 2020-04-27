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
using WuDataContract.Faults;

namespace WuDataContract.Interface
{
    /// <summary>
    /// Allows to remote control the update system of a windows operating system.
    /// </summary>
    [ServiceContract(
        ProtectionLevel = System.Net.Security.ProtectionLevel.EncryptAndSign, 
        CallbackContract = typeof(IWuRemoteServiceCallback), 
        SessionMode = SessionMode.Required)]
    public interface IWuRemoteService
    {
        /// <summary>
        /// Registers the client to receives notifications about service changes via the <see cref="IWuRemoteServiceCallback"/> interface.
        /// Registering for callbacks avoids polling.
        /// </summary>
        [OperationContract(IsOneWay = true)]
        void RegisterForCallback();

        /// <summary>
        /// Starts to search for updates. The operation is asynchronous.
        /// Listen for <see cref="IWuRemoteServiceCallback.OnAsyncOperationCompleted(AsyncOperation, WuStateId)"/> to get the search result.
        /// </summary>
        [FaultContract(typeof(InvalidStateTransitionFault))]
        [FaultContract(typeof(ApiFault))]
        [OperationContract]
        WuStateId BeginSearchUpdates();

        /// <summary>
        /// Sets the <see cref="WuSettings.SearchTimeoutSec"/> setting.
        /// Currently running search operations will not be affected.
        /// </summary>
        /// <param name="timeout">Timeout in seconds.</param>
        /// <returns>Timeout value which will be used by the next search operation.</returns>
        [FaultContract(typeof(BadArgumentFault))]
        [OperationContract]
        int SetSearchTimeout(int timeout);

        /// <summary>
        /// Aborts searching for updates.
        /// </summary>
        [FaultContract(typeof(InvalidStateTransitionFault))]
        [FaultContract(typeof(ApiFault))]
        [OperationContract]
        WuStateId AbortSearchUpdates();

        /// <summary>
        /// A list of updates that can be downloaded or installed.
        /// Can be empty or null when the search operation found no updates or the search operation was never started.
        /// </summary>
        /// <returns></returns>
        [OperationContract]
        UpdateDescription[] GetAvailableUpdates();

        /// <summary>
        /// Starts to download selected updates. The operation is asynchronous.
        /// Listen for <see cref="IWuRemoteServiceCallback.OnAsyncOperationCompleted(AsyncOperation, WuStateId)"/> to get the download result.
        /// </summary>
        [FaultContract(typeof(InvalidStateTransitionFault))]
        [FaultContract(typeof(PreConditionNotFulfilledFault))]
        [FaultContract(typeof(ApiFault))]
        [OperationContract]
        WuStateId BeginDownloadUpdates();

        /// <summary>
        /// Sets the <see cref="WuSettings.DownloadTimeoutSec"/> setting.
        /// Currently running download operations will not be affected.
        /// </summary>
        /// <param name="timeout">Timeout in seconds.</param>
        /// <returns>Timeout value which will be used by the next download operation.</returns>
        [FaultContract(typeof(BadArgumentFault))]
        [OperationContract]
        int SetDownloadTimeout(int timeout);

        /// <summary>
        /// Aborts the download of updates. Already downloaded updates will not be deleted.
        /// </summary>
        [FaultContract(typeof(InvalidStateTransitionFault))]
        [FaultContract(typeof(ApiFault))]
        [OperationContract]
        WuStateId AbortDownloadUpdates();

        /// <summary>
        /// Sets the <see cref="WuSettings.InstallTimeoutSec"/> setting.
        /// Currently running install operations will not be affected.
        /// </summary>
        /// <param name="timeout">Timeout in seconds.</param>
        /// <returns>Timeout value which will be used by the next install operation.</returns>
        [FaultContract(typeof(BadArgumentFault))]
        [OperationContract]
        int SetInstallTimeout(int timeout);

        /// <summary>
        /// Starts to install selected updates. The operation is asynchronous.
        /// Listen for <see cref="IWuRemoteServiceCallback.OnAsyncOperationCompleted(AsyncOperation, WuStateId)"/> to get the installation result.
        /// </summary>
        [FaultContract(typeof(InvalidStateTransitionFault))]
        [FaultContract(typeof(PreConditionNotFulfilledFault))]
        [FaultContract(typeof(ApiFault))]
        [OperationContract]
        WuStateId BeginInstallUpdates();

        /// <summary>
        /// Aborts the installation of updates. Already installed updates will not be uninstalled.
        /// </summary>
        [FaultContract(typeof(InvalidStateTransitionFault))]
        [FaultContract(typeof(ApiFault))]
        [OperationContract]
        WuStateId AbortInstallUpdates();

        /// <summary>
        /// Returns all important state informations about the windows update remote service.
        /// </summary>
        [OperationContract]
        StateDescription GetWuStatus();

        /// <summary>
        /// Returns the progress of a currently running async operation. Null, if no such operation is running.
        /// </summary>
        /// <returns></returns>
        [OperationContract]
        ProgressDescription GetCurrentProgress();

        /// <summary>
        /// Sets the <see cref="WuSettings.AutoAcceptEulas"/> setting.
        /// </summary>
        [OperationContract]
        bool SetAutoAcceptEulas(bool value);

        /// <summary>
        /// Sets the <see cref="WuSettings.AutoSelectUpdates"/> setting.
        /// </summary>
        [OperationContract]
        bool SetAutoSelectUpdates(bool value);

        /// <summary>
        /// Returns the configured settings of the windows update remote service.
        /// </summary>
        [OperationContract]
        WuSettings GetSettings();

        /// <summary>
        /// Disposes and recreates the update service.
        /// Running async operations will be aborted.
        /// </summary>
        /// <returns>State of the recreated service.</returns>
        [OperationContract]
        WuStateId ResetService();

        /// <summary>
        /// Tries to reboot the host with a small delay.
        /// There is no success feedback.
        /// </summary>
        [FaultContract(typeof(InvalidStateTransitionFault))]
        [FaultContract(typeof(PreConditionNotFulfilledFault))]
        [OperationContract]
        WuStateId RebootHost();

        /// <summary>
        /// Fqdn of the service host.
        /// </summary>
        [OperationContract]
        string GetFQDN();

        /// <summary>
        /// Versiondetails about the service components.
        /// </summary>
        [OperationContract]
        VersionInfo[] GetServiceVersion();

        /// <summary>
        /// Accepts the eula of the specified update. Does nothing if the eula is already accepted.
        /// </summary>
        /// <param name="updateId">The id of the update.</param>
        /// <returns>True, when the eula of the update is accepted, otherwise false.</returns>
        [FaultContract(typeof(ApiFault))]
        [FaultContract(typeof(UpdateNotFoundFault))]
        [FaultContract(typeof(BadArgumentFault))]
        [OperationContract]
        bool AcceptEula(string updateId);

        /// <summary>
        /// Selects an update. The service will only download and install selected updates.
        /// </summary>
        /// <param name="updateId">The id of the update to select.</param>
        /// <returns></returns>
        [FaultContract(typeof(UpdateNotFoundFault))]
        [FaultContract(typeof(BadArgumentFault))]
        [OperationContract]
        bool SelectUpdate(string updateId);

        /// <summary>
        /// Removes the selection of an update. The service will only download and install selected updates.
        /// </summary>
        /// <param name="updateId">The id of the update to unselect.</param>
        /// <returns></returns>
        [FaultContract(typeof(UpdateNotFoundFault))]
        [FaultContract(typeof(BadArgumentFault))]
        [OperationContract]
        bool UnselectUpdate(string updateId);

        /// <summary>
        /// Select all applicable updates. The service will only download and install selected updates.
        /// </summary>
        /// <returns>Number of updates which changed the state from unselected to selected.</returns>
        [OperationContract]
        int SelectAllUpdates();

        /// <summary>
        /// Unselect all applicable updates. The service will only download and install selected updates.
        /// </summary>
        /// <returns>Number of updates which changed the state from selected to unselected.</returns>
        [OperationContract]
        int UnselectAllUpdates();

    }


}
