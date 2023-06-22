using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace LunarMods.Utilities;

public static class StringUtil
{
    public static string TrimInvalid(this string str)
    {
        // Replace invalid characters with empty strings.
        try {
            str = Regex.Replace(str, @"[^\w\.@-]", "",
                RegexOptions.None, TimeSpan.FromSeconds(1.5));
            return Regex.Replace(str, @"\s", "-",
                RegexOptions.None, TimeSpan.FromSeconds(1.5));
        }
        // If we timeout when replacing invalid characters,
        // we should return Empty.
        catch (RegexMatchTimeoutException) {
            return string.Empty;
        }
    }

    public static string AppendId(this string str)
    {
        return str + "_" + DateTime.UtcNow.ToString("yyyy-MM-ddTHHmmss") + "_" + Guid.NewGuid().ToString()[..7];
    }

    public static string JsonString(this string str)
    {
        return str.Replace(@"\", @"\\");
    }

    public static string GetBaseUrl(this HttpRequest request)
    {
        DefaultInterpolatedStringHandler interpolatedStringHandler = new(3, 4);
        interpolatedStringHandler.AppendFormatted(request.Scheme);
        interpolatedStringHandler.AppendLiteral("://");
        interpolatedStringHandler.AppendFormatted(request.Host);
        return interpolatedStringHandler.ToStringAndClear();
    }

    public static string FormatDate(this string str)
    {
        return DateTime.ParseExact(str, "s", CultureInfo.InvariantCulture).ToString("MMMM dd, yyyy");
    }

    public static string NowDate()
    {
        return DateTime.UtcNow.ToString("s", CultureInfo.InvariantCulture);
    }

    public static string BytesToString(this long byteCount)
    {
        string[] suf = { "B", "KB", "MB", "GB", "TB", "PB", "EB" }; //Longs run out around EB
        if (byteCount == 0)
            return "0" + suf[0];
        long bytes = Math.Abs(byteCount);
        int place = Convert.ToInt32(Math.Floor(Math.Log(bytes, 1024)));
        double num = Math.Round(bytes / Math.Pow(1024, place), 1);
        return (Math.Sign(byteCount) * num) + suf[place];
    }

    public static IEnumerable<string> SSplit(this string str)
    {
        return str.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).Select(n => n.Trim());
    }
}
