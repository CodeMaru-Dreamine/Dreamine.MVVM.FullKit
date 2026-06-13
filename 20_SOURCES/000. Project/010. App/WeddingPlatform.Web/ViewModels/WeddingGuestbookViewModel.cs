using WeddingPlatform.Models;
using WeddingPlatform.Services;

namespace WeddingPlatform.ViewModels;

public sealed class WeddingGuestbookViewModel
{
    private readonly IGuestbookStorage _storage;
    private List<GuestbookEntry> _entries = new();

    public WeddingGuestbookViewModel(IGuestbookStorage storage) => _storage = storage;

    public string Name { get; set; } = "";
    public string Contact { get; set; } = "";
    public string Message { get; set; } = "";

    private static readonly string[] DefaultMessages =
    [
        "💖 두 분의 앞날에 사랑과 행복이 가득하시길 바랍니다 ✨",
        "🎉 결혼을 진심으로 축하드립니다! 영원히 함께하세요 💍",
        "🌸 오늘의 약속이 평생의 기쁨으로 이어지길 바랍니다 💕",
        "🕊️ 늘 서로를 아끼고 존중하며 행복한 가정을 꾸리시길 🙏",
        "🌈 두 분의 미래가 언제나 밝고 빛나길 바랍니다 ✨"
    ];

    public void ApplyDefaultMessage()
    {
        if (string.IsNullOrEmpty(Message))
            Message = DefaultMessages[Random.Shared.Next(DefaultMessages.Length)];
    }
    public string LastStatus { get; private set; } = "";
    public bool IsSaving { get; private set; }

    public IReadOnlyList<GuestbookEntryView> EntriesView =>
        _entries.OrderByDescending(e => e.CreatedLocal)
                .Select(e => new GuestbookEntryView { Name = e.Name, Message = e.Message, CreatedLocal = e.CreatedLocal })
                .ToList();

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
