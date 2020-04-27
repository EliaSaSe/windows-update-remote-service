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
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.ServiceModel;
using System.Threading;
using WcfWuRemoteService;
using WcfWuRemoteService.Helper;
using WindowsUpdateApiController;
using WindowsUpdateApiController.EventArguments;
using WindowsUpdateApiController.Exceptions;
using WuDataContract.DTO;
using WuDataContract.Enums;
using WuDataContract.Interface;

namespace WcfWuRemoteServiceUnitTest
{
    [TestClass]
    public class WuRemoteServiceTest
    {
        readonly MockRepository MoqFactory = new MockRepository(MockBehavior.Strict) { };
        private IPrincipal _originalPrincipal;
        readonly OperationContextProvider OpsProvider = new OperationContextProvider();


        [TestCleanup]
        public void ResetOriginalPrincipal()
        {
            Thread.CurrentPrincipal = _originalPrincipal;
        }

        [TestInitialize]
        public void SetupPrincipal()
        {
            _originalPrincipal = Thread.CurrentPrincipal;
            WindowsPrincipal currentWindowsUser = new WindowsPrincipal(WindowsIdentity.GetCurrent());
            var requiredIdentifier = new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, null).ToString();
            if (!currentWindowsUser.Claims.Any(c => c.Value.Equals(requiredIdentifier)))
            {
                Assert.Fail($"test user {currentWindowsUser.Identity.Name} requires local administrator rights to execute methods with {nameof(AdministratorPrincipalRequiredAttribute)} attribute");
            }
            Thread.CurrentPrincipal = currentWindowsUser;
            Assert.IsTrue(Thread.CurrentPrincipal.Identity.IsAuthenticated, "Could not setup test principal.");
        }

        private IWuApiConfigProvider GetConfigProvider()
        {
            var mock = MoqFactory.Create<IWuApiConfigProvider>();
            mock.Setup(m => m.Save());
            mock.Setup(m => m.Dispose());
            mock.SetupAllProperties();
            mock.Object.AutoAcceptEulas = false;
            mock.Object.AutoSelectUpdates = true;
            mock.Object.DownloadTimeout = (int)DefaultAsyncOperationTimeout.DownloadTimeout;
            mock.Object.SearchTimeout = (int)DefaultAsyncOperationTimeout.SearchTimeout;
            mock.Object.InstallTimeout = (int)DefaultAsyncOperationTimeout.InstallTimeout;

            return mock.Object;
        }

        private WuApiControllerFactory GetFactory(IWuApiController controller = null)
        {
            controller = controller ?? MoqFactory.Create<IWuApiController>(MockBehavior.Loose).Object;
            var mock = MoqFactory.Create<WuApiControllerFactory>();
            mock.Setup(f => f.GetController()).Returns(controller);
            return mock.Object;
        }

        private OperationContextProvider GetProvider(IWuRemoteServiceCallback callback, CommunicationState comState)
        {
            var opp = MoqFactory.Create<OperationContextProvider>();
            opp.Setup(o => o.GetCallbackChannel()).Returns(callback);
            opp.Setup(o => o.GetCommunicationState(It.IsAny<IWuRemoteServiceCallback>())).Returns(comState);
            return opp.Object;
        }

        [TestMethod, TestCategory("ContractConfiguration")]
        public void Find_ConcurrencyModeReentrant_On_ServiceImplementation()
        {
            var serviceBehavior = typeof(WuRemoteService).CustomAttributes.Single(a => a.AttributeType == typeof(ServiceBehaviorAttribute));
            var concurrencyMode = (ConcurrencyMode)(serviceBehavior.NamedArguments.Single(a => a.TypedValue.ArgumentType == typeof(ConcurrencyMode)).TypedValue.Value);
            Assert.IsTrue(concurrencyMode == ConcurrencyMode.Reentrant, "The service is not protected against race conditions, the concurrency mode must be reentrant.");
        }

        [TestMethod, TestCategory("ContractConfiguration")]
        public void Find_InstanceContextModeSingle_On_ServiceImplementation()
        {
            var serviceBehavior = typeof(WuRemoteService).CustomAttributes.Single(a => a.AttributeType == typeof(ServiceBehaviorAttribute));
            var contextMode = (InstanceContextMode)(serviceBehavior.NamedArguments.Single(a => a.TypedValue.ArgumentType == typeof(InstanceContextMode)).TypedValue.Value);
            Assert.IsTrue(contextMode == InstanceContextMode.Single, $"The instance mode must be single, the underlying {nameof(WindowsUpdateApiController)} must be persistent for each call.");
        }


