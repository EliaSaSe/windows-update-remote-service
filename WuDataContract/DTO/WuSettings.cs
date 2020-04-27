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
    /// Settings of a running <see cref="IWuRemoteService"/> instance.
    /// </summary>
    [DataContract]
    public class WuSettings
    {
        /// <summary>
        /// A running search will be aborted when this timeout was reached.
        /// </summary>
        [DataMember]
        public int SearchTimeoutSec { get; private set; }

        /// <summary>
        /// A running download will be aborted when this timeout was reached.
        /// </summary>
        [DataMember]
        public int DownloadTimeoutSec { get; private set; }

        /// <summary>
        /// A running installation will be aborted when this timeout was reached.
        /// </summary>
        [DataMember]
        public int InstallTimeoutSec { get; private set; }

        /// <summary>
        /// Autoaccept the eulas of updates before download or install them.
        /// </summary>
        [DataMember]
        public bool AutoAcceptEulas { get; private set; }

        /// <summary>
        /// Autoselect updates for installation, when <see cref="UpdateDescription.IsImportant"/> is set.
        /// </summary>
        [DataMember]
        public bool AutoSelectUpdates { get; private set; }

        /// <exception cref="ArgumentOutOfRangeException" />
        public WuSettings(int searchTimeout, int downloadTimeout, int installTimeout, bool autoAcceptEulas, bool autoSelectUpdates)
        {
            if (searchTimeout < 0) throw new ArgumentOutOfRangeException(nameof(searchTimeout));
            if (downloadTimeout < 0) throw new ArgumentOutOfRangeException(nameof(downloadTimeout));
            if (installTimeout < 0) throw new ArgumentOutOfRangeException(nameof(installTimeout));

            SearchTimeoutSec = searchTimeout;
            DownloadTimeoutSec = downloadTimeout;
            InstallTimeoutSec = installTimeout;
            AutoAcceptEulas = autoAcceptEulas;
            AutoSelectUpdates = autoSelectUpdates;
        }
    }
}
