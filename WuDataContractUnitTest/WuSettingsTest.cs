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
    public class WuSettingsTest
    {
        [TestMethod]
        public void Should_ContainSpecifiedValues_When_CreateWuSettings()
        {
            int timeout1 = 1, timeout2 = 2, timeout3 = 3;

            var set = new WuSettings(timeout1, timeout2, timeout3, true, false);
            Assert.AreEqual(set.SearchTimeoutSec, timeout1);
            Assert.AreEqual(set.DownloadTimeoutSec, timeout2);
            Assert.AreEqual(set.InstallTimeoutSec, timeout3);
            Assert.AreEqual(set.AutoAcceptEulas, true);
            Assert.AreEqual(set.AutoSelectUpdates, false);

            var set2 = new WuSettings(timeout1, timeout2, timeout3, false, true);
            Assert.AreEqual(set2.AutoAcceptEulas, false);
            Assert.AreEqual(set2.AutoSelectUpdates, true);
        }

        [TestMethod]
        public void Should_NotAcceptNegativeTimeouts_When_CreateWuSettings()
        {
            new WuSettings(1, 1, 1, true, false);
            new WuSettings(0, 0, 0, true, false);
            try
            {
                new WuSettings(-1, 0, 0, true, false);
                Assert.Fail("exception expected");
            }
            catch (ArgumentOutOfRangeException) { }
            try
            {
                new WuSettings(0, -1, 0, true, false);
                Assert.Fail("exception expected");
            }
            catch (ArgumentOutOfRangeException) { }
            try
            {
                new WuSettings(0, 0, -1, true, false);
                Assert.Fail("exception expected");
            }
            catch (ArgumentOutOfRangeException) { }
        }
    }
}
