using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using WeddingThankYou.Models;
using WeddingThankYou.Services;

namespace Dreamine.FullKit.Tests.Wedding;

public sealed class CsvGuestbookStorageTests
{
    [Fact]
    public async Task ExistingLegacyFile_RemainsInPlaceAndContinuesToBeUsed()
    {
        using TestHostEnvironment environment = new();
        string guestbookDirectory = environment.GuestbookDirectory;
        Directory.CreateDirectory(guestbookDirectory);
        string legacyPath = Path.Combine(guestbookDirectory, "existing-couple.csv");
        await File.WriteAllTextAsync(
            legacyPath,
            "Name,Contact,Message,CreatedLocal\r\n" +
            "기존 하객,010-0000-0000,축하합니다,2026-07-28 10:00:00\r\n");
        CsvGuestbookStorage storage = new(environment);

        IReadOnlyList<GuestbookEntry> loaded = await storage.LoadAsync("existing-couple");
        await storage.SaveAsync("existing-couple", loaded);

        Assert.Single(loaded);
        Assert.Equal("기존 하객", loaded[0].Name);
        Assert.True(File.Exists(legacyPath));
        Assert.Single(Directory.EnumerateFiles(guestbookDirectory, "*.csv"));
    }

    [Fact]
    public async Task NewFile_UsesHashedLeafNameAndCannotEscapeGuestbookDirectory()
    {
        using TestHostEnvironment environment = new();
        CsvGuestbookStorage storage = new(environment);
        GuestbookEntry entry = new()
        {
            Name = "하객",
            Contact = "010-0000-0000",
            Message = "축하합니다",
            CreatedLocal = new DateTime(2026, 7, 28, 10, 0, 0)
        };

        await storage.SaveAsync("../../outside", [entry]);

        string csvPath = Assert.Single(
            Directory.EnumerateFiles(environment.GuestbookDirectory, "*.csv"));
        string fileName = Path.GetFileNameWithoutExtension(csvPath);
        Assert.Equal(64, fileName.Length);
        Assert.All(fileName, character => Assert.True(Uri.IsHexDigit(character)));
        Assert.False(File.Exists(Path.Combine(environment.ContentRootPath, "outside.csv")));
    }

    private sealed class TestHostEnvironment : IHostEnvironment, IDisposable
    {
        public TestHostEnvironment()
        {
            ContentRootPath = Path.Combine(
                Path.GetTempPath(),
                "DreamineGuestbookTests",
                Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(ContentRootPath);
            ContentRootFileProvider = new PhysicalFileProvider(ContentRootPath);
        }

        public string GuestbookDirectory =>
            Path.Combine(ContentRootPath, "App_Data", "Guestbook");

        public string EnvironmentName { get; set; } = Environments.Development;

        public string ApplicationName { get; set; } = nameof(CsvGuestbookStorageTests);

        public string ContentRootPath { get; set; }

        public IFileProvider ContentRootFileProvider { get; set; }

        public void Dispose()
        {
            if (ContentRootFileProvider is IDisposable disposable)
            {
                disposable.Dispose();
            }

            Directory.Delete(ContentRootPath, recursive: true);
        }
    }
}
