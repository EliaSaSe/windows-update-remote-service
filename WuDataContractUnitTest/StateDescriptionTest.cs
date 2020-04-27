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
using WuDataContract.Enums;

namespace WuDataContractUnitTest
{
    [TestClass]
    public class StateDescriptionTest
    {
        [TestMethod]
        public void Should_ContainSpecifiedValues_When_CreateStateDescription()
        {
            ProgressDescription pd = new ProgressDescription();
            string name = "name";
            string desc = "desc";
            var insts = InstallerStatus.Busy;
            var en = new WuEnviroment("a", "a", "a", "a", TimeSpan.Zero, 10);

            StateDescription sd = new StateDescription(WuStateId.Installing, name, desc, insts, en, pd);

            Assert.AreEqual(name, sd.DisplayName);
            Assert.AreEqual(desc, sd.Description);
            Assert.AreEqual(WuStateId.Installing, sd.StateId);
            Assert.AreEqual(insts, sd.InstallerStatus);
            Assert.AreSame(pd, sd.Progress);
            Assert.AreSame(en, sd.Enviroment);

        }

        [TestMethod, TestCategory("No Null")]
        public void Should_NotAllowEmptyDisplayName_When_CreateStateDescription()
        {
            var en = new WuEnviroment("a", "a", "a", "a", TimeSpan.Zero, 10);
            try
            {
                StateDescription sd = new StateDescription(WuStateId.Installing, "", "",InstallerStatus.Busy, en);
                Assert.Fail("exception expected");
            }
            catch (ArgumentException) { }

            try
            {
                StateDescription sd = new StateDescription(WuStateId.Installing, " ", "", InstallerStatus.Busy, en);
                Assert.Fail("exception expected");
            }
            catch (ArgumentException) { }

            try
            {
                StateDescription sd = new StateDescription(WuStateId.Installing, null, "", InstallerStatus.Busy, en);
                Assert.Fail("exception expected");
            }
            catch (ArgumentException) { }
        }

        [TestMethod]
        public void Should_ReturnTrue_When_CompareStateId()
        {
            var en = new WuEnviroment("a", "a", "a", "a", TimeSpan.Zero, 10);
            StateDescription sd = new StateDescription(WuStateId.Installing, "name", "", InstallerStatus.Busy, en);
            Assert.IsTrue(sd.Equals(WuStateId.Installing));
        }

        [TestMethod]
        public void Should_ReturnStateIdString_When_CallToString()
        {
            var en = new WuEnviroment("a", "a", "a", "a", TimeSpan.Zero, 10);
            StateDescription sd = new StateDescription(WuStateId.DownloadCompleted, "name", "desc", InstallerStatus.Busy, en);
            Assert.AreEqual(sd.ToString(), WuStateId.DownloadCompleted.ToString("G"));
        }
    }
}
