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
using Moq;
using System;
using System.Linq;
using System.ServiceModel;
using System.Threading.Tasks;
using WcfWuRemoteClient.Models;
using WcfWuRemoteClient.ViewModels;
using WcfWuRemoteService;
using WcfWuRemoteService.Helper;
using WindowsUpdateApiController;
using WuApiMocks;
using WuDataContract.Interface;

namespace WcfWuRemoteClientUnitTest
{
    [TestClass]
    public class AddHostViewModelTest
    {
        static ServiceHost _hosting;

        [ClassInitialize]
        public static void ClassSetup(TestContext context)
        {
            var simulator = new WuApiSimulator().SetSearchTime(1).SetDownloadTime(1).SetInstallTime(1).Configure();
            var factory = new Mock<WuApiControllerFactory>();
            factory.Setup(f => f.GetController()).Returns(new WuApiController(simulator.UpdateSession, simulator.UpdateCollectionFactory, CommonMocks.GetSystemInfo()));

            var service = new WuRemoteService(factory.Object, new OperationContextProvider(), new WuApiConfigProvider());
            _hosting = new ServiceHost(service);
            _hosting.AddServiceEndpoint(typeof(IWuRemoteService), new NetTcpBinding(), "net.tcp://0.0.0.0:8523/WuRemoteService");
            _hosting.Open();
            if (!(_hosting.State == CommunicationState.Created || _hosting.State == CommunicationState.Opened)) throw new Exception($"Can not setup {nameof(WuRemoteService)} to run client tests.");
        }

        [ClassCleanup]
        public static void ClassCleanup()
        {
            _hosting?.Close();
        }


        /// <summary>
        /// Waits for the Task to complete execution within a specified number of milliseconds. Re-throws thrown expections in the task.
        /// </summary>
        /// <typeparam name="T">Type of the task result.</typeparam>
        /// <param name="task">The task to wait for.</param>
        /// <param name="milliseconds">Milliseconds to wait before a <see cref="TimeoutException"/> will be thrown. A longer running task will not be canceled.</param>
        /// <returns>The result of the task.</returns>
        /// <exception cref="TimeoutException">Thrown if the task needs longer than <paramref name="milliseconds"/> to complete.</exception> 
        private T WaitForTaskAndThrow<T>(Task<T> task, int milliseconds)
        {
            if (task.Status == TaskStatus.Created) task.Start();
            try
            {
                if (task.Wait(milliseconds))
                {
                    if (task.Exception != null)
                    {
                        if (task.Exception.InnerExceptions.Count == 1) throw task.Exception.InnerExceptions.First();
                        throw task.Exception;
                    }
                    return task.Result;
                }
            }
            catch(AggregateException e)
            {
                if (e.InnerExceptions.Count == 1) throw e.InnerExceptions.First();
                throw;
            }
            throw new TimeoutException("Task needs to long to complete.");
        }

        [TestMethod, TestCategory("Connect")]
        public void Should_ConnectToEndpointWithDefaultSettings_When_OnlyHostnameGiven()
        {
            var result = WaitForTaskAndThrow(AddHostViewModel.ConnectToHosts(new WuEndpointCollection(), "localhost"), 5000);

            Assert.IsNotNull(result);
            Assert.IsTrue(result.Count() == 1);
            Assert.IsTrue(result.First().Success);
        }

        [TestMethod, TestCategory("Connect")]
        public void Should_IndicateFailure_When_FailedToConnect()
        {
            var result = WaitForTaskAndThrow(AddHostViewModel.ConnectToHosts(new WuEndpointCollection(), "test"), 5000);

            Assert.IsNotNull(result);
            Assert.IsTrue(result.Count() == 1);
            Assert.IsFalse(result.First().Success);
            Assert.IsNotNull(result.First().Exception);
        }

        [TestMethod, TestCategory("Connect")]
        public void Should_IgnoreEmptyLines_When_ConnectToHosts()
        {
            var result = WaitForTaskAndThrow(AddHostViewModel.ConnectToHosts(new WuEndpointCollection(), $"test1{Environment.NewLine}{Environment.NewLine}{Environment.NewLine}test2"), 5000);

            Assert.IsNotNull(result);
            Assert.IsTrue(result.Count() == 2);
            Assert.IsTrue(result.All(r => !r.Success));
        }

        [TestMethod, TestCategory("Connect")]
        public void Should_DismissConnection_When_ConnectToAlreadyConntectedHost()
        {
            var endpointCol = new WuEndpointCollection();
            var result1 = WaitForTaskAndThrow(AddHostViewModel.ConnectToHosts(endpointCol, $"localhost{Environment.NewLine}localhost"), 5000);
            var result2 = WaitForTaskAndThrow(AddHostViewModel.ConnectToHosts(endpointCol, $"localhost"), 5000);

            Assert.IsTrue(result1.Count() == 1);
            Assert.IsTrue(result2.Count() == 0);
            Assert.IsTrue(endpointCol.Count == 1);          
        }

        [TestMethod, TestCategory("Connect")]
        public void Should_OnlyUseConnectedEndpoints_When_TestForAlreadyConntectedHost()
        {
            var result1 = WaitForTaskAndThrow(AddHostViewModel.ConnectToHosts(new WuEndpointCollection(), $"test1{Environment.NewLine}localhost"), 5000);
            Assert.IsTrue(result1.Count(e => e.Success)==1);
        }

        [TestMethod, TestCategory("Connect")]
        public void Should_ContainSpecifiedUrl_When_CreateAddHostViewModel()
        {
            var url = "//net.tcp://test";
            var result = WaitForTaskAndThrow(AddHostViewModel.ConnectToHosts(new WuEndpointCollection(), url), 2000);
            Assert.AreEqual(result.First().Url, url);
        }

        [TestMethod, TestCategory("Connect"), TestCategory("No Null")]
        public void Should_ThrowException_When_InvalidUrlGiven()
        {
            var result = WaitForTaskAndThrow(AddHostViewModel.ConnectToHosts(new WuEndpointCollection(), "//#?net.tcp://localhost:8523/WuRemoteService"), 2000);
            Assert.IsFalse(result.First().Success);
            Assert.IsTrue(result.First().Exception is ArgumentException);
        }

        [TestMethod, TestCategory("Connect"), TestCategory("No Null")]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Should_NotAllowOnlyWhiteSpaces_When_ConnectToHosts()
        {
            WaitForTaskAndThrow(AddHostViewModel.ConnectToHosts(new WuEndpointCollection(), $"   {Environment.NewLine} {Environment.NewLine}"), 2000);
        }

        [TestMethod, TestCategory("Connect"), TestCategory("No Null")]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Should_NotAllowNullString_When_ConnectToHosts()
        {
            WaitForTaskAndThrow(AddHostViewModel.ConnectToHosts(new WuEndpointCollection(), null), 2000);
        }

    }
}