        [TestMethod, TestCategory("Security"), TestCategory("ContractConfiguration")]
        public void Find_AdminPrincReqAttr_On_AllMethods()
        {
            var methods = typeof(WuRemoteService).GetMethods().Where(m => m.IsPublic && m.DeclaringType == typeof(WuRemoteService) && m.Name != "Dispose");
            Assert.IsTrue(methods.All(m => m.CustomAttributes.Single(a => a.AttributeType == typeof(AdministratorPrincipalRequiredAttribute)) != null));
        }

        [TestMethod, TestCategory("Timeouts")]
        public void Should_ReturnSpecifiedTimeoutValues_When_SetTimeoutValuesBefore()
        {
            int search = 5, download = 10, install = 15;
            using (var service = new WuRemoteService(GetFactory(), OpsProvider, GetConfigProvider()))
            {
                service.SetSearchTimeout(search);
                service.SetDownloadTimeout(download);
                service.SetInstallTimeout(install);

                var settings = service.GetSettings();

                Assert.AreEqual(settings.SearchTimeoutSec, search);
                Assert.AreEqual(settings.DownloadTimeoutSec, download);
                Assert.AreEqual(settings.InstallTimeoutSec, install);
            }
        }

        [TestMethod, TestCategory("Timeouts")]
        public void Should_ReturnDefaultTimeoutValues_When_ConfigContainsDefaults()
        {
            int search = (int)DefaultAsyncOperationTimeout.SearchTimeout, 
                download = (int)DefaultAsyncOperationTimeout.DownloadTimeout,
                install = (int)DefaultAsyncOperationTimeout.InstallTimeout;
            using (var service = new WuRemoteService(GetFactory(), OpsProvider, GetConfigProvider()))
            {
                var settings = service.GetSettings();
                Assert.AreEqual(search, settings.SearchTimeoutSec);
                Assert.AreEqual(download, settings.DownloadTimeoutSec);
                Assert.AreEqual(install, settings.InstallTimeoutSec);
            }
        }

        [TestMethod, TestCategory("Timeouts"), TestCategory("Faults")]
        public void Should_ThrowFault_When_SetInvalidTimeouts()
        {
            List<int> invalidTimeouts = new List<int>() { 0, -1, int.MaxValue / 1000 + 1 };

            using (var service = new WuRemoteService(GetFactory(), OpsProvider, GetConfigProvider()))
            {
                foreach (int timeout in invalidTimeouts)
                {
                    try
                    {
                        service.SetSearchTimeout(timeout);
                        Assert.Fail("Exception expected");
                    }
                    catch (FaultException e)
                    {
                        Assert.AreEqual(e.Code.Name, "BadArgument");
                    }
                    try
                    {
                        service.SetDownloadTimeout(timeout);
                        Assert.Fail("Exception expected");
                    }
                    catch (FaultException e)
                    {
                        Assert.AreEqual(e.Code.Name, "BadArgument");
                    }
                    try
                    {
                        service.SetInstallTimeout(timeout);
                        Assert.Fail("Exception expected");
                    }
                    catch (FaultException e)
                    {
                        Assert.AreEqual(e.Code.Name, "BadArgument");
                    }
                }
            }
        }

        [TestMethod, TestCategory("Settings"), TestCategory("Timeouts")]
        public void Should_ReturnCurrentSettings_When_RequestSettings()
        {
            var controller = MoqFactory.Create<IWuApiController>();

            controller.SetupProperty(c => c.AutoAcceptEulas);
            controller.SetupProperty(c => c.AutoSelectUpdates);
            controller.Setup(c => c.AutoAcceptEulas).Returns(true);
            controller.Setup(c => c.AutoSelectUpdates).Returns(false);

            var service = new WuRemoteService(GetFactory(controller.Object), OpsProvider, GetConfigProvider());
            service.SetSearchTimeout(1);
            service.SetDownloadTimeout(2);
            service.SetInstallTimeout(3);

            var settings = service.GetSettings();

            Assert.AreEqual(settings.AutoAcceptEulas, true);
            Assert.AreEqual(settings.AutoSelectUpdates, false);
            Assert.AreEqual(settings.SearchTimeoutSec, 1);
            Assert.AreEqual(settings.DownloadTimeoutSec, 2);
            Assert.AreEqual(settings.InstallTimeoutSec, 3);

            controller.Setup(c => c.AutoAcceptEulas).Returns(false);
            controller.Setup(c => c.AutoSelectUpdates).Returns(true);
            settings = service.GetSettings();
            Assert.AreEqual(settings.AutoAcceptEulas, false);
            Assert.AreEqual(settings.AutoSelectUpdates, true);


            service.Dispose();
        }

