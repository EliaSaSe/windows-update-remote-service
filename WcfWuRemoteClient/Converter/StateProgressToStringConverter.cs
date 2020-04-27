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
using System.Diagnostics;
using System.Globalization;
using System.Windows.Data;
using WuDataContract.DTO;

namespace WcfWuRemoteClient.Converter
{
    public class StateProgressToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            StateDescription state = value as StateDescription;
            Debug.Assert(state != null, $"{nameof(value)} should be castable to {typeof(StateDescription).Name}");
            Debug.Assert(targetType == typeof(string));

            if (state == null) return String.Empty;
            return (state.Progress != null && !state.Progress.IsIndeterminate)?state.Progress.Percent.ToString()+"%":String.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
