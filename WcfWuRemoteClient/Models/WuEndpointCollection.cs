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
using System.Linq;
using CCSWE.Collections.ObjectModel;

namespace WcfWuRemoteClient.Models
{
    /// <summary>
    /// List of <see cref="IWuEndpoint"/>.
    /// </summary>
    public class WuEndpointCollection : INotifyCollectionChanged, ICollection<IWuEndpoint>, IReadOnlyCollection<IWuEndpoint>
    {
        private readonly SynchronizedObservableCollection<IWuEndpoint> _endpoints = 
            new SynchronizedObservableCollection<IWuEndpoint>();
        public event NotifyCollectionChangedEventHandler CollectionChanged;

        public WuEndpointCollection()
        {
            _endpoints.CollectionChanged += (sender, e) => CollectionChanged?.Invoke(this, e);
        }

        public void AddRange(IEnumerable<IWuEndpoint> items)
        {
            if (items == null) throw new ArgumentNullException(nameof(items));
            var distinct = items.Where(e => !_endpoints.Contains(e)).ToList();
            // EndpointCollection.AddRange(distinct); Buggy? 
            // A read lock may not be acquired with the write lock held in this mode.
            foreach (var endpoint in distinct)
            {
                Add(endpoint);
            }
        }

        public void RemoveAndDisposeRange(IEnumerable<IWuEndpoint> endpoints)
        {
            if (endpoints == null) throw new ArgumentNullException(nameof(endpoints));
            // To prevent a InvalidOperationException like "Collection was modified; 
            // enumeration operation may not execute.",
            // create a separate enumeration to allow to remove items from the endpoint list.
            endpoints = endpoints.ToList();
            foreach (var endpoint in endpoints)
            {
                _endpoints.Remove(endpoint);
                endpoint?.Dispose();
            }
        }

        #region ICollection<IWuEndpoint>, IReadOnlyCollection<IWuEndpoint>

        public int Count => _endpoints.Count;
        public bool IsReadOnly => false;
        public void Add(IWuEndpoint item) => _endpoints.Add(item);
        public void Clear() => _endpoints.Clear();
        public bool Contains(IWuEndpoint item) => _endpoints.Contains(item);
        public void CopyTo(IWuEndpoint[] array, int arrayIndex) => _endpoints.CopyTo(array, arrayIndex);

        public bool Remove(IWuEndpoint item) => _endpoints.Remove(item);

        public IEnumerator<IWuEndpoint> GetEnumerator() => _endpoints.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => _endpoints.GetEnumerator();

        #endregion
    }
}
