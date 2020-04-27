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
using WuDataContract.Enums;

namespace WindowsUpdateApiController.States
{

    internal abstract class WuProcessState
    {
        private string _displayName;
        private string _stateDesc;
        public readonly WuStateId StateId;

        public WuProcessState(WuStateId id, string displayName)
        {
            if (String.IsNullOrWhiteSpace(displayName))
                throw new ArgumentNullException(nameof(displayName));

            _displayName = displayName;
            StateId = id;
        }

        /// <summary>
        /// Display name of the state.
        /// </summary>
        public string DisplayName { get { return _displayName; } }

        /// <summary>
        /// Called when this state becomes the current state.
        /// </summary>
        /// <param name="oldState">thr previous state, can be null, if its the first state</param>
        public abstract void EnterState(WuProcessState oldState);

        /// <summary>
        /// Called when this state is not longer the current state.
        /// </summary>
        public abstract void LeaveState();

        /// <summary>
        /// Description to give the user information about the internal state.
        /// </summary>
        public string StateDesc
        {
            get
            {
                return (_stateDesc == null) ? String.Empty : _stateDesc;
            }
            protected set
            {
                _stateDesc = value;
            }
        }
    }
}
