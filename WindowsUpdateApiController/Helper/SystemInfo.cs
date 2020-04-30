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
using Microsoft.Win32;
using System;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using WUApiLib;

namespace WindowsUpdateApiController.Helper
{
    /// <summary>
    /// Provides informations about the host system.
    /// </summary>
    public interface ISystemInfo
    {
        /// <summary>
        /// FQDN of the hostsystem.
        /// </summary>
        string GetFQDN();
        
        /// <summary>
        /// Free space in bytes on the system drive.
        /// </summary>
        long GetFreeSpace();

        /// <summary>
        /// Name of the operating system.
        /// </summary>
        string GetOperatingSystemName();

        /// <summary>
        /// Time elapsed since the system started.
        /// </summary>
        TimeSpan GetUptime();

        /// <summary>
        /// Update server used by this host.
        /// </summary>
        string GetWuServer();

        /// <summary>
        /// WSUS Targeting Group.
        /// </summary>
        string GetTargetGroup();

        /// <summary>
        /// Indicates whether the system must be rebooted before updates can be installed.
        /// </summary>
        bool IsRebootRequired();

    }

    /// <summary>
    /// Provides informations about the host system. This class is not thread safe.
    /// </summary>
    public class SystemInfo : ISystemInfo
    {
        readonly DriveInfo _windrive;
        readonly ISystemInformation _wuclientinfo = new SystemInformation();
        string _fqdn = null;
        string _wuServer = null;
        string _targetGroup = null;

        public SystemInfo()
        {
            _windrive = new DriveInfo(Path.GetPathRoot(Environment.SystemDirectory));
        }

        /// <summary>
        /// Use this method (UINT64) instead of <see cref="Environment.TickCount"/> (INT32) 
        /// to prevent integer overflow on long running servers.
        /// </summary>
        [DllImport("kernel32")]
        private extern static UInt64 GetTickCount64();

        /// <summary>
        /// FQDN of the hostsystem.
        /// </summary>
        public string GetFQDN()
        {
            if (_fqdn == null)
            {
                //http://stackoverflow.com/questions/804700/how-to-find-fqdn-of-local-machine-in-c-net
                string domainName = IPGlobalProperties.GetIPGlobalProperties().DomainName;
                string hostName = Dns.GetHostName();
                domainName = "." + domainName;
                if (!hostName.EndsWith(domainName)) hostName += domainName;
                _fqdn = hostName;
            }
            return _fqdn;
        }

        /// <summary>
        /// Free space in bytes on the system drive.
        /// </summary>
        public long GetFreeSpace() => _windrive.AvailableFreeSpace;

        /// <summary>
        /// Name of the operating system.
        /// </summary>
        public string GetOperatingSystemName() => Environment.OSVersion.VersionString;

        /// <summary>
        /// Time elapsed since the system started.
        /// </summary>
        public TimeSpan GetUptime() => TimeSpan.FromMilliseconds(GetTickCount64());

        /// <summary>
        /// The used update server.
        /// </summary>
        public string GetWuServer()
        {
            if (_wuServer == null)
            {               
                _wuServer = GetRegValue<string>(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate", @"WUServer", "Microsoft");
                if (_wuServer == null) _wuServer = "Microsoft";
            }
            return _wuServer;
        }

        /// <summary>
        /// WSUS Targeting Group
        /// </summary>
        public string GetTargetGroup()
        {
            if (_targetGroup == null)
            {
                int enabled = GetRegValue<int>(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate", @"TargetGroupEnabled", 0);
                if (enabled == 1)
                {
                    _targetGroup = GetRegValue<string>(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate", @"TargetGroup", "Disabled");
                    if (_targetGroup == null) _targetGroup = "Disabled";
                }
                else
                {
                    _targetGroup = "Disabled";
                }           
            }
            return _targetGroup;
        }

        /// <summary>
        /// Indicates whether the system must be rebooted before updates can be installed.
        /// </summary>
        public bool IsRebootRequired() => _wuclientinfo.RebootRequired;

        private T GetRegValue<T>(string registryKeyPath, string value, T defaultValue = default(T))
        {
            try
            {
                return (T)Registry.GetValue(registryKeyPath, value, defaultValue);
            }
            catch
            {
                return defaultValue;
            }
        }
    }
}
