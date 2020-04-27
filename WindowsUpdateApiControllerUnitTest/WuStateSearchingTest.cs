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
using System.Threading;
using WindowsUpdateApiController.States;
using WUApiLib;
using WuApiMocks;

namespace WindowsUpdateApiControllerUnitTest
{
    [TestClass]
    public class WuStateSearchingTest
    {

        [TestMethod, TestCategory("No Null")]
        public void Should_NotAllowNullValues_When_CreateWuStateSearching()
        {
            IUpdateSearcher searcher = new UpdateSearcherFake();

            try
            {
                new WuStateSearching(null, (x)=> { }, (x,y)=> { }, 100);
                Assert.Fail("exception expected");
            }
            catch (ArgumentNullException) { }
            try
            {
                new WuStateSearching(searcher, null, (x, y) => { }, 100);
                Assert.Fail("exception expected");
            }
            catch (ArgumentNullException) { }
        }

        [TestMethod, TestCategory("Callback")]
        public void Should_CallCompletedCallback_When_SearchingCompletes()
        {
            ManualResetEvent callbackSignal = new ManualResetEvent(false);
            ISearchResult result = null;
            WuStateSearching.SearchCompletedCallback callback = (x) => { result = x; callbackSignal.Set(); };
            IUpdateCollection updates = new UpdateCollectionFake();

            UpdateSearcherFake searcher = new UpdateSearcherFake();
            searcher.FakeSearchResult = CommonMocks.GetSearchResult(updates);

            var state = new WuStateSearching(searcher, callback, (x, y) => { }, 100);
            state.EnterState(new WuStateReady());
            if (!callbackSignal.WaitOne(1000))
            {
                Assert.Fail($"callback was not called");
            }
            Assert.AreSame(searcher.FakeSearchResult, result);
        }
    }
}
