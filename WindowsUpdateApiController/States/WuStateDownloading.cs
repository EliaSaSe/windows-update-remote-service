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
    internal class WuStateDownloading : WuStateAsyncJob 
    {
        private IUpdateDownloader _uDownloader;
        private IUpdateCollection _updates;

        DownloadCompletedCallback _completedCallback;
        public delegate void DownloadCompletedCallback(IDownloadResult result, IUpdateCollection updates);

        class CallbackReceiver : IDownloadProgressChangedCallback, IDownloadCompletedCallback
        {
            WuStateDownloading _state;
            public CallbackReceiver(WuStateDownloading state)
            {
                _state = state;
            }

            public void Invoke(IDownloadJob downloadJob, IDownloadCompletedCallbackArgs callbackArgs) => _state.Invoke(downloadJob, callbackArgs);

            public void Invoke(IDownloadJob downloadJob, IDownloadProgressChangedCallbackArgs callbackArgs) => _state.Invoke(downloadJob, callbackArgs);
        }

        public WuStateDownloading(IUpdateDownloader uDownloader, IUpdateCollection updates,
            DownloadCompletedCallback completedCallback, TimeoutCallback timeoutCallback, ProgressChangedCallback progressCallback,
            int timeoutSec) : base(WuStateId.Downloading, "Downloading Updates", timeoutSec, timeoutCallback, progressCallback)
        {
            if (uDownloader == null) throw new ArgumentNullException(nameof(uDownloader));
            if (updates == null) throw new ArgumentNullException(nameof(updates));
            if (completedCallback == null) throw new ArgumentNullException(nameof(completedCallback));

            _uDownloader = uDownloader;
            _updates = updates;
            _completedCallback = completedCallback;
        }

        private IDownloadJob DownloadJob => Job.InternalJobObject as IDownloadJob;

        protected override void EnterStateInternal(WuProcessState oldState)
        {
            lock (JobLock)
            {
                _uDownloader.Updates = (UpdateCollection)_updates;
                var callbackReceiver = new CallbackReceiver(this);
                Job = new WuApiDownloadJobAdapter(_uDownloader.BeginDownload(callbackReceiver, callbackReceiver, null));
                StateDesc = $"Starting download of {_updates.Count} update(s)";
            }
        }

        /// <summary>
        /// Callback. Used by the windows update api when the download completes. Do not call this method.
        /// </summary>
        void Invoke(IDownloadJob downloadJob, IDownloadCompletedCallbackArgs callbackArgs)
        {
            bool doCallback = false;
            lock (JobLock)
            {
                if (Job != null && Job.InternalJobObject == downloadJob && Job.IsCompleted)
                {
                    StopTimeoutTimer();
                    doCallback = true;
                }
            }
            // calling the callback inside the lock can lead to deadlocks when the callback tries to dispose this object
            if (doCallback) _completedCallback(_uDownloader.EndDownload(DownloadJob), _uDownloader.Updates);         
        }

        /// <summary>
        /// Callback. Used by the windows update api when the download makes progress. Do not call this method.
        /// </summary>
        void Invoke(IDownloadJob downloadJob, IDownloadProgressChangedCallbackArgs callbackArgs)
        {
            bool doCallback = false;
            lock (JobLock)
            {
                if (Job != null && Job.InternalJobObject == downloadJob && !Job.IsCompleted)
                {
                    StateDesc = downloadJob.Updates[callbackArgs.Progress.CurrentUpdateIndex].Title;
                    doCallback = true;
                }
            }
            if (doCallback) // calling the callback inside the lock can lead to deadlocks when the callback tries to dispose this object
            {
                OnProgress(downloadJob.Updates[callbackArgs.Progress.CurrentUpdateIndex], callbackArgs.Progress.CurrentUpdateIndex, downloadJob.Updates.Count, callbackArgs.Progress.PercentComplete);
            }
        }
    }
}
