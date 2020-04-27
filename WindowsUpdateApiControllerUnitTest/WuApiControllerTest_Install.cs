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
using System.Collections.Generic;
using System.Linq;
using WindowsUpdateApiController;
using WindowsUpdateApiController.Exceptions;
using WUApiLib;
using WuApiMocks;
using WuDataContract.Enums;

namespace WindowsUpdateApiControllerUnitTest
{
    [TestClass]
    public class WuApiControllerTest_Install : WuApiControllerTestBase
    {
        [TestMethod]
        public void Should_EnterInstallCompletedState_When_InstallCompleted()
        {
            var session = new UpdateSessionFake(true);
            var update = new UpdateFake("update1", true);
            update.IsDownloaded = true;
            session.SearcherMock.FakeSearchResult = CommonMocks.GetSearchResult(ToUpdateCollection(update));

            using (WuApiController wuau = new WuApiController(session, UpdateCollectionFactory, SystemInfo))
            {
                wuau.AutoAcceptEulas = true;
                wuau.BeginSearchUpdates();
                WaitForStateChange(wuau, WuStateId.SearchCompleted);
                wuau.BeginInstallUpdates();
                WaitForStateChange(wuau, WuStateId.InstallCompleted);
            }
        }

        [TestMethod]
        public void Should_EnterInstallFailedState_When_AbortInstall()
        {
            var session = new UpdateSessionFake(true);
            var update = new UpdateFake("update1", true);
            update.IsDownloaded = true;
            session.SearcherMock.FakeSearchResult = CommonMocks.GetSearchResult(ToUpdateCollection(update));
            session.InstallerMock.FakeInstallTimeMs = 10000;

            using (WuApiController wu = new WuApiController(session, UpdateCollectionFactory, SystemInfo))
            {
                wu.AutoAcceptEulas = true;
                wu.BeginSearchUpdates();
                WaitForStateChange(wu, WuStateId.SearchCompleted);
                wu.BeginInstallUpdates();
                Assert.AreEqual(WuStateId.InstallFailed, wu.AbortInstallUpdates());
                Assert.AreEqual(WuStateId.InstallFailed, wu.GetWuStatus().StateId);
            }
        }

        [TestMethod]
        public void Should_EnterInstallFailedState_When_InstallationFailed()
        {
            var session = new UpdateSessionFake(true);
            var update = new UpdateFake("update1", true);
            update.IsDownloaded = true;
            session.SearcherMock.FakeSearchResult = CommonMocks.GetSearchResult(ToUpdateCollection(update));

            List<IInstallationResult> results = new List<IInstallationResult>();
            results.Add(CommonMocks.GetInstallationResult(OperationResultCode.orcFailed));
            results.Add(CommonMocks.GetInstallationResult(OperationResultCode.orcAborted));
            results.Add(CommonMocks.GetInstallationResult(OperationResultCode.orcNotStarted));

            foreach (var result in results)
            {
                session.InstallerMock.FakeInstallResult = result;
                using (WuApiController wu = new WuApiController(session, UpdateCollectionFactory, SystemInfo))
                {
                    wu.AutoAcceptEulas = true;
                    wu.BeginSearchUpdates();
                    WaitForStateChange(wu, WuStateId.SearchCompleted);
                    wu.BeginInstallUpdates();
                    WaitForStateChange(wu, WuStateId.InstallFailed);
                }
            }
        }

        [TestMethod]
        public void Should_EnterInstallFailedState_When_DownloadTimeRunsOut()
        {
            UpdateFake update = new UpdateFake("update1", true);
            update.IsDownloaded = true;
            var session = new UpdateSessionFake(true);
            session.SearcherMock.FakeSearchResult = CommonMocks.GetSearchResult(ToUpdateCollection(update));
            session.InstallerMock.FakeInstallTimeMs = 10000;

            using (WuApiController wu = new WuApiController(session, UpdateCollectionFactory, SystemInfo))
            {
                wu.AutoAcceptEulas = true;
                wu.BeginSearchUpdates();
                WaitForStateChange(wu, WuStateId.SearchCompleted);            
                wu.BeginInstallUpdates(1);
                WaitForStateChange(wu, WuStateId.InstallFailed, 2000);
                Assert.IsTrue(wu.GetWuStatus().Description.Contains("Timeout"));
            }
        }

