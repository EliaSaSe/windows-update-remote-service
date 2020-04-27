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
namespace WuDataContract.Enums
{
    /// <summary>
    /// A readiness indicator for the update installer component.
    /// </summary>
    public enum InstallerStatus
    {
        /// <summary>
        /// The installer is ready, updates can be installed.
        /// </summary>
        Ready,
        /// <summary>
        /// A reboot is required before the installer can install more updates.
        /// </summary>
        RebootRequiredBeforeInstallation,
        /// <summary>
        /// The installer is currently installing or uninstalling updates (because the current service installs updates or an update session, controlled by another process, is using the installer)
        /// </summary>
        Busy
    }
}
