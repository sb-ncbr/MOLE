/*
 * Copyright (c) 2016 David Sehnal, licensed under MIT license, See LICENSE file for more info.
 */

using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace WebChemistry.Tunnels.WPF
{
    public class BooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter,
                              CultureInfo culture)
        {
            if (value is Boolean)
            {
                return ((bool)value) ? Visibility.Visible : Visibility.Collapsed;
            }

            if (value is Visibility)
            {
                var vis = (Visibility)value;
                return vis == Visibility.Visible;
            }

            return value == null ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter,
                                  CultureInfo culture)
        {
            if (value is Boolean)
            {
                return ((bool)value) ? Visibility.Visible : Visibility.Collapsed;
            }

            if (value is Visibility)
            {
                var vis = (Visibility)value;
                return vis == Visibility.Visible;
            }

            return value == null ? Visibility.Collapsed : Visibility.Visible;
        }
    }
}
