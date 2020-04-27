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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using WindowsUpdateApiController;
using WindowsUpdateApiController.Exceptions;
using WuApiMocks;
using WuDataContract.DTO;
using WuDataContract.Enums;

namespace WindowsUpdateApiControllerUnitTest
{
    [TestClass]
    public class WuApiControllerTest_General : WuApiControllerTestBase
    {
        [TestMethod]
        public void Should_NotThrowExpection_When_CreateWuApiControllerWithDefaultConstructor()
        {
            using (WuApiController wu = new WuApiController())
            {
                Assert.IsTrue(wu.GetAvailableUpdates().Count == 0);
            }
        }

        [TestMethod, TestCategory("Events")]
        public void Should_FireStateChangedEvent_When_StateHasChanged()
        {
            var session = new UpdateSessionFake(true);
            using (WuApiController wu = new WuApiController(session, UpdateCollectionFactory, SystemInfo))
            {

                bool validEventState = false;
                WuStateId oldStateExpected = WuStateId.Searching;
                WuStateId newStateExpected = WuStateId.SearchCompleted;
                wu.OnStateChanged += (sender, args) => validEventState = (args.OldState == oldStateExpected && args.NewState == newStateExpected) ? true : false;

                wu.BeginSearchUpdates();
                WaitForStateChange(wu, WuStateId.SearchCompleted);
                Assert.IsTrue(validEventState);
            }
        }

        [TestMethod, TestCategory("Events")]
        public void Should_FireProgressChangedEvent_When_ProgressHasChanged()
        {
            var session = new UpdateSessionFake(true);

            var u1 = new UpdateFake("1", true);
            var u2 = new UpdateFake("2", true);

            List<Action> asserts = new List<Action>(); // assert in others threads will not be caught

            session.SearcherMock.FakeSearchResult = CommonMocks.GetSearchResult(ToUpdateCollection(u1, u2));
            session.DownloaderMock.FakeDownloadTimeMs = 101;

            using (WuApiController wu = new WuApiController(session, UpdateCollectionFactory, SystemInfo))
            {
                wu.AutoAcceptEulas = true;
                wu.BeginSearchUpdates();
                WaitForStateChange(wu, WuStateId.SearchCompleted);
                wu.BeginDownloadUpdates();

                wu.OnProgressChanged += (sender, args) =>
                {
                    if (args.Progress.CurrentUpdate.ID == u1.Identity.UpdateID)
                    {
                        asserts.Add(() => { Assert.AreEqual(args.Progress.CurrentIndex, 0); });
                        asserts.Add(() => { Assert.AreEqual(args.Progress.Count, 2); });
                    }
                    else if (args.Progress.CurrentUpdate.ID == u2.Identity.UpdateID)
                    {
                        asserts.Add(() => { Assert.AreEqual(args.Progress.CurrentIndex, 1); });
                        asserts.Add(() => { Assert.AreEqual(args.Progress.Count, 2); });
                    }
                    else
                    {
                        asserts.Add(() => { Assert.Fail("unkown update"); });
                    }
                };
                WaitForStateChange(wu, WuStateId.DownloadCompleted);
                asserts.ForEach(a => a()); // fire asserts
            }
        }

        [TestMethod, TestCategory("Events")]
        public void Should_FireAsyncOperationCompletedEvent_When_AsyncOperationCompleted()
        {
            var session = new UpdateSessionFake(true);
            session.SearcherMock.FakeSearchResult = CommonMocks.GetSearchResult(ToUpdateCollection(new UpdateFake("u", true)));          
            using (WuApiController wu = new WuApiController(session, UpdateCollectionFactory, SystemInfo))
            {
                wu.AutoAcceptEulas = true;

                ManualResetEvent eventSignal = new ManualResetEvent(false);
                AsyncOperation result = AsyncOperation.Installing;
                wu.OnAsyncOperationCompleted += (sender, args) => { result = args.Operation; eventSignal.Set(); };

                eventSignal.Reset();
                wu.BeginSearchUpdates();
                if (!eventSignal.WaitOne(1000))
                {
                    Assert.Fail("expected event not fired");
                }
                Assert.AreEqual(AsyncOperation.Searching, result);

                eventSignal.Reset();
                wu.BeginDownloadUpdates();
                if (!eventSignal.WaitOne(1000))
                {
                    Assert.Fail("expected event not fired");
                }
                Assert.AreEqual(AsyncOperation.Downloading, result);

                eventSignal.Reset();
                wu.BeginInstallUpdates();
                if (!eventSignal.WaitOne(1000))
                {
                    Assert.Fail("expected event not fired");
                }
                Assert.AreEqual(AsyncOperation.Installing, result);

            }
        }

