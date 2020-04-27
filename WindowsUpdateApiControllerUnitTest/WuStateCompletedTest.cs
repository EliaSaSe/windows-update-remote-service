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
using WindowsUpdateApiController.States;
using WUApiLib;
using WuApiMocks;

namespace WindowsUpdateApiControllerUnitTest
{
    [TestClass]
    public class WuStateCompletedTest
    {

        [TestMethod, TestCategory("No Null")]
        public void Should_NotAcceptNullUpdateCollection_When_CreateWuStateCompleted()
        {
            try {
                new WuStateSearchCompleted(null);
                Assert.Fail("exception expected");
            }
            catch (ArgumentNullException) { }
            try
            {
                new WuStateDownloadCompleted(null);
                Assert.Fail("exception expected");
            }
            catch (ArgumentNullException) { }
            try
            {
                new WuStateInstallCompleted(null);
                Assert.Fail("exception expected");
            }
            catch (ArgumentNullException) { }
        }

        [TestMethod]
        public void Should_ContainSpecifiedValues_When_CreateWuStateCompleted()
        {
            IUpdateCollection collection = new UpdateCollectionFake();
            int hresult = 2;

            var sc = new WuStateSearchCompleted(collection, hresult);
            var dc = new WuStateDownloadCompleted(collection, hresult);
            var ic = new WuStateInstallCompleted(collection, hresult);

            Assert.AreSame(collection, sc.Updates);
            Assert.AreEqual(hresult, sc.HResult);
            Assert.AreSame(collection, dc.Updates);
            Assert.AreEqual(hresult, dc.HResult);
            Assert.AreSame(collection, ic.Updates);
            Assert.AreEqual(hresult, ic.HResult);

        }
    }
}
