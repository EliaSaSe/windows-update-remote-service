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
using System;
using System.Linq;
using WUApiLib;
using WuDataContract.Enums;

namespace WindowsUpdateApiController.States
{

    internal abstract class WuStateCompleted : WuProcessState
    {
        public readonly IUpdateCollection Updates;
        public readonly int HResult;

        public WuStateCompleted(WuStateId id, string DisplayName, IUpdateCollection updates, int hResult) : base(id, DisplayName)
        {
            if (updates == null) throw new ArgumentNullException(nameof(updates));
            Updates = updates;
            HResult = hResult;
        }

        public override void LeaveState(){}
    }

    internal class WuStateSearchCompleted : WuStateCompleted
    {

        public WuStateSearchCompleted(IUpdateCollection updates, int hResult = 0) : base(WuStateId.SearchCompleted, "Search Completed", updates, hResult) { }

        public override void EnterState(WuProcessState oldState) => StateDesc = Updates.OfType<IUpdate>().Count(u => !u.IsInstalled) + " update(s) found";
    }

    internal class WuStateDownloadCompleted : WuStateCompleted
    {
        public WuStateDownloadCompleted(IUpdateCollection updates, int hResult = 0) : base(WuStateId.DownloadCompleted, "Download Completed", updates, hResult) { }

        public override void EnterState(WuProcessState oldState) => StateDesc = Updates.OfType<IUpdate>().Count(u => u.IsDownloaded) + " update(s) downloaded";
    }

    internal class WuStateInstallCompleted : WuStateCompleted
    {
        public WuStateInstallCompleted(IUpdateCollection updates, int hResult = 0) : base(WuStateId.InstallCompleted, "Installation Completed", updates, hResult) { }

        public override void EnterState(WuProcessState oldState) => StateDesc = Updates.OfType<IUpdate>().Count() + " update(s) installed";
    }
}
