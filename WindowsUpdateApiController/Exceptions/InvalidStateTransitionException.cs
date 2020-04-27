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
using WindowsUpdateApiController.States;

namespace WindowsUpdateApiController.Exceptions
{
    /// <summary>
    /// A state change to desired state is not valid.
    /// </summary>
    [Serializable]
    public class InvalidStateTransitionException : InvalidOperationException
    {

        internal readonly WuProcessState From, To;
        internal readonly Type FromType, ToType;

        internal InvalidStateTransitionException(WuProcessState from, WuProcessState to, Exception innerException) : base($"Not allowed: Transition from '{from.DisplayName}' to '{to.DisplayName}'.", innerException){
            From = from;
            To = to;
        }

        internal InvalidStateTransitionException(WuProcessState from, WuProcessState to, string message, Exception innerException) : base(message, innerException)
        {
            From = from;
            To = to;
        }

        internal InvalidStateTransitionException(WuProcessState from, WuProcessState to) : this(from, to, innerException: null){}

        internal InvalidStateTransitionException(WuProcessState from, WuProcessState to, string message) : this(from, to, message, null){}

        public InvalidStateTransitionException(Type from, Type to, Exception innerException) : base($"Not allowed: Transition from '{from.Name}' to '{to.Name}'.", innerException)
        {
            FromType = from;
            ToType = to;
        }

        public InvalidStateTransitionException(Type from, Type to, string message, Exception innerException) : base(message, innerException)
        {
            FromType = from;
            ToType = to;
        }

        public InvalidStateTransitionException(Type from, Type to) : this(from, to, innerException: null) { }

        public InvalidStateTransitionException(Type from, Type to, string message) : this(from, to, message, null) { }

    }
}
