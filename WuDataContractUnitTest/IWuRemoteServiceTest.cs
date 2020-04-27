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
using System.Linq;
using System.ServiceModel;
using WuDataContract.Interface;

namespace WuDataContractUnitTest
{
    [TestClass]
    public class IWuRemoteServiceTest
    {
        private ServiceContractAttribute GetServiceContractAttribute(Type t)
        {
            Assert.IsTrue(t.CustomAttributes.Single(a => a.AttributeType == typeof(ServiceContractAttribute)) != null, $"{t.Name} is not annotated with attribute {nameof(ServiceContractAttribute)}");
            return t.GetCustomAttributes(typeof(ServiceContractAttribute), false).Single() as ServiceContractAttribute;
        }

        [TestMethod, TestCategory("ContractConfiguration")]
        public void Find_OnlyOnewayOperations_On_CallbackContract()
        {
            var methods = typeof(IWuRemoteServiceCallback).GetMethods();
            Assert.IsTrue(methods.All(m => m.CustomAttributes.Single(a => a.AttributeType == typeof(OperationContractAttribute)) != null));
            Assert.IsTrue(methods.All(m => (m.GetCustomAttributes(typeof(OperationContractAttribute), false).Single() as OperationContractAttribute).IsOneWay));
        }

        [TestMethod, TestCategory("ContractConfiguration")]
        public void Find_CallbackContract_On_ServiceContract()
        {
            var attr = GetServiceContractAttribute(typeof(IWuRemoteService));
            Assert.AreEqual(attr.CallbackContract, typeof(IWuRemoteServiceCallback));
        }

        [TestMethod, TestCategory("ContractConfiguration"), TestCategory("Security")]
        public void Find_EnabledEncryption_On_ServiceContractRequirements()
        {
            var attr = GetServiceContractAttribute(typeof(IWuRemoteService));
            Assert.AreEqual(attr.ProtectionLevel, System.Net.Security.ProtectionLevel.EncryptAndSign);
        }

        [TestMethod, TestCategory("ContractConfiguration")]
        public void Find_OperationContractAttr_On_ServiceContractMethods()
        {
            var methods = typeof(IWuRemoteService).GetMethods();
            Assert.IsTrue(methods.All(m => m.CustomAttributes.Single(a => a.AttributeType == typeof(OperationContractAttribute)) != null));
        }
    }
}
