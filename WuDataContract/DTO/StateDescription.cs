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
using System.Runtime.Serialization;
using WuDataContract.Enums;

namespace WuDataContract.DTO
{
    /// <summary>
    /// Describes the internal state of an <see cref="IWuRemoteService"/> instance.
    /// </summary>
    [DataContract]
    public class StateDescription: IEquatable<WuStateId>
    {
        /// <summary>
        /// The progress of the state. Can be null if the state does not have a progress.
        /// </summary>
        [DataMember]
        public ProgressDescription Progress { get; set; }
        /// <summary>
        /// Id of the state.
        /// </summary>
        [DataMember]
        public WuStateId StateId { get; private set; }
        /// <summary>
        /// Display name of the state.
        /// </summary>
        [DataMember]
        public string DisplayName { get; set; }
        /// <summary>
        /// Additional information about the state. Can be null.
        /// </summary>
        [DataMember]
        public string Description { get; set; }
        /// <summary>
        /// Status of the internal update installer component.
        /// </summary>
        [DataMember]
        public InstallerStatus InstallerStatus { get; set; }
        /// <summary>
        /// Informations about the hostsystem.
        /// </summary>
        [DataMember]
        public WuEnviroment Enviroment { get; set; }

        public StateDescription(WuStateId stateId, string displayName, string description, InstallerStatus installerStatus, WuEnviroment enviroment, ProgressDescription progress = null)
        {
            if (String.IsNullOrWhiteSpace(displayName)) throw new ArgumentException("Null or white space not allowed.", nameof(displayName));

            Progress = progress;
            StateId = stateId;
            DisplayName = displayName;
            Description = description;
            InstallerStatus = installerStatus;
            Enviroment = enviroment;
        }

        public bool Equals(WuStateId other) => StateId.Equals(other);

        public override string ToString() => StateId.ToString("G");
    }
}
