using System.Globalization;

namespace MyApp.App
{
    /// <summary>
    /// Single place where money is turned into text so every financial value in the
    /// application is shown consistently using Malaysian Ringgit, e.g. "RM 1,250.00".
    /// </summary>
    public static class CurrencyFormatter
    {
        /// <summary>Default currency symbol used by MyApp.</summary>
        public const string CurrencySymbol = "RM";

        /// <summary>
        /// Formats an amount as RM with thousands separators and two decimals,
        /// e.g. 1250 -> "RM 1,250.00", -1250 -> "-RM 1,250.00".
        /// </summary>
        public static string Format(decimal amount)
        {
            var absolute = Math.Abs(amount).ToString("N2", CultureInfo.InvariantCulture);
            return amount < 0
                ? $"-{CurrencySymbol} {absolute}"
                : $"{CurrencySymbol} {absolute}";
        }

        /// <summary>Formats a double amount (used by chart axes) as MYR text.</summary>
        public static string Format(double amount)
        {
            return Format((decimal)amount);
        }
    }
}
