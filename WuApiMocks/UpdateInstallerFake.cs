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
using Moq;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WUApiLib;

namespace WuApiMocks
{
    public class UpdateInstallerFake : IUpdateInstaller
    {
        public UpdateInstallerFake()
        {
            FakeInstallTimeMs = 0;
        }

        public int FakeInstallTimeMs { get; set; }
        public IInstallationResult FakeInstallResult { get; set; }


        #region interface

        public bool AllowSourcePrompts { get; set; }

        public string ClientApplicationID { get; set; }

        public bool IsBusy { get; set; }

        public bool IsForced { get; set; }

        public dynamic parentWindow
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public bool RebootRequiredBeforeInstallation{get; set;}

        public UpdateCollection Updates { get; set; }

        public IInstallationJob BeginInstall(object onProgressChanged, object onCompleted, object state)
        {
            CancellationTokenSource source = new CancellationTokenSource();
            var installJobMock = new Mock<IInstallationJob>();
            installJobMock.Setup(i => i.RequestAbort()).Callback(() => source.Cancel());
            installJobMock.Setup(i => i.AsyncState).Returns(state);
            installJobMock.Setup(i => i.IsCompleted).Returns(false);

            Task.Run(
                () =>
                {
                    for (int delayed = 0; delayed <= FakeInstallTimeMs; delayed = delayed + 10)
                    {
                        if (source.Token.IsCancellationRequested) return;
                        Thread.Sleep(10);
                    }
                    if (!source.Token.IsCancellationRequested)
                    {
                        installJobMock.Setup(i => i.IsCompleted).Returns(true);
                        ((IInstallationCompletedCallback)onCompleted).Invoke(installJobMock.Object, null);
                    }
                }, source.Token);
            return installJobMock.Object;
        }

        public IInstallationJob BeginUninstall(object onProgressChanged, object onCompleted, object state)
        {
            throw new NotImplementedException();
        }

        public IInstallationResult EndInstall(IInstallationJob value)
        {
            if (FakeInstallResult == null)
            {
                Updates.OfType<UpdateFake>().ToList().ForEach(u => u.IsInstalled = true);
                return CommonMocks.GetInstallationResult(OperationResultCode.orcSucceeded);
            }
            return FakeInstallResult;
        }

        public IInstallationResult EndUninstall(IInstallationJob value)
        {
            throw new NotImplementedException();
        }

        public IntPtr get_ParentHwnd()
        {
            throw new NotImplementedException();
        }

        public IInstallationResult Install()
        {
            Thread.Sleep(FakeInstallTimeMs);
            if (FakeInstallResult == null)
            {
                Updates.OfType<UpdateFake>().ToList().ForEach(u => u.IsInstalled = true);
                return CommonMocks.GetInstallationResult(OperationResultCode.orcSucceeded);
            }
            return FakeInstallResult;
        }

        public IInstallationResult RunWizard(string dialogTitle = "")
        {
            throw new NotImplementedException();
        }

        public void set_ParentHwnd(ref _RemotableHandle retval)
        {
            throw new NotImplementedException();
        }

        public IInstallationResult Uninstall()
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
