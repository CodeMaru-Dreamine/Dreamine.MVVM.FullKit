namespace WeddingPlatform.Models;

/// <summary>
/// \if KO
/// <para>Guestbook Entry 기능과 관련 상태를 캡슐화합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates guestbook entry functionality and related state.</para>
/// \endif
/// </summary>
public sealed class GuestbookEntry
{
    /// <summary>
    /// \if KO
    /// <para>Name 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the name value.</para>
    /// \endif
    /// </summary>
    public string Name { get; set; } = "";
    /// <summary>
    /// \if KO
    /// <para>Contact 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the contact value.</para>
    /// \endif
    /// </summary>
    public string Contact { get; set; } = "";
    /// <summary>
    /// \if KO
    /// <para>Message 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the message value.</para>
    /// \endif
    /// </summary>
    public string Message { get; set; } = "";
    /// <summary>
    /// \if KO
    /// <para>Created Local 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the created local value.</para>
    /// \endif
    /// </summary>
    public DateTime CreatedLocal { get; set; } = DateTime.Now;
}

/// <summary>
/// \if KO
/// <para>Guestbook Entry View 기능과 관련 상태를 캡슐화합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates guestbook entry view functionality and related state.</para>
/// \endif
/// </summary>
public sealed class GuestbookEntryView
{
    /// <summary>
    /// \if KO
    /// <para>Name 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the name value.</para>
    /// \endif
    /// </summary>
    public string Name { get; init; } = "";
    /// <summary>
    /// \if KO
    /// <para>Message 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the message value.</para>
    /// \endif
    /// </summary>
    public string Message { get; init; } = "";
    /// <summary>
    /// \if KO
    /// <para>Created Local 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the created local value.</para>
    /// \endif
    /// </summary>
    public DateTime CreatedLocal { get; init; }
}
