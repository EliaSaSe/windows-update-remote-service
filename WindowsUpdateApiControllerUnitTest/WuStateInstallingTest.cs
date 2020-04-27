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
using System.Threading;
using WindowsUpdateApiController.States;
using WUApiLib;
using WuApiMocks;

namespace WindowsUpdateApiControllerUnitTest
{
    [TestClass]
    public class WuStateInstallingTest
    {

        [TestMethod, TestCategory("No Null")]
        public void Should_NotAllowNullValues_When_CreateWuStateInstalling()
        {
            IUpdateInstaller installer = new UpdateInstallerFake();
            IUpdateCollection updates = new UpdateCollectionFake();

            try
            {
                new WuStateInstalling(null, updates, (x, u) => { }, (x, y) => { }, null, 100);
                Assert.Fail("exception expected");
            }
            catch (ArgumentNullException) { }
            try
            {
                new WuStateInstalling(installer, null, (x, u) => { }, (x, y) => { }, null, 100);
                Assert.Fail("exception expected");
            }
            catch (ArgumentNullException) { }
            try
            {
                new WuStateInstalling(installer, updates, null, (x, y) => { }, null, 100);
                Assert.Fail("exception expected");
            }
            catch (ArgumentNullException) { }
        }


        [TestMethod]
        public void Should_UseGivenUpdateCollection_When_EnterWuStateInstalling()
        {
            IUpdateInstaller installer = new UpdateInstallerFake();
            IUpdateCollection updates = new UpdateCollectionFake();

            using (var state = new WuStateInstalling(installer, updates, (x, u) => { }, (x, y) => { }, null, 100))
            {
                Assert.IsNull(installer.Updates);
                state.EnterState(new WuStateReady());
                Assert.AreSame(updates, installer.Updates);
            }
        }

        [TestMethod]
        public void Should_NotEnterState_When_InstallerIsNotReady()
        {
            UpdateInstallerFake installer = new UpdateInstallerFake();
            IUpdateCollection updates = new UpdateCollectionFake();

            using (var state = new WuStateInstalling(installer, updates, (x, u) => { }, (x, y) => { }, null, 100))
            {
                installer.IsBusy = true;
                try
                {
                    state.EnterState(new WuStateReady());
                    Assert.Fail("exception expected");
                }
                catch (InvalidOperationException) { }
                finally {
                    installer.IsBusy = false;
                }
                installer.RebootRequiredBeforeInstallation = true;
                try
                {
                    state.EnterState(new WuStateReady());
                    Assert.Fail("exception expected");
                }
                catch (InvalidOperationException) { }
                finally
                {
                    installer.RebootRequiredBeforeInstallation = false;
                }
            }
        }

        [TestMethod, TestCategory("Callback")]
        public void Should_CallCompletedCallback_When_InstallingCompletes()
        {
            ManualResetEvent callbackSignal = new ManualResetEvent(false);
            IInstallationResult result = null;
            WuStateInstalling.InstallCompletedCallback callback = (x, u) => { result = x; callbackSignal.Set(); };
            IUpdateCollection updates = new UpdateCollectionFake();

            UpdateInstallerFake installer = new UpdateInstallerFake();
            installer.FakeInstallResult = CommonMocks.GetInstallationResult(OperationResultCode.orcSucceeded);

            var state = new WuStateInstalling(installer, updates, callback, (x, y) => { }, null, 100);
            state.EnterState(new WuStateReady());
            if (!callbackSignal.WaitOne(1000))
            {
                Assert.Fail($"callback was not called");
            }
            Assert.AreSame(installer.FakeInstallResult, result);
        }
    }
}
