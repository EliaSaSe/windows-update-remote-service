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
using System.Linq;
using System.Threading;
using WcfWuRemoteClient.InteractionServices;
using WcfWuRemoteClient.Models;
using WcfWuRemoteClient.ViewModels;
using WuDataContract.DTO;
using WuDataContract.Interface;

namespace WcfWuRemoteClientUnitTest
{
    [TestClass]
    public class UpdateOverviewViewModelTest
    {
        readonly MockRepository MoqFactory = new MockRepository(MockBehavior.Strict) {};


        [TestMethod]
        [TestCategory("ViewModel Datamapping")]
        public void Should_UpdateAvailableUpdatesProperty_When_RefeshAsync()
        {
            var modalmock = MoqFactory.Create<IModalService>();
            var endpointMock = MoqFactory.Create<IWuEndpoint>(MockBehavior.Loose);

            endpointMock.Setup(e => e.Updates).Returns(new[] { new UpdateDescription() { Title="update1", ID="update1" }, new UpdateDescription() { Title = "update2", ID = "update2" } });
            endpointMock.Setup(e => e.Settings).Returns(new WuSettings(0, 0, 0, false, false));

            var uovm = new UpdateOverviewViewModel(modalmock.Object, endpointMock.Object);

            Assert.IsNull(uovm.Updates);

            uovm.RefreshAsync().Wait();

            Assert.AreEqual(2, uovm.Updates.Count());
            Assert.IsNotNull(uovm.Updates.Single(u => u.ID.Equals("update1")));
            Assert.IsNotNull(uovm.Updates.Single(u => u.ID.Equals("update2")));
        }

        [TestMethod]
        [TestCategory("ViewModel Datamapping")]
        public void Should_UpdateSettingsProperties_When_RefeshAsync()
        {
            var modalmock = MoqFactory.Create<IModalService>();
            var endpointMock = MoqFactory.Create<IWuEndpoint>(MockBehavior.Loose);
            var uovm = new UpdateOverviewViewModel(modalmock.Object, endpointMock.Object);

            endpointMock.Setup(e => e.Updates).Returns(new[] {new UpdateDescription()});
            endpointMock.Setup(e => e.Settings).Returns(new WuSettings(0, 0, 0, false, true));     
            uovm.RefreshAsync().Wait();
            Assert.IsFalse(uovm.AutoAcceptEulas);
            Assert.IsTrue(uovm.AutoSelectUpdates);

            endpointMock.Setup(e => e.Settings).Returns(new WuSettings(0, 0, 0, true, false));
            uovm.RefreshAsync().Wait();
            Assert.IsTrue(uovm.AutoAcceptEulas);
            Assert.IsFalse(uovm.AutoSelectUpdates);
        }

        [TestMethod]
        [TestCategory("ViewModel Datamapping")]
        public void Should_UpdateHostnameProperty_When_RefeshAsync()
        {
            var modalmock = MoqFactory.Create<IModalService>();
            var endpointMock = MoqFactory.Create<IWuEndpoint>(MockBehavior.Loose);
            var uovm = new UpdateOverviewViewModel(modalmock.Object, endpointMock.Object);

            endpointMock.Setup(e => e.Updates).Returns(new[] { new UpdateDescription() });
            endpointMock.Setup(e => e.Settings).Returns(new WuSettings(0, 0, 0, false, true));
            endpointMock.Setup(e => e.FQDN).Returns("mock1");
            Assert.IsNull(uovm.Hostname);
            uovm.RefreshAsync().Wait();
            Assert.AreEqual("mock1", uovm.Hostname);
        }

        [TestMethod]
        [TestCategory("Events")]
        public void Should_FirePropertyChanged_When_RefeshAsyncCompleted()
        {
            var modalmock = MoqFactory.Create<IModalService>();
            var endpointMock = MoqFactory.Create<IWuEndpoint>(MockBehavior.Loose);
            var uovm = new UpdateOverviewViewModel(modalmock.Object, endpointMock.Object);

            endpointMock.Setup(e => e.Updates).Returns(new[] { new UpdateDescription() });
            endpointMock.Setup(e => e.Settings).Returns(new WuSettings(0, 0, 0, false, true));
            endpointMock.Setup(e => e.FQDN).Returns("mock1");

            List<string> propertiesToFire = new List<string>(){
                nameof(uovm.Hostname),
                nameof(uovm.AutoAcceptEulas),
                nameof(uovm.AutoSelectUpdates),
                nameof(uovm.Updates)
            };

            uovm.PropertyChanged += (s, e) => {
                if (propertiesToFire.Contains(e.PropertyName))
                {
                    propertiesToFire.Remove(e.PropertyName);
                }
                else
                {
                    Assert.Fail($"Property changed event for unexpected property {e.PropertyName} was fired.");
                }
            };

            uovm.RefreshAsync().Wait();
            Assert.IsFalse(propertiesToFire.Any());
        }

        [TestMethod]
        [TestCategory("Modal")]
        public void Should_DisplayMessageBox_When_RefeshAsyncFailed()
        {
            var modalmock = MoqFactory.Create<IModalService>(MockBehavior.Loose);
            var endpointMock = MoqFactory.Create<IWuEndpoint>(MockBehavior.Loose);
            var uovm = new UpdateOverviewViewModel(modalmock.Object, endpointMock.Object);

            endpointMock.Setup(e => e.RefreshSettingsAsync()).Throws(new Exception());
            uovm.RefreshAsync().Wait();

            modalmock.Verify(m => m.ShowMessageBox(It.IsAny<string>(), It.IsAny<string>(), MessageType.Error));
        }

        [TestMethod]
        [TestCategory("Settings")]
        public void Should_UpdateAutoAcceptEulasProperty_When_AsyncPropertyUpdateCompleted()
        {
            var modalmock = MoqFactory.Create<IModalService>();
            var endpointMock = MoqFactory.Create<IWuEndpoint>(MockBehavior.Loose);
            var serviceMock = MoqFactory.Create<IWuRemoteService>();
            serviceMock.Setup(s => s.SetAutoAcceptEulas(true)).Returns(true);
            endpointMock.Setup(e => e.Service).Returns(serviceMock.Object);

            var uovm = new UpdateOverviewViewModel(modalmock.Object, endpointMock.Object);

            string propName = nameof(uovm.AutoAcceptEulas);
            ManualResetEvent propChanged = new ManualResetEvent(false);

            uovm.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName != propName) Assert.Fail($"Update of unexpected property: {e.PropertyName}.");
                propChanged.Set();
            };

            uovm.AutoAcceptEulas = true;

            if (propChanged.WaitOne(500))
            {
                serviceMock.Verify(s => s.SetAutoAcceptEulas(true), Times.Once);
                Assert.IsTrue(uovm.AutoAcceptEulas);
            }
            else
            {
                Assert.Fail("Property changed event is missing.");
            }
        }
    }
}
