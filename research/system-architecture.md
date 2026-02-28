# Unity Dev Framework - System Architecture

## 1. Vision & Goals

Build an accessible, voice-driven Unity development framework for a neurodiverse user (autism + ADHD). The system should:

- Accept voice commands to create and modify Unity projects
- Use a Claude subscription (Claude Max) instead of per-API-call billing
- Connect to Unity Editor via MCP server for full editor control
- Apply all 15 accessibility design principles (see accessibility-ux-patterns.md)
- Run on Windows
- Minimize cognitive load, maximize immediate feedback, and support executive function

---

## 2. Architecture Overview

```
+------------------------------------------------------------------+
|                        WINDOWS DESKTOP                            |
|                                                                   |
|  +------------------+    +------------------+    +-------------+  |
|  |  Voice Pipeline  |    |   Claude Code    |    | Unity Editor|  |
|  |    (Python)      |--->|   CLI (Max sub)  |--->| + MCP Plugin|  |
|  |                  |<---|                  |<---|             |  |
|  | - Mic capture    |    | - MCP client     |    | - WebSocket |  |
|  | - Wake word      |    | - Conversation   |    |   server    |  |
|  | - VAD            |    |   context        |    | - Tool      |  |
|  | - STT            |    | - Tool routing   |    |   handlers  |  |
|  | - TTS            |    | - CLAUDE.md      |    | - Undo      |  |
|  | - Audio playback |    |   persona        |    |   tracking  |  |
|  +------------------+    +------------------+    +-------------+  |
|         |                        |                      |         |
|         v                        v                      v         |
|  +------------------+    +------------------+    +-------------+  |
|  | Companion UI     |    | Unity MCP Server |    | Game Project|  |
|  | (Electron/Web)   |    | (Node.js stdio)  |    | (Assets,    |  |
|  | - Visual echo    |    | - Tools          |    |  Scripts,   |  |
|  | - Progress       |    | - Resources      |    |  Scenes)    |  |
|  | - Status panel   |    | - Prompts        |    |             |  |
|  +------------------+    +------------------+    +-------------+  |
+------------------------------------------------------------------+
```

### Data Flow

```
User speaks
  -> [Porcupine wake word / push-to-talk hotkey]
  -> [Silero VAD detects speech boundaries]
  -> [Deepgram Nova-3 streaming STT]
  -> Transcribed text
  -> [Claude Code CLI --print mode]
  -> Claude processes with MCP tools available
  -> Claude calls Unity MCP tools as needed
  -> [Unity MCP Server forwards to Unity Editor via WebSocket]
  -> [Unity Editor executes changes]
  -> Claude returns text response
  -> [Cartesia Sonic 3 streaming TTS]
  -> User hears response
  -> [Companion UI shows visual echo + status]
```

---

## 3. Claude Subscription Integration

### Strategy: Claude Code CLI as the AI backbone

Using Claude Max subscription ($100/mo) eliminates per-API-call costs. Claude Code CLI is the AI engine:

**How it works:**
1. Voice Pipeline transcribes speech to text
2. Text is sent to Claude Code CLI using its SDK/subprocess mode
3. Claude Code has the Unity MCP server configured in its settings
4. Claude Code calls MCP tools to control Unity
5. Claude Code's text response is captured and sent to TTS

**Claude Code invocation options (in order of preference):**

#### Option A: Claude Code SDK (Node.js) - Recommended
```javascript
import Anthropic from "@anthropic-ai/claude-code";

const client = new Anthropic();
// Uses Max subscription credits
const response = await client.messages.create({
  model: "claude-sonnet-4-5-20250929",
  messages: conversationHistory,
  // MCP servers auto-loaded from project config
});
```

#### Option B: Claude Code CLI subprocess
```python
import subprocess
result = subprocess.run(
    ["claude", "-p", "--output-format", "json", user_text],
    capture_output=True, text=True,
    cwd=unity_project_path
)
response = json.loads(result.stdout)
```

#### Option C: Claude Code with custom session management
```bash
# Maintain session for conversation continuity
claude --session-id "voice-session" -p "create a player controller"
```

