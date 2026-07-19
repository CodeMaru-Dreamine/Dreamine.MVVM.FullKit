using Microsoft.AspNetCore.Hosting;
using System.IO;

namespace DreamineWeb.Ontology.Infrastructure;

/// <summary>Resolves generated ontology artifacts in development and published layouts.</summary>
public interface IOntologyDataPathResolver
{
    string ResolveOntologyDirectory();
}

/// <summary>Resolves the generated source-mirror root without exposing it to Blazor.</summary>
public interface IOntologySourcePathResolver
{
    string ResolveSourceDirectory();
}

/// <summary>Finds published ontology files first, then the repository-level .ua directory.</summary>
public sealed class OntologyDataPathResolver : IOntologyDataPathResolver
{
    private readonly IWebHostEnvironment _environment;

    public OntologyDataPathResolver(IWebHostEnvironment environment)
    {
        _environment = environment ?? throw new ArgumentNullException(nameof(environment));
    }

    /// <inheritdoc />
    public string ResolveOntologyDirectory()
    {
        string published = Path.Combine(_environment.WebRootPath, "understand", "ontology");
        if (File.Exists(Path.Combine(published, "manifest.json")))
            return published;

        DirectoryInfo? current = new(_environment.ContentRootPath);
        while (current is not null)
        {
            string candidate = Path.Combine(current.FullName, ".ua", "ontology");
            if (File.Exists(Path.Combine(candidate, "manifest.json")))
                return candidate;
            current = current.Parent;
        }

        return published;
    }
}

/// <summary>Resolves a fixed directory and is useful for tests and offline hosts.</summary>
public sealed class FixedOntologyDataPathResolver : IOntologyDataPathResolver
{
    private readonly string _directory;

    public FixedOntologyDataPathResolver(string directory)
    {
        _directory = Path.GetFullPath(directory ?? throw new ArgumentNullException(nameof(directory)));
    }

    /// <inheritdoc />
    public string ResolveOntologyDirectory() => _directory;
}

/// <summary>Resolves published source mirrors under the Dreamine.Web web root.</summary>
public sealed class OntologySourcePathResolver : IOntologySourcePathResolver
{
    private readonly IWebHostEnvironment _environment;

    public OntologySourcePathResolver(IWebHostEnvironment environment)
    {
        _environment = environment ?? throw new ArgumentNullException(nameof(environment));
    }

    /// <inheritdoc />
    public string ResolveSourceDirectory()
    {
        string webRoot = string.IsNullOrWhiteSpace(_environment.WebRootPath)
            ? Path.Combine(_environment.ContentRootPath, "wwwroot")
            : _environment.WebRootPath;
        return Path.GetFullPath(Path.Combine(webRoot, "understand", "source"));
    }
}

/// <summary>Resolves a fixed source-mirror directory for tests.</summary>
public sealed class FixedOntologySourcePathResolver : IOntologySourcePathResolver
{
    private readonly string _directory;

    public FixedOntologySourcePathResolver(string directory)
    {
        _directory = Path.GetFullPath(directory ?? throw new ArgumentNullException(nameof(directory)));
    }

    /// <inheritdoc />
    public string ResolveSourceDirectory() => _directory;
}
