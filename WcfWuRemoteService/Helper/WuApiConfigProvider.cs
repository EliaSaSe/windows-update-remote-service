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
using WcfWuRemoteService.Configuration;

namespace WcfWuRemoteService.Helper
{
    /// <summary>
    /// Provides the configuration data for <see cref="WuRemoteService"/>.
    /// </summary>
    class WuApiConfigProvider : IWuApiConfigProvider
    {
        readonly System.Configuration.Configuration _appConfiguration;
        readonly WuApiControllerConfigSection _section;
        private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public WuApiConfigProvider()
        {
            Log.Info($"Reading configuration section '{WuApiControllerConfigSection.SectionName}'.");
            _appConfiguration = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            _section = (WuApiControllerConfigSection)_appConfiguration.GetSection(WuApiControllerConfigSection.SectionName);
            if (_section == null)
            {
                Log.Warn($"The configuration section '{WuApiControllerConfigSection.SectionName}' could not be found, using default settings.");
                _section = new WuApiControllerConfigSection();
                _appConfiguration.Sections.Add(WuApiControllerConfigSection.SectionName, _section);
            }
            if (_section.TimeoutValues == null) _section.TimeoutValues = new WuApiControllerTimeoutElement();
        }

        public bool AutoAcceptEulas
        {
            get { return _section.AutoAcceptEulasValue; }
            set { _section.AutoAcceptEulasValue = value; }
        }

        public bool AutoSelectUpdates
        {
            get { return _section.AutoSelectUpdatesValue; }
            set { _section.AutoSelectUpdatesValue = value; }
        }


        public int DownloadTimeout
        {
            get { return _section.TimeoutValues.DownloadTimeoutValue; }
            set { _section.TimeoutValues.DownloadTimeoutValue = value; }
        }

        public int InstallTimeout
        {
            get { return _section.TimeoutValues.InstallTimeoutValue; }
            set { _section.TimeoutValues.InstallTimeoutValue = value; }
        }

        public int SearchTimeout
        {
            get { return _section.TimeoutValues.SearchTimeoutValue; }
            set { _section.TimeoutValues.SearchTimeoutValue = value; }
        }

        public void Dispose() => Save();

        public void Save()
        {
            Log.Info($"Saving configuration in section '{WuApiControllerConfigSection.SectionName}'.");
            _appConfiguration.Save(ConfigurationSaveMode.Modified, true);
            ConfigurationManager.RefreshSection(WuApiControllerConfigSection.SectionName);
        }
    }
}
