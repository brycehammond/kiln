"""Streaming speech-to-text using Deepgram SDK.

Takes raw PCM audio bytes and returns a transcript string via the
Deepgram pre-recorded (synchronous) API for simplicity. Streaming
can be added later.
"""

from __future__ import annotations

import logging

from deepgram import DeepgramClient, PrerecordedOptions

log = logging.getLogger(__name__)


class DeepgramSTT:
    """Transcribe audio using Deepgram's pre-recorded API."""

    def __init__(self, api_key: str) -> None:
        self._client = DeepgramClient(api_key)

    async def transcribe(self, audio_bytes: bytes, sample_rate: int = 16000) -> str:
        """Transcribe raw PCM 16-bit mono audio bytes to text.

        Args:
            audio_bytes: Raw PCM int16 audio data.
            sample_rate: Sample rate of the audio (default 16000).

        Returns:
            The transcribed text, or empty string if nothing was detected.
        """
        if not audio_bytes:
            return ""

        log.info("Transcribing %d bytes of audio", len(audio_bytes))

        options = PrerecordedOptions(
            model="nova-2",
            language="en",
            smart_format=True,
            encoding="linear16",
            sample_rate=sample_rate,
            channels=1,
        )

        payload = {"buffer": audio_bytes}

        response = self._client.listen.rest.v("1").transcribe_file(
            payload, options
        )

        transcript = (
            response.results.channels[0].alternatives[0].transcript
            if response.results
            and response.results.channels
            and response.results.channels[0].alternatives
            else ""
        )

        log.info("Transcript: %s", transcript)
        return transcript.strip()