        [TestMethod, TestCategory("Settings"), TestCategory("Timeouts")]
        public void Should_PassTimeoutToMethod_When_CallBegin()
        {
            var controller = MoqFactory.Create<IWuApiController>(MockBehavior.Loose);
            using (var service = new WuRemoteService(GetFactory(controller.Object), OpsProvider, GetConfigProvider()))
            {

                service.SetSearchTimeout(1);
                service.SetDownloadTimeout(2);
                service.SetInstallTimeout(3);

                service.BeginSearchUpdates();
                service.BeginDownloadUpdates();
                service.BeginInstallUpdates();

                controller.Verify(c => c.BeginSearchUpdates(It.Is<int>(i => i==1)), Times.Once);
                controller.Verify(c => c.BeginDownloadUpdates(It.Is<int>(i => i == 2)), Times.Once);
                controller.Verify(c => c.BeginInstallUpdates(It.Is<int>(i => i == 3)), Times.Once);
            }
        }

        [TestMethod, TestCategory("Events")]
        public void Should_UnregisterEventWatching_When_ReplaceController()
        {
            var controller = MoqFactory.Create<IWuApiController>(MockBehavior.Loose);
            var dispose = controller.As<IDisposable>();
            dispose.Setup(d => d.Dispose());
            var controller2 = MoqFactory.Create<IWuApiController>(MockBehavior.Loose);
            controller2.Setup(c => c.GetWuStatus()).Returns(new StateDescription(WuStateId.Ready, "f", "f", InstallerStatus.Ready, new WuEnviroment("f", "f", "f", "f", TimeSpan.MinValue, 1)));
            var callback = MoqFactory.Create<IWuRemoteServiceCallback>(MockBehavior.Loose);
            var factory = MoqFactory.Create<WuApiControllerFactory>();
            factory.SetupSequence(f => f.GetController()).Returns(controller.Object).Returns(controller2.Object);

            using (var service = new WuRemoteService(factory.Object, GetProvider(callback.Object, CommunicationState.Opened), GetConfigProvider()))
            {
                service.RegisterForCallback();
                service.ResetService(); // This should dispose the current controller and an new controller should be used.
                controller.Raise(c => c.OnAsyncOperationCompleted += null, new AsyncOperationCompletedEventArgs(AsyncOperation.Searching, WuStateId.SearchCompleted)); // Events from the old controller should be ignored.
                Thread.Sleep(50); // A seperate thread needs a few ms to send the callback.
                callback.Verify(c => c.OnAsyncOperationCompleted(It.IsAny<AsyncOperation>(), It.IsAny<WuStateId>()), Times.Never);
                dispose.Verify(c => c.Dispose(), Times.Once);
            }
        }


        [TestMethod, TestCategory("Callback"), TestCategory("Events")]
        public void Should_CallbackToClient_When_ProgressChangedEventFires()
        {
            var controller = MoqFactory.Create<IWuApiController>(MockBehavior.Loose);
            var callback = MoqFactory.Create<IWuRemoteServiceCallback>(MockBehavior.Loose);

            using (var service = new WuRemoteService(GetFactory(controller.Object), GetProvider(callback.Object, CommunicationState.Opened), GetConfigProvider()))
            {
                service.RegisterForCallback();
                controller.Raise(c => c.OnProgressChanged += null, new ProgressChangedEventArgs(WuStateId.Searching, new ProgressDescription()));
                Thread.Sleep(50); // A seperate thread needs a few ms to send the callback 
                callback.Verify(c => c.OnProgressChanged(It.IsAny<ProgressDescription>(), It.IsAny<WuStateId>()), Times.Once);
            }
        }

