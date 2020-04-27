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
using System.Threading;
using WindowsUpdateApiController.Helper;
using WindowsUpdateApiController.States;
using WUApiLib;
using WuDataContract.Enums;

namespace WindowsUpdateApiControllerUnitTest.Mocks
{
    class WuStateAsyncJobProxy : WuStateAsyncJob
    {

        public ManualResetEvent OnTimeoutSignal = new ManualResetEvent(false);

        public bool AbortCalled = false;
        public bool TimeoutCalled = false;

        public WuStateAsyncJobProxy(WuStateId id, string displayName, int timeoutSec, TimeoutCallback timeoutCallback, ProgressChangedCallback progressCallback)
            : base(id, displayName, timeoutSec, timeoutCallback, progressCallback)
        { }

        public WuStateAsyncJobProxy(WuStateId id, string displayName, int timeoutSec) : base(id, displayName, timeoutSec, (a, b) => { }, (a, b, c, d, e) => { })
        { }

        new public WuApiJobAdapter Job
        {
            get { return base.Job; }
            set { base.Job = value; }
        }

        protected override void EnterStateInternal(WuProcessState oldState)
        {
            OnTimeoutSignal.Reset();
        }

        public new ProgressChangedCallback ProgressChangedCallbackDelegate => base.ProgressChangedCallbackDelegate;
        public new TimeoutCallback TimeoutCallbackDelegate => base.TimeoutCallbackDelegate;

        protected override void OnTimeout()
        {
            lock(JobLock)
            {
                OnTimeoutSignal.Set();
                base.OnTimeout();
            }
        }

        public void SimulateOnProgressNow(IUpdate update, int currentIndex, int count, int percent)
        {
            OnProgress(update, currentIndex, count, percent);
        }

        public override void Abort()
        {
            AbortCalled = true;
            base.Abort();
        }

        protected override void Dispose(bool disposing)
        {
            lock (JobLock)
            {
                try
                {
                    OnTimeoutSignal.Dispose();
                }
                finally {
                    base.Dispose(disposing);
                }           
            }
        }
    }
}