        [TestMethod]
        public void Should_EnterInstallPartiallyFailedState_When_InstallPartiallyFailed()
        {
            var session = new UpdateSessionFake(true);
            UpdateFake update = new UpdateFake("update1", true);
            update.IsDownloaded = true;

            session.SearcherMock.FakeSearchResult = CommonMocks.GetSearchResult(ToUpdateCollection(update));
            session.InstallerMock.FakeInstallResult = CommonMocks.GetInstallationResult(OperationResultCode.orcSucceededWithErrors);

            using (WuApiController wu = new WuApiController(session, UpdateCollectionFactory, SystemInfo))
            {
                wu.AutoAcceptEulas = true;
                wu.BeginSearchUpdates();
                WaitForStateChange(wu, WuStateId.SearchCompleted);

                wu.BeginInstallUpdates();
                WaitForStateChange(wu, WuStateId.InstallPartiallyFailed);
            }
        }

        [TestMethod, TestCategory("Install updates")]
        [ExpectedException(typeof(PreConditionNotFulfilledException))]
        public void Should_NotInstallAlreadyInstalledUpdates_When_BeginInstallUpdates()
        {
            var session = new UpdateSessionFake(true);
            var update1 = new UpdateFake("update1", true);
            update1.IsInstalled = true;
            session.SearcherMock.FakeSearchResult = CommonMocks.GetSearchResult(ToUpdateCollection(update1));

            using (WuApiController wu = new WuApiController(session, UpdateCollectionFactory, SystemInfo))
            {
                wu.BeginSearchUpdates();
                WaitForStateChange(wu, WuStateId.SearchCompleted);
                wu.BeginInstallUpdates();
            }
        }

        [TestMethod, TestCategory("Install updates"), TestCategory("Auto select updates")]
        public void Should_NotInstallOptionalUpdate_When_AutoSelectEnabled()
        {
            var session = new UpdateSessionFake(true);
            var update1 = new UpdateFake("update1", true);
            update1.IsDownloaded = true;
            var update2 = new UpdateFake("update2", false);
            update2.IsDownloaded = true;
            session.SearcherMock.FakeSearchResult = CommonMocks.GetSearchResult(ToUpdateCollection(update1, update2));

            using (WuApiController wu = new WuApiController(session, UpdateCollectionFactory, SystemInfo))
            {
                wu.AutoSelectUpdates = true;
                wu.AutoAcceptEulas =true;
                wu.BeginSearchUpdates();
                WaitForStateChange(wu, WuStateId.SearchCompleted);
                wu.BeginInstallUpdates();
                WaitForStateChange(wu, WuStateId.InstallCompleted);
                Assert.AreEqual(wu.GetAvailableUpdates().Count, 2);
                Assert.IsNotNull(wu.GetAvailableUpdates().SingleOrDefault(u => u.ID.Equals("update1") && u.IsInstalled));
                Assert.IsNotNull(wu.GetAvailableUpdates().SingleOrDefault(u => u.ID.Equals("update2") && !u.IsInstalled));
            }
        }

        [TestMethod, TestCategory("AcceptEula"), TestCategory("Install updates")]
        public void Should_AcceptEulas_When_BeginInstallingAndAutoAcceptEnabled()
        {
            var session = new UpdateSessionFake(true);
            var update = new UpdateFake("update1", true);
            update.IsDownloaded = true;
            session.SearcherMock.FakeSearchResult = CommonMocks.GetSearchResult(ToUpdateCollection(update));

            using (WuApiController wu = new WuApiController(session, UpdateCollectionFactory, SystemInfo))
            {
                wu.AutoAcceptEulas =true;
                wu.BeginSearchUpdates();
                WaitForStateChange(wu, WuStateId.SearchCompleted);
                update.EulaAccepted = false;
                wu.BeginInstallUpdates();
                WaitForStateChange(wu, WuStateId.InstallCompleted);
                Assert.AreEqual(wu.GetAvailableUpdates().Count, 1);
                Assert.IsTrue(wu.GetAvailableUpdates().All(u => u.IsDownloaded && u.IsInstalled && u.EulaAccepted));
            }
        }

