namespace DreamineWeb.Models;

/// <summary>
/// \if KO
/// <para>Playground Demo 기능과 관련 상태를 캡슐화합니다.</para>
/// \endif
/// \if EN
/// <para>One control demo on the playground page. The live interaction is fixed in code by Id; the rest of the content (title, description, code snippets, and media) can be edited in the admin page.</para>
/// \endif
/// </summary>
public sealed class PlaygroundDemo
{
    /// <summary>
    /// \if KO
    /// <para>Id 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Key matched with the live render fragment, such as "button" or "checkbox".</para>
    /// \endif
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// \if KO
    /// <para>Title 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the title value.</para>
    /// \endif
    /// </summary>
    public string Title { get; set; } = string.Empty;
    /// <summary>
    /// \if KO
    /// <para>Title En 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the title en value.</para>
    /// \endif
    /// </summary>
    public string? TitleEn { get; set; }
    /// <summary>
    /// \if KO
    /// <para>Description 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the description value.</para>
    /// \endif
    /// </summary>
    public string Description { get; set; } = string.Empty;
    /// <summary>
    /// \if KO
    /// <para>Description En 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the description en value.</para>
    /// \endif
    /// </summary>
    public string? DescriptionEn { get; set; }

    /// <summary>
    /// \if KO
    /// <para>Sort Order 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the sort order value.</para>
    /// \endif
    /// </summary>
    public int SortOrder { get; set; }
    /// <summary>
    /// \if KO
    /// <para>Is Visible 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the is visible value.</para>
    /// \endif
    /// </summary>
    public bool IsVisible { get; set; } = true;

    /// <summary>
    /// \if KO
    /// <para>Nav Label 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Short label shown in the side navigation. Falls back to Title when empty.</para>
    /// \endif
    /// </summary>
    public string? NavLabel { get; set; }

    // -- Platform code snippets ----------------------------
    /// <summary>
    /// \if KO
    /// <para>Blazor Code 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the blazor code value.</para>
    /// \endif
    /// </summary>
    public string BlazorCode { get; set; } = string.Empty;
    /// <summary>
    /// \if KO
    /// <para>Wpf Code 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the wpf code value.</para>
    /// \endif
    /// </summary>
    public string WpfCode { get; set; } = string.Empty;
    /// <summary>
    /// \if KO
    /// <para>Win Forms Code 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the win forms code value.</para>
    /// \endif
    /// </summary>
    public string WinFormsCode { get; set; } = string.Empty;
    /// <summary>
    /// \if KO
    /// <para>Maui Code 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the maui code value.</para>
    /// \endif
    /// </summary>
    public string MauiCode { get; set; } = string.Empty;
    /// <summary>
    /// \if KO
    /// <para>Vm Code 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the vm code value.</para>
    /// \endif
    /// </summary>
    public string VmCode { get; set; } = string.Empty;

    // -- Running screen media paths ------------------------
    /// <summary>
    /// \if KO
    /// <para>Wpf Shot 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the wpf shot value.</para>
    /// \endif
    /// </summary>
    public string WpfShot { get; set; } = string.Empty;
    /// <summary>
    /// \if KO
    /// <para>Win Forms Shot 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the win forms shot value.</para>
    /// \endif
    /// </summary>
    public string WinFormsShot { get; set; } = string.Empty;
    /// <summary>
    /// \if KO
    /// <para>Maui Shot 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the maui shot value.</para>
    /// \endif
    /// </summary>
    public string MauiShot { get; set; } = string.Empty;

    /// <summary>
    /// \if KO
    /// <para>Updated At 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the updated at value.</para>
    /// \endif
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
