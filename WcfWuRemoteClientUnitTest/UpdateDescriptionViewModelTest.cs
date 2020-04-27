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
using System.Threading;
using WcfWuRemoteClient.InteractionServices;
using WcfWuRemoteClient.Models;
using WcfWuRemoteClient.ViewModels;
using WuDataContract.DTO;
using WuDataContract.Interface;

namespace WcfWuRemoteClientUnitTest
{
    [TestClass]
    public class UpdateDescriptionViewModelTest
    {
        readonly MockRepository MoqFactory = new MockRepository(MockBehavior.Strict) { };

        [TestMethod, TestCategory("UpdateDescription")]
        public void Should_ContainSpecifiedValues_When_CreateUpdateDescViewModel()
        {
            var modalMock = MoqFactory.Create<IModalService>();
            var endpointMock = MoqFactory.Create<IWuEndpoint>();

            var updateDesc = new UpdateDescription();
            updateDesc.Description = "desc";
            updateDesc.ID = "id";
            updateDesc.IsImportant = true;
            updateDesc.IsDownloaded = true;
            updateDesc.IsInstalled = true;
            updateDesc.MaxByteSize = 200;
            updateDesc.MinByteSize = 100;
            updateDesc.Title = "title";
            var udvm = new UpdateDescriptionViewModel(modalMock.Object, updateDesc, endpointMock.Object);

            Assert.AreEqual(updateDesc.Description, udvm.Description);
            Assert.AreEqual(updateDesc.ID, udvm.ID);
            Assert.AreEqual(updateDesc.MaxByteSize, udvm.MaxByteSize);
            Assert.AreEqual(updateDesc.MinByteSize, udvm.MinByteSize);
            Assert.AreEqual(updateDesc.Title, udvm.Title);

            Assert.AreEqual(updateDesc.IsImportant, udvm.IsImportant);
            updateDesc.IsImportant = false;
            Assert.AreEqual(updateDesc.IsImportant, udvm.IsImportant);

            Assert.AreEqual(updateDesc.IsDownloaded, udvm.IsDownloaded);
            updateDesc.IsDownloaded = false;
            Assert.AreEqual(updateDesc.IsDownloaded, udvm.IsDownloaded);

            Assert.AreEqual(updateDesc.IsInstalled, udvm.IsInstalled);
            updateDesc.IsInstalled = false;
            Assert.AreEqual(updateDesc.IsInstalled, udvm.IsInstalled);

            var updateDesc2 = new UpdateDescription() { EulaAccepted = true, SelectedForInstallation = false };
            var udvm2 = new UpdateDescriptionViewModel(modalMock.Object, updateDesc2, endpointMock.Object);

            var updateDesc3 = new UpdateDescription() { EulaAccepted = false, SelectedForInstallation = true };
            var udvm3 = new UpdateDescriptionViewModel(modalMock.Object, updateDesc3, endpointMock.Object);

            Assert.AreEqual(updateDesc2.EulaAccepted, udvm2.EulaAccepted);
            Assert.AreEqual(updateDesc3.EulaAccepted, udvm3.EulaAccepted);
            Assert.AreEqual(updateDesc2.SelectedForInstallation, udvm2.Selected);
            Assert.AreEqual(updateDesc3.SelectedForInstallation, udvm3.Selected);
        }

        [TestMethod, TestCategory("UpdateDescription"), TestCategory("Exception"), TestCategory("AcceptEula")]
        [ExpectedException(typeof(InvalidOperationException))]
        public void Should_ThrowException_When_UnacceptEula()
        {
            var endpointMock = MoqFactory.Create<IWuEndpoint>();
            var udvm = new UpdateDescriptionViewModel(MoqFactory.Create<IModalService>().Object, new UpdateDescription(), endpointMock.Object);
            udvm.EulaAccepted = false;
        }

        private UpdateDescriptionViewModel SetupViewModelState(IWuRemoteService serviceMock = null, IModalService modalMock = null, UpdateDescription updateDesc = null)
        {
            serviceMock = (serviceMock != null) ? serviceMock : MoqFactory.Create<IWuRemoteService>(MockBehavior.Loose).Object;
            var endpointMock = MoqFactory.Create<IWuEndpoint>();
            endpointMock.Setup(e => e.Service).Returns(serviceMock);
            endpointMock.Setup(e => e.ConnectionState).Returns(System.ServiceModel.CommunicationState.Opened);
            endpointMock.Setup(e => e.IsDisposed).Returns(false);
            endpointMock.Setup(e => e.FQDN).Returns("mock");
            modalMock = (modalMock != null) ? modalMock : MoqFactory.Create<IModalService>(MockBehavior.Loose).Object;
            updateDesc = (updateDesc != null) ? updateDesc : new UpdateDescription();
            return new UpdateDescriptionViewModel(modalMock, updateDesc, endpointMock.Object);
        }

        [TestMethod, TestCategory("Select Updates"), TestCategory("Modal")]
        public void Should_DisplayDialog_When_SelectUpdateFails()
        {
            var modalMock = MoqFactory.Create<IModalService>();
            var serviceMock = MoqFactory.Create<IWuRemoteService>();
            ManualResetEvent signal = new ManualResetEvent(false);
            modalMock.Setup(m => m.ShowMessageBox(It.IsAny<string>(), It.IsAny<string>(), MessageType.Error)).Callback(() => { signal.Set(); });
            serviceMock.Setup(m => m.SelectUpdate(It.IsAny<string>())).Returns(false);

            var udvm = SetupViewModelState(serviceMock.Object, modalMock.Object, new UpdateDescription() { SelectedForInstallation = false });

            udvm.Selected = true;

            signal.WaitOne(2000);
            modalMock.Verify(m => m.ShowMessageBox(It.IsAny<string>(), It.IsAny<string>(), MessageType.Error), Times.Once);
            Assert.IsFalse(udvm.Selected);
        }