        [TestMethod, TestCategory("Callback"), TestCategory("Events")]
        public void Should_CallbackToClient_When_StateChangedEventFires()
        {
            var controller = MoqFactory.Create<IWuApiController>(MockBehavior.Loose);
            var callback = MoqFactory.Create<IWuRemoteServiceCallback>(MockBehavior.Loose);

            using (var service = new WuRemoteService(GetFactory(controller.Object), GetProvider(callback.Object, CommunicationState.Opened), GetConfigProvider()))
            {
                service.RegisterForCallback();
                controller.Raise(c => c.OnStateChanged+=null, new StateChangedEventArgs(WuStateId.Searching, WuStateId.SearchCompleted));
                Thread.Sleep(50); // A seperate thread needs a few ms to send the callback 
                callback.Verify(c => c.OnStateChanged(It.IsAny<WuStateId>(), It.IsAny<WuStateId>()), Times.Once);
            }
        }

        [TestMethod, TestCategory("Callback"), TestCategory("Events")]
        public void Should_CallbackToClient_When_AsyncOperationEventFires()
        {
            var controller = MoqFactory.Create<IWuApiController>(MockBehavior.Loose);
            var callback = MoqFactory.Create<IWuRemoteServiceCallback>(MockBehavior.Loose);

            using (var service = new WuRemoteService(GetFactory(controller.Object), GetProvider(callback.Object, CommunicationState.Opened), GetConfigProvider()))
            {
                service.RegisterForCallback();
                controller.Raise(c => c.OnAsyncOperationCompleted += null, new AsyncOperationCompletedEventArgs(AsyncOperation.Searching, WuStateId.SearchCompleted));
                Thread.Sleep(50); // A seperate thread needs a few ms to send the callback 
                callback.Verify(c => c.OnAsyncOperationCompleted(It.IsAny<AsyncOperation>(), It.IsAny<WuStateId>()), Times.Once);
            }
        }

        [TestMethod, TestCategory("Callback")]
        public void Should_CallbackToClient_When_ServiceWillShutdown()
        {
            var callback = MoqFactory.Create<IWuRemoteServiceCallback>(MockBehavior.Loose);
            
            using (var service = new WuRemoteService(GetFactory(), GetProvider(callback.Object, CommunicationState.Opened), GetConfigProvider()))
            {
                service.RegisterForCallback();
                service.SendShutdownSignal();
                Thread.Sleep(50); // A seperate thread needs a few ms to send the callback 
                callback.Verify(c => c.OnServiceShutdown(), Times.Once);
            }
        }

        [TestMethod, TestCategory("Callback")]
        public void Should_NotCallbackToClient_When_ConnectionIsNotReady()
        {
            var controller = MoqFactory.Create<IWuApiController>(MockBehavior.Loose);
            var callback = MoqFactory.Create<IWuRemoteServiceCallback>(MockBehavior.Loose);

            using (var service = new WuRemoteService(GetFactory(controller.Object), GetProvider(callback.Object, CommunicationState.Closed), GetConfigProvider()))
            {
                service.RegisterForCallback();
                controller.Raise(c => c.OnAsyncOperationCompleted+=null, new AsyncOperationCompletedEventArgs(AsyncOperation.Searching, WuStateId.SearchCompleted));
                callback.Verify(c => c.OnAsyncOperationCompleted(It.IsAny<AsyncOperation>(), It.IsAny<WuStateId>()), Times.Never);
            }
            using (var service = new WuRemoteService(GetFactory(controller.Object), GetProvider(callback.Object, CommunicationState.Closing), GetConfigProvider()))
            {
                service.RegisterForCallback();
                controller.Raise(c => c.OnAsyncOperationCompleted+=null, new AsyncOperationCompletedEventArgs(AsyncOperation.Searching, WuStateId.SearchCompleted));
                callback.Verify(c => c.OnAsyncOperationCompleted(It.IsAny<AsyncOperation>(), It.IsAny<WuStateId>()), Times.Never);
            }
            using (var service = new WuRemoteService(GetFactory(controller.Object), GetProvider(callback.Object, CommunicationState.Faulted), GetConfigProvider()))
            {
                service.RegisterForCallback();
                controller.Raise(c => c.OnAsyncOperationCompleted+=null, new AsyncOperationCompletedEventArgs(AsyncOperation.Searching, WuStateId.SearchCompleted));
                callback.Verify(c => c.OnAsyncOperationCompleted(It.IsAny<AsyncOperation>(), It.IsAny<WuStateId>()), Times.Never);
            }
            using (var service = new WuRemoteService(GetFactory(controller.Object), GetProvider(callback.Object, CommunicationState.Opening), GetConfigProvider()))
            {
                service.RegisterForCallback();
                controller.Raise(c => c.OnAsyncOperationCompleted+=null, new AsyncOperationCompletedEventArgs(AsyncOperation.Searching, WuStateId.SearchCompleted));
                callback.Verify(c => c.OnAsyncOperationCompleted(It.IsAny<AsyncOperation>(), It.IsAny<WuStateId>()), Times.Never);
            }
        }

