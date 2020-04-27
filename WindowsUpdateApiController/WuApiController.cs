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
using System.Diagnostics;
using System.Linq;
using WindowsUpdateApiController.EventArguments;
using WindowsUpdateApiController.Exceptions;
using WindowsUpdateApiController.States;
using WindowsUpdateApiController.Helper;
using WUApiLib;
using WuDataContract.DTO;
using WuDataContract.Enums;
using ll = WindowsUpdateApiController.Helper.LockWithLog;

namespace WindowsUpdateApiController
{
    /// <summary>
    /// Allows to search, download and install windows updates.
    /// </summary>
    public class WuApiController : IDisposable, IWuApiController
    {
        readonly static log4net.ILog Log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        readonly IUpdateSession UpdateSession;
        readonly IUpdateSearcher UpdateSearcher; // find search criterias here: https://msdn.microsoft.com/en-us/library/windows/desktop/aa386526%28v=VS.85%29.aspx
        readonly IUpdateDownloader UpdateDownloader;
        readonly IUpdateInstaller UpdateInstaller;
        readonly WuUpdateHolder UpdateHolder = new WuUpdateHolder(); // stores updates, which were found by the IUpdateSearcher
        readonly ISystemInfo SystemInfo; // OS environment data
        readonly UpdateCollectionFactory UpdateCollectionFactory;
        readonly StateTransitionCollection StateTransitions; // describes valid state transitions

        readonly LockObj StateLock = new LockObj("StateLock"); // lock for prepare a state change or read the current state
        readonly LockObj StateChangingLock = new LockObj("StateChangingLock"); // lock when changing the state
        readonly LockObj UpdateHolderLock = new LockObj("UpdateHolderLock");

        volatile bool _autoAcceptEulas = false;
        WuProcessState _currentState; // internal state of the state maschine
        ProgressDescription _currentProgress; // Progress of the current state, USE THE CurrentProgress-PROPERTY TO SET AND GET THE PROGRESS.

        #region constructors and dispose

        /// <summary>
        /// Creates a <see cref="WuApiController"/> which uses Microsoft's WuApiLib-COM-Interface to search, download and install updates.
        /// </summary>
        public WuApiController() : this(new UpdateSession(), new UpdateCollectionFactory(), new SystemInfo()) { }

        /// <summary>
        /// Creates a <see cref="WuApiController"/> which uses the given Interfaces to search, download and install updates.
        /// </summary>
        /// <param name="session">Session to be used.</param>
        /// <param name="updateCollectionFactory">Factory to create <see cref="IUpdateCollection"/>s.</param>
        /// <param name="systemInfo">System informations about the OS enviroment.</param>
        public WuApiController(IUpdateSession session, UpdateCollectionFactory updateCollectionFactory, ISystemInfo systemInfo)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (updateCollectionFactory == null) throw new ArgumentNullException(nameof(updateCollectionFactory));
            if (systemInfo == null) throw new ArgumentNullException(nameof(systemInfo));

            UpdateSession = session;
            UpdateSession.ClientApplicationID = this.GetType().FullName;
            UpdateSearcher = session.CreateUpdateSearcher();
            UpdateDownloader = session.CreateUpdateDownloader();
            UpdateInstaller = session.CreateUpdateInstaller();
            UpdateCollectionFactory = updateCollectionFactory;
            SystemInfo = systemInfo;
            StateTransitions = SetupStateTransitions();

            EnterState((SystemInfo.IsRebootRequired()) ? (WuProcessState)new WuStateRebootRequired() : new WuStateReady());
            Log.Info("Initial state: " + _currentState.GetType().Name);
            if (Log.IsDebugEnabled)
            {
                OnStateChanged += (s, e) => { Log.Debug($"Event {nameof(OnStateChanged)} fired: {e.ToString()}"); };
                OnAsyncOperationCompleted += (s, e) => { Log.Debug($"Event {nameof(OnAsyncOperationCompleted)} fired: {e.ToString()}"); };
                OnProgressChanged += (s, e) => { Log.Debug($"Event {nameof(OnProgressChanged)} fired: {e.ToString()}"); };
            }
        }

        public void Dispose()
        {
            using (ll.Lock(StateLock))
            {
                try
                {
                    Log.Info("Disposing.");
                    (_currentState as WuStateAsyncJob)?.Abort();
                }
                finally
                {
                    (_currentState as IDisposable)?.Dispose();
                }
            }
        }

        #endregion

        #region events

        public delegate void StateChangedHandler(object sender, StateChangedEventArgs e);
        public delegate void AsyncOperationCompletedHandler(object sender, AsyncOperationCompletedEventArgs e);
        public delegate void ProgressChangedHandler(object sender, ProgressChangedEventArgs e);

        /// <summary>
        /// Fires after the internal state has changed.
        /// </summary>
        public event StateChangedHandler OnStateChanged;

