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
using System.Runtime.Serialization;

namespace WuDataContract.DTO
{
    /// <summary>
    /// Describes the progress of an async "Windows Update"-operation.
    /// </summary>
    [DataContract]
    public class ProgressDescription
    {
        /// <summary>
        /// The update on that the service is currently working on. Can be null if it is unkown.
        /// </summary>
        [DataMember]
        public UpdateDescription CurrentUpdate { get; private set; }

        /// <summary>
        /// The zero based index of the update which is currently processed by the async operation.
        /// Null if the number of updates to proceed is unknown.
        /// </summary>
        [DataMember]
        public int? CurrentIndex { get; private set; }

        /// <summary>
        /// The total number of updates that the async operation has to process.
        /// Null if the number of updates to proceed is unknown.
        /// </summary>
        [DataMember]
        public int? Count { get; private set; }

        /// <summary>
        /// Progress in percent between 0 and 100 (includes 0 and 100).
        /// </summary>
        [DataMember]
        public int Percent { get; private set; }

        /// <summary>
        /// True if the number of updates to proceed is known, otherwise false.
        /// </summary>
        [DataMember]
        public bool IsIndeterminate {
            get; private set;
        }

        /// <summary>
        /// Creates a progress description with definable end.
        /// </summary>
        public ProgressDescription(UpdateDescription currentUpdate, int currentIndex, int count, int percent)
        {
            if (count <= 0) throw new IndexOutOfRangeException($"{nameof(count)} must be greater than zero.");
            if (currentIndex < 0 || (currentIndex >= count)) throw new IndexOutOfRangeException($"{nameof(currentIndex)} must be positive and smaller than {nameof(count)}.");
            if (percent < 0 || percent > 100) throw new IndexOutOfRangeException($"{nameof(percent)} must be between 0 and 100.");

            Count = count;
            CurrentIndex = currentIndex;
            CurrentUpdate = currentUpdate;
            IsIndeterminate = (Count == null) ? true : false;
            Percent = percent;
        }

        /// <summary>
        /// Creates a progress description without definable end.
        /// </summary>
        public ProgressDescription(UpdateDescription currentUpdate = null)
        {
            Count = null;
            CurrentIndex = null;
            CurrentUpdate = currentUpdate;
            IsIndeterminate = (Count == null) ? true : false;
        }

        public override string ToString()
        {
            if (Count == null) return "Progress not countable";
            if (CurrentUpdate != null) return $"{CurrentIndex + 1} of {Count}, {CurrentUpdate.Title}";
            return $"{CurrentIndex + 1} of {Count}";
        }

    }
}
