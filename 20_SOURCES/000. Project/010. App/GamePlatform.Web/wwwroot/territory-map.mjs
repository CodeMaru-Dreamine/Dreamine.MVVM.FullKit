import { WORLD_SIZE, HOME, bounds, zoomAt, centerAt, journeyProgress, routePoint } from './territory-map-math.mjs?v=5';

// All camera motion stays on the client. The server owns discovered tiles, missions,
// costs, power and completion times; this module can only move presentation elements.
export function mount(root, serverNow) {
    const homeX = Number(root.dataset.homeX) || HOME, homeY = Number(root.dataset.homeY) || HOME;
    const preferredZoom = Number(root.dataset.defaultZoom) || .82;
    const defaultZoom = Math.min(root.clientWidth <= 600 ? .68 : 1.8, Math.max(.3, preferredZoom));
    const viewport = root.querySelector('[data-camera]'), plane = root.querySelector('[data-plane]');
    const zoomLabel = root.querySelector('[data-zoom]'), coordinates = root.querySelector('[data-camera-coordinates]');
    const minimap = root.querySelector('[data-minimap-camera]');
    const abort = new AbortController(), pointers = new Map();
    let camera = { x: 0, y: 0, scale: defaultZoom }, width = 0, height = 0;
    let disposed = false, initialized = false, drag = null, pinch = null, suppressUntil = 0;
    let clockOffset = serverNow - Date.now(), frameId = 0, lastPaint = 0, marches = [], follow = false, followIndex = -1;
    let marchPoint = { x: HOME, y: HOME };
    let lastFocus = null;
    const on = (target, name, handler, opts = {}) => target.addEventListener(name, handler, { ...opts, signal: abort.signal });
    const pointerPoint = event => { const r = viewport.getBoundingClientRect(); return { x: event.clientX - r.left, y: event.clientY - r.top }; };
    function drawCamera() {
        camera = bounds(camera, width, height);
        root.dataset.overview = camera.scale < .18 ? 'true' : 'false';
        plane.style.setProperty('--overview-scale', 1 / camera.scale);
        plane.style.setProperty('--region-width', `${Math.min(64, WORLD_SIZE * camera.scale / 5 - 3)}px`);
        plane.style.transform = `translate3d(${camera.x}px,${camera.y}px,0) scale(${camera.scale})`;
        zoomLabel.textContent = `${Math.round(camera.scale * 100)}%`;
        const x = Math.max(0, -camera.x / camera.scale), y = Math.max(0, -camera.y / camera.scale);
        minimap.style.cssText = `left:${x / WORLD_SIZE * 100}%;top:${y / WORLD_SIZE * 100}%;width:${Math.min(WORLD_SIZE, width / camera.scale) / WORLD_SIZE * 100}%;height:${Math.min(WORLD_SIZE, height / camera.scale) / WORLD_SIZE * 100}%`;
        const cx = (width / 2 - camera.x) / camera.scale, cy = (height / 2 - camera.y) / camera.scale;
        coordinates.textContent = `${Math.min(25, Math.max(1, Math.floor(cy / 280) + 1))} : ${Math.min(25, Math.max(1, Math.floor(cx / 280) + 1))}`;
    }
    function resize() {
        const cx = initialized ? (width / 2 - camera.x) / camera.scale : homeX;
        const cy = initialized ? (height / 2 - camera.y) / camera.scale : homeY;
        width = viewport.clientWidth; height = viewport.clientHeight;
        if (!width || !height) return;
        camera.scale = Math.max(Math.min(width, height) / WORLD_SIZE, camera.scale);
        camera = centerAt(camera, cx, cy, width, height); initialized = true; drawCamera();
    }
    function startPinch() {
        const [a, b] = [...pointers.values()];
        pinch = { distance: Math.max(1, Math.hypot(a.x - b.x, a.y - b.y)), x: (a.x + b.x) / 2, y: (a.y + b.y) / 2, camera: { ...camera } };
        drag = null; follow = false; suppressUntil = performance.now() + 600;
    }
    on(viewport, 'pointerdown', e => {
        if (e.pointerType === 'mouse' && e.button !== 0) return;
        const p = pointerPoint(e); pointers.set(e.pointerId, p);
        follow = false;
        if (!e.target.closest('button')) root.focus({ preventScroll: true });
        if (pointers.size === 1) drag = { id: e.pointerId, x: p.x, y: p.y, camera: { ...camera }, active: false };
        else if (pointers.size === 2) startPinch();
    });
    on(viewport, 'pointermove', e => {
        if (!pointers.has(e.pointerId)) return;
        const p = pointerPoint(e); pointers.set(e.pointerId, p);
        if (pointers.size >= 2 && pinch) {
            const [a, b] = [...pointers.values()], cx = (a.x + b.x) / 2, cy = (a.y + b.y) / 2;
            camera = zoomAt(pinch.camera, Math.hypot(a.x - b.x, a.y - b.y) / pinch.distance, pinch.x, pinch.y, width, height);
            camera.x += cx - pinch.x; camera.y += cy - pinch.y;
            suppressUntil = performance.now() + 600; e.preventDefault(); drawCamera();
        } else if (drag?.id === e.pointerId) {
            const dx = p.x - drag.x, dy = p.y - drag.y;
            if (!drag.active && Math.hypot(dx, dy) < 6) return;
            drag.active = true; viewport.setPointerCapture(e.pointerId); viewport.classList.add('dragging');
            camera = { ...drag.camera, x: drag.camera.x + dx, y: drag.camera.y + dy };
            suppressUntil = performance.now() + 600; e.preventDefault(); drawCamera();
        }
    }, { passive: false });
    function endPointer(e) {
        const wasGesture = drag?.active || pinch;
        pointers.delete(e.pointerId);
        if (viewport.hasPointerCapture(e.pointerId)) viewport.releasePointerCapture(e.pointerId);
        if (wasGesture) suppressUntil = performance.now() + 600;
        drag = null; pinch = null; viewport.classList.remove('dragging');
        if (pointers.size === 1) {
            const [id, p] = [...pointers.entries()][0]; drag = { id, x: p.x, y: p.y, camera: { ...camera }, active: false };
        }
    }
    on(viewport, 'pointerup', endPointer); on(viewport, 'pointercancel', endPointer);
    on(viewport, 'lostpointercapture', e => { if (pointers.has(e.pointerId)) endPointer(e); });
    on(viewport, 'click', e => { if (performance.now() < suppressUntil) { e.preventDefault(); e.stopImmediatePropagation(); } }, { capture: true });
    on(viewport, 'wheel', e => { e.preventDefault(); const p = pointerPoint(e); follow = false; camera = zoomAt(camera, Math.exp(-Math.sign(e.deltaY) * .12), p.x, p.y, width, height); drawCamera(); }, { passive: false });
    function command(name, e) {
        follow = name === 'army';
        switch (name) {
            case 'zoom-in': camera = zoomAt(camera, 1.2, width / 2, height / 2, width, height); break;
            case 'zoom-out': camera = zoomAt(camera, 1 / 1.2, width / 2, height / 2, width, height); break;
            case 'overview': camera.scale = Math.min(width, Math.max(150, height - 90)) / WORLD_SIZE; camera = centerAt(camera, HOME, HOME, width, height - 10); break;
            case 'home': camera.scale = defaultZoom; camera = centerAt(camera, homeX, homeY, width, height); break;
            case 'army':
                followIndex = (followIndex + 1) % Math.max(1, marches.length);
                if (marches[followIndex]) { const d = marches[followIndex].dataset; marchPoint = routePoint(journeyProgress(Number(d.start), Number(d.end), Number(d.progress), Date.now() + clockOffset), Number(d.x), Number(d.y)); }
                camera = centerAt(camera, marchPoint.x, marchPoint.y, width, height); break;
            case 'region': {
                const region = e.target.closest('[data-map-command="region"]');
                camera.scale = defaultZoom;
                camera = centerAt(camera, Number(region.dataset.x), Number(region.dataset.y), width, height);
                break;
            }
            case 'up': camera.y += height * .35; break;
            case 'down': camera.y -= height * .35; break;
            case 'left': camera.x += width * .35; break;
            case 'right': camera.x -= width * .35; break;
            case 'minimap': {
                const r = root.querySelector('.mini-map').getBoundingClientRect();
                // A keyboard-activated minimap button returns to home.
                const x = e.detail ? (e.clientX - r.left) / r.width * WORLD_SIZE : HOME;
                const y = e.detail ? (e.clientY - r.top) / r.height * WORLD_SIZE : HOME;
                camera = centerAt(camera, x, y, width, height); break;
            }
        }
        drawCamera();
    }
    on(root, 'click', e => { const button = e.target.closest('[data-map-command]'); if (button) command(button.dataset.mapCommand, e); });
    on(root, 'keydown', e => {
        if (['INPUT', 'SELECT', 'TEXTAREA'].includes(e.target.tagName)) return;
        const key = { ArrowUp: 'up', ArrowDown: 'down', ArrowLeft: 'left', ArrowRight: 'right', Home: 'home', '+': 'zoom-in', '=': 'zoom-in', '-': 'zoom-out' }[e.key];
        if (key) { e.preventDefault(); e.stopPropagation(); command(key, e); }
    });
    function animate(timestamp) {
        if (disposed) return;
        if (!root.isConnected) { dispose(); return; }
        frameId = requestAnimationFrame(animate);
        if (document.hidden || timestamp - lastPaint < 32) return;
        lastPaint = timestamp;
        marches = [...root.querySelectorAll('[data-march]')];
        for (const [index, march] of marches.entries()) {
        const d = march.dataset;
        const progress = journeyProgress(Number(d.start), Number(d.end), Number(d.progress), Date.now() + clockOffset);
        marchPoint = routePoint(progress, Number(d.x), Number(d.y));
        march.style.left = `${marchPoint.x}px`; march.style.top = `${marchPoint.y}px`;
        const direction = (Number(d.x) >= HOME ? 1 : -1) * (marchPoint.returning ? -1 : 1);
        march.style.setProperty('--facing', direction);
        march.classList.toggle('arrived', marchPoint.stopped || marchPoint.complete);
        const phase = marchPoint.complete ? '귀환 확인' : marchPoint.returning ? '귀환' : marchPoint.stopped ? (d.mission === 'conquer' ? '교전' : d.mission === 'gather' ? '채집' : '정찰') : '행군';
        const label = march.querySelector('[data-march-phase]'); if (label.textContent !== phase) label.textContent = phase;
        if (follow && index === followIndex) { camera = centerAt(camera, marchPoint.x, marchPoint.y, width, height); drawCamera(); }
        }
    }
    function sync(time) {
        clockOffset = time - Date.now(); marches = [...root.querySelectorAll('[data-march]')];
        const focus = root.dataset.focusTarget;
        if (focus && focus !== lastFocus) {
            const x = Number(root.dataset.focusX), y = Number(root.dataset.focusY);
            if (Number.isFinite(x) && Number.isFinite(y)) { camera.scale = defaultZoom; camera = centerAt(camera,x,y,width,height); follow = false; drawCamera(); }
        }
        lastFocus = focus;
    }
    function dispose() { if (disposed) return; disposed = true; abort.abort(); observer.disconnect(); removalObserver.disconnect(); cancelAnimationFrame(frameId); pointers.clear(); }
    const observer = new ResizeObserver(resize); observer.observe(viewport);
    // Dispose client listeners even when a disconnected Blazor circuit cannot call JS.
    const removalObserver = new MutationObserver(() => { if (!root.isConnected) dispose(); });
    removalObserver.observe(root.parentNode, { childList: true });
    resize(); sync(serverNow); frameId = requestAnimationFrame(animate);
    return { sync, dispose };
}
