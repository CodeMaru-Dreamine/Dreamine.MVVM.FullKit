(function () {
    const players = new Map();
    const LOG = '[dreamine-vms-hls]';
    const READY_TIMEOUT_MS = 12000;
    const READY_INTERVAL_MS = 500;
    const RESTART_DELAY_MS = 1500;
    const WATCHDOG_INTERVAL_MS = 1000;
    const LOW_READY_LIMIT = 24;
    const STAGNANT_LIMIT = 35;

    function sleep(ms) {
        return new Promise(resolve => setTimeout(resolve, ms));
    }

    function toAbsoluteUrl(source) {
        return new URL(source, window.location.href).toString();
    }

    function addCacheBuster(url) {
        const absolute = new URL(url, window.location.href);
        absolute.searchParams.set('_', Date.now().toString());
        return absolute.toString();
    }

    function canUseNativeHls(video) {
        return video.canPlayType('application/vnd.apple.mpegurl') !== '';
    }

    function getTile(video) {
        return video ? video.closest('.video-tile') : null;
    }

    function setTileStatus(video, message, recovering) {
        const tile = getTile(video);
        if (!tile) {
            return;
        }

        if (message) {
            tile.setAttribute('data-status', message);
        } else {
            tile.removeAttribute('data-status');
        }

        if (recovering) {
            tile.classList.add('recovering');
        } else {
            tile.classList.remove('recovering');
        }
    }

    function getSegmentUrls(playlistUrl, playlistText) {
        const base = new URL(playlistUrl, window.location.href);
        return playlistText
            .split(/\r?\n/)
            .map(line => line.trim())
            .filter(line => line.length > 0 && !line.startsWith('#'))
            .map(line => new URL(line, base).toString());
    }

    async function canReadSegment(segmentUrl) {
        try {
            const response = await fetch(addCacheBuster(segmentUrl), {
                method: 'GET',
                cache: 'no-store',
                headers: { 'Range': 'bytes=0-188' }
            });

            return response.ok || response.status === 206;
        } catch (err) {
            console.warn(LOG, 'segment not ready:', segmentUrl, err);
            return false;
        }
    }

    async function waitUntilReady(source, timeoutMs, intervalMs, video) {
        const started = Date.now();
        const playlistUrl = toAbsoluteUrl(source);

        while (Date.now() - started < timeoutMs) {
            try {
                setTileStatus(video, 'Waiting for HLS segment...', true);

                const response = await fetch(addCacheBuster(playlistUrl), {
                    method: 'GET',
                    cache: 'no-store'
                });

                if (!response.ok) {
                    await sleep(intervalMs);
                    continue;
                }

                const text = await response.text();
                if (!text.includes('#EXTM3U')) {
                    await sleep(intervalMs);
                    continue;
                }

                const segments = getSegmentUrls(playlistUrl, text);
                if (segments.length === 0) {
                    await sleep(intervalMs);
                    continue;
                }

                const candidates = segments.slice(-4).reverse();
                for (const segmentUrl of candidates) {
                    if (await canReadSegment(segmentUrl)) {
                        return true;
                    }
                }
            } catch (err) {
                console.warn(LOG, 'playlist not ready:', source, err);
            }

            await sleep(intervalMs);
        }

        return false;
    }

    function configureVideo(video) {
        video.muted = true;
        video.autoplay = true;
        video.playsInline = true;
        video.preload = 'auto';
    }

    async function safePlay(video, elementId) {
        configureVideo(video);

        try {
            const promise = video.play();
            if (promise && typeof promise.catch === 'function') {
                await promise;
            }
        } catch (err) {
            console.warn(LOG, 'play() rejected:', elementId, err);
        }
    }

    function awaitMicrotaskSafePlay(video, elementId) {
        setTimeout(() => { safePlay(video, elementId); }, 0);
    }

    function wireVideoRecoveryEvents(elementId, source, video) {
        const restart = function (reason) {
            console.warn(LOG, 'video recovery event:', elementId, reason);
            setTileStatus(video, `Recovering stream (${reason})...`, true);
            scheduleRetry(elementId, source, RESTART_DELAY_MS);
        };

        video.onstalled = function () { restart('stalled'); };
        video.onerror = function () { restart('video error'); };
        video.onemptied = function () { restart('emptied'); };
        video.onsuspend = function () { safePlay(video, elementId); };
        video.onwaiting = function () {
            setTileStatus(video, 'Buffering stream...', true);
            safePlay(video, elementId);
        };
        video.onplaying = function () { setTileStatus(video, null, false); };
        video.oncanplay = function () { setTileStatus(video, null, false); };
        video.ontimeupdate = function () {
            if (video.readyState >= 2 && video.videoWidth > 0) {
                setTileStatus(video, null, false);
            }
        };
        video.onloadeddata = function () { setTileStatus(video, null, false); };
    }

    function destroy(elementId, reason) {
        const player = players.get(elementId);
        if (!player) {
            const video = document.getElementById(elementId);
            if (video) {
                setTileStatus(video, reason || 'Stream stopped.', false);
            }
            return;
        }

        if (player.watchdogId) {
            clearInterval(player.watchdogId);
        }

        if (player.retryTimerId) {
            clearTimeout(player.retryTimerId);
        }

        if (player.hls) {
            try { player.hls.destroy(); } catch { }
        }

        if (player.video) {
            try {
                player.video.onstalled = null;
                player.video.onerror = null;
                player.video.onemptied = null;
                player.video.onsuspend = null;
                player.video.onwaiting = null;
                player.video.onplaying = null;
                player.video.oncanplay = null;
                player.video.ontimeupdate = null;
                player.video.onloadeddata = null;
                setTileStatus(player.video, reason || 'Stream stopped.', false);
                player.video.pause();
                player.video.removeAttribute('src');
                player.video.load();
            } catch { }
        }

        players.delete(elementId);
    }

    function startPlaybackWatchdog(elementId, source) {
        const player = players.get(elementId);
        if (!player || !player.video) {
            return;
        }

        let lastTime = -1;
        let stagnantCount = 0;
        let lowReadyCount = 0;

        player.watchdogId = setInterval(() => {
            const current = players.get(elementId);
            if (!current || !current.video) {
                return;
            }

            const video = current.video;

            if (document.hidden) {
                return;
            }

            if (video.paused && video.readyState >= 2) {
                safePlay(video, elementId);
            }

            if (video.readyState < 2) {
                lowReadyCount++;

                if (video.videoWidth <= 0) {
                    setTileStatus(video, 'Waiting for stream data...', true);
                }

                if (lowReadyCount >= LOW_READY_LIMIT) {
                    lowReadyCount = 0;
                    console.warn(LOG, 'video readyState stayed low. restarting player:', elementId, video.readyState);
                    init(elementId, source);
                }
                return;
            }

            lowReadyCount = 0;

            if (Math.abs(video.currentTime - lastTime) < 0.05) {
                stagnantCount++;
            } else {
                stagnantCount = 0;
                lastTime = video.currentTime;
            }

            if (stagnantCount < STAGNANT_LIMIT) {
                return;
            }

            stagnantCount = 0;
            console.warn(LOG, 'playback stagnant. recovering player:', elementId);

            if (current.hls) {
                try {
                    setTileStatus(video, 'Recovering HLS buffer...', true);
                    current.hls.stopLoad();
                    awaitMicrotaskSafePlay(video, elementId);
                    current.hls.startLoad(-1);
                    return;
                } catch (err) {
                    console.warn(LOG, 'hls startLoad recovery failed:', elementId, err);
                }
            }

            setTileStatus(video, 'Recovering frozen stream...', true);
            init(elementId, source);
        }, WATCHDOG_INTERVAL_MS);
    }

    function scheduleRetry(elementId, source, delayMs) {
        const existing = players.get(elementId);
        if (existing && existing.retryTimerId) {
            clearTimeout(existing.retryTimerId);
            existing.retryTimerId = null;
        }

        const retryTimerId = setTimeout(() => {
            init(elementId, source);
        }, delayMs);

        const player = players.get(elementId);
        if (player) {
            player.retryTimerId = retryTimerId;
        }
    }

    async function init(elementId, source) {
        console.log(LOG, 'init called:', elementId, source);

        const video = document.getElementById(elementId);
        if (!video) {
            console.warn(LOG, 'video element not found:', elementId);
            return;
        }

        const existing = players.get(elementId);
        if (existing && existing.source === source && existing.video === video) {
            if (existing.hls) {
                try { existing.hls.startLoad(-1); } catch { }
            }

            if (video.readyState < 2) {
                setTileStatus(video, 'Waiting for stream data...', true);
            }

            await safePlay(video, elementId);
            return;
        }

        destroy(elementId);
        configureVideo(video);
        wireVideoRecoveryEvents(elementId, source, video);
        setTileStatus(video, 'Preparing HLS stream...', true);

        const ready = await waitUntilReady(source, READY_TIMEOUT_MS, READY_INTERVAL_MS, video);
        if (!ready) {
            console.warn(LOG, 'playlist or segment was not ready. Starting hls.js anyway with retry:', source);
            setTileStatus(video, 'HLS not ready. Retrying...', true);
        }

        const playbackSource = addCacheBuster(source);

        if (window.Hls && window.Hls.isSupported()) {
            const hls = new window.Hls({
                lowLatencyMode: false,
                enableWorker: true,
                backBufferLength: 30,
                liveSyncDurationCount: 3,
                liveMaxLatencyDurationCount: 12,
                maxLiveSyncPlaybackRate: 1.5,
                manifestLoadingTimeOut: 10000,
                manifestLoadingMaxRetry: 999,
                manifestLoadingRetryDelay: 1000,
                manifestLoadingMaxRetryTimeout: 5000,
                levelLoadingTimeOut: 10000,
                levelLoadingMaxRetry: 999,
                fragLoadingTimeOut: 20000,
                fragLoadingMaxRetry: 999,
                fragLoadingRetryDelay: 1000,
                appendErrorMaxRetry: 10
            });

            players.set(elementId, { video: video, hls: hls, source: source, watchdogId: null, retryTimerId: null });

            hls.attachMedia(video);

            hls.on(window.Hls.Events.MEDIA_ATTACHED, function () {
                console.log(LOG, 'media attached, loading source:', playbackSource);
                setTileStatus(video, 'Loading HLS manifest...', true);
                hls.loadSource(playbackSource);
                hls.startLoad(-1);
            });

            hls.on(window.Hls.Events.MANIFEST_PARSED, function (_event, data) {
                const levelCount = data && data.levels ? data.levels.length : 'n/a';
                console.log(LOG, 'manifest parsed:', elementId, 'levels:', levelCount);
                setTileStatus(video, 'Starting playback...', true);
                safePlay(video, elementId);
            });

            hls.on(window.Hls.Events.LEVEL_LOADED, function () {
                safePlay(video, elementId);
            });

            hls.on(window.Hls.Events.FRAG_BUFFERED, function (_event, data) {
                const sn = data && data.frag ? data.frag.sn : 'n/a';
                console.log(LOG, 'fragment buffered:', elementId, 'sn:', sn);
                setTileStatus(video, null, false);
                safePlay(video, elementId);
            });

            hls.on(window.Hls.Events.ERROR, function (_event, data) {
                if (!data) {
                    return;
                }

                console.warn(LOG, 'hls error:', elementId,
                    'type:', data.type,
                    'details:', data.details,
                    'fatal:', data.fatal);

                setTileStatus(video, `Recovering HLS (${data.details || data.type})...`, true);

                if (!data.fatal) {
                    try { hls.startLoad(-1); } catch { }
                    return;
                }

                if (data.type === window.Hls.ErrorTypes.NETWORK_ERROR) {
                    try {
                        hls.startLoad(-1);
                    } catch {
                        scheduleRetry(elementId, source, RESTART_DELAY_MS);
                    }
                    return;
                }

                if (data.type === window.Hls.ErrorTypes.MEDIA_ERROR) {
                    try {
                        hls.recoverMediaError();
                    } catch {
                        scheduleRetry(elementId, source, RESTART_DELAY_MS);
                    }
                    return;
                }

                scheduleRetry(elementId, source, RESTART_DELAY_MS);
            });

            startPlaybackWatchdog(elementId, source);
            return;
        }

        if (canUseNativeHls(video)) {
            console.log(LOG, 'using native HLS:', elementId);
            video.src = playbackSource;
            players.set(elementId, { video: video, hls: null, source: source, watchdogId: null, retryTimerId: null });
            await safePlay(video, elementId);
            startPlaybackWatchdog(elementId, source);
            return;
        }

        console.error(LOG, 'hls.js is not available and native HLS is not supported:', elementId);
        players.set(elementId, { video: video, hls: null, source: source, watchdogId: null, retryTimerId: null });
        setTileStatus(video, 'hls.js not loaded. Check network or bundle hls.min.js locally.', true);
        scheduleRetry(elementId, source, 5000);
    }

    async function ensure(elementId, source) {
        const video = document.getElementById(elementId);
        if (!video) {
            console.warn(LOG, 'video element not found for ensure:', elementId);
            return;
        }

        const player = players.get(elementId);
        if (!player || player.source !== source) {
            await init(elementId, source);
            return;
        }

        if (player.hls) {
            try { player.hls.startLoad(-1); } catch { }
        }

        if (video.readyState < 2) {
            setTileStatus(video, 'Waiting for stream data...', true);
        }

        await safePlay(video, elementId);
    }

    document.addEventListener('visibilitychange', function () {
        if (document.hidden) {
            return;
        }

        for (const [elementId, player] of players.entries()) {
            if (!player || !player.video) {
                continue;
            }

            safePlay(player.video, elementId);
            if (player.hls) {
                try { player.hls.startLoad(-1); } catch { }
            }
        }
    });

    window.dreamineVmsHls = {
        version: '2026.05.27.5',
        init: init,
        ensure: ensure,
        ensureOrInit: ensure,
        destroy: destroy
    };
})();
