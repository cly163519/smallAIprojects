#!/usr/bin/env python3
"""Generate a gentle ambient loop for the Not Human Today project."""
from __future__ import annotations

import math
import wave
from pathlib import Path

SAMPLE_RATE = 44100
DURATION_SECONDS = 24
OUTPUT = Path(__file__).resolve().parents[1] / "Assets" / "ambient.wav"


def envelope(position: float) -> float:
    """Simple fade-in and fade-out envelope."""
    fade = min(position / 3.0, 1.0) * min((DURATION_SECONDS - position) / 3.0, 1.0)
    return max(fade, 0.05)


def synth_frame(n: int) -> float:
    t = n / SAMPLE_RATE
    base = (
        math.sin(2 * math.pi * 220 * t) * 0.4
        + math.sin(2 * math.pi * 440 * t) * 0.25
        + math.sin(2 * math.pi * 660 * t) * 0.1
    )
    slow_pulse = math.sin(2 * math.pi * 0.1 * t) * 0.2
    shimmer = math.sin(2 * math.pi * 6 * t) * 0.05
    return (base + slow_pulse + shimmer) * envelope(t)


def render() -> None:
    frames = bytearray()
    total_frames = SAMPLE_RATE * DURATION_SECONDS
    for i in range(total_frames):
        sample = max(-1.0, min(1.0, synth_frame(i)))
        frames.extend(int(sample * 32767).to_bytes(2, "little", signed=True))
    with wave.open(str(OUTPUT), "wb") as wav:
        wav.setnchannels(1)
        wav.setsampwidth(2)
        wav.setframerate(SAMPLE_RATE)
        wav.writeframes(bytes(frames))


if __name__ == "__main__":
    OUTPUT.parent.mkdir(parents=True, exist_ok=True)
    render()
    print(f"Wrote {OUTPUT}")
