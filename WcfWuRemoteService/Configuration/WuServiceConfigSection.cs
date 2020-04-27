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
using System.Configuration;

namespace WcfWuRemoteService.Configuration
{
    /// <summary>
    /// Configuration section for <see cref="WuRemoteService"/> and <see cref="WindowsService"/>.
    /// </summary>
    public class WuServiceConfigSection : ConfigurationSection
    {
        private static ConfigurationPropertyCollection _properties;
        private static ConfigurationProperty _createFWRule;

        const string CreateFirewallRule = "createFirewallRule";
        public const string SectionName = "wuservice";

        public WuServiceConfigSection()
        {
            _createFWRule = new ConfigurationProperty(CreateFirewallRule, typeof(bool), true, ConfigurationPropertyOptions.None);
            _properties = new ConfigurationPropertyCollection();
            _properties.Add(_createFWRule);
        }

        /// <summary>
        /// Gets the setting for the automatic firewall rule creation.
        /// </summary>
        [ConfigurationProperty(CreateFirewallRule)]
        public bool CreateFirewallRuleValue
        {
            get { return (bool)base[_createFWRule]; }
            set { base[_createFWRule] = value; }
        }

        /// <summary>
        /// The collection of properties.
        /// </summary>
        protected override ConfigurationPropertyCollection Properties
        {
            get { return _properties; }
        }

        /// <summary>
        /// This configuration section is never read only.
        /// </summary>
        /// <returns>false</returns>
        public override bool IsReadOnly()
        {
            return false;
        }
    }
}
