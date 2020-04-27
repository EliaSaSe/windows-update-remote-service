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
namespace WcfWuRemoteClient.InteractionServices
{
    /// <summary>
    /// This interface is to decouple classes from the direct usage of <see cref="MessageBox"/> to allow unit tests.
    /// </summary>
    public interface IModalService
    {
        void ShowMessageBox(string message, string caption, MessageType type);

        ModalOptionResult ShowMessageBox(string message, string caption, ModalOption option, MessageType type);
    }

    /// <summary>
    /// Modal options for <see cref="IModalService"/>.
    /// </summary>
    public enum ModalOption
    {
        OK,
        OKCancel
    }

    /// <summary>
    /// Modal result for <see cref="IModalService"/>.
    /// </summary>
    public enum ModalOptionResult
    {
        OK,
        Cancel
    }

    /// <summary>
    /// Modal message types for <see cref="IModalService"/>.
    /// </summary>
    public enum MessageType
    {
        Error,
        Warning,
        Info,
        Question
    }
}
