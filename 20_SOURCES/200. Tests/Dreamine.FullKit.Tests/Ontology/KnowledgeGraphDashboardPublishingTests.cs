using System.Text;
using System.Text.RegularExpressions;
using Xunit;

namespace Dreamine.FullKit.Tests.Ontology;

/// <summary>Guards the static knowledge-graph viewer against broken publish bundles.</summary>
public sealed partial class KnowledgeGraphDashboardPublishingTests
{
    [Fact]
    public async Task DashboardEntry_IsValidHtmlAndEveryLocalBundleReferenceExists()
    {
        string root = FindRepositoryRoot();
        string graphRoot = Path.Combine(
            root,
            "20_SOURCES", "000. Project", "010. App", "Dreamine.Web",
            "wwwroot", "understand", "graph");
        string indexPath = Path.Combine(graphRoot, "index.html");
        string html = await File.ReadAllTextAsync(indexPath, Encoding.UTF8);

        int titleStart = html.IndexOf("<title>", StringComparison.OrdinalIgnoreCase);
        int titleEnd = html.IndexOf("</title>", StringComparison.OrdinalIgnoreCase);
        int bodyStart = html.IndexOf("<body>", StringComparison.OrdinalIgnoreCase);

        Assert.True(titleStart >= 0, "The dashboard entry is missing <title>.");
        Assert.True(titleEnd > titleStart, "The dashboard entry has an unclosed <title>; the browser will treat the app as title text.");
        Assert.True(bodyStart > titleEnd, "The dashboard body must follow the closed title element.");
        Assert.Contains("<div id=\"root\"></div>", html, StringComparison.Ordinal);

        MatchCollection references = LocalGraphAssetRegex().Matches(html);
        Assert.NotEmpty(references);
        Assert.Contains("<script type=\"module\"", html, StringComparison.OrdinalIgnoreCase);

        foreach (Match reference in references)
        {
            string webPath = reference.Groups["path"].Value;
            string relativePath = webPath["/understand/graph/".Length..]
                .Replace('/', Path.DirectorySeparatorChar);
            FileInfo asset = new(Path.Combine(graphRoot, relativePath));
            Assert.True(asset is { Exists: true, Length: > 0 }, $"Missing dashboard bundle asset: {webPath}");
        }
    }

    [Fact]
    public void DashboardPublisher_IsAsciiSafeForWindowsPowerShell51()
    {
        string scriptPath = Path.Combine(
            FindRepositoryRoot(),
            "50_SETUP", "UnderstandAnything", "Publish-UnderstandDashboard.ps1");
        byte[] script = File.ReadAllBytes(scriptPath);

        Assert.DoesNotContain(script, value => value > 0x7f);
    }

    [GeneratedRegex("(?:src|href)=\\\"(?<path>/understand/graph/[^\\\"]+)\\\"", RegexOptions.IgnoreCase)]
    private static partial Regex LocalGraphAssetRegex();

    private static string FindRepositoryRoot()
    {
        foreach (string start in new[] { AppContext.BaseDirectory, Directory.GetCurrentDirectory() })
        {
            DirectoryInfo? current = new(start);
            while (current is not null)
            {
                if (Directory.Exists(Path.Combine(current.FullName, ".ua")))
                    return current.FullName;
                current = current.Parent;
            }
        }

        throw new DirectoryNotFoundException("Could not find the repository root.");
    }
}
