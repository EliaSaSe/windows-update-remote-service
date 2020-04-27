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

namespace WindowsUpdateApiController.States
{
    /// <summary>
    /// Represents a transition rule from one <see cref="WuProcessState"/> to an other <see cref="WuProcessState"/> in a <see cref="IWuApiController"/> state machine.
    /// </summary>
    internal class StateTransition
    {
        /// <summary>
        /// Type of the origin state.
        /// </summary>
        public readonly Type FromState;
        /// <summary>
        /// Type of the target state.
        /// </summary>
        public readonly Type ToState;
        /// <summary>
        /// Condition which must be fulfilled, before the state transition is valid.
        /// Is null, when no specific condition must be met before the transition can be executed.
        /// </summary>
        public readonly TransitionCondition Condition;

        public delegate ConditionEvalResult TransitionCondition(WuProcessState currentState);

        /// <summary>
        /// Creates a state transition rule, that allows a transition from one state (<paramref name="fromState"/>) to an other state (<paramref name="toState"/>) 
        /// when a specific <paramref name="condition"/> is fulfilled.
        /// </summary>
        /// <param name="fromState">Type of the origin state. Type must be a subclass of <see cref="WuProcessState"/>.</param>
        /// <param name="toState">Type of the target state. Type must be a subclass of <see cref="WuProcessState"/>.</param>
        /// <param name="condition">Condition which must be fulfilled, before the state transition is valid.</param>
        public StateTransition(Type fromState, Type toState, TransitionCondition condition) : this(fromState, toState)
        {
            if (condition != null) Condition = condition;
        }

        /// <summary>
        /// Creates a state transition rule, that allows a transition from one state (<paramref name="fromState"/>) to an other state (<paramref name="toState"/>).
        /// </summary>
        /// <param name="fromState">Type of the origin state. Type must be a subclass of <see cref="WuProcessState"/>.</param>
        /// <param name="toState">Type of the target state. Type must be a subclass of <see cref="WuProcessState"/>.</param>
        public StateTransition(Type fromState, Type toState)
        {
            if (fromState == null) throw new ArgumentNullException(nameof(fromState));
            if (!fromState.IsSubclassOf(typeof(WuProcessState))) throw new ArgumentException($"{fromState.Name} is not a subclass of {nameof(WuProcessState)}", nameof(fromState));
            if (toState == null) throw new ArgumentNullException(nameof(toState));
            if (!toState.IsSubclassOf(typeof(WuProcessState))) throw new ArgumentException($"{toState.Name} is not a subclass of {nameof(WuProcessState)}", nameof(toState));

            FromState = fromState;
            ToState = toState;
        }

        public override int GetHashCode() => 17 + 31 * FromState.GetHashCode() + 31 * ToState.GetHashCode();

        public override bool Equals(object obj)
        {
            StateTransition other = obj as StateTransition;
            return other != null && this.FromState == other.FromState && this.ToState == other.ToState;
        }

        public override string ToString() => $"{FromState.Name} --> {ToState.Name}, condition: { ((Condition != null) ? "yes" : "no") }";
    }

    /// <summary>
    /// Result of a condition evaluation.
    /// </summary>
    internal struct ConditionEvalResult
    {
        /// <summary>
        /// True, when the condition is met.
        /// </summary>
        public readonly bool IsFulfilled;
        /// <summary>
        /// Message for the user to display, when the state change is not fulfilled.
        /// </summary>
        public readonly string Message;

        public ConditionEvalResult(bool isFulfilled, string message)
        {
            IsFulfilled = isFulfilled;
            Message = message;
        }

        /// <summary>
        /// Returns a condition evaluation result which is fulfilled. (<see cref="IsFulfilled"/>: true, <see cref="Message"/>: null)
        /// </summary>
        public static ConditionEvalResult ValidStateChange { get { return new ConditionEvalResult(true, null); } }
    }
}
