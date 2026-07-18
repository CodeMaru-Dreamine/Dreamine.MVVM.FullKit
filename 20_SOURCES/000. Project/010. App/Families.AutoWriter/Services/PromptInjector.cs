namespace FamiliesAutoWriter.Services;

/// <summary>
/// \if KO
/// <para>각 AI 웹사이트의 DOM에 프롬프트를 주입하고 전송하는 JS 코드를 생성합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates prompt injector functionality and related state.</para>
/// \endif
/// </summary>
public static class PromptInjector
{
    /// <summary>
    /// \if KO
    /// <para>Inject Script 값을 구성합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Builds the inject script value.</para>
    /// \endif
    /// </summary>
    /// <param name="prompt">
    /// \if KO
    /// <para>prompt에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for prompt.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Build Inject Script 작업에서 생성한 <c>string</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> result produced by the build inject script operation.</para>
    /// \endif
    /// </returns>
    public static string BuildInjectScript(string prompt)
    {
        // JSON 문자열로 안전하게 이스케이프
        var escaped = System.Text.Json.JsonSerializer.Serialize(prompt);

        return $$"""
        (async function() {
            const text = {{escaped}};

            // ── Claude.ai ──────────────────────────────────────────────
            // ProseMirror contenteditable div
            let el = document.querySelector('div[contenteditable="true"].ProseMirror')
                  ?? document.querySelector('[contenteditable="true"][data-placeholder]')
                  ?? document.querySelector('div[contenteditable="true"]');

            if (el) {
                el.focus();
                // 기존 내용 지우기
                document.execCommand('selectAll', false, null);
                document.execCommand('delete', false, null);
                // 텍스트 입력
                document.execCommand('insertText', false, text);
                await new Promise(r => setTimeout(r, 300));
                // 전송 버튼 클릭 시도
                const sendBtn = document.querySelector('button[data-testid="send-button"]')
                             ?? document.querySelector('button[aria-label*="Send"]')
                             ?? document.querySelector('button[type="submit"]');
                if (sendBtn && !sendBtn.disabled) { sendBtn.click(); return 'sent:claude'; }
                // Enter로 전송 (shift 없이)
                el.dispatchEvent(new KeyboardEvent('keydown', {key:'Enter', keyCode:13, bubbles:true}));
                return 'sent:claude-enter';
            }

            // ── ChatGPT ────────────────────────────────────────────────
            let gptEl = document.getElementById('prompt-textarea')
                     ?? document.querySelector('textarea[placeholder*="Message"]')
                     ?? document.querySelector('textarea[data-id]');

            if (gptEl) {
                gptEl.focus();
                const nativeSetter = Object.getOwnPropertyDescriptor(window.HTMLTextAreaElement.prototype, 'value').set;
                nativeSetter.call(gptEl, text);
                gptEl.dispatchEvent(new Event('input', {bubbles:true}));
                await new Promise(r => setTimeout(r, 300));
                const btn = document.querySelector('button[data-testid="send-button"]')
                         ?? document.querySelector('button[aria-label="Send prompt"]');
                if (btn && !btn.disabled) { btn.click(); return 'sent:chatgpt'; }
                gptEl.dispatchEvent(new KeyboardEvent('keydown', {key:'Enter', keyCode:13, bubbles:true}));
                return 'sent:chatgpt-enter';
            }

            // ── Gemini ─────────────────────────────────────────────────
            let gemEl = document.querySelector('rich-textarea .ql-editor')
                     ?? document.querySelector('.ql-editor[contenteditable="true"]')
                     ?? document.querySelector('textarea.input-area')
                     ?? document.querySelector('p[data-placeholder]');

            if (gemEl) {
                gemEl.focus();
                document.execCommand('selectAll', false, null);
                document.execCommand('delete', false, null);
                document.execCommand('insertText', false, text);
                await new Promise(r => setTimeout(r, 400));
                const btn = document.querySelector('button.send-button')
                         ?? document.querySelector('button[aria-label*="전송"]')
                         ?? document.querySelector('button[aria-label*="Send"]')
                         ?? document.querySelector('mat-icon[data-mat-icon-name="send"]')?.closest('button');
                if (btn) { btn.click(); return 'sent:gemini'; }
                gemEl.dispatchEvent(new KeyboardEvent('keydown', {key:'Enter', keyCode:13, bubbles:true}));
                return 'sent:gemini-enter';
            }

            return 'no-input-found';
        })();
        """;
    }
}
