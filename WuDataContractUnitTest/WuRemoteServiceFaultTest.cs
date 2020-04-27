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
using System.Runtime.InteropServices;
using WuDataContract.Faults;

namespace WuDataContractUnitTest
{
    [TestClass]
    public class WuRemoteServiceFaultTest
    {
        class TestFault : WuRemoteServiceFault
        {
            public TestFault(Exception e) : base(e){}
        }

        [TestMethod, TestCategory("Faults")]
        public void Should_ContainExpectedFaultCode_When_CreateFault()
        {

            Exception ex = new Exception();
            ArgumentException aex = new ArgumentException();
            var comex = new COMException();

            var fault1 = InvalidStateTransitionFault.GetFault(ex);
            var fault2 = PreConditionNotFulfilledFault.GetFault(ex);
            var fault3 = BadArgumentFault.GetFault(aex);
            var fault4 = ApiFault.GetFault(comex);
            var fault5 = UpdateNotFoundFault.GetFault(ex, "update1");

            Assert.AreEqual(fault1.Code.Name, "InvalidTransition");
            Assert.AreEqual(fault2.Code.Name, "PreConditionNotFulfilled");
            Assert.AreEqual(fault3.Code.Name, "BadArgument");
            Assert.AreEqual(fault4.Code.Name, "ApiFault");
            Assert.AreEqual(fault5.Code.Name, "UpdateNotFound");
        }

        [TestMethod, TestCategory("Faults")]
        public void Should_ContainExceptionMessage_When_CreateFault()
        {
            string message = "UnitTest";
            Exception ex = new Exception(message);
            ArgumentException aex = new ArgumentException(message);
            var comex = new COMException(message);

            var fault1 = InvalidStateTransitionFault.GetFault(ex);
            var fault2 = PreConditionNotFulfilledFault.GetFault(ex);
            var fault3 = BadArgumentFault.GetFault(aex);
            var fault4 = ApiFault.GetFault(comex);
            var fault5 = UpdateNotFoundFault.GetFault(ex, "update1");

            Assert.AreEqual(fault1.Message, message);
            Assert.AreEqual(fault2.Message, message);
            Assert.AreEqual(fault3.Message, message);
            Assert.AreEqual(fault4.Message, message);
            Assert.AreEqual(fault5.Message, message);

            var fault9 = new TestFault(new Exception(message));
            Assert.AreEqual(fault9.Message, message);

        }

        [TestMethod, TestCategory("Faults"), TestCategory("No Null")]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Should_NotAllowNullException_When_CreateFault()
        {
            new TestFault(null);
        }

        [TestMethod, TestCategory("Faults")]
        public void Should_ContainHResult_When_CreateApiFault()
        {
            string message = "UnitTest";
            int code = 5555;
            var comex = new COMException(message, code);
            var fault = new ApiFault(comex);
            Assert.AreEqual(fault.HResult, code);
        }

        [TestMethod, TestCategory("Faults")]
        public void Should_ContainParameterName_When_CreateBadArgumentFault()
        {
            string message = "UnitTest";
            string param = "ParameterName";
            ArgumentException aex = new ArgumentException(message, param);
            var fault = new BadArgumentFault(aex);
            Assert.AreEqual(fault.Parameter, param);
        }

        [TestMethod, TestCategory("Faults")]
        public void Should_ContainUpdateId_When_CreateUpdateNotFoundFault()
        {
            string updateId = "updateId";
            Exception ex = new Exception();
            var fault = new UpdateNotFoundFault(ex, updateId);
            Assert.AreEqual(fault.UpdateId, updateId);
        }
    }
}
