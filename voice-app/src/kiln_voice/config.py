"""Load configuration from ~/.kiln/config.json and environment variables."""

from __future__ import annotations

import json
import os
from pathlib import Path

from pydantic import BaseModel, Field


_CONFIG_PATH = Path.home() / ".kiln" / "config.json"


class Config(BaseModel):
    deepgram_api_key: str = ""
    cartesia_api_key: str = ""
    claude_path: str = "claude"
    unity_project_path: str = ""
    push_to_talk_key: str = "space"
    tts_voice: str = "default"

    @classmethod
    def load(cls) -> Config:
        """Load config from ~/.kiln/config.json, then overlay env vars."""
        data: dict = {}
        if _CONFIG_PATH.exists():
            with open(_CONFIG_PATH) as f:
                data = json.load(f)

        # Environment variables take precedence.
        env_map = {
            "DEEPGRAM_API_KEY": "deepgram_api_key",
            "CARTESIA_API_KEY": "cartesia_api_key",
        }
        for env_key, field in env_map.items():
            val = os.environ.get(env_key)
            if val:
                data[field] = val

        return cls(**data)