        [TestMethod, TestCategory("Passthrough")]
        public void Should_PassthroughCall_When_CallAbort()
        {
            var controllerMock = MoqFactory.Create<IWuApiController>(MockBehavior.Loose);

            using (var service = new WuRemoteService(GetFactory(controllerMock.Object), OpsProvider, GetConfigProvider()))
            {
                service.AbortDownloadUpdates();
                service.AbortInstallUpdates();
                service.AbortSearchUpdates();

                controllerMock.Verify(c => c.AbortDownloadUpdates(), Times.Once);
                controllerMock.Verify(c => c.AbortInstallUpdates(), Times.Once);
                controllerMock.Verify(c => c.AbortSearchUpdates(), Times.Once);
            }
        }

        [TestMethod, TestCategory("Passthrough")]
        public void Should_PassthroughCall_When_CallBegin()
        {
            var controllerMock = MoqFactory.Create<IWuApiController>(MockBehavior.Loose);

            using (var service = new WuRemoteService(GetFactory(controllerMock.Object), OpsProvider, GetConfigProvider()))
            {
                service.BeginDownloadUpdates();
                service.BeginInstallUpdates();
                service.BeginSearchUpdates();

                controllerMock.Verify(c => c.BeginDownloadUpdates(It.Is<int>(i => i.Equals(7200))), Times.Once);
                controllerMock.Verify(c => c.BeginInstallUpdates(It.Is<int>(i => i.Equals(10800))), Times.Once);
                controllerMock.Verify(c => c.BeginSearchUpdates(It.Is<int>(i => i.Equals(1200))), Times.Once);
            }
        }

        private void TestExceptionToFaultConversion(WuRemoteService service, List<Action> methodsToCall, string expectedFaultCode)
        {
            foreach (var method in methodsToCall)
            {
                try
                {
                    method();
                    Assert.Fail($"{nameof(FaultException)} expected.");
                }
                catch (FaultException fault)
                {
                    Assert.AreEqual(expectedFaultCode, fault.Code.Name);
                }
                catch (Exception e)
                {
                    Assert.Fail($"A {nameof(FaultException)} was expected, but {e.GetType().Name} was thrown.");
                }
            }          
        }

        [TestMethod, TestCategory("Faults"), TestCategory("Exception")]
        public void Should_ConvertToInvalidStateTransFault_When_CatchInvalidStateTransException()
        {
            var controllerMock = MoqFactory.Create<IWuApiController>();
            var exception = new InvalidStateTransitionException(typeof(object), typeof(object));

            controllerMock.SetupProperty(c => c.AutoSelectUpdates);
            controllerMock.SetupProperty(c => c.AutoAcceptEulas);
            controllerMock.Setup(c => c.AbortDownloadUpdates()).Throws(exception);
            controllerMock.Setup(c => c.AbortInstallUpdates()).Throws(exception);
            controllerMock.Setup(c => c.AbortSearchUpdates()).Throws(exception);
            controllerMock.Setup(c => c.BeginDownloadUpdates(It.IsAny<int>())).Throws(exception);
            controllerMock.Setup(c => c.BeginSearchUpdates(It.IsAny<int>())).Throws(exception);
            controllerMock.Setup(c => c.BeginInstallUpdates(It.IsAny<int>())).Throws(exception);
            controllerMock.Setup(c => c.Reboot()).Throws(exception);

            var service = new WuRemoteService(GetFactory(controllerMock.Object), OpsProvider, GetConfigProvider());
            List<Action> methodsToCall = new List<Action>() {
                ()=>service.AbortDownloadUpdates(),
                ()=>service.AbortInstallUpdates(),
                ()=>service.AbortSearchUpdates(),
                ()=>service.BeginDownloadUpdates(),
                ()=>service.BeginInstallUpdates(),
                ()=>service.BeginSearchUpdates(),
                ()=>service.RebootHost()
            };
            TestExceptionToFaultConversion(service, methodsToCall, "InvalidTransition");
            service.Dispose();
        }