        [TestMethod, TestCategory("Select Updates")]
        public void Should_UpdateSelectProperty_When_SelectUpdate()
        {
            ManualResetEvent backendCalled = new ManualResetEvent(false); // wait for delayed async task which is communicating with the endpoint 
            var serviceMock = MoqFactory.Create<IWuRemoteService>();
            serviceMock.Setup(m => m.SelectUpdate(It.IsAny<string>())).Returns(true).Callback(() => backendCalled.Set());
            var udvm = SetupViewModelState(serviceMock.Object, null, new UpdateDescription() { SelectedForInstallation = false });

            udvm.Selected = true;

            if (backendCalled.WaitOne(2000))
            {
                Assert.IsTrue(udvm.Selected);
            }
            else
            {
                Assert.Fail("The backend service was not called to select the update.");
            }
        }

        [TestMethod, TestCategory("Select Updates")]
        public void Should_UpdateSelectProperty_When_UnselectUpdate()
        {
            ManualResetEvent backendCalled = new ManualResetEvent(false); // wait for delayed async task which is communicating with the endpoint  
            var serviceMock = MoqFactory.Create<IWuRemoteService>();
            serviceMock.Setup(m => m.UnselectUpdate(It.IsAny<string>())).Returns(true).Callback(() => backendCalled.Set());

            var udvm = SetupViewModelState(serviceMock.Object, null, new UpdateDescription() { SelectedForInstallation = true });

            udvm.Selected = false;

            if (backendCalled.WaitOne(2000))
            {
                Assert.IsFalse(udvm.Selected);
            }
            else
            {
                Assert.Fail("The backend service was not called to unselect the update.");
            }
        }

        [TestMethod, TestCategory("Select Updates")]
        public void Should_UseCorrectUpdateId_When_ToggleUpdateSelection()
        {
            ManualResetEvent backendCalled = new ManualResetEvent(false); // wait for delayed async task which is communicating with the endpoint  
            var updateDesc = new UpdateDescription() { SelectedForInstallation = false, ID = "test12345" };
            var serviceMock = MoqFactory.Create<IWuRemoteService>();
            serviceMock.Setup(m => m.SelectUpdate(It.Is<string>(s => s.Equals("test12345")))).Returns(true).Callback(()=> backendCalled.Set());
            var udvm = SetupViewModelState(serviceMock.Object, null, updateDesc);

            udvm.Selected = true;

            if (backendCalled.WaitOne(2000))
            {
                serviceMock.Verify(m => m.SelectUpdate(It.Is<string>(s => s.Equals("test12345"))));
            }
            else
            {
                Assert.Fail("The backend service was not called to change selection");
            }
        }

        [TestMethod, TestCategory("AcceptEula")]
        public void Should_DisplayDialog_When_AcceptEulaFails()
        {
            var modalMock = MoqFactory.Create<IModalService>();
            var serviceMock = MoqFactory.Create<IWuRemoteService>();
            ManualResetEvent signal = new ManualResetEvent(false);
            modalMock.Setup(m => m.ShowMessageBox(It.IsAny<string>(), It.IsAny<string>(), MessageType.Error)).Callback(() => { signal.Set(); });
            serviceMock.Setup(m => m.AcceptEula(It.IsAny<string>())).Returns(false);

            var udvm = SetupViewModelState(serviceMock.Object, modalMock.Object, new UpdateDescription() { EulaAccepted = false });

            udvm.EulaAccepted = true;

            signal.WaitOne(2000);
            modalMock.Verify(m => m.ShowMessageBox(It.IsAny<string>(), It.IsAny<string>(), MessageType.Error));
            Assert.IsFalse(udvm.EulaAccepted);
        }

        [TestMethod, TestCategory("AcceptEula")]
        public void Should_UpdateEulaProperty_When_AcceptEula()
        {
            ManualResetEvent backendCalled = new ManualResetEvent(false); // wait for delayed async task which is communicating with the endpoint 
            var serviceMock = MoqFactory.Create<IWuRemoteService>();
            serviceMock.Setup(m => m.AcceptEula(It.IsAny<string>())).Returns(true).Callback(() => backendCalled.Set());
            var udvm = SetupViewModelState(serviceMock.Object, null, new UpdateDescription() { EulaAccepted = false });

            udvm.EulaAccepted = true;

            if (backendCalled.WaitOne(2000))
            {
                Assert.IsTrue(udvm.EulaAccepted);
            }
            else
            {
                Assert.Fail("The backend service was not called to accept the eula.");
            }
        }

        [TestMethod, TestCategory("AcceptEula")]
        public void Should_UseCorrectUpdateId_When_AcceptEula()
        {
            ManualResetEvent backendCalled = new ManualResetEvent(false); // wait for delayed async task which is communicating with the endpoint
            var serviceMock = MoqFactory.Create<IWuRemoteService>();
            serviceMock.Setup(m => m.AcceptEula(It.IsAny<string>())).Returns(true).Callback(() => backendCalled.Set());

            var updateDesc = new UpdateDescription() { EulaAccepted = false, ID = "test12345" };
            var udvm = SetupViewModelState(serviceMock.Object, null, updateDesc);

            udvm.EulaAccepted = true;

            if (backendCalled.WaitOne(2000))
            {
                serviceMock.Verify(m => m.AcceptEula(It.Is<string>((s) => s.Equals("test12345"))), Times.Once);
            }
            else
            {
                Assert.Fail("The backend service was not called to accept the eula");
            }
        }

    }
}
