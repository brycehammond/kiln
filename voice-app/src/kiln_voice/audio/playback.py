"""Audio playback using sounddevice.

Plays raw PCM audio (or numpy arrays) through the default output device.
Supports async playback so the voice loop can wait for speech to finish.
"""

from __future__ import annotations

import asyncio
import logging
import threading

import numpy as np
import sounddevice as sd

log = logging.getLogger(__name__)

SAMPLE_RATE = 24000  # Cartesia outputs 24 kHz by default
CHANNELS = 1
DTYPE = "float32"


class AudioPlayback:
    """Plays audio samples through the default output device."""

    def __init__(self, sample_rate: int = SAMPLE_RATE) -> None:
        self._sample_rate = sample_rate
        self._playing = False
        self._lock = threading.Lock()

    async def play(self, audio_data: np.ndarray) -> None:
        """Play audio and wait until playback finishes."""
        if audio_data.size == 0:
            return

        loop = asyncio.get_running_loop()
        done = asyncio.Event()

        def _play_blocking() -> None:
            with self._lock:
                self._playing = True
            try:
                sd.play(audio_data, samplerate=self._sample_rate, blocking=True)
            finally:
                with self._lock:
                    self._playing = False
                loop.call_soon_threadsafe(done.set)

        thread = threading.Thread(target=_play_blocking, daemon=True)
        thread.start()
        await done.wait()

    async def play_bytes(self, pcm_bytes: bytes, sample_rate: int | None = None) -> None:
        """Play raw PCM float32 bytes."""
        if not pcm_bytes:
            return
        audio = np.frombuffer(pcm_bytes, dtype=np.float32)
        if sample_rate:
            self._sample_rate = sample_rate
        await self.play(audio)

    @property
    def is_playing(self) -> bool:
        return self._playing

    def stop(self) -> None:
        """Stop current playback."""
        sd.stop()
        with self._lock:
            self._playing = False
