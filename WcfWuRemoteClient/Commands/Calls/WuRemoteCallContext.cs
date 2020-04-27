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
using System.ComponentModel;
using System.Threading.Tasks;
using WcfWuRemoteClient.Models;

namespace WcfWuRemoteClient.Commands.Calls
{
    /// <summary>
    /// Represents the relation between a <see cref="WuRemoteCall"/> and a <see cref="IWuEndpoint"/>.
    /// </summary>
    class WuRemoteCallContext : INotifyPropertyChanged
    {
        WuRemoteCall _call;
        IWuEndpoint _endpoint;
        object _taskLock = new object();
        Task<WuRemoteCallResult> _task;
        TaskStatus _lastTaskStatus;

        readonly string _callName;
        readonly string _endpointName;
        readonly DateTime _date = DateTime.Now;

        /// <summary>
        /// Displayname of the <see cref="WuRemoteCall"/>.
        /// </summary>
        public string CallName
        {
            get { return _callName; }
        }

        /// <summary>
        /// Displayname of the <see cref="IWuEndpoint"/>.
        /// </summary>
        public string EndpointName
        {
            get { return _endpointName; }
        }

        /// <summary>
        /// Date on which a call (described by <see cref="CallName"/>) was send to an endpoint (described by <see cref="EndpointName"/>).
        /// </summary>
        public DateTime Date
        {
            get { return _date; }
        }

        /// <summary>
        /// The result of the call. Is null until the call is completed.
        /// </summary>
        public WuRemoteCallResult Result { get; private set; }

        /// <summary>
        /// Status of the call.
        /// </summary>
        public TaskStatus Status
        {
            get
            {
                lock (_taskLock)
                {
                    return _task?.Status ?? _lastTaskStatus;
                }
            }
        }

        public WuRemoteCallContext(WuRemoteCall call, IWuEndpoint endpoint, Task<WuRemoteCallResult> task)
        {
            if (call == null) throw new ArgumentNullException(nameof(call));
            if (endpoint == null) throw new ArgumentNullException(nameof(endpoint));
            if (task == null) throw new ArgumentNullException(nameof(task));
            if (task.Status != TaskStatus.Created) throw new InvalidOperationException("Task started before added to call context.");

            _callName = call.Name;
            _endpointName = endpoint.FQDN;
            Result = null;

            _call = call;
            _endpoint = endpoint;

            task.ContinueWith((t) => OnTaskFinished(t));
        }

        private void OnTaskFinished(Task<WuRemoteCallResult> task)
        {
            Result = task.Result;
            _lastTaskStatus = task.Status;

            if (task.Exception != null && task.Result == null)
            {
                Result = new WuRemoteCallResult(_endpoint, _call, false, task.Exception, task.Exception.Message);
            }

            _call = null;
            _endpoint = null;

            lock (_taskLock)
            {
                _task = null;
            }

            OnPropertyChanged(nameof(Result));
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
