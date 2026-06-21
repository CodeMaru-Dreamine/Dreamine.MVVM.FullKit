namespace FamiliesAutoWriter.Services;

public static class ResponseExtractor
{
    public static string BuildExtractScript() => """
        (function() {
            // 응답 영역에서 텍스트를 추출하는 함수
            // completed: 마지막 섹션인 ===VIDEO=== 까지 나와야 완료
            function makeResult(source, text) {
                // 시작 마커(===TITLE=== 또는 ===DETAIL_*_TITLE===)와
                // 종료 마커(===VIDEO===)가 모두 있어야 완료로 판정
                // → 이전 대화 텍스트가 섞여 오탐되는 것 방지
                const hasEnd   = text.includes('===VIDEO===');
                const hasStart = text.includes('===TITLE===') || /===DETAIL_\S+_TITLE===/.test(text);
                const completed = hasEnd && hasStart;
                return JSON.stringify({ source, text, completed });
            }

            // ── ChatGPT (다중 셀렉터 폴백) ────────────────────────────
            const gptSelectors = [
                '[data-message-author-role="assistant"]',
                'article[data-testid*="conversation-turn"]:not([data-testid*="user"])',
                '.agent-turn',
                'div[class*="agent-turn"]',
                '[data-role="assistant"]',
            ];
            for (const sel of gptSelectors) {
                const msgs = document.querySelectorAll(sel);
                if (msgs.length > 0) {
                    const last = msgs[msgs.length - 1];
                    const prose = last.querySelector('.prose')
                               ?? last.querySelector('[class*="markdown"]')
                               ?? last.querySelector('div[class*="prose"]')
                               ?? last;
                    const text = prose.innerText.trim();
                    if (text.length > 10) return makeResult('chatgpt', text);
                }
            }

            // ── Claude.ai ──────────────────────────────────────────────
            for (const sel of ['[data-testid="assistant-message"]', '.font-claude-message', '.prose']) {
                const msgs = document.querySelectorAll(sel);
                if (msgs.length > 0) {
                    const text = msgs[msgs.length - 1].innerText.trim();
                    if (text.length > 10) return makeResult('claude', text);
                }
            }

            // ── Gemini ─────────────────────────────────────────────────
            const geminiMsgs = document.querySelectorAll('model-response, .response-container, .model-response-text');
            if (geminiMsgs.length > 0) {
                const text = geminiMsgs[geminiMsgs.length - 1].innerText.trim();
                if (text.length > 10) return makeResult('gemini', text);
            }

            // 아직 응답 없음
            return JSON.stringify({ source: 'unknown', text: '', completed: false });
        })()
        """;
}
