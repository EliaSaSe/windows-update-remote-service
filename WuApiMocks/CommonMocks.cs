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
using WindowsUpdateApiController.Helper;
using WUApiLib;

namespace WuApiMocks
{
    /// <summary>
    /// Returns mocks with configurations which are often used by unit tests. 
    /// </summary>
    public static class CommonMocks
    {
        public static IDownloadResult GetDownloadResult(OperationResultCode resultcode, int hresult = 0)
        {
            var mock = new Mock<IDownloadResult>();
            mock.Setup(m => m.ResultCode).Returns(resultcode);
            mock.Setup(m => m.HResult).Returns(hresult);
            mock.Setup(m => m.GetUpdateResult(It.IsAny<int>())).Throws(new NotImplementedException("Not supported by this mock."));
            return mock.Object;
        }

        public static IInstallationResult GetInstallationResult(OperationResultCode resultcode, int hresult = 0, bool rebootRequired = false)
        {
            var mock = new Mock<IInstallationResult>();
            mock.Setup(m => m.ResultCode).Returns(resultcode);
            mock.Setup(m => m.HResult).Returns(hresult);
            mock.Setup(m => m.RebootRequired).Returns(rebootRequired);
            mock.Setup(m => m.GetUpdateResult(It.IsAny<int>())).Throws(new NotImplementedException("Not supported by this mock."));
            return mock.Object;
        }

        public static ISearchResult GetSearchResult(IUpdateCollection updates, OperationResultCode resultcode = OperationResultCode.orcSucceeded)
        {
            var mock = new Mock<ISearchResult>();
            mock.Setup(m => m.ResultCode).Returns(resultcode);
            mock.Setup(m => m.Updates).Returns(((UpdateCollection)updates));
            mock.Setup(m => m.RootCategories).Throws(new NotImplementedException("Not supported by this mock."));
            mock.Setup(m => m.Warnings).Returns((IUpdateExceptionCollection)null);
            return mock.Object;
        }

        public static IUpdateIdentity GetUpdateIdentity(string id, int rev = 0)
        {
            var idMock = new Mock<IUpdateIdentity>();
            idMock.Setup(b => b.UpdateID).Returns(id);
            idMock.Setup(b => b.RevisionNumber).Returns(rev);
            return idMock.Object;
        }

        public static ISystemInfo GetSystemInfo()
        {
            var sysInfo = new Mock<ISystemInfo>(MockBehavior.Strict);
            sysInfo.Setup(s => s.GetFQDN()).Returns("mock");
            sysInfo.Setup(s => s.GetFreeSpace()).Returns(10000000);
            sysInfo.Setup(s => s.GetOperatingSystemName()).Returns("Mock OS");
            sysInfo.Setup(s => s.GetTargetGroup()).Returns("mock target group");
            sysInfo.Setup(s => s.GetUptime()).Returns(TimeSpan.FromHours(1));
            sysInfo.Setup(s => s.GetWuServer()).Returns("mock update server");
            sysInfo.Setup(s => s.IsRebootRequired()).Returns(false);
            return sysInfo.Object;
        }

    }
}
