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

namespace WcfWuRemoteService.Helper
{
    /// <summary>
    /// Provides the configuration data for <see cref="WuRemoteService"/>.
    /// </summary>
    public interface IWuApiConfigProvider : IDisposable
    {
        /// <summary>
        /// Timeout value (sec.) for search operations.
        /// </summary>
        int SearchTimeout { get; set; }
        /// <summary>
        /// Timeout value (sec.) for download operations.
        /// </summary>
        int DownloadTimeout { get; set; }
        /// <summary>
        /// Timeout value (sec.) for install operations.
        /// </summary>
        int InstallTimeout { get; set; }
        /// <summary>
        /// Value for the auto accept eula setting.
        /// </summary>
        bool AutoAcceptEulas { get; set; }
        /// <summary>
        /// Value for the auto select updates setting.
        /// </summary>
        bool AutoSelectUpdates { get; set; }
        /// <summary>
        /// Writes changed settings.
        /// </summary>
        void Save();
    }
}
