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
using Moq;
using System;
using System.Threading;
using WindowsUpdateApiController.Helper;
using WindowsUpdateApiController.States;
using WindowsUpdateApiControllerUnitTest.Mocks;
using WUApiLib;
using WuApiMocks;
using WuDataContract.Enums;

namespace WindowsUpdateApiControllerUnitTest
{
    [TestClass]
    public class WuStateAsyncJobTest
    {
        readonly MockRepository MoqFactory = new MockRepository(MockBehavior.Strict) { };

        [TestMethod, TestCategory("Timeouts")]
        public void Should_ContainSpecifiedTimeOut_When_CreateWuStateAsyncJob()
        {
            int timeout = 34;
            WuStateAsyncJob.ProgressChangedCallback progressCallback = (a, b, c, d, e) => { };
            WuStateAsyncJob.TimeoutCallback timeoutCallback = (a, b) => { };
            using (var state = new WuStateAsyncJobProxy(WuStateId.Downloading, "name", timeout, timeoutCallback, progressCallback))
            {
                Assert.AreEqual(timeout, state.TimeoutSec);
                Assert.AreSame(progressCallback, state.ProgressChangedCallbackDelegate);
                Assert.AreSame(timeoutCallback, state.TimeoutCallbackDelegate);
            }
        }

        [TestMethod, TestCategory("Timeouts")]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void Should_NotAllowNegativeTimeout_When_CreateWuStateAsyncJob()
        {
            WuStateAsyncJob state = new WuStateAsyncJobProxy(WuStateId.Downloading, "name", -1);
        }

        [TestMethod, TestCategory("Timeouts")]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Should_NotAllowNullTimeoutDelegate_When_CreateWuStateAsyncJob()
        {
            WuStateAsyncJob state = new WuStateAsyncJobProxy(WuStateId.Downloading, "name", 1, null, null);
        }

        [TestMethod]
        [ExpectedException(typeof(ObjectDisposedException))]
        public void Should_NotAllowReEnter_When_WuStateAsyncJobIsDisposed()
        {
            WuStateAsyncJob state = new WuStateAsyncJobProxy(WuStateId.Downloading, "name", 1);
            state.Dispose();
            state.EnterState(new WuStateReady());
        }

        [TestMethod]
        public void Should_ReturnTrue_When_CallIsDisposedAfterDispose()
        {
            WuStateAsyncJob state = new WuStateAsyncJobProxy(WuStateId.Downloading, "name", 0);
            Assert.IsFalse(state.IsDisposed);
            state.Dispose();
            Assert.IsTrue(state.IsDisposed);
        }

        [TestMethod]
        public void Should_CallAbort_When_Dispose()
        {
            WuStateAsyncJobProxy state = new WuStateAsyncJobProxy(WuStateId.Downloading, "name", 0);
            state.Dispose();
            Assert.IsTrue(state.AbortCalled);
        }

        [TestMethod]
        public void Should_CallAbort_When_CallLeaveState()
        {
            using (var state = new WuStateAsyncJobProxy(WuStateId.Downloading, "name", 0))
            {
                state.LeaveState();
                Assert.IsTrue(state.AbortCalled);
            }
        }

        [TestMethod, TestCategory("Timeouts")]
        public void Should_CallOnTimeout_When_TimeRunsOut()
        {
            int timeout = 1;
            WuStateAsyncJobProxy state = new WuStateAsyncJobProxy(WuStateId.Downloading, "name", timeout);
            state.EnterState(new WuStateReady());

            if (!state.OnTimeoutSignal.WaitOne((int)(timeout * 1000 * 1.5)))
            {
                Assert.Fail($"OnTimeout was not called");
            }
            Assert.IsFalse(state.IsRunning);
            state.Dispose();
        }

        [TestMethod, TestCategory("Callback"), TestCategory("Timeouts")]
        public void Should_CallTimeoutCallback_When_TimeRunsOut()
        {
            ManualResetEvent callbackSignal = new ManualResetEvent(false);
            int timeoutSec = 1;

            WuStateAsyncJob.TimeoutCallback callback = (x, y) => { callbackSignal.Set(); };

            var job = (new WuStateAsyncJobProxy(WuStateId.Downloading, "name", timeoutSec, callback, null));

            job.EnterState(new WuStateReady());
            if (!callbackSignal.WaitOne(timeoutSec * 2000))
            {
                Assert.Fail($"timeout callback was not called");
            }
            Assert.IsFalse(job.IsRunning);
            job.Dispose();
        }

