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
using System;
using WUApiLib;

namespace WindowsUpdateApiController.Helper
{
    /// <summary>
    /// Represents the common interface for <see cref="ISearchJob"/>, <see cref="IDownloadJob"/> and <see cref="IInstallationJob"/>.
    /// </summary>
    internal abstract class WuApiJobAdapter
    {
        public abstract void CleanUp();
        public abstract void RequestAbort();
        public abstract bool IsCompleted { get; }
        public abstract dynamic AsyncState { get; }

        /// <summary>
        /// The underlying job.
        /// </summary>
        public abstract object InternalJobObject { get; }
    }

    /// <summary>
    /// Adpater for <see cref="ISearchJob"/> to <see cref="WuApiJobAdapter"/>.
    /// </summary>
    internal class WuApiSearchJobAdapter : WuApiJobAdapter
    {
        ISearchJob _job;

        public WuApiSearchJobAdapter(ISearchJob job)
        {
            if (job == null) throw new ArgumentNullException(nameof(job));
            _job = job;
        }

        public override void CleanUp() => _job.CleanUp();
        public override void RequestAbort() => _job.RequestAbort();
        public override bool IsCompleted => _job.IsCompleted;
        public override dynamic AsyncState => _job.AsyncState;
        public override object InternalJobObject => _job;
    }

    /// <summary>
    /// Adpater for <see cref="IDownloadJob"/> to <see cref="WuApiJobAdapter"/>.
    /// </summary>
    internal class WuApiDownloadJobAdapter : WuApiJobAdapter
    {
        IDownloadJob _job;

        public WuApiDownloadJobAdapter(IDownloadJob job)
        {
            if (job == null) throw new ArgumentNullException(nameof(job));
            _job = job;
        }

        public override void CleanUp() => _job.CleanUp();
        public override void RequestAbort() => _job.RequestAbort();
        public override bool IsCompleted => _job.IsCompleted;
        public override dynamic AsyncState => _job.AsyncState;
        public override object InternalJobObject => _job;
    }

    /// <summary>
    /// Adpater for <see cref="IInstallationJob"/> to <see cref="WuApiJobAdapter"/>.
    /// </summary>
    internal class WuApiInstallJobAdapter : WuApiJobAdapter
    {
        IInstallationJob _job;

        public WuApiInstallJobAdapter(IInstallationJob job)
        {
            if (job == null) throw new ArgumentNullException(nameof(job));
            _job = job;
        }

        public override void CleanUp() => _job.CleanUp();
        public override void RequestAbort() => _job.RequestAbort();
        public override bool IsCompleted => _job.IsCompleted;
        public override dynamic AsyncState => _job.AsyncState;
        public override object InternalJobObject => _job;
    }


}
