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
using System.Collections;
using System.Collections.Generic;
using WindowsUpdateApiController.States;
using WUApiLib;

namespace WindowsUpdateApiControllerUnitTest
{
    [TestClass]
    public class WuStateFailedTest
    {
        [TestMethod]
        public void Should_ContainSpecifiedValues_When_CreateWuStateFailed()
        {
            IUpdateExceptionCollection warnings = new UpdateExceptionCollection();
            string reason = "reason";

            List<WuStateFailed> objects = new List<WuStateFailed>{
                new WuStateSearchFailed(warnings, reason),
                new WuStateDownloadFailed(warnings, reason),
                new WuStateDownloadPartiallyFailed(warnings, reason),
                new WuStateInstallFailed(warnings, reason),
                new WuStateInstallPartiallyFailed(warnings, reason)
            };

            foreach (var obj in objects)
            {
                Assert.AreEqual(reason, obj.Reason);
                Assert.IsTrue(obj.StateDesc.Contains(reason));
                Assert.AreSame(warnings, obj.Warnings);               
            }


        }

        private class UpdateExceptionCollection : IUpdateExceptionCollection
        {
            public IUpdateException this[int index]
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public int Count
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public IEnumerator GetEnumerator()
            {
                throw new NotImplementedException();
            }
        }
    }
}
