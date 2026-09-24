using BabyCare.Domain;

namespace BabyCare.Application;

public interface ICareInterpreter
{
    Task<ParseResult> ParseAsync(string text, DateTimeOffset now, IReadOnlyList<CareEntry> context, CancellationToken cancellationToken, string language = "ko");
}
