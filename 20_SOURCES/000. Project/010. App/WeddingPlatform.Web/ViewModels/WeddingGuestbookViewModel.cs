using WeddingPlatform.Models;
using WeddingPlatform.Services;

namespace WeddingPlatform.ViewModels;

/// <summary>
/// \if KO
/// <para>Wedding Guestbook View Model 기능과 관련 상태를 캡슐화합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates wedding guestbook view model functionality and related state.</para>
/// \endif
/// </summary>
public sealed class WeddingGuestbookViewModel
{
    /// <summary>
    /// \if KO
    /// <para>storage 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the storage value.</para>
    /// \endif
    /// </summary>
    private readonly IGuestbookStorage _storage;
    /// <summary>
    /// \if KO
    /// <para>entries 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the entries value.</para>
    /// \endif
    /// </summary>
    private List<GuestbookEntry> _entries = new();

    /// <summary>
    /// \if KO
    /// <para>지정한 설정으로 <see cref="WeddingGuestbookViewModel"/> 클래스의 새 인스턴스를 초기화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Initializes a new instance of the <see cref="WeddingGuestbookViewModel"/> class with the specified settings.</para>
    /// \endif
    /// </summary>
    /// <param name="storage">
    /// \if KO
    /// <para>storage에 사용할 <c>IGuestbookStorage</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>IGuestbookStorage</c> value used for storage.</para>
    /// \endif
    /// </param>
    public WeddingGuestbookViewModel(IGuestbookStorage storage) => _storage = storage;

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
    /// <para>Default Messages 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the default messages value.</para>
    /// \endif
    /// </summary>
    private static readonly string[] DefaultMessages =
    [
        "💖 두 분의 앞날에 사랑과 행복이 가득하시길 바랍니다 ✨",
        "🎉 결혼을 진심으로 축하드립니다! 영원히 함께하세요 💍",
        "🌸 오늘의 약속이 평생의 기쁨으로 이어지길 바랍니다 💕",
        "🕊️ 늘 서로를 아끼고 존중하며 행복한 가정을 꾸리시길 🙏",
        "🌈 두 분의 미래가 언제나 밝고 빛나길 바랍니다 ✨"
    ];

    /// <summary>
    /// \if KO
    /// <para>Apply Default Message 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the apply default message operation.</para>
    /// \endif
    /// </summary>
    public void ApplyDefaultMessage()
    {
        if (string.IsNullOrEmpty(Message))
            Message = DefaultMessages[Random.Shared.Next(DefaultMessages.Length)];
    }
    /// <summary>
    /// \if KO
    /// <para>Last Status 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the last status value.</para>
    /// \endif
    /// </summary>
    public string LastStatus { get; private set; } = "";
    /// <summary>
    /// \if KO
    /// <para>Is Saving 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the is saving value.</para>
    /// \endif
    /// </summary>
    public bool IsSaving { get; private set; }

    /// <summary>
    /// \if KO
    /// <para>Entries View 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the entries view value.</para>
    /// \endif
    /// </summary>
    public IReadOnlyList<GuestbookEntryView> EntriesView =>
        _entries.OrderByDescending(e => e.CreatedLocal)
                .Select(e => new GuestbookEntryView { Name = e.Name, Message = e.Message, CreatedLocal = e.CreatedLocal })
                .ToList();

    /// <summary>
    /// \if KO
    /// <para>Initialize Async 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the initialize async operation.</para>
    /// \endif
    /// </summary>
    /// <param name="slug">
    /// \if KO
    /// <para>slug에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for slug.</para>
    /// \endif
    /// </param>
    /// <param name="ct">
    /// \if KO
    /// <para>취소 요청을 감시하는 토큰입니다.</para>
    /// \endif
    /// \if EN
    /// <para>A token used to observe cancellation requests.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Initialize Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the initialize async operation.</para>
    /// \endif
    /// </returns>
    public async Task InitializeAsync(string slug, CancellationToken ct = default)
    {
        try
        {
            var list = await _storage.LoadAsync(slug, ct).ConfigureAwait(false);
            _entries = list.ToList();
            LastStatus = "";
        }
        catch { LastStatus = "방명록 데이터를 불러오는 중 오류가 발생했어요."; }
    }

    /// <summary>
    /// \if KO
    /// <para>Entry Async 항목을 추가합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Adds the entry async item.</para>
    /// \endif
    /// </summary>
    /// <param name="slug">
    /// \if KO
    /// <para>slug에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for slug.</para>
    /// \endif
    /// </param>
    /// <param name="ct">
    /// \if KO
    /// <para>취소 요청을 감시하는 토큰입니다.</para>
    /// \endif
    /// \if EN
    /// <para>A token used to observe cancellation requests.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Add Entry Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the add entry async operation.</para>
    /// \endif
    /// </returns>
    public async Task AddEntryAsync(string slug, CancellationToken ct = default)
    {
        if (IsSaving) return;
        if (string.IsNullOrWhiteSpace(Name)) { LastStatus = "이름을 입력해주세요."; return; }
        if (string.IsNullOrWhiteSpace(Message)) { LastStatus = "메시지를 입력해주세요."; return; }

        var entry = new GuestbookEntry
        {
            Name = Name.Trim(),
            Contact = Contact.Trim(),
            Message = Message.Trim(),
            CreatedLocal = DateTime.Now
        };

        IsSaving = true;
        try
        {
            var updated = new List<GuestbookEntry> { entry };
            updated.AddRange(_entries);
            await _storage.SaveAsync(slug, updated, ct).ConfigureAwait(false);
            _entries = updated;
            Name = ""; Contact = ""; Message = "";
            LastStatus = "저장되었습니다. 감사합니다 🙏";
        }
        catch { LastStatus = "저장 중 오류가 발생했어요."; }
        finally { IsSaving = false; }
    }

    /// <summary>
    /// \if KO
    /// <para>Reload Async 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the reload async operation.</para>
    /// \endif
    /// </summary>
    /// <param name="slug">
    /// \if KO
    /// <para>slug에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for slug.</para>
    /// \endif
    /// </param>
    /// <param name="ct">
    /// \if KO
    /// <para>취소 요청을 감시하는 토큰입니다.</para>
    /// \endif
    /// \if EN
    /// <para>A token used to observe cancellation requests.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Reload Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the reload async operation.</para>
    /// \endif
    /// </returns>
    public async Task ReloadAsync(string slug, CancellationToken ct = default)
    {
        try
        {
            var list = await _storage.LoadAsync(slug, ct).ConfigureAwait(false);
            _entries = list.ToList();
            LastStatus = "새로고침 완료";
        }
        catch { LastStatus = "새로고침 중 오류가 발생했어요."; }
    }
}
