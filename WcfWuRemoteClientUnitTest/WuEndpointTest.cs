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
using WcfWuRemoteClient.Models;
using WuDataContract.Interface;

namespace WcfWuRemoteClientUnitTest
{
    [TestClass]
    public class WuEndpointTest
    {
        readonly MockRepository MoqFactory = new MockRepository(MockBehavior.Strict) { };

        [TestMethod, TestCategory("Connect")]
        public void Should_CatchCommunicationObjectFaultedException_When_CloseEndpointConnection()
        {
            var assembly = typeof(IWuRemoteService).Assembly.GetName();

            var serviceMock = MoqFactory.Create<IWuRemoteService>(MockBehavior.Loose);
            var channelMock = serviceMock.As<IChannel>();
            channelMock.Setup(s => s.Close()).Throws<CommunicationObjectFaultedException>();
            channelMock.Setup(s => s.State).Returns(CommunicationState.Opened);
            serviceMock.Setup(s => s.GetFQDN()).Returns("test");
            var endpointMock = MoqFactory.Create<WuEndpoint.CallbackReceiver>(MockBehavior.Loose);

            var serviceFactory = MoqFactory.Create<WuRemoteServiceFactory>(MockBehavior.Loose);
            serviceFactory.Setup(f => f.GetInstance(
                It.IsAny<Binding>(),
                It.IsAny<EndpointAddress>(),
                It.IsAny<WuEndpoint.CallbackReceiver>())).Returns(serviceMock.Object);

            WuEndpoint endpoint = new WuEndpoint(serviceFactory.Object, new NetTcpBinding(), new EndpointAddress("net.tcp://test.com"));

            endpoint.Disconnect(); // Should not throw CommunicationObjectFaultedException.
            channelMock.Verify(s => s.Close(), Times.Once);
        }
    }
}
