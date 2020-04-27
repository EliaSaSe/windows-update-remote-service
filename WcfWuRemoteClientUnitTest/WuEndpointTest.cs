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
using System.ServiceModel;
using System.ServiceModel.Channels;
using WcfWuRemoteClient;
using WcfWuRemoteClient.Models;
using WuDataContract.DTO;
using WuDataContract.Interface;

namespace WcfWuRemoteClientUnitTest
{
    [TestClass]
    public class WuEndpointTest
    {
        readonly MockRepository MoqFactory = new MockRepository(MockBehavior.Strict) { };
        ServiceHost _hosting = null;
        Mock<IWuRemoteService> _service = null;

        [TestInitialize]
        public void TestSetup()
        {
            var state = new StateDescription(
                WuDataContract.Enums.WuStateId.Ready,
                "mock", "mock", WuDataContract.Enums.InstallerStatus.Ready, 
                new WuEnviroment("mock", "mock", "mock", "mock", TimeSpan.MinValue, 1));
            var settings = new WuSettings(1, 1, 1, true, true);

            Castle.DynamicProxy.Generators.AttributesToAvoidReplicating.Add<ServiceContractAttribute>();
            _service = MoqFactory.Create<IWuRemoteService>(MockBehavior.Loose);

            _service.Setup(s => s.GetFQDN()).Returns("mock");
            _service.Setup(s => s.GetWuStatus()).Returns(state);
            _service.Setup(s => s.GetSettings()).Returns(settings);
            _hosting = MockServiceHostFactory.GenerateMockServiceHost(_service.Object);
            if (!(_hosting.State == CommunicationState.Created || _hosting.State == CommunicationState.Opened)) throw new Exception($"Can not setup {nameof(IWuRemoteService)} to run client tests.");
        }

        [TestCleanup]
        public void TestCleanup()
        {
            _hosting?.Close();
        }

        [TestMethod, TestCategory("Connect")]
        public void Should_DismissConnection_When_EndpointNotProvidesVersionInfo()
        {
            _service.Setup(s => s.GetServiceVersion()).Returns<VersionInfo[]>(null);

            WuEndpoint endpoint = null;
            Exception exception = null;
            WuEndpoint.TryCreateWuEndpoint(new NetTcpBinding(), new EndpointAddress("net.tcp://localhost:8524/WuRemoteService"), out endpoint, out exception);

            Assert.IsInstanceOfType(exception, typeof(EndpointNotSupportedException));
        }

        [TestMethod, TestCategory("Connect")]
        public void Should_RegconsizeUpgradeNeed_When_EndpointVersionIsOld()
        {
            var assembly = typeof(IWuRemoteService).Assembly.GetName();
            _service.Setup(s => s.GetServiceVersion()).Returns(new VersionInfo[] { new VersionInfo(assembly.Name, 0, 9, 99, 99, true) });

            WuEndpoint endpoint = null;
            Exception exception = null;
            WuEndpoint.TryCreateWuEndpoint(new NetTcpBinding(), new EndpointAddress("net.tcp://localhost:8524/WuRemoteService"), out endpoint, out exception);

            Assert.IsInstanceOfType(exception, typeof(EndpointNeedsUpgradeException));
            endpoint?.Disconnect();
        }

        [TestMethod, TestCategory("Connect")]
        public void Should_AllowConection_When_EndpointBuildVersionIsOld()
        {
            var assembly = typeof(IWuRemoteService).Assembly.GetName();
            var ver = new VersionInfo[] { new VersionInfo(assembly.Name, assembly.Version.Major, assembly.Version.Minor, assembly.Version.Build, assembly.Version.Revision-1, true) };
            _service.Setup(s => s.GetServiceVersion()).Returns(ver);

            WuEndpoint endpoint = null;
            Exception exception = null;
            WuEndpoint.TryCreateWuEndpoint(new NetTcpBinding(), new EndpointAddress("net.tcp://localhost:8524/WuRemoteService"), out endpoint, out exception);
            Assert.IsNull(exception);
        }

