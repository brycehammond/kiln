"""Voice loop orchestrator.

Coordinates the full pipeline:
1. Push-to-talk (spacebar) -> record audio
2. Deepgram STT -> transcript
3. Claude Code subprocess -> MCP tool calls -> Unity
4. Cartesia TTS -> Speaker
5. Update transcript UI
"""

from __future__ import annotations

import asyncio
import logging

from .config import Config
from .audio.capture import AudioCapture
from .audio.playback import AudioPlayback
from .stt.deepgram_stt import DeepgramSTT
from .tts.cartesia_tts import CartesiaTTS
from .claude.claude_code import ClaudeCodeClient
from .ui.window import KilnWindow

log = logging.getLogger(__name__)

UI_POLL_INTERVAL = 0.03  # ~30fps for Tk event loop


class App:
    """Main application orchestrator."""

    def __init__(self, config: Config) -> None:
        self._config = config

        # Audio
        self._capture = AudioCapture()
        self._playback = AudioPlayback()

        # STT / TTS
        self._stt = DeepgramSTT(api_key=config.deepgram_api_key)
        self._tts = CartesiaTTS(
            api_key=config.cartesia_api_key,
            voice_id=config.tts_voice if config.tts_voice != "default" else "",
        )

        # Claude Code subprocess client
        self._claude = ClaudeCodeClient(
            claude_path=config.claude_path,
            cwd=config.unity_project_path or None,
        )

        # UI
        self._window = KilnWindow(
            on_ptt_press=self._on_ptt_press,
            on_ptt_release=self._on_ptt_release,
        )

        self._processing = False
        self._ptt_held = False

    # ------------------------------------------------------------------
    # Public
    # ------------------------------------------------------------------

    async def run(self) -> None:
        """Start the app and run until the window is closed."""
        self._window.set_connected(True)

        try:
            while self._window.is_alive():
                self._window.update()
                await asyncio.sleep(UI_POLL_INTERVAL)
        except KeyboardInterrupt:
            pass

    # ------------------------------------------------------------------
    # Push-to-talk callbacks (called from Tk thread)
    # ------------------------------------------------------------------

    def _on_ptt_press(self) -> None:
        if self._processing or self._ptt_held:
            return
        self._ptt_held = True
        self._window.set_ptt_active(True)
        self._window.set_status("Listening...")
        self._capture.start_recording()

    def _on_ptt_release(self) -> None:
        if not self._ptt_held:
            return
        self._ptt_held = False
        self._window.set_ptt_active(False)

        audio_bytes = self._capture.stop_recording()
        if not audio_bytes:
            self._window.set_status("Ready")
            return

        # Schedule async processing.
        asyncio.ensure_future(self._process_audio(audio_bytes))

    # ------------------------------------------------------------------
    # Processing pipeline
    # ------------------------------------------------------------------

    async def _process_audio(self, audio_bytes: bytes) -> None:
        """Full pipeline: STT -> Claude Code -> TTS."""
        self._processing = True
        try:
            # Step 1: Transcribe
            self._window.set_status("Transcribing...")
            transcript = await self._stt.transcribe(audio_bytes)

            if not transcript:
                self._window.set_status("Ready")
                return

            self._window.append_transcript("You", transcript)

            # Step 2: Send to Claude Code (tool calls handled internally)
            self._window.set_status("Thinking...")
            result = await self._claude.send(transcript)

            # Step 3: Speak Claude's response
            if result.assistant_text:
                self._window.append_transcript("Kiln", result.assistant_text)
                await self._speak(result.assistant_text)

            self._window.set_status("Ready")

        except Exception as exc:
            log.exception("Processing failed")
            self._window.set_status("Ready")
            self._window.append_transcript("Kiln", f"Something went wrong: {exc}")
        finally:
            self._processing = False

    async def _speak(self, text: str) -> None:
        """Synthesize and play speech."""
        self._window.set_status("Speaking...")
        try:
            audio = await self._tts.synthesize(text)
            await self._playback.play(audio)
        except Exception as exc:
            log.error("TTS failed: %s", exc)
