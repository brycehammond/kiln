"""Microphone capture with push-to-talk support.

Records audio from the default input device while the push-to-talk key
is held down. Audio is delivered as 16-bit PCM at 16 kHz mono.
"""

from __future__ import annotations

import asyncio
import logging
import threading
from typing import Callable

import numpy as np
import sounddevice as sd

log = logging.getLogger(__name__)

SAMPLE_RATE = 16000
CHANNELS = 1
DTYPE = "int16"
BLOCK_SIZE = 1024


class AudioCapture:
    """Records microphone audio in a background thread."""

    def __init__(self) -> None:
        self._recording = False
        self._chunks: list[np.ndarray] = []
        self._stream: sd.InputStream | None = None
        self._lock = threading.Lock()

    def start_recording(self) -> None:
        """Begin capturing audio from the microphone."""
        with self._lock:
            if self._recording:
                return
            self._chunks.clear()
            self._recording = True

        log.info("Mic recording started")
        self._stream = sd.InputStream(
            samplerate=SAMPLE_RATE,
            channels=CHANNELS,
            dtype=DTYPE,
            blocksize=BLOCK_SIZE,
            callback=self._audio_callback,
        )
        self._stream.start()

    def stop_recording(self) -> bytes:
        """Stop capturing and return the recorded audio as raw PCM bytes."""
        with self._lock:
            self._recording = False

        if self._stream:
            self._stream.stop()
            self._stream.close()
            self._stream = None

        log.info("Mic recording stopped, %d chunks captured", len(self._chunks))

        if not self._chunks:
            return b""

        audio = np.concatenate(self._chunks)
        self._chunks.clear()
        return audio.tobytes()

    @property
    def is_recording(self) -> bool:
        return self._recording

    def _audio_callback(
        self,
        indata: np.ndarray,
        frames: int,
        time_info: object,
        status: sd.CallbackFlags,
    ) -> None:
        if status:
            log.warning("Audio capture status: %s", status)
        with self._lock:
            if self._recording:
                self._chunks.append(indata.copy())