### CLAUDE.md Persona File
A `CLAUDE.md` file in the Unity project configures Claude Code's behavior:

```markdown
# Unity Dev Assistant

You are a friendly Unity development assistant for a neurodiverse developer.

## Communication Style
- Keep responses to 1-2 sentences maximum
- Use simple, literal language (no idioms, sarcasm, or jargon)
- Always confirm what you understood before acting
- Offer details only when asked ("Want more detail?")
- Celebrate completed steps with brief encouragement

## Behavior Rules
- ALWAYS use Undo-compatible operations
- NEVER execute destructive actions without explicit confirmation
- Present one step at a time for multi-step tasks
- When errors occur, explain: what happened, why, and what to do next
- Auto-save after every change

## Available MCP Tools
You have access to Unity Editor via MCP. Use these tools to:
- Create/modify GameObjects and components
- Generate and edit C# scripts
- Manage scenes, prefabs, and materials
- Run builds and tests
- Read console logs for debugging
```

---

## 4. Component Design

### 4.1 Voice Pipeline (Python)

**Purpose**: Capture voice, convert to text, send to Claude Code, speak responses.

**Technology Stack:**
| Component | Primary | Fallback | Library |
|-----------|---------|----------|---------|
| Wake word | Picovoice Porcupine | Push-to-talk hotkey | `pvporcupine` |
| VAD | Silero VAD | WebRTC VAD | `silero-vad` (ONNX) |
| STT | Deepgram Nova-3 | whisper.cpp (local) | `deepgram-sdk` |
| TTS | Cartesia Sonic 3 | Piper (local) | `cartesia` / `piper-tts` |
| Audio I/O | sounddevice | pyaudio | `sounddevice` |
| Runtime | Python 3.11+ asyncio | | |

**Key Design Decisions:**
- **Streaming throughout**: STT transcribes while user speaks, TTS starts before full response
- **Hybrid activation**: Both wake word ("Hey Dev") and push-to-talk (configurable hotkey)
- **ADHD-friendly VAD**: 800-1200ms silence threshold (longer than typical 400ms)
- **Barge-in support**: User can interrupt TTS playback with new speech
- **Offline fallback**: whisper.cpp + Piper when internet unavailable

**Voice Pipeline Architecture:**
```python
class VoicePipeline:
    """Core voice loop orchestrator."""

    async def run(self):
        while True:
            # Wait for activation
            await self.activation.wait_for_trigger()

            # Capture speech with VAD
            audio = await self.vad.capture_utterance(
                silence_threshold_ms=800  # ADHD-friendly: don't rush
            )

            # Stream to STT
            transcript = await self.stt.transcribe(audio)

            # Visual echo (show what was heard)
            self.companion_ui.show_transcript(transcript)

            # Send to Claude Code
            response = await self.claude.send(transcript)

            # Parse response (separate spoken text from code/visual content)
            spoken, visual = self.response_parser.parse(response)

            # Stream TTS with barge-in detection
            await self.tts.speak_with_barge_in(spoken, self.vad)

            # Update companion UI with visual content
            self.companion_ui.show_response(visual)
```

**Cost Estimate (voice pipeline only, Claude covered by subscription):**
| Component | Rate | Est. Usage/hr | Cost/hr |
|-----------|------|---------------|---------|
| Deepgram STT | $0.0077/min | 15 min | $0.12 |
| Cartesia TTS | Usage-based | 10 min | $0.10 |
| Porcupine | Free tier | Always-on | $0.00 |
| **Total** | | | **~$0.22/hr** |

With Claude subscription, total cost is ~$0.22/hr + $100/mo flat = **~$135/mo at 40hr/week**.

### 4.2 Unity MCP Server

**Purpose**: Bridge between Claude Code and Unity Editor, exposing Unity operations as MCP tools.

