/**
 * @file bgm.js
 * @brief 랜덤 셔플/자동 다음 곡 BGM 플레이어(Blazor JSInterop 대응).
 * @details
 *  - 전역 window.Bgm 으로 공개
 *  - Fisher–Yates 셔플
 *  - ended → 다음 곡
 */

/** 전역 오염 방지를 위한 IIFE */
(function attachBgmToWindow(global) {
    'use strict';

    /** @type {HTMLAudioElement|null} */
    let audio = null;
    /** @type {string[]} */
    let playlist = [];
    /** @type {number[]} */
    let order = [];
    /** @type {number} */
    let cursor = 0;

    /** @typedef {{shuffle:boolean, volume:number, resumeOnEnded:boolean}} BgmOptions */
    /** @type {BgmOptions} */
    let opts = { shuffle: true, volume: 0.5, resumeOnEnded: true };

    /**
     * @brief Fisher–Yates 셔플
     * @param {any[]} arr
     * @returns {any[]}
     */
    function shuffleInPlace(arr) {
        for (let i = arr.length - 1; i > 0; i--) {
            const j = Math.floor(Math.random() * (i + 1));
            [arr[i], arr[j]] = [arr[j], arr[i]];
        }
        return arr;
    }

    /**
     * @brief 재생 순서 재구성
     */
    function rebuildOrder() {
        order = [...playlist.keys()];
        if (opts.shuffle) shuffleInPlace(order);
        cursor = 0;
    }

    /**
     * @brief 현재 곡 로드
     * @param {number} [resumeAt=0]
     */
    function loadCurrent(resumeAt = 0) {
        if (!audio || !playlist.length) return;
        const src = playlist[order[cursor]];
        audio.src = src;
        try { audio.currentTime = resumeAt || 0; } catch { }
        audio.load();
    }

    /**
     * @brief 현재 곡 재생
     * @param {boolean} [unmute=false]
     * @returns {Promise<void>}
     */
    async function playCurrent(unmute = false) {
        if (!audio) return;
        if (unmute) audio.muted = false;
        try { await audio.play(); }
        catch (e) { console.debug('[Bgm] play blocked:', e); }
    }

    /**
     * @brief 다음 곡
     */
    function nextTrack() {
        if (!playlist.length) return;
        cursor++;
        if (cursor >= order.length) rebuildOrder();
        loadCurrent(0);
        void playCurrent(false);
    }

    /**
     * @brief 이전 곡
     */
    function prevTrack() {
        if (!playlist.length) return;
        cursor = (cursor - 1 + order.length) % order.length;
        loadCurrent(0);
        void playCurrent(false);
    }

    /**
 * @brief 임의의 다음 트랙으로 이동한다.
 * @param {boolean} [autoplay=true] true면 즉시 재생
 * @param {boolean} [avoidSame=true] true면 현재 곡과 같은 곡은 피함(단, 1곡이면 허용)
 */
    function nextRandom(autoplay = true, avoidSame = true) {
        if (!audio || !playlist.length) return;

        // 현재 곡의 '플레이리스트 인덱스' 구하기
        const curPlaylistIdx = order[cursor];

        // 랜덤으로 다음 곡 선택
        let picked;
        if (playlist.length === 1) {
            picked = curPlaylistIdx; // 1곡만 있으면 그대로
        } else {
            do {
                picked = Math.floor(Math.random() * playlist.length); // 0..N-1
            } while (avoidSame && picked === curPlaylistIdx);
        }

        // order 내에서 해당 인덱스의 위치를 커서로 설정
        const pos = order.findIndex(k => k === picked);
        cursor = (pos >= 0) ? pos : 0;

        // 로드 후 재생 옵션
        loadCurrent(0);
        if (autoplay) void playCurrent(false);
    }


    /**
     * @brief ended 핸들러
     */
    function onEnded() {
        if (opts.resumeOnEnded) nextTrack();
    }

    /**
     * @brief 오디오 이벤트 바인딩
     */
    function wireUp() {
        if (!audio) return;
        audio.removeEventListener('ended', onEnded);
        audio.addEventListener('ended', onEnded, { passive: true });
    }

    /**
     * @brief 볼륨 설정
     * @param {number} v
     */
    function setVolume(v) {
        if (!audio) return;
        const vv = Math.min(1, Math.max(0, Number(v) || 0));
        audio.volume = vv;
    }

    /**
     * @brief 셔플 설정
     * @param {boolean} enabled
     */
    function setShuffle(enabled) {
        opts.shuffle = !!enabled;
        const cur = audio?.src || '';
        rebuildOrder();
        if (cur) {
            const idx = playlist.findIndex(p => cur.endsWith(p));
            if (idx >= 0) {
                const pos = order.findIndex(k => k === idx);
                if (pos >= 0) cursor = pos;
            }
        }
    }

    /**
     * @brief 전역 API
     */
    const api = {
        /**
         * @brief 초기화
         * @param {string} selector 오디오 선택자
         * @param {string[]} files mp3 경로 배열
         * @param {Partial<BgmOptions>} [options]
         */
        init(selector, files, options) {
            audio = /** @type {HTMLAudioElement} */ (document.querySelector(selector));
            if (!audio) { console.error('[Bgm] audio element not found:', selector); return; }
            playlist = Array.isArray(files) ? files.slice() : [];
            opts = { ...opts, ...(options || {}) };
            setVolume(opts.volume);
            audio.muted = true;   // 초기 음소거(브라우저 정책 회피)
            rebuildOrder();
            loadCurrent(0);
            wireUp();
            console.debug('[Bgm] ready:', { count: playlist.length, opts });
        },

        /**
         * @brief 재생
         * @param {string} [selector]
         * @param {boolean} [unmute=false]
         */
        play(selector, unmute = false) {
            if (selector) {
                const el = document.querySelector(selector);
                if (el && el !== audio) { audio = /** @type {HTMLAudioElement} */ (el); wireUp(); }
            }
            void playCurrent(!!unmute);
        },

        /**
         * @brief 일시정지
         * @param {string} [selector]
         */
        pause(selector) {
            if (selector) {
                const el = document.querySelector(selector);
                if (el && el !== audio) { audio = /** @type {HTMLAudioElement} */ (el); wireUp(); }
            }
            try { audio?.pause(); } catch { }
        },
        nextRandom: (autoplay = true, avoidSame = true) => nextRandom(autoplay, avoidSame),

    /** @brief 다음 곡 */ next() { nextTrack(); },
    /** @brief 이전 곡 */ prev() { prevTrack(); },
    /** @brief 셔플 on/off */ setShuffle(enabled) { setShuffle(enabled); },

    /** @brief 볼륨 설정 */ setVolume(v) { setVolume(v); }
    };

    // 🔒 전역 window.Bgm 으로 안전하게 내보내기(기존 값 보존 X, 덮어씀)
    global.Bgm = Object.freeze(api);

})(window);
