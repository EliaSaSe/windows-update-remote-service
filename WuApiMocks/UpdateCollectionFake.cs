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
using System.Linq;
using WindowsUpdateApiController.Helper;
using WUApiLib;

namespace WuApiMocks
{
    public class UpdateCollectionFake : UpdateCollection
    {
        public class Factory : UpdateCollectionFactory
        {
            public override IUpdateCollection GetInstance() => new UpdateCollectionFake();
        }

        List<IUpdate> _items = new List<IUpdate>();

        public IUpdate this[int index]
        {
            get { return _items.ElementAt(index); }
            set { _items[index] = value; }
        }

        public int Count => _items.Count;

        public bool ReadOnly => false;

        public int Add(IUpdate value)
        {
            _items.Add(value);
            return _items.Count - 1;
        }

        public void Clear() => _items.Clear();

        public UpdateCollection Copy()
        {
            throw new NotImplementedException();
        }

        public IEnumerator GetEnumerator() => _items.GetEnumerator();

        public void Insert(int index, IUpdate value) => _items.Insert(index, value);

        public void RemoveAt(int index) => _items.RemoveAt(index);
    }
}