        [TestMethod, TestCategory("Callback")]
        public void Should_CallProgressCallback_When_OnProgressIsCalled()
        {
            ManualResetEvent callbackSignal = new ManualResetEvent(false);
            int timeoutSec = 1;
            IUpdate callbackUpdate = null, inputUpdate = new UpdateFake("id");
            int callbackIndex = -1, callbackCount = -1, callbackPercent = -1, inputIndex = 0, inputCount = 1, inputPercent = 37;
            WuStateAsyncJob callbackJob = null;

            WuStateAsyncJob.ProgressChangedCallback callback = (sender, update, index, count, percent) =>
            {
                callbackJob = sender;
                callbackUpdate = update;
                callbackIndex = index;
                callbackCount = count;
                callbackPercent = percent;
                callbackSignal.Set();
            };

            var job = (new WuStateAsyncJobProxy(WuStateId.Downloading, "name", timeoutSec, (x, y) => { }, callback));

            job.EnterState(new WuStateReady());
            job.SimulateOnProgressNow(inputUpdate, inputIndex, inputCount, inputPercent);
            if (!callbackSignal.WaitOne(timeoutSec * 2000))
            {
                Assert.Fail($"progress callback was not called");
            }
            Assert.AreEqual(callbackIndex, inputIndex);
            Assert.AreEqual(callbackCount, inputCount);
            Assert.AreSame(callbackUpdate, inputUpdate);
            Assert.AreSame(callbackJob, job);

            job.Dispose();
        }

        [TestMethod]
        public void Should_UpdateRunningIndicator_When_StartAndAbortTimer()
        {
            using (WuStateAsyncJobProxy state = new WuStateAsyncJobProxy(WuStateId.Downloading, "name", 10000))
            {
                Assert.IsFalse(state.IsRunning);
                state.EnterState(new WuStateReady());
                Assert.IsTrue(state.IsRunning);
                state.Abort();
                Assert.IsFalse(state.IsRunning);
            }

        }

        [TestMethod, TestCategory("Abort")]
        public void Should_CallRequestAbort_When_AbortJob()
        {
            var job = MoqFactory.Create<WuApiJobAdapter>(MockBehavior.Loose);
            job.Setup(j => j.RequestAbort());
            using (WuStateAsyncJobProxy state = new WuStateAsyncJobProxy(WuStateId.Downloading, "name", 10000))
            {
                state.Job = job.Object;
                state.EnterState(new WuStateReady());
                state.Abort();
                job.Verify(j => j.RequestAbort(), Times.Once);
            }
        }

        [TestMethod, TestCategory("Callback")]
        public void Should_SuppressProgessCallback_When_NewProgessEqualsOldProgress()
        {
            int progressChangedCallbackCounter = 0;
            WuStateAsyncJob.ProgressChangedCallback progressCallback = (j, cu, ci, co, p) => { progressChangedCallbackCounter++; };
            WuStateAsyncJob.TimeoutCallback timeoutCallback = (j, t) => { Assert.Fail("Should not be called."); };

            UpdateFake update = new UpdateFake("update");
            Tuple<IUpdate, int, int, int>[] progessChanges = {
                new Tuple<IUpdate, int, int, int>(update, 0, 1, 0),
                new Tuple<IUpdate, int, int, int>(update, 0, 1, 0),
                new Tuple<IUpdate, int, int, int>(update, 0, 1, 5)
            };

            using (var asyncJob = new WuStateAsyncJobProxy(WuStateId.Downloading, "name", 10000, timeoutCallback, progressCallback))
            {
                foreach (var pc in progessChanges)
                {
                    asyncJob.SimulateOnProgressNow(pc.Item1, pc.Item2, pc.Item3, pc.Item4);
                }
            }
            Assert.AreEqual(2, progressChangedCallbackCounter);
        }

        [TestMethod, TestCategory("Callback")]
        public void Should_CallProgressCallback_When_NewProgessNotEqualsOldProgress()
        {
            int progressChangedCallbackCounter = 0;
            WuStateAsyncJob.ProgressChangedCallback progressCallback = (j, cu, ci, co, p) => { progressChangedCallbackCounter++; };
            WuStateAsyncJob.TimeoutCallback timeoutCallback = (j, t) => { Assert.Fail("Should not be called."); };

            UpdateFake update1 = new UpdateFake("update1");
            UpdateFake update2 = new UpdateFake("update2");

            Tuple<IUpdate, int, int, int>[] progessChanges = {
                new Tuple<IUpdate, int, int, int>(update1, 0, 1, 0),
                new Tuple<IUpdate, int, int, int>(update1, 0, 1, 100),
                new Tuple<IUpdate, int, int, int>(update1, 1, 1, 100),
                new Tuple<IUpdate, int, int, int>(update2, 1, 1, 100)
            };

            using (var asyncJob = new WuStateAsyncJobProxy(WuStateId.Downloading, "name", 10000, timeoutCallback, progressCallback))
            {
                foreach (var pc in progessChanges)
                {
                    asyncJob.SimulateOnProgressNow(pc.Item1, pc.Item2, pc.Item3, pc.Item4);
                }
            }
            Assert.AreEqual(progessChanges.Length, progressChangedCallbackCounter);
        }
    }
}
