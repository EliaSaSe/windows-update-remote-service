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
using WUApiLib;
using WuDataContract.Enums;
using WindowsUpdateApiController.Helper;

namespace WindowsUpdateApiController.States
{
    class WuStateInstalling : WuStateAsyncJob
    {
        IUpdateInstaller _uInstaller;
        IUpdateCollection _updates;

        InstallCompletedCallback _completedCallback;
        public delegate void InstallCompletedCallback(IInstallationResult result, IUpdateCollection updates);

        class CallbackReceiver : IInstallationProgressChangedCallback, IInstallationCompletedCallback
        {
            WuStateInstalling _state;
            public CallbackReceiver(WuStateInstalling state)
            {
                _state = state;
            }

            public void Invoke(IInstallationJob installationJob, IInstallationCompletedCallbackArgs callbackArgs) => _state.Invoke(installationJob, callbackArgs);

            public void Invoke(IInstallationJob installationJob, IInstallationProgressChangedCallbackArgs callbackArgs) => _state.Invoke(installationJob, callbackArgs);
        }

        public WuStateInstalling(IUpdateInstaller uInstaller, IUpdateCollection updates,
    InstallCompletedCallback completedCallback, TimeoutCallback timeoutCallback, ProgressChangedCallback progressCallback,
    int timeoutSec) : base(WuStateId.Installing, "Installing Updates", timeoutSec, timeoutCallback, progressCallback)
        {
            if (uInstaller == null) throw new ArgumentNullException(nameof(uInstaller));
            if (updates == null) throw new ArgumentNullException(nameof(updates));
            if (completedCallback == null) throw new ArgumentNullException(nameof(completedCallback));

            _uInstaller = uInstaller;
            _updates = updates;
            _completedCallback = completedCallback;
        }

        private IInstallationJob InstallJob => Job.InternalJobObject as IInstallationJob;


        protected override void EnterStateInternal(WuProcessState oldState)
        {
            lock (JobLock)
            {
                if (_uInstaller.IsBusy) throw new InvalidOperationException("Update installer is busy.");
                if (_uInstaller.RebootRequiredBeforeInstallation) throw new InvalidOperationException("A reboot is required before update installation can start.");

                _uInstaller.Updates = (UpdateCollection)_updates;
                var callbackReceiver = new CallbackReceiver(this);
                Job = new WuApiInstallJobAdapter(_uInstaller.BeginInstall(callbackReceiver, callbackReceiver, null));
                StateDesc = $"Starting installation of {_updates.Count} update(s)";
            }
        }

        /// <summary>
        /// Used by the windows update api. Do not call this method.
        /// </summary>
        void Invoke(IInstallationJob installationJob, IInstallationCompletedCallbackArgs callbackArgs)
        {
            bool doCallback = false;
            lock (JobLock)
            {
                if (Job != null && Job.InternalJobObject == installationJob && Job.IsCompleted)
                {
                    StopTimeoutTimer();
                    doCallback = true;
                }
            }
            // calling the callback inside the lock can lead to deadlocks when the callback tries to dispose this object
            if (doCallback) _completedCallback(_uInstaller.EndInstall(InstallJob), _uInstaller.Updates);
        }

        /// <summary>
        /// Used by the windows update api. Do not call this method.
        /// </summary>
        void Invoke(IInstallationJob installationJob, IInstallationProgressChangedCallbackArgs callbackArgs)
        {
            bool doCallback = true;
            lock (JobLock)
            {
                if (Job != null && Job.InternalJobObject == installationJob && !Job.IsCompleted)
                {
                    StateDesc = installationJob.Updates[callbackArgs.Progress.CurrentUpdateIndex].Title;
                    doCallback = true;
                }
            }
            if (doCallback) // calling the callback inside the lock can lead to deadlocks when the callback tries to dispose this object
            {
                OnProgress(installationJob.Updates[callbackArgs.Progress.CurrentUpdateIndex], callbackArgs.Progress.CurrentUpdateIndex, installationJob.Updates.Count, callbackArgs.Progress.PercentComplete);
            }
        }
    }
}
