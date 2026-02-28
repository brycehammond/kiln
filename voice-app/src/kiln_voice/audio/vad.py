"""Voice Activity Detection capture using silero-vad.

Opens a continuous microphone stream and fires an async callback
when speech is detected and followed by sufficient silence.
Audio format matches AudioCapture: 16 kHz mono int16 PCM.
"""

from __future__ import annotations

import asyncio
import logging
import threading
from typing import Callable, Awaitable

import numpy as np
import sounddevice as sd
from silero_vad import load_silero_vad, get_speech_timestamps

log = logging.getLogger(__name__)

SAMPLE_RATE = 16000
CHANNELS = 1
DTYPE = "int16"
BLOCK_SIZE = 512  # silero-vad works best with 512 samples at 16kHz

SPEECH_THRESHOLD = 0.5
SILENCE_DURATION_S = 0.8
SILENCE_CHUNKS = int(SILENCE_DURATION_S * SAMPLE_RATE / BLOCK_SIZE)


class VadCapture:
    """Continuous mic capture with voice-activity-based segmentation."""

    def __init__(
        self,
        on_speech: Callable[[bytes], Awaitable[None]],
        loop: asyncio.AbstractEventLoop | None = None,
    ) -> None:
        self._on_speech = on_speech
        self._loop = loop or asyncio.get_event_loop()

        self._model = load_silero_vad(onnx=True)
        self._stream: sd.InputStream | None = None
        self._lock = threading.Lock()

        # Buffering state (accessed from audio callback thread).
        self._in_speech = False
        self._chunks: list[np.ndarray] = []
        self._silence_count = 0

    # ------------------------------------------------------------------
    # Lifecycle
    # ------------------------------------------------------------------

    def start(self) -> None:
        """Open the mic and begin listening for speech."""
        if self._stream is not None:
            return
        log.info("VAD capture starting")
        self._stream = sd.InputStream(
            samplerate=SAMPLE_RATE,
            channels=CHANNELS,
            dtype=DTYPE,
            blocksize=BLOCK_SIZE,
            callback=self._audio_callback,
        )
        self._stream.start()

    def stop(self) -> None:
        """Stop the mic stream and discard any partial buffer."""
        if self._stream is None:
            return
        log.info("VAD capture stopping")
        self._stream.stop()
        self._stream.close()
        self._stream = None

        with self._lock:
            self._in_speech = False
            self._chunks.clear()
            self._silence_count = 0

    # ------------------------------------------------------------------
    # Audio callback (runs on sounddevice thread)
    # ------------------------------------------------------------------

    def _audio_callback(
        self,
        indata: np.ndarray,
        frames: int,
        time_info: object,
        status: sd.CallbackFlags,
    ) -> None:
        if status:
            log.warning("VAD audio status: %s", status)

        # silero-vad expects a 1-D float32 tensor in [-1, 1].
        audio_f32 = indata[:, 0].astype(np.float32) / 32768.0

        import torch

        tensor = torch.from_numpy(audio_f32)
        prob = self._model(tensor, SAMPLE_RATE).item()

        with self._lock:
            if prob >= SPEECH_THRESHOLD:
                if not self._in_speech:
                    log.debug("Speech start detected (prob=%.2f)", prob)
                self._in_speech = True
                self._silence_count = 0
                self._chunks.append(indata.copy())

            elif self._in_speech:
                # Still buffering, but counting silence.
                self._chunks.append(indata.copy())
                self._silence_count += 1

                if self._silence_count >= SILENCE_CHUNKS:
                    self._flush()

    def _flush(self) -> None:
        """Emit buffered audio and reset. Must be called under self._lock."""
        if not self._chunks:
            return

        audio = np.concatenate(self._chunks)
        pcm_bytes = audio.tobytes()

        self._chunks.clear()
        self._in_speech = False
        self._silence_count = 0

        log.info("VAD flushing %d bytes of speech", len(pcm_bytes))
        self._loop.call_soon_threadsafe(
            asyncio.ensure_future,
            self._on_speech(pcm_bytes),
        )
