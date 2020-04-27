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
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WuDataContract.Enums;

namespace WcfWuRemoteService.Configuration
{
    /// <summary>
    /// Configuration element for timeout values of <see cref="WindowsUpdateApiController.WuApiController"/>.
    /// </summary>
    public class WuApiControllerTimeoutElement : ConfigurationElement
    {
        public const string ElementName = "timeouts";
        const string SearchTimeout = "searchTimeout";
        const string DownloadTimeout = "downloadTimeout";
        const string InstallTimeout = "installTimeout";

        private static ConfigurationPropertyCollection _properties;
        private static ConfigurationProperty _searchTimeout;
        private static ConfigurationProperty _downloadTimeout;
        private static ConfigurationProperty _installTimeout;

        public WuApiControllerTimeoutElement() {
            _searchTimeout = new ConfigurationProperty(SearchTimeout, typeof(int), (int)DefaultAsyncOperationTimeout.SearchTimeout);
            _downloadTimeout = new ConfigurationProperty(DownloadTimeout, typeof(int), (int)DefaultAsyncOperationTimeout.DownloadTimeout);
            _installTimeout = new ConfigurationProperty(InstallTimeout, typeof(int), (int)DefaultAsyncOperationTimeout.InstallTimeout);
            _properties = new ConfigurationPropertyCollection();
            _properties.Add(_searchTimeout);
            _properties.Add(_downloadTimeout);
            _properties.Add(_installTimeout);

        }

        /// <summary>
        /// Search timeout in seconds.
        /// </summary>
        [ConfigurationProperty(SearchTimeout, IsRequired = false)]
        public int SearchTimeoutValue
        {
            get {
                int value = (int)base[_searchTimeout];
                if (value > int.MaxValue / 1000) value = int.MaxValue / 1000;
                if (value < 0) value = 0;
                return value;
            }
            set { base[_searchTimeout] = value; }
        }

        /// <summary>
        /// Search timeout in seconds.
        /// </summary>
        [ConfigurationProperty(DownloadTimeout, IsRequired = false)]
        public int DownloadTimeoutValue
        {
            get
            {
                int value = (int)base[_downloadTimeout];
                if (value > int.MaxValue / 1000) value = int.MaxValue / 1000;
                if (value < 0) value = 0;
                return value;
            }
            set { base[_downloadTimeout] = value; }
        }

        /// <summary>
        /// Installation timeout in seconds.
        /// </summary>
        [ConfigurationProperty(InstallTimeout, IsRequired = false)]
        public int InstallTimeoutValue
        {
            get
            {
                int value = (int)base[_installTimeout];
                if (value > int.MaxValue / 1000) value = int.MaxValue / 1000;
                if (value < 0) value = 0;
                return value;
            }
            set { base[_installTimeout] = value; }
        }

        /// <summary>
        /// The collection of properties.
        /// </summary>
        protected override ConfigurationPropertyCollection Properties
        {
            get { return _properties; }
        }

        /// <summary>
        /// This configuration section is never read only.
        /// </summary>
        /// <returns>false</returns>
        public override bool IsReadOnly() => false;

    }
}
