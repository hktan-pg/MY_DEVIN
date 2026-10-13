using System.Globalization;
using System.Windows.Data;

namespace MyApp.App.Converters
{
    /// <summary>
    /// Converts a numeric amount into MYR display text, e.g. "RM 1,250.00".
    /// </summary>
    public class CurrencyConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var amount = value switch
            {
                decimal decimalValue => decimalValue,
                double doubleValue => (decimal)doubleValue,
                float floatValue => (decimal)floatValue,
                int intValue => intValue,
                long longValue => longValue,
                _ => 0m
            };

            return CurrencyFormatter.Format(amount);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
