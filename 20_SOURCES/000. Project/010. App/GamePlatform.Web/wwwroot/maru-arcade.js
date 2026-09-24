(() => {
    const veinGames = new WeakMap();
    const formationGames = new WeakMap();

    const loadImage = url => {
        if (!url) return null;
        const image = new Image();
        image.decoding = "async";
        image.src = url;
        return image;
    };
    const clamp = (value, min, max) => Math.max(min, Math.min(max, value));
    const roundedRect = (ctx, x, y, w, h, r) => { ctx.beginPath(); ctx.roundRect(x, y, w, h, r); };
    const cover = (ctx, image, width, height) => {
        if (!image?.complete || !image.naturalWidth) return false;
        const scale = Math.max(width / image.naturalWidth, height / image.naturalHeight);
        const w = image.naturalWidth * scale, h = image.naturalHeight * scale;
        ctx.drawImage(image, (width - w) / 2, (height - h) / 2, w, h);
        return true;
    };
    const drawImage = (ctx, image, x, y, size, fallback) => {
        if (image?.complete && image.naturalWidth) ctx.drawImage(image, x - size / 2, y - size / 2, size, size);
        else { ctx.fillStyle = fallback; ctx.beginPath(); ctx.arc(x, y, size * .34, 0, Math.PI * 2); ctx.fill(); }
    };
    const formatTime = seconds => `${Math.floor(seconds / 60)}:${String(seconds % 60).padStart(2, "0")}`;

    function stopVein(canvas) {
        const state = veinGames.get(canvas);
        if (!state) return;
        state.stopped = true;
        cancelAnimationFrame(state.raf);
        canvas.removeEventListener("pointerdown", state.pointer);
        canvas.removeEventListener("pointermove", state.pointerMove);
        canvas.removeEventListener("pointerup", state.pointerUp);
        canvas.removeEventListener("pointercancel", state.pointerUp);
        window.removeEventListener("keydown", state.keyDown);
        veinGames.delete(canvas);
    }

    function startVein(canvas, rootId, dotnet, config) {
        stopVein(canvas);
        const ctx = canvas.getContext("2d", { alpha: false });
        const w = canvas.width, h = canvas.height;
        const hud = document.getElementById(`${rootId}-hud`);
        const lanes = [.25, .5, .75];
        const state = {
            ctx, w, h, hud, dotnet, stopped: false, finished: false,
            bg: loadImage(config.backgroundUrl), player: loadImage(config.playerUrl),
            images: { gold: loadImage(config.goldUrl), material: loadImage(config.materialUrl), ticket: loadImage(config.ticketUrl) },
            targetX: lanes[1], playerX: lanes[1], elapsed: 0, distance: 0, essence: 0, combo: 1,
            shield: 3, spawnClock: .8, objects: [], sparks: [], gold: 0, materials: 0, tickets: 0,
            lastFrame: performance.now()
        };
        const setTarget = clientX => {
            const rect = canvas.getBoundingClientRect();
            state.targetX = clamp((clientX - rect.left) / rect.width, .13, .87);
        };
        state.pointer = event => { state.dragging = true; canvas.setPointerCapture?.(event.pointerId); setTarget(event.clientX); canvas.focus({ preventScroll: true }); event.preventDefault(); };
        state.pointerMove = event => { if (state.dragging || event.pointerType === "mouse") { setTarget(event.clientX); event.preventDefault(); } };
        state.pointerUp = event => { state.dragging = false; canvas.releasePointerCapture?.(event.pointerId); };
        state.keyDown = event => {
            if (event.key !== "ArrowLeft" && event.key !== "ArrowRight" && event.key !== "a" && event.key !== "d") return;
            state.targetX = clamp(state.targetX + ((event.key === "ArrowLeft" || event.key === "a") ? -.12 : .12), .13, .87);
            event.preventDefault();
        };
        canvas.addEventListener("pointerdown", state.pointer, { passive: false });
        canvas.addEventListener("pointermove", state.pointerMove, { passive: false });
        canvas.addEventListener("pointerup", state.pointerUp, { passive: true });
        canvas.addEventListener("pointercancel", state.pointerUp, { passive: true });
        window.addEventListener("keydown", state.keyDown, { passive: false });
        veinGames.set(canvas, state);

        function spawn() {
            const roll = Math.random();
            const type = roll < .43 ? "rift" : roll < .76 ? "essence" : roll < .87 ? "gold" : roll < .96 ? "material" : "ticket";
            state.objects.push({ lane: Math.floor(Math.random() * 3), y: -.08, type, spin: Math.random() * 6.28, hit: false });
        }
        function finish() {
            if (state.finished || state.stopped) return;
            state.finished = state.stopped = true;
            cancelAnimationFrame(state.raf);
            dotnet.invokeMethodAsync("FinishAsync", Math.floor(state.elapsed), Math.floor(state.distance), state.essence, state.gold, state.materials, state.tickets).catch(() => {});
        }
        function update(dt) {
            state.elapsed += dt;
            state.distance += dt * (16 + Math.min(12, state.elapsed * .08));
            state.playerX += (state.targetX - state.playerX) * Math.min(1, dt * 16);
            state.spawnClock -= dt;
            if (state.spawnClock <= 0) {
                spawn();
                if (state.elapsed > 55 && Math.random() < Math.min(.35, state.elapsed / 400)) spawn();
                state.spawnClock = Math.max(.36, .92 - state.elapsed * .0034) + Math.random() * .18;
            }
            const speed = .44 + Math.min(.38, state.elapsed * .0024);
            for (const item of state.objects) { item.y += dt * speed; item.spin += dt * 2.4; }
            state.objects = state.objects.filter(item => {
                if (item.y > 1.05) return false;
                if (item.hit || item.y < .74 || item.y > .91 || Math.abs(lanes[item.lane] - state.playerX) > .115) return true;
                item.hit = true;
                if (item.type === "rift") {
                    state.shield--; state.combo = 1;
                    state.sparks.push({ x: lanes[item.lane], y: .81, life: .55, danger: true });
                    if (state.shield <= 0) finish();
                } else {
                    state.combo = Math.min(12, state.combo + 1);
                    if (item.type === "essence") state.essence++;
                    else if (item.type === "gold") state.gold++;
                    else if (item.type === "material") state.materials++;
                    else state.tickets++;
                    state.sparks.push({ x: lanes[item.lane], y: .81, life: .42, danger: false });
                }
                return false;
            });
            for (const spark of state.sparks) spark.life -= dt;
            state.sparks = state.sparks.filter(spark => spark.life > 0);
        }
        function drawTrack() {
            if (!cover(ctx, state.bg, w, h)) {
                const gradient = ctx.createLinearGradient(0, 0, 0, h); gradient.addColorStop(0, "#092f3a"); gradient.addColorStop(1, "#031014");
                ctx.fillStyle = gradient; ctx.fillRect(0, 0, w, h);
            }
            ctx.fillStyle = "rgba(1,10,15,.19)"; ctx.fillRect(0, 0, w, h);
            for (let lane = 0; lane < 3; lane++) {
                const x = lanes[lane] * w;
                const glow = ctx.createLinearGradient(x - 80, 0, x + 80, 0);
                const selected = Math.abs(x / w - state.playerX) < .16;
                glow.addColorStop(0, "rgba(52,233,211,0)"); glow.addColorStop(.5, selected ? "rgba(255,244,184,.13)" : "rgba(40,123,129,.025)"); glow.addColorStop(1, "rgba(52,233,211,0)");
                ctx.fillStyle = glow; ctx.fillRect(x - 92, 0, 184, h);
            }
            ctx.strokeStyle = "rgba(220,255,249,.42)"; ctx.lineWidth = 3;
            for (let i = 0; i < 16; i++) {
                const y = ((i * 83 + state.distance * 5) % (h + 100)) - 50;
                const x = (i * 137 % w);
                ctx.globalAlpha = .18 + (y / h) * .35;
                ctx.beginPath(); ctx.moveTo(x, y); ctx.lineTo(x + (x - w / 2) * .06, y + 42); ctx.stroke();
            }
            ctx.globalAlpha = 1;
        }
        function drawObject(item) {
            const x = lanes[item.lane] * w, y = item.y * h;
            ctx.save(); ctx.translate(x, y); ctx.rotate(item.spin);
            if (item.type === "rift") {
                ctx.shadowColor = "#ff493c"; ctx.shadowBlur = 22; ctx.strokeStyle = "#ff6b55"; ctx.lineWidth = 9;
                ctx.beginPath(); ctx.moveTo(-28, -42); ctx.lineTo(14, -12); ctx.lineTo(-9, 8); ctx.lineTo(31, 42); ctx.stroke();
            } else if (item.type === "essence") {
                ctx.shadowColor = "#58f7dc"; ctx.shadowBlur = 25; ctx.fillStyle = "#79ffe7";
                ctx.beginPath(); ctx.moveTo(0, -34); ctx.lineTo(27, 0); ctx.lineTo(0, 34); ctx.lineTo(-27, 0); ctx.closePath(); ctx.fill();
                ctx.fillStyle = "#fff6b2"; ctx.beginPath(); ctx.arc(0, 0, 9, 0, Math.PI * 2); ctx.fill();
            } else {
                ctx.rotate(-item.spin); drawImage(ctx, state.images[item.type], 0, 0, 74, "#f6cf59");
            }
            ctx.restore();
        }
        function draw() {
            drawTrack();
            for (const item of state.objects) drawObject(item);
            ctx.save(); ctx.shadowColor = "#59f1dd"; ctx.shadowBlur = 27;
            ctx.fillStyle = "rgba(57,235,216,.18)"; ctx.beginPath(); ctx.ellipse(state.playerX * w, h * .86, 62, 22, 0, 0, Math.PI * 2); ctx.fill();
            ctx.strokeStyle = "#6af3df"; ctx.lineWidth = 4; ctx.stroke();
            drawImage(ctx, state.player, state.playerX * w, h * .80, 132, "#e8cf83"); ctx.restore();
            for (const spark of state.sparks) {
                ctx.globalAlpha = clamp(spark.life * 3, 0, 1); ctx.fillStyle = spark.danger ? "#ff5648" : "#7fffe8";
                ctx.beginPath(); ctx.arc(spark.x * w, spark.y * h, 30 + (1 - spark.life) * 70, 0, Math.PI * 2); ctx.fill(); ctx.globalAlpha = 1;
            }
            roundedRect(ctx, 18, 18, 230, 60, 14); ctx.fillStyle = "rgba(2,16,22,.88)"; ctx.fill();
            ctx.fillStyle = "#ffe39a"; ctx.font = "900 17px system-ui"; ctx.textAlign = "left"; ctx.fillText("영맥 수호막", 34, 45);
            for (let i = 0; i < 3; i++) {
                roundedRect(ctx, 147 + i * 27, 30, 20, 30, 6); ctx.fillStyle = i < state.shield ? "#72f7df" : "rgba(120,145,143,.24)"; ctx.fill();
                ctx.strokeStyle = i < state.shield ? "#fff3b1" : "rgba(180,193,190,.25)"; ctx.lineWidth = 1.5; ctx.stroke();
            }
        }
        function updateHud() {
            if (!hud) return;
            hud.querySelector("[data-vein-time]").textContent = formatTime(Math.floor(state.elapsed));
            hud.querySelector("[data-vein-distance]").textContent = `${Math.floor(state.distance).toLocaleString()}m`;
            hud.querySelector("[data-vein-essence]").textContent = state.essence.toLocaleString();
            hud.querySelector("[data-vein-combo]").textContent = `×${state.combo}`;
        }
        function frame(now) {
            if (state.stopped) return;
            const dt = clamp((now - state.lastFrame) / 1000, 0, .05); state.lastFrame = now;
            update(dt); draw(); updateHud(); if (!state.stopped) state.raf = requestAnimationFrame(frame);
        }
        canvas.focus({ preventScroll: true }); state.raf = requestAnimationFrame(frame);
    }

    function stopFormation(canvas) {
        const state = formationGames.get(canvas);
        if (!state) return;
        state.stopped = true; cancelAnimationFrame(state.raf);
        canvas.removeEventListener("pointerdown", state.pointer);
        window.removeEventListener("keydown", state.keyDown);
        formationGames.delete(canvas);
    }

    function startFormation(canvas, rootId, dotnet, config) {
        stopFormation(canvas);
        const ctx = canvas.getContext("2d", { alpha: false });
        const w = canvas.width, h = canvas.height, cx = w / 2, cy = h * .52;
        const hud = document.getElementById(`${rootId}-hud`);
        const vectors = [{ x: 0, y: -1 }, { x: 1, y: 0 }, { x: 0, y: 1 }, { x: -1, y: 0 }];
        const state = {
            ctx, w, h, hud, dotnet, stopped: false, finished: false,
            bg: loadImage(config.backgroundUrl), enemy: loadImage(config.enemyUrl), player: loadImage(config.playerUrl), sword: loadImage(config.swordUrl),
            elapsed: 0, direction: 0, core: 100, kills: 0, wave: 1, charge: 0,
            spawnClock: .5, strikeClock: 0, slashLife: 0, enemies: [], blades: [], sparks: [], gold: 0, materials: 0, tickets: 0,
            lastFrame: performance.now()
        };
        const chooseDirection = (x, y) => {
            const dx = x - cx, dy = y - cy;
            state.direction = Math.abs(dx) > Math.abs(dy) ? (dx > 0 ? 1 : 3) : (dy > 0 ? 2 : 0);
            state.slashLife = .16;
        };
        state.pointer = event => {
            const rect = canvas.getBoundingClientRect();
            chooseDirection((event.clientX - rect.left) / rect.width * w, (event.clientY - rect.top) / rect.height * h);
            canvas.focus({ preventScroll: true }); event.preventDefault();
        };
        state.keyDown = event => {
            const map = { ArrowUp: 0, w: 0, ArrowRight: 1, d: 1, ArrowDown: 2, s: 2, ArrowLeft: 3, a: 3 };
            if (map[event.key] === undefined) return;
            state.direction = map[event.key]; state.slashLife = .16; event.preventDefault();
        };
        canvas.addEventListener("pointerdown", state.pointer, { passive: false });
        window.addEventListener("keydown", state.keyDown, { passive: false });
        formationGames.set(canvas, state);

        function spawn() {
            const direction = Math.floor(Math.random() * 4);
            state.enemies.push({ direction, radius: .58, hp: 1 + Math.floor(state.elapsed / 55), maxHp: 1 + Math.floor(state.elapsed / 55), size: 62 + Math.min(28, state.elapsed * .15) });
        }
        function finish() {
            if (state.finished || state.stopped) return;
            state.finished = state.stopped = true; cancelAnimationFrame(state.raf);
            dotnet.invokeMethodAsync("FinishAsync", Math.floor(state.elapsed), state.kills, state.wave, state.gold, state.materials, state.tickets).catch(() => {});
        }
        function strike() {
            state.slashLife = .22;
            const charged = state.charge >= 8;
            state.blades.push({ direction: state.direction, radius: .105, power: charged ? 99 : 1, charged, hit: new Set() });
            if (charged) state.charge = 0;
        }
        function rewardKill(direction, charged) {
            state.kills++; state.charge = charged ? state.charge : Math.min(8, state.charge + 1);
            state.sparks.push({ direction, life: charged ? .58 : .3, charged });
            const roll = Math.random();
            if (roll < .035) state.gold++; else if (roll < .052) state.materials++; else if (roll < .056) state.tickets++;
        }
        function update(dt) {
            state.elapsed += dt; state.wave = Math.floor(state.elapsed / 12) + 1;
            state.spawnClock -= dt; state.strikeClock -= dt; state.slashLife -= dt;
            if (state.spawnClock <= 0) {
                spawn(); if (state.elapsed > 50 && Math.random() < Math.min(.42, state.elapsed / 360)) spawn();
                state.spawnClock = Math.max(.28, 1.02 - state.elapsed * .0048) + Math.random() * .14;
            }
            if (state.strikeClock <= 0) { strike(); state.strikeClock = Math.max(.16, .38 - state.wave * .004); }
            const speed = .055 + Math.min(.065, state.elapsed * .00028);
            for (const enemy of state.enemies) enemy.radius -= dt * speed;
            for (const blade of state.blades) {
                blade.radius += dt * (blade.charged ? 1.08 : .72);
                for (const enemy of state.enemies) {
                    if (enemy.dead || enemy.direction !== blade.direction || blade.hit.has(enemy) || Math.abs(enemy.radius - blade.radius) > .052) continue;
                    blade.hit.add(enemy); enemy.hp -= blade.power;
                    if (enemy.hp <= 0) { enemy.dead = true; rewardKill(enemy.direction, blade.charged); }
                }
            }
            state.blades = state.blades.filter(blade => blade.radius < .65);
            state.enemies = state.enemies.filter(enemy => {
                if (enemy.dead) return false;
                if (enemy.radius > .09) return true;
                state.core -= 8 + Math.min(8, Math.floor(state.wave / 4));
                state.sparks.push({ direction: enemy.direction, life: .48, danger: true });
                if (state.core <= 0) { state.core = 0; finish(); }
                return false;
            });
            for (const spark of state.sparks) spark.life -= dt;
            state.sparks = state.sparks.filter(spark => spark.life > 0);
        }
        function point(direction, radius) {
            const v = vectors[direction]; return { x: cx + v.x * radius * w, y: cy + v.y * radius * h * .72 };
        }
        function draw() {
            if (!cover(ctx, state.bg, w, h)) { ctx.fillStyle = "#06151c"; ctx.fillRect(0, 0, w, h); }
            ctx.fillStyle = "rgba(1,8,13,.22)"; ctx.fillRect(0, 0, w, h);
            const corridor = vectors[state.direction];
            const beam = ctx.createLinearGradient(cx, cy, cx + corridor.x * w * .58, cy + corridor.y * h * .42);
            beam.addColorStop(0, "rgba(104,246,222,.28)"); beam.addColorStop(1, "rgba(104,246,222,0)");
            ctx.save(); ctx.translate(cx, cy); ctx.rotate(state.direction * Math.PI / 2);
            ctx.fillStyle = beam; ctx.beginPath(); ctx.moveTo(-62, -48); ctx.lineTo(62, -48); ctx.lineTo(118, -500); ctx.lineTo(-118, -500); ctx.closePath(); ctx.fill(); ctx.restore();
            for (const enemy of state.enemies) {
                const p = point(enemy.direction, enemy.radius);
                drawImage(ctx, state.enemy, p.x, p.y, enemy.size * 1.55, "#a3473d");
                ctx.fillStyle = "#260b0b"; ctx.fillRect(p.x - 36, p.y - 46, 72, 7);
                ctx.fillStyle = "#ff6958"; ctx.fillRect(p.x - 36, p.y - 46, 72 * clamp(enemy.hp / enemy.maxHp, 0, 1), 7);
            }
            for (const blade of state.blades) {
                const p = point(blade.direction, blade.radius);
                ctx.save(); ctx.translate(p.x, p.y); ctx.rotate(blade.direction * Math.PI / 2);
                ctx.shadowColor = blade.charged ? "#ffe47a" : "#73f8e2"; ctx.shadowBlur = blade.charged ? 34 : 21;
                ctx.strokeStyle = blade.charged ? "#fff1a0" : "#b5fff1"; ctx.lineWidth = blade.charged ? 24 : 12;
                ctx.beginPath(); ctx.arc(0, 0, blade.charged ? 92 : 58, Math.PI * 1.13, Math.PI * 1.87); ctx.stroke(); ctx.restore();
            }
            const selected = vectors[state.direction];
            if (state.slashLife > 0) {
                ctx.save(); ctx.translate(cx, cy); ctx.rotate(state.direction * Math.PI / 2);
                ctx.globalAlpha = clamp(state.slashLife * 5, 0, 1); ctx.strokeStyle = state.charge >= 8 ? "#ffe36f" : "#7ffff0"; ctx.lineWidth = state.charge >= 8 ? 28 : 15;
                ctx.shadowColor = ctx.strokeStyle; ctx.shadowBlur = 28; ctx.beginPath(); ctx.arc(0, -48, 155, Math.PI * 1.12, Math.PI * 1.88); ctx.stroke(); ctx.restore();
            }
            ctx.save(); ctx.shadowColor = "#63f2dc"; ctx.shadowBlur = 34;
            ctx.fillStyle = "rgba(41,205,191,.23)"; ctx.beginPath(); ctx.arc(cx, cy, 94, 0, Math.PI * 2); ctx.fill();
            ctx.strokeStyle = "#f5d469"; ctx.lineWidth = 5; ctx.beginPath(); ctx.arc(cx, cy, 72, -.5 * Math.PI, (-.5 + 2 * state.charge / 8) * Math.PI); ctx.stroke();
            drawImage(ctx, state.player, cx, cy + 5, 148, "#e9ce7d");
            for (let i = 0; i < 3; i++) {
                const angle = state.elapsed * 1.7 + i * Math.PI * 2 / 3;
                ctx.save(); ctx.translate(Math.cos(angle) * 102, Math.sin(angle) * 102); ctx.rotate(angle + Math.PI / 2); drawImage(ctx, state.sword, 0, 0, 58, "#ffe377"); ctx.restore();
            }
            ctx.translate(selected.x * 72, selected.y * 72); ctx.restore();
            for (const spark of state.sparks) {
                const p = point(spark.direction, spark.danger ? .1 : .28);
                ctx.globalAlpha = clamp(spark.life * 3, 0, 1); ctx.fillStyle = spark.danger ? "#ff5347" : spark.charged ? "#ffe56e" : "#72fbe6";
                ctx.beginPath(); ctx.arc(p.x, p.y, 24 + (1 - spark.life) * 82, 0, Math.PI * 2); ctx.fill(); ctx.globalAlpha = 1;
            }
            roundedRect(ctx, 18, 18, 270, 68, 14); ctx.fillStyle = "rgba(2,15,21,.9)"; ctx.fill();
            ctx.fillStyle = state.charge >= 8 ? "#ffe66f" : "#d9f9f2"; ctx.font = "900 17px system-ui"; ctx.textAlign = "left"; ctx.fillText(state.charge >= 8 ? "천뢰 검기 준비 완료" : "검기 충전", 34, 44);
            roundedRect(ctx, 34, 55, 230, 12, 6); ctx.fillStyle = "rgba(111,142,141,.25)"; ctx.fill();
            roundedRect(ctx, 34, 55, 230 * state.charge / 8, 12, 6); ctx.fillStyle = state.charge >= 8 ? "#ffe065" : "#65ead7"; ctx.fill();
        }
        function updateHud() {
            if (!hud) return;
            hud.querySelector("[data-formation-time]").textContent = formatTime(Math.floor(state.elapsed));
            hud.querySelector("[data-formation-wave]").textContent = `${state.wave}파`;
            hud.querySelector("[data-formation-kills]").textContent = state.kills.toLocaleString();
            hud.querySelector("[data-formation-core]").textContent = `${Math.max(0, state.core)}%`;
        }
        function frame(now) {
            if (state.stopped) return;
            const dt = clamp((now - state.lastFrame) / 1000, 0, .05); state.lastFrame = now;
            update(dt); draw(); updateHud(); if (!state.stopped) state.raf = requestAnimationFrame(frame);
        }
        canvas.focus({ preventScroll: true }); state.raf = requestAnimationFrame(frame);
    }

    window.maruSpiritVein = { start: startVein, stop: stopVein };
    window.maruSwordFormation = { start: startFormation, stop: stopFormation };
})();
