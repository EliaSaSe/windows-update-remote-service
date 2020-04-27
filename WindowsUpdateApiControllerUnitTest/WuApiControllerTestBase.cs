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
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WindowsUpdateApiController.Helper;
using System.Threading;
using WindowsUpdateApiController;
using WUApiLib;
using WuApiMocks;
using WuDataContract.Enums;

namespace WindowsUpdateApiControllerUnitTest
{
    public abstract class WuApiControllerTestBase
    {
        internal readonly UpdateCollectionFake.Factory UpdateCollectionFactory = new UpdateCollectionFake.Factory();
        internal readonly ISystemInfo SystemInfo;

        public WuApiControllerTestBase()
        {
            SystemInfo = CommonMocks.GetSystemInfo();
        }

        internal IUpdateCollection ToUpdateCollection(params IUpdate[] updates)
        {
            var coll = UpdateCollectionFactory.GetInstance();
            foreach (var update in updates)
            {
                coll.Add(update);
            }
            return coll;
        }

        /// <summary>
        /// Waits until the <see cref="WuStateId"/> of the given <see cref="WuApiController"/> has changed and compares the new <see cref="WuStateId"/> with an expected <see cref="WuStateId"/>.
        /// This method blocks while it's waiting. Is the service already in the expected state, the method immediately returns.
        /// </summary>
        /// <param name="wu">Service to observe.</param>
        /// <param name="expected">Expected state after the current state has changed.</param>
        /// <param name="timeoutMs">Timeout to prevents endless waiting.</param>
        internal void WaitForStateChange(WuApiController wu, WuStateId expected, int timeoutMs = 5000)
        {
            WuStateId current = wu.GetWuStatus().StateId;

            if (current == expected) return;

            ManualResetEvent stateChangeSignal = new ManualResetEvent(false);
            WuApiController.StateChangedHandler handler = (sender, e) => stateChangeSignal.Set();      

            try
            {
                wu.OnStateChanged += handler;
                if (!stateChangeSignal.WaitOne(timeoutMs))
                {                   
                    if (wu.GetWuStatus().Equals(expected)) return; // handler registered to late to the event, so we didn't see the state change?
                    Assert.Fail($"State change from '{current.ToString()}' to '{expected.ToString()}' takes to long, timeout after {timeoutMs} ms with state '{wu.GetWuStatus().ToString()}'.");
                }
                if (!wu.GetWuStatus().Equals(expected))
                {
                    Assert.Fail($"Unexpected object state: {wu.GetWuStatus().ToString()}, should be {expected.ToString()}.");
                }
            }
            finally {
                stateChangeSignal.Dispose();
                wu.OnStateChanged -= handler;               
            }
        }
    }
}
