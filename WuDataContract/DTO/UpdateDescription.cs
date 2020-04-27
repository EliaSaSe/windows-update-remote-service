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
using System.Runtime.Serialization;

namespace WuDataContract.DTO
{
    /// <summary>
    /// Simplified representation of a windows update.
    /// </summary>
    [DataContract]
    public class UpdateDescription
    {
        /// <summary>
        /// The update is marked as important by microsoft.
        /// </summary>
        [DataMember]
        public bool IsImportant { get; set; }
        /// <summary>
        /// The Update-Identifier. This is not a KB-Number.
        /// </summary>
        [DataMember]
        public string ID { get; set; }
        /// <summary>
        /// Display id/title of the update.
        /// </summary>
        [DataMember]
        public string Title { get; set; }
        /// <summary>
        /// Microsoft's description for the update.
        /// </summary>
        [DataMember]
        public string Description { get; set; }
        /// <summary>
        /// Max size of the update, the effective size depends on the operating system and other factors.
        /// </summary>
        [DataMember]
        public long MaxByteSize { get; set; }
        /// <summary>
        /// Min size of the update, the effective size depends on the operating system and other factors.
        /// </summary>
        [DataMember]
        public long MinByteSize { get; set; }
        /// <summary>
        /// True if the update is installed. The value is not accurate when a reboot is pending to complete
        /// update installations.
        /// </summary>
        [DataMember]
        public bool IsInstalled { get; set; }
        /// <summary>
        /// True if the update is downloaded.
        /// </summary>
        [DataMember]
        public bool IsDownloaded { get; set; }
        /// <summary>
        /// Eula acceptance state. To install the update, the eula of the update must be accepted.
        /// </summary>
        [DataMember]
        public bool EulaAccepted { get; set; }
        /// <summary>
        /// Indicates whether the update is selected for download/installation.
        /// An update can be selected automatically or manually.
        /// </summary>
        [DataMember]
        public bool SelectedForInstallation { get; set; }

        public override bool Equals(object obj)
        {
            UpdateDescription other = obj as UpdateDescription;
            if (other == null) return false;
            if (object.ReferenceEquals(this, other)) return true;
            if (string.IsNullOrEmpty(ID)) return false;
            return ID.Equals(other.ID);
        }

        public override int GetHashCode() => (string.IsNullOrEmpty(ID)) ? base.GetHashCode() : ID.GetHashCode();
    }
}
