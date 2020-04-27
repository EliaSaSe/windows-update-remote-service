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
using WuDataContract.DTO;
using WuDataContract.Enums;

namespace WindowsUpdateApiController.EventArguments
{
    public class ProgressChangedEventArgs : EventArgs
    {
        public readonly WuStateId StateId;
        public readonly ProgressDescription Progress;

        public ProgressChangedEventArgs(WuStateId stateId, ProgressDescription progress)
        {
            StateId = stateId;
            Progress = progress;
        }

        public override string ToString() => $"{StateId.ToString()}: {Progress?.Percent}% (IsIndeterminate: {Progress?.IsIndeterminate})";

    }
}
