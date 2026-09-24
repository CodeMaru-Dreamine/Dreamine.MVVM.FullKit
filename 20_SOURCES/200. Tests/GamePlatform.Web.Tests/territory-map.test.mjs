import { test } from 'node:test';
import assert from 'node:assert/strict';
import { WORLD_SIZE, HOME, bounds, centerAt, zoomAt, journeyProgress, routePoint } from '../../000. Project/010. App/GamePlatform.Web/wwwroot/territory-map-math.mjs';

test('camera clamps both axes without exposing empty edges at zoomed scale', () => {
    assert.deepEqual(bounds({ x: 900, y: -10000, scale: 1 }, 360, 500), { x: 0, y: -6500, scale: 1 });
    assert.deepEqual(bounds({ x: 1, y: 1, scale: .04 }, 360, 500), { x: 40, y: 110, scale: .04 });
});
test('home centers at any viewport size and pointer zoom retains focal point', () => {
    const camera = centerAt({ scale: .8 }, HOME, HOME, 360, 400);
    assert.equal((180 - camera.x) / camera.scale, HOME);
    const zoomed = zoomAt(camera, 1.2, 123, 145, 360, 400);
    assert.ok(Math.abs((123 - camera.x) / camera.scale - (123 - zoomed.x) / zoomed.scale) < .00001);
    assert.ok(Math.abs((145 - camera.y) / camera.scale - (145 - zoomed.y) / zoomed.scale) < .00001);
});
test('zoom limits and extreme drag remain finite', () => {
    const camera = centerAt({ scale: .8 }, HOME, HOME, 360, 500);
    assert.equal(zoomAt(camera, 10000, 180, 250, 360, 500).scale, 1.8);
    assert.equal(zoomAt(camera, .00001, 180, 250, 360, 500).scale, 360 / WORLD_SIZE);
});
test('round trip moves out, pauses at target, returns home', () => {
    assert.equal(routePoint(0, 140, 980).x, HOME);
    assert.equal(routePoint(.45, 140, 980).x, 140);
    assert.equal(routePoint(.5, 140, 980).stopped, true);
    assert.equal(routePoint(.6, 140, 980).returning, true);
    assert.equal(routePoint(1, 140, 980).x, HOME);
    assert.equal(routePoint(1, 140, 980).complete, true);
});
test('boosted journey is continuous and survives serialized timing', () => {
    const before = journeyProgress(0, 90000, 0, 30000);
    assert.equal(before, journeyProgress(30000, 60000, before, 30000));
    assert.ok(Math.abs(journeyProgress(30000, 60000, before, 45000) - 2/3) < .00001);
    assert.equal(journeyProgress(30000, 60000, before, 60000), 1);
    assert.equal(journeyProgress(30000, 60000, before, 10000), before);
});
