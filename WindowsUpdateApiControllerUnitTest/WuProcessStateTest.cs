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
using WindowsUpdateApiController.States;
using WindowsUpdateApiControllerUnitTest.Mocks;
using WuApiMocks;
using WuDataContract.Enums;

namespace WindowsUpdateApiControllerUnitTest
{
    [TestClass]
    public class WuProcessStateTest
    {
        [TestMethod]
        public void Should_ContainSpecifiedObjects_When_CreateWuProcessState()
        {
            string displayName = "a display name";
            WuStateId id = WuStateId.UserInputRequired;
            WuProcessState state = new WuProcessStateMock(id, displayName);

            Assert.AreEqual(displayName, state.DisplayName);
            Assert.AreEqual(id, state.StateId);
        }

        [TestMethod]
        public void Should_ContainSpecifiedStateDesc_When_RequestStateDesc()
        {
            WuProcessStateMock state = new WuProcessStateMock(WuStateId.UserInputRequired, "a display name");

            string stateDesc = "a state desc";
            state.SetStateDesc(stateDesc);           
            Assert.AreEqual(state.StateDesc, stateDesc);

            state.SetStateDesc(null);
            Assert.AreEqual(state.StateDesc, String.Empty);

        }

        [TestMethod, TestCategory("No Null")]
        public void Should_NotAllowEmptyDisplayNames_When_CreateWuProcessState()
        {
            string displayName1 = String.Empty;
            string displayName2 = null;
            WuStateId id = WuStateId.UserInputRequired;

            try
            {
                WuProcessState state = new WuProcessStateMock(id, displayName1);
                Assert.Fail("exception expected");
            }
            catch (ArgumentException) { }

            try
            {
                WuProcessState state = new WuProcessStateMock(id, displayName2);
                Assert.Fail("exception expected");
            }
            catch (ArgumentException) { }
        }

        [TestMethod]
        public void Should_SetCorrectStateId_When_CreateWuProcessStateObject()
        {

            var searching = new WuStateSearching(new UpdateSearcherFake(), (x) => { }, (x,y) => { }, 100);
            var downloading = new WuStateDownloading(new UpdateDownloaderFake(), new UpdateCollectionFake(), (x, u) => { }, (x,y) => { }, null, 100);
            var installing = new WuStateInstalling(new UpdateInstallerFake(), new UpdateCollectionFake(), (x, u) => { }, (x,y) => { }, null, 100);

            Assert.AreEqual(WuStateId.Searching, searching.StateId);
            Assert.AreEqual(WuStateId.Downloading, downloading.StateId);
            Assert.AreEqual(WuStateId.Installing, installing.StateId);

            searching.Dispose();
            downloading.Dispose();
            installing.Dispose();

            var sfailed = new WuStateSearchFailed(null);
            var dfailed = new WuStateDownloadFailed(null);
            var dpfailed = new WuStateDownloadPartiallyFailed(null);
            var ifailed = new WuStateInstallFailed(null);
            var ipfailed = new WuStateInstallPartiallyFailed(null);

            Assert.AreEqual(WuStateId.SearchFailed, sfailed.StateId);
            Assert.AreEqual(WuStateId.DownloadFailed, dfailed.StateId);
            Assert.AreEqual(WuStateId.DownloadPartiallyFailed, dpfailed.StateId);
            Assert.AreEqual(WuStateId.InstallFailed, ifailed.StateId);
            Assert.AreEqual(WuStateId.InstallPartiallyFailed, ipfailed.StateId);

            var scom = new WuStateSearchCompleted(new UpdateCollectionFake());
            var dcom = new WuStateDownloadCompleted(new UpdateCollectionFake(), 0);
            var icom = new WuStateInstallCompleted(new UpdateCollectionFake(), 0);

            Assert.AreEqual(WuStateId.SearchCompleted, scom.StateId);
            Assert.AreEqual(WuStateId.DownloadCompleted, dcom.StateId);
            Assert.AreEqual(WuStateId.InstallCompleted, icom.StateId);

            var ready = new WuStateReady();
            var rebootreq = new WuStateRebootRequired();
            var reboot = new WuStateRestartSentToOS();
            var userinput = new WuStateUserInputRequired(String.Empty);


            Assert.AreEqual(WuStateId.Ready, ready.StateId);
            Assert.AreEqual(WuStateId.RebootRequired, rebootreq.StateId);
            Assert.AreEqual(WuStateId.RestartSentToOS, reboot.StateId);
            Assert.AreEqual(WuStateId.UserInputRequired, userinput.StateId);
        }

        [TestMethod]
        public void Should_ContainSpecifiedObjects_When_CreateStateChangeEvalResult()
        {
            bool success1 = true;
            string message1 = "a message";
            bool success2 = false;
            string message2 = null;

            var result1 = new ConditionEvalResult(success1, message1);
            var result2 = new ConditionEvalResult(success2, message2);

            Assert.AreEqual(result1.IsFulfilled, success1);
            Assert.AreEqual(result1.Message, message1);
            Assert.AreEqual(result2.IsFulfilled, success2);
            Assert.AreEqual(result2.Message, message2);
        }

        [TestMethod]
        public void Should_ReturnValidStateChangeEvalResult_When_CallStaticValidStateChange()
        {
            var result1 = ConditionEvalResult.ValidStateChange;
            var result2 = ConditionEvalResult.ValidStateChange;
            Assert.AreEqual(result1.IsFulfilled, true);
            Assert.AreEqual(result1.Message, null);
            Assert.AreEqual(result2.IsFulfilled, true);
            Assert.AreEqual(result2.Message, null);
            Assert.AreNotSame(result1, result2);
        }

        [TestMethod]
        public void Should_ContainSpecifiedValues_When_CreateWuStateUserInputRequired()
        {
            string reason = "reason";
            WuStateUserInputRequired state = new WuStateUserInputRequired(reason);
            Assert.AreEqual(reason, state.Reason);
            Assert.AreEqual(reason, state.StateDesc);
        }
    }
}
