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
using System.ServiceModel;
using WuDataContract.Interface;

namespace WcfWuRemoteService
{
    /// <summary>
    /// Decouples <see cref="WuRemoteService"/> from the direct use of <see cref="OperationContext"/> and <see cref="IServiceChannel"/>
    /// to allow unit testing.
    /// </summary>
    internal class OperationContextProvider
    {
        /// <summary>
        /// Returns the communication state for the given callback. Override to replace the default behavior.
        /// </summary>
        virtual public CommunicationState GetCommunicationState(IWuRemoteServiceCallback callback)
        {
            if (callback == null) throw new ArgumentNullException(nameof(callback));
            return ((IServiceChannel)callback).State;
        }

        /// <summary>
        /// Returns the callback channel of the current operation context. Override to replace the default behavior.
        /// </summary>
        virtual public IWuRemoteServiceCallback GetCallbackChannel()
        {
            return OperationContext.Current.GetCallbackChannel<IWuRemoteServiceCallback>();
        }
    }
}
