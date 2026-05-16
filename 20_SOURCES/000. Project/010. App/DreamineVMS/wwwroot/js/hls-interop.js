(function () {
    const players = new Map();
    const LOG = '[dreamine-vms-hls]';

    function sleep(ms) {
        return new Promise(resolve => setTimeout(resolve, ms));
    }

    function canUseNativeHls(video) {
        return video.canPlayType('application/vnd.apple.mpegurl') !== '';
    }

    function toAbsoluteUrl(source) {
        return new URL(source, window.location.href).toString();
    }

    function addCacheBuster(url) {
        const absolute = new URL(url, window.location.href);
        absolute.searchParams.set('_', Date.now().toString());
        return absolute.toString();
    }

    function getFirstSegmentUrl(playlistUrl, playlistText) {
        const base = new URL(playlistUrl, window.location.href);
        const lines = playlistText
            .split(/\r?\n/)
            .map(line => line.trim())
            .filter(line => line.length > 0 && !line.startsWith('#'));

        if (lines.length === 0) {
            return null;
        }

        return new URL(lines[lines.length - 1], base).toString();
    }

    async function canReadSegment(segmentUrl) {
        try {
            const response = await fetch(addCacheBuster(segmentUrl), {
                method: 'GET',
                cache: 'no-store',
                headers: {
                    'Range': 'bytes=0-188'
                }
            });

            return response.ok || response.status === 206;
        } catch (err) {
            console.warn(LOG, 'segment not ready:', segmentUrl, err);
            return false;
        }
    }

    async function waitUntilReady(source, timeoutMs, intervalMs) {
        const started = Date.now();
        const playlistUrl = toAbsoluteUrl(source);

        while (Date.now() - started < timeoutMs) {
            try {
                const response = await fetch(addCacheBuster(playlistUrl), {
                    method: 'GET',
                    cache: 'no-store'
                });

                if (!response.ok) {
                    await sleep(intervalMs);
                    continue;
                }

                const text = await response.text();
                const hasPlaylist = text.includes('#EXTM3U');
                const hasFragment = text.includes('#EXTINF');
                const segmentUrl = getFirstSegmentUrl(playlistUrl, text);

                if (hasPlaylist && hasFragment && segmentUrl && await canReadSegment(segmentUrl)) {
                    return true;
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

    function destroy(elementId) {
        const player = players.get(elementId);
        if (!player) {
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

        player.watchdogId = setInterval(() => {
            const current = players.get(elementId);
            if (!current || !current.video) {
                return;
            }

            const video = current.video;

            if (video.paused && video.readyState >= 2) {
                safePlay(video, elementId);
            }

            if (video.readyState < 2) {
                return;
            }

            if (Math.abs(video.currentTime - lastTime) < 0.05) {
                stagnantCount++;
            } else {
                stagnantCount = 0;
                lastTime = video.currentTime;
            }

            if (stagnantCount < 5) {
                return;
            }

            stagnantCount = 0;
            console.warn(LOG, 'playback stagnant. restarting player:', elementId);

            if (current.hls) {
                try {
                    current.hls.stopLoad();
                    current.hls.startLoad(-1);
                    safePlay(video, elementId);
                    return;
                } catch { }
            }

            init(elementId, source);
        }, 1000);
    }

    function scheduleRetry(elementId, source, delayMs) {
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

        destroy(elementId);
        configureVideo(video);

        const ready = await waitUntilReady(source, 30000, 500);
        if (!ready) {
            console.warn(LOG, 'playlist or segment was not ready within timeout:', source);
            players.set(elementId, { video: video, hls: null, watchdogId: null, retryTimerId: null });
            scheduleRetry(elementId, source, 2000);
            return;
        }

        if (canUseNativeHls(video)) {
            console.log(LOG, 'using native HLS:', elementId);
            video.src = source;
            players.set(elementId, { video: video, hls: null, watchdogId: null, retryTimerId: null });
            await safePlay(video, elementId);
            startPlaybackWatchdog(elementId, source);
            return;
        }

        if (!window.Hls || !window.Hls.isSupported()) {
            console.warn(LOG, 'hls.js not available, fallback to native src:', elementId);
            video.src = source;
            players.set(elementId, { video: video, hls: null, watchdogId: null, retryTimerId: null });
            await safePlay(video, elementId);
            startPlaybackWatchdog(elementId, source);
            return;
        }

        const hls = new window.Hls({
            lowLatencyMode: false,
            enableWorker: true,
            backBufferLength: 30,
            liveSyncDurationCount: 2,
            liveMaxLatencyDurationCount: 6,
            manifestLoadingMaxRetry: 999,
            manifestLoadingRetryDelay: 500,
            manifestLoadingMaxRetryTimeout: 5000,
            levelLoadingMaxRetry: 999,
            fragLoadingMaxRetry: 999,
            appendErrorMaxRetry: 5
        });

        players.set(elementId, { video: video, hls: hls, watchdogId: null, retryTimerId: null });

        hls.attachMedia(video);

        hls.on(window.Hls.Events.MEDIA_ATTACHED, function () {
            console.log(LOG, 'media attached, loading source:', source);
            hls.loadSource(source);
            hls.startLoad(-1);
        });

        hls.on(window.Hls.Events.MANIFEST_PARSED, function (_event, data) {
            const levelCount = data && data.levels ? data.levels.length : 'n/a';
            console.log(LOG, 'manifest parsed:', elementId, 'levels:', levelCount);
            safePlay(video, elementId);
        });

        hls.on(window.Hls.Events.LEVEL_LOADED, function () {
            safePlay(video, elementId);
        });

        hls.on(window.Hls.Events.FRAG_BUFFERED, function (_event, data) {
            const sn = data && data.frag ? data.frag.sn : 'n/a';
            console.log(LOG, 'fragment buffered:', elementId, 'sn:', sn);
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

            if (!data.fatal) {
                return;
            }

            if (data.type === window.Hls.ErrorTypes.NETWORK_ERROR) {
                try {
                    hls.startLoad(-1);
                } catch {
                    scheduleRetry(elementId, source, 1500);
                }
                return;
            }

            if (data.type === window.Hls.ErrorTypes.MEDIA_ERROR) {
                try {
                    hls.recoverMediaError();
                } catch {
                    scheduleRetry(elementId, source, 1500);
                }
                return;
            }

            scheduleRetry(elementId, source, 1500);
        });

        startPlaybackWatchdog(elementId, source);
    }

    window.dreamineVmsHls = {
        init: init,
        destroy: destroy
    };
})();
