(() => {
    const games = new WeakMap();
    const clamp = (value, min, max) => Math.max(min, Math.min(max, value));
    const loadImage = url => {
        if (!url) return null;
        const image = new Image();
        image.decoding = "async";
        image.src = url;
        return image;
    };
    const cover = (ctx, image, width, height) => {
        if (!image?.complete || !image.naturalWidth) return false;
        const scale = Math.max(width / image.naturalWidth, height / image.naturalHeight);
        const drawWidth = image.naturalWidth * scale;
        const drawHeight = image.naturalHeight * scale;
        ctx.drawImage(image, (width - drawWidth) / 2, (height - drawHeight) / 2, drawWidth, drawHeight);
        return true;
    };
    const drawImage = (ctx, image, x, y, size, fallback) => {
        if (image?.complete && image.naturalWidth) ctx.drawImage(image, x - size / 2, y - size / 2, size, size);
        else { ctx.fillStyle = fallback; ctx.beginPath(); ctx.arc(x, y, size * .34, 0, Math.PI * 2); ctx.fill(); }
    };
    const drawFittedText = (ctx, text, x, y, maxWidth, maxSize, minSize = 10, weight = 900) => {
        let size = maxSize;
        ctx.font = `${weight} ${size}px system-ui`;
        while (size > minSize && ctx.measureText(text).width > maxWidth) {
            size -= .5;
            ctx.font = `${weight} ${size}px system-ui`;
        }
        ctx.fillText(text, x, y);
    };
    const formatTime = seconds => `${Math.floor(seconds / 60)}:${String(seconds % 60).padStart(2, "0")}`;
    const towerEvolutions = [
        { name: "목책 망루", shortName: "망루", role: "균형 사격", color: "#8ee9d7", buildCost: 50 },
        { name: "연사 화살탑", shortName: "화살", role: "질주 특효", color: "#ffe17d", buildCost: 70 },
        { name: "조총 감시루", shortName: "조총", role: "중갑 관통", color: "#ffad67", buildCost: 90 },
        { name: "화포 성루", shortName: "화포", role: "군집 폭격", color: "#8ff7ff", buildCost: 110 }
    ];
    const enemyKinds = {
        normal: { name: "마물", color: "#ff6958", hp: 1, speed: 1, gate: 1, energy: 11, size: 72 },
        runner: { name: "질주", color: "#7ffff0", hp: .56, speed: 1.72, gate: .7, energy: 7, size: 58 },
        armored: { name: "중갑", color: "#ffad67", hp: 2.35, speed: .64, gate: 1.65, energy: 17, size: 88 },
        swarm: { name: "군집", color: "#c690ff", hp: .40, speed: 1.12, gate: .48, energy: 4, size: 50 },
        boss: { name: "우두머리", color: "#ffd45f", hp: 5.5, speed: .53, gate: 2.5, energy: 46, size: 112 }
    };
    const effectiveTowerLevel = level => Math.min(level, 10) + Math.pow(Math.max(0, level - 10), .72);
    const towerCost = (level, type) => Math.round(44 + level * 18 + type * 14 + Math.pow(level, 1.45) * 7);
    const towerDamage = (type, level) => {
        const effectiveLevel = effectiveTowerLevel(level);
        if (type === 0) return Math.round(20 + effectiveLevel * 10);
        if (type === 1) return Math.round(18 + effectiveLevel * 6);
        if (type === 2) return Math.round(72 + effectiveLevel * 28);
        return Math.round(112 + effectiveLevel * 34);
    };
    const towerCooldown = (type, level) => {
        if (type === 0) return Math.max(.32, .67 - level * .026);
        if (type === 1) return Math.max(.16, .30 - level * .012);
        if (type === 2) return Math.max(.48, .88 - level * .018);
        return Math.max(.60, 1.08 - level * .018);
    };
    const towerRange = (type, level) => Math.min(.38, .23 + level * .007 + type * .014);
    const combatProfiles = [
        { key: "runner", name: "질주", hint: "화살탑 유리", color: "#7ffff0" },
        { key: "swarm", name: "군집", hint: "화포 유리", color: "#c690ff" },
        { key: "armored", name: "중갑", hint: "조총 유리", color: "#ffad67" },
        { key: "mixed", name: "혼성", hint: "균형 대응", color: "#8ee9d7" }
    ];
    const waveAffixes = [
        { key: "haste", name: "폭주", speed: 1.18, hp: 1, gate: 1, shield: 0 },
        { key: "iron", name: "강철", speed: 1, hp: 1.30, gate: 1, shield: 0 },
        { key: "breach", name: "결사", speed: 1, hp: 1, gate: 1.36, shield: 0 },
        { key: "ward", name: "보호막", speed: 1, hp: 1, gate: 1, shield: 2 }
    ];
    const rollWaveProfile = (wave, previousKey) => {
        const base = wave % 5 === 0
            ? { key: "boss", name: "우두머리", hint: "조총 집중", color: "#ffd45f" }
            : wave < 6
                ? { key: "normal", name: "전초", hint: "균형 배치", color: "#8ee9d7" }
                : combatProfiles.filter(profile => profile.key !== previousKey)[Math.floor(Math.random() * combatProfiles.filter(profile => profile.key !== previousKey).length)];
        const affixChance = wave < 10 ? 0 : Math.min(.9, .28 + (wave - 10) * .025);
        const affix = Math.random() < affixChance ? waveAffixes[Math.floor(Math.random() * waveAffixes.length)] : null;
        return {
            ...base,
            name: affix ? `${base.name}·${affix.name}` : base.name,
            speed: affix?.speed || 1,
            hp: affix?.hp || 1,
            gate: affix?.gate || 1,
            shield: affix?.shield || 0
        };
    };
    const profileFor = (state, wave) => {
        while (state.profiles.length <= wave) {
            const nextWave = state.profiles.length;
            state.profiles.push(rollWaveProfile(nextWave, state.profiles[nextWave - 1]?.key));
        }
        return state.profiles[wave];
    };
    const damageAgainst = (tier, baseDamage, enemy) => {
        if (enemy.kind === "runner") return Math.round(baseDamage * (tier === 1 ? 2.05 : tier === 3 ? .48 : .82));
        if (enemy.kind === "armored") return Math.round(baseDamage * (tier === 2 ? 2.15 : tier === 3 ? .72 : .42));
        if (enemy.kind === "swarm") return Math.round(baseDamage * (tier === 3 ? 1.65 : tier === 2 ? .62 : 1));
        if (enemy.kind === "boss") return Math.round(baseDamage * (tier === 2 ? 1.75 : .64));
        return baseDamage;
    };

    const drawTower = (ctx, image, tier, x, y, size) => {
        if (!image?.complete || !image.naturalWidth) {
            drawImage(ctx, image, x, y, size, towerEvolutions[tier].color);
            return;
        }
        const cellWidth = image.naturalWidth / 4;
        const sourceX = cellWidth * tier;
        const destinationHeight = size;
        const destinationWidth = size * (cellWidth / image.naturalHeight);
        ctx.drawImage(image, sourceX, 0, cellWidth, image.naturalHeight,
            x - destinationWidth / 2, y - destinationHeight * .72, destinationWidth, destinationHeight);
    };

    function stop(canvas) {
        const state = games.get(canvas);
        if (!state) return;
        state.stopped = true;
        cancelAnimationFrame(state.raf);
        canvas.removeEventListener("pointerdown", state.pointer);
        games.delete(canvas);
    }

    const createReport = state => ({
        durationSeconds: Math.floor(state.elapsed),
        kills: state.kills,
        waves: state.wave,
        towerLevels: state.pads.reduce((sum, pad) => sum + pad.level, 0),
        goldPickups: state.gold,
        materialPickups: state.materials,
        ticketPickups: state.tickets
    });

    function settle(canvas) {
        const state = games.get(canvas);
        if (!state || state.finished) return null;
        state.finished = state.stopped = true;
        cancelAnimationFrame(state.raf);
        canvas.removeEventListener("pointerdown", state.pointer);
        games.delete(canvas);
        return createReport(state);
    }

    function start(canvas, rootId, dotnet, config) {
        stop(canvas);
        const ctx = canvas.getContext("2d", { alpha: false });
        const w = canvas.width, h = canvas.height;
        const hud = document.getElementById(`${rootId}-hud`);
        const path = [
            { x: .57, y: -.04 }, { x: .59, y: .13 }, { x: .47, y: .25 },
            { x: .56, y: .37 }, { x: .44, y: .49 }, { x: .54, y: .61 },
            { x: .45, y: .73 }, { x: .52, y: .86 }, { x: .50, y: 1.02 }
        ];
        // These are the actual centres of the eight circular stone platforms painted
        // into spirit-fortress-v1.png (the canvas uses the same 2:3 aspect ratio).
        const pads = [
            { x: .383, y: .199 }, { x: .691, y: .267 },
            { x: .287, y: .342 }, { x: .711, y: .391 },
            { x: .261, y: .476 }, { x: .710, y: .518 },
            { x: .263, y: .627 }, { x: .730, y: .660 }
        ].map((pad, index) => ({ ...pad, index, type: null, level: 0, cooldown: 0, pulse: 0 }));
        const state = {
            ctx, w, h, hud, dotnet, stopped: false, finished: false,
            background: loadImage(config.backgroundUrl), enemyImage: loadImage(config.enemyUrl), towerImage: loadImage(config.towerUrl),
            elapsed: 0, wave: 1, previousWave: 1, bossWave: 0, kills: 0, energy: 90, gate: 100, repairs: 0,
            spawnClock: .45, enemies: [], projectiles: [], effects: [], pads, selectedPad: null,
            gold: 0, materials: 0, tickets: 0, profiles: [null], lastFrame: performance.now()
        };
        profileFor(state, 2);

        state.pointer = event => {
            const rect = canvas.getBoundingClientRect();
            const x = (event.clientX - rect.left) / rect.width;
            const y = (event.clientY - rect.top) / rect.height;
            if (state.selectedPad !== null && y >= .785 && y <= .925 && x >= .035 && x <= .965) {
                const type = Math.min(3, Math.floor((x - .035) / .2325));
                const option = towerEvolutions[type];
                const pad = state.pads[state.selectedPad];
                if (pad && pad.level === 0 && state.energy >= option.buildCost) {
                    state.energy -= option.buildCost;
                    pad.type = type;
                    pad.level = 1;
                    pad.pulse = .7;
                    state.effects.push({ x: pad.x, y: pad.y, life: .65, color: option.color, size: 28 + type * 5 });
                    state.selectedPad = null;
                } else if (pad) pad.pulse = -.35;
                canvas.focus({ preventScroll: true });
                event.preventDefault();
                return;
            }
            const repairCost = 90 + state.repairs * 38;
            const repairAmount = Math.max(6, 12 - Math.floor(state.repairs / 2));
            if (Math.hypot(x - .5, (y - .86) * 1.35) < .105) {
                if (state.gate < 100 && state.energy >= repairCost) {
                    state.energy -= repairCost;
                    state.gate = Math.min(100, state.gate + repairAmount);
                    state.repairs++;
                    state.effects.push({ x: .5, y: .86, life: .8, color: "#77f4d8", size: 48 });
                } else {
                    state.effects.push({ x: .5, y: .86, life: .35, color: "#ff6b59", size: 30 });
                }
                canvas.focus({ preventScroll: true });
                event.preventDefault();
                return;
            }
            const pad = state.pads.find(item => Math.hypot(item.x - x, (item.y - y) * 1.55) < .092);
            if (!pad) { state.selectedPad = null; return; }
            if (pad.level === 0) {
                state.selectedPad = pad.index;
                pad.pulse = .55;
                canvas.focus({ preventScroll: true });
                event.preventDefault();
                return;
            }
            state.selectedPad = null;
            const cost = towerCost(pad.level, pad.type);
            if (state.energy < cost) {
                pad.pulse = -.35;
            } else {
                state.energy -= cost;
                pad.level++;
                pad.pulse = .65;
                state.effects.push({ x: pad.x, y: pad.y, life: .55, color: towerEvolutions[pad.type].color, size: 22 + pad.type * 6 });
            }
            canvas.focus({ preventScroll: true });
            event.preventDefault();
        };
        canvas.addEventListener("pointerdown", state.pointer, { passive: false });
        games.set(canvas, state);

        function pointOnPath(progress) {
            const scaled = clamp(progress, 0, .9999) * (path.length - 1);
            const index = Math.floor(scaled), amount = scaled - index;
            const from = path[index], to = path[Math.min(path.length - 1, index + 1)];
            return { x: from.x + (to.x - from.x) * amount, y: from.y + (to.y - from.y) * amount };
        }
        function spawnEnemy(kind = "normal", progress = 0) {
            const definition = enemyKinds[kind] || enemyKinds.normal;
            const profile = profileFor(state, state.wave);
            const latePressure = 1 + Math.pow(Math.max(0, state.wave - 12), 1.32) * .012;
            const hp = Math.round((28 + state.wave * 10.5 + state.elapsed * .18) * definition.hp * profile.hp * latePressure);
            state.enemies.push({
                kind, progress, hp, maxHp: hp,
                speed: (.029 + Math.min(.023, state.wave * .00095)) * definition.speed * profile.speed,
                gateDamage: definition.gate * profile.gate, energyReward: definition.energy,
                shieldHits: profile.shield, maxShieldHits: profile.shield,
                elite: kind === "boss", hit: 0, incoming: 0
            });
        }
        function spawnWaveGroup() {
            const profile = profileFor(state, state.wave);
            if (profile.key === "boss" && state.bossWave !== state.wave) {
                state.bossWave = state.wave;
                spawnEnemy("boss");
                return;
            }
            const roll = Math.random();
            if (profile.key === "runner" && roll < .68) { spawnEnemy("runner"); return; }
            if (profile.key === "armored" && roll < .48) { spawnEnemy("armored"); return; }
            if (profile.key === "swarm" && roll < .72) {
                spawnEnemy("swarm", 0);
                spawnEnemy("swarm", -.012);
                spawnEnemy("swarm", -.024);
                return;
            }
            if (profile.key === "boss" && roll < .24) { spawnEnemy("armored"); return; }
            if (profile.key === "mixed") {
                spawnEnemy(["normal", "runner", "armored", "swarm"][Math.floor(Math.random() * 4)]);
                return;
            }
            if (state.wave > 8 && roll > .9) spawnEnemy(["runner", "armored", "swarm"][Math.floor(Math.random() * 3)]);
            else spawnEnemy("normal");
        }
        function finish() {
            if (state.finished || state.stopped) return;
            state.finished = state.stopped = true;
            cancelAnimationFrame(state.raf);
            canvas.removeEventListener("pointerdown", state.pointer);
            games.delete(canvas);
            const report = createReport(state);
            dotnet.invokeMethodAsync("FinishAsync", report.durationSeconds, report.kills, report.waves, report.towerLevels, report.goldPickups, report.materialPickups, report.ticketPickups).catch(() => {});
        }
        function reward(enemy) {
            state.kills++;
            state.energy = Math.min(9999, state.energy + enemy.energyReward);
            const roll = Math.random();
            if (roll < .045) state.gold++;
            else if (roll < .065) state.materials++;
            else if (roll < .069) state.tickets++;
        }
        function update(dt) {
            state.elapsed += dt;
            state.wave = Math.floor(state.elapsed / 14) + 1;
            if (state.wave !== state.previousWave) {
                profileFor(state, state.wave + 1);
                const warning = profileFor(state, state.wave);
                state.effects.push({ x: .5, y: .08, life: .8, color: warning.color, size: 30 });
            }
            state.previousWave = state.wave;
            state.spawnClock -= dt;
            if (state.spawnClock <= 0) {
                spawnWaveGroup();
                if (state.wave > 9 && Math.random() < Math.min(.32, (state.wave - 8) * .012)) spawnEnemy("normal");
                state.spawnClock = Math.max(.30, 1.14 - state.wave * .038) + Math.random() * .16;
            }
            for (const pad of state.pads) {
                pad.cooldown -= dt;
                pad.pulse = pad.pulse > 0 ? Math.max(0, pad.pulse - dt) : Math.min(0, pad.pulse + dt);
                if (pad.level <= 0 || pad.cooldown > 0) continue;
                const tier = pad.type;
                let target = null, fallback = null, best = -1, fallbackBest = -1;
                for (const enemy of state.enemies) {
                    if (enemy.dead) continue;
                    const position = pointOnPath(enemy.progress);
                    const distance = Math.hypot(position.x - pad.x, (position.y - pad.y) * 1.5);
                    if (distance > towerRange(tier, pad.level)) continue;
                    if (enemy.progress > fallbackBest) { fallback = enemy; fallbackBest = enemy.progress; }
                    const roleBonus = tier === 1 && enemy.kind === "runner" ? 1.6
                        : tier === 2 && (enemy.kind === "armored" || enemy.kind === "boss") ? 1.6
                        : tier === 3 && enemy.kind === "swarm" ? 1.6 : 0;
                    const score = enemy.progress + roleBonus;
                    if (enemy.hp - (enemy.incoming || 0) > 0 && score > best) { target = enemy; best = score; }
                }
                target ??= fallback;
                if (!target) continue;
                const critical = Math.random() < .075;
                const baseDamage = Math.round(towerDamage(tier, pad.level) * (critical ? 1.85 : 1));
                const expectedDamage = damageAgainst(tier, baseDamage, target);
                target.incoming = (target.incoming || 0) + expectedDamage;
                state.projectiles.push({ x: pad.x, y: pad.y, target, speed: 1.3 + tier * .17, baseDamage, expectedDamage, critical, splash: tier === 3 ? .062 : 0, life: 1.2, level: pad.level, tier });
                pad.cooldown = towerCooldown(tier, pad.level) * (state.gate <= 25 ? .82 : 1);
            }
            for (const projectile of state.projectiles) {
                projectile.life -= dt;
                if (projectile.target.dead) { projectile.target.incoming = Math.max(0, (projectile.target.incoming || 0) - projectile.expectedDamage); projectile.life = 0; continue; }
                const target = pointOnPath(projectile.target.progress);
                const dx = target.x - projectile.x, dy = target.y - projectile.y;
                const distance = Math.hypot(dx, dy);
                const step = projectile.speed * dt;
                if (distance <= step || distance < .012) {
                    projectile.target.incoming = Math.max(0, (projectile.target.incoming || 0) - projectile.expectedDamage);
                    const shielded = projectile.target.shieldHits > 0;
                    const impactDamage = Math.round(projectile.expectedDamage * (shielded ? .28 : 1));
                    if (shielded) projectile.target.shieldHits--;
                    projectile.target.hp -= impactDamage;
                    projectile.target.hit = .12;
                    if (projectile.splash > 0) {
                        for (const enemy of state.enemies) {
                            if (enemy === projectile.target || enemy.dead) continue;
                            const nearby = pointOnPath(enemy.progress);
                            if (Math.hypot(nearby.x - target.x, (nearby.y - target.y) * 1.5) > projectile.splash) continue;
                            const shieldMultiplier = enemy.shieldHits > 0 ? .28 : 1;
                            enemy.hp -= Math.round(damageAgainst(projectile.tier, projectile.baseDamage, enemy) * .55 * shieldMultiplier);
                            if (enemy.shieldHits > 0) enemy.shieldHits--;
                            enemy.hit = .12;
                            if (enemy.hp <= 0 && !enemy.dead) { enemy.dead = true; reward(enemy); }
                        }
                    }
                    state.effects.push({ x: target.x, y: target.y, life: .22, color: towerEvolutions[projectile.tier].color, size: 17 + projectile.tier * 12 });
                    if (projectile.target.hp <= 0 && !projectile.target.dead) { projectile.target.dead = true; reward(projectile.target); }
                    projectile.life = 0;
                } else { projectile.x += dx / distance * step; projectile.y += dy / distance * step; }
            }
            state.projectiles = state.projectiles.filter(projectile => projectile.life > 0);
            for (const enemy of state.enemies) {
                if (enemy.dead) continue;
                enemy.progress += dt * enemy.speed;
                enemy.hit = Math.max(0, enemy.hit - dt);
                if (enemy.progress < 1) continue;
                enemy.dead = true;
                const baseGateDamage = Math.min(18, 9 + Math.floor(state.wave / 8));
                state.gate = Math.max(0, state.gate - Math.round(baseGateDamage * enemy.gateDamage));
                state.effects.push({ x: .5, y: .91, life: .7, color: "#ff5f52", size: 42 });
                if (state.gate <= 0) finish();
            }
            state.enemies = state.enemies.filter(enemy => !enemy.dead);
            for (const effect of state.effects) effect.life -= dt;
            state.effects = state.effects.filter(effect => effect.life > 0);
        }
        function draw() {
            if (!cover(ctx, state.background, w, h)) { ctx.fillStyle = "#06151c"; ctx.fillRect(0, 0, w, h); }
            ctx.fillStyle = "rgba(1,8,13,.14)";
            ctx.fillRect(0, 0, w, h);

            const currentProfile = profileFor(state, state.wave), nextProfile = profileFor(state, state.wave + 1);
            const waveProgress = (state.elapsed % 14) / 14;
            ctx.save();
            const scoutLeft = 12, scoutTop = 12, scoutWidth = Math.min(300, w - 24), scoutHeight = 90;
            const scoutRight = scoutLeft + scoutWidth;
            const scoutGradient = ctx.createLinearGradient(scoutLeft, scoutTop, scoutRight, scoutTop);
            scoutGradient.addColorStop(0, "rgba(3,24,29,.96)"); scoutGradient.addColorStop(1, "rgba(3,12,17,.9)");
            ctx.fillStyle = scoutGradient; ctx.strokeStyle = currentProfile.color; ctx.lineWidth = 2;
            ctx.shadowColor = currentProfile.color; ctx.shadowBlur = 13;
            ctx.beginPath(); ctx.roundRect(scoutLeft, scoutTop, scoutWidth, scoutHeight, 12); ctx.fill(); ctx.stroke();
            ctx.shadowBlur = 0;
            ctx.fillStyle = "#8faeaa"; ctx.font = "900 10px system-ui"; ctx.textAlign = "left"; ctx.fillText(`WAVE ${state.wave} · 정찰 보고`, scoutLeft + 13, scoutTop + 18);
            ctx.fillStyle = currentProfile.color;
            drawFittedText(ctx, `${currentProfile.name} · ${currentProfile.hint}`, scoutLeft + 13, scoutTop + 40, scoutWidth - 26, 16, 12);
            ctx.fillStyle = "rgba(255,255,255,.12)"; ctx.fillRect(scoutLeft + 13, scoutTop + 50, scoutWidth - 26, 5);
            ctx.fillStyle = currentProfile.color; ctx.fillRect(scoutLeft + 13, scoutTop + 50, (scoutWidth - 26) * waveProgress, 5);
            ctx.fillStyle = "#8faeaa"; ctx.font = "800 10px system-ui"; ctx.fillText("NEXT", scoutLeft + 13, scoutTop + 72);
            ctx.fillStyle = nextProfile.color;
            drawFittedText(ctx, `${nextProfile.name} · ${nextProfile.hint}`, scoutLeft + 53, scoutTop + 72, scoutWidth - 66, 12, 10);
            ctx.restore();

            for (const pad of state.pads) {
                const x = pad.x * w, y = pad.y * h;
                ctx.save();
                const option = pad.level ? towerEvolutions[pad.type] : null;
                ctx.shadowColor = option?.color || "#d9b957";
                ctx.shadowBlur = pad.pulse < 0 ? 6 : 16 + pad.pulse * 22;
                ctx.fillStyle = pad.level ? "rgba(7,47,50,.68)" : "rgba(8,18,22,.50)";
                ctx.strokeStyle = pad.pulse < 0 ? "#ff6b59" : option?.color || (state.selectedPad === pad.index ? "#ffffff" : "rgba(255,222,126,.76)");
                ctx.lineWidth = pad.level ? 4 : 2;
                ctx.beginPath(); ctx.ellipse(x, y, 54, 31, 0, 0, Math.PI * 2); ctx.fill(); ctx.stroke();
                if (pad.level) {
                    const tier = pad.type;
                    drawTower(ctx, state.towerImage, tier, x, y - 11, 88 + tier * 7);
                    const plateGradient = ctx.createLinearGradient(x - 59, y + 32, x + 59, y + 69);
                    plateGradient.addColorStop(0, "rgba(2,17,21,.94)"); plateGradient.addColorStop(1, "rgba(8,29,31,.9)");
                    ctx.fillStyle = plateGradient; ctx.strokeStyle = towerEvolutions[tier].color; ctx.lineWidth = 1;
                    ctx.beginPath(); ctx.roundRect(x - 60, y + 31, 120, 40, 8); ctx.fill(); ctx.stroke();
                    ctx.fillStyle = towerEvolutions[tier].color; ctx.font = "900 12px system-ui"; ctx.textAlign = "center";
                    ctx.fillText(`${towerEvolutions[tier].shortName} · Lv.${pad.level}`, x, y + 48);
                    ctx.fillStyle = "#f9ddb0"; ctx.font = "800 10px system-ui"; ctx.fillText(`강화 ${towerCost(pad.level, tier)} 영기`, x, y + 64);
                } else {
                    ctx.fillStyle = state.selectedPad === pad.index ? "#ffffff" : "#ffe08a"; ctx.font = "900 15px system-ui"; ctx.textAlign = "center";
                    ctx.fillText(state.selectedPad === pad.index ? "탑 선택 ↓" : "건설", x, y + 5);
                }
                ctx.restore();
            }
            if (state.selectedPad !== null) {
                const panelTop = .735 * h, cardsTop = .785 * h, left = .035 * w, cellWidth = .2325 * w, panelHeight = .19 * h;
                ctx.save();
                const panelGradient = ctx.createLinearGradient(0, panelTop, 0, panelTop + panelHeight);
                panelGradient.addColorStop(0, "rgba(13,39,42,.97)"); panelGradient.addColorStop(1, "rgba(1,10,14,.98)");
                ctx.fillStyle = panelGradient; ctx.strokeStyle = "#e2be63"; ctx.lineWidth = 2;
                ctx.shadowColor = "rgba(0,0,0,.9)"; ctx.shadowBlur = 24;
                ctx.beginPath(); ctx.roundRect(left - 8, panelTop, cellWidth * 4 + 16, panelHeight, 16); ctx.fill(); ctx.stroke();
                ctx.shadowBlur = 0;
                ctx.fillStyle = "#64e9d8"; ctx.font = "900 10px system-ui"; ctx.textAlign = "left"; ctx.fillText("FORTRESS COMMAND", left + 10, panelTop + 19);
                ctx.fillStyle = "#ffe9b0"; ctx.font = "900 16px system-ui"; ctx.fillText(`제 ${state.selectedPad + 1} 설치대 · 방어시설 선택`, left + 10, panelTop + 40);
                towerEvolutions.forEach((item, type) => {
                    const cardX = left + type * cellWidth, cardWidth = cellWidth - 6, cardHeight = .13 * h;
                    const cardGradient = ctx.createLinearGradient(cardX, cardsTop, cardX, cardsTop + cardHeight);
                    cardGradient.addColorStop(0, "rgba(24,55,55,.96)"); cardGradient.addColorStop(1, "rgba(5,21,25,.96)");
                    ctx.fillStyle = cardGradient; ctx.strokeStyle = item.color; ctx.lineWidth = 1.5;
                    ctx.beginPath(); ctx.roundRect(cardX + 3, cardsTop, cardWidth, cardHeight, 11); ctx.fill(); ctx.stroke();
                    ctx.save(); ctx.shadowColor = item.color; ctx.shadowBlur = 12;
                    drawTower(ctx, state.towerImage, type, cardX + cellWidth / 2, cardsTop + 43, 62);
                    ctx.restore();
                    ctx.fillStyle = "rgba(2,10,13,.84)"; ctx.fillRect(cardX + 8, cardsTop + 70, cardWidth - 10, 43);
                    ctx.fillStyle = item.color; ctx.font = "900 13px system-ui"; ctx.textAlign = "center"; ctx.fillText(item.shortName, cardX + cellWidth / 2, cardsTop + 86);
                    ctx.fillStyle = "#d8e5df"; ctx.font = "800 10px system-ui"; ctx.fillText(item.role, cardX + cellWidth / 2, cardsTop + 101);
                    ctx.fillStyle = "#ffe08a"; ctx.font = "900 11px system-ui"; ctx.fillText(`${item.buildCost} 영기`, cardX + cellWidth / 2, cardsTop + 116);
                });
                ctx.restore();
            }
            if (state.gate < 100) {
                const x = .5 * w, y = .875 * h;
                const repairCost = 90 + state.repairs * 38;
                const repairAmount = Math.max(6, 12 - Math.floor(state.repairs / 2));
                ctx.save();
                if (state.gate <= 25) {
                    ctx.fillStyle = "rgba(99,20,13,.88)"; ctx.strokeStyle = "#ffcc67";
                    ctx.beginPath(); ctx.roundRect(x - 92, y - 55, 184, 27, 13); ctx.fill(); ctx.stroke();
                    ctx.fillStyle = "#ffe08a"; ctx.font = "900 12px system-ui"; ctx.textAlign = "center";
                    ctx.fillText("최후 저항 · 포탑 공속 +18%", x, y - 37);
                }
                ctx.fillStyle = "rgba(3,16,20,.9)"; ctx.strokeStyle = state.energy >= repairCost ? "#75f2dd" : "#ff7a67"; ctx.lineWidth = 2;
                ctx.beginPath(); ctx.roundRect(x - 82, y - 18, 164, 36, 18); ctx.fill(); ctx.stroke();
                ctx.fillStyle = state.energy >= repairCost ? "#b8fff2" : "#ffb0a3"; ctx.font = "900 13px system-ui"; ctx.textAlign = "center";
                ctx.fillText(`성문 수리 ${repairCost} · +${repairAmount}%`, x, y + 5);
                ctx.restore();
            }
            for (const enemy of state.enemies) {
                const position = pointOnPath(enemy.progress), x = position.x * w, y = position.y * h;
                const definition = enemyKinds[enemy.kind];
                ctx.save();
                if (enemy.shieldHits > 0) {
                    ctx.strokeStyle = "#8edcff"; ctx.lineWidth = 4; ctx.shadowColor = "#58caff"; ctx.shadowBlur = 18;
                    ctx.beginPath(); ctx.arc(x, y, definition.size * .48, 0, Math.PI * 2); ctx.stroke();
                }
                if (enemy.hit > 0) { ctx.shadowColor = "#fff2a0"; ctx.shadowBlur = 28; }
                drawImage(ctx, state.enemyImage, x, y, definition.size, definition.color);
                ctx.fillStyle = "rgba(28,4,5,.9)"; ctx.fillRect(x - 34, y - 42, 68, 6);
                ctx.fillStyle = definition.color; ctx.fillRect(x - 34, y - 42, 68 * clamp(enemy.hp / enemy.maxHp, 0, 1), 6);
                if (enemy.kind !== "normal") {
                    ctx.fillStyle = "rgba(2,10,13,.88)"; ctx.fillRect(x - 28, y - 58, 56, 16);
                    ctx.fillStyle = definition.color; ctx.font = "900 11px system-ui"; ctx.textAlign = "center"; ctx.fillText(definition.name, x, y - 46);
                }
                ctx.restore();
            }
            for (const projectile of state.projectiles) {
                const x = projectile.x * w, y = projectile.y * h;
                const color = projectile.critical ? "#fff4a3" : towerEvolutions[projectile.tier].color;
                ctx.save(); ctx.shadowColor = color; ctx.shadowBlur = projectile.tier === 3 ? 25 : 17;
                ctx.fillStyle = color; ctx.beginPath(); ctx.arc(x, y, 4 + projectile.tier * 2 + (projectile.critical ? 3 : 0), 0, Math.PI * 2); ctx.fill(); ctx.restore();
            }
            for (const effect of state.effects) {
                ctx.save(); ctx.globalAlpha = clamp(effect.life * 3, 0, 1); ctx.strokeStyle = effect.color; ctx.lineWidth = 5; ctx.shadowColor = effect.color; ctx.shadowBlur = 24;
                ctx.beginPath(); ctx.arc(effect.x * w, effect.y * h, effect.size * (1.3 - effect.life), 0, Math.PI * 2); ctx.stroke(); ctx.restore();
            }
        }
        function updateHud() {
            if (!hud) return;
            hud.querySelector("[data-fortress-time]").textContent = formatTime(Math.floor(state.elapsed));
            const profile = profileFor(state, state.wave);
            hud.querySelector("[data-fortress-wave]").textContent = `${state.wave}파 · ${profile.name}`;
            hud.querySelector("[data-fortress-energy]").textContent = state.energy.toLocaleString();
            hud.querySelector("[data-fortress-gate]").textContent = `${state.gate}%`;
        }
        function frame(now) {
            if (state.stopped) return;
            const dt = clamp((now - state.lastFrame) / 1000, 0, .05);
            state.lastFrame = now;
            update(dt); draw(); updateHud();
            if (!state.stopped) state.raf = requestAnimationFrame(frame);
        }
        canvas.focus({ preventScroll: true });
        state.raf = requestAnimationFrame(frame);
    }

    window.maruSpiritFortress = { start, stop, settle };
})();
