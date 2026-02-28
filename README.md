# Kiln

Accessible Unity game development through natural language. Type what you want in Claude Code, and it happens in Unity.

Kiln supports both full 3D games and top-down 2D games (Zelda-style).

## How It Works

```
Claude Code CLI
  └── Kiln MCP Server (Node.js)
        └── WebSocket → Unity Editor (port 8091)
              └── Kiln Unity Package (Editor tools)
```

You type a command like "create a red cube at position 0, 1, 0" in Claude Code. Kiln translates that into Unity API calls, executes them on the main thread, and reports back. Every change is undoable.

## Tools

| Tool | Description |
|------|-------------|
| `create_gameobject` | Create 3D primitives, 2D sprites, set color/position/components |
| `describe_scene` | Get a natural language description of the current scene |
| `explain_error` | Translate Unity errors into plain English with fixes |
| `create_script` | Generate C# scripts and optionally attach to objects |
| `read_script` | Read script contents by path or class name |
| `get_project_summary` | Overview of project assets, scenes, and state |

## Setup

### Prerequisites

- Unity 2022.3+
- Node.js 18+
- Python 3.11+ (for the voice app only)
- Claude Code CLI (Anthropic Max subscription)

### 1. Build the MCP server

```bash
cd mcp-server
npm install
npm run build
```

### 2. Add the Unity package

In your Unity project's `Packages/manifest.json`, add a local reference:

```json
{
  "dependencies": {
    "com.kiln.mcp": "file:../../unity-dev-framework/unity-package",
    "com.unity.nuget.newtonsoft-json": "3.2.1"
  }
}
```

Adjust the path to point to the `unity-package/` directory.

### 3. Open Unity

The Kiln WebSocket server starts automatically on port 8091. Check status via **Window > Kiln > Status**.

### 4. Use Claude Code

From this project directory:

```bash
claude
```

The MCP server is configured in `.claude/settings.json` and connects automatically.

### 5. Voice app (optional)

The voice app adds hands-free voice control. See the [Voice App Setup](#voice-app-setup) section below.

## Platform Setup

### macOS

1. Install dependencies with [Homebrew](https://brew.sh):

```bash
brew install node python
```

2. PortAudio is required by the voice app for microphone access. Install it before installing the Python dependencies:

```bash
brew install portaudio
```

3. On first run, macOS will prompt for microphone permission. Click **Allow** when the system dialog appears.

### Windows

1. Install [Node.js](https://nodejs.org) (v18+) and [Python](https://www.python.org/downloads/) (v3.11+) using their official installers. Make sure both are added to your `PATH` during installation.

2. The voice app uses `sounddevice`, which depends on PortAudio. On Windows, `sounddevice` bundles PortAudio automatically — no extra install needed.

3. If you see build errors when installing Python packages, install the [Visual Studio Build Tools](https://visualstudio.microsoft.com/visual-cpp-build-tools/) with the "Desktop development with C++" workload.

## Voice App Setup

The voice app provides a GUI for talking to Kiln instead of typing. It records your voice, transcribes it, sends the text to Claude Code, and speaks the response.

### Prerequisites

- A [Deepgram](https://deepgram.com) API key (for speech-to-text)
- A [Cartesia](https://cartesia.ai) API key (for text-to-speech)

### Install

```bash
cd voice-app
pip install -e .
```

### Configure

Create `~/.kiln/config.json`:

```json
{
  "deepgram_api_key": "YOUR_DEEPGRAM_KEY",
  "cartesia_api_key": "YOUR_CARTESIA_KEY",
  "unity_project_path": "/path/to/your/unity/project"
}
```

You can also set `DEEPGRAM_API_KEY` and `CARTESIA_API_KEY` as environment variables instead.

All config options:

| Key | Default | Description |
|-----|---------|-------------|
| `deepgram_api_key` | (required) | Deepgram API key for speech-to-text |
| `cartesia_api_key` | (required) | Cartesia API key for text-to-speech |
| `unity_project_path` | `""` | Path to Unity project (working directory for Claude Code) |
| `claude_path` | `"claude"` | Path to the Claude Code CLI binary |
| `push_to_talk_key` | `"space"` | Key used for push-to-talk |
| `input_mode` | `"ptt"` | `"ptt"` for push-to-talk, `"vad"` for hands-free voice detection |
| `tts_voice` | `"default"` | Cartesia voice ID |

### Run

```bash
kiln-voice
```

Hold **Space** to talk (push-to-talk mode, the default). Release to send your speech through the pipeline.

### Hands-free mode (VAD)

Set `"input_mode": "vad"` in your config to enable voice activity detection. The app will listen continuously and automatically detect when you start and stop speaking — no button press needed.

## Examples

```
> create a red cube at position 0, 1, 0
> describe the scene
> create a script called PlayerMovement
> what does this error mean? NullReferenceException: Object reference not set
> get project summary
```

## Project Structure

```
kiln/
├── mcp-server/          # Node.js MCP server (TypeScript)
│   └── src/
│       ├── index.ts             # Entry point, registers tools
│       ├── unity-bridge/        # WebSocket client to Unity
│       └── tools/               # Tool implementations
├── unity-package/       # Unity Editor package (C#)
│   └── Editor/
│       ├── Core/                # WebSocket server, message routing
│       ├── Tools/               # Tool implementations
│       └── UI/                  # Status window
└── voice-app/           # Voice-controlled GUI (Python)
    └── src/kiln_voice/
        ├── main.py              # Entry point
        ├── app.py               # Orchestrator (STT → Claude → TTS)
        ├── audio/               # Mic capture, playback, VAD
        ├── stt/                 # Deepgram speech-to-text
        ├── tts/                 # Cartesia text-to-speech
        ├── claude/              # Claude Code subprocess client
        └── ui/                  # CustomTkinter window
```

## License

MIT
