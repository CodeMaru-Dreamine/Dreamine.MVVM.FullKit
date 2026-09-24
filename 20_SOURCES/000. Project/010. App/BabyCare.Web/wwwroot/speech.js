let recognition;
export function available() { return window.isSecureContext && !!(window.SpeechRecognition || window.webkitSpeechRecognition); }
export function offsetMinutes() { return -new Date().getTimezoneOffset(); }
export function start(dotnet, language = 'ko') {
    dispose();
    const Recognition = window.SpeechRecognition || window.webkitSpeechRecognition;
    if (!available()) throw new Error('음성 인식을 지원하지 않습니다.');
    recognition = new Recognition();
    recognition.lang = ({ko:'ko-KR',en:'en-US',es:'es-ES',fr:'fr-FR',it:'it-IT',pt:'pt-PT',ja:'ja-JP','zh-hans':'zh-CN','zh-hant':'zh-TW',vi:'vi-VN'})[language] || 'ko-KR';
    recognition.continuous = false;
    recognition.interimResults = false;
    recognition.onresult = event => {
        const text = Array.from(event.results).filter(result => result.isFinal).map(result => result[0].transcript).join(' ');
        if (text.trim()) dotnet.invokeMethodAsync('SpeechResult', text);
    };
    recognition.onerror = event => dotnet.invokeMethodAsync('SpeechError', event.error === 'not-allowed' ? '마이크 권한을 확인하거나 직접 입력해주세요.' : '마이크 권한을 확인하거나 직접 입력해주세요.');
    recognition.onend = () => dotnet.invokeMethodAsync('SpeechEnded');
    recognition.start();
}
export function stop() { recognition?.stop(); }
export function dispose() {
    if (!recognition) return;
    recognition.onresult = recognition.onerror = recognition.onend = null;
    recognition.abort(); recognition = null;
}

export function focusEditor() { const editor = document.getElementById("care-editor"); if (editor) { editor.focus({preventScroll:true}); editor.scrollIntoView({block:"start",behavior:"instant"}); } }
