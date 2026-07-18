namespace DreamineWeb.Models;

/// <summary>
/// \if KO
/// <para>Doc Member Kind 기능과 관련 상태를 캡슐화합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates doc member kind functionality and related state.</para>
/// \endif
/// </summary>
public enum DocMemberKind
{
    /// <summary>
    /// \if KO
    /// <para>타입 선언을 나타냅니다.</para>
    /// \endif
    /// \if EN
    /// <para>Represents a type declaration.</para>
    /// \endif
    /// </summary>
    Type,

    /// <summary>
    /// \if KO
    /// <para>메서드 선언을 나타냅니다.</para>
    /// \endif
    /// \if EN
    /// <para>Represents a method declaration.</para>
    /// \endif
    /// </summary>
    Method,

    /// <summary>
    /// \if KO
    /// <para>속성 선언을 나타냅니다.</para>
    /// \endif
    /// \if EN
    /// <para>Represents a property declaration.</para>
    /// \endif
    /// </summary>
    Property,

    /// <summary>
    /// \if KO
    /// <para>필드 선언을 나타냅니다.</para>
    /// \endif
    /// \if EN
    /// <para>Represents a field declaration.</para>
    /// \endif
    /// </summary>
    Field,

    /// <summary>
    /// \if KO
    /// <para>이벤트 선언을 나타냅니다.</para>
    /// \endif
    /// \if EN
    /// <para>Represents an event declaration.</para>
    /// \endif
    /// </summary>
    Event
}

/// <summary>
/// \if KO
/// <para>Doc Member 기능과 관련 상태를 캡슐화합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates doc member functionality and related state.</para>
/// \endif
/// </summary>
public sealed class DocMember
{
    /// <summary>
    /// \if KO
    /// <para>Full Name 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the full name value.</para>
    /// \endif
    /// </summary>
    public string FullName { get; set; } = string.Empty;
    /// <summary>
    /// \if KO
    /// <para>Short Name 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the short name value.</para>
    /// \endif
    /// </summary>
    public string ShortName { get; set; } = string.Empty;
    /// <summary>
    /// \if KO
    /// <para>Kind 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the kind value.</para>
    /// \endif
    /// </summary>
    public DocMemberKind Kind { get; set; }
    /// <summary>
    /// \if KO
    /// <para>Summary 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the summary value.</para>
    /// \endif
    /// </summary>
    public string? Summary { get; set; }
    /// <summary>
    /// \if KO
    /// <para>Remarks 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the remarks value.</para>
    /// \endif
    /// </summary>
    public string? Remarks { get; set; }
    /// <summary>
    /// \if KO
    /// <para>Returns 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the returns value.</para>
    /// \endif
    /// </summary>
    public string? Returns { get; set; }
    /// <summary>
    /// \if KO
    /// <para>Params 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the params value.</para>
    /// \endif
    /// </summary>
    public List<DocParam> Params { get; set; } = [];
    /// <summary>
    /// \if KO
    /// <para>Type Name 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the type name value.</para>
    /// \endif
    /// </summary>
    public string? TypeName { get; set; }   // 소속 타입명 (메서드/프로퍼티)
}

/// <summary>
/// \if KO
/// <para>Doc Param 기능과 관련 상태를 캡슐화합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates doc param functionality and related state.</para>
/// \endif
/// </summary>
public sealed class DocParam
{
    /// <summary>
    /// \if KO
    /// <para>Name 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the name value.</para>
    /// \endif
    /// </summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>
    /// \if KO
    /// <para>Description 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the description value.</para>
    /// \endif
    /// </summary>
    public string? Description { get; set; }
}
