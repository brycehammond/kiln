"""Entry point for the Kiln voice assistant."""

from __future__ import annotations

import asyncio
import logging
import sys

from .config import Config
from .app import App


def main() -> None:
    logging.basicConfig(
        level=logging.INFO,
        format="%(asctime)s [%(name)s] %(levelname)s: %(message)s",
        stream=sys.stderr,
    )

    config = Config.load()

    # Validate required keys.
    missing = []
    if not config.deepgram_api_key:
        missing.append("deepgram_api_key / DEEPGRAM_API_KEY")
    if not config.cartesia_api_key:
        missing.append("cartesia_api_key / CARTESIA_API_KEY")

    if missing:
        print(
            f"Missing required config: {', '.join(missing)}\n"
            "Set them in ~/.kiln/config.json or as environment variables.",
            file=sys.stderr,
        )
        sys.exit(1)

    app = App(config)
    asyncio.run(app.run())


if __name__ == "__main__":
    main()
