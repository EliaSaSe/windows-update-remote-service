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
using System.Collections.Generic;
using System.Linq;
using WindowsUpdateApiController;
using WUApiLib;
using WuApiMocks;
using WuDataContract.Enums;

namespace WindowsUpdateApiControllerUnitTest
{
    [TestClass]
    public class WuApiControllerTest_Search : WuApiControllerTestBase
    {
        [TestMethod, TestCategory("Searching Updates"), TestCategory("Timeouts")]
        public void Should_EnterTimeoutState_When_SearchTimeRunsOut()
        {
            var session = new UpdateSessionFake(true);
            session.SearcherMock.FakeSearchTimeMs = 10000;
            WuApiController Wu = new WuApiController(session, UpdateCollectionFactory, SystemInfo);

            Wu.BeginSearchUpdates(1);

            WaitForStateChange(Wu, WuStateId.SearchFailed);
        }

        [TestMethod, TestCategory("Searching Updates")]
        public void Should_EnterSearchCompletedState_When_SearchCompleted()
        {
            var session = new UpdateSessionFake(true);
            session.SearcherMock.FakeSearchTimeMs = 1;

            WuApiController wu = new WuApiController(session, UpdateCollectionFactory, SystemInfo);
            wu.BeginSearchUpdates();

            WaitForStateChange(wu, WuStateId.SearchCompleted);
        }

        [TestMethod, TestCategory("Searching Updates")]
        public void Should_EnterSearchFailedState_When_SearchCompletedWithErrors()
        {
            var session = new UpdateSessionFake(true);
            session.SearcherMock.FakeSearchTimeMs = 1;

            List<ISearchResult> results = new List<ISearchResult>();
            results.Add(CommonMocks.GetSearchResult(ToUpdateCollection(new UpdateFake("update")), OperationResultCode.orcAborted));
            results.Add(CommonMocks.GetSearchResult(ToUpdateCollection(new UpdateFake("update")), OperationResultCode.orcFailed));
            results.Add(CommonMocks.GetSearchResult(ToUpdateCollection(new UpdateFake("update")), OperationResultCode.orcNotStarted));
            results.Add(CommonMocks.GetSearchResult(ToUpdateCollection(new UpdateFake("update")), OperationResultCode.orcSucceededWithErrors));

            foreach (var result in results)
            {
                session.SearcherMock.FakeSearchResult = result;
                using (WuApiController wu = new WuApiController(session, UpdateCollectionFactory, SystemInfo))
                {
                    wu.BeginSearchUpdates();
                    WaitForStateChange(wu, WuStateId.SearchFailed);
                }
            }
        }

        [TestMethod, TestCategory("Searching Updates")]
        public void Should_EnterSearchFailedState_When_AbortSearch()
        {
            var session = new UpdateSessionFake(true);
            session.SearcherMock.FakeSearchTimeMs = 10000;

            using (WuApiController wu = new WuApiController(session, UpdateCollectionFactory, SystemInfo))
            {
                wu.BeginSearchUpdates();
                Assert.AreEqual(WuStateId.SearchFailed, wu.AbortSearchUpdates());
                Assert.AreEqual(WuStateId.SearchFailed, wu.GetWuStatus().StateId);
            }
        }

        [TestMethod, TestCategory("Searching Updates")]
        public void Should_SkipInstalledUpdates_When_SearchCompleted()
        {
            UpdateFake update = new UpdateFake("update1");
            update.IsInstalled = true;
            IUpdateCollection updateCollection = ToUpdateCollection(update);

            var session = new UpdateSessionFake(true);
            session.SearcherMock.FakeSearchResult = CommonMocks.GetSearchResult(updateCollection);

            using (WuApiController wu = new WuApiController(session, UpdateCollectionFactory, SystemInfo))
            {
                wu.BeginSearchUpdates();
                WaitForStateChange(wu, WuStateId.SearchCompleted);
                Assert.IsTrue(wu.GetAvailableUpdates().Count == 0);
            }
        }

        [TestMethod, TestCategory("Searching Updates")]
        public void Should_UpdateApplicableUpdateList_When_SearchCompleted()
        {
            UpdateSessionFake session = new UpdateSessionFake(true);

            UpdateFake update = new UpdateFake("update1");
            update.IsMandatory = true;
            UpdateFake update2 = new UpdateFake("update2");
            update2.IsMandatory = false;

            session.SearcherMock.FakeSearchResult = CommonMocks.GetSearchResult(ToUpdateCollection(update, update2));

            using (WuApiController wu = new WuApiController(session, UpdateCollectionFactory, SystemInfo))
            {
                wu.AutoSelectUpdates =true;
                wu.BeginSearchUpdates();
                WaitForStateChange(wu, WuStateId.SearchCompleted);
                Assert.IsTrue(wu.GetAvailableUpdates().Count == 2);
                Assert.IsTrue(wu.GetAvailableUpdates().First().ID == "update1");
                Assert.IsTrue(wu.GetAvailableUpdates().Skip(1).First().ID == "update2");              
            }
        }

        [TestMethod, TestCategory("Searching Updates")]
        public void Should_UpdateSetProgressDescription_When_BeginSearch()
        {
            UpdateSessionFake session = new UpdateSessionFake(true);
            UpdateFake update = new UpdateFake("update1", true);

            session.SearcherMock.FakeSearchResult = CommonMocks.GetSearchResult(ToUpdateCollection(update));
            session.SearcherMock.FakeSearchTimeMs = 10000;

            using (WuApiController wu = new WuApiController(session, UpdateCollectionFactory, SystemInfo))
            {
                wu.BeginSearchUpdates();
                Assert.IsNotNull(wu.GetWuStatus().Progress);
                Assert.IsNull(wu.GetWuStatus().Progress.Count);
                wu.AbortSearchUpdates();
            }
        }
    }
}
