using System;
using System.Globalization;
using System.Windows.Data;


namespace ISMP_Pluging.ViewChildren
{
    public class MultiplyConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double factor = 1D;
            if (parameter is string fString)
                if (double.TryParse(fString, NumberStyles.Float, CultureInfo.CreateSpecificCulture("en-US"), out double factorParam))
                    factor = factorParam;
            return ((double)value) * factor;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double factor = 1D;
            if (parameter is string fString)
                if (Double.TryParse(fString, NumberStyles.Float, CultureInfo.CreateSpecificCulture("en-US"), out double factorParam))
                    factor = factorParam;
            return ((double)value) / factor;
        }
    }










}


