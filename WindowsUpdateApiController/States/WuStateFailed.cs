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
using WUApiLib;
using WuDataContract.Enums;

namespace WindowsUpdateApiController.States
{
    internal abstract class WuStateFailed : WuProcessState
    {
        public readonly IUpdateExceptionCollection Warnings;
        public readonly string Reason;

        public WuStateFailed(WuStateId id, string DisplayName, IUpdateExceptionCollection warnings, string reason = null) : base(id, DisplayName)
        {
            Warnings = warnings;
            Reason = reason;
            StateDesc = "Failure: " +Reason;
        }

        public override void LeaveState() { }
        public override void EnterState(WuProcessState oldState) { }
    }


    internal class WuStateSearchFailed : WuStateFailed
    {
        public WuStateSearchFailed(IUpdateExceptionCollection warnings, string reason = null) : base(WuStateId.SearchFailed, "Search Failed", warnings, reason) { }
    }

    internal class WuStateDownloadFailed : WuStateFailed
    {
        public WuStateDownloadFailed(IUpdateExceptionCollection warnings, string reason = null) : base(WuStateId.DownloadFailed, "Download Failed", warnings, reason) { }
    }

    internal class WuStateDownloadPartiallyFailed : WuStateFailed
    {
        public WuStateDownloadPartiallyFailed(IUpdateExceptionCollection warnings, string reason = null) : base(WuStateId.DownloadPartiallyFailed, "Download partially failed", warnings, reason) { }
    }

    internal class WuStateInstallFailed : WuStateFailed
    {
        public WuStateInstallFailed(IUpdateExceptionCollection warnings, string reason = null) : base(WuStateId.InstallFailed, "Installation Failed", warnings, reason) { }
    }

    internal class WuStateInstallPartiallyFailed : WuStateFailed
    {
        public WuStateInstallPartiallyFailed(IUpdateExceptionCollection warnings, string reason = null) : base(WuStateId.InstallPartiallyFailed, "Installation partially failed", warnings, reason) { }
    }
}
