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
using System;
using WindowsUpdateApiController.Helper;

namespace WindowsUpdateApiControllerUnitTest
{
    [TestClass]
    public class SystemInfoTest
    {
        [TestMethod, TestCategory("No Null")]
        public void Should_NotReturnNull_When_RequestSystemInfoProperties()
        {
            var systeminfo = new SystemInfo();

            var target = systeminfo.GetTargetGroup();
            var server = systeminfo.GetWuServer();
            var os = systeminfo.GetOperatingSystemName();
            var freeSpace = systeminfo.GetFreeSpace();

            Assert.IsNotNull(target);
            Assert.IsNotNull(server);
            Assert.IsFalse(string.IsNullOrWhiteSpace(os));
            Assert.IsTrue(freeSpace > (decimal)0.0);
        }

        [TestMethod]
        public void Should_ContainNetBiosName_When_RequestFQDN()
        {
            var systeminfo = new SystemInfo();

            if (systeminfo.GetFQDN().IndexOf(Environment.MachineName, StringComparison.OrdinalIgnoreCase)  == -1)
            {
                Assert.Fail("netbios name not found in fqdn");
            }
        }

        [TestMethod]
        public void Should_NotReturnZeroTimeSpan_When_RequestUptime()
        {
            var systeminfo = new SystemInfo();
            var uptime = systeminfo.GetUptime();
            Assert.IsTrue(uptime.Ticks > 0); // system bootup and test execution in less than one tick?
        }
    }
}
