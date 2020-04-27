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
    /// Configuration section with properties for <see cref="WindowsUpdateApiController.WuApiController"/>.
    /// </summary>
    public class WuApiControllerConfigSection : ConfigurationSection
    {
        private static ConfigurationPropertyCollection _properties;
        private static ConfigurationProperty _autoSelectUpdatesBool;
        private static ConfigurationProperty _autoAcceptEulasBool;
        private static ConfigurationProperty _timeouts;

        const string AutoSelectUpdates = "autoSelectUpdates";
        const string AutoAcceptEulas = "autoAcceptEulas";
        public const string SectionName = "wuapicontroller";

        public WuApiControllerConfigSection()
        {
            _autoSelectUpdatesBool = new ConfigurationProperty(AutoSelectUpdates, typeof(bool), true, ConfigurationPropertyOptions.None);
            _autoAcceptEulasBool = new ConfigurationProperty(AutoAcceptEulas, typeof(bool), false, ConfigurationPropertyOptions.None);
            _timeouts = new ConfigurationProperty(WuApiControllerTimeoutElement.ElementName, typeof(WuApiControllerTimeoutElement));
            _properties = new ConfigurationPropertyCollection();
            _properties.Add(_autoSelectUpdatesBool);
            _properties.Add(_autoAcceptEulasBool);
            _properties.Add(_timeouts);
        }

        /// <summary>
        /// Value for <see cref="WindowsUpdateApiController.WuApiController.AutoSelectUpdates"/>.
        /// </summary>
        [ConfigurationProperty(AutoSelectUpdates)]
        public bool AutoSelectUpdatesValue
        {
            get { return (bool)base[_autoSelectUpdatesBool]; }
            set { base[_autoSelectUpdatesBool] = value; }
        }

        /// <summary>
        /// Value for <see cref="WindowsUpdateApiController.WuApiController.AutoAcceptEulas"/>.
        /// </summary>
        [ConfigurationProperty(AutoAcceptEulas)]
        public bool AutoAcceptEulasValue
        {
            get { return (bool)base[_autoAcceptEulasBool]; }
            set { base[_autoAcceptEulasBool] = value; }
        }

        [ConfigurationProperty(WuApiControllerTimeoutElement.ElementName, IsRequired = false)]
        public WuApiControllerTimeoutElement TimeoutValues
        {
            get { return (WuApiControllerTimeoutElement)base[_timeouts]; }
            set { base[_timeouts] = value; }
        }

        /// <summary>
        /// The collection of properties.
        /// </summary>
        protected override ConfigurationPropertyCollection Properties => _properties;

        /// <summary>
        /// This configuration section is never read only.
        /// </summary>
        /// <returns>false</returns>
        public override bool IsReadOnly() => false;
    }
}
