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
    public class UpdateDescriptionTest
    {
        [TestMethod, TestCategory("UpdateDescription")]
        public void Should_ContainSpecifiedObjects_When_SetAndGetProperties()
        {
            var ud = new UpdateDescription();
            string str1 = "t1", str2 = "t2", str3 = "t3";
            long size1 = 5, size2 = 10;

            ud.Description = str1;
            ud.ID = str2;
            ud.Title = str3;
            ud.MaxByteSize = size1;
            ud.MinByteSize = size2;

            Assert.AreEqual(ud.Description, str1);
            Assert.AreEqual(ud.ID, str2);
            Assert.AreEqual(ud.Title, str3);
            Assert.AreEqual(ud.MaxByteSize, size1);
            Assert.AreEqual(ud.MinByteSize, size2);

            ud.IsImportant = true;
            Assert.AreEqual(ud.IsImportant, true);
            ud.IsImportant = false;

            ud.EulaAccepted = true;
            Assert.AreEqual(ud.EulaAccepted, true);
            ud.EulaAccepted = false;

            ud.IsDownloaded = true;
            Assert.AreEqual(ud.IsDownloaded, true);
            ud.IsDownloaded = false;

            ud.IsInstalled = true;
            Assert.AreEqual(ud.IsInstalled, true);
            ud.IsInstalled = false;

            ud.SelectedForInstallation = true;
            Assert.AreEqual(ud.SelectedForInstallation, true);
            ud.SelectedForInstallation = false;
        }

        [TestMethod, TestCategory("UpdateDescription")]
        public void Should_ReturnTrue_When_ObjectsAreEqual()
        {
            var ud = new UpdateDescription();
            Assert.IsTrue(ud.Equals(ud));

            var ud1 = new UpdateDescription();
            ud1.ID = "id";
            var ud2 = new UpdateDescription();
            ud2.ID = "id";
            Assert.IsTrue(ud1.Equals(ud2));
        }

        [TestMethod, TestCategory("UpdateDescription")]
        public void Should_ReturnFalse_When_ObjectsAreNotEqual()
        {
            var ud = new UpdateDescription();
            var obj = new Object();

            Assert.IsFalse(ud.Equals(obj));
            Assert.IsFalse(ud.Equals(null));

            var ud1 = new UpdateDescription();
            ud1.ID = "id1";
            var ud2 = new UpdateDescription();
            ud2.ID = "id2";
            Assert.IsFalse(ud1.Equals(ud2));

            var ud3 = new UpdateDescription();
            ud3.ID = "id3";
            var ud4 = new UpdateDescription();
            Assert.IsFalse(ud1.Equals(ud2));

            var ud5 = new UpdateDescription();
            var ud6 = new UpdateDescription();
            ud6.ID = "id6";
            Assert.IsFalse(ud1.Equals(ud2));
        }

        [TestMethod, TestCategory("UpdateDescription")]
        public void Should_UseIdForHash_When_CallGetHash()
        {
            var ud = new UpdateDescription();
            ud.ID = "id";
            string id = "id";

            Assert.AreEqual(ud.GetHashCode(), id.GetHashCode());
        }
    }
}
