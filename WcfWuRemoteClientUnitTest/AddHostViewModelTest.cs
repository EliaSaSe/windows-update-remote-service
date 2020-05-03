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
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Threading.Tasks;
using WcfWuRemoteClient.Models;
using WcfWuRemoteClient.ViewModels;

namespace WcfWuRemoteClientUnitTest
{
    [TestClass]
    public class AddHostViewModelTest
    {
        delegate bool TryCreateWuEndpointCallback(
            Binding binding, 
            EndpointAddress address,
            out IWuEndpoint endpoint, 
            out Exception ex);

        private class WuEndpointFactoryMock : WuEndpointFactory
        {
            readonly Exception _exception;
            readonly IEnumerable<IWuEndpoint> _endpoint;
            readonly bool _result;
            IEnumerator<IWuEndpoint> _enumerator;
            readonly object _lock = new object();

            public WuEndpointFactoryMock(IWuEndpoint endpoint, Exception exception, bool result)
            {
                _endpoint = new[] { endpoint };
                _exception = exception;
                _result = result;
            }

            public WuEndpointFactoryMock(Exception exception, bool result, params IWuEndpoint[] endpoints)
            {
                _endpoint = endpoints;
                _exception = exception;
                _result = result;
            }

            public override bool TryCreateWuEndpoint(Binding binding, EndpointAddress remoteAddress, out IWuEndpoint endpoint, out Exception exception)
            {
                lock (_lock)
                {
                    if (_enumerator == null) _enumerator = _endpoint?.GetEnumerator();
                    _enumerator?.MoveNext();

                    endpoint = _enumerator?.Current ?? null;
                    exception = _exception;
                    return _result;
                }
            }
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
            catch (AggregateException e)
            {
                if (e.InnerExceptions.Count == 1) throw e.InnerExceptions.First();
                throw;
            }
            throw new TimeoutException("Task needs to long to complete.");
        }

        [TestMethod, TestCategory("Connect")]
        public void Should_ConnectToEndpointWithDefaultSettings_When_OnlyHostnameGiven()
        {
            var wuEndpoint = new Mock<IWuEndpoint>();
            wuEndpoint.Setup(e => e.ConnectionState).Returns(CommunicationState.Created);

            var result = WaitForTaskAndThrow(AddHostViewModel.ConnectToHosts(
                new WuEndpointFactoryMock(wuEndpoint.Object, null, true),
                new WuEndpointCollection(),
                "localhost"), 5000);

            Assert.IsNotNull(result);
            Assert.IsTrue(result.Count() == 1);
            Assert.IsTrue(result.First().Success);
        }

        [TestMethod, TestCategory("Connect")]
        public void Should_IndicateFailure_When_FailedToConnect()
        {
            var result = WaitForTaskAndThrow(AddHostViewModel.ConnectToHosts(
                new WuEndpointFactoryMock(null, new Exception("mock"), false),
                new WuEndpointCollection(),
                "test"), 5000);

            Assert.IsNotNull(result);
            Assert.IsTrue(result.Count() == 1);
            Assert.IsFalse(result.First().Success);
            Assert.IsNotNull(result.First().Exception);
        }

        [TestMethod, TestCategory("Connect")]
        public void Should_IgnoreEmptyLines_When_ConnectToHosts()
        {
            var wuEndpoint1 = new Mock<IWuEndpoint>();
            wuEndpoint1.Setup(e => e.ConnectionState).Returns(CommunicationState.Created);
            wuEndpoint1.Setup(e => e.FQDN).Returns("s1");
            var wuEndpoint2 = new Mock<IWuEndpoint>();
            wuEndpoint2.Setup(e => e.ConnectionState).Returns(CommunicationState.Created);
            wuEndpoint2.Setup(e => e.FQDN).Returns("s2");

            var result = WaitForTaskAndThrow(AddHostViewModel.ConnectToHosts(
                new WuEndpointFactoryMock(null, true, wuEndpoint1.Object, wuEndpoint2.Object),
                new WuEndpointCollection(),
                $"test1{Environment.NewLine}{Environment.NewLine}{Environment.NewLine}test2"),
                5000);

            Trace.WriteLine(result.First().Url);

            Assert.IsNotNull(result);
            Assert.IsTrue(result.Count() == 2);
            Assert.IsTrue(result.All(r => r.Success));
        }

