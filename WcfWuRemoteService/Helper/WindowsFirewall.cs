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
using log4net;
using NetFwTypeLib;
using System;
using System.IO;
using System.Linq;

namespace WcfWuRemoteService.Helper
{
    /// <summary>
    /// Opens or closes the windows firewall for the application.
    /// Public members are not thread safe.
    /// </summary>
    class WindowsFirewall
    {
        private static readonly ILog Log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        /// <param name="appName">Displayname of the application.</param>
        public WindowsFirewall(string appName)
        {
            if (String.IsNullOrWhiteSpace(appName)) throw new ArgumentNullException(nameof(appName));

            AppName = appName;

        }

        /// <summary>
        /// The <see cref="AppName"/> is the rule name and is shown in the rulelist of the firewall.
        /// </summary>
        public readonly string AppName;

        /// <summary>
        /// Image path of the application, determined via <see cref="System.Reflection.Assembly"/>.
        /// </summary>
        private FileInfo ImagePath
        {
            get
            {
                var path = System.Reflection.Assembly.GetEntryAssembly().CodeBase;
                path = path.Substring(8); // remove 'file:\\'
                return new FileInfo(path);
            }
        }

        /// <summary>
        /// Creates a rule on the current firewall profile, if such a rule not allready exists. Does nothing if the firewall is not enabled.
        /// Adds the <see cref="ImagePath"/> to the authorized applications.
        /// </summary>
        public void OpenFirewall()
        {
            INetFwAuthorizedApplication authApp = null;
            var profile = GetCurrentProfile();

            if (!profile.FirewallEnabled)
            {
                Log.Info($"The current firewall profile is not enabled, so it's not required to create a firewall rule.");
                return;
            }

            try
            {
                if (!IsAppFound(AppName))
                {
                    Log.Debug($"Authorizing application {AppName} on the current firewall profile, image path for authorizing: " + ImagePath.FullName);
                    authApp = (INetFwAuthorizedApplication)(Activator.CreateInstance(Type.GetTypeFromCLSID(new Guid("{EC9846B3-2762-4A6B-A214-6ACB603462D2}"))));
                    authApp.Name = AppName;
                    authApp.Enabled = true;
                    authApp.ProcessImageFileName = ImagePath.FullName;
                    profile.AuthorizedApplications.Add(authApp);
                }

                Log.Warn($"Firewall is open for application {AppName}.");
            }
            finally
            {
                if (authApp != null) authApp = null;
            }
        }

        /// <summary>
        /// Removes the application from the authorized applications on the current firewall profile, if such a rule exists.
        /// </summary>
        public void CloseFirewall()
        {
            var profile = GetCurrentProfile();
            if (IsAppFound(AppName))
            {
                profile.AuthorizedApplications.Remove(ImagePath.FullName);
                Log.Debug($"Application {AppName} is no longer authorized on the current firewall profile.");
            }
        }

        /// <summary>
        /// Checks if a rule with given name exists on the current firewall profile.
        /// </summary>
        /// <param name="ruleName">Name of the rule.</param>
        private bool IsAppFound(string ruleName)
        {
            INetFwMgr firewall = null;
            try
            {
                Type progID = Type.GetTypeFromProgID("HNetCfg.FwMgr");
                firewall = Activator.CreateInstance(progID) as INetFwMgr;
                if (firewall.LocalPolicy.CurrentProfile.FirewallEnabled)
                {
                    return firewall.LocalPolicy.CurrentProfile.AuthorizedApplications.OfType<INetFwAuthorizedApplication>().Any(a => a.Name.Equals(ruleName));
                }
                return false;
            }
            finally
            {
                if (firewall != null) firewall = null;
            }
        }

        /// <summary>
        /// The currently active firewall profile.
        /// </summary>
        private INetFwProfile GetCurrentProfile()
        {
            var t = Type.GetTypeFromCLSID(new Guid("{304CE942-6E39-40D8-943A-B913C40C9CD4}"));
            return ((INetFwMgr)(Activator.CreateInstance(t))).LocalPolicy.CurrentProfile;
        }
    }
}
