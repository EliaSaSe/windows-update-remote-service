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
using System.Windows;

namespace WcfWuRemoteClient.InteractionServices
{
    class WpfModalService : IModalService
    {
        public void ShowMessageBox(string message, string caption, MessageType type)
        {
            MessageBox.Show(message, caption, MessageBoxButton.OK, MessageTypeToWPF(type));
        }

        public ModalOptionResult ShowMessageBox(string message, string caption, ModalOption option, MessageType type)
        {
            var result = MessageBox.Show(message, caption, ModalOptionToWPF(option), MessageTypeToWPF(type));
            return WPFToModalOptionResult(result);
        }


        static private MessageBoxImage MessageTypeToWPF(MessageType type)
        {
            switch (type)
            {
                case MessageType.Error:
                    return MessageBoxImage.Error;
                case MessageType.Warning:
                    return MessageBoxImage.Warning;
                case MessageType.Info:
                    return MessageBoxImage.Information;
                case MessageType.Question:
                    return MessageBoxImage.Question;
                default:
                    throw new NotImplementedException(type.ToString("G"));
            }
        }

        static private MessageBoxButton ModalOptionToWPF(ModalOption option)
        {
            switch (option)
            {
                case ModalOption.OK:
                    return MessageBoxButton.OK;
                case ModalOption.OKCancel:
                    return MessageBoxButton.OKCancel;
                default:
                    throw new NotImplementedException(option.ToString("G"));
            }
        }

        static private ModalOptionResult WPFToModalOptionResult(MessageBoxResult result)
        {
            switch (result)
            {
                case MessageBoxResult.OK:
                    return ModalOptionResult.OK;
                case MessageBoxResult.Cancel:
                    return ModalOptionResult.Cancel;
                default:
                    throw new NotImplementedException(result.ToString("G"));
            }
        }

    }
}
