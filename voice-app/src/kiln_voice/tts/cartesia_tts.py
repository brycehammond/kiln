"""Text-to-speech using Cartesia SDK.

Converts text to speech audio and returns it as a numpy array
ready for playback.
"""

from __future__ import annotations

import io
import logging

import numpy as np
from cartesia import Cartesia

log = logging.getLogger(__name__)

# Cartesia's default English voice
DEFAULT_VOICE_ID = "a0e99841-438c-4a64-b679-ae501e7d6091"
SAMPLE_RATE = 24000


class CartesiaTTS:
    """Synthesize speech using Cartesia's API."""

    def __init__(self, api_key: str, voice_id: str = "") -> None:
        self._client = Cartesia(api_key=api_key)
        self._voice_id = voice_id or DEFAULT_VOICE_ID

    async def synthesize(self, text: str) -> np.ndarray:
        """Convert text to speech audio.

        Args:
            text: The text to speak.

        Returns:
            Numpy float32 array of audio samples at 24 kHz.
        """
        if not text.strip():
            return np.array([], dtype=np.float32)

        log.info("Synthesizing: %s", text[:80])

        output = self._client.tts.bytes(
            model_id="sonic-2",
            transcript=text,
            voice_id=self._voice_id,
            output_format={
                "container": "raw",
                "encoding": "pcm_f32le",
                "sample_rate": SAMPLE_RATE,
            },
        )

        audio = np.frombuffer(output, dtype=np.float32)
        log.info("Synthesized %d samples (%.1fs)", len(audio), len(audio) / SAMPLE_RATE)
        return audio
