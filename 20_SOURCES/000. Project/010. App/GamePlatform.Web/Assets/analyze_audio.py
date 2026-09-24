"""Analyze project-owned source audio without modifying the originals.

The script uses a portable FFmpeg/FFprobe pair and NumPy only.  It writes
machine-readable metrics plus waveform/spectrogram PNGs that can be reviewed
before any source interval is promoted to a runtime game asset.
"""

from __future__ import annotations

import argparse
import csv
import json
import math
import subprocess
from pathlib import Path

import numpy as np


def run(command: list[str], *, capture: bool = False) -> subprocess.CompletedProcess[bytes]:
    """Run an FFmpeg-family command and fail with its diagnostic intact."""
    return subprocess.run(command, check=True, capture_output=capture)


def db(value: float) -> float:
    """Convert a linear full-scale amplitude to dBFS."""
    return 20.0 * math.log10(max(value, 1e-12))


def intervals(mask: np.ndarray, frame_seconds: float, minimum_seconds: float) -> list[dict[str, float]]:
    """Return contiguous true runs from a frame mask."""
    result: list[dict[str, float]] = []
    start: int | None = None
    for index, active in enumerate(np.append(mask, False)):
        if active and start is None:
            start = index
        elif not active and start is not None:
            duration = (index - start) * frame_seconds
            if duration >= minimum_seconds:
                result.append({"start": round(start * frame_seconds, 3), "end": round(index * frame_seconds, 3), "duration": round(duration, 3)})
            start = None
    return result


