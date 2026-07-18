using Microsoft.Extensions.Configuration;
using System.IO;

namespace DreamineWeb.Services;

/// <summary>
/// \if KO
/// <para>Dreamine.Web에서 제공할 생성 문서의 물리 경로를 찾습니다.</para>
/// \endif
/// \if EN
/// <para>Locates generated documentation directories served by Dreamine.Web.</para>
/// \endif
/// </summary>
public static class DocumentationPathResolver
{
    /// <summary>
    /// \if KO
    /// <para>설정값과 알려진 솔루션 위치를 순서대로 검사해 Doxygen 출력 루트를 반환합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Returns the Doxygen output root by checking configuration and known solution locations in order.</para>
    /// \endif
    /// </summary>
    /// <param name="configuration">
    /// \if KO
    /// <para><c>Documentation:DoxygenRoot</c> 설정을 제공하는 구성입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The configuration that may provide <c>Documentation:DoxygenRoot</c>.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>존재하는 Doxygen 출력 디렉터리의 절대 경로이며, 찾지 못하면 <see langword="null"/>입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The absolute path of an existing Doxygen output directory, or <see langword="null"/> when unavailable.</para>
    /// \endif
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// \if KO
    /// <para><paramref name="configuration"/>이 <see langword="null"/>인 경우 발생합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Thrown when <paramref name="configuration"/> is <see langword="null"/>.</para>
    /// \endif
    /// </exception>
    public static string? ResolveDoxygenRoot(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        string? configuredPath = configuration["Documentation:DoxygenRoot"];
        if (!string.IsNullOrWhiteSpace(configuredPath))
        {
            string absolutePath = Path.GetFullPath(configuredPath);
            if (Directory.Exists(absolutePath))
                return absolutePath;
        }

        string packagedPath = Path.Combine(AppContext.BaseDirectory, "Doxygen");
        if (Directory.Exists(packagedPath))
            return packagedPath;

        return FindSolutionDocumentation(Directory.GetCurrentDirectory())
            ?? FindSolutionDocumentation(AppContext.BaseDirectory);
    }

    /// <summary>
    /// \if KO
    /// <para>시작 디렉터리부터 상위 경로를 탐색해 솔루션의 Doxygen 폴더를 찾습니다.</para>
    /// \endif
    /// \if EN
    /// <para>Searches upward from a starting directory for the solution Doxygen folder.</para>
    /// \endif
    /// </summary>
    /// <param name="startPath">
    /// \if KO
    /// <para>상위 탐색을 시작할 디렉터리입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The directory where the upward search begins.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>발견한 Doxygen 디렉터리 경로이며, 없으면 <see langword="null"/>입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The discovered Doxygen directory path, or <see langword="null"/> when absent.</para>
    /// \endif
    /// </returns>
    private static string? FindSolutionDocumentation(string startPath)
    {
        DirectoryInfo? directory = new(Path.GetFullPath(startPath));
        while (directory is not null)
        {
            string candidate = Path.Combine(directory.FullName, "10_DOCUMENTS", "Doxygen");
            if (Directory.Exists(candidate))
                return candidate;

            directory = directory.Parent;
        }

        return null;
    }
}
