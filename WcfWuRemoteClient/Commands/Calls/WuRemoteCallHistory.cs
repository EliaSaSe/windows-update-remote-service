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
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using CCSWE.Collections.ObjectModel;
using WcfWuRemoteClient.Models;

namespace WcfWuRemoteClient.Commands.Calls
{
    /// <summary>
    /// Reperesents a list of <see cref="WuRemoteCallContext"/>.
    /// </summary>
    class WuRemoteCallHistory : INotifyCollectionChanged, INotifyPropertyChanged, 
        ICollection<WuRemoteCallContext>, IReadOnlyCollection<WuRemoteCallContext>
    {
        readonly SynchronizedObservableCollection<WuRemoteCallContext> _callHistory 
            = new SynchronizedObservableCollection<WuRemoteCallContext>();

        public WuRemoteCallHistory()
        {
            _callHistory.CollectionChanged += (sender, e) => CollectionChanged?.Invoke(this, e);
        }

        PropertyChangedEventHandler PropChangedHandler => (sender, e) => PropertyChanged?.Invoke(this, e);

        public void Add(WuRemoteCall call, IWuEndpoint endpoint, Task<WuRemoteCallResult> task) 
            => Add(new WuRemoteCallContext(call, endpoint, task));

        #region ICollection<WuRemoteCallContext>

        public void Add(WuRemoteCallContext item)
        {
            _callHistory.Insert(0, item);
            item.PropertyChanged += PropChangedHandler;
        }
        public void Clear()
        {
            _callHistory.ToList().ForEach(i => i.PropertyChanged -= PropChangedHandler);
            _callHistory.Clear();
        }

        public bool Contains(WuRemoteCallContext item) => _callHistory.Contains(item);

        public void CopyTo(WuRemoteCallContext[] array, int arrayIndex) => _callHistory.CopyTo(array, arrayIndex);

        public bool Remove(WuRemoteCallContext item)
        {
            var result = _callHistory.Remove(item);
            if(result) item.PropertyChanged -= PropChangedHandler;
            return result;
        }

        public IEnumerator<WuRemoteCallContext> GetEnumerator() => _callHistory.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => _callHistory.GetEnumerator();

        public int Count => _callHistory.Count;

        public bool IsReadOnly => false;

        #endregion

        #region INotifyCollectionChanged, INotifyPropertyChanged

        public event NotifyCollectionChangedEventHandler CollectionChanged;
        public event PropertyChangedEventHandler PropertyChanged;

        #endregion


    }
}