        [TestMethod, TestCategory("Faults"), TestCategory("Exception")]
        public void Should_ConvertToApiFault_When_CatchCOMException()
        {
            var controllerMock = MoqFactory.Create<IWuApiController>();
            var exception = new COMException("test", 1);

            controllerMock.SetupProperty(c => c.AutoSelectUpdates);
            controllerMock.SetupProperty(c => c.AutoAcceptEulas);
            controllerMock.Setup(c => c.AbortDownloadUpdates()).Throws(exception);
            controllerMock.Setup(c => c.AbortInstallUpdates()).Throws(exception);
            controllerMock.Setup(c => c.AbortSearchUpdates()).Throws(exception);
            controllerMock.Setup(c => c.BeginDownloadUpdates(It.IsAny<int>())).Throws(exception);
            controllerMock.Setup(c => c.BeginSearchUpdates(It.IsAny<int>())).Throws(exception);
            controllerMock.Setup(c => c.BeginInstallUpdates(It.IsAny<int>())).Throws(exception);
            controllerMock.Setup(c => c.AcceptEula(It.IsAny<string>())).Throws(exception);

            var service = new WuRemoteService(GetFactory(controllerMock.Object), OpsProvider, GetConfigProvider());
            List<Action> methodsToCall = new List<Action>() {
                ()=>service.AbortDownloadUpdates(),
                ()=>service.AbortInstallUpdates(),
                ()=>service.AbortSearchUpdates(),
                ()=>service.BeginDownloadUpdates(),
                ()=>service.BeginInstallUpdates(),
                ()=>service.BeginSearchUpdates(),
                ()=>service.AcceptEula("test"),
            };

            TestExceptionToFaultConversion(service, methodsToCall, "ApiFault");
            service.Dispose();
        }

        [TestMethod, TestCategory("Faults"), TestCategory("Exception")]
        public void Should_ConvertToBadArgumentFault_When_CatchArgumentException()
        {
            var controllerMock = MoqFactory.Create<IWuApiController>();
            var exception = new ArgumentException();

            controllerMock.SetupProperty(c => c.AutoSelectUpdates);
            controllerMock.SetupProperty(c => c.AutoAcceptEulas);
            controllerMock.Setup(c => c.AcceptEula(It.IsAny<string>())).Throws(exception);
            controllerMock.Setup(c => c.SelectUpdate(It.IsAny<string>())).Throws(exception);
            controllerMock.Setup(c => c.UnselectUpdate(It.IsAny<string>())).Throws(exception);

            var service = new WuRemoteService(GetFactory(controllerMock.Object), OpsProvider, GetConfigProvider());
            List<Action> methodsToCall = new List<Action>() {
                ()=>service.SetSearchTimeout(int.MaxValue),
                ()=>service.SetInstallTimeout(int.MaxValue),
                ()=>service.SetDownloadTimeout(int.MaxValue),
                ()=>service.AcceptEula("test"),
                ()=>service.SelectUpdate("test"),
                ()=>service.UnselectUpdate("test"),
            };
            TestExceptionToFaultConversion(service, methodsToCall, "BadArgument");
            service.Dispose();
        }

        [TestMethod, TestCategory("Faults"), TestCategory("Exception")]
        public void Should_ConvertToPreConNotFullfilledFault_When_CatchPreConNotFulfilledException()
        {
            var controllerMock = MoqFactory.Create<IWuApiController>();
            var exception = new PreConditionNotFulfilledException(typeof(object), typeof(object), "Test");

            controllerMock.SetupProperty(c => c.AutoSelectUpdates);
            controllerMock.SetupProperty(c => c.AutoAcceptEulas);
            controllerMock.Setup(c => c.BeginDownloadUpdates(It.IsAny<int>())).Throws(exception);
            controllerMock.Setup(c => c.BeginSearchUpdates(It.IsAny<int>())).Throws(exception);
            controllerMock.Setup(c => c.BeginInstallUpdates(It.IsAny<int>())).Throws(exception);
            controllerMock.Setup(c => c.Reboot()).Throws(exception);

            var service = new WuRemoteService(GetFactory(controllerMock.Object), OpsProvider, GetConfigProvider());
            List<Action> methodsToCall = new List<Action>() {
                ()=>service.BeginDownloadUpdates(),
                ()=>service.BeginInstallUpdates(),
                ()=>service.BeginSearchUpdates(),
                ()=>service.RebootHost()
            };
            TestExceptionToFaultConversion(service, methodsToCall, "PreConditionNotFulfilled");
            service.Dispose();
        }
    }
}
