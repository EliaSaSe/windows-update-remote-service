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

namespace WcfWuRemoteClient.Commands.Calls
{
    /// <summary>
    /// Represents the result of a call execution on an endpoint.
    /// </summary>
    class WuRemoteCallResult
    {
        readonly bool _success;
        readonly IWuEndpoint _endpoint;
        readonly WuRemoteCall _call;
        readonly Exception _failure;
        readonly String _message;

        /// <summary>
        /// True, when the endpoint has executed the call successfully.
        /// </summary>
        public bool Success { get { return _success; } }
        /// <summary>
        /// The traget endpoint for the call.
        /// </summary>
        public IWuEndpoint Endpoint { get { return _endpoint; } }
        /// <summary>
        /// The call which was send to the endpoint.
        /// </summary>
        public WuRemoteCall Call { get { return _call; } }
        /// <summary>
        /// The exception occurred during the call execution. Null if none was thrown.
        /// </summary>
        public Exception Failure { get { return _failure; } }
        /// <summary>
        /// A result message for the user.
        /// </summary>
        public string Message { get { return _message; } }

        public WuRemoteCallResult(IWuEndpoint endpoint, WuRemoteCall call, bool success, Exception failure, string message)
        {
            if (call == null) throw new ArgumentNullException(nameof(call));

            _endpoint = endpoint;
            _call = call;
            _success = success;
            _failure = failure;
            _message = (message == null && failure != null) ? _failure.Message : message;
        }

        /// <summary>
        /// Returns a successful call result with an empty <see cref="Message"/>.
        /// </summary>
        /// <param name="endpoint">The target endpoint.</param>
        /// <param name="call">The call which was send to the endpoint.</param>
        public static WuRemoteCallResult SuccessResult(IWuEndpoint endpoint, WuRemoteCall call)
        {
            return SuccessResult(endpoint, call, "");
        }

        /// <summary>
        /// Returns a successful call result with the given message.
        /// </summary>
        /// <param name="endpoint">The target endpoint.</param>
        /// <param name="call">The call which was send to the endpoint.</param>
        /// <param name="message">Message for the user.</param>
        public static WuRemoteCallResult SuccessResult(IWuEndpoint endpoint, WuRemoteCall call, string message)
        {
            return new WuRemoteCallResult(endpoint, call, true, null, message);
        }

        /// <summary>
        /// Returns a unsuccessful call result with a 'service not available' message.
        /// </summary>
        /// <param name="endpoint">The target endpoint.</param>
        /// <param name="call">The call which was send to the endpoint.</param>
        public static WuRemoteCallResult EndpointNotAvailableResult(IWuEndpoint endpoint, WuRemoteCall call)
        {
            return new WuRemoteCallResult(endpoint, call, false, null, "The remote service is currently not available.");
        }
    }
}
