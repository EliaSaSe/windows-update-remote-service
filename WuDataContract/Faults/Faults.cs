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
using System.Runtime.Serialization;
using System.ServiceModel;

namespace WuDataContract.Faults
{
    /// <summary>
    /// Baseclass for wcf fault details.
    /// </summary>
    [DataContract]
    public abstract class WuRemoteServiceFault
    {
        public WuRemoteServiceFault(Exception e)
        {
            if (e == null) throw new ArgumentNullException(nameof(e));
            Message = e.Message;
        }

        /// <summary>
        /// The error/fault message.
        /// </summary>
        [DataMember]
        public string Message { get; set; }
    }

    /// <summary>
    /// A state change to the desired state is not allowed.
    /// </summary>
    [DataContract]
    public class InvalidStateTransitionFault : WuRemoteServiceFault
    {
        internal InvalidStateTransitionFault(Exception e) : base(e){}

        public static FaultException<InvalidStateTransitionFault> GetFault(Exception e)
        {
            return new FaultException<InvalidStateTransitionFault>(new InvalidStateTransitionFault(e), e.Message, new FaultCode("InvalidTransition"));
        }
    }

    /// <summary>
    /// Minimum one condition to switch the internal state of the service (<see cref="WuStateId"/>) was not fullfilled.
    /// </summary>
    [DataContract]
    public class PreConditionNotFulfilledFault : InvalidStateTransitionFault
    {
        internal PreConditionNotFulfilledFault(Exception e) : base(e) { }

        public new static FaultException<PreConditionNotFulfilledFault> GetFault(Exception e)
        {
            return new FaultException<PreConditionNotFulfilledFault>(new PreConditionNotFulfilledFault(e), e.Message, new FaultCode("PreConditionNotFulfilled"));
        }
    }

    /// <summary>
    /// The requested update was not found.
    /// </summary>
    [DataContract]
    public class UpdateNotFoundFault : WuRemoteServiceFault
    {
        public readonly string UpdateId;

        internal UpdateNotFoundFault(Exception e, string updateId) : base(e) {
            UpdateId = updateId;
        }

        public static FaultException<UpdateNotFoundFault> GetFault(Exception e, string updateId)
        {
            return new FaultException<UpdateNotFoundFault>(new UpdateNotFoundFault(e, updateId), e.Message, new FaultCode("UpdateNotFound"));
        }
    }

    /// <summary>
    /// An argument was invalid or not allowed within the current program state.
    /// </summary>
    [DataContract]
    public class BadArgumentFault : WuRemoteServiceFault
    {
        internal BadArgumentFault(ArgumentException e): base(e)
        {
            Parameter = e.ParamName;
        }

        /// <summary>
        /// Name of the parameter.
        /// </summary>
        [DataMember]
        public string Parameter { get; set; }

        public static FaultException<BadArgumentFault> GetFault(ArgumentException e)
        {
            return new FaultException<BadArgumentFault>(new BadArgumentFault(e), e.Message, new FaultCode("BadArgument"));
        }
    }

    /// <summary>
    /// An error occured while communicating with the windows update service or an error occured inside of the windows update service.
    /// </summary>
    [DataContract]
    public class ApiFault : WuRemoteServiceFault
    {
        internal ApiFault(System.Runtime.InteropServices.COMException e) : base(e)
        {
            HResult = e.HResult;
        }

        /// <summary>
        /// HResult of the error.
        /// </summary>
        [DataMember]
        public int HResult { get; set; }

        public static FaultException<ApiFault> GetFault(System.Runtime.InteropServices.COMException e)
        {
            return new FaultException<ApiFault>(new ApiFault(e), e.Message, new FaultCode("ApiFault"));
        }
    }
}
