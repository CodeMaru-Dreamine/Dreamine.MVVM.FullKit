// AutoScroll 로그 데모용 — Blazor에는 "스크롤을 맨 아래로" 같은 DOM 조작이 없어서
// 최소한의 JS interop으로 채운다.
window.dreamineScrollToBottom = (element) => {
    if (element) {
        element.scrollTop = element.scrollHeight;
    }
};
