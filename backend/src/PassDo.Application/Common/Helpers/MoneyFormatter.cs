using System.Globalization;

namespace PassDo.Application.Common.Helpers;

public static class MoneyFormatter
{
    private static readonly CultureInfo ViCulture = CultureInfo.GetCultureInfo("vi-VN");

    /// <summary>Formats an amount like "60.000 ₫" (vi-VN grouping, no decimals, currency symbol suffix).</summary>
    public static string FormatVnd(decimal amount) => $"{amount.ToString("N0", ViCulture)} \u20ab";
}
