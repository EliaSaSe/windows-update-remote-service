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
using System.Collections;
using System.Collections.Generic;

namespace WindowsUpdateApiController.States
{
    /// <summary>
    /// Collection of transition rules in a <see cref="IWuApiController"/> state machine.
    /// </summary>
    class StateTransitionCollection : ICollection<StateTransition>
    {
        private List<StateTransition> _items = new List<StateTransition>();

        public int Count => _items.Count;
        /// <summary>
        /// The collection is never readonly.
        /// </summary>
        public bool IsReadOnly => false;

        /// <summary>
        /// Adds a new transition rule to the collection.
        /// </summary>
        /// <param name="item"></param>
        /// <exception cref="ArgumentException">Thrown, when a transition rule with the same <see cref="StateTransition.FromState"/> and <see cref="StateTransition.ToState"/>
        /// states is already present in the collection.</exception>
        public void Add(StateTransition item)
        {
            if (_items.Contains(item)) throw new ArgumentException($"A '{item.FromState.Name} --> {item.ToState.Name}' transition already exisits in the collection.", nameof(item));
            _items.Add(item);
        }

        public void Add<T1, T2>() => Add(new StateTransition(typeof(T1), typeof(T2)));

        public void Add<T1, T2>(StateTransition.TransitionCondition condition) => Add(new StateTransition(typeof(T1), typeof(T2), condition));

        public void Clear() => _items.Clear();

        public bool Contains(StateTransition item) => _items.Contains(item);

        public void CopyTo(StateTransition[] array, int arrayIndex) => _items.CopyTo(array, arrayIndex);

        public IEnumerator<StateTransition> GetEnumerator() => _items.GetEnumerator();

        public bool Remove(StateTransition item) => _items.Remove(item);

        IEnumerator IEnumerable.GetEnumerator() => _items.GetEnumerator();
    }
}
