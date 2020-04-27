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
using WcfWuRemoteClient.Models;

namespace WcfWuRemoteClient
{
    /// <summary>
    /// Thrown when a <see cref="IWuEndpoint"/> is not running the last known version of the data contract.
    /// This does not mean, that the endpoint can not be used, but you may need to apply some kind of compatibility mode.
    /// </summary>
    [Serializable]
    internal class EndpointNeedsUpgradeException : Exception
    {
        /// <summary>
        /// The endpoint with an old version of the data contract.
        /// </summary>
        public IWuEndpoint Endpoint { get; private set; }

        internal EndpointNeedsUpgradeException(IWuEndpoint endpoint) : base($"The version of the endpoint {endpoint?.FQDN} is not uptodate.")
        {
            if (endpoint == null) throw new ArgumentNullException(nameof(endpoint));
            Endpoint = endpoint;
        }

        protected EndpointNeedsUpgradeException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
