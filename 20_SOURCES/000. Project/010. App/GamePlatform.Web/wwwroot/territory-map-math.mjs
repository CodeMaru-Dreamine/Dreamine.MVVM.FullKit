export const WORLD_SIZE = 7000;
export const HOME = WORLD_SIZE / 2;
export const clamp = (n, min, max) => Math.max(min, Math.min(max, n));
export function bounds(camera, width, height) {
    const size = WORLD_SIZE * camera.scale;
    return { ...camera, x: size <= width ? (width - size) / 2 : clamp(camera.x, width - size, 0),
        y: size <= height ? (height - size) / 2 : clamp(camera.y, height - size, 0) };
}
export function zoomAt(camera, factor, px, py, width, height) {
    const minimum = Math.min(width, height) / WORLD_SIZE;
    const scale = clamp(camera.scale * factor, minimum, 1.8);
    const wx = (px - camera.x) / camera.scale, wy = (py - camera.y) / camera.scale;
    return bounds({ x: px - wx * scale, y: py - wy * scale, scale }, width, height);
}
export function centerAt(camera, x, y, width, height) {
    return bounds({ ...camera, x: width / 2 - x * camera.scale, y: height / 2 - y * camera.scale }, width, height);
}
export function journeyProgress(start, end, base, now) {
    base = clamp(Number.isFinite(base) ? base : 0, 0, 1);
    return base + (1 - base) * clamp((now - start) / Math.max(1, end - start), 0, 1);
}
export function routePoint(progress, x, y) {
    const p = clamp(progress, 0, 1);
    const leg = p < .45 ? p / .45 : p <= .55 ? 1 : (1 - p) / .45;
    return { x: HOME + (x - HOME) * leg, y: HOME + (y - HOME) * leg,
        stopped: p >= .45 && p <= .55, returning: p > .55, complete: p >= 1 };
}