        /// <summary>
        /// Fires after a search, download or installation operation has completed (successfully or unsuccessfully).
        /// </summary>
        public event AsyncOperationCompletedHandler OnAsyncOperationCompleted;

        /// <summary>
        /// Fires after the progress of the current state has changed.
        /// </summary>
        public event ProgressChangedHandler OnProgressChanged;

        #endregion

        #region helper methods

        /// <summary>
        /// Converts <see cref="IUpdate"/> enumerations to <see cref="IUpdateCollection"/>.
        /// </summary>
        private IUpdateCollection ToUpdateCollection(IEnumerable<IUpdate> updateList)
        {
            IUpdateCollection collection = UpdateCollectionFactory.GetInstance();
            if (updateList.Any())
            {
                foreach (IUpdate update in updateList)
                {
                    collection.Add(update);
                }
            }
            return collection;
        }

        #endregion

        #region properties

        /// <summary>
        /// If enabled, the eulas of updates will automatically accepted (if required) before download or install them.
        /// If disabled, updates with not accepted eulas will not be downloaded or installed.
        /// </summary>
        public bool AutoAcceptEulas
        {
            get { return _autoAcceptEulas; }
            set { _autoAcceptEulas = value; }
        }

        /// <summary>
        /// If enabled, important updates will be automatically selected for download and installation after <see cref="BeginSearchUpdates(int)"/> completes.
        /// If disabled, no update will be automatically selected for download and installation.
        /// </summary>
        public bool AutoSelectUpdates
        {
            get { return UpdateHolder.AutoSelectUpdates; }
            set { UpdateHolder.AutoSelectUpdates = value; }
        }

        /// <summary>
        /// Holds the progress of the current state.
        /// If the property gets updated, the <see cref="OnProgressChanged"/> event will be fired, except the current progress changes to null.
        /// Listen to the <see cref="OnStateChanged"/> event to get informed when the current progress changes to null.
        /// </summary>
        public ProgressDescription CurrentProgress
        {
            get { return _currentProgress; }
            private set
            {
                ProgressDescription old;
                WuStateId stateId;
                bool callEvent = false;
                using (ll.Lock(StateLock))
                {
                    old = _currentProgress;
                    _currentProgress = value;
                    stateId = _currentState.StateId;
                    // Do not fire, when the progress changes to null, a state change could be ongoing.
                    // Fire in this situation can lead in to a deadlock, event handlers may wants to access locked properties/methods (because a state change is ongoing)
                    // and the state change is waiting for the CurrentProgress set operation to complete. Alternative solution: fire the event async?  
                    callEvent = (_currentProgress != null && old != _currentProgress) ? true : false;
                }
                if (callEvent) OnProgressChanged?.Invoke(this, new ProgressChangedEventArgs(stateId, value));
            }
        }

        #endregion

        #region state handling

