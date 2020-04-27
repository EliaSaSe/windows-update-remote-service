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
using System.Security;
using System.Security.Permissions;
using System.Security.Principal;

namespace WcfWuRemoteService.Helper
{
    /// <summary>
    /// Code access security attribute that requires that a principal is member of the builtin administrator group/role.
    /// </summary>
    [Serializable, AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class AdministratorPrincipalRequiredAttribute : CodeAccessSecurityAttribute
    {
        /// <param name="action">Only <see cref="SecurityAction.Demand"/> is currently spported.</param>
        public AdministratorPrincipalRequiredAttribute(SecurityAction action) : base(action)
        {
            if (action != SecurityAction.Demand) throw new NotImplementedException($"The given security action {action.ToString("G")} is untested.");
        }

        /// <summary>
        /// Creates a <see cref="PrincipalPermission"/> which requires that the user is member of the builtin administrator group/role.
        /// If UAC is enabled, the process needs elevated privileges to get the administrator role.
        /// </summary>
        public override IPermission CreatePermission()
        {
#if DEBUG
            // Do not enforce admin privileges when running in debug mode.
            var identifier = new SecurityIdentifier(WellKnownSidType.AuthenticatedUserSid, null);
#else
            var identifier = new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, null);
#endif
            var role = identifier.Translate(typeof(NTAccount)).Value;
            return new PrincipalPermission(null, role);
        }
    }
}
