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
    /// A precondition to switch from one state to an other state are not fulfilled.
    /// </summary>
    public class PreConditionNotFulfilledException : InvalidStateTransitionException
    {
        internal PreConditionNotFulfilledException(WuProcessState from, WuProcessState to, string message, Exception innerException) : base(from, to, message, innerException){}
        internal PreConditionNotFulfilledException(WuProcessState from, WuProcessState to, string message) : base(from, to, message) { }

        internal PreConditionNotFulfilledException(Type from, Type to, string message, Exception innerException) : base(from, to, message, innerException) { }
        internal PreConditionNotFulfilledException(Type from, Type to, string message) : base(from, to, message) { }
    }
}
