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
using WindowsUpdateApiController.Helper;
using WUApiLib;
using WuApiMocks;
using WuDataContract.Enums;

namespace WindowsUpdateApiControllerUnitTest
{
    [TestClass]
    public class WuApiControllerTest_Download : WuApiControllerTestBase
    {
        readonly MockRepository MoqFactory = new MockRepository(MockBehavior.Strict) { };

        [TestMethod, TestCategory("Download updates")]
        public void Should_EnterDownloadCompletedState_When_DownloadCompleted()
        {
            var session = new UpdateSessionFake(true);
            session.SearcherMock.FakeSearchResult = CommonMocks.GetSearchResult(ToUpdateCollection(new UpdateFake("update1", true)));

            using (WuApiController wu = new WuApiController(session, UpdateCollectionFactory, SystemInfo))
            {
                wu.AutoAcceptEulas = true;
                wu.BeginSearchUpdates();
                WaitForStateChange(wu, WuStateId.SearchCompleted);
                wu.BeginDownloadUpdates();
                WaitForStateChange(wu, WuStateId.DownloadCompleted);
            }
        }

        [TestMethod, TestCategory("Download updates")]
        public void Should_EnterDownloadFailedState_When_DownloadFailed()
        {
            var session = new UpdateSessionFake(true);
            session.SearcherMock.FakeSearchResult = CommonMocks.GetSearchResult(ToUpdateCollection(new UpdateFake("update1", true)));

            List<IDownloadResult> results = new List<IDownloadResult>();
            results.Add(CommonMocks.GetDownloadResult(OperationResultCode.orcFailed));
            results.Add(CommonMocks.GetDownloadResult(OperationResultCode.orcAborted));
            results.Add(CommonMocks.GetDownloadResult(OperationResultCode.orcNotStarted));

            foreach (var result in results)
            {
                session.DownloaderMock.FakeDownloadResult = result;
                using (WuApiController wu = new WuApiController(session, UpdateCollectionFactory, SystemInfo))
                {
                    wu.AutoAcceptEulas = true;
                    wu.BeginSearchUpdates();
                    WaitForStateChange(wu, WuStateId.SearchCompleted);
                    wu.BeginDownloadUpdates();
                    WaitForStateChange(wu, WuStateId.DownloadFailed);
                }
            }
        }

        [TestMethod, TestCategory("Download updates")]
        public void Should_EnterDownloadPartiallyFailedState_When_DownloadPartiallyFailed()
        {
            var session = new UpdateSessionFake(true);
            session.SearcherMock.FakeSearchResult = CommonMocks.GetSearchResult(ToUpdateCollection(new UpdateFake("update1", true)));
            session.DownloaderMock.FakeDownloadResult = CommonMocks.GetDownloadResult(OperationResultCode.orcSucceededWithErrors);

            using (WuApiController wu = new WuApiController(session, UpdateCollectionFactory, SystemInfo))
            {
                wu.AutoAcceptEulas = true;
                wu.BeginSearchUpdates();
                WaitForStateChange(wu, WuStateId.SearchCompleted);
                wu.BeginDownloadUpdates();
                WaitForStateChange(wu, WuStateId.DownloadPartiallyFailed);
            }
        }

        [TestMethod, TestCategory("Download updates")]
        public void Should_EnterDownloadFailedState_When_AbortDownload()
        {
            var session = new UpdateSessionFake(true);
            session.SearcherMock.FakeSearchResult = CommonMocks.GetSearchResult(ToUpdateCollection(new UpdateFake("update1", true)));
            session.DownloaderMock.FakeDownloadTimeMs = 10000;

            using (WuApiController wu = new WuApiController(session, UpdateCollectionFactory, SystemInfo))
            {
                wu.AutoAcceptEulas = true;
                wu.BeginSearchUpdates();
                WaitForStateChange(wu, WuStateId.SearchCompleted);
                wu.BeginDownloadUpdates();
                Assert.AreEqual(WuStateId.DownloadFailed, wu.AbortDownloadUpdates());
                Assert.AreEqual(WuStateId.DownloadFailed, wu.GetWuStatus().StateId);
            }
        }

