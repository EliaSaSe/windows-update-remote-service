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
using WuDataContract.Enums;

namespace WindowsUpdateApiController.States
{
    internal class WuStateRebootRequired : WuProcessState
    {
        public WuStateRebootRequired() : base(WuStateId.RebootRequired, "Reboot required") { }

        public override void EnterState(WuProcessState oldState) {
            if (oldState != null && oldState.StateId == WuStateId.Installing) StateDesc = "To complete the installation of updates, a reboot is required.";
            if (oldState == null) StateDesc = "Before updates can be installed, a reboot is required.";
        }

        public override void LeaveState() { }
    }
}
