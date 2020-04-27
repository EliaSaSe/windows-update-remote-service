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
using WuDataContract.DTO;

namespace WuDataContractUnitTest
{
    [TestClass]
    public class WuEnviromentTest
    {
        [TestMethod]
        public void Should_ContainSpecifiedValues_When_CreateWuEnviroment()
        {
            string fqdn = "fqdn", os = "osname", updateserver = "update server", target = "target group";
            TimeSpan uptime = new TimeSpan(1, 0, 0);
            long freespace = 10;
            var env = new WuEnviroment(fqdn, os, updateserver, target, uptime, freespace);

            Assert.AreEqual(env.FQDN, fqdn);
            Assert.AreEqual(env.OperatingSystemName, os);
            Assert.AreEqual(env.UpdateServer, updateserver);
            Assert.AreEqual(env.TargetGroup, target);
            Assert.AreEqual(env.UpTime, uptime);
            Assert.AreEqual(env.FreeSpace, freespace);
        }

        [TestMethod]
        public void Should_NotAcceptNegativeFreeSpace_When_CreateWuEnviroment()
        {
            new WuEnviroment("a", "a", "a", "a", new TimeSpan(1, 0, 0), 1);
            new WuEnviroment("a", "a", "a", "a", new TimeSpan(1, 0, 0), 0);
            try
            {
                new WuEnviroment("a", "a", "a", "a", new TimeSpan(1, 0, 0), -1);
                Assert.Fail("exception expected");
            }
            catch (ArgumentOutOfRangeException) { }
        }

        [TestMethod, TestCategory("No Null")]
        public void Should_NotAcceptNullValues_When_CreateWuEnviroment()
        {
            string fqdn = "fqdn", os = "osname", updateserver = "update server", target = "target group";
            TimeSpan uptime = new TimeSpan(1, 0, 0);
            long freespace = 10;
            var env = new WuEnviroment(fqdn, os, updateserver, target, uptime, freespace);

            try
            {
                new WuEnviroment(null, os, updateserver, target, uptime, freespace);
                Assert.Fail("exception expected");
            }
            catch (ArgumentNullException) { }

            try
            {
                new WuEnviroment(" ", os, updateserver, target, uptime, freespace);
                Assert.Fail("exception expected");
            }
            catch (ArgumentNullException) { }

            try
            {
                new WuEnviroment(fqdn, null, updateserver, target, uptime, freespace);
                Assert.Fail("exception expected");
            }
            catch (ArgumentNullException) { }

            try
            {
                new WuEnviroment(fqdn, " ", updateserver, target, uptime, freespace);
                Assert.Fail("exception expected");
            }
            catch (ArgumentNullException) { }

            try
            {
                new WuEnviroment(fqdn, os, null, target, uptime, freespace);
                Assert.Fail("exception expected");
            }
            catch (ArgumentNullException) { }

            try
            {
                new WuEnviroment(fqdn, os, updateserver, null, uptime, freespace);
                Assert.Fail("exception expected");
            }
            catch (ArgumentNullException) { }
        }
    }
}
