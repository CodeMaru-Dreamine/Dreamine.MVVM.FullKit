namespace Codemaru.Models;

public sealed record CardHybridUser(
    string Id,
    string Email,
    string DisplayName,
    string PasswordHash,
    DateTime SignedInAt)
{
    public static CardHybridUser Guest { get; } = new(
        Id: "guest",
        Email: string.Empty,
        DisplayName: "Guest",
        PasswordHash: string.Empty,
        SignedInAt: DateTime.Now);
}
