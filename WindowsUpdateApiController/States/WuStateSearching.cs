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
    internal class WuStateSearching : WuStateAsyncJob
    {
        IUpdateSearcher _searcher;
        SearchCompletedCallback _completedCallback;
        public delegate void SearchCompletedCallback(ISearchResult result);

        class CallbackReceiver : ISearchCompletedCallback
        {
            WuStateSearching _state;
            public CallbackReceiver(WuStateSearching state)
            {
                _state = state;
            }

            public void Invoke(ISearchJob searchJob, ISearchCompletedCallbackArgs callbackArgs) => _state.Invoke(searchJob, callbackArgs);
        }

        public WuStateSearching(IUpdateSearcher searcher, SearchCompletedCallback completedCallback, TimeoutCallback timeoutCallback, int timeoutSec) : base(WuStateId.Searching, "Searching Updates", timeoutSec, timeoutCallback, null)
        {
            if (searcher == null) throw new ArgumentNullException(nameof(searcher));
            if (completedCallback == null) throw new ArgumentNullException(nameof(completedCallback));

            _completedCallback = completedCallback;
            _searcher = searcher;
        }

        /// <summary>
        /// Used by the windows update api. Do not call this method.
        /// </summary>
        void Invoke(ISearchJob searchJob, ISearchCompletedCallbackArgs callbackArgs)
        {
            bool doCallback = false;
            lock (JobLock)
            {
                if (Job != null && Job.InternalJobObject == searchJob && Job.IsCompleted)
                {
                    StopTimeoutTimer();                  
                    doCallback = true;
                }
            }
            // calling the callback inside the lock can lead to deadlocks when the callback tries to dispose this object
            if (doCallback) _completedCallback(_searcher.EndSearch(searchJob));
        }

        protected override void EnterStateInternal(WuProcessState oldState)
        {
            lock (JobLock)
            {
                var callbackReceiver = new CallbackReceiver(this);
                Job = new WuApiSearchJobAdapter(_searcher.BeginSearch("IsInstalled=0 and Type='Software' and IsHidden=0", callbackReceiver, null));
            }
        }
    }
}
