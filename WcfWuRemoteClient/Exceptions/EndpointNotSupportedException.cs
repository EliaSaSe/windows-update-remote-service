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

namespace WcfWuRemoteClient
{
    /// <summary>
    /// The endpoint is using a data contract that is not compatible with this client.
    /// </summary>
    [Serializable]
    public class EndpointNotSupportedException : PlatformNotSupportedException
    {
        public EndpointNotSupportedException() { }
        public EndpointNotSupportedException(string message) : base(message) { }
        public EndpointNotSupportedException(string message, Exception inner) : base(message, inner) { }
        protected EndpointNotSupportedException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
