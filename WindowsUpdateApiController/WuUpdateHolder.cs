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
using System.Collections.Generic;
using System.Linq;
using WindowsUpdateApiController.Exceptions;
using WUApiLib;
using WuDataContract.DTO;

namespace WindowsUpdateApiController
{
    /// <summary>
    /// Helperclass to hold a windows update search result.
    /// Allows to mark updates as selected.
    /// </summary>
    internal class WuUpdateHolder
    {
        IUpdateCollection _applicableUpdates = null;
        List<string> _selectedUpdates = new List<string>();
        volatile bool _autoSelectUpdates = true;
        readonly object _updateLock = new object();

        /// <summary>
        /// List of updates that were found by an update search.
        /// Null if no search result is present.
        /// </summary>
        public IUpdateCollection ApplicableUpdates
        {
            get
            {
                lock (_updateLock)
                {
                    return _applicableUpdates;
                }
            }
        }

        /// <summary>
        /// Returns all selected updates.
        /// The result is always a subset of <see cref="ApplicableUpdates"/>.
        /// </summary>
        public IEnumerable<IUpdate> GetSelectedUpdates() => GetSelectedUpdates(null);

        /// <summary>
        /// Returns all selected updates where <paramref name="filter"/> applies.
        /// If <paramref name="filter" /> is null, the result will not be filtered.
        /// The result is always a subset of <see cref="ApplicableUpdates"/>.
        /// </summary>
        public IEnumerable<IUpdate> GetSelectedUpdates(Func<IUpdate, bool> filter)
        {
            lock (_updateLock)
            {
                if (_applicableUpdates == null) return new List<IUpdate>();
                var selected = _applicableUpdates.OfType<IUpdate>().Where(u => _selectedUpdates.Contains(u.Identity.UpdateID));
                if (filter == null) return selected;
                return selected.Where(u => filter(u));
            }
        }

        /// <summary>
        /// If enabled, important updates will be automatically marked as selected (<see cref="GetSelectedUpdates"/>).
        /// Auto-Assignment only occures when <see cref="SetApplicableUpdates(IUpdateCollection)"/> is called.
        /// </summary>
        public bool AutoSelectUpdates
        {
            get { return _autoSelectUpdates; }
            set { _autoSelectUpdates = value; }
        }

        /// <summary>
        /// Creates the update holder.
        /// </summary>
        /// <param name="autoSelectUpdates">Initial value of <see cref="AutoSelectUpdates"/>.</param>
        public WuUpdateHolder(bool autoSelectUpdates = true)
        {
            AutoSelectUpdates = autoSelectUpdates;
        }

        /// <summary>
        /// Sets the windows update search result with updates that are applicable to this maschine.
        /// </summary>
        /// <param name="applicableUpdates">The windows update search result.</param>
        /// <exception cref="ArgumentNullException" />
        public void SetApplicableUpdates(IUpdateCollection applicableUpdates)
        {
            if (applicableUpdates == null) throw new ArgumentNullException(nameof(applicableUpdates));
            
            lock (_updateLock)
            {
                _applicableUpdates = applicableUpdates;
                _selectedUpdates.Clear();
                if (AutoSelectUpdates)
                {
                    foreach (var update in _applicableUpdates.OfType<IUpdate>().Where(u => IsImportant(u)))
                    {
                        _selectedUpdates.Add(update.Identity.UpdateID);
                    }
                }
            }
        }

        /// <summary>
        /// Checks if the update is marked as selected.
        /// </summary>
        /// <param name="updateId">The id of the update to check.</param>
        public bool IsSelected(string updateId)
        {
            lock (_updateLock)
            {
                return _selectedUpdates.Contains(updateId);
            }
        }

