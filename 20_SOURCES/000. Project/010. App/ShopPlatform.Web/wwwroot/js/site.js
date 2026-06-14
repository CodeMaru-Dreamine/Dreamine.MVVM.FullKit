// Blazor 연결 후 자동 포커스 제거 — hero 링크에 outline 박스 방지
(function () {
    function blurAutoFocus() {
        var el = document.activeElement;
        if (el && el !== document.body && el.tagName !== 'INPUT' && el.tagName !== 'TEXTAREA' && el.tagName !== 'SELECT') {
            el.blur();
        }
    }

    // Blazor Interactive Server 연결 시점
    document.addEventListener('blazor:connected', blurAutoFocus);

    // 일반 페이지 로드 시
    document.addEventListener('DOMContentLoaded', function () {
        setTimeout(blurAutoFocus, 100);
    });
})();
