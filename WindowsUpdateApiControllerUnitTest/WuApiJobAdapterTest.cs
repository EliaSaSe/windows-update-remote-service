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
using WindowsUpdateApiController.Helper;
using WUApiLib;

namespace WindowsUpdateApiControllerUnitTest
{
    [TestClass]
    public class WuApiJobAdapterTest
    {
        readonly MockRepository MoqFactory = new MockRepository(MockBehavior.Strict) { };

        [TestMethod, TestCategory("Passthrough")]
        public void Should_PassThroughInvokes_When_UsingSearchAdapter()
        {
            var job = MoqFactory.Create<ISearchJob>(MockBehavior.Loose);
            var adapter = new WuApiSearchJobAdapter(job.Object);

            var x = adapter.AsyncState;
            job.Verify(j => j.AsyncState, Times.Once);

            var y = adapter.IsCompleted;
            job.Verify(j => j.IsCompleted, Times.Once);

            adapter.CleanUp();
            job.Verify(j => j.CleanUp(), Times.Once);

            adapter.RequestAbort();
            job.Verify(j => j.RequestAbort(), Times.Once);

            Assert.AreSame(job.Object, adapter.InternalJobObject);
        }

        [TestMethod, TestCategory("Passthrough")]
        public void Should_PassThroughInvokes_When_UsingDownloadAdapter()
        {
            var job = MoqFactory.Create<IDownloadJob>(MockBehavior.Loose);
            var adapter = new WuApiDownloadJobAdapter(job.Object);

            var x = adapter.AsyncState;
            job.Verify(j => j.AsyncState, Times.Once);

            var y = adapter.IsCompleted;
            job.Verify(j => j.IsCompleted, Times.Once);

            adapter.CleanUp();
            job.Verify(j => j.CleanUp(), Times.Once);

            adapter.RequestAbort();
            job.Verify(j => j.RequestAbort(), Times.Once);

            Assert.AreSame(job.Object, adapter.InternalJobObject);
        }

        [TestMethod, TestCategory("Passthrough")]
        public void Should_PassThroughInvokes_When_UsingInstallAdapter()
        {
            var job = MoqFactory.Create<IInstallationJob>(MockBehavior.Loose);
            var adapter = new WuApiInstallJobAdapter(job.Object);

            var x = adapter.AsyncState;
            job.Verify(j => j.AsyncState, Times.Once);

            var y = adapter.IsCompleted;
            job.Verify(j => j.IsCompleted, Times.Once);

            adapter.CleanUp();
            job.Verify(j => j.CleanUp(), Times.Once);

            adapter.RequestAbort();
            job.Verify(j => j.RequestAbort(), Times.Once);

            Assert.AreSame(job.Object, adapter.InternalJobObject);
        }

        [TestMethod, TestCategory("No Null")]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Should_NotAllowNullJobs_When_CreatingSearchAdapter()
        {
            new WuApiSearchJobAdapter(null);
        }

        [TestMethod, TestCategory("No Null")]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Should_NotAllowNullJobs_When_CreatingDownloadAdapter()
        {
            new WuApiDownloadJobAdapter(null);
        }

        [TestMethod, TestCategory("No Null")]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Should_NotAllowNullJobs_When_CreatingInstallAdapter()
        {
            new WuApiInstallJobAdapter(null);
        }
    }
}