        /// <summary>
        /// Setups a list of all valid state transistions.
        /// </summary>
        private StateTransitionCollection SetupStateTransitions()
        {
            StateTransition.TransitionCondition downloadingPreCon = (c) =>
            {
                using (ll.Lock(UpdateHolderLock))
                {
                    var applUpdates = UpdateHolder.GetSelectedUpdates((u) => u.EulaAccepted);
                    if (applUpdates.Any())
                    {
                        if (applUpdates.All(u => u.IsDownloaded || u.IsInstalled))
                        {
                            return new ConditionEvalResult(true, "All selected updates are already downloaded."); // or installed
                        }
                        if (SystemInfo.GetFreeSpace() < (decimal)1.5 * applUpdates.Where(u => !u.IsDownloaded && !u.IsInstalled).Sum(u => (u.RecommendedHardDiskSpace > 0) ? u.RecommendedHardDiskSpace : u.MaxDownloadSize))
                        {
                            return new ConditionEvalResult(false, "Not enough free space on system drive.");
                        }
                        return ConditionEvalResult.ValidStateChange;
                    }
                    return new ConditionEvalResult(false, "Please search for updates first, select some updates and accept the eulas.");
                }
            };
            StateTransition.TransitionCondition installingPreCon = (c) =>
            {
                using (ll.Lock(UpdateHolderLock))
                {
                    if (UpdateInstaller.IsBusy)
                    {
                        return new ConditionEvalResult(false, "The update installer is currently busy, an other update session installs or uninstalls updates.");
                    }
                    if (UpdateInstaller.RebootRequiredBeforeInstallation)
                    {
                        return new ConditionEvalResult(false, "A reboot is required before updates can be installed.");
                    }
                    var applUpdates = UpdateHolder.GetSelectedUpdates((u) => u.EulaAccepted);
                    if (applUpdates.Any())
                    {
                        if (applUpdates.All(u => u.IsInstalled))
                        {
                            return new ConditionEvalResult(true, "All selected updates are already downloaded."); // or installed
                        }
                        return ConditionEvalResult.ValidStateChange;
                    }
                    return new ConditionEvalResult(false, "Please search for updates first, select some updates and accept the eulas.");
                }
            };

            var stateTransitions = new StateTransitionCollection();
            //                fromState      -goes->     toState
            stateTransitions.Add<WuStateReady, WuStateSearching>();
            stateTransitions.Add<WuStateReady, WuStateRestartSentToOS>();

            stateTransitions.Add<WuStateSearching, WuStateSearchCompleted>();
            stateTransitions.Add<WuStateSearching, WuStateSearchFailed>();

            stateTransitions.Add<WuStateSearchCompleted, WuStateSearching>();
            stateTransitions.Add<WuStateSearchCompleted, WuStateDownloading>(downloadingPreCon);
            stateTransitions.Add<WuStateSearchCompleted, WuStateInstalling>(installingPreCon);

            stateTransitions.Add<WuStateSearchFailed, WuStateSearching>();
            stateTransitions.Add<WuStateSearchFailed, WuStateDownloading>(downloadingPreCon);
            stateTransitions.Add<WuStateSearchFailed, WuStateInstalling>(installingPreCon);

            stateTransitions.Add<WuStateDownloading, WuStateDownloadFailed>();
            stateTransitions.Add<WuStateDownloading, WuStateDownloadCompleted>();
            stateTransitions.Add<WuStateDownloading, WuStateDownloadPartiallyFailed>();

            stateTransitions.Add<WuStateDownloadFailed, WuStateSearching>();
            stateTransitions.Add<WuStateDownloadFailed, WuStateDownloading>(downloadingPreCon);
            stateTransitions.Add<WuStateDownloadFailed, WuStateInstalling>(installingPreCon);

            stateTransitions.Add<WuStateDownloadCompleted, WuStateSearching>();
            stateTransitions.Add<WuStateDownloadCompleted, WuStateDownloading>(downloadingPreCon);
            stateTransitions.Add<WuStateDownloadCompleted, WuStateInstalling>(installingPreCon);

            stateTransitions.Add<WuStateDownloadPartiallyFailed, WuStateSearching>();
            stateTransitions.Add<WuStateDownloadPartiallyFailed, WuStateDownloading>(downloadingPreCon);
            stateTransitions.Add<WuStateDownloadPartiallyFailed, WuStateInstalling>(installingPreCon);

            stateTransitions.Add<WuStateInstalling, WuStateInstallCompleted>();
            stateTransitions.Add<WuStateInstalling, WuStateInstallFailed>();
            stateTransitions.Add<WuStateInstalling, WuStateInstallPartiallyFailed>();
            stateTransitions.Add<WuStateInstalling, WuStateRebootRequired>();
            stateTransitions.Add<WuStateInstalling, WuStateUserInputRequired>();

            stateTransitions.Add<WuStateInstallCompleted, WuStateSearching>();
            stateTransitions.Add<WuStateInstallCompleted, WuStateDownloading>(downloadingPreCon);
            stateTransitions.Add<WuStateInstallCompleted, WuStateInstalling>(installingPreCon);

            stateTransitions.Add<WuStateInstallFailed, WuStateSearching>();
            stateTransitions.Add<WuStateInstallFailed, WuStateDownloading>(downloadingPreCon);
            stateTransitions.Add<WuStateInstallFailed, WuStateInstalling>(installingPreCon);

            stateTransitions.Add<WuStateInstallPartiallyFailed, WuStateSearching>();
            stateTransitions.Add<WuStateInstallPartiallyFailed, WuStateDownloading>(downloadingPreCon);
            stateTransitions.Add<WuStateInstallPartiallyFailed, WuStateInstalling>(installingPreCon);
            stateTransitions.Add<WuStateInstallPartiallyFailed, WuStateRestartSentToOS>();

            stateTransitions.Add<WuStateUserInputRequired, WuStateSearching>();
            stateTransitions.Add<WuStateUserInputRequired, WuStateDownloading>(downloadingPreCon);
            stateTransitions.Add<WuStateUserInputRequired, WuStateInstalling>(installingPreCon);

            stateTransitions.Add<WuStateRebootRequired, WuStateRestartSentToOS>();
            return stateTransitions;
        }

        /// <summary>
        /// Checks if the current state type equals a requested state type.
        /// </summary>
        /// <typeparam name="T">The requested type.</typeparam>
        /// <returns>True, if the current state equals the requested state.</returns>
        private bool Is<T>() => _currentState.GetType() == typeof(T);

        /// <summary>
        /// Checks if a transition from the current state to a requested state is valid.
        /// </summary>
        /// <typeparam name="T">The requested state type.</typeparam>
        /// <returns>True, if the transition is valid with the current state.</returns>
        private bool IsValidTransition<T>()
        {
            var result = StateTransitions.SingleOrDefault(st => st.FromState == _currentState.GetType() && st.ToState == typeof(T));
            return (result != null && (result.Condition == null || result.Condition(_currentState).IsFulfilled));
        }

