using System.Globalization;

namespace BabyCare.Application;

public static class CareLocalTime
{
    // datetime-local submits an invariant ISO value, independently of the UI language.
    public static bool TryParse(string? value, out DateTime time) => DateTime.TryParseExact(
        value, ["yyyy-MM-dd'T'HH:mm", "yyyy-MM-dd'T'HH:mm:ss", "yyyy-MM-dd'T'HH:mm:ss.FFFFFFF"],
        CultureInfo.InvariantCulture, DateTimeStyles.None, out time);
}
