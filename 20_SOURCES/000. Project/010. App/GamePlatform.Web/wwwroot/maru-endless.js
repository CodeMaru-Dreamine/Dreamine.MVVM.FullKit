(() => {
    const games = new WeakMap();

    function loadImage(url) {
        if (!url) return null;
        const image = new Image();
        image.decoding = "async";
        image.src = url;
        return image;
    }

    function cover(ctx, image, width, height) {
        if (!image?.complete || !image.naturalWidth) return false;
        const scale = Math.max(width / image.naturalWidth, height / image.naturalHeight);
        const w = image.naturalWidth * scale;
        const h = image.naturalHeight * scale;
        ctx.drawImage(image, (width - w) / 2, (height - h) / 2, w, h);
        return true;
    }

    function roundedRect(ctx, x, y, w, h, r) {
        ctx.beginPath();
        ctx.roundRect(x, y, w, h, r);
    }

    function stop(canvas) {
        const state = games.get(canvas);
        if (!state) return;
        state.stopped = true;
        cancelAnimationFrame(state.raf);
        canvas.removeEventListener("pointerdown", state.pointerDown);
        canvas.removeEventListener("pointermove", state.pointerMove);
        canvas.removeEventListener("pointerup", state.pointerUp);
        canvas.removeEventListener("pointercancel", state.pointerUp);
        window.removeEventListener("keydown", state.keyDown);
        games.delete(canvas);
    }

    function start(canvas, rootId, dotnet, config) {
        stop(canvas);
        const ctx = canvas.getContext("2d", { alpha: false });
        const width = canvas.width;
        const height = canvas.height;
        const root = document.getElementById(rootId);
        const hud = document.getElementById(`${rootId}-hud`);
        const state = {
            ctx, width, height, root, hud, dotnet,
            background: loadImage(config.backgroundUrl),
            enemyImage: loadImage(config.enemyUrl),
            players: (config.playerUrls || []).map(loadImage).filter(Boolean),
            weapons: (config.weaponUrls || []).map(loadImage).filter(Boolean),
            rewardImages: {
                gold: loadImage(config.goldUrl),
                material: loadImage(config.materialUrl),
                ticket: loadImage(config.ticketUrl)
            },
            stopped: false, finished: false, dragging: false,
            playerX: .5, targetX: .5, elapsed: 0, lastFrame: performance.now(),
            spawnClock: .55, pickupClock: 1.5, shotClock: 0,
            power: 1, allies: 1, shield: 4, maxShield: 4, shieldRecoveryAt: 35, kills: 0,
            goldPickups: 0, materialPickups: 0, ticketPickups: 0,
            bullets: [], enemies: [], pickups: [], flashes: []
        };

        const playerMinX = .20;
        const playerMaxX = .80;

        function roadLaneX(lane, y, visualHalfWidth) {
            const progress = Math.max(0, Math.min(1, y));
            const left = .27 - .18 * progress;
            const right = .73 + .18 * progress;
            const margin = visualHalfWidth / width + .012;
            const usableLeft = Math.min(.49, left + margin);
            const usableRight = Math.max(.51, right - margin);
            return usableLeft + (usableRight - usableLeft) * Math.max(0, Math.min(1, lane));
        }

        const setTarget = event => {
            const rect = canvas.getBoundingClientRect();
            state.targetX = Math.max(playerMinX, Math.min(playerMaxX, (event.clientX - rect.left) / rect.width));
        };
        state.pointerDown = event => {
            state.dragging = true;
            canvas.setPointerCapture?.(event.pointerId);
            setTarget(event);
            canvas.focus({ preventScroll: true });
            event.preventDefault();
        };
        state.pointerMove = event => {
            if (!state.dragging && event.pointerType !== "mouse") return;
            setTarget(event);
            event.preventDefault();
        };
        state.pointerUp = event => {
            state.dragging = false;
            canvas.releasePointerCapture?.(event.pointerId);
        };
        state.keyDown = event => {
            if (event.key !== "ArrowLeft" && event.key !== "ArrowRight") return;
            state.targetX = Math.max(playerMinX, Math.min(playerMaxX,
                state.targetX + (event.key === "ArrowLeft" ? -.09 : .09)));
            event.preventDefault();
        };
        canvas.addEventListener("pointerdown", state.pointerDown, { passive: false });
        canvas.addEventListener("pointermove", state.pointerMove, { passive: false });
        canvas.addEventListener("pointerup", state.pointerUp, { passive: true });
        canvas.addEventListener("pointercancel", state.pointerUp, { passive: true });
        window.addEventListener("keydown", state.keyDown, { passive: false });
        games.set(canvas, state);

        function spawnEnemy() {
            const difficulty = 1 + state.elapsed / 24;
            const hp = Math.max(2, Math.ceil(difficulty * (1.15 + Math.random() * .9)));
            const size = 54 + Math.min(38, difficulty * 2.8);
            const enemy = {
                lane: Math.random(),
                x: .5,
                y: .09,
                age: 0,
                hp,
                maxHp: hp,
                speed: 48 + state.elapsed + Math.random() * 20,
                size
            };
            enemy.x = roadLaneX(enemy.lane, enemy.y, enemy.size * .85);
            state.enemies.push(enemy);
        }

        function spawnPickup() {
            const roll = Math.random();
            const type = roll < .18 ? "weapon"
                : roll < .30 ? "ally"
                : roll < .65 ? "gold"
                : roll < .87 ? "material" : "ticket";
            const pickup = { lane: Math.random(), x: .5, y: .10, type, speed: 70 + state.elapsed * .38 };
            pickup.x = roadLaneX(pickup.lane, pickup.y, 48);
            state.pickups.push(pickup);
        }

        function shoot() {
            const spread = Math.min(5, state.allies);
            for (let i = 0; i < spread; i++) {
                const offset = (i - (spread - 1) / 2) * .034;
                state.bullets.push({ x: state.playerX + offset, y: .76 - Math.floor(i / 3) * .035, power: 1 + Math.floor(state.power / 4), type: (state.power + i) % Math.max(1, state.weapons.length), life: 0 });
            }
        }

        function finish() {
            if (state.finished || state.stopped) return;
            state.finished = true;
            state.stopped = true;
            cancelAnimationFrame(state.raf);
            dotnet.invokeMethodAsync("FinishAsync",
                Math.floor(state.elapsed), state.kills, state.goldPickups,
                state.materialPickups, state.ticketPickups).catch(() => {});
        }

        function update(dt) {
            state.elapsed += dt;
            state.playerX += (state.targetX - state.playerX) * Math.min(1, dt * 12);
            state.spawnClock -= dt;
            state.pickupClock -= dt;
            state.shotClock -= dt;
            if (state.spawnClock <= 0) {
                spawnEnemy();
                if (state.elapsed > 60 && Math.random() < Math.min(.48, state.elapsed / 300)) spawnEnemy();
                state.spawnClock = Math.max(.42, 1.0 - state.elapsed * .0046);
            }
            if (state.pickupClock <= 0) {
                spawnPickup();
                state.pickupClock = 4.4 + Math.random() * 2.4;
            }
            if (state.shotClock <= 0) {
                shoot();
                state.shotClock = Math.max(.10, .43 - state.power * .018);
            }

            for (const bullet of state.bullets) { bullet.y -= dt * (1.05 + state.power * .008); bullet.life += dt; }
            for (const enemy of state.enemies) {
                enemy.age += dt;
                enemy.y += dt * enemy.speed / height;
                enemy.x = roadLaneX(enemy.lane, enemy.y, enemy.size * .85);
            }
            for (const pickup of state.pickups) {
                pickup.y += dt * pickup.speed / height;
                pickup.x = roadLaneX(pickup.lane, pickup.y, 48);
            }
            state.bullets = state.bullets.filter(bullet => bullet.y > -.08);

            for (const bullet of state.bullets) {
                if (bullet.hit) continue;
                for (const enemy of state.enemies) {
                    if (enemy.dead || enemy.age < .28 || Math.abs(bullet.x - enemy.x) > .052 || Math.abs(bullet.y - enemy.y) > .055) continue;
                    bullet.hit = true;
                    enemy.hp -= bullet.power;
                    state.flashes.push({ x: enemy.x, y: enemy.y, life: .18 });
                    if (enemy.hp <= 0) {
                        enemy.dead = true;
                        state.kills++;
                        if (state.kills >= state.shieldRecoveryAt) {
                            state.shield = Math.min(state.maxShield, state.shield + 1);
                            state.shieldRecoveryAt += 35;
                            state.flashes.push({ x: state.playerX, y: .82, life: .55, reward: true });
                        }
                    }
                    break;
                }
            }
            state.bullets = state.bullets.filter(bullet => !bullet.hit);
            state.enemies = state.enemies.filter(enemy => {
                if (enemy.dead) return false;
                if (enemy.y < .84) return true;
                state.shield--;
                state.flashes.push({ x: state.playerX, y: .82, life: .45, danger: true });
                if (state.shield <= 0) finish();
                return false;
            });

            state.pickups = state.pickups.filter(item => {
                if (item.y > 1.04) return false;
                if (item.y < .76 || Math.abs(item.x - state.playerX) > .11) return true;
                if (item.type === "weapon") state.power++;
                else if (item.type === "ally") state.allies = Math.min(8, state.allies + 1);
                else if (item.type === "gold") state.goldPickups++;
                else if (item.type === "material") state.materialPickups++;
                else state.ticketPickups++;
                state.flashes.push({ x: item.x, y: item.y, life: .38, reward: true });
                return false;
            });
            for (const flash of state.flashes) flash.life -= dt;
            state.flashes = state.flashes.filter(flash => flash.life > 0);
        }

        function drawRoad() {
            const { ctx, width: w, height: h } = state;
            if (!cover(ctx, state.background, w, h)) {
                const bg = ctx.createLinearGradient(0, 0, 0, h);
                bg.addColorStop(0, "#173943"); bg.addColorStop(1, "#071217");
                ctx.fillStyle = bg; ctx.fillRect(0, 0, w, h);
            }
            ctx.fillStyle = "rgba(3,10,14,.16)"; ctx.fillRect(0, 0, w, h);
            const focus = ctx.createRadialGradient(state.playerX * w, h * .78, 10, state.playerX * w, h * .78, w * .34);
            focus.addColorStop(0, "rgba(76,235,214,.12)"); focus.addColorStop(1, "rgba(76,235,214,0)");
            ctx.fillStyle = focus; ctx.fillRect(0, h * .45, w, h * .55);
            ctx.strokeStyle = "rgba(182,255,240,.34)"; ctx.lineWidth = 2;
            for (let i = 0; i < 13; i++) {
                const y = ((i * 97 + state.elapsed * 240) % (h + 120)) - 60;
                const x = (i * 163) % w;
                ctx.globalAlpha = .1 + .3 * Math.max(0, y / h);
                ctx.beginPath(); ctx.moveTo(x, y); ctx.lineTo(x + (x - w / 2) * .05, y + 38); ctx.stroke();
            }
            ctx.globalAlpha = 1;
        }

        function drawImageOrOrb(image, x, y, size, fallback) {
            const { ctx, width: w, height: h } = state;
            const px = x * w, py = y * h;
            if (image?.complete && image.naturalWidth) {
                ctx.drawImage(image, px - size / 2, py - size / 2, size, size);
                return;
            }
            ctx.fillStyle = fallback; ctx.beginPath(); ctx.arc(px, py, size * .35, 0, Math.PI * 2); ctx.fill();
        }

        function draw() {
            const { ctx, width: w, height: h } = state;
            drawRoad();
            for (const pickup of state.pickups) {
                const x = pickup.x * w, y = pickup.y * h;
                const isReward = pickup.type === "gold" || pickup.type === "material" || pickup.type === "ticket";
                const image = pickup.type === "weapon"
                    ? state.weapons[state.power % Math.max(1, state.weapons.length)]
                    : pickup.type === "ally" ? state.players[state.allies % Math.max(1, state.players.length)]
                    : isReward ? state.rewardImages[pickup.type] : null;
                ctx.save();
                ctx.shadowColor = pickup.type === "ticket" ? "#ffd966" : "#5ff4e3"; ctx.shadowBlur = 18;
                ctx.beginPath(); ctx.arc(x, y - 7, 38, 0, Math.PI * 2);
                ctx.fillStyle = "rgba(5,24,29,.82)"; ctx.fill();
                ctx.strokeStyle = pickup.type === "ticket" ? "#f1c34f" : "#62d9cf"; ctx.lineWidth = 3; ctx.stroke();
                if (image?.complete && image.naturalWidth) ctx.drawImage(image, x - 31, y - 38, 62, 62);
                else {
                    ctx.fillStyle = "#ffe28a"; ctx.font = "900 16px system-ui"; ctx.textAlign = "center"; ctx.textBaseline = "middle";
                    ctx.fillText(pickup.type === "ally" ? "동료" : pickup.type === "ticket" ? "소환" : "무기", x, y - 7);
                }
                roundedRect(ctx, x - 37, y + 27, 74, 22, 10); ctx.fillStyle = "rgba(2,17,22,.92)"; ctx.fill();
                ctx.fillStyle = "#eafff9"; ctx.font = "800 11px system-ui"; ctx.textAlign = "center"; ctx.textBaseline = "middle"; ctx.fillText(
                    pickup.type === "weapon" ? "화력 강화" : pickup.type === "ally" ? "동료 합류" : pickup.type === "gold" ? "금화" : pickup.type === "material" ? "강화 광석" : "소환패", x, y + 38);
                ctx.restore();
            }
            for (const bullet of state.bullets) {
                const x = bullet.x * w, y = bullet.y * h;
                ctx.save(); ctx.translate(x, y); ctx.shadowColor = bullet.type === 1 ? "#6ff6e3" : "#ffe373"; ctx.shadowBlur = 18;
                const trail = ctx.createLinearGradient(0, 0, 0, 45); trail.addColorStop(0, bullet.type === 1 ? "#bafff5" : "#fff1a4"); trail.addColorStop(1, "rgba(255,230,115,0)");
                ctx.strokeStyle = trail; ctx.lineWidth = 7; ctx.beginPath(); ctx.moveTo(0, 26); ctx.lineTo(0, -24); ctx.stroke();
                const weapon = state.weapons[bullet.type];
                if (weapon?.complete && weapon.naturalWidth) ctx.drawImage(weapon, -19, -28, 38, 38);
                else { ctx.fillStyle = "#fff0a0"; ctx.beginPath(); ctx.ellipse(0, -8, 5, 18, 0, 0, Math.PI * 2); ctx.fill(); }
                ctx.restore();
            }
            ctx.shadowBlur = 0;
            for (const enemy of state.enemies) {
                const scale = .72 + enemy.y * .62;
                ctx.save(); ctx.globalAlpha = Math.min(1, enemy.age * 3.5);
                ctx.fillStyle = "rgba(255,79,59,.16)"; ctx.beginPath(); ctx.ellipse(enemy.x * w, enemy.y * h + enemy.size * .32, enemy.size * .68, enemy.size * .22, 0, 0, Math.PI * 2); ctx.fill();
                drawImageOrOrb(state.enemyImage, enemy.x, enemy.y, enemy.size * 1.8 * scale, "#a7473d");
                const barW = enemy.size * 1.12 * scale, x = enemy.x * w - barW / 2, y = enemy.y * h - enemy.size * .62 * scale;
                ctx.fillStyle = "#1b0809"; ctx.fillRect(x, y, barW, 7);
                ctx.fillStyle = "#ff6955"; ctx.fillRect(x, y, barW * Math.max(0, enemy.hp / enemy.maxHp), 7);
                ctx.restore();
            }
            const formation = Math.min(state.allies, 8);
            for (let i = formation - 1; i >= 0; i--) {
                const row = Math.floor(i / 3), rowCount = Math.min(3, formation - row * 3), column = i % 3 - (rowCount - 1) / 2;
                const x = state.playerX + column * .115, y = .80 + row * .076;
                ctx.fillStyle = "rgba(72,225,211,.18)"; ctx.beginPath(); ctx.ellipse(x * w, (y + .032) * h, 44, 17, 0, 0, Math.PI * 2); ctx.fill();
                ctx.strokeStyle = "#61e4d7"; ctx.lineWidth = 3; ctx.stroke();
                drawImageOrOrb(state.players[i % Math.max(1, state.players.length)], x, y, 104, "#e0c277");
            }
            for (const flash of state.flashes) {
                ctx.globalAlpha = Math.min(1, flash.life * 4);
                ctx.fillStyle = flash.danger ? "#ff5c4d" : flash.reward ? "#66f4de" : "#fff0a2";
                ctx.beginPath(); ctx.arc(flash.x * w, flash.y * h, 18 + (1 - flash.life) * 45, 0, Math.PI * 2); ctx.fill();
                ctx.globalAlpha = 1;
            }
            ctx.fillStyle = "rgba(4,13,17,.88)"; roundedRect(ctx, 18, 18, 220, 58, 14); ctx.fill();
            ctx.fillStyle = "#ffe38b"; ctx.font = "900 17px system-ui"; ctx.textAlign = "left"; ctx.fillText("원정 방벽", 34, 43);
            for (let i = 0; i < state.maxShield; i++) {
                roundedRect(ctx, 122 + i * 24, 29, 18, 28, 5);
                ctx.fillStyle = i < state.shield ? "#62ead7" : "rgba(118,139,137,.22)"; ctx.fill();
                ctx.strokeStyle = i < state.shield ? "#dfffee" : "rgba(159,176,171,.25)"; ctx.lineWidth = 1.5; ctx.stroke();
            }
        }

        function updateHud() {
            if (!state.hud) return;
            const seconds = Math.floor(state.elapsed);
            const time = state.hud.querySelector("[data-trial-time]");
            const kills = state.hud.querySelector("[data-trial-kills]");
            const power = state.hud.querySelector("[data-trial-power]");
            const allies = state.hud.querySelector("[data-trial-allies]");
            if (time) time.textContent = `${Math.floor(seconds / 60)}:${String(seconds % 60).padStart(2, "0")}`;
            if (kills) kills.textContent = state.kills.toLocaleString();
            if (power) power.textContent = `Lv.${state.power}`;
            if (allies) allies.textContent = `${state.allies}명`;
        }

        function frame(now) {
            if (state.stopped) return;
            const dt = Math.min(.05, Math.max(0, (now - state.lastFrame) / 1000));
            state.lastFrame = now;
            update(dt); draw(); updateHud();
            if (!state.stopped) state.raf = requestAnimationFrame(frame);
        }
        canvas.focus({ preventScroll: true });
        state.raf = requestAnimationFrame(frame);
    }

    window.maruEndless = { start, stop };
})();