**Foundation**: Extend CoderGamester/mcp-unity (most mature, MIT licensed, Node.js + C#)

**Architecture:**
```
Claude Code  <--stdio-->  Node.js MCP Server  <--WebSocket-->  Unity C# Plugin
                          (tools/resources)                    (editor automation)
```

**Why extend CoderGamester/mcp-unity:**
- Most mature and actively maintained
- Clean Node.js + C# WebSocket architecture
- 32+ tools already implemented
- MIT license
- UPM installable
- Already supports Claude Code via stdio transport

**Custom extensions we add:**

**Accessibility-specific tools:**
- `describe_scene` - Natural language scene description for voice output
- `create_from_template` - Simplified object creation from templates
- `explain_error` - Human-readable error explanations
- `suggest_next_step` - Context-aware next action suggestions
- `undo_last` / `redo_last` - Simple undo/redo wrappers
- `save_checkpoint` - Named save points for easy rollback
- `get_project_summary` - Brief project status overview

**Accessibility-specific resources:**
- `unity://session-recap` - What happened since last session
- `unity://project-progress` - Visual progress data
- `unity://recent-changes` - List of recent modifications with undo info

**Accessibility-specific prompts:**
- `new-project-wizard` - Guided project setup with voice
- `debug-helper` - Step-by-step error resolution
- `session-start` - "Where were we?" recap prompt

**MCP Server Configuration (`.claude/settings.json`):**
```json
{
  "mcpServers": {
    "unity": {
      "command": "node",
      "args": ["path/to/mcp-unity/Server~/build/index.js"],
      "env": {
        "UNITY_PORT": "8090"
      }
    }
  }
}
```

### 4.3 Unity Editor Plugin (C#)

**Purpose**: Receive MCP commands via WebSocket, execute Unity API calls on main thread.

**Key Components:**

```
Editor/
├── Core/
│   ├── McpBridge.cs              # WebSocket server, message routing
│   ├── MainThreadDispatcher.cs   # Thread-safe Unity API dispatch
│   └── UndoTracker.cs            # Track all changes for rollback
├── Tools/
│   ├── SceneTools.cs             # Scene hierarchy operations
│   ├── GameObjectTools.cs        # Create/modify/delete GameObjects
│   ├── ComponentTools.cs         # Add/modify components
│   ├── ScriptGenerator.cs        # C# script generation
│   ├── AssetTools.cs             # Asset management
│   ├── TemplateTools.cs          # Template-based creation
│   └── AccessibilityTools.cs     # describe_scene, explain_error, etc.
├── Resources/
│   ├── SceneDescriber.cs         # NL scene descriptions for voice
│   ├── ProgressTracker.cs        # Project progress tracking
│   └── SessionRecap.cs           # Session state management
├── UI/
│   ├── McpStatusWindow.cs        # Connection status + config
│   └── VoiceStatusOverlay.cs     # Voice pipeline status in editor
└── Templates/
    ├── Platformer2D/             # 2D platformer template
    ├── TopDownRPG/               # Top-down RPG template
    └── NarrativeAdventure/       # Story-driven template
```

**Critical implementation patterns:**

1. **Main thread dispatch** (all Unity API calls):
```csharp
public static Task<T> RunOnMainThread<T>(Func<T> action)
{
    var tcs = new TaskCompletionSource<T>();
    mainThreadQueue.Enqueue(() => {
        try { tcs.SetResult(action()); }
        catch (Exception e) { tcs.SetException(e); }
    });
    return tcs.Task;
}
```

2. **Undo integration** (every modification):
```csharp
// ALL changes go through Undo for safe rollback
Undo.RegisterCreatedObjectUndo(go, "Create " + go.name);
Undo.AddComponent<Rigidbody>(go);
Undo.RecordObject(transform, "Move " + go.name);
```

3. **Structured response format** (voice-friendly):
```csharp
return new ToolResult {
    SpokenSummary = "Created a player object with a Rigidbody.",
    DetailedResult = "Created GameObject 'Player' at (0,1,0) with Rigidbody (mass=1, useGravity=true)",
    VisualData = new { objectId = go.GetInstanceID(), position = go.transform.position }
};
```

### 4.4 Companion UI (Optional Desktop Overlay)

**Purpose**: Visual feedback panel alongside Unity Editor showing voice status, transcripts, and progress.

**Technology**: Electron app or Unity EditorWindow (or both)

**Features:**
- Voice input visual echo (show what was heard)
- Current action status (listening / thinking / speaking / idle)
- Recent conversation history (scrollable)
- Project progress dashboard
- Quick action buttons (undo, save, pause, stop listening)
- Sensory settings (theme, font size, audio volume)

**Design following accessibility principles:**
- Muted color palette (soft blues/greens/warm grays, no pure white)
- Large, clear text (minimum 16px)
- Consistent layout that never changes
- Gentle status indicators (soft pulsing, not flashing)
- No auto-playing animations
- Respects `prefers-reduced-motion`

---

## 5. Game Templates

Pre-built, AI-aware templates with natural-language-addressable parameters:

### Template: 2D Platformer
```yaml
name: "2D Platformer"
description: "Side-scrolling platform game with player movement and jumping"
parameters:
  player_speed: { default: 5, voice: "player speed", range: [1, 20] }
  jump_force: { default: 10, voice: "jump height", range: [5, 30] }
  gravity_scale: { default: 2, voice: "gravity", range: [0.5, 5] }
  has_double_jump: { default: false, voice: "double jump" }
  has_wall_jump: { default: false, voice: "wall jump" }
includes:
  - PlayerController.cs
  - CameraFollow.cs
  - LevelManager.cs
  - SampleLevel.unity
```

**Voice interaction example:**
> "Start a platformer game"
> "Got it. I'll create a 2D platformer with default settings. This includes a player that can move and jump, a camera that follows, and a sample level. Should I go ahead?"
> "Yeah"
> "Creating now... Done! Your platformer is ready. The player can move with arrow keys and jump with space. Want to try it or make changes?"
> "Make the player jump higher"
> "I'll increase the jump height from 10 to 15. Sound good?"
> "Yeah, and add double jump"
> "Done. Jump height is now 15 and double jump is on. Want to test it?"

---

## 6. Session Management

### Session Start (accessibility principle: Externalize Memory)
```
Voice: "Hey Dev"
System: "Welcome back! Last time you were working on your platformer.
         You added the player controller and created the first level.
         Ready to continue, or start something new?"
```

### Session End
```
Voice: "I'm done for today"
System: "Great session! Today you:
         - Added enemy AI to Level 1
         - Created 3 new sprites
         - Fixed the collision bug
         Everything is saved. See you next time!"
```

### Context Preservation
- Auto-save every change immediately
- Session state stored in project-local file
- Conversation history maintained across sessions (within Claude Code)
- Named checkpoints for easy rollback ("go back to before I added the enemies")

---

## 7. Technology Stack Summary

| Layer | Technology | Language | Why |
|-------|-----------|----------|-----|
| Voice Pipeline | Custom app | Python 3.11+ | Richest voice AI ecosystem |
| Wake Word | Picovoice Porcupine | Python SDK | 97% accuracy, free tier |
| VAD | Silero VAD | ONNX/Python | 4x fewer errors than WebRTC |
| STT | Deepgram Nova-3 | WebSocket | Sub-300ms, best streaming accuracy |
| STT Fallback | whisper.cpp | C++ (Python binding) | Offline capability |
| AI Engine | Claude Code CLI | Max subscription | No per-call costs |
| TTS | Cartesia Sonic 3 | WebSocket | 40ms time-to-first-audio |
| TTS Fallback | Piper | ONNX/Python | Offline, neural quality |
| MCP Server | mcp-unity (extended) | TypeScript/Node.js | Most mature, stdio transport |
| Unity Plugin | Custom C# package | C# | Editor automation, WebSocket |
| Companion UI | Electron / Unity IMGUI | TypeScript or C# | Visual feedback overlay |
| Target Unity | 2022.3 LTS minimum | | Broadest compatibility |
| Target OS | Windows 10/11 | | Primary platform |

---

## 8. Project Repository Structure

```
unity-dev-framework/
├── README.md
├── CLAUDE.md                          # Claude Code persona + instructions
├── .claude/
│   └── settings.json                  # MCP server configuration
│
├── voice-pipeline/                    # Python voice application
│   ├── pyproject.toml                 # Dependencies (uv/pip)
│   ├── src/
│   │   ├── __init__.py
│   │   ├── main.py                    # Entry point
│   │   ├── pipeline.py                # Core voice loop orchestrator
│   │   ├── activation/
│   │   │   ├── wake_word.py           # Porcupine wake word
│   │   │   └── push_to_talk.py        # Hotkey activation
│   │   ├── audio/
│   │   │   ├── capture.py             # Mic input (sounddevice)
│   │   │   ├── playback.py            # Speaker output
│   │   │   └── vad.py                 # Silero VAD
│   │   ├── stt/
│   │   │   ├── deepgram_stt.py        # Primary STT
│   │   │   └── whisper_stt.py         # Offline fallback
│   │   ├── tts/
│   │   │   ├── cartesia_tts.py        # Primary TTS
│   │   │   └── piper_tts.py           # Offline fallback
│   │   ├── claude/
│   │   │   ├── claude_code_bridge.py  # Interface to Claude Code CLI
│   │   │   └── response_parser.py     # Parse responses for voice vs visual
│   │   ├── ui/
│   │   │   ├── companion_window.py    # Desktop overlay UI
│   │   │   └── system_tray.py         # System tray icon + menu
│   │   └── config/
│   │       ├── settings.py            # User preferences
│   │       └── defaults.py            # Sensory-safe defaults
│   └── tests/
│       ├── test_pipeline.py
│       ├── test_vad.py
│       └── test_claude_bridge.py
│
├── mcp-server/                        # Unity MCP Server (Node.js)
│   ├── package.json
│   ├── tsconfig.json
│   └── src/
│       ├── index.ts                   # Server entry point
│       ├── tools/
│       │   ├── scene-tools.ts         # Scene management
│       │   ├── gameobject-tools.ts    # GameObject CRUD
│       │   ├── component-tools.ts     # Component management
│       │   ├── script-tools.ts        # Script generation
│       │   ├── template-tools.ts      # Template instantiation
│       │   └── accessibility-tools.ts # describe_scene, explain_error
│       ├── resources/
│       │   ├── scene-resources.ts
│       │   ├── progress-resources.ts
│       │   └── session-resources.ts
│       ├── prompts/
│       │   ├── new-project.ts
│       │   ├── debug-helper.ts
│       │   └── session-start.ts
│       └── unity-bridge/
│           └── websocket-client.ts    # WebSocket to Unity
│
├── unity-package/                     # Unity Editor Plugin (UPM)
│   ├── package.json                   # UPM manifest
│   ├── Editor/
│   │   ├── com.devframework.unity-mcp.editor.asmdef
│   │   ├── Core/
│   │   │   ├── McpBridge.cs
│   │   │   ├── MainThreadDispatcher.cs
│   │   │   └── UndoTracker.cs
│   │   ├── Tools/
│   │   │   ├── SceneTools.cs
│   │   │   ├── GameObjectTools.cs
│   │   │   ├── ComponentTools.cs
│   │   │   ├── ScriptGenerator.cs
│   │   │   ├── TemplateTools.cs
│   │   │   └── AccessibilityTools.cs
│   │   ├── Resources/
│   │   │   ├── SceneDescriber.cs
│   │   │   ├── ProgressTracker.cs
│   │   │   └── SessionRecap.cs
│   │   └── UI/
│   │       ├── McpStatusWindow.cs
│   │       └── VoiceStatusOverlay.cs
│   └── Runtime/
│       └── com.devframework.unity-mcp.runtime.asmdef
│
├── templates/                         # Game templates
│   ├── platformer-2d/
│   │   ├── template.yaml              # Template metadata + NL parameters
│   │   ├── Scripts/
│   │   ├── Scenes/
│   │   └── Prefabs/
│   ├── topdown-rpg/
│   └── narrative-adventure/
│
├── installer/                         # Windows installer
│   ├── install.ps1                    # PowerShell setup script
│   └── requirements.txt              # System requirements check
│
└── research/                          # Research documents
    ├── accessibility-ux-patterns.md
    ├── unity-mcp-analysis.md
    ├── voice-io-analysis.md
    ├── ai-gamedev-landscape.md
    └── system-architecture.md         # This document
```

---

## 9. Installation & Setup (Windows)

### Prerequisites
- Windows 10/11
- Unity 2022.3 LTS or Unity 6+
- Node.js 18+
- Python 3.11+
- Claude Max subscription with Claude Code installed

### Setup Script (installer/install.ps1)
```powershell
# 1. Install Python voice pipeline dependencies
cd voice-pipeline
pip install -e .

# 2. Install MCP server dependencies
cd ../mcp-server
npm install && npm run build

# 3. Install Unity package (via Package Manager in Unity Editor)
# Add git URL: https://github.com/user/unity-dev-framework.git?path=unity-package

# 4. Configure Claude Code MCP
claude mcp add unity node ./mcp-server/build/index.js

# 5. Set up wake word (optional)
python -m voice_pipeline.setup_wake_word

# 6. Launch
python -m voice_pipeline
```

---

## 10. Phased Development Plan

### Phase 1: Foundation (Weeks 1-3)
**Goal**: Text-based Claude Code + Unity MCP working end-to-end

- [ ] Fork/extend CoderGamester/mcp-unity or set up custom MCP server
- [ ] Implement core MCP tools: create_gameobject, modify_gameobject, add_component, create_script
- [ ] Create Unity Editor WebSocket plugin
- [ ] Write CLAUDE.md persona with accessibility guidelines
- [ ] Configure Claude Code with MCP server
- [ ] Test: type commands in Claude Code, see changes in Unity Editor
- [ ] Add `describe_scene` and `explain_error` accessibility tools

**Milestone**: Can type "create a red cube at position 0,1,0" in Claude Code and see it appear in Unity.

### Phase 2: Voice Pipeline MVP (Weeks 4-6)
**Goal**: Speak commands, hear responses, see Unity changes

- [ ] Build Python voice pipeline skeleton (main loop, plugin architecture)
- [ ] Integrate Deepgram streaming STT
- [ ] Integrate Cartesia streaming TTS
- [ ] Build Claude Code bridge (subprocess/SDK integration)
- [ ] Implement push-to-talk activation (simpler than wake word)
- [ ] Add basic visual echo (terminal/console output of transcripts)
- [ ] Test: speak "create a player object", hear confirmation, see it in Unity

**Milestone**: Full voice loop working - speak, AI acts in Unity, hear response.

### Phase 3: Accessibility & Polish (Weeks 7-9)
**Goal**: Apply all 15 accessibility design principles

- [ ] Add Porcupine wake word detection
- [ ] Add Silero VAD with ADHD-friendly silence thresholds
- [ ] Implement barge-in support
- [ ] Build session management (start recap, end summary)
- [ ] Add undo/redo tracking and "go back to before X"
- [ ] Create companion UI overlay (status, transcript, progress)
- [ ] Implement sensory-safe defaults (colors, sounds, animations)
- [ ] Add configurable verbosity levels
- [ ] Offline fallback mode (whisper.cpp + Piper)

**Milestone**: Comfortable, patient voice assistant that remembers context and celebrates progress.

### Phase 4: Templates & Content (Weeks 10-12)
**Goal**: Quick-start templates and guided project creation

- [ ] Build 2D Platformer template with NL-addressable parameters
- [ ] Build Top-Down RPG template
- [ ] Build Narrative Adventure template
- [ ] Implement `create_from_template` MCP tool
- [ ] Create new-project-wizard guided flow
- [ ] Add project progress tracking and milestone system
- [ ] Optional gamification (achievements for learning)

**Milestone**: "Start a platformer game" creates a playable project in under 60 seconds.

### Phase 5: Advanced Features (Ongoing)
- [ ] Multi-turn task tracking with visual progress
- [ ] Proactive suggestions based on Unity Editor state
- [ ] Voice-driven debugging ("why is my player falling through the floor?")
- [ ] Asset generation integration (Unity AI generators or Stable Diffusion)
- [ ] Custom voice persona selection
- [ ] Collaborative mode (parent + child working together)

---

## 11. Key Design Decisions & Rationale

### Why Claude Code CLI (not raw API)?
- Uses Claude Max subscription (flat monthly cost vs per-call)
- Native MCP server support (no custom MCP client needed)
- Conversation context management built-in
- CLAUDE.md persona configuration
- Session persistence across interactions

### Why Python for voice pipeline (not Node.js or C#)?
- Richest ecosystem for voice AI (Silero, Whisper, Porcupine, sounddevice)
- All recommended voice components have first-class Python SDKs
- asyncio for concurrent audio processing
- PyTorch/ONNX runtime for local ML models

### Why extend mcp-unity (not build from scratch)?
- 32+ tools already implemented and tested
- Active community and maintenance
- Clean architecture (Node.js stdio + C# WebSocket)
- MIT license allows extension
- Saves months of development time

### Why WebSocket for Unity bridge (not HTTP or pipes)?
- Bidirectional, full-duplex communication
- Low latency after handshake
- Native C# support in Unity
- Consensus approach used by 2 of 3 major MCP implementations

### Why Deepgram + Cartesia (not Azure or OpenAI)?
- Deepgram: Best streaming latency (<300ms) with highest accuracy
- Cartesia: 40ms time-to-first-audio (4x faster than alternatives)
- Both have WebSocket streaming APIs ideal for real-time voice
- Both have Python SDKs

### Why offline fallbacks?
- Internet outages shouldn't stop development
- Reduces dependence on cloud services
- whisper.cpp + Piper provide reasonable quality without internet
- Aligns with lessons from Mycroft/OVOS failure (cloud dependence was fatal)

---

## 12. Accessibility Integration Summary

Every component applies the 15 design principles:

| Principle | Voice Pipeline | MCP Server | Unity Plugin | Companion UI |
|-----------|---------------|------------|--------------|--------------|
| P1: Start Simple | Push-to-talk default | Core tools only | Simple EditorWindow | Minimal default view |
| P2: One at a Time | One command per turn | Sequential execution | One change at a time | Single status display |
| P3: Immediate Feedback | Listening indicator | Tool result confirmation | Visual change in editor | Status updates |
| P4: Never Surprise | Confirm before executing | Preview changes | Undo everything | Show what will happen |
| P5: Calm Defaults | Gentle audio cues | - | Muted theme | Soft colors, no white |
| P6: Externalize Memory | Session recap | Session resources | Progress tracking | History panel |
| P7: Reduce Friction | Wake word / hotkey | Smart defaults | Template-based creation | One-click actions |
| P8: Brief Voice | 1-2 sentence responses | Concise tool results | Short status text | Summary view |
| P9: Consistent | Same voice, same flow | Same tool patterns | Same layout always | Fixed layout |
| P10: Celebrate Progress | "Nice work!" feedback | Progress resources | Milestone markers | Achievement display |
| P11: Patient | Long silence threshold | No timeouts | No auto-dismiss | Persistent display |
| P12: Multimodal | Voice + text + visual | - | GUI + keyboard | All input modes |
| P13: Customizable | Voice speed, volume | - | Theme, font size | Sensory profiles |
| P14: Literal | Plain language | Clear error messages | Labeled icons | No jargon |
| P15: Support, Don't Overwhelm | Suggest, don't force | Offer, don't dump | Progressive disclosure | Hide complexity |

---

## 13. Security Considerations

- WebSocket server binds to localhost only (no network exposure)
- All file paths validated against directory traversal
- No arbitrary code execution - only defined MCP tools
- Confirmation required for destructive operations (delete, overwrite)
- Rate limiting on MCP operations to prevent runaway loops
- Audio data processed locally or via encrypted cloud APIs (HTTPS)
- No persistent storage of voice recordings
- Claude Code's built-in safety guardrails apply

---

## 14. Success Metrics

1. **Time to first playable game**: Under 10 minutes from "Hey Dev, start a platformer"
2. **Voice command success rate**: >90% of commands correctly understood and executed
3. **Undo reliability**: 100% of changes reversible
4. **Response latency**: <700ms from end of speech to first audio response
5. **Session continuity**: Seamless resume after breaks (recap, context preservation)
6. **User engagement**: Positive session summaries, progressive skill building
7. **Accessibility compliance**: All 15 design principles implemented and verified
