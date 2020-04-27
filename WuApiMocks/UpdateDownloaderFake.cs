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
using Moq;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WUApiLib;

namespace WuApiMocks
{
    public class UpdateDownloaderFake : IUpdateDownloader, UpdateDownloader
    {
        public UpdateDownloaderFake()
        {
            FakeDownloadTimeMs = 0;
        }

        public int FakeDownloadTimeMs { get; set; }
        public IDownloadResult FakeDownloadResult { get; set; }

        public string ClientApplicationID { get; set; }

        public bool IsForced { get; set; }

        public DownloadPriority Priority
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        public UpdateCollection Updates { get; set; }

        public IDownloadJob BeginDownload(object onProgressChanged, object onCompleted, object state)
        {
            CancellationTokenSource source = new CancellationTokenSource();
            var jobMock = new Mock<IDownloadJob>();
            jobMock.Setup(i => i.RequestAbort()).Callback(() => source.Cancel());
            jobMock.Setup(i => i.AsyncState).Returns(state);
            jobMock.Setup(i => i.IsCompleted).Returns(false);
            jobMock.Setup(i => i.Updates).Returns(Updates);

            Task.Run(
                () => {
                    if (FakeDownloadTimeMs >= 99 && Updates.Count > 1)
                    {
                        int interval = FakeDownloadTimeMs / Updates.Count;
                        for (int i = 0; i < Updates.Count; i++)
                        {
                            Thread.Sleep(interval);
                            if (source.Token.IsCancellationRequested) return;

                            var progressMock = new Mock<IDownloadProgress>();
                            progressMock.Setup(p => p.CurrentUpdateIndex).Returns(i);
                            progressMock.Setup(p => p.PercentComplete).Returns(100 / Updates.Count * i);

                            var argsMock = new Mock<IDownloadProgressChangedCallbackArgs>();
                            argsMock.Setup(a => a.Progress).Returns(progressMock.Object);

                            ((IDownloadProgressChangedCallback)onCompleted).Invoke(jobMock.Object, argsMock.Object);
                        }
                    }
                    else
                    {
                        for (int delayed = 0; delayed <= FakeDownloadTimeMs; delayed = delayed + 10)
                        {
                            if (source.Token.IsCancellationRequested)
                            {
                                return;
                            }
                            Thread.Sleep(10);
                        }
                    }
                    if (!source.Token.IsCancellationRequested)
                    {
                        jobMock.Setup(i => i.IsCompleted).Returns(true);
                        ((IDownloadCompletedCallback)onCompleted).Invoke(jobMock.Object, null);
                    }                   
                }, source.Token);
            return jobMock.Object;
        }

        public IDownloadResult Download()
        {
            Thread.Sleep(FakeDownloadTimeMs);
            if (FakeDownloadResult == null)
            {
                Updates.OfType<UpdateFake>().ToList().ForEach(u => u.IsDownloaded = true);
                return CommonMocks.GetDownloadResult(OperationResultCode.orcSucceeded);
            }
            return FakeDownloadResult;
        }

        public IDownloadResult EndDownload(IDownloadJob value)
        {
            if (FakeDownloadResult == null)
            {
                Updates.OfType<UpdateFake>().ToList().ForEach(u => u.IsDownloaded = true);
                return CommonMocks.GetDownloadResult(OperationResultCode.orcSucceeded);
            }
            return FakeDownloadResult;
        }
    }
}
