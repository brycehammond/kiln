"""Claude Code subprocess client.

Sends user text to `claude -p` and parses JSON responses.
Claude Code handles all MCP tool calls to Unity internally.
"""

from __future__ import annotations

import asyncio
import json
import logging
from dataclasses import dataclass

from .system_prompt import SYSTEM_PROMPT

log = logging.getLogger(__name__)

# All kiln MCP tools that should be pre-approved (no permission prompts).
ALLOWED_TOOLS = ",".join([
    "mcp__kiln__create_gameobject",
    "mcp__kiln__describe_scene",
    "mcp__kiln__explain_error",
    "mcp__kiln__create_script",
    "mcp__kiln__read_script",
    "mcp__kiln__get_project_summary",
    "mcp__kiln__save",
    "mcp__kiln__list_saves",
    "mcp__kiln__load_save",
])


@dataclass
class TurnResult:
    """Result of a single conversation turn."""

    assistant_text: str = ""


class ClaudeCodeClient:
    """Sends user utterances to Claude Code via subprocess."""

    def __init__(self, claude_path: str = "claude", cwd: str | None = None) -> None:
        self._claude_path = claude_path
        self._cwd = cwd  # Unity project dir so .claude/settings.json is found
        self._session_id: str | None = None

    async def send(self, user_text: str) -> TurnResult:
        """Send a user message to Claude Code and return the response."""
        cmd = [
            self._claude_path, "-p",
            "--output-format", "json",
            "--append-system-prompt", SYSTEM_PROMPT,
            "--allowedTools", ALLOWED_TOOLS,
            "--max-turns", "10",
        ]

        if self._session_id:
            cmd.extend(["--resume", self._session_id])

        cmd.append(user_text)

        log.info("Running: %s", " ".join(cmd[:6]) + " ...")

        proc = await asyncio.create_subprocess_exec(
            *cmd,
            stdout=asyncio.subprocess.PIPE,
            stderr=asyncio.subprocess.PIPE,
            cwd=self._cwd,
        )
        stdout, stderr = await proc.communicate()

        if proc.returncode != 0:
            err_text = stderr.decode(errors="replace").strip()
            log.error("claude exited %d: %s", proc.returncode, err_text)
            raise RuntimeError(f"Claude Code failed (exit {proc.returncode}): {err_text}")

        data = json.loads(stdout)
        self._session_id = data.get("session_id")

        result_text = data.get("result", "")
        log.info("Response (%d chars), session=%s", len(result_text), self._session_id)

        return TurnResult(assistant_text=result_text)