        [TestMethod, TestCategory("Settings")]
        public void Should_ReturnDefaults_When_CreateWuApiController()
        {
            var session = new UpdateSessionFake(true);
            using (WuApiController wu = new WuApiController(session, UpdateCollectionFactory, SystemInfo))
            {
                Assert.IsFalse(wu.AutoAcceptEulas);
                Assert.IsTrue(wu.AutoSelectUpdates);
                Assert.IsFalse(wu.GetAvailableUpdates().Any());
            }
        }

        [TestMethod]
        public void Should_ReturnChangedSettings_When_ChangingSettings()
        {
            var session = new UpdateSessionFake(true);
            using (WuApiController wu = new WuApiController(session, UpdateCollectionFactory, SystemInfo))
            {
                var autoAccept = wu.AutoAcceptEulas;
                var autoSelect = wu.AutoSelectUpdates;

                wu.AutoAcceptEulas = !autoAccept;
                wu.AutoSelectUpdates = !autoSelect;

                Assert.AreEqual(!autoAccept, wu.AutoAcceptEulas);
                Assert.AreEqual(!autoSelect, wu.AutoSelectUpdates);
            }
        }

        [TestMethod]
        public void Should_UpdateResetProgressDescription_When_StateChanges()
        {
            UpdateSessionFake session = new UpdateSessionFake(true);
            UpdateFake update = new UpdateFake("update1", true);

            session.SearcherMock.FakeSearchResult = CommonMocks.GetSearchResult(ToUpdateCollection(update));
            session.SearcherMock.FakeSearchTimeMs = 2000;

            using (WuApiController wu = new WuApiController(session, UpdateCollectionFactory, SystemInfo))
            {
                wu.BeginSearchUpdates();
                Assert.IsNotNull(wu.GetWuStatus().Progress);
                wu.AbortSearchUpdates();
                Assert.IsNull(wu.GetWuStatus().Progress);
            }
        }

        [TestMethod, TestCategory("Exception")]
        [ExpectedException(typeof(InvalidStateTransitionException))]
        public void Should_ThrowException_When_RequestedStateChangeIsInvalid()
        {
            UpdateSessionFake session = new UpdateSessionFake(true);

            using (WuApiController wu = new WuApiController(session, UpdateCollectionFactory, SystemInfo))
            {
                wu.AbortDownloadUpdates();
            }
        }

        [TestMethod, TestCategory("Exception")]
        [ExpectedException(typeof(PreConditionNotFulfilledException))]
        public void Should_TestPreConditions_When_BeforeStateChange()
        {
            UpdateSessionFake session = new UpdateSessionFake(true);

            using (WuApiController wu = new WuApiController(session, UpdateCollectionFactory, SystemInfo))
            {
                wu.BeginSearchUpdates();
                WaitForStateChange(wu, WuStateId.SearchCompleted);
                wu.BeginInstallUpdates();
            }
        }

        [TestMethod, TestCategory("Exception"), TestCategory("Timeouts")]
        public void Should_ThrowException_When_TimeoutValueIsToHigh()
        {
            UpdateSessionFake session = new UpdateSessionFake(true);
            session.SearcherMock.FakeSearchResult = CommonMocks.GetSearchResult(ToUpdateCollection(new UpdateFake("update1", true)));

            int max = int.MaxValue / 1000;
            int outOfRange = max + 1;

            using (WuApiController wu = new WuApiController(session, UpdateCollectionFactory, SystemInfo))
            {
                wu.AutoAcceptEulas = true;

                try
                {
                    wu.BeginSearchUpdates(outOfRange);
                    Assert.Fail("exception expected");
                }
                catch (ArgumentOutOfRangeException) { }
                wu.BeginSearchUpdates(max);
                WaitForStateChange(wu, WuStateId.SearchCompleted);

                try
                {
                    wu.BeginDownloadUpdates(outOfRange);
                    Assert.Fail("exception expected");
                }
                catch (ArgumentOutOfRangeException) { }
                wu.BeginDownloadUpdates(max);
                WaitForStateChange(wu, WuStateId.DownloadCompleted);

                try
                {
                    wu.BeginInstallUpdates(outOfRange);
                    Assert.Fail("exception expected");
                }
                catch (ArgumentOutOfRangeException) { }
                wu.BeginInstallUpdates(max);
                WaitForStateChange(wu, WuStateId.InstallCompleted);
            }
        }

