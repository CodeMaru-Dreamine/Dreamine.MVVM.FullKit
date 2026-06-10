/*!
 * \file hls-interop.js
 * \brief Blazor ↔ hls.js 상호운용(초기화/해제/자동복구/온라인 복귀) 유틸.
 * \details
 *  - 비정상 종료, 일시적인 네트워크 끊김 이후 자동 복구/재초기화.
 *  - Safari(네이티브 HLS) 및 MediaSource(hls.js) 양쪽 지원.
 *  - 지수 백오프 기반 재시도, online 이벤트 시 전체 재시도.
 *  - 퍼지 중복 재시작 방지(플래그) 및 정리(destroy) 안전성 보장.
 */

(function () {
    /** @typedef {{ hls?: any, video: HTMLVideoElement, src: string, retryCount: number, retryTimer?: number, lastMediaErrorRecoverAt?: number, isDestroying?: boolean }} PlayerCtx */

    /** \brief elementId → PlayerCtx 사전. */
    const players = /** @type {Record<string, PlayerCtx>} */ ({});

    /** \brief 재시도 파라미터. */
    const RETRY = {
        max: 6,              // 최대 6회
        baseDelayMs: 1500,   // 1.5s 시작
        maxDelayMs: 15000,   // 15s 상한
        mediaRecoverGapMs: 5000 // MEDIA_ERROR 연속 recover 최소 간격
    };

    /**
     * \brief 지수 백오프 딜레이(ms) 계산.
     * \param {number} n 시도 횟수(0부터)
     */
    function backoff(n) {
        const d = Math.min(RETRY.baseDelayMs * Math.pow(2, n), RETRY.maxDelayMs);
        // 약간의 지터 추가
        return Math.floor(d * (0.8 + Math.random() * 0.4));
    }

    /**
     * \brief 플레이어 정리(destroy) 안전 처리.
     * \param {string} elementId 비디오 엘리먼트 id
     */
    function safeDestroy(elementId) {
        const ctx = players[elementId];
        if (!ctx || ctx.isDestroying) return;
        ctx.isDestroying = true;

        try {
            if (ctx.retryTimer) {
                clearTimeout(ctx.retryTimer);
                ctx.retryTimer = undefined;
            }
            if (ctx.hls) {
                try { ctx.hls.destroy(); } catch { /* noop */ }
                ctx.hls = undefined;
            } else if (ctx.video) {
                // Safari 등 네이티브 소스 정리
                try {
                    ctx.video.pause();
                    ctx.video.removeAttribute('src');
                    ctx.video.load();
                } catch { /* noop */ }
            }
        } finally {
            ctx.isDestroying = false;
        }
    }

    /**
     * \brief 네이티브 HLS(Safari) 초기화.
     * \param {PlayerCtx} ctx
     */
    function initNative(ctx) {
        ctx.video.src = ctx.src;
        const onError = () => scheduleRetry(ctx, 'NATIVE_ERROR');

        ctx.video.addEventListener('error', onError);
        ctx.video.addEventListener('stalled', onError);
        ctx.video.addEventListener('emptied', onError);
        ctx.video.play().catch(() => { /* 사용자 제스처 필요 시 무시 */ });

        // 정리 훅
        ctx.__cleanup = () => {
            ctx.video.removeEventListener('error', onError);
            ctx.video.removeEventListener('stalled', onError);
            ctx.video.removeEventListener('emptied', onError);
        };
    }

    /**
     * \brief hls.js 초기화 및 에러 핸들링 바인딩.
     * \param {PlayerCtx} ctx
     */
    function initHls(ctx) {
        // eslint-disable-next-line no-undef
        const hls = new Hls({
            // 안정성 관련 기본 값(필요 시 프로젝트에 맞춰 조정)
            enableWorker: true,
            lowLatencyMode: false,
            backBufferLength: 90,
            fragLoadingRetry: 0, // 내부 재시도는 최소화하고 외부 루틴으로 통일
            manifestLoadingRetry: 0,
            levelLoadingRetry: 0
        });
        ctx.hls = hls;

        hls.attachMedia(ctx.video);
        hls.on(Hls.Events.MEDIA_ATTACHED, () => {
            hls.loadSource(ctx.src);
        });

        hls.on(Hls.Events.ERROR, (_evt, data) => {
            const { type, details, fatal } = data;

            // 네트워크 관련: manifest/fragment 타임아웃/에러 등
            if (type === Hls.ErrorTypes.NETWORK_ERROR) {
                if (fatal) {
                    // fatal 네트워크 에러는 재초기화(reloadSource) 대신 완전 재시작이 안정적
                    scheduleRetry(ctx, details || 'NETWORK_FATAL');
                } else {
                    // 비치명적인 네트워크 에러: 일시 정지 후 재개 시도
                    try { hls.startLoad(); } catch { scheduleRetry(ctx, details || 'NETWORK'); }
                }
                return;
            }

            // 미디어 디코딩 관련
            if (type === Hls.ErrorTypes.MEDIA_ERROR) {
                // 너무 자주 recoverMediaError 호출하면 내부 상태 꼬임 → 최소 간격 보장
                const now = Date.now();
                if (!ctx.lastMediaErrorRecoverAt || (now - ctx.lastMediaErrorRecoverAt) > RETRY.mediaRecoverGapMs) {
                    ctx.lastMediaErrorRecoverAt = now;
                    try {
                        hls.recoverMediaError();
                        return;
                    } catch { /* fallthrough */ }
                }
                // recover 실패 → 재초기화
                scheduleRetry(ctx, details || 'MEDIA_FATAL');
                return;
            }

            // 그 외 치명적 에러
            if (fatal) {
                scheduleRetry(ctx, details || 'FATAL');
            }
        });

        // 디버그 로그(필요 없으면 주석 처리)
        // hls.on(Hls.Events.LEVEL_UPDATED, (_e, d) => console.debug('LEVEL_UPDATED', d));
        // hls.on(Hls.Events.FRAG_LOADED, (_e, d) => console.debug('FRAG_LOADED', d));
    }

    /**
     * \brief 재시도 예약(지수 백오프).
     * \param {PlayerCtx} ctx
     * \param {string} reason 로깅용 사유
     */
    function scheduleRetry(ctx, reason) {
        if (ctx.retryCount >= RETRY.max) {
            console.warn('[hls-interop] retry max reached:', reason);
            return;
        }
        const delay = backoff(ctx.retryCount++);
        if (ctx.retryTimer) clearTimeout(ctx.retryTimer);

        console.warn(`[hls-interop] schedule retry in ${delay}ms (reason=${reason})`);
        ctx.retryTimer = setTimeout(() => {
            ctx.retryTimer = undefined;
            restart(ctx);
        }, delay);
    }

    /**
     * \brief 플레이어 완전 재시작(깨끗한 초기화).
     * \param {PlayerCtx} ctx
     */
    function restart(ctx) {
        // 파손된 상태 정리
        safeDestroy(ctx.video.id);

        // Safari vs hls.js 분기
        if (canUseNative()) {
            // 네이티브는 src 재지정 + play 재요청
            initNative(ctx);
        } else {
            initHls(ctx);
        }
    }

    /** \brief Safari(네이티브 HLS) 사용 가능 여부. */
    function canUseNative() {
        const v = document.createElement('video');
        return v.canPlayType('application/vnd.apple.mpegurl') !== '';
    }

    /**
     * \brief 플레이어 초기화(공개 API).
     * \param {string} elementId 비디오 엘리먼트 id
     * \param {string} src m3u8 주소
     */
    window.initHlsPlayer = function (elementId, src) {
        /** @type {HTMLVideoElement|null} */
        const video = document.getElementById(elementId);
        if (!video) {
            console.warn('[hls-interop] video element not found:', elementId);
            return;
        }

        // 기존 인스턴스가 있으면 먼저 정리
        if (players[elementId]) {
            safeDestroy(elementId);
        }

        const ctx = /** @type {PlayerCtx} */ ({
            video, src, retryCount: 0, lastMediaErrorRecoverAt: 0
        });
        players[elementId] = ctx;

        // 네이티브/MediaSource 분기
        if (canUseNative()) initNative(ctx); else initHls(ctx);
    };

    /**
     * \brief 플레이어 해제(공개 API).
     * \param {string} elementId 비디오 엘리먼트 id
     */
    window.destroyHlsPlayer = function (elementId) {
        const ctx = players[elementId];
        if (!ctx) return;
        if (ctx.__cleanup) try { ctx.__cleanup(); } catch { /* noop */ }
        safeDestroy(elementId);
        delete players[elementId];
    };

    /**
     * \brief 온라인 복귀 시 전체 플레이어를 재시도.
     */
    window.addEventListener('online', () => {
        Object.keys(players).forEach((id) => {
            const ctx = players[id];
            if (!ctx) return;
            // 즉시 재시작(딜레이 없이)
            ctx.retryCount = 0;
            restart(ctx);
        });
    });

})();