        /// <summary>
        /// Checks if a transition from the current state to a requested state is valid. If not, an exception will be thrown.
        /// </summary>
        /// <param name="next">The requested state type.</param>
        /// <exception cref="InvalidStateTransitionException">The state transition is not allowed.</exception>
        /// <exception cref="PreConditionNotFulfilledException">Preconditions for the requested state are not fullfiled.</exception>
        private void ThrowIfInvalidStateTransition(Type next)
        {
            if (next == null) throw new ArgumentNullException(nameof(next));
            var result = StateTransitions.SingleOrDefault(st => st.FromState == _currentState.GetType() && st.ToState == next);
            if (result == null) throw new InvalidStateTransitionException(_currentState.GetType(), next);

            Debug.Assert(next == result.ToState);
            if (result.Condition != null)
            {
                var conEval = result.Condition(_currentState);
                if (!conEval.IsFulfilled) throw new PreConditionNotFulfilledException(_currentState.GetType(), next, $"Not allowed or possible: {conEval.Message}");
            }
        }

        /// <summary>
        /// Leaves and disposes the current state and enters a new state.
        /// </summary>
        /// <param name="next">The new state to change to.</param>
        private void EnterState(WuProcessState next)
        {
            if (next == null) throw new ArgumentNullException(nameof(next));
            using (ll.Lock(StateChangingLock))
            {
                if (_currentState != null) ThrowIfInvalidStateTransition(next.GetType());
                Log.Info($"Changing state from '{_currentState?.DisplayName}' to '{next.DisplayName}'.");
                WuProcessState oldState = _currentState;
                try
                {
                    next.EnterState(oldState);
                }
                catch (Exception e)
                {
                    Log.Error($"Failed to enter state '{next?.DisplayName}' properly.", e);
                    Debug.Assert(_currentState != next, "Should not switch to the next state when an error occured.");
                    throw;
                }

                _currentState = next;
                CurrentProgress = null;

                try
                {
                    oldState?.LeaveState();
                }
                catch (Exception e)
                {
                    Debug.Assert(true, $"Failed to leave state '{oldState?.DisplayName}' properly: {e.Message}."); // do not hide this exception in test scenarios
                    Log.Error($"Failed to leave state '{oldState?.DisplayName}' properly.", e);
                }
                finally
                {
                    (oldState as IDisposable)?.Dispose();
                }

                OnStateChanged?.Invoke(this, new StateChangedEventArgs((oldState != null) ? oldState.StateId : WuStateId.Ready, next.StateId));
                if (OnAsyncOperationCompleted != null && oldState is WuStateAsyncJob)
                {
                    if (oldState is WuStateSearching) OnAsyncOperationCompleted(this, new AsyncOperationCompletedEventArgs(AsyncOperation.Searching, next.StateId));
                    else if (oldState is WuStateDownloading) OnAsyncOperationCompleted(this, new AsyncOperationCompletedEventArgs(AsyncOperation.Downloading, next.StateId));
                    else if (oldState is WuStateInstalling) OnAsyncOperationCompleted(this, new AsyncOperationCompletedEventArgs(AsyncOperation.Installing, next.StateId));
                    else throw new NotImplementedException($"For {oldState.GetType()} are no {nameof(OnAsyncOperationCompleted)} event args implemented.");
                }
                Debug.Assert(_currentState == next, "State was not changed.");
                Debug.Assert(CurrentProgress == null, "Should reset progress when state changes.");
            }
        }

        /// <summary>
        /// Wrapper for <see cref="EnterState(WuProcessState)"/>. Only calls <see cref="EnterState(WuProcessState)"/> if the current state allows the change to the requested state.
        /// If the state change is not allowed, the method does nothing.
        /// </summary>
        /// <typeparam name="T">The state type to change to.</typeparam>
        /// <param name="next">The state to change to.</param>
        /// <returns>True, if the state change was allowed and executed. False, if the state change is not allowed.</returns>
        bool EnterStateWhenAllowed<T>(T next) where T : WuProcessState
        {
            if (next == null) throw new ArgumentNullException(nameof(next));
            using (ll.Lock(StateLock))
            {
                if (IsValidTransition<T>())
                {
                    EnterState(next);
                    return true;
                }
                else
                {
                    Log.Warn($"State change from {_currentState.DisplayName} to {next.DisplayName} is not allowed.");
                    return false;
                }
            }
        }

        /// <summary>
        /// The state of the control service.
        /// </summary>
        public StateDescription GetWuStatus()
        {
            using (ll.Lock(StateLock))
            {
                return new StateDescription(_currentState.StateId, _currentState.DisplayName, _currentState.StateDesc, GetInstallerStatus(), GetEnviroment(), CurrentProgress);
            }
        }

