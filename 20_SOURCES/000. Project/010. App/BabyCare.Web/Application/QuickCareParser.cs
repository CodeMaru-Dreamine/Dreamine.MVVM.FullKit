using System.Text.RegularExpressions;
using System.Globalization;
using BabyCare.Domain;

namespace BabyCare.Application;

// Only complete, unambiguous current-time commands. Other input stays with the AI.
public static class QuickCareParser
{
    public static CareEntry? TryParse(string text, DateTimeOffset now)
    {
        if (CareLanguages.TryFeeding(text, now) is { } feeding) return feeding;
        var compact = Regex.Replace(text, @"\s+", "");
        var temperature = Regex.Match(compact,
            @"^(?:(?<period>오전|오후)(?<hour>\d{1,2})시(?:(?<minute>\d{1,2})분)?|지금)?(?:체온|온도)(?<value>\d{2}(?:[.점]\d)?)도(?:야|예요|입니다)?[.!。]?$",
            RegexOptions.CultureInvariant);
        if (temperature.Success)
        {
            var at = now;
            if (temperature.Groups["hour"].Success)
            {
                var hour = int.Parse(temperature.Groups["hour"].Value);
                var minute = temperature.Groups["minute"].Success ? int.Parse(temperature.Groups["minute"].Value) : 0;
                if (hour is < 1 or > 12 || minute > 59) return null;
                hour = hour % 12 + (temperature.Groups["period"].Value == "오후" ? 12 : 0);
                at = new DateTimeOffset(now.Year, now.Month, now.Day, hour, minute, 0, now.Offset);
                if (at > now) return null;
            }
            return new CareEntry { Kind = "temperature", StartedAt = at,
                TemperatureC = decimal.Parse(temperature.Groups["value"].Value.Replace('점', '.'), CultureInfo.InvariantCulture) };
        }
        var match = Regex.Match(compact,
            @"^(?:지금)?기저(?:귀|기)(?:교체|교환|갈았어|갈았어요)(?<detail>소변대변|대변소변|소변\+대변|대변\+소변|둘다|대변|소변)[.!。]?$",
            RegexOptions.CultureInvariant);
        if (!match.Success) return null;
        var detail = match.Groups["detail"].Value;
        return new CareEntry { Kind = "diaper", StartedAt = now,
            Detail = detail == "대변" ? "dirty" : detail == "소변" ? "wet" : "mixed" };
    }
}
