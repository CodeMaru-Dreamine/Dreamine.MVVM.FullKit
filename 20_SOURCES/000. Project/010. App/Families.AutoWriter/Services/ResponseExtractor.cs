namespace FamiliesAutoWriter.Services;

/// <summary>
/// AI 웹사이트에서 마지막 응답 텍스트를 추출하는 JS 스크립트를 생성합니다.
/// </summary>
public static class ResponseExtractor
{
    public static string BuildExtractScript() => """
        (function() {
            // ── ChatGPT ────────────────────────────────────────────────
            // 마지막 assistant 메시지
            const gptMsgs = document.querySelectorAll('[data-message-author-role="assistant"]');
            if (gptMsgs.length > 0) {
                const last = gptMsgs[gptMsgs.length - 1];
                const content = last.querySelector('.markdown') ?? last;
                return JSON.stringify({ source: 'chatgpt', text: content.innerText.trim() });
            }

            // ── Claude.ai ──────────────────────────────────────────────
            const claudeMsgs = document.querySelectorAll('[data-testid="assistant-message"]');
            if (claudeMsgs.length > 0) {
                const last = claudeMsgs[claudeMsgs.length - 1];
                return JSON.stringify({ source: 'claude', text: last.innerText.trim() });
            }
            // fallback
            const claudeAlt = document.querySelectorAll('.font-claude-message, .prose');
            if (claudeAlt.length > 0) {
                const last = claudeAlt[claudeAlt.length - 1];
                return JSON.stringify({ source: 'claude', text: last.innerText.trim() });
            }

            // ── Gemini ─────────────────────────────────────────────────
            const geminiMsgs = document.querySelectorAll('model-response, .response-container, .model-response-text');
            if (geminiMsgs.length > 0) {
                const last = geminiMsgs[geminiMsgs.length - 1];
                return JSON.stringify({ source: 'gemini', text: last.innerText.trim() });
            }

            return JSON.stringify({ source: 'unknown', text: '' });
        })()
        """;
}
