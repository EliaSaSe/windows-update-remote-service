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

namespace WindowsUpdateApiControllerUnitTest
{
    [TestClass]
    public class StateTransitionCollectionTest
    {
        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void Should_NotAllowEqualTransitions_When_AddToTransitionList()
        {
            StateTransitionCollection coll = new StateTransitionCollection();
            coll.Add<WuStateReady, WuStateDownloading>();
            coll.Add<WuStateReady, WuStateDownloading>((c) => { return new ConditionEvalResult(true, null); });
        }

        [TestMethod]
        public void Should_ReturnFalse_When_CallIsReadOnly()
        {
            StateTransitionCollection coll = new StateTransitionCollection();
            Assert.IsFalse(coll.IsReadOnly);
        }
    }
}
