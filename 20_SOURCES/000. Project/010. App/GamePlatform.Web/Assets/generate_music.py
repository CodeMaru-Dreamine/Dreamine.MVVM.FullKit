"""Generate deterministic East-Asian fantasy BGM loops as browser-ready MP3 files."""

from __future__ import annotations

import json
import math
import pathlib
import random
import sys

import lameenc
import numpy as np


ROOT = pathlib.Path(__file__).resolve().parents[1]
CATALOG = ROOT / "Content" / "audio-tracks.json"
OUTPUT = ROOT / "wwwroot" / "audio" / "maru-idle"
SAMPLE_RATE = 44_100


PENTATONIC = (0, 2, 5, 7, 9, 12)
BOSS_KEYS = {
    "bgm-marsh-crown", "bgm-silent-bell", "bgm-red-horn", "bgm-white-gate",
    "bgm-tide-judge", "bgm-glass-sun", "bgm-blue-wake-boss", "bgm-sky-seal",
    "bgm-rift-heart", "bgm-eclipse-throne",
}


def note(root: float, semitones: int) -> float:
    return root * (2.0 ** (semitones / 12.0))


def render(track: dict[str, object]) -> np.ndarray:
    duration = float(track["loopEnd"])
    root = float(track["rootHz"])
    rng = random.Random(int(track["seed"]))
    count = int(duration * SAMPLE_RATE)
    t = np.arange(count, dtype=np.float64) / SAMPLE_RATE
    boss = str(track["assetKey"]) in BOSS_KEYS

    # A stable root/fifth drone keeps the harmony calm instead of stacking dissonant sines.
    drone_root = root * .5
    slow_breath = .72 + np.sin(t * math.tau / duration - math.pi / 2) * .18
    drone = (np.sin(t * drone_root * math.tau) * .11
             + np.sin(t * drone_root * 1.5 * math.tau + .35) * .065
             + np.sin(t * drone_root * 2.0 * math.tau + 1.2) * .026) * slow_breath

    # Deterministic two-second plucked-string phrases using a pentatonic scale.
    phrase = np.zeros(count, dtype=np.float64)
    steps = int(duration / 2)
    sequence = [rng.choice(PENTATONIC) for _ in range(steps)]
    sequence[-1] = 0
    for step, semitones in enumerate(sequence):
        start = int(step * 2 * SAMPLE_RATE)
        end = min(count, start + int(1.8 * SAMPLE_RATE))
        local_t = np.arange(end - start, dtype=np.float64) / SAMPLE_RATE
        frequency = note(root, semitones)
        attack = np.minimum(1.0, local_t / .018)
        decay = np.exp(-local_t * (2.0 if boss else 1.65))
        pluck = (np.sin(local_t * frequency * math.tau)
                 + np.sin(local_t * frequency * 2.0 * math.tau + .25) * .38
                 + np.sin(local_t * frequency * 3.0 * math.tau + .7) * .13)
        phrase[start:end] += pluck * attack * decay * (.17 if boss else .13)

    # A restrained flute line answers every other pluck.
    flute = np.zeros(count, dtype=np.float64)
    for step in range(1, steps, 2):
        start = int((step * 2 + .35) * SAMPLE_RATE)
        end = min(count, start + int(1.15 * SAMPLE_RATE))
        if start >= count:
            continue
        local_t = np.arange(end - start, dtype=np.float64) / SAMPLE_RATE
        frequency = note(root * 2, sequence[step])
        breath = np.minimum(1.0, local_t / .12) * np.exp(-local_t * 1.55)
        vibrato = np.sin(local_t * 5.1 * math.tau) * .005
        flute[start:end] += np.sin(local_t * frequency * (1 + vibrato) * math.tau) * breath * .055

    # Sparse low percussion gives boss tracks more motion without turning into harsh noise.
    percussion = np.zeros(count, dtype=np.float64)
    beat_seconds = 1.0 if boss else 2.0
    for beat in np.arange(0, duration, beat_seconds):
        start = int(beat * SAMPLE_RATE)
        end = min(count, start + int(.28 * SAMPLE_RATE))
        local_t = np.arange(end - start, dtype=np.float64) / SAMPLE_RATE
        frequency = 72 if boss else 58
        percussion[start:end] += np.sin(local_t * frequency * math.tau) * np.exp(-local_t * 18) * (.16 if boss else .065)

    mono = drone + phrase + flute + percussion
    mono *= float(track["volumeNormalization"])
    mono = np.tanh(mono * 1.3)
    fade_samples = int(.12 * SAMPLE_RATE)
    fade = np.sin(np.linspace(0, math.pi / 2, fade_samples)) ** 2
    mono[:fade_samples] *= fade
    mono[-fade_samples:] *= fade[::-1]
    peak = max(.001, float(np.max(np.abs(mono))))
    mono *= .76 / peak
    left = mono * (0.97 + np.sin(t * .31) * .03)
    right = mono * (0.97 + np.sin(t * .29 + 1.1) * .03)
    stereo = np.stack((left, right), axis=1)
    return np.clip(stereo * 32767, -32768, 32767).astype("<i2")


def encode(track: dict[str, object]) -> pathlib.Path:
    encoder = lameenc.Encoder()
    encoder.set_bit_rate(96)
    encoder.set_in_sample_rate(SAMPLE_RATE)
    encoder.set_channels(2)
    encoder.set_quality(2)
    pcm = render(track).tobytes()
    encoded = encoder.encode(pcm) + encoder.flush()
    target = OUTPUT / f"{track['assetKey']}.mp3"
    target.write_bytes(encoded)
    return target


def main() -> int:
    OUTPUT.mkdir(parents=True, exist_ok=True)
    tracks = json.loads(CATALOG.read_text(encoding="utf-8"))
    for track in tracks:
        path = encode(track)
        print(f"{path.name}: {path.stat().st_size} bytes")
    return 0


if __name__ == "__main__":
    sys.exit(main())
