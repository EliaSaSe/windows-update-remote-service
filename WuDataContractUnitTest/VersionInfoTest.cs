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
using System.Collections.Generic;
using WuDataContract.DTO;

namespace WuDataContractUnitTest
{
    [TestClass]
    public class VersionInfoTest
    {
        [TestMethod, TestCategory("Property initialization"), TestCategory("VersionInfo")]
        public void Should_InitializingProperties_When_CreateVersionInfo()
        {
            int major = 4, minor = 3, build = 2, rev = 1;
            string com = "component1";

            var vI = new VersionInfo(com, major, minor, build, rev);

            Assert.AreEqual(major, vI.Major);
            Assert.AreEqual(minor, vI.Minor);
            Assert.AreEqual(build, vI.Build);
            Assert.AreEqual(rev, vI.Revision);
            Assert.AreEqual(com, vI.ComponentName);
            Assert.IsFalse(vI.IsContract);

            var vI2 = new VersionInfo("c", 1, 0, 0, 0, false);
            var vI3 = new VersionInfo("c", 1, 0, 0, 0, true);

            Assert.IsFalse(vI2.IsContract);
            Assert.IsTrue(vI3.IsContract);
        }

        [TestMethod, TestCategory("Property initialization"), TestCategory("VersionInfo")]
        public void Should_NotAllowNegativeVersionNumber_When_CreateVersionInfo()
        {
            var badVersions = new List<Tuple<int, int, int, int>>();
            badVersions.Add(new Tuple<int, int, int, int>(1, 0, 0, -1));
            badVersions.Add(new Tuple<int, int, int, int>(1, 0, -1, 0));
            badVersions.Add(new Tuple<int, int, int, int>(1, -1, 0, 0));
            badVersions.Add(new Tuple<int, int, int, int>(-1, 0, 0, 0));
            badVersions.Add(new Tuple<int, int, int, int>(1, 0, 0, int.MinValue));
            badVersions.Add(new Tuple<int, int, int, int>(1, 0, int.MinValue, 0));
            badVersions.Add(new Tuple<int, int, int, int>(1, int.MinValue, 0, 0));
            badVersions.Add(new Tuple<int, int, int, int>(int.MinValue, 0, 0, 0));
            foreach (var ver in badVersions)
            {
                try
                {
                    new VersionInfo("c", ver.Item1, ver.Item2, ver.Item3, ver.Item4);
                    Assert.Fail($"{nameof(ArgumentOutOfRangeException)} expected.");
                }
                catch (ArgumentOutOfRangeException) { } // Test passed
            }
        }

        [TestMethod, TestCategory("Property initialization"), TestCategory("VersionInfo")]
        public void Should_AllowPositiveVersionNumber_When_CreateVersionInfo()
        {
            var versions = new List<Tuple<int, int, int, int>>();
            versions.Add(new Tuple<int, int, int, int>(int.MaxValue, int.MaxValue, int.MaxValue, int.MaxValue));
            versions.Add(new Tuple<int, int, int, int>(0, 0, 0, 0));
            foreach (var ver in versions)
            {
                new VersionInfo("c", ver.Item1, ver.Item2, ver.Item3, ver.Item4);
            }
        }

        [TestMethod, TestCategory("Compare"), TestCategory("VersionInfo")]
        public void Should_BeEqual_When_CompareEqualComponentNameAndVersion()
        {
            var v1 = new VersionInfo("c", 1, 2, 3, 4);
            var v2 = new VersionInfo("c", 1, 2, 3, 4);
            Assert.IsTrue(v1.Equals(v2));
        }

        [TestMethod, TestCategory("Compare"), TestCategory("VersionInfo")]
        public void Should_BeEqual_When_CompareSameReference()
        {
            var v1 = new VersionInfo("c", 1, 2, 3, 4);
            Assert.IsTrue(v1.Equals(v1));
        }

        [TestMethod, TestCategory("Compare"), TestCategory("VersionInfo")]
        public void Should_NotBeEqual_When_ComponentNamesAreNotEqual()
        {
            var v1 = new VersionInfo("c", 1, 2, 3, 4);
            var v2 = new VersionInfo("c2", 1, 2, 3, 4);
            Assert.IsFalse(v1.Equals(v2));
        }

        [TestMethod, TestCategory("Compare"), TestCategory("VersionInfo")]
        public void Should_NotBeEqual_When_ComponentVersionNumbersAreNotEqual()
        {
            var v1 = new VersionInfo("c", 1, 2, 3, 4);
            var versions = new VersionInfo[] {
                new VersionInfo("c", 1, 2, 2, 4),
                new VersionInfo("c", 1, 3, 3, 4),
                new VersionInfo("c", 2, 2, 3, 4),
                new VersionInfo("c", 1, 2, 4, 4),
                new VersionInfo("c", 4, 2, 3, 3),
            };
            foreach (var ver in versions)
            {
                Assert.IsFalse(v1.Equals(ver));
            }
        }