        [TestMethod, TestCategory("Download updates")]
        public void Should_NotDownloadOptionalUpdate_When_AutoSelectEnabled()
        {
            var session = new UpdateSessionFake(true);
            var update1 = new UpdateFake("update1", true);
            var update2 = new UpdateFake("update2", false);
            session.SearcherMock.FakeSearchResult = CommonMocks.GetSearchResult(ToUpdateCollection(update1, update2));

            using (WuApiController wu = new WuApiController(session, UpdateCollectionFactory, SystemInfo))
            {
                wu.AutoSelectUpdates = true;
                wu.AutoAcceptEulas = true;
                wu.BeginSearchUpdates();
                WaitForStateChange(wu, WuStateId.SearchCompleted);
                wu.BeginDownloadUpdates();
                WaitForStateChange(wu, WuStateId.DownloadCompleted);
                Assert.AreEqual(wu.GetAvailableUpdates().Count, 2);
                Assert.IsNotNull(wu.GetAvailableUpdates().SingleOrDefault(u => u.ID.Equals("update1") && u.IsDownloaded));
                Assert.IsNotNull(wu.GetAvailableUpdates().SingleOrDefault(u => u.ID.Equals("update2") && !u.IsDownloaded));
            }
        }

        [TestMethod, TestCategory("AcceptEula"), TestCategory("Download updates")]
        public void Should_AcceptEulas_When_BeginDownloadingAndAutoAcceptEnabled()
        {
            var session = new UpdateSessionFake(true);

            var update1 = new UpdateFake("update1", true);
            update1.EulaAccepted = false;
            var update2 = new UpdateFake("update2", true);
            update2.EulaAccepted = true;
            session.SearcherMock.FakeSearchResult = CommonMocks.GetSearchResult(ToUpdateCollection(update1, update2));

            using (WuApiController wu = new WuApiController(session, UpdateCollectionFactory, SystemInfo))
            {
                wu.AutoAcceptEulas =true;
                wu.BeginSearchUpdates();
                WaitForStateChange(wu, WuStateId.SearchCompleted);
                wu.BeginDownloadUpdates();
                WaitForStateChange(wu, WuStateId.DownloadCompleted);
                Assert.IsTrue(update1.EulaAccepted);
                Assert.IsTrue(update2.EulaAccepted);
                Assert.AreEqual(wu.GetAvailableUpdates().Count, 2);
                Assert.IsTrue(wu.GetAvailableUpdates().All(u => u.IsDownloaded && u.EulaAccepted));
            }
        }

        [TestMethod, TestCategory("AcceptEula"), TestCategory("Download updates")]
        public void Should_NotDownloadUpdate_When_EulaIsNotAccepted()
        {
            var session = new UpdateSessionFake(true);

            var update1 = new UpdateFake("update1", true);
            update1.EulaAccepted = true;
            var update2 = new UpdateFake("update2", true);
            update2.EulaAccepted = false;
            session.SearcherMock.FakeSearchResult = CommonMocks.GetSearchResult(ToUpdateCollection(update1, update2));

            using (WuApiController wu = new WuApiController(session, UpdateCollectionFactory, SystemInfo))
            {
                wu.AutoAcceptEulas =false;
                wu.BeginSearchUpdates();
                WaitForStateChange(wu, WuStateId.SearchCompleted);
                wu.BeginDownloadUpdates();
                WaitForStateChange(wu, WuStateId.DownloadCompleted);
                Assert.IsTrue(update1.EulaAccepted);
                Assert.IsFalse(update2.EulaAccepted);
                Assert.AreEqual(wu.GetAvailableUpdates().Count, 2);
                Assert.IsNotNull(wu.GetAvailableUpdates().SingleOrDefault(u => u.ID.Equals("update1") && u.IsDownloaded && u.EulaAccepted));
                Assert.IsNotNull(wu.GetAvailableUpdates().SingleOrDefault(u => u.ID.Equals("update2") && !u.IsDownloaded && !u.EulaAccepted));
            }
        }

        [TestMethod, TestCategory("Download updates")]
        public void Should_NotDownloadInstalledUpdates_When_DownloadUpdates()
        {
            var session = new UpdateSessionFake(true);

            var update1 = new UpdateFake("update1", true);
            update1.IsInstalled = false;
            update1.IsDownloaded = false;
            var update2 = new UpdateFake("update2", true);
            update2.IsInstalled = false;
            update2.IsDownloaded = false;
            session.SearcherMock.FakeSearchResult = CommonMocks.GetSearchResult(ToUpdateCollection(update1, update2));

            using (WuApiController wu = new WuApiController(session, UpdateCollectionFactory, SystemInfo))
            {
                wu.AutoAcceptEulas = true;
                wu.BeginSearchUpdates();
                WaitForStateChange(wu, WuStateId.SearchCompleted);
                update1.IsInstalled = true;
                wu.BeginDownloadUpdates();
                WaitForStateChange(wu, WuStateId.DownloadCompleted);
                Assert.AreEqual(wu.GetAvailableUpdates().Count, 2);
                Assert.IsNotNull(wu.GetAvailableUpdates().SingleOrDefault(u => u.ID.Equals("update1") && !u.IsDownloaded && u.IsInstalled));
                Assert.IsNotNull(wu.GetAvailableUpdates().SingleOrDefault(u => u.ID.Equals("update2") && u.IsDownloaded && !u.IsInstalled));
            }
        }

