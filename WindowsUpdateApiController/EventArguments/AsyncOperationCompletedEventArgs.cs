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

namespace WindowsUpdateApiController.EventArguments
{
    public class AsyncOperationCompletedEventArgs : EventArgs
    {
        /// <summary>
        /// Operation which completes.
        /// </summary>
        public readonly AsyncOperation Operation;
        /// <summary>
        /// The resulting state of the operation.
        /// </summary>
        public readonly WuStateId Result;

        public AsyncOperationCompletedEventArgs(AsyncOperation operation, WuStateId result)
        {
            Operation = operation;
            Result = result;
        }

        public override string ToString() => $"{Operation.ToString()} with result {Result.ToString()}";
    }
}
