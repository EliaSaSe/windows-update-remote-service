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
using System.Linq;
using System.Windows.Data;
using WuDataContract.DTO;

namespace WcfWuRemoteClient.Converter
{
    public class UpdateDescArrayToStatsStringConverter : IValueConverter
    {

        public enum StatsConverterParameter { UpdateCount, ImportantUpdateCount, OptionalUpdateCount, SelectedUpdateCount };

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Debug.Assert(value == null || value is UpdateDescription[], $"{nameof(value)} should be a {typeof(UpdateDescription[]).Name}");
            Debug.Assert(targetType == typeof(string));
            Debug.Assert(parameter != null && parameter is StatsConverterParameter);
            if (!(parameter is StatsConverterParameter)) throw new ArgumentException($"Must be a enum of {nameof(StatsConverterParameter)}.", nameof(parameter));

            StatsConverterParameter param = (StatsConverterParameter)parameter;
            UpdateDescription[] updates = value as UpdateDescription[];
            if (updates == null) return 0;

            switch (param)
            {
                case StatsConverterParameter.ImportantUpdateCount:
                    return updates.Count(u => u.IsImportant && !u.IsInstalled);
                case StatsConverterParameter.OptionalUpdateCount:
                    return updates.Count(u => !u.IsImportant && !u.IsInstalled);
                case StatsConverterParameter.SelectedUpdateCount:
                    return updates.Count(u => u.SelectedForInstallation && !u.IsInstalled);
                case StatsConverterParameter.UpdateCount:
                    return updates.Count(u => !u.IsInstalled);
            }
            throw new NotSupportedException($"Value of {parameter} is not supported.");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