        [TestMethod, TestCategory("Connect")]
        public void Should_DismissConnection_When_ConnectToAlreadyConntectedHost()
        {
            var wuEndpoint = new Mock<IWuEndpoint>();
            wuEndpoint.Setup(e => e.ConnectionState).Returns(CommunicationState.Created);
            wuEndpoint.Setup(e => e.FQDN).Returns("samename");

            var endpointCol = new WuEndpointCollection();

            var result1 = WaitForTaskAndThrow(AddHostViewModel.ConnectToHosts(
                new WuEndpointFactoryMock(null, true, wuEndpoint.Object, wuEndpoint.Object),
                endpointCol,
                $"samename{Environment.NewLine}samename"), 5000);

            var result2 = WaitForTaskAndThrow(AddHostViewModel.ConnectToHosts(
                new WuEndpointFactoryMock(null, true, wuEndpoint.Object),
                endpointCol,
                $"samename"), 5000);

            Assert.IsTrue(result1.Count() == 1);
            Assert.IsTrue(result2.Count() == 0);
            Assert.IsTrue(endpointCol.Count == 1);
        }

        [TestMethod, TestCategory("Connect")]
        public void Should_OnlyUseConnectedEndpoints_When_TestForAlreadyConntectedHost()
        {
            var endpointFactory = new Mock<WuEndpointFactory>();

            var wuEndpoint1 = new Mock<IWuEndpoint>();
            wuEndpoint1.Setup(e => e.ConnectionState).Returns(CommunicationState.Created);
            wuEndpoint1.Setup(e => e.FQDN).Returns("t1");

            endpointFactory.Setup(m => m.TryCreateWuEndpoint(
                It.IsAny<Binding>(),
                It.Is<EndpointAddress>(e => e.Uri.Host.Equals($"t1")),
                out It.Ref<IWuEndpoint>.IsAny,
                out It.Ref<Exception>.IsAny
                )).Returns(new TryCreateWuEndpointCallback((Binding binding, EndpointAddress endpointAddress, 
                    out IWuEndpoint enpoint, out Exception ex) =>
                {
                    enpoint = wuEndpoint1.Object;
                    ex = null;
                    return true;
                }));

            endpointFactory.Setup(m => m.TryCreateWuEndpoint(
                It.IsAny<Binding>(),
                It.Is<EndpointAddress>(e => e.Uri.Host.Equals($"t2")),
                out It.Ref<IWuEndpoint>.IsAny,
                out It.Ref<Exception>.IsAny
                )).Returns(new TryCreateWuEndpointCallback((Binding binding, EndpointAddress endpointAddress,
                    out IWuEndpoint enpoint, out Exception ex) =>
                {
                    enpoint = null;
                    ex = new Exception("mock");
                    return false;
                }));

            var result1 = WaitForTaskAndThrow(AddHostViewModel.ConnectToHosts(
                endpointFactory.Object,
                new WuEndpointCollection(),
                $"t1{Environment.NewLine}t2"),
                5000000);

            Assert.IsTrue(result1.Count(e => e.Success) == 1);
        }

        [TestMethod, TestCategory("Connect")]
        public void Should_ContainSpecifiedUrl_When_CreateAddHostViewModel()
        {
            var url = "//net.tcp://test";

            var result = WaitForTaskAndThrow(AddHostViewModel.ConnectToHosts(
                new Mock<WuEndpointFactory>().Object,
                new WuEndpointCollection(),
                url),
                2000);

            Assert.AreEqual(result.First().Url, url);
        }

        [TestMethod, TestCategory("Connect"), TestCategory("No Null")]
        public void Should_ThrowException_When_InvalidUrlGiven()
        {
            var result = WaitForTaskAndThrow(AddHostViewModel.ConnectToHosts(
                new Mock<WuEndpointFactory>().Object, 
                new WuEndpointCollection(), 
                "//#?net.tcp://localhost:8523/WuRemoteService"), 2000);

            Assert.IsFalse(result.First().Success);
            Assert.IsTrue(result.First().Exception is ArgumentException);
        }

        [TestMethod, TestCategory("Connect"), TestCategory("No Null")]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Should_NotAllowOnlyWhiteSpaces_When_ConnectToHosts()
        {
            WaitForTaskAndThrow(AddHostViewModel.ConnectToHosts(
                new Mock<WuEndpointFactory>().Object, 
                new WuEndpointCollection(), 
                $"   {Environment.NewLine} {Environment.NewLine}"), 
                2000);
        }

        [TestMethod, TestCategory("Connect"), TestCategory("No Null")]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Should_NotAllowNullString_When_ConnectToHosts()
        {
            WaitForTaskAndThrow(AddHostViewModel.ConnectToHosts(
                new WuEndpointFactory(), 
                new WuEndpointCollection(), null), 2000);
        }

    }
}
