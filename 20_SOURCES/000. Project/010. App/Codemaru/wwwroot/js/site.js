/**
 * @file site.js
 * @brief 브라우저에서 HLS(m3u8) 재생을 초기화/해제/복구(워치독)하는 유틸리티.
 * @details
 *  - m3u8 캐시 무력화: 요청마다 cache-buster 쿼리 추가 + fetch cache:no-store.
 *  - Hls.js 재시도/복구: fatal 에러 시 자동 복구, 정체(stall) 감지 워치독.
 *  - 버퍼 튜닝: 저지연 옵션 + backBufferLength 축소.
 *  - 네이티브(HLS 지원 브라우저, 주로 iOS Safari)도 캐시버스터 적용.
 */

/**
 * @brief 내부 상태 테이블(요소별 Hls 인스턴스/워치독 타이머 등).
 * @details key: elementId
 */
const _hlsState = Object.create(null);

/**
 * @brief 캐시 무력화를 위해 매 요청마다 고유 쿼리스트링(_cb=timestamp)을 부착합니다.
 * @param {string} url - 원본 m3u8 URL(예: "/hls/front/index.m3u8").
 * @return {string} 쿼리스트링이 부착된 URL.
 */
function withCacheBuster(url) {
    try {
        const u = new URL(url, window.location.origin);
        u.searchParams.set("_cb", Date.now().toString());
        return u.toString();
    } catch {
        const sep = url.includes("?") ? "&" : "?";
        return `${url}${sep}_cb=${Date.now()}`;
    }
}

/**
 * @brief 비디오 진행 정체(stall) 감지용 워치독을 시작합니다.
 * @param {HTMLVideoElement} video - 감시할 video 요소.
 * @param {() => void} onStall - 정체시 수행할 복구 콜백.
 * @return {number} setInterval 타이머 id.
 */
function startWatchdog(video, onStall) {
    let lastTime = video.currentTime || 0;
    let stillCount = 0;

    // 2초마다 진행 확인 → 3회 연속(약 6초) 정체 시 복구
    return window.setInterval(() => {
        const now = video.currentTime || 0;

        // 재생 중일 때만 체크
        if (!video.paused && !video.seeking && video.readyState >= 2) {
            if (Math.abs(now - lastTime) < 0.01) {
                stillCount++;
            } else {
                stillCount = 0;
                lastTime = now;
            }
            if (stillCount >= 3) { // ~6초 정체
                stillCount = 0;
                try { onStall(); } catch { /* noop */ }
            }
        }
    }, 2000);
}

/**
 * @brief 지정한 video 요소에 HLS 스트림을 바인딩합니다(캐시 무력화/복구 포함).
 * @param {string} elementId - video 태그의 DOM id.
 * @param {string} m3u8Url - HLS m3u8 주소(예: /hls/back/index.m3u8).
 */
window.initHlsPlayer = function (elementId, m3u8Url) {
    const video = document.getElementById(elementId);
    if (!video) return;

    // 기존 인스턴스/워치독 정리
    window.destroyHlsPlayer(elementId);

    // 매번 다른 쿼리로 로드하여 캐시 완전 우회
    const busted = withCacheBuster(m3u8Url);

    /**
     * @brief 정체 발생 시 재초기화(캐시버스터 갱신 포함)
     */
    const recover = () => {
        // 네이티브/Hls.js 모두 동일하게 재초기화
        window.initHlsPlayer(elementId, m3u8Url);
    };

    // 네이티브 HLS(iOS Safari 등) 경로
    if (video.canPlayType('application/vnd.apple.mpegurl')) {
        video.src = busted;
        // 이벤트 기반 복구 보조
        video.addEventListener('stalled', recover, { passive: true });
        video.addEventListener('error', recover, { passive: true });

        // 워치독 시작
        const wd = startWatchdog(video, recover);
        _hlsState[elementId] = { hls: null, wd, events: ['stalled', 'error'] };

        video.play().catch(() => { /* 자동재생 차단 무시 */ });
        return;
    }

    // Hls.js 경로
    if (window.Hls && window.Hls.isSupported()) {
        /** @brief Hls.js 인스턴스 생성(저지연/버퍼/캐시무력화/재시도) */
        const hls = new Hls({
            enableWorker: true,
            lowLatencyMode: true,
            // 너무 큰 백버퍼는 고착 시 복구가 느릴 수 있음 → 축소
            backBufferLength: 3,
            // fetch 옵션 커스터마이즈: 모든 요청 cache:no-store
            fetchSetup: (ctx, init) => new Request(ctx.url, { ...init, cache: 'no-store' }),
            // 네트워크/조각 로딩 재시도(기본값 상향)
            manifestLoadingMaxRetry: 3,
            manifestLoadingRetryDelay: 800,
            fragLoadingMaxRetry: 3,
            fragLoadingRetryDelay: 500
        });

        hls.attachMedia(video);
        hls.on(Hls.Events.MEDIA_ATTACHED, () => {
            hls.loadSource(busted);
        });

        // 오류 복구 전략
        hls.on(Hls.Events.ERROR, (_evt, data) => {
            if (!data?.fatal) return;
            switch (data.type) {
                case Hls.ErrorTypes.NETWORK_ERROR:
                    // 네트워크 오류 → 다시 로드 시도
                    try { hls.startLoad(); } catch { recover(); }
                    break;
                case Hls.ErrorTypes.MEDIA_ERROR:
                    // 미디어 오류 → 복구 시도
                    try { hls.recoverMediaError(); } catch { recover(); }
                    break;
                default:
                    // 그 외 치명적 → 완전 재초기화
                    recover();
                    break;
            }
        });

        // 비디오 이벤트 기반 보조 복구
        const onStalled = () => recover();
        const onError = () => recover();
        video.addEventListener('stalled', onStalled, { passive: true });
        video.addEventListener('error', onError, { passive: true });

        // 워치독 시작
        const wd = startWatchdog(video, recover);

        // 상태 저장(정리용)
        video.__hls = hls;
        _hlsState[elementId] = { hls, wd, events: ['stalled', 'error'] };

        video.play().catch(() => { /* 자동재생 차단 무시 */ });
        return;
    }

    console.error('HLS not supported on this browser.');
};

