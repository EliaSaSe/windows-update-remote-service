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
using System.Threading;
using System.Threading.Tasks;
using WUApiLib;

namespace WuApiMocks
{
    public class UpdateSearcherFake : IUpdateSearcher
    {
        public UpdateSearcherFake()
        {
            FakeSearchTimeMs = 0;
        }

        public int FakeSearchTimeMs { get; set; }
        public ISearchResult FakeSearchResult { get; set; }

        #region interface


        public bool CanAutomaticallyUpgradeService
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

        public string ClientApplicationID { get; set; }

        public bool IncludePotentiallySupersededUpdates
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

        public bool Online { get; set; }

        public ServerSelection ServerSelection
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

        public string ServiceID { get; set; }

        public ISearchJob BeginSearch(string criteria, object onCompleted, object state)
        {
            CancellationTokenSource source = new CancellationTokenSource();

            var jobMock = new Mock<ISearchJob>();
            jobMock.Setup(j => j.AsyncState).Returns(state);
            jobMock.Setup(j => j.IsCompleted).Returns(false);
            jobMock.Setup(j => j.RequestAbort()).Callback(() => source.Cancel());

            Task.Run(
                () => {
                    for (int delayed = 0; delayed <= FakeSearchTimeMs; delayed = delayed + 10)
                    {
                        if (source.Token.IsCancellationRequested) return;
                        Thread.Sleep(10);
                    }
                    if (!source.Token.IsCancellationRequested)
                    {
                        jobMock.Setup(j => j.IsCompleted).Returns(true);
                        ((ISearchCompletedCallback)onCompleted).Invoke(jobMock.Object, null);
                    }

                }, source.Token);
            return jobMock.Object;
        }

        public ISearchResult EndSearch(ISearchJob searchJob)
        {
            if (FakeSearchResult == null)
            {
                return CommonMocks.GetSearchResult(new UpdateCollectionFake(), OperationResultCode.orcSucceeded);
            }
            return FakeSearchResult;
        }

        public string EscapeString(string unescaped)
        {
            throw new NotImplementedException();
        }

        public int GetTotalHistoryCount()
        {
            throw new NotImplementedException();
        }

        public IUpdateHistoryEntryCollection QueryHistory(int startIndex, int Count)
        {
            throw new NotImplementedException();
        }

        public ISearchResult Search(string criteria)
        {
            Thread.Sleep(FakeSearchTimeMs);
            if (FakeSearchResult == null)
            {
                return CommonMocks.GetSearchResult(new UpdateCollectionFake(), OperationResultCode.orcSucceeded);
            }
            return FakeSearchResult;
        }

        #endregion
    }
}
