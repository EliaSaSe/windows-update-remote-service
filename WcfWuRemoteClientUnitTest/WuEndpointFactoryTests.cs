using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Channels;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using WcfWuRemoteClient;
using WcfWuRemoteClient.Models;
using WuDataContract.DTO;
using WuDataContract.Interface;

namespace WcfWuRemoteClientUnitTest
{
    [TestClass]
    public class WuEndpointFactoryTests
    {
        readonly MockRepository MoqFactory = new MockRepository(MockBehavior.Strict) { };

        Mock<IWuRemoteService> _service;
        WuRemoteServiceFactory _serviceFactory;

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

            _service.As<IChannel>();

            var serviceFactory = MoqFactory.Create<WuRemoteServiceFactory>(MockBehavior.Loose);
            serviceFactory.Setup(f => f.GetInstance(
                It.IsAny<Binding>(),
                It.IsAny<EndpointAddress>(),
                It.IsAny<WuEndpoint.CallbackReceiver>())).Returns(_service.Object);
            _serviceFactory = serviceFactory.Object;
        }

        [TestMethod, TestCategory("Connect")]
        public void Should_DismissConnection_When_EndpointNotProvidesVersionInfo()
        {
            _service.Setup(s => s.GetServiceVersion()).Returns<VersionInfo[]>(null);

            var factory = new WuEndpointFactory(_serviceFactory);

            factory.TryCreateWuEndpoint(
                new NetTcpBinding(), 
                new EndpointAddress("net.tcp://any"), 
                out IWuEndpoint endpoint, 
                out Exception exception);

            Assert.IsInstanceOfType(exception, typeof(EndpointNotSupportedException));
        }

        [TestMethod, TestCategory("Connect")]
        public void Should_RegconsizeUpgradeNeed_When_EndpointVersionIsOld()
        {
            var assembly = typeof(IWuRemoteService).Assembly.GetName();
            _service.Setup(s => s.GetServiceVersion()).Returns(new VersionInfo[] { new VersionInfo(assembly.Name, 0, 9, 99, 99, true) });

            new WuEndpointFactory(_serviceFactory).TryCreateWuEndpoint(
                new NetTcpBinding(),
                new EndpointAddress("net.tcp://any"),
                out IWuEndpoint endpoint,
                out Exception exception);

            Assert.IsInstanceOfType(exception, typeof(EndpointNeedsUpgradeException));
            endpoint?.Disconnect();
        }

        [TestMethod, TestCategory("Connect")]
        public void Should_AllowConection_When_EndpointBuildVersionIsOld()
        {
            var assembly = typeof(IWuRemoteService).Assembly.GetName();
            var ver = new VersionInfo[] { new VersionInfo(assembly.Name, assembly.Version.Major, assembly.Version.Minor, assembly.Version.Build, assembly.Version.Revision - 1, true) };
            _service.Setup(s => s.GetServiceVersion()).Returns(ver);

            new WuEndpointFactory(_serviceFactory).TryCreateWuEndpoint(
                new NetTcpBinding(), 
                new EndpointAddress("net.tcp://any"), 
                out IWuEndpoint endpoint, 
                out Exception exception);
            Assert.IsNull(exception);
        }

        [TestMethod, TestCategory("Connect")]
        public void Should_DismissConnection_When_EndpointVersionIsToNew()
        {
            var assembly = typeof(IWuRemoteService).Assembly.GetName();
            var ver = new VersionInfo[] { new VersionInfo(assembly.Name, assembly.Version.Major, assembly.Version.Minor + 1, 0, 0, true) };
            _service.Setup(s => s.GetServiceVersion()).Returns(ver);

            new WuEndpointFactory(_serviceFactory).TryCreateWuEndpoint(
                new NetTcpBinding(),
                new EndpointAddress("net.tcp://localhost:8524/WuRemoteService"),
                out IWuEndpoint endpoint,
                out Exception exception);

            Assert.IsNull(endpoint);
            Assert.IsInstanceOfType(exception, typeof(EndpointNotSupportedException));
            endpoint?.Disconnect();
        }

        [TestMethod, TestCategory("Connect")]
        public void Should_EagerLoadProperties_When_ConnectToEndpoint()
        {
            var assembly = typeof(IWuRemoteService).Assembly.GetName();
            var ver = new VersionInfo[] { new VersionInfo(assembly.Name, assembly.Version.Major, assembly.Version.Minor, 0, 0, true) };
            _service.Setup(s => s.GetServiceVersion()).Returns(ver);

            new WuEndpointFactory(_serviceFactory)
                .TryCreateWuEndpoint(
                new NetTcpBinding(),
                new EndpointAddress("net.tcp://localhost:8524/WuRemoteService"),
                out IWuEndpoint endpoint,
                out Exception exception);

            _service.Verify(s => s.GetFQDN(), Times.Exactly(2));
            _service.Verify(s => s.GetServiceVersion(), Times.Once);
            _service.Verify(s => s.GetWuStatus(), Times.Once);
            _service.Verify(s => s.GetSettings(), Times.Once);
            _service.Verify(s => s.GetAvailableUpdates(), Times.Once);
            endpoint.Disconnect();
        }

        [TestMethod, TestCategory("No Null")]
        public void Should_NotAllowNullBinding_When_CreateWuEndpoint()
        {
            try
            {
                new WuEndpointFactory(_serviceFactory)
                    .TryCreateWuEndpoint(new NetTcpBinding(), null, out _, out _);
                Assert.Fail($"{nameof(ArgumentNullException)} expected.");
            }
            catch (ArgumentNullException) { }
            catch (Exception) { Assert.Fail($"{nameof(ArgumentNullException)} expected."); }
        }

        [TestMethod, TestCategory("No Null")]
        public void Should_NotAllowNullRemoteAddress_When_CreateWuEndpoint()
        {
            try
            {
                new WuEndpointFactory(_serviceFactory)
                    .TryCreateWuEndpoint(null, new EndpointAddress("http://test.com"), out _, out _);
                Assert.Fail($"{nameof(ArgumentNullException)} expected.");
            }
            catch (ArgumentNullException) { }
            catch (Exception) { Assert.Fail($"{nameof(ArgumentNullException)} expected."); }
        }
    }
}
