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
│       └── tools/               # 6 tool implementations
└── unity-package/       # Unity Editor package (C#)
    └── Editor/
        ├── Core/                # WebSocket server, message routing
        ├── Tools/               # 6 tool implementations
        └── UI/                  # Status window
```

## License

MIT