        [TestMethod, TestCategory("AcceptEula"), TestCategory("Install updates")]
        public void Should_NotInstallUpdate_When_EulaIsNotAccepted()
        {
            var session = new UpdateSessionFake(true);

            var update1 = new UpdateFake("update1", true);
            update1.IsDownloaded = true;
            var update2 = new UpdateFake("update2", true);
            update2.IsDownloaded = true;
            session.SearcherMock.FakeSearchResult = CommonMocks.GetSearchResult(ToUpdateCollection(update1, update2));

            using (WuApiController wu = new WuApiController(session, UpdateCollectionFactory, SystemInfo))
            {
                wu.AutoAcceptEulas =true;
                wu.BeginSearchUpdates();
                WaitForStateChange(wu, WuStateId.SearchCompleted);
                update1.EulaAccepted = true;
                update2.EulaAccepted = false;
                wu.AutoAcceptEulas =false;
                wu.BeginInstallUpdates();
                WaitForStateChange(wu, WuStateId.UserInputRequired);

                Assert.IsTrue(update1.EulaAccepted);
                Assert.IsFalse(update2.EulaAccepted);
                Assert.AreEqual(wu.GetAvailableUpdates().Count, 2);
                Assert.IsNotNull(wu.GetAvailableUpdates().SingleOrDefault(u => u.ID.Equals("update1") && u.IsInstalled && u.EulaAccepted));
                Assert.IsNotNull(wu.GetAvailableUpdates().SingleOrDefault(u => u.ID.Equals("update2") && !u.IsInstalled && !u.EulaAccepted));
            }
        }

        [TestMethod]
        public void Should_NotEnterInstallingState_When_NoUpdatesAvailable()
        {
            var session = new UpdateSessionFake(true);
            var update1 = new UpdateFake("update1", true);
            update1.IsInstalled = true;

            var update2 = new UpdateFake("update2", true);
            update2.IsInstalled = false;
            update2.IsDownloaded = false;

            using (WuApiController wu = new WuApiController(session, UpdateCollectionFactory, SystemInfo))
            {
                try
                {
                    wu.BeginInstallUpdates(); // nerver searched for updates, no updates should be available
                    Assert.Fail("exception expected");
                }
                catch (InvalidStateTransitionException) { }

                session.SearcherMock.FakeSearchResult = CommonMocks.GetSearchResult(ToUpdateCollection(update1));
                wu.BeginSearchUpdates();
                WaitForStateChange(wu, WuStateId.SearchCompleted);

                try
                {
                    wu.BeginInstallUpdates(); // available updates are already installed
                    Assert.Fail("exception expected");
                }
                catch (InvalidStateTransitionException) { }

                session.SearcherMock.FakeSearchResult = CommonMocks.GetSearchResult(ToUpdateCollection(update2));
                wu.BeginSearchUpdates();
                WaitForStateChange(wu, WuStateId.SearchCompleted);

                try
                {
                    wu.BeginInstallUpdates(); // available updates are already installed
                    Assert.Fail("exception expected");
                }
                catch (InvalidStateTransitionException) { }
            }
        }