def analyze(ffmpeg: Path, ffprobe: Path, source: Path, output: Path) -> dict[str, object]:
    """Decode one MP3 and return objective level, silence and transient metrics."""
    probe = run([
        str(ffprobe), "-v", "error", "-show_entries",
        "format=duration,bit_rate:stream=codec_name,sample_rate,channels,channel_layout,bit_rate",
        "-of", "json", str(source),
    ], capture=True)
    metadata = json.loads(probe.stdout.decode("utf-8"))
    stream = metadata["streams"][0]
    duration = float(metadata["format"]["duration"])

    decoded = run([
        str(ffmpeg), "-v", "error", "-i", str(source), "-f", "f32le", "-acodec", "pcm_f32le",
        "-ar", "48000", "-ac", "2", "pipe:1",
    ], capture=True).stdout
    stereo = np.frombuffer(decoded, dtype="<f4").reshape(-1, 2)
    mono = stereo.mean(axis=1).astype(np.float64)
    absolute = np.abs(stereo.astype(np.float64))
    peak = float(absolute.max(initial=0.0))
    rms = float(np.sqrt(np.mean(np.square(stereo.astype(np.float64)))))
    clipping_samples = int(np.count_nonzero(absolute >= 0.999))

    frame_size = 2400  # 50 ms at 48 kHz
    usable = mono[: len(mono) // frame_size * frame_size]
    frames = usable.reshape(-1, frame_size)
    frame_rms = np.sqrt(np.mean(np.square(frames), axis=1) + 1e-15)
    frame_db = 20 * np.log10(frame_rms + 1e-12)
    silent = intervals(frame_db <= -50.0, frame_size / 48000.0, 0.25)

    # Energy onset + spectral-flux candidates, merged to avoid reporting the
    # same physical hit many times.
    energy_delta = np.maximum(0.0, np.diff(frame_db, prepend=frame_db[0]))
    fft_frames = frames[:, ::2] * np.hanning(frame_size // 2)
    spectra = np.abs(np.fft.rfft(fft_frames, axis=1))
    spectra /= np.maximum(spectra.sum(axis=1, keepdims=True), 1e-12)
    flux = np.sqrt(np.sum(np.square(np.maximum(0.0, np.diff(spectra, axis=0, prepend=spectra[:1]))), axis=1))
    score = (energy_delta - np.median(energy_delta)) / (np.std(energy_delta) + 1e-9)
    score += (flux - np.median(flux)) / (np.std(flux) + 1e-9)
    candidates = np.flatnonzero((score > 5.0) & (frame_db > -45.0))
    transient_seconds: list[float] = []
    for candidate in candidates:
        timestamp = candidate * frame_size / 48000.0
        if not transient_seconds or timestamp - transient_seconds[-1] >= 0.18:
            transient_seconds.append(timestamp)
    ranked = sorted(transient_seconds, key=lambda t: score[min(len(score) - 1, round(t * 48000 / frame_size))], reverse=True)[:24]
    ranked.sort()

    # Suggest a musically useful long interval whose boundary frames have
    # similar energy and spectral centroid.  A crossfade is still required.
    centroids = (spectra * np.arange(spectra.shape[1])).sum(axis=1)
    loop_length = min(90.0, max(30.0, duration - 20.0))
    candidate_starts = np.arange(5.0, max(5.01, duration - loop_length - 3.0), 0.5)
    best_start = 0.0
    best_score = float("inf")
    for start_seconds in candidate_starts:
        end_seconds = start_seconds + loop_length
        start_index = min(len(frame_db) - 1, int(start_seconds / (frame_size / 48000.0)))
        end_index = min(len(frame_db) - 1, int(end_seconds / (frame_size / 48000.0)))
        boundary_score = abs(frame_db[start_index] - frame_db[end_index])
        boundary_score += abs(centroids[start_index] - centroids[end_index]) * 0.02
        boundary_score += max(0.0, -42.0 - frame_db[start_index])
        if boundary_score < best_score:
            best_start, best_score = float(start_seconds), float(boundary_score)

    stem = source.stem.lower().replace(" ", "-").replace("(", "").replace(")", "")
    waveform = output / f"{stem}-waveform.png"
    spectrum = output / f"{stem}-spectrogram.png"
    run([str(ffmpeg), "-y", "-v", "error", "-i", str(source), "-filter_complex", "showwavespic=s=1600x360:colors=0x63f5df", "-frames:v", "1", str(waveform)])
    run([str(ffmpeg), "-y", "-v", "error", "-i", str(source), "-lavfi", "showspectrumpic=s=1600x720:legend=1:color=viridis:scale=log", "-frames:v", "1", str(spectrum)])

    return {
        "file": source.name,
        "durationSeconds": round(duration, 3),
        "codec": stream.get("codec_name"),
        "bitRateKbps": round(int(stream.get("bit_rate") or metadata["format"].get("bit_rate") or 0) / 1000),
        "sampleRate": int(stream["sample_rate"]),
        "channels": int(stream["channels"]),
        "channelLayout": stream.get("channel_layout"),
        "samplePeakDbfs": round(db(peak), 3),
        "rmsDbfs": round(db(rms), 3),
        "clippingSampleCount": clipping_samples,
        "clippingPercent": round(clipping_samples / max(1, absolute.size) * 100, 6),
        "silenceThresholdDbfs": -50,
        "silenceIntervals": silent,
        "transientCandidatesSeconds": [round(item, 3) for item in ranked],
        "loopCandidate": {"start": round(best_start, 3), "end": round(best_start + loop_length, 3), "boundaryScore": round(best_score, 3)},
        "waveform": waveform.name,
        "spectrogram": spectrum.name,
    }


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--ffmpeg", required=True, type=Path)
    parser.add_argument("--ffprobe", required=True, type=Path)
    parser.add_argument("--output", required=True, type=Path)
    parser.add_argument("sources", nargs="+", type=Path)
    args = parser.parse_args()
    args.output.mkdir(parents=True, exist_ok=True)
    results = [analyze(args.ffmpeg, args.ffprobe, source, args.output) for source in args.sources]
    (args.output / "analysis.json").write_text(json.dumps(results, ensure_ascii=False, indent=2), encoding="utf-8")
    with (args.output / "analysis.csv").open("w", newline="", encoding="utf-8-sig") as handle:
        writer = csv.DictWriter(handle, fieldnames=[
            "file", "durationSeconds", "codec", "bitRateKbps", "sampleRate", "channels", "samplePeakDbfs", "rmsDbfs",
            "clippingSampleCount", "clippingPercent", "transientCandidatesSeconds", "loopCandidate",
        ])
        writer.writeheader()
        for item in results:
            writer.writerow({key: item[key] for key in writer.fieldnames})


if __name__ == "__main__":
    main()
