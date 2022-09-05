using System;
using System.Diagnostics;
using System.Globalization;
using System.Windows.Data;

namespace Wpf.BidControls.Converters
{
    public class BiddingRoundToTopConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Debug.Assert(value != null, nameof(value) + " != null");
            return ((int)value * 15) + 2;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
