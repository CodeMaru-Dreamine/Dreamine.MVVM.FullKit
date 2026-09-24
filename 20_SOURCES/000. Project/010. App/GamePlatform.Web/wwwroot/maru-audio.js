(() => {
    let context = null;
    let musicBus = null;
    let sfxBus = null;
    let uiBus = null;
    let current = null;
    let retiring = [];
    let paused = false;
    let explorationMuted = false;
    let explorationEpoch = 0;
    let applied = { bgm: 0.52, sfx: 0.72, ui: 0.56, muted: false, crossfadeMilliseconds: 1500 };
    let musicRequest = 0;
    let gestureUnlockHandler = null;
    const effectBuffers = new Map();
    const activeEffects = new Map();
    const lastEffectAt = new Map();

    const createContext = () => {
        if (context) return true;
        const AudioContextType = window.AudioContext || window.webkitAudioContext;
        if (!AudioContextType) return false;
        context = new AudioContextType({ latencyHint: "interactive" });
        musicBus = context.createGain();
        sfxBus = context.createGain();
        uiBus = context.createGain();
        musicBus.connect(context.destination);
        sfxBus.connect(context.destination);
        uiBus.connect(context.destination);
        return true;
    };

    const volume = value => Math.max(0, Math.min(1, Number(value) || 0));
    const updateGains = () => {
        if (!context) return;
        const now = context.currentTime;
        musicBus.gain.setTargetAtTime(paused ? 0 : volume(applied.bgm), now, .025);
        sfxBus.gain.setTargetAtTime(volume(applied.sfx), now, .025);
        uiBus.gain.setTargetAtTime(volume(applied.ui), now, .025);
    };

    const random = seed => {
        let value = seed >>> 0;
        return () => ((value = (value * 1664525 + 1013904223) >>> 0) / 4294967296);
    };

    const buildBuffer = track => {
        const duration = Math.max(4, Number(track.loopEnd) || 8);
        const sampleRate = Math.min(24000, context.sampleRate);
        const buffer = context.createBuffer(2, Math.floor(duration * sampleRate), sampleRate);
        const root = Math.max(60, Number(track.rootHz) || 180);
        const seeded = random(Number(track.seed) || 1);
        const phases = [seeded() * Math.PI * 2, seeded() * Math.PI * 2, seeded() * Math.PI * 2];
        const ratios = [1, 1.5, 2, 2.5];
        for (let channel = 0; channel < 2; channel++) {
            const data = buffer.getChannelData(channel);
            let noise = 0;
            for (let i = 0; i < data.length; i++) {
                const t = i / sampleRate;
                const drift = 1 + Math.sin(t * Math.PI / duration * 2 + channel * .7) * .006;
                const pad = ratios.reduce((sum, ratio, index) =>
                    sum + Math.sin(t * root * ratio * drift * Math.PI * 2 + phases[index % 3] + channel * .13) / (index + 1.5), 0);
                noise = noise * .986 + (seeded() * 2 - 1) * .014;
                const pulse = Math.sin(t * Math.PI * 2 / 2) * .5 + .5;
                const edge = Math.min(1, i / 900, (data.length - i - 1) / 900);
                data[i] = (pad * .105 + noise * .07 * pulse) * edge * (Number(track.volumeNormalization) || 1);
            }
        }
        return buffer;
    };

    const loadTrackBuffer = async track => {
        if (track.sourceUrl) {
            try {
                const response = await fetch(track.sourceUrl, { cache: "force-cache" });
                if (!response.ok) throw new Error(`HTTP ${response.status}`);
                return await context.decodeAudioData(await response.arrayBuffer());
            } catch (error) {
                console.warn(`Maru BGM '${track.assetKey}' file decode failed; using procedural fallback.`, error);
            }
        }
        return buildBuffer(track);
    };

    const loadEffectBuffer = async effect => {
        if (!effect?.sourceUrl) throw new Error("Missing effect source URL");
        if (!effectBuffers.has(effect.sourceUrl)) {
            const pending = fetch(effect.sourceUrl, { cache: "force-cache" })
                .then(response => {
                    if (!response.ok) throw new Error(`HTTP ${response.status}`);
                    return response.arrayBuffer();
                })
                .then(data => context.decodeAudioData(data))
                .catch(error => {
                    effectBuffers.delete(effect.sourceUrl);
                    console.warn(`Maru SFX '${effect.assetKey}' decode failed.`, error);
                    throw error;
                });
            effectBuffers.set(effect.sourceUrl, pending);
        }
        return effectBuffers.get(effect.sourceUrl);
    };

    const stopEffectNode = entry => {
        try { entry.source.stop(); } catch { }
        try { entry.source.disconnect(); entry.gain.disconnect(); } catch { }
    };

    const stopNode = node => {
        try { node.source.stop(); } catch { }
        try { node.source.disconnect(); node.gain.disconnect(); } catch { }
    };

    const unbindGestureUnlock = () => {
        if (!gestureUnlockHandler) return;
        document.removeEventListener("pointerdown", gestureUnlockHandler, true);
        document.removeEventListener("keydown", gestureUnlockHandler, true);
        gestureUnlockHandler = null;
    };

    window.maruAudio = {
        initialize(settings, effects) {
            if (!createContext()) return false;
            this.applySettings(settings);
            for (const effect of effects || []) void loadEffectBuffer(effect).catch(() => false);
            return true;
        },

        async unlock() {
            if (!createContext()) return false;
            try {
                await context.resume();
                return context.state === "running";
            } catch (error) {
                console.warn("Maru audio unlock failed", error);
                return false;
            }
        },

        bindAutoUnlock(dotNetReference) {
            if (gestureUnlockHandler) return;
            gestureUnlockHandler = () => {
                if (!createContext()) return;
                context.resume().then(async () => {
                    if (context?.state !== "running") return;
                    unbindGestureUnlock();
                    try {
                        await dotNetReference?.invokeMethodAsync("OnAudioGestureUnlocked");
                    } catch (error) {
                        console.warn("Maru audio unlock callback failed", error);
                    }
                }).catch(error => console.warn("Maru gesture audio unlock failed", error));
            };
            document.addEventListener("pointerdown", gestureUnlockHandler, true);
            document.addEventListener("keydown", gestureUnlockHandler, true);
        },

        applySettings(settings) {
            applied = { ...applied, ...(settings || {}) };
            updateGains();
        },

        async playMusic(track, settings) {
            if (!createContext()) return false;
            this.applySettings(settings);
            if (context.state !== "running") return false;
            if (current?.key === track.assetKey) return true;

            const request = ++musicRequest;
            const buffer = await loadTrackBuffer(track);
            if (request !== musicRequest || !context) return false;

            retiring.forEach(stopNode);
            retiring = [];
            const source = context.createBufferSource();
            const gain = context.createGain();
            source.buffer = buffer;
            source.loop = true;
            source.loopStart = Number(track.loopStart) || 0;
            source.loopEnd = Number(track.loopEnd) || source.buffer.duration;
            source.connect(gain).connect(musicBus);
            const now = context.currentTime;
            const fade = Math.max(.25, Math.min(5, (Number(applied.crossfadeMilliseconds) || 1500) / 1000));
            gain.gain.setValueAtTime(0, now);
            gain.gain.linearRampToValueAtTime(Math.max(.05, Math.min(2, Number(track.volumeNormalization) || 1)), now + fade);
            source.start(now);

            if (current) {
                current.gain.gain.cancelScheduledValues(now);
                current.gain.gain.setValueAtTime(current.gain.gain.value, now);
                current.gain.gain.linearRampToValueAtTime(0, now + fade);
                current.source.stop(now + fade + .05);
                retiring.push(current);
            }
            current = { key: track.assetKey, source, gain };
            return true;
        },

        setPaused(value) {
            paused = !!value;
            updateGains();
        },

        setExplorationMuted(value) {
            const next = !!value;
            if (next === explorationMuted) return;
            explorationMuted = next;
            explorationEpoch++;
            if (next) {
                for (const entries of activeEffects.values())
                    for (const entry of [...entries]) if (entry.scope === "exploration") stopEffectNode(entry);
            }
        },

        async playEffect(effect, uiEffect, scope = "exploration") {
            const epoch = explorationEpoch;
            const blocked = () => !uiEffect && scope === "exploration" && (explorationMuted || epoch !== explorationEpoch);
            if (blocked()) return false;
            if (!createContext()) return false;
            if (context.state !== "running") {
                try { await context.resume(); } catch { return false; }
            }
            if (context.state !== "running" || blocked()) return false;
            const now = context.currentTime;
            if (uiEffect) {
                const chime = context.createOscillator();
                const harmony = context.createOscillator();
                const chimeGain = context.createGain();
                const harmonyGain = context.createGain();
                chime.type = "sine";
                harmony.type = "triangle";
                const base = effect === "tutorial-open" ? 520 : effect === "premium-credit" ? 720 : effect === "companion-rank-up" ? 610 : 560;
                chime.frequency.setValueAtTime(base, now);
                chime.frequency.exponentialRampToValueAtTime(base * 1.55, now + .2);
                harmony.frequency.setValueAtTime(base * .75, now);
                harmony.frequency.exponentialRampToValueAtTime(base * 1.18, now + .24);
                chimeGain.gain.setValueAtTime(.34, now);
                chimeGain.gain.exponentialRampToValueAtTime(.0001, now + .25);
                harmonyGain.gain.setValueAtTime(.16, now);
                harmonyGain.gain.exponentialRampToValueAtTime(.0001, now + .3);
                chime.connect(chimeGain).connect(uiBus);
                harmony.connect(harmonyGain).connect(uiBus);
                chime.start(now);
                harmony.start(now);
                chime.stop(now + .26);
                harmony.stop(now + .31);
                return true;
            }
            if (!effect?.assetKey) return false;
            const wallNow = performance.now();
            const minimumGap = Math.max(0, Number(effect.minimumIntervalMilliseconds) || 0);
            if (wallNow - (lastEffectAt.get(effect.assetKey) || 0) < minimumGap) return false;
            lastEffectAt.set(effect.assetKey, wallNow);

            let buffer;
            try { buffer = await loadEffectBuffer(effect); }
            catch { return false; }
            if (!context || context.state !== "running" || blocked()) return false;

            const voices = activeEffects.get(effect.assetKey) || [];
            const maxVoices = Math.max(1, Math.min(16, Number(effect.maxVoices) || 1));
            while (voices.length >= maxVoices) stopEffectNode(voices.shift());

            const source = context.createBufferSource();
            const gain = context.createGain();
            const lowRate = Math.max(.5, Number(effect.playbackRateMin) || 1);
            const highRate = Math.max(lowRate, Number(effect.playbackRateMax) || lowRate);
            source.buffer = buffer;
            source.playbackRate.value = lowRate + Math.random() * (highRate - lowRate);
            gain.gain.value = Math.max(.01, Math.min(2, Number(effect.volumeNormalization) || 1));
            source.connect(gain).connect(sfxBus);
            const entry = { source, gain, scope };
            voices.push(entry);
            activeEffects.set(effect.assetKey, voices);
            source.onended = () => {
                const remaining = activeEffects.get(effect.assetKey) || [];
                const index = remaining.indexOf(entry);
                if (index >= 0) remaining.splice(index, 1);
                try { source.disconnect(); gain.disconnect(); } catch { }
            };
            source.start();
            return true;
        },

        supportsVibration() { return "vibrate" in navigator; },
        prefersReducedMotion() { return matchMedia("(prefers-reduced-motion: reduce)").matches; },
        loadGuestSettings() {
            try { return JSON.parse(localStorage.getItem("maru-idle:guest-settings") || "null"); }
            catch { return null; }
        },
        clearGuestSettings() { localStorage.removeItem("maru-idle:guest-settings"); },
        shouldShowUnlockHint() { return sessionStorage.getItem("maru-idle:audio-hint") !== "seen"; },
        markUnlockHintSeen() { sessionStorage.setItem("maru-idle:audio-hint", "seen"); },
        getDiagnostics() {
            return {
                contextState: context?.state || "closed",
                currentTrack: current?.key || null,
                decodedEffects: effectBuffers.size,
                activeEffects: Array.from(activeEffects.values()).reduce((sum, entries) => sum + entries.length, 0),
                explorationMuted
            };
        },

        async dispose() {
            musicRequest++;
            unbindGestureUnlock();
            retiring.forEach(stopNode);
            retiring = [];
            if (current) stopNode(current);
            current = null;
            for (const entries of activeEffects.values()) entries.forEach(stopEffectNode);
            activeEffects.clear();
            effectBuffers.clear();
            lastEffectAt.clear();
            if (context) {
                try { await context.close(); } catch { }
            }
            context = musicBus = sfxBus = uiBus = null;
        }
    };
})();