        [TestMethod, TestCategory("AcceptEula")]
        public void Should_NotAcceptEula_When_UpdateIsNotSelected()
        {
            UpdateSessionFake session = new UpdateSessionFake(true);

            var update1 = new UpdateFake("update1", false);
            var update2 = new UpdateFake("update2", false);
            session.SearcherMock.FakeSearchResult = CommonMocks.GetSearchResult(ToUpdateCollection(update1, update2));
            update1.EulaAccepted = false;
            update2.EulaAccepted = false;

            using (WuApiController wu = new WuApiController(session, UpdateCollectionFactory, SystemInfo))
            {
                wu.AutoAcceptEulas = true;
                wu.AutoSelectUpdates = false;

                wu.BeginSearchUpdates();
                WaitForStateChange(wu, WuStateId.SearchCompleted);
                wu.SelectUpdate(update2.Identity.UpdateID);
                wu.BeginDownloadUpdates();
                WaitForStateChange(wu, WuStateId.DownloadCompleted);

                var updates = wu.GetAvailableUpdates();

                Assert.IsFalse(updates.Single(u => u.ID.Equals("update1")).SelectedForInstallation);
                Assert.IsFalse(updates.Single(u => u.ID.Equals("update1")).EulaAccepted);
                Assert.IsTrue(updates.Single(u => u.ID.Equals("update2")).EulaAccepted);
                Assert.IsTrue(updates.Single(u => u.ID.Equals("update2")).SelectedForInstallation);
            }
        }


        [TestMethod, TestCategory("AcceptEula")]
        public void Should_AcceptEula_When_EulaIsNotAccepted()
        {
            UpdateSessionFake session = new UpdateSessionFake(true);

            var update1 = new UpdateFake("update1", true);
            var update2 = new UpdateFake("update2", true);
            session.SearcherMock.FakeSearchResult = CommonMocks.GetSearchResult(ToUpdateCollection(update1, update2));

            update1.EulaAccepted = false;
            update1.Identity = CommonMocks.GetUpdateIdentity("update1Id");
            update2.EulaAccepted = false;
            update2.Identity = CommonMocks.GetUpdateIdentity("update2Id");

            using (WuApiController wu = new WuApiController(session, UpdateCollectionFactory, SystemInfo))
            {
                wu.AutoAcceptEulas = false;

                wu.BeginSearchUpdates();
                WaitForStateChange(wu, WuStateId.SearchCompleted);

                var updates = wu.GetAvailableUpdates();

                Assert.IsNotNull(updates.Single(u => u.ID.Equals("update1Id")));
                Assert.IsNotNull(updates.Single(u => u.ID.Equals("update2Id")));

                wu.AcceptEula(updates.Single(u => u.ID.Equals("update1Id")));
                Assert.IsTrue(wu.GetAvailableUpdates().Single(u => u.ID.Equals("update1Id")).EulaAccepted);
                wu.AcceptEula(updates.Single(u => u.ID.Equals("update2Id")).ID);
                Assert.IsTrue(wu.GetAvailableUpdates().Single(u => u.ID.Equals("update2Id")).EulaAccepted);
            }
        }

        [TestMethod, TestCategory("AcceptEula")]
        public void Should_NotThrowException_When_EulaIsAlreadyAccepted()
        {
            UpdateSessionFake session = new UpdateSessionFake(true);

            var update1 = new UpdateFake("update1", true);
            session.SearcherMock.FakeSearchResult = CommonMocks.GetSearchResult(ToUpdateCollection(update1));
            update1.EulaAccepted = true;
            update1.Identity = CommonMocks.GetUpdateIdentity("update1Id");

            using (WuApiController wu = new WuApiController(session, UpdateCollectionFactory, SystemInfo))
            {
                wu.AutoAcceptEulas = false;
                wu.BeginSearchUpdates();
                WaitForStateChange(wu, WuStateId.SearchCompleted);
                wu.AcceptEula("update1Id");
            }
        }