        [TestMethod]
        public void Should_EnterRebootRequiredState_When_UpdateInstallationRequiresReboot()
        {
            var session = new UpdateSessionFake(true);
            var update = new UpdateFake("update1", true);
            update.IsDownloaded = true;

            session.SearcherMock.FakeSearchResult = CommonMocks.GetSearchResult(ToUpdateCollection(update));
            session.InstallerMock.FakeInstallResult = CommonMocks.GetInstallationResult(OperationResultCode.orcSucceeded, 0, true);

            using (WuApiController wu = new WuApiController(session, UpdateCollectionFactory, SystemInfo))
            {
                wu.AutoAcceptEulas = true;
                wu.BeginSearchUpdates();
                WaitForStateChange(wu, WuStateId.SearchCompleted);
                wu.BeginInstallUpdates();
                WaitForStateChange(wu, WuStateId.RebootRequired);
            }
        }

        [TestMethod]
        public void Should_EnterUserInputRequiredState_When_NotInstalledUpdateCanRequestInput()
        {
            var session = new UpdateSessionFake(true);
            var behavMock = new Mock<IInstallationBehavior>();
            behavMock.Setup(b => b.CanRequestUserInput).Returns(true);
            var update = new UpdateFake("update1", true);
            update.IsDownloaded = true;
            update.InstallationBehavior = behavMock.Object;
            session.SearcherMock.FakeSearchResult = CommonMocks.GetSearchResult(ToUpdateCollection(update));

            using (WuApiController wu = new WuApiController(session, UpdateCollectionFactory, SystemInfo))
            {
                wu.AutoAcceptEulas = true;
                wu.BeginSearchUpdates();
                WaitForStateChange(wu, WuStateId.SearchCompleted);
                wu.BeginInstallUpdates();
                WaitForStateChange(wu, WuStateId.UserInputRequired);
            }
        }

        [TestMethod]
        [ExpectedException(typeof(PreConditionNotFulfilledException))]
        public void Should_NotEnterDownloadingState_When_InstallerIsNotReady()
        {
            var session = new UpdateSessionFake(true);
            session.SearcherMock.FakeSearchResult = CommonMocks.GetSearchResult(ToUpdateCollection(new UpdateFake("update1", true)));
            session.InstallerMock.IsBusy = true;

            using (WuApiController wu = new WuApiController(session, UpdateCollectionFactory, SystemInfo))
            {
                wu.BeginSearchUpdates();
                WaitForStateChange(wu, WuStateId.SearchCompleted);
                wu.BeginInstallUpdates();
            }
        }

        [TestMethod]
        [ExpectedException(typeof(PreConditionNotFulfilledException))]
        public void Should_NotEnterDownloadingState_When_InstallerRequiresReboot()
        {
            var session = new UpdateSessionFake(true);
            session.SearcherMock.FakeSearchResult = CommonMocks.GetSearchResult(ToUpdateCollection(new UpdateFake("update1", true)));
            session.InstallerMock.RebootRequiredBeforeInstallation = true;

            using (WuApiController wu = new WuApiController(session, UpdateCollectionFactory, SystemInfo))
            {
                wu.BeginSearchUpdates();
                WaitForStateChange(wu, WuStateId.SearchCompleted);
                wu.BeginInstallUpdates();
            }
        }

        [TestMethod]
        public void Should_ReturnExpectedInstallerStatus_When_RequestInstallerStatus()
        {
            UpdateSessionFake session = new UpdateSessionFake(true);
            using (WuApiController wu = new WuApiController(session, UpdateCollectionFactory, SystemInfo))
            {
                session.InstallerMock.IsBusy = true;
                Assert.AreEqual(wu.GetInstallerStatus(), InstallerStatus.Busy);
                session.InstallerMock.IsBusy = false;
            }
            using (WuApiController wu = new WuApiController(session, UpdateCollectionFactory, SystemInfo))
            {
                session.InstallerMock.RebootRequiredBeforeInstallation = true;
                Assert.AreEqual(wu.GetInstallerStatus(), InstallerStatus.RebootRequiredBeforeInstallation);
                session.InstallerMock.RebootRequiredBeforeInstallation = false;
            }
            using (WuApiController wu = new WuApiController(session, UpdateCollectionFactory, SystemInfo))
            {
                Assert.AreEqual(wu.GetInstallerStatus(), InstallerStatus.Ready);
            }
        }
    }


}