/**
 * @brief initHlsPlayer로 붙인 Hls.js/네이티브 소스 및 워치독을 해제합니다.
 * @param {string} elementId - video 태그 id.
 */
window.destroyHlsPlayer = function (elementId) {
    const video = document.getElementById(elementId);
    if (!video) return;

    // 워치독/이벤트 제거
    const st = _hlsState[elementId];
    if (st) {
        if (st.wd) { try { clearInterval(st.wd); } catch { /* noop */ } }
        if (st.events && Array.isArray(st.events)) {
            try { st.events.forEach(evt => video.removeEventListener(evt, window.initHlsPlayer)); } catch { /* noop */ }
        }
        delete _hlsState[elementId];
    }

    // Hls.js 인스턴스 제거
    if (video.__hls) {
        try { video.__hls.destroy(); } catch { /* noop */ }
        delete video.__hls;
    }

    // 네이티브 소스 제거
    video.removeAttribute('src');
    try { video.load(); } catch { /* noop */ }
};

function openPopup(url) {
    window.open(
        url,
        'cctvPopup',
        'width=1024,height=768,resizable=yes,scrollbars=yes'
    );
}

function openModal(url) {
    document.getElementById("cctvModal").style.display = "block";
    document.getElementById("cctvFrame").src = url;
}
function closeModal() {
    document.getElementById("cctvModal").style.display = "none";
    document.getElementById("cctvFrame").src = "";
}

/**
 * @brief .cm-site-preview-wrap 안의 iframe을 컨테이너 크기에 맞게 scale 조정합니다.
 * @details Blazor afterRender 후 호출. ResizeObserver로 리사이즈 시에도 자동 재조정.
 */
window.scaleInternalIframe = function () {
    const wrap = document.querySelector('.cm-iframe-wrap');
    const iframe = document.querySelector('.cm-iframe-preview');
    if (!wrap || !iframe) return;

    const scale = wrap.clientWidth / 1280;
    iframe.style.transform = `scale(${scale})`;
    wrap.style.height = (720 * scale) + 'px';

    if (!window._iframeRO) {
        window._iframeRO = new ResizeObserver(() => {
            const s = wrap.clientWidth / 1280;
            iframe.style.transform = `scale(${s})`;
            wrap.style.height = (720 * s) + 'px';
        });
        window._iframeRO.observe(wrap);
    }
};

window.scalePreviewIframes = function () {
    const IFRAME_W = 1280;

    function applyScale(wrap) {
        const iframe = wrap.querySelector('.cm-site-preview');
        if (!iframe) return;
        const scale = wrap.clientWidth / IFRAME_W;
        iframe.style.transform = `scale(${scale})`;
        wrap.style.height = (720 * scale) + 'px';
    }

    const wraps = document.querySelectorAll('.cm-site-preview-wrap');
    wraps.forEach(applyScale);

    if (!window._previewRO) {
        window._previewRO = new ResizeObserver(entries => {
            entries.forEach(e => applyScale(e.target));
        });
    }
    wraps.forEach(w => window._previewRO.observe(w));
};
