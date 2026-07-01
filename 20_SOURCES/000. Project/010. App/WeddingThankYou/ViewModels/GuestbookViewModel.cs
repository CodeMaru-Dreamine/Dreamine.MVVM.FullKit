using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WeddingThankYou.Models;
using WeddingThankYou.Services;

namespace WeddingThankYou.ViewModels
{
	/// <summary>
	/// \file GuestbookViewModel.cs
	/// \brief 슬러그별 방명록 입력/목록/저장 로직 ViewModel.
	/// \details
	///  - SRP: 화면 상태와 저장소 호출만 담당.
	///  - 노출 정책: 화면에는 Contact를 절대 전달하지 않도록 EntriesView(표시 전용 DTO) 제공.
	///  - 예외 안전: 초기 로드/새로고침/저장에서 예외를 UI 상태로만 전달(앱 풀 크래시 방지).
	/// </summary>
	public sealed class GuestbookViewModel
	{
		private readonly IGuestbookStorage _storage;
		private List<GuestbookEntry> _entries = new();

		/// <summary>\brief DI 생성자.</summary>
		public GuestbookViewModel(IGuestbookStorage storage) => _storage = storage;

		/// <summary>\brief 입력: 이름.</summary>
		public string Name { get; set; } = string.Empty;

		/// <summary>\brief 입력: 연락처(저장만 함, 화면 미노출).</summary>
		public string Contact { get; set; } = string.Empty;

		/// <summary>\brief 입력: 축하/감사 메시지.</summary>
		public string Message { get; set; } = string.Empty;

		private static readonly string[] DefaultMessages =
		{
			"💖 두 분의 앞날에 사랑과 행복이 가득하시길 바랍니다 ✨",
			"🎉 결혼을 진심으로 축하드립니다! 영원히 함께하세요 💍",
			"🌸 오늘의 약속이 평생의 기쁨으로 이어지길 바랍니다 💕",
			"🕊️ 늘 서로를 아끼고 존중하며 행복한 가정을 꾸리시길 🙏",
			"🌈 두 분의 미래가 언제나 밝고 빛나길 바랍니다 ✨"
		};

		/// <summary>\brief 메시지가 비어 있으면 추천 문구 중 하나를 랜덤으로 채웁니다.</summary>
		public void ApplyDefaultMessage()
		{
			if (string.IsNullOrEmpty(Message))
				Message = DefaultMessages[Random.Shared.Next(DefaultMessages.Length)];
		}

		/// <summary>\brief 상태 메시지(UI 알림).</summary>
		public string LastStatus { get; private set; } = string.Empty;

		/// <summary>\brief 저장 중 여부(더블탭 방지).</summary>
		public bool IsSaving { get; private set; }

		/// <summary>
		/// \brief 화면 표시 전용 목록(이름/메시지/일시만 포함, 최신순).
		/// </summary>
		public IReadOnlyList<GuestbookEntryView> EntriesView =>
			_entries.OrderByDescending(e => e.CreatedLocal)
					.Select(e => new GuestbookEntryView { Name = e.Name, Message = e.Message, CreatedLocal = e.CreatedLocal })
					.ToList();

		/// <summary>
		/// \brief 페이지 최초 로드시 슬러그별 저장소에서 목록 로드.
		/// </summary>
		public async Task InitializeAsync(string slug, CancellationToken ct = default)
		{
			try
			{
				var list = await _storage.LoadAsync(slug, ct).ConfigureAwait(false);
				_entries = list.ToList();
				LastStatus = string.Empty;
			}
			catch { LastStatus = "방명록 데이터를 불러오는 중 오류가 발생했어요."; }
		}

		/// <summary>
		/// \brief 등록(추가) + 즉시 저장.
		/// \details 검증(공백 방지) → 메모리 목록 상단 삽입 → 전체 저장소에 저장.
		/// </summary>
		public async Task AddEntryAsync(string slug, CancellationToken ct = default)
		{
			if (IsSaving) return;
			if (string.IsNullOrWhiteSpace(Name)) { LastStatus = "이름을 입력해주세요."; return; }
			if (string.IsNullOrWhiteSpace(Message)) { LastStatus = "메시지를 입력해주세요."; return; }

			var entry = new GuestbookEntry
			{
				Name = Name.Trim(),
				Contact = Contact?.Trim() ?? string.Empty,
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
				Name = string.Empty;
				Contact = string.Empty;
				Message = string.Empty;
				LastStatus = "저장되었습니다. 감사합니다 🙏";
			}
			catch { LastStatus = "저장 중 오류가 발생했어요."; }
			finally { IsSaving = false; }
		}

		/// <summary>\brief 파일에서 다시 불러오기.</summary>
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

	/// <summary>
	/// \file GuestbookEntryView.cs
	/// \brief UI 표시 전용 DTO. Contact 제거.
	/// </summary>
	public sealed class GuestbookEntryView
	{
		/// <summary>\brief 표시용 이름.</summary>
		public string Name { get; init; } = string.Empty;
		/// <summary>\brief 표시용 메시지.</summary>
		public string Message { get; init; } = string.Empty;
		/// <summary>\brief 표시용 로컬 시각.</summary>
		public DateTime CreatedLocal { get; init; }
	}
}