        /// <summary>
        /// Marks an update as selected.
        /// </summary>
        /// <param name="updateId">The id of the update to select.</param>
        /// <exception cref="UpdateNotFoundException" />
        public void SelectUpdate(string updateId)
        {
            lock (_updateLock)
            {
                if (_applicableUpdates == null) throw new UpdateNotFoundException(updateId, $"Update with id '{updateId}' was not found.");

                var updates = _applicableUpdates.OfType<IUpdate>().Where(u => u.Identity.UpdateID.Equals(updateId));
                if (updates.Any())
                {
                    if (!_selectedUpdates.Contains(updateId)) _selectedUpdates.Add(updateId);
                }
                else
                {
                    throw new UpdateNotFoundException(updateId, $"Update with id '{updateId}' was not found.");
                }
            }
        }

        /// <summary>
        /// Unselects an update.
        /// </summary>
        /// <param name="updateId">The id of the update to unselect.</param>
        /// <exception cref="UpdateNotFoundException" />
        public void UnselectUpdate(string updateId)
        {
            lock (_updateLock)
            {
                if (_applicableUpdates == null) throw new UpdateNotFoundException(updateId, $"Update with id '{updateId}' was not found.");

                var updates = _applicableUpdates.OfType<IUpdate>().Where(u => u.Identity.UpdateID.Equals(updateId));
                if (updates.Any())
                {
                    if (_selectedUpdates.Contains(updateId)) _selectedUpdates.RemoveAll(i => i.Equals(updateId));
                }
                else
                {
                    throw new UpdateNotFoundException(updateId, $"Update with id '{updateId}' was not found.");
                }
            }
        }

        /// <summary>
        /// Converts <see cref="IUpdate"/> to simplified <see cref="UpdateDescription"/>.
        /// </summary>
        /// <exception cref="ArgumentNullException" />
        public UpdateDescription ToUpdateDescription(IUpdate update)
        {
            if (update == null) throw new ArgumentNullException(nameof(update));

            UpdateDescription updateDesc = new UpdateDescription();
            updateDesc.IsImportant = IsImportant(update);
            updateDesc.Description = update.Description;
            updateDesc.ID = update.Identity.UpdateID;
            updateDesc.Title = (update.DeploymentAction == DeploymentAction.daUninstallation) ? "(Uninstallation) " : "" + update.Title; // ToDo: DeploymentAction to own property 
            updateDesc.MaxByteSize = (long)update.MaxDownloadSize;
            updateDesc.MinByteSize = (long)update.MinDownloadSize;
            updateDesc.IsDownloaded = update.IsDownloaded;
            updateDesc.IsInstalled = update.IsInstalled;
            updateDesc.EulaAccepted = update.EulaAccepted;
            updateDesc.SelectedForInstallation = IsSelected(update.Identity.UpdateID);
            return updateDesc;
        }

        /// <summary>
        /// Checks if an update is considered important by Microsoft.
        /// </summary>
        /// <param name="update">Update to verifiy.</param>
        /// <exception cref="ArgumentNullException" />
        private bool IsImportant(IUpdate update)
        {
            if (update == null) throw new ArgumentNullException(nameof(update));

            // Depending on the OS and Windows Update Service version, extending interfaces for IUpdate are available: IUpdate2, IUpdate3, etc.
            var update5 = update as IUpdate5;
            if (update5 != null)
            {
                switch (update5.AutoSelection) // https://msdn.microsoft.com/en-us/library/windows/desktop/ee694833(v=vs.85).aspx
                {
                    case AutoSelectionMode.asAlwaysAutoSelect:
                        return true;
                    case AutoSelectionMode.asNeverAutoSelect:
                        return false;
                    case AutoSelectionMode.asAutoSelectIfDownloaded:
                        return update.IsDownloaded;
                    case AutoSelectionMode.asLetWindowsUpdateDecide:
                    default:
                        break;
                }
            }
            var update3 = update as IUpdate3;
            return (update3 != null) ? !update3.BrowseOnly : update.IsMandatory || update.AutoSelectOnWebSites;
        }

    }
}
