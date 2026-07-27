using Dreamine.AppSecurity;

namespace Dreamine.FullKit.Tests.Security;

public sealed class StoragePathGuardTests
{
    public static TheoryData<string> InvalidIdentifiers => new()
    {
        string.Empty,
        " ",
        "../escape",
        @"..\escape",
        "/rooted",
        @"C:\rooted",
        @"\\server\share",
        "contains/slash",
        @"contains\slash",
        "safe%2fescape",
        "safe%5cescape",
        "safe%2e%2e",
        "control\u0001",
        new string('a', StoragePathGuard.MaxIdentifierLength + 1)
    };

    [Theory]
    [MemberData(nameof(InvalidIdentifiers))]
    public void ResolveIdentifierDirectory_RejectsUnsafeIdentifiers(string identifier)
    {
        var root = Path.Combine(Path.GetTempPath(), "dreamine-path-guard");

        Assert.ThrowsAny<ArgumentException>(
            () => StoragePathGuard.ResolveIdentifierDirectory(root, identifier, "identifier"));
    }

    [Fact]
    public void ResolveIdentifierDirectory_AllowsNormalizedUnicodeLettersAndSafePunctuation()
    {
        var root = Path.Combine(Path.GetTempPath(), "dreamine-path-guard");

        var actual = StoragePathGuard.ResolveIdentifierDirectory(root, "가족-2026_01", "identifier");

        Assert.Equal(
            Path.GetFullPath(Path.Combine(root, "가족-2026_01")),
            actual);
    }

    [Fact]
    public void ResolveUnderRoot_RejectsSiblingPrefixEscape()
    {
        var root = Path.Combine(Path.GetTempPath(), "dreamine-root");

        Assert.Throws<InvalidOperationException>(
            () => StoragePathGuard.ResolveUnderRoot(root, "..", "dreamine-root-sibling", "payload.json"));
    }

    [Fact]
    public void ResolveUnderRoot_RejectsRootedAndUncSegments()
    {
        var root = Path.Combine(Path.GetTempPath(), "dreamine-root");

        Assert.Throws<ArgumentException>(
            () => StoragePathGuard.ResolveUnderRoot(root, Path.GetPathRoot(root)!));
        Assert.Throws<ArgumentException>(
            () => StoragePathGuard.ResolveUnderRoot(root, @"\\server\share"));
    }

    [Fact]
    public void ResolveIdentifierFile_ProducesAContainedFilePath()
    {
        var root = Path.Combine(Path.GetTempPath(), "dreamine-root");

        var actual = StoragePathGuard.ResolveIdentifierFile(root, "a1b2_c3-d4", ".json", "id");
        var relative = Path.GetRelativePath(Path.GetFullPath(root), actual);

        Assert.Equal("a1b2_c3-d4.json", relative);
    }
}
