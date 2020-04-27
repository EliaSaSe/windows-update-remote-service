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
    /// Informations about the system where the windows update remote service is running on.
    /// </summary>
    [DataContract]
    public class WuEnviroment
    {
        /// <summary>
        /// FQDN of the system.
        /// </summary>
        [DataMember]
        public string FQDN {get; private set;}

        /// <summary>
        /// Free space on system disk in bytes.
        /// </summary>
        [DataMember]
        public long FreeSpace { get; private set; }

        /// <summary>
        /// OS-Name of the system.
        /// </summary>
        [DataMember]
        public string OperatingSystemName { get; private set; }

        /// <summary>
        /// Uptime of the system.
        /// </summary>
        [DataMember]
        public TimeSpan UpTime { get; private set; }

        /// <summary>
        /// Update-Server, where the system will search and download updates.
        /// </summary>
        [DataMember]
        public string UpdateServer { get; private set; }

        /// <summary>
        /// WSUS-Targetinggroup where the system belongs to.
        /// </summary>
        [DataMember]
        public string TargetGroup { get; private set; }

        public WuEnviroment(string fqdn, string osName, string updateServer, string targetGroup, TimeSpan uptime, long freeSpace)
        {
            if (string.IsNullOrWhiteSpace(fqdn)) throw new ArgumentNullException(nameof(fqdn));
            if (string.IsNullOrWhiteSpace(osName)) throw new ArgumentNullException(nameof(osName));

            if (updateServer == null) throw new ArgumentNullException(nameof(updateServer));
            if (targetGroup == null) throw new ArgumentNullException(nameof(targetGroup));
            if (freeSpace < 0) throw new ArgumentOutOfRangeException("Must be zero or greather.", nameof(uptime));

            FQDN = fqdn;
            OperatingSystemName = osName;
            UpdateServer = updateServer;
            TargetGroup = targetGroup;
            UpTime = uptime;
            FreeSpace = freeSpace;
        }
    }
}
