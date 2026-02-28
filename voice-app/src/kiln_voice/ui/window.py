"""CustomTkinter window for the Kiln voice assistant.

Shows status, Unity connection indicator, scrolling transcript,
and a push-to-talk button.
"""

from __future__ import annotations

import asyncio
import logging
import tkinter as tk
from typing import Callable

import customtkinter as ctk

from . import theme

log = logging.getLogger(__name__)


class KilnWindow:
    """Main application window."""

    def __init__(
        self,
        on_ptt_press: Callable[[], None] | None = None,
        on_ptt_release: Callable[[], None] | None = None,
    ) -> None:
        self._on_ptt_press = on_ptt_press
        self._on_ptt_release = on_ptt_release

        ctk.set_appearance_mode("light")

        self.root = ctk.CTk()
        self.root.title("Kiln Voice")
        self.root.geometry("480x640")
        self.root.configure(fg_color=theme.BG)
        self.root.resizable(True, True)

        self._build_ui()
        self._bind_keys()

    # ------------------------------------------------------------------
    # UI construction
    # ------------------------------------------------------------------

    def _build_ui(self) -> None:
        # Top bar: status + connection indicator
        top_frame = ctk.CTkFrame(self.root, fg_color=theme.BG, corner_radius=0)
        top_frame.pack(fill="x", padx=16, pady=(16, 8))

        self._status_label = ctk.CTkLabel(
            top_frame,
            text="Ready",
            font=(theme.FONT_FAMILY, theme.FONT_SIZE_LARGE, "bold"),
            text_color=theme.TEXT,
        )
        self._status_label.pack(side="left")

        self._connection_dot = ctk.CTkLabel(
            top_frame,
            text="\u25cf",  # Filled circle
            font=(theme.FONT_FAMILY, theme.FONT_SIZE),
            text_color=theme.ERROR,
        )
        self._connection_dot.pack(side="right")

        self._connection_label = ctk.CTkLabel(
            top_frame,
            text="Unity: disconnected",
            font=(theme.FONT_FAMILY, theme.FONT_SIZE_SMALL),
            text_color=theme.TEXT_MUTED,
        )
        self._connection_label.pack(side="right", padx=(0, 8))

        # Transcript area
        self._transcript = ctk.CTkTextbox(
            self.root,
            font=(theme.FONT_FAMILY, theme.FONT_SIZE),
            fg_color=theme.BG_DARKER,
            text_color=theme.TEXT,
            corner_radius=8,
            wrap="word",
            state="disabled",
        )
        self._transcript.pack(fill="both", expand=True, padx=16, pady=8)

        # Push-to-talk button
        self._ptt_button = ctk.CTkButton(
            self.root,
            text="Hold Space to Talk",
            font=(theme.FONT_FAMILY, theme.FONT_SIZE, "bold"),
            fg_color=theme.ACCENT,
            hover_color=theme.ACCENT_HOVER,
            text_color="white",
            corner_radius=8,
            height=56,
        )
        self._ptt_button.pack(fill="x", padx=16, pady=(8, 16))

    # ------------------------------------------------------------------
    # Key bindings
    # ------------------------------------------------------------------

    def _bind_keys(self) -> None:
        self.root.bind("<KeyPress-space>", self._on_space_press)
        self.root.bind("<KeyRelease-space>", self._on_space_release)

        # Also support mouse on the button
        self._ptt_button.bind("<ButtonPress-1>", self._on_space_press)
        self._ptt_button.bind("<ButtonRelease-1>", self._on_space_release)

    def _on_space_press(self, event: tk.Event | None = None) -> None:
        if self._on_ptt_press:
            self._on_ptt_press()

    def _on_space_release(self, event: tk.Event | None = None) -> None:
        if self._on_ptt_release:
            self._on_ptt_release()

    # ------------------------------------------------------------------
    # Public API
    # ------------------------------------------------------------------

    def set_status(self, text: str) -> None:
        """Update the status label."""
        self._status_label.configure(text=text)

    def set_connected(self, connected: bool) -> None:
        """Update the Unity connection indicator."""
        if connected:
            self._connection_dot.configure(text_color=theme.SUCCESS)
            self._connection_label.configure(text="Unity: connected")
        else:
            self._connection_dot.configure(text_color=theme.ERROR)
            self._connection_label.configure(text="Unity: disconnected")

    def append_transcript(self, speaker: str, text: str) -> None:
        """Append a line to the transcript area."""
        self._transcript.configure(state="normal")
        self._transcript.insert("end", f"{speaker}: {text}\n\n")
        self._transcript.configure(state="disabled")
        self._transcript.see("end")

    def set_ptt_active(self, active: bool) -> None:
        """Visually indicate push-to-talk state."""
        if active:
            self._ptt_button.configure(
                text="Listening...",
                fg_color=theme.WARNING,
            )
        else:
            self._ptt_button.configure(
                text="Hold Space to Talk",
                fg_color=theme.ACCENT,
            )

    def update(self) -> None:
        """Process pending Tk events (call from async loop)."""
        self.root.update_idletasks()
        self.root.update()

    def is_alive(self) -> bool:
        """Check if the window is still open."""
        try:
            self.root.winfo_exists()
            return True
        except tk.TclError:
            return False