        [TestMethod, TestCategory("Connect")]
        public void Should_DismissConnection_When_EndpointVersionIsToNew()
        {
            var assembly = typeof(IWuRemoteService).Assembly.GetName();
            var ver = new VersionInfo[] { new VersionInfo(assembly.Name, assembly.Version.Major, assembly.Version.Minor + 1, 0, 0, true) };
            _service.Setup(s => s.GetServiceVersion()).Returns(ver);

            WuEndpoint endpoint = null;
            Exception exception = null;
            WuEndpoint.TryCreateWuEndpoint(new NetTcpBinding(), new EndpointAddress("net.tcp://localhost:8524/WuRemoteService"), out endpoint, out exception);
            Assert.IsNull(endpoint);
            Assert.IsInstanceOfType(exception,typeof(EndpointNotSupportedException));
            endpoint?.Disconnect();
        }

        [TestMethod, TestCategory("Connect")]
        public void Should_EagerLoadProperties_When_ConnectToEndpoint()
        {
            var assembly = typeof(IWuRemoteService).Assembly.GetName();
            var ver = new VersionInfo[] { new VersionInfo(assembly.Name, assembly.Version.Major, assembly.Version.Minor, 0, 0, true) };
            _service.Setup(s => s.GetServiceVersion()).Returns(ver);

            WuEndpoint endpoint = null;
            Exception exception = null;
            WuEndpoint.TryCreateWuEndpoint(new NetTcpBinding(), new EndpointAddress("net.tcp://localhost:8524/WuRemoteService"), out endpoint, out exception);

            _service.Verify(s => s.GetFQDN(), Times.Exactly(2));
            _service.Verify(s => s.GetServiceVersion(), Times.Once);
            _service.Verify(s => s.GetWuStatus(), Times.Once);
            _service.Verify(s => s.GetSettings(), Times.Once);
            _service.Verify(s => s.GetAvailableUpdates(), Times.Once);
            endpoint.Disconnect();
        }

        [TestMethod, TestCategory("Connect")]
        public void Should_CatchCommunicationObjectFaultedException_When_CloseEndointConnection()
        {
            var assembly = typeof(IWuRemoteService).Assembly.GetName();
            var ver = new VersionInfo[] { new VersionInfo(assembly.Name, assembly.Version.Major, assembly.Version.Minor, 0, 0, true) };
            _service.Setup(s => s.GetServiceVersion()).Returns(ver);

            var serviceMock = MoqFactory.Create<IWuRemoteService>(MockBehavior.Loose);
            var channelMock = serviceMock.As<IChannel>();
            channelMock.Setup(s => s.Close()).Throws<CommunicationObjectFaultedException>();
            channelMock.Setup(s => s.State).Returns(CommunicationState.Opened);
            serviceMock.Setup(s => s.GetFQDN()).Returns("test");
            var endpointMock = MoqFactory.Create<WuEndpoint.CallbackReceiver>(MockBehavior.Loose);

            WuEndpoint endpoint = new WuEndpoint(serviceMock.Object, endpointMock.Object, new NetTcpBinding(), new EndpointAddress("net.tcp://test.com"));

            endpoint.Disconnect(); // Should not throw CommunicationObjectFaultedException.
            channelMock.Verify(s => s.Close(), Times.Once);
        }


        [TestMethod, TestCategory("No Null")]
        public void Should_NotAllowNullValues_When_CreateWuEndpoint()
        {
            WuEndpoint endpoint = null;
            Exception exception = null;
            var remoteAddr = new EndpointAddress("http://test.com");
            var binding = new NetTcpBinding();

            try
            {
                WuEndpoint.TryCreateWuEndpoint(null, remoteAddr, out endpoint, out exception);
                Assert.Fail($"{nameof(ArgumentNullException)} expected.");
            }
            catch (ArgumentNullException) { }
            catch (Exception) { Assert.Fail($"{nameof(ArgumentNullException)} expected."); }

            try
            {
                WuEndpoint.TryCreateWuEndpoint(binding, null, out endpoint, out exception);
                Assert.Fail($"{nameof(ArgumentNullException)} expected.");
            }
            catch (ArgumentNullException) { }
            catch (Exception) { Assert.Fail($"{nameof(ArgumentNullException)} expected."); }
        }
    }
}