        [TestMethod, TestCategory("AcceptEula"), ExpectedException(typeof(UpdateNotFoundException))]
        public void Should_ThrowException_When_AcceptEulaOfUnkownUpdate()
        {
            UpdateSessionFake session = new UpdateSessionFake(true);

            var update1 = new UpdateFake("update1", true);
            session.SearcherMock.FakeSearchResult = CommonMocks.GetSearchResult(ToUpdateCollection(update1));
            update1.EulaAccepted = true;
            update1.Identity = CommonMocks.GetUpdateIdentity("update1Id");

            using (WuApiController wu = new WuApiController(session, UpdateCollectionFactory, SystemInfo))
            {
                wu.AutoAcceptEulas = false;
                wu.BeginSearchUpdates();
                WaitForStateChange(wu, WuStateId.SearchCompleted);
                wu.AcceptEula("update2Id");
            }
        }

        [TestMethod, TestCategory("AcceptEula"), TestCategory("No Null")]
        public void Should_NotAllowNull_When_RequestForAcceptEula()
        {
            UpdateSessionFake session = new UpdateSessionFake(true);

            using (WuApiController wu = new WuApiController(session, UpdateCollectionFactory, SystemInfo))
            {
                try
                {
                    UpdateDescription ud = null;
                    wu.AcceptEula(ud);
                    Assert.Fail("exception expected");
                }
                catch (ArgumentNullException) { }
                try
                {
                    string id = null;
                    wu.AcceptEula(id);
                    Assert.Fail("exception expected");
                }
                catch (ArgumentNullException) { }
                try
                {
                    string id = "";
                    wu.AcceptEula(id);
                    Assert.Fail("exception expected");
                }
                catch (ArgumentNullException) { }
                try
                {
                    string id = " ";
                    wu.AcceptEula(id);
                    Assert.Fail("exception expected");
                }
                catch (ArgumentNullException) { }
            }
        }

        [TestMethod, TestCategory("Select Updates"), ExpectedException(typeof(UpdateNotFoundException))]
        public void Should_ThrowException_When_SelectUnkownUpdate()
        {
            UpdateSessionFake session = new UpdateSessionFake(true);

            var update1 = new UpdateFake("update1", false);
            session.SearcherMock.FakeSearchResult = CommonMocks.GetSearchResult(ToUpdateCollection(update1));

            using (WuApiController wu = new WuApiController(session, UpdateCollectionFactory, SystemInfo))
            {
                wu.BeginSearchUpdates();
                WaitForStateChange(wu, WuStateId.SearchCompleted);
                wu.SelectUpdate("update2");
            }
        }

        [TestMethod, TestCategory("Select Updates"), ExpectedException(typeof(UpdateNotFoundException))]
        public void Should_ThrowException_When_UnselectUnkownUpdate()
        {
            UpdateSessionFake session = new UpdateSessionFake(true);
            using (WuApiController wu = new WuApiController(session, UpdateCollectionFactory, SystemInfo))
            {
                wu.SelectUpdate("update");
            }
        }

        [TestMethod, TestCategory("Select Updates")]
        public void Should_MarkUpdateAsSelected_When_SelectUpdate()
        {
            UpdateSessionFake session = new UpdateSessionFake(true);

            var update1 = new UpdateFake("update1", false);
            session.SearcherMock.FakeSearchResult = CommonMocks.GetSearchResult(ToUpdateCollection(update1));

            using (WuApiController wu = new WuApiController(session, UpdateCollectionFactory, SystemInfo))
            {
                wu.BeginSearchUpdates();
                WaitForStateChange(wu, WuStateId.SearchCompleted);
                wu.SelectUpdate("update1");
                Assert.IsTrue(wu.GetAvailableUpdates().Single().SelectedForInstallation);
            }
        }

        [TestMethod, TestCategory("Select Updates")]
        public void Should_MarkUpdateAsUnselected_When_UnselectUpdate()
        {
            UpdateSessionFake session = new UpdateSessionFake(true);

            var update1 = new UpdateFake("update1", true);
            session.SearcherMock.FakeSearchResult = CommonMocks.GetSearchResult(ToUpdateCollection(update1));

            using (WuApiController wu = new WuApiController(session, UpdateCollectionFactory, SystemInfo))
            {
                wu.AutoSelectUpdates = true;
                wu.BeginSearchUpdates();
                WaitForStateChange(wu, WuStateId.SearchCompleted);
                Assert.IsTrue(wu.GetAvailableUpdates().Single().SelectedForInstallation);
                wu.UnselectUpdate("update1");
                Assert.IsFalse(wu.GetAvailableUpdates().Single().SelectedForInstallation);
            }
        }
    }
}
