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
using System.Timers;
using WindowsUpdateApiController.Helper;
using WUApiLib;
using WuDataContract.Enums;

namespace WindowsUpdateApiController.States
{
    /// <summary>
    /// Represents WuStates that are running asynchronous tasks while they are the active/current state.
    /// </summary>
    internal abstract class WuStateAsyncJob : WuProcessState, IDisposable
    {
        readonly public int TimeoutSec;
        Timer _timeoutTimer;
        bool _isDisposed = false;
        WuApiJobAdapter _job;
        object _jobLock = new object();
        protected readonly TimeoutCallback TimeoutCallbackDelegate;
        protected readonly ProgressChangedCallback ProgressChangedCallbackDelegate;

        public delegate void TimeoutCallback(WuStateAsyncJob job, int timeoutSec);
        public delegate void ProgressChangedCallback(WuStateAsyncJob job, IUpdate currentUpdate, int currentIndex, int count, int percent);

        /// <param name="id">Id of the state.</param>
        /// <param name="displayName">Displayname of the state.</param>
        /// <param name="timeoutSec">Seconds, after the asynchronous task should be aborted.</param>
        /// <param name="timeoutCallback">Callback to report, that the task was aborted because of a timeout.</param>
        /// <param name="progressCallback">Callback to report, that the task makes progress.</param>
        public WuStateAsyncJob(WuStateId id, string displayName, int timeoutSec, TimeoutCallback timeoutCallback, ProgressChangedCallback progressCallback) : base(id, displayName)
        {
            if (timeoutSec < 0) throw new ArgumentOutOfRangeException(nameof(timeoutSec), "Negative timeout is not allowed.");
            if (timeoutSec > int.MaxValue / 1000) throw new ArgumentOutOfRangeException(nameof(timeoutSec), $"Max timeout is {int.MaxValue / 1000} sec.");
            if (timeoutCallback == null) throw new ArgumentNullException(nameof(timeoutCallback));

            TimeoutSec = timeoutSec;
            TimeoutCallbackDelegate = timeoutCallback;
            ProgressChangedCallbackDelegate = progressCallback;
        }

        /// <summary>
        /// The async job of this wu.-state.
        /// </summary>
        protected WuApiJobAdapter Job
        {
            get { return _job; }
            set { _job = value; }
        }

        /// <summary>
        /// Lock object for operations which could change the state of the <see cref="Job"/> object.  
        /// </summary>
        protected object JobLock
        {
            get { return _jobLock; }
        }

        #region timer/timeout handling

        protected void StartTimeoutTimer()
        {
            if (_timeoutTimer == null)
            {
                _timeoutTimer = new Timer(TimeoutSec * 1000);
                _timeoutTimer.Elapsed += (sender, e) => { OnTimeout(); };
            }
            _timeoutTimer.Start();
        }

        protected void StopTimeoutTimer()
        {
            if (_timeoutTimer != null) _timeoutTimer.Stop();
        }

        /// <summary>
        /// Called when the timeout timer reaches <see cref="TimeoutSec"/>.
        /// Will abort a currently running async operation.
        /// Invokes <see cref="TimeoutCallbackDelegate"/>.
        /// </summary>
        protected virtual void OnTimeout()
        {
            lock (JobLock)
            {
                if ((Job != null && Job.IsCompleted) || IsDisposed) return;
                Abort();               
            }
            TimeoutCallbackDelegate(this, TimeoutSec);
        }

        /// <summary>
        /// Indicator for a running async operation. True, when <see cref="_timeoutTimer"/> is running.
        /// </summary>
        public bool IsRunning
        {
            get
            {
                lock (JobLock)
                {
                    if (_timeoutTimer == null) return false;
                    return _timeoutTimer.Enabled;
                }
            }
        }

        #endregion

        public override void EnterState(WuProcessState oldState)
        {
            lock (JobLock)
            {
                if (IsDisposed) throw new ObjectDisposedException(this.GetType().FullName);
                if (IsRunning) return;

                EnterStateInternal(oldState);
                StartTimeoutTimer();
            }
        }

        protected abstract void EnterStateInternal(WuProcessState oldState);

        /// <summary>
        /// Saves the parameter values of the last call of <see cref="OnProgress(IUpdate, int, int, int)"/>.
        /// </summary>
        private Tuple<IUpdate, int, int, int> _lastOnProgessValues = null;
        private object _lastOnProgessValuesLock = new object();

        /// <summary>
        /// Called when the current job makes progress.
        /// </summary>
        protected virtual void OnProgress(IUpdate currentUpdate, int currentIndex, int count, int percent)
        {
            Tuple<IUpdate, int, int, int> lastValues;
            lock (_lastOnProgessValuesLock)
            {
                lastValues = _lastOnProgessValues;
                _lastOnProgessValues = new Tuple<IUpdate, int, int, int>(currentUpdate, currentIndex, count, percent);
            }
            // Suppress irrelevant progress changes send from the wuapi.
            if (!(lastValues != null
                && currentUpdate.Identity.UpdateID == lastValues.Item1.Identity.UpdateID
                && currentIndex == lastValues.Item2
                && count == lastValues.Item3
                && percent == lastValues.Item4))
            {
                ProgressChangedCallbackDelegate?.Invoke(this, currentUpdate, currentIndex, count, percent);
            }            
        }

        /// <summary>
        /// Aborts the currently running async operation.
        /// </summary>
        public virtual void Abort()
        {
            lock (JobLock)
            {
                try
                {
                    if (Job != null && !Job.IsCompleted) Job.RequestAbort();
                }
                finally
                {
                    if (IsRunning) StopTimeoutTimer();
                }

            }
        }

        public override void LeaveState() => Abort();

        #region Dispose
        public bool IsDisposed
        {
            get { return _isDisposed; }
        }

        protected virtual void Dispose(bool disposing)
        {
            lock (JobLock)
            {
                try
                {

                    if (disposing)
                    {
                        Abort();
                        if (_timeoutTimer != null) _timeoutTimer.Dispose();
                        //Job?.CleanUp();
                    }
                }
                finally
                {
                    _isDisposed = true;
                }
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}