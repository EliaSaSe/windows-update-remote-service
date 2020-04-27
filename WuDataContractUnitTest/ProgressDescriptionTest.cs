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
    public class ProgressDescriptionTest
    {
        [TestMethod]
        public void Should_ContainSpecifiedValues_When_CreateProgressDescription()
        {
            UpdateDescription ud = new UpdateDescription();
            ud.Title = "title";
            ProgressDescription pd1 = new ProgressDescription(ud, 0, 1, 50);
            ProgressDescription pd2 = new ProgressDescription(ud);
            ProgressDescription pd3 = new ProgressDescription();

            Assert.AreSame(pd1.CurrentUpdate, ud);
            Assert.AreSame(pd2.CurrentUpdate, ud);
            Assert.IsNull(pd3.CurrentUpdate);

            Assert.AreEqual(pd1.CurrentIndex, 0);
            Assert.IsNull(pd2.CurrentIndex);
            Assert.IsNull(pd3.CurrentIndex);

            Assert.AreEqual(pd1.Count, 1);
            Assert.IsNull(pd2.Count);          
            Assert.IsNull(pd3.Count);

            Assert.IsFalse(pd1.IsIndeterminate);
            Assert.IsTrue(pd2.IsIndeterminate);
            Assert.IsTrue(pd3.IsIndeterminate);

            Assert.IsTrue(pd1.ToString().Contains(ud.Title));
            Assert.IsFalse(String.IsNullOrWhiteSpace(pd2.ToString()));
            Assert.IsFalse(String.IsNullOrWhiteSpace(pd3.ToString()));

            Assert.AreEqual(pd1.Percent, 50);

        }

        [TestMethod]
        public void Should_NotAllowInvalidCount_When_CreateProgressDescription()
        {
            try {
                ProgressDescription pd = new ProgressDescription(null, 0, -1, 0);
                Assert.Fail("exception expected");
            }
            catch (IndexOutOfRangeException) { }

            try
            {
                ProgressDescription pd = new ProgressDescription(null, 0, 0, 0);
                Assert.Fail("exception expected");
            }
            catch (IndexOutOfRangeException) { }
        }

        [TestMethod]
        public void Should_NotAllowInvalidCurrentIndex_When_CreateProgressDescription()
        {
            try
            {
                ProgressDescription pd = new ProgressDescription(null, -1, 1, 0);
                Assert.Fail("exception expected");
            }
            catch (IndexOutOfRangeException) { }

            try
            {
                ProgressDescription pd = new ProgressDescription(null, 1, 1, 0);
                Assert.Fail("exception expected");
            }
            catch (IndexOutOfRangeException) { }

            try
            {
                ProgressDescription pd = new ProgressDescription(null, 2, 1, 0);
                Assert.Fail("exception expected");
            }
            catch (IndexOutOfRangeException) { }
        }

        [TestMethod]
        public void Should_NotAllowInvalidPercentValue_When_CreateProgressDescription()
        {
            new ProgressDescription(null, 0, 1, 0); // 0, ok
            new ProgressDescription(null, 0, 1, 100); // 100, ok

            try
            {
                ProgressDescription pd = new ProgressDescription(null, 0, 1, -1); // -1, not valid
                Assert.Fail("exception expected");
            }
            catch (IndexOutOfRangeException) { }

            try
            {
                ProgressDescription pd = new ProgressDescription(null, 0, 1, 101); // 101, not valid
                Assert.Fail("exception expected");
            }
            catch (IndexOutOfRangeException) { }
        }
    }
}
