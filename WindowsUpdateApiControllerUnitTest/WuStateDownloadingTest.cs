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
using System.Collections.Generic;
using System.Threading;
using WindowsUpdateApiController.States;
using WUApiLib;
using WuApiMocks;

namespace WindowsUpdateApiControllerUnitTest
{
    [TestClass]
    public class WuStateDownloadingTest
    {
        WuStateDownloading.DownloadCompletedCallback _defaultCompleted = (x, u) => { };
        WuStateAsyncJob.TimeoutCallback _defaultTimeout = (x, y) => { };


        [TestMethod, TestCategory("No Null")]
        public void Should_NotAllowNullValues_When_CreateWuStateDownloading()
        {
            IUpdateDownloader downloader = new UpdateDownloaderFake();
            IUpdateCollection updates = new UpdateCollectionFake();

            try
            {
                new WuStateDownloading(null, updates, _defaultCompleted, _defaultTimeout, null, 100);
                Assert.Fail("exception expected");
            }
            catch (ArgumentNullException) { }
            try
            {
                new WuStateDownloading(downloader, null, _defaultCompleted, _defaultTimeout, null, 100);
                Assert.Fail("exception expected");
            }
            catch (ArgumentNullException) { }
            try
            {
                new WuStateDownloading(downloader, updates, null, _defaultTimeout, null, 100);
                Assert.Fail("exception expected");
            }
            catch (ArgumentNullException) { }
            try
            {
                new WuStateDownloading(downloader, updates, _defaultCompleted, null, null, 100);
                Assert.Fail("exception expected");
            }
            catch (ArgumentNullException) { }
        }

        private UpdateDownloaderFake GetDownloaderWithResultCode(OperationResultCode code)
        {
            UpdateDownloaderFake downloader = new UpdateDownloaderFake();
            downloader.FakeDownloadResult = CommonMocks.GetDownloadResult(code);
            return downloader;
        }

        [TestMethod, TestCategory("Callback")]
        public void Should_CallCompletedCallback_When_DownloadCompletes()
        {
            ManualResetEvent callbackSignal = new ManualResetEvent(false);
            IDownloadResult result = null;
            WuStateDownloading.DownloadCompletedCallback callback = (x, u) => { result = x; callbackSignal.Set(); };
            IUpdateCollection updates = new UpdateCollectionFake();

            Dictionary<OperationResultCode, UpdateDownloaderFake> downloaders = new Dictionary<OperationResultCode, UpdateDownloaderFake>();
            downloaders.Add(OperationResultCode.orcAborted, GetDownloaderWithResultCode(OperationResultCode.orcAborted));
            downloaders.Add(OperationResultCode.orcFailed, GetDownloaderWithResultCode(OperationResultCode.orcFailed));
            downloaders.Add(OperationResultCode.orcSucceeded, GetDownloaderWithResultCode(OperationResultCode.orcSucceeded));
            downloaders.Add(OperationResultCode.orcSucceededWithErrors, GetDownloaderWithResultCode(OperationResultCode.orcSucceededWithErrors));

            foreach (var downloader in downloaders)
            {
                callbackSignal.Reset();
                result = null;
                var downloading = new WuStateDownloading(downloader.Value, updates, callback, _defaultTimeout, null, 100);
                downloading.EnterState(new WuStateReady());

                if (!callbackSignal.WaitOne(1000))
                {
                    Assert.Fail($"callback was not called");
                }
                Assert.AreEqual(result.ResultCode, downloader.Key);
            }
        }
    }
}