        [TestMethod, TestCategory("Download updates")]
        public void Should_NotEnterDonwloadingState_When_NoUpdatesAvailable()
        {
            var session = new UpdateSessionFake(true);

            var update1 = new UpdateFake("update1", true);
            update1.IsInstalled = true;
            update1.IsDownloaded = false;
            var update2 = new UpdateFake("update2", true);
            update2.IsDownloaded = true;
            

            using (WuApiController wu = new WuApiController(session, UpdateCollectionFactory, SystemInfo))
            {
                try
                {
                    wu.BeginDownloadUpdates(); // nerver searched for updates, no updates should be available
                    Assert.Fail("exception expected");
                }
                catch (InvalidStateTransitionException) { }

                session.SearcherMock.FakeSearchResult = CommonMocks.GetSearchResult(ToUpdateCollection(update1, update2));
                wu.BeginSearchUpdates();
                WaitForStateChange(wu, WuStateId.SearchCompleted);

                try
                {
                    wu.BeginDownloadUpdates(); // available updates are already installed or downloaded
                    Assert.Fail("exception expected");
                }
                catch (InvalidStateTransitionException) { }
            }
        }

        [TestMethod, TestCategory("Download updates")]
        public void Should_EnterDownloadFailedState_When_DownloadTimeRunsOut()
        {
            UpdateFake update = new UpdateFake("update1", true);
            IUpdateCollection updateCollection = ToUpdateCollection(update);

            var session = new UpdateSessionFake(true);
            session.SearcherMock.FakeSearchResult = CommonMocks.GetSearchResult(updateCollection);
            session.DownloaderMock.FakeDownloadTimeMs = 10000;

            using (WuApiController wu = new WuApiController(session, UpdateCollectionFactory, SystemInfo))
            {
                wu.AutoAcceptEulas = true;
                wu.BeginSearchUpdates();
                WaitForStateChange(wu, WuStateId.SearchCompleted);
                wu.BeginDownloadUpdates(1);
                WaitForStateChange(wu, WuStateId.DownloadFailed, 2000);
                Assert.IsTrue(wu.GetWuStatus().Description.Contains("Timeout"));
            }
        }

        [TestMethod, TestCategory("Download updates")]
        public void Should_NotEnterDownloadingState_When_NotEnoughFreeSpaceAvailable()
        {
            var system = MoqFactory.Create<ISystemInfo>(MockBehavior.Loose);
            int freespace = 100;
            system.Setup(s => s.GetFreeSpace()).Returns(freespace);
            system.Setup(s => s.GetFQDN()).Returns("fqdn");
            system.Setup(s => s.GetOperatingSystemName()).Returns("osname");
            system.Setup(s => s.GetWuServer()).Returns("update server");
            system.Setup(s => s.GetTargetGroup()).Returns("target group");

            UpdateFake update = new UpdateFake("update1");
            update.IsMandatory = true;
            update.EulaAccepted = true;
            update.MaxDownloadSize = freespace;
            UpdateFake update2 = new UpdateFake("update2");
            update2.IsMandatory = true;
            update2.EulaAccepted = true;
            update2.RecommendedHardDiskSpace = 10;

            IUpdateCollection updateCollection = ToUpdateCollection(update, update2);

            var session = new UpdateSessionFake(true);
            session.SearcherMock.FakeSearchResult = CommonMocks.GetSearchResult(updateCollection);

            WuApiController wu = new WuApiController(session, UpdateCollectionFactory, system.Object);

            wu.BeginSearchUpdates();
            WaitForStateChange(wu, WuStateId.SearchCompleted);

            try
            {
                wu.BeginDownloadUpdates();
                Assert.Fail("exception expected");
            }
            catch (InvalidStateTransitionException e)
            {
                Assert.IsTrue(e.Message.Contains("free space"));
                Assert.IsTrue(wu.GetWuStatus().Equals(WuStateId.SearchCompleted));
            }
        }

    }
}
