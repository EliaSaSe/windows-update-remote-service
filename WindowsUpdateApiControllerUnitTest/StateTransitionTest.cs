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
    public class StateTransitionTest
    {
        [TestMethod]
        public void Should_ContainSpecifiedValues_When_CreateStateTransition()
        {
            Type s1 = typeof(WuStateReady);
            Type s2 = typeof(WuStateRebootRequired);
            StateTransition.TransitionCondition condition = (x) => { return ConditionEvalResult.ValidStateChange; };

            StateTransition t1 = new StateTransition(s1,s2,condition);

            Assert.AreEqual(s1, t1.FromState);
            Assert.AreEqual(s2, t1.ToState);
            Assert.AreSame(condition, t1.Condition);

        }


        [TestMethod]
        public void Should_BeEqual_When_CompareTransitionsWithSameFromToTypes()
        {
            StateTransition.TransitionCondition condition = (x) => { return ConditionEvalResult.ValidStateChange; };

            StateTransition t1 = new StateTransition(typeof(WuStateReady), typeof(WuStateRebootRequired));
            StateTransition t2 = new StateTransition(typeof(WuStateReady), typeof(WuStateRebootRequired), condition);
            StateTransition t3 = new StateTransition(typeof(WuStateRebootRequired), typeof(WuStateReady));
            StateTransition t4 = new StateTransition(typeof(WuStateRebootRequired), typeof(WuStateReady), condition);

            Assert.AreEqual(t1, t2);
            Assert.AreEqual(t3, t4);
            Assert.AreNotEqual(t1, t3);
            Assert.AreNotEqual(t2, t4);
        }

        [TestMethod]
        public void Should_NotAllowNonProcessStateTypes_When_CreateStateTransition()
        {
            try
            {
                new StateTransition(typeof(int), typeof(WuStateReady));
                Assert.Fail("exception expected");
            }
            catch (ArgumentException) { }

            try
            {
                new StateTransition(typeof(WuStateReady), typeof(int));
                Assert.Fail("exception expected");
            }
            catch (ArgumentException) { }

            try
            {
                new StateTransition(null, typeof(WuStateReady));
                Assert.Fail("exception expected");
            }
            catch (ArgumentNullException) { }

            try
            {
                new StateTransition(typeof(WuStateReady), null);
                Assert.Fail("exception expected");
            }
            catch (ArgumentNullException) { }
        }

        [TestMethod]
        public void Should_ReturnExpectedString_When_CallToString()
        {           
            string expect1 = "WuStateReady --> WuStateRebootRequired, condition: no";
            string expect2 = "WuStateReady --> WuStateRebootRequired, condition: yes";
            StateTransition t1 = new StateTransition(typeof(WuStateReady), typeof(WuStateRebootRequired));
            StateTransition t2 = new StateTransition(typeof(WuStateReady), typeof(WuStateRebootRequired), (x) => { return ConditionEvalResult.ValidStateChange; });
            Assert.AreEqual(expect1, t1.ToString());
            Assert.AreEqual(expect2, t2.ToString());
        }

        [TestMethod]
        public void Should_ReturnHashCode_When_CallGetHashCode()
        {
            Type FromState = typeof(WuStateReady);
            Type ToState = typeof(WuStateRebootRequired);
            int expectedHash = 17 + 31 * FromState.GetHashCode() + 31 * ToState.GetHashCode();
            StateTransition t1 = new StateTransition(FromState, ToState);
            Assert.AreEqual(expectedHash, t1.GetHashCode());
        }
    }
}
