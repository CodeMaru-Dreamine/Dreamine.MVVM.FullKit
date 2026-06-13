/*!
 * \file contact.js
 * \brief 연락처 페이지용 JS 유틸(클립보드 복사).
 * \details navigator.clipboard 우선, 미지원/HTTP 환경은 폴백 처리.
 */
window.contact = window.contact || {};
window.contact.copyText = async function (text) {
    try {
        if (navigator.clipboard && window.isSecureContext) {
            await navigator.clipboard.writeText(text);
        } else {
            // 폴백: 임시 textarea 생성 후 선택/복사
            const ta = document.createElement('textarea');
            ta.value = text;
            ta.setAttribute('readonly', '');
            ta.style.position = 'fixed';
            ta.style.top = '-1000px';
            document.body.appendChild(ta);
            ta.select();
            document.execCommand('copy');
            document.body.removeChild(ta);
        }
        // 간단한 토스트
        window.clearTimeout(window.__contact_toast__);
        const toast = document.getElementById('contact-toast') || (() => {
            const d = document.createElement('div');
            d.id = 'contact-toast';
            d.style.position = 'fixed';
            d.style.left = '50%';
            d.style.bottom = '10vh';
            d.style.transform = 'translateX(-50%)';
            d.style.padding = '10px 14px';
            d.style.borderRadius = '12px';
            d.style.background = 'rgba(0,0,0,.75)';
            d.style.color = '#fff';
            d.style.fontSize = '14px';
            d.style.zIndex = '2000';
            document.body.appendChild(d);
            return d;
        })();
        toast.textContent = '복사되었습니다';
        toast.style.opacity = '1';
        window.__contact_toast__ = setTimeout(() => toast.style.opacity = '0', 1200);
    } catch (e) {
        alert('복사를 지원하지 않는 환경입니다.\n직접 선택해 복사해주세요.');
    }
};
