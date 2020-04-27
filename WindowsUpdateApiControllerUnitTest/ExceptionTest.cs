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
using WindowsUpdateApiController.Exceptions;
using WindowsUpdateApiController.States;

namespace WindowsUpdateApiControllerUnitTest
{
    [TestClass]
    public class ExceptionTest
    {
        [TestMethod, TestCategory("Exception")]
        public void Should_ContainSpecifiedValues_When_CreateInvalidStateTransitionException()
        {
            WuProcessState From = new WuStateReady(), To = new WuStateRebootRequired();
            Type FromType = typeof(WuStateReady), ToType = typeof(WuStateRebootRequired);

            List<InvalidStateTransitionException> exceptions = new List<InvalidStateTransitionException>();

            exceptions.Add(new InvalidStateTransitionException(From, To));
            exceptions.Add(new InvalidStateTransitionException(From, To, innerException: null));
            exceptions.Add(new InvalidStateTransitionException(From, To, "message"));
            exceptions.Add(new InvalidStateTransitionException(From, To, "message", null));

            foreach (var ex in exceptions)
            {
                Assert.AreSame(From, ex.From);
                Assert.AreSame(To, ex.To);
            }

            exceptions.Clear();
            exceptions.Add(new InvalidStateTransitionException(FromType, ToType));
            exceptions.Add(new InvalidStateTransitionException(FromType, ToType, innerException: null));
            exceptions.Add(new InvalidStateTransitionException(FromType, ToType, "message"));
            exceptions.Add(new InvalidStateTransitionException(FromType, ToType, "message", null));

            foreach (var ex in exceptions)
            {
                Assert.AreSame(FromType, ex.FromType);
                Assert.AreSame(ToType, ex.ToType);
            }

        }

        [TestMethod, TestCategory("Exception")]
        public void Should_ContainSpecifiedValues_When_CreatePreConNotFullfilledException()
        {
            WuProcessState From = new WuStateReady(), To = new WuStateRebootRequired();
            Type FromType = typeof(WuStateReady), ToType = typeof(WuStateRebootRequired);

            List<PreConditionNotFulfilledException> exceptions = new List<PreConditionNotFulfilledException>();

            exceptions.Add(new PreConditionNotFulfilledException(From, To, "message"));
            exceptions.Add(new PreConditionNotFulfilledException(From, To, "message", null));

            foreach (var ex in exceptions)
            {
                Assert.AreSame(From, ex.From);
                Assert.AreSame(To, ex.To);
            }

            exceptions.Clear();
            exceptions.Add(new PreConditionNotFulfilledException(FromType, ToType, "message"));
            exceptions.Add(new PreConditionNotFulfilledException(FromType, ToType, "message", null));

            foreach (var ex in exceptions)
            {
                Assert.AreSame(FromType, ex.FromType);
                Assert.AreSame(ToType, ex.ToType);
            }
        }

        [TestMethod, TestCategory("Exception")]
        public void Should_ContainSpecifiedValues_When_CreateUpdateNotFoundException()
        {
            string message = "message", updateId = "updateId";
            Exception inner = new Exception();

            List<UpdateNotFoundException> exceptions = new List<UpdateNotFoundException>();

            exceptions.Add(new UpdateNotFoundException(updateId, message));
            exceptions.Add(new UpdateNotFoundException(updateId, message, inner));

            foreach (var ex in exceptions)
            {
                Assert.AreEqual(ex.UpdateId, updateId);
                Assert.AreEqual(ex.Message, message);
            }
        }
    }
}