        /// <summary>
        /// Callback for async operations (search, download and install) to update the <see cref="CurrentProgress"/> property.
        /// </summary>
        /// <param name="job">The async operation which makes progress.</param>
        /// <param name="currentUpdate">The current update where the async operation is working on.</param>
        /// <param name="currentIndex">The index of the current update.</param>
        /// <param name="count">Total number of updates which the async operation has to proceed.</param>
        private void ProgressChangedCallback(WuStateAsyncJob job, IUpdate currentUpdate, int currentIndex, int count, int percent)
        {
            Debug.Assert(count >= 0 && currentIndex >= 0 && percent >= 0);
            Debug.Assert(percent <= 100);
            Debug.Assert(currentIndex < count);
            Debug.Assert(job != null && currentUpdate != null);

            try
            {
                var progress = new ProgressDescription((currentUpdate != null) ? UpdateHolder.ToUpdateDescription(currentUpdate) : null, currentIndex, count, percent);
                using (ll.Lock(StateLock))
                {
                    if (_currentState == job)
                    {
                        CurrentProgress = progress;
                        Log.Debug("Progress callback received: " + CurrentProgress);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.Assert(false, e.Message); // do not hide in test scenarios
                Log.Error($"Failed to create {nameof(ProgressDescription)}, progress will be ignored.", e);
            }

        }

        #endregion

        #region Search Updates

        /// <summary>
        /// Starts to search for windows updates.
        /// </summary>
        /// <param name="timeoutSec">Seconds until the search will be aborted to prevent never ending searches, must be positive.</param>
        /// <returns>The new state when the method finished.</returns>
        /// <exception cref="InvalidStateTransitionException" />
        /// <exception cref="ArgumentOutOfRangeException" />
        /// <exception cref="System.Runtime.InteropServices.COMException" />
        public WuStateId BeginSearchUpdates(int timeoutSec = (int)DefaultAsyncOperationTimeout.SearchTimeout)
        {
            using (ll.Lock(StateLock))
            {
                ThrowIfInvalidStateTransition(typeof(WuStateSearching));
                Log.Debug("Start async search for updates.");
                EnterState(new WuStateSearching(UpdateSearcher, EndSearchUpdates, TimeoutSearchUpdates, timeoutSec));
            }
            CurrentProgress = new ProgressDescription(); // progress not defineable, searching does not have any progress callbacks
            return _currentState.StateId;
        }

        /// <summary>
        /// Aborts a currently running update search process.
        /// </summary>
        /// <returns>the new state after the abort</returns>
        /// <exception cref="InvalidStateTransitionException" />
        public WuStateId AbortSearchUpdates()
        {
            using (ll.Lock(StateLock))
            {
                if (!Is<WuStateSearching>()) throw new InvalidStateTransitionException(_currentState.GetType(), typeof(WuStateSearchFailed));
                (_currentState as WuStateAsyncJob)?.Abort();
                Log.Info("Update search aborted by user.");
                EnterState(new WuStateSearchFailed(null, "Aborted by user"));
                return _currentState.StateId;
            }
        }

        /// <summary>
        /// Timeout callback for search operations. 
        /// </summary>
        private void TimeoutSearchUpdates(WuStateAsyncJob job, int timeoutSec)
        {
            Log.Error($"Searching for updates aborted: timeout after {timeoutSec} sec.");
            EnterStateWhenAllowed(new WuStateSearchFailed(null, $"Timeout after {timeoutSec} sec."));
        }

        /// <summary>
        /// Callback for completed search operations.
        /// </summary>
        private void EndSearchUpdates(ISearchResult result)
        {
            Log.Debug("Search result callback received: " + result.ResultCode.ToString("G"));
            using (ll.Lock(StateLock))
            {
                if (result.ResultCode == OperationResultCode.orcSucceeded)
                {
                    if (IsValidTransition<WuStateSearchCompleted>())
                    {
                        using (ll.Lock(UpdateHolderLock))
                        {
                            UpdateHolder.SetApplicableUpdates(ToUpdateCollection(result.Updates.OfType<IUpdate>().Where(u => !u.IsInstalled)));
                        }
                        EnterState(new WuStateSearchCompleted(result.Updates));
                    }
                }
                else
                {
                    if (_currentState is WuStateSearchFailed) return;
                    string message = "Could not search updates. " + ((result.Warnings != null) ? String.Join(", ", result.Warnings.OfType<IUpdateException>().Select(u => "HResult: " + u.HResult)) : "");
                    EnterStateWhenAllowed(new WuStateSearchFailed(result.Warnings, message));
                }
            }
        }

        /// <summary>
        /// A list of updates found by the last update search (<see cref="BeginSearchUpdates(int)"/>).
        /// </summary>
        /// <returns>List of updates.</returns>
        public ICollection<UpdateDescription> GetAvailableUpdates()
        {
            List<UpdateDescription> _result = new List<UpdateDescription>();
            using (ll.Lock(UpdateHolderLock))
            {
                if (UpdateHolder.ApplicableUpdates == null) return _result;
                foreach (IUpdate update in UpdateHolder.ApplicableUpdates)
                {
                    _result.Add(UpdateHolder.ToUpdateDescription(update));
                }
                return _result;
            }
        }
        #endregion

        #region Download Updates

        /// <summary>
        /// Starts downloading the updates found with <see cref="BeginSearchUpdates(int)"/>.
        /// Throws an expection if no updates are available, check it with <see cref="GetAvailableUpdates"/>
        /// </summary>
        /// <exception cref="InvalidStateTransitionException" />
        /// <exception cref="PreConditionNotFulfilledException" />
        /// <exception cref="System.Runtime.InteropServices.COMException" />
        public WuStateId BeginDownloadUpdates(int timeoutSec = (int)DefaultAsyncOperationTimeout.DownloadTimeout)
        {
            Log.Debug("Start async download of updates.");
            using (ll.Lock(StateLock))
            {
                AcceptEulas();
                ThrowIfInvalidStateTransition(typeof(WuStateDownloading));
                WuStateDownloading downloading;
                IUpdateCollection updatesToDownload;
                using (ll.Lock(UpdateHolderLock))
                {
                    updatesToDownload = ToUpdateCollection(UpdateHolder.GetSelectedUpdates((u) => u.EulaAccepted && !u.IsInstalled && !u.IsDownloaded));
                }
                downloading = new WuStateDownloading(UpdateDownloader, updatesToDownload, EndDownloadUpdates, TimeoutDownloadUpdates, ProgressChangedCallback, timeoutSec);
                EnterState(downloading);
                return _currentState.StateId;
            }
        }

        /// <summary>
        /// Callback for completed download operations.
        /// </summary>
        private void EndDownloadUpdates(IDownloadResult result, IUpdateCollection updates)
        {
            Log.Debug("Download result callback received: " + result.ResultCode.ToString("G"));
            using (ll.Lock(StateLock))
            {
                switch (result.ResultCode)
                {
                    case OperationResultCode.orcSucceeded:
                        EnterStateWhenAllowed(new WuStateDownloadCompleted(updates, result.HResult));
                        break;
                    case OperationResultCode.orcSucceededWithErrors:
                        EnterStateWhenAllowed(new WuStateDownloadPartiallyFailed(null, "Could not download all updates: HResult " + result.HResult));
                        break;
                    default:
                        if (_currentState is WuStateDownloadFailed) return;
                        EnterStateWhenAllowed(new WuStateDownloadFailed(null, "Could not download updates: HResult" + result.HResult));
                        break;
                }
            }
        }

        /// <summary>
        /// Timeout callback for download operations.
        /// </summary>
        private void TimeoutDownloadUpdates(WuStateAsyncJob job, int timeoutSec)
        {
            Log.Error($"Downloading updates aborted: timeout after {timeoutSec} sec.");
            EnterStateWhenAllowed(new WuStateDownloadFailed(null, $"Timeout after {timeoutSec} sec"));
        }

        /// <summary>
        /// Aborts a currently running downloading process.
        /// </summary>
        /// <returns>The new state after the abort.</returns>
        /// <exception cref="InvalidStateTransitionException" />
        public WuStateId AbortDownloadUpdates()
        {
            using (ll.Lock(StateLock))
            {
                if (!Is<WuStateDownloading>()) throw new InvalidStateTransitionException(_currentState.GetType(), typeof(WuStateDownloadFailed));
                (_currentState as WuStateAsyncJob)?.Abort();
                Log.Info("Download aborted by user.");
                EnterState(new WuStateDownloadFailed(null, "Aborted by user"));
                return _currentState.StateId;
            }
        }

        #endregion

        #region Install Updates

        /// <summary>
        /// Starts to install downloaded updates. Throws an expection if no updates are available, check it with <see cref="GetAvailableUpdates"/>.
        /// </summary>
        /// <exception cref="InvalidStateTransitionException" />
        /// <exception cref="PreConditionNotFulfilledException" />
        /// <exception cref="ArgumentOutOfRangeException" />
        /// <exception cref="System.Runtime.InteropServices.COMException" />
        public WuStateId BeginInstallUpdates(int timeoutSec = (int)DefaultAsyncOperationTimeout.InstallTimeout)
        {
            using (ll.Lock(StateLock))
            {
                AcceptEulas();
                ThrowIfInvalidStateTransition(typeof(WuStateInstalling));
                Log.Debug("Start async installation of updates.");

                IEnumerable<IUpdate> updatesToInstall;
                using (ll.Lock(UpdateHolderLock))
                {
                    updatesToInstall = UpdateHolder.GetSelectedUpdates((u) => u.EulaAccepted && u.IsDownloaded && !u.InstallationBehavior.CanRequestUserInput && !u.IsInstalled);
                }
                Log.Debug("Selected applicable updates for installation: " + String.Join(", ", updatesToInstall.Select(u => u.Identity.UpdateID)));

                WuStateInstalling installing = new WuStateInstalling(UpdateInstaller, ToUpdateCollection(updatesToInstall), EndInstallUpdates, TimeoutInstallUpdates, ProgressChangedCallback, timeoutSec);
                EnterState(installing);
                return _currentState.StateId;
            }
        }

        /// <summary>
        /// Callback for completed installation operations.
        /// </summary>
        private void EndInstallUpdates(IInstallationResult result, IUpdateCollection updates)
        {
            Log.Debug("Installation result callback received: " + result.ResultCode.ToString("G"));
            using (ll.Lock(StateLock))
            {
                if (result.ResultCode == OperationResultCode.orcSucceeded)
                {
                    IEnumerable<IUpdate> notInstalledUpdates;
                    using (ll.Lock(UpdateHolderLock))
                    {
                        notInstalledUpdates = UpdateHolder.GetSelectedUpdates((u) => !u.IsInstalled);
                    }
                    if (result.RebootRequired || SystemInfo.IsRebootRequired()) EnterStateWhenAllowed(new WuStateRebootRequired());
                    else if (notInstalledUpdates.Any(u => !u.EulaAccepted || u.InstallationBehavior.CanRequestUserInput))
                    {
                        if (IsValidTransition<WuStateUserInputRequired>())
                        {
                            string reason = (notInstalledUpdates.Any(u => !u.EulaAccepted)) 
                                ? "Some selected updates were not installed. Please accept the eulas for these updates."
                                : "Some updates were not installed because they can request user input.";
                            EnterState(new WuStateUserInputRequired(reason));
                        }
                    }
                    else
                    {
                        EnterStateWhenAllowed(new WuStateInstallCompleted(updates, result.HResult));
                    }
                }
                else if (result.ResultCode == OperationResultCode.orcSucceededWithErrors)
                {
                    EnterStateWhenAllowed(new WuStateInstallPartiallyFailed(null, "Could not install all updates: HResult " + result.HResult));
                }
                else
                {
                    if (_currentState is WuStateInstallFailed) return;
                    EnterStateWhenAllowed(new WuStateInstallFailed(null, "Could not install updates: HResult " + result.HResult.ToString()));
                }
            }
        }

        /// <summary>
        /// Timeout callback for install operations.
        /// </summary>
        private void TimeoutInstallUpdates(WuStateAsyncJob job, int timeoutSec)
        {
            Log.Error($"Installation of updates aborted: timeout after {timeoutSec} sec.");
            EnterStateWhenAllowed(new WuStateInstallFailed(null, $"Timeout after {timeoutSec} sec."));
        }

        /// <summary>
        /// Aborts a currently running installation process.
        /// </summary>
        /// <returns>The new state after the abort.</returns>
        /// <exception cref="InvalidStateTransitionException" />
        public WuStateId AbortInstallUpdates()
        {
            using (ll.Lock(StateLock))
            {
                if (!Is<WuStateInstalling>()) throw new InvalidStateTransitionException(_currentState.GetType(), typeof(WuStateInstallFailed));
                (_currentState as WuStateAsyncJob)?.Abort();
                Log.Info("Installation aborted by user.");
                EnterState(new WuStateInstallFailed(null, "Aborted by user"));
                return _currentState.StateId;
            }
        }

        /// <summary>
        /// Returns a readiness indicator about the internal update installer component of the OS.
        /// If the installer is not ready, you should not call <see cref="BeginInstallUpdates(int)"/>.
        /// </summary>
        public InstallerStatus GetInstallerStatus()
        {
            if (UpdateInstaller.RebootRequiredBeforeInstallation) return InstallerStatus.RebootRequiredBeforeInstallation;
            if (UpdateInstaller.IsBusy) return InstallerStatus.Busy;
            return InstallerStatus.Ready;
        }

        #endregion

        #region Eula handling

        /// <summary>
        /// Accepts the eula of the specified update.
        /// If no exception is thrown, the eula was successfully accepted.
        /// Does nothing if the eula of the specified update is already accepted.
        /// </summary>
        /// <param name="update">Update whose eula should be accepted.</param>
        /// <exception cref="UpdateNotFoundException" />
        /// <exception cref="ArgumentNullException" />
        /// <exception cref="System.Runtime.InteropServices.COMException" />
        public void AcceptEula(UpdateDescription update)
        {
            if (update == null) throw new ArgumentNullException(nameof(update));
            AcceptEula(update.ID);
        }

        /// <summary>
        /// Accepts the eula of the specified update.
        /// If no exception is thrown, the eula was successfully accepted.
        /// Does nothing if the eula of the specified update is already accepted.
        /// </summary>
        /// <param name="updateId">Id of the update whose eula should be accepted.</param>
        /// <exception cref="UpdateNotFoundException" />
        /// <exception cref="ArgumentNullException" />
        /// <exception cref="System.Runtime.InteropServices.COMException" />
        public void AcceptEula(string updateId)
        {
            if (string.IsNullOrWhiteSpace(updateId)) throw new ArgumentNullException(nameof(updateId));
            using (ll.Lock(UpdateHolderLock))
            {
                if (UpdateHolder.ApplicableUpdates == null) throw new UpdateNotFoundException(updateId, $"No updates are available, please search for updates first.");
                var update = UpdateHolder.ApplicableUpdates.OfType<IUpdate>().SingleOrDefault(u => u.Identity.UpdateID.Equals(updateId));
                if (update == null) throw new UpdateNotFoundException(updateId, $"An update with id '{updateId}' could not be found.");
                if (!update.EulaAccepted) update.AcceptEula();
                Debug.Assert(update.EulaAccepted);
            }
        }

        /// <summary>
        /// Accepts the eulas of all available updates if <see cref="AutoAcceptEulas"/> is enabled.
        /// Does nothing if <see cref="AutoAcceptEulas"/> is false.
        /// </summary>
        private void AcceptEulas()
        {
            if (AutoAcceptEulas)
            {
                using (ll.Lock(UpdateHolderLock))
                {
                    if (UpdateHolder.ApplicableUpdates == null) return;
                    foreach (IUpdate update in UpdateHolder.GetSelectedUpdates())
                    {
                        if (!update.EulaAccepted)
                        {
                            Log.Info("Auto accepting eula for update " + update.Identity.UpdateID);
                            try
                            {
                                update.AcceptEula();
                            }
                            catch (Exception e)
                            {
                                Log.Error($"Could not accept the eula for update {update.Title}.", e);
                            }
                            Debug.Assert(update.EulaAccepted); // do not hide in test scenarios
                        }
                    }
                }
            }
            else
            {
                Log.Info("Auto accept of eulas is disabled, will not download/install updates without accepted eulas.");
            }
        }

        #endregion

        /// <summary>
        /// Tries to restarts the system with a small delay.
        /// There is no gurantee, that the restart occurs.
        /// </summary>
        /// <exception cref="InvalidStateTransitionException" />
        public WuStateId Reboot()
        {
            using (ll.Lock(StateLock))
            {
                ThrowIfInvalidStateTransition(typeof(WuStateRestartSentToOS));
                Log.Info("Reboot request received.");
                EnterState(new WuStateRestartSentToOS());
                return _currentState.StateId;
            }
        }

        /// <summary>
        /// Returns patching releated informations about the operating system.
        /// </summary>
        public WuEnviroment GetEnviroment()
        {
            return new WuEnviroment(
                SystemInfo.GetFQDN(),
                SystemInfo.GetOperatingSystemName(),
                SystemInfo.GetWuServer(),
                SystemInfo.GetTargetGroup(),
                SystemInfo.GetUptime(),
                SystemInfo.GetFreeSpace());
        }

        /// <summary>
        /// Selects a specific update for download and installation. Does nothing if the update is already selected.
        /// After <see cref="BeginSearchUpdates(int)"/> found some applicable updates, only important or none of them gets automatically selected (depends on <see cref="AutoSelectUpdates"/>).
        /// </summary>
        /// <param name="update">The update to select.</param>
        /// <exception cref="UpdateNotFoundException" />
        /// <exception cref="ArgumentNullException" />
        public void SelectUpdate(UpdateDescription update)
        {
            if (update == null) throw new ArgumentNullException(nameof(update));
            SelectUpdate(update.ID);
        }

        /// <summary>
        /// Selects a specific update for download and installation. Does nothing if the update is already selected.
        /// After <see cref="BeginSearchUpdates(int)"/> found some applicable updates, only important or none of them gets automatically selected (depends on <see cref="AutoSelectUpdates"/>).
        /// </summary>
        /// <param name="updateId">The id of the update to select.</param>
        /// <exception cref="UpdateNotFoundException" />
        public void SelectUpdate(string updateId) => UpdateHolder.SelectUpdate(updateId);

        /// <summary>
        /// Unselects a specific update. Does nothing if the update is already not selected.
        /// </summary>
        /// <param name="update">The update to unselect.</param>
        /// <exception cref="UpdateNotFoundException" />
        /// <exception cref="ArgumentNullException" />
        public void UnselectUpdate(UpdateDescription update)
        {
            if (update == null) throw new ArgumentNullException(nameof(update));
            UnselectUpdate(update.ID);
        }

        /// <summary>
        /// Unselects a specific update. Does nothing if the update is already not selected.
        /// </summary>
        /// <param name="updateId">The id of the update to unselect.</param>
        /// <exception cref="UpdateNotFoundException" />
        public void UnselectUpdate(string updateId) => UpdateHolder.UnselectUpdate(updateId);
    }
}