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
using System.Reflection;
using System.Runtime.Serialization;

namespace WuDataContract.DTO
{
    /// <summary>
    /// Descripes the version of a component/module.
    /// Allows clients to use compatibility behaviors when needed.
    /// </summary>
    [DataContract]
    public class VersionInfo : IComparable<VersionInfo>, IComparable
    {
        /// <summary>
        /// Name of the component.
        /// </summary>
        [DataMember]
        public string ComponentName { get; private set; }

        /// <summary>
        /// Major version number of the component.
        /// </summary>
        [DataMember]
        public int Major { get; private set; }

        /// <summary>
        /// Minor version number of the component.
        /// </summary>
        [DataMember]
        public int Minor { get; private set; }

        /// <summary>
        /// Patch/Build-Level of the component.
        /// </summary>
        [DataMember]
        public int Build { get; private set; }

        /// <summary>
        /// Revision-Level of the component.
        /// </summary>
        [DataMember]
        public int Revision { get; private set; }

        /// <summary>
        /// True if the component is the contract definition between windows update remote server and client.
        /// </summary>
        [DataMember]
        public bool IsContract { get; private set; }

        public VersionInfo(string component, int majorVersion, int minorVersion, int build, int revision, bool isContract = false)
        {
            if (String.IsNullOrWhiteSpace(component)) throw new ArgumentNullException(nameof(component));
            if (majorVersion < 0) throw new ArgumentOutOfRangeException($"Must be zero or higher.", nameof(majorVersion));
            if (minorVersion < 0) throw new ArgumentOutOfRangeException($"Must be zero or higher.", nameof(minorVersion));
            if (build < 0) throw new ArgumentOutOfRangeException($"Must be zero or higher.", nameof(build));
            if (revision < 0) throw new ArgumentOutOfRangeException($"Must be zero or higher.", nameof(revision));
            ComponentName = component;
            Major = majorVersion;
            Minor = minorVersion;
            Build = build;
            Revision = revision;
            IsContract = isContract;
        }

        public int CompareTo(VersionInfo other)
        {
            if (other == null) return 1;
            if (ReferenceEquals(this, other)) return 0;
            if (!other.ComponentName.Equals(ComponentName)) return ComponentName.CompareTo(other.ComponentName);
            if (Major != other.Major) return Major.CompareTo(other.Major);
            if (Minor != other.Minor) return Minor.CompareTo(other.Minor);
            if (Build != other.Build) return Build.CompareTo(other.Build);
            return Minor.CompareTo(other.Revision);
        }

        public int CompareTo(object obj)
        {
            if (obj is VersionInfo || obj == null) return CompareTo((VersionInfo)obj);
            throw new ArgumentException($"{nameof(obj)} is not a {nameof(VersionInfo)}.", nameof(obj));
        }

        public override bool Equals(object obj)
        {
            if (obj == null) return false;
            if (Object.ReferenceEquals(this, obj)) return true;
            if (!(obj is VersionInfo)) return false;
            var other = (VersionInfo)obj;
            return ComponentName.Equals(other.ComponentName)
                && Major == other.Major
                && Minor == other.Minor
                && Build == other.Build
                && Revision == other.Revision
                && IsContract == other.IsContract;       
        }

        public override int GetHashCode() => ComponentName.GetHashCode() * (Major * 100000 + Minor * 1000 + Build * 100 + Revision);
        public override string ToString() => $"{ComponentName} {Major}.{Minor}.{Build}.{Revision}";

        public bool HasLowerVersionThan(VersionInfo other, bool ignoreBuildAndRevision = false)
        {
            if (other == null) throw new ArgumentNullException(nameof(other));
            if (Major < other.Major || (Major == other.Major && Minor < other.Minor)) return true;
            if (Major == other.Major && Minor > other.Minor) return false;
            if (Major == other.Major && Minor == other.Minor && ignoreBuildAndRevision) return false;            
            if (Build < other.Build || (Build == other.Build && Revision < other.Revision)) return true;
            return false;
        }

        public bool HasHigherVersionThan(VersionInfo other, bool ignoreBuildAndRevision = false)
        {
            if (other == null) throw new ArgumentNullException(nameof(other));
            if (Major == other.Major && Minor == other.Minor && ignoreBuildAndRevision) return false;
            return !HasLowerVersionThan(other, ignoreBuildAndRevision);
        }

        public static implicit operator VersionInfo(AssemblyName assembly) => FromAssembly(assembly);
        public static VersionInfo FromAssembly(AssemblyName assembly, bool isContract = false)
        {
            if (assembly == null) throw new ArgumentNullException(nameof(assembly));
            return new VersionInfo(
                assembly.Name,
                assembly.Version.Major,
                assembly.Version.Minor,
                assembly.Version.Build,
                assembly.Version.Revision,
                isContract);
        }

    }
}