        [TestMethod, TestCategory("Compare"), TestCategory("VersionInfo")]
        public void Should_ReturnNegative1_When_CompareHigherVersions()
        {
            var v1 = new VersionInfo("c", 1, 0, 0, 0);
            var versions = new VersionInfo[] {
                new VersionInfo("c", 1, 0, 0, 1),
                new VersionInfo("c", 1, 0, 1, 0),
                new VersionInfo("c", 1, 1, 0, 0),
                new VersionInfo("c", 2, 0, 0, 0),
                new VersionInfo("c", 1, 1, 1, 1)
            };
            foreach (var ver in versions)
            {
                Assert.AreEqual(-1, v1.CompareTo(ver));
            }
        }

        [TestMethod, TestCategory("Compare"), TestCategory("VersionInfo")]
        public void Should_Return1_When_CompareLowerVersions()
        {
            var v1 = new VersionInfo("c", 2, 0, 0, 0);
            var versions = new VersionInfo[] {
                new VersionInfo("c", 1, int.MaxValue, int.MaxValue, int.MaxValue),
                new VersionInfo("c", 1, 0, 0, int.MaxValue),
                new VersionInfo("c", 1, 0, 0, 0),
                new VersionInfo("c", 1, 1, 0, 0),
                new VersionInfo("c", 1, 0, 1, 0),
                new VersionInfo("c", 1, 0, 0, 1)
            };
            foreach (var ver in versions)
            {
                Assert.AreEqual(1, v1.CompareTo(ver));
            }
        }

        [TestMethod, TestCategory("Compare"), TestCategory("VersionInfo")]
        public void Should_SortAscendingByNameThenByVersion_When_SortListOfVersionInfo()
        {
            var v1 = new VersionInfo("c", 1, 1, 0, 0);
            var v2 = new VersionInfo("c", 2, 0, 0, 0);
            var v3 = new VersionInfo("b", 2, 0, 0, 0);
            var v4 = new VersionInfo("c", 1, 0, 0, 0);
            var v5 = new VersionInfo("a", 1, 0, 0, 0);
            var v6 = new VersionInfo("c", 1, 1, 1, 0);
            var v7 = new VersionInfo("c", 1, 0, 1, 0);
            var v8 = new VersionInfo("c", 1, 1, 1, 1);

            var versions = new List<VersionInfo>() {v1, v2, null, v3, v4, v5, v6, v7, v8};
            versions.Sort();

            Assert.IsNull(versions[0]);
            Assert.AreEqual(v5, versions[1]);
            Assert.AreEqual(v3, versions[2]);
            Assert.AreEqual(v4, versions[3]);
            Assert.AreEqual(v7, versions[4]);
            Assert.AreEqual(v1, versions[5]);
            Assert.AreEqual(v6, versions[6]);
            Assert.AreEqual(v8, versions[7]);
            Assert.AreEqual(v2, versions[8]);
        }

        [TestMethod, TestCategory("Compare"), TestCategory("VersionInfo")]
        public void Should_IgnoreRevisionNumber_When_CompareAndRevIgnoreFlagIsSet()
        {
            var v1 = new VersionInfo("c", 2, 0, 10, 10);
            var v2 = new VersionInfo("c", 2, 0, 00, 00);
            var v3 = new VersionInfo("c", 2, 0, 20, 20);

            Assert.IsFalse(v1.HasHigherVersionThan(v2, true));
            Assert.IsFalse(v1.HasHigherVersionThan(v3, true));
            Assert.IsFalse(v1.HasLowerVersionThan(v2, true));
            Assert.IsFalse(v1.HasLowerVersionThan(v3, true));
        }

        [TestMethod, TestCategory("Compare"), TestCategory("VersionInfo")]
        public void Should_NotIgnoreRevisionNumber_When_CompareAndNoRevIgnoreFlag()
        {
            var v1 = new VersionInfo("c", 2, 0, 10, 10);
            var v2 = new VersionInfo("c", 2, 0, 00, 00);
            var v3 = new VersionInfo("c", 2, 0, 20, 20);
            var v4 = new VersionInfo("c", 2, 0, 20, 30);
            var v5 = new VersionInfo("c", 2, 0, 30, 40);

            Assert.IsTrue(v1.HasHigherVersionThan(v2, false));
            Assert.IsFalse(v1.HasHigherVersionThan(v3, false));
            Assert.IsFalse(v1.HasLowerVersionThan(v2, false));
            Assert.IsTrue(v1.HasLowerVersionThan(v3, false));
            Assert.IsTrue(v4.HasLowerVersionThan(v5, false));
        }

        [TestMethod, TestCategory("No Null"), TestCategory("VersionInfo")]
        public void Should_NotAllowNullString_When_CreateVersionInfo()
        {
            var badNames = new String[] { "", " ", null };
            foreach (var name in badNames)
            {
                try
                {
                    new VersionInfo(name, 1, 0, 0, 0);
                    Assert.Fail($"{nameof(ArgumentNullException)} expected.");
                }
                catch (ArgumentNullException) { } // Test passed
            }
        }
    }
}
