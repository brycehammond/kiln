# Unity MCP Server & Editor Scripting - Technical Analysis

## Table of Contents
1. [Existing Unity MCP Server Implementations](#1-existing-unity-mcp-server-implementations)
2. [MCP Protocol Overview](#2-mcp-protocol-overview)
3. [Unity Editor Scripting APIs](#3-unity-editor-scripting-apis)
4. [Programmatic Unity Control](#4-programmatic-unity-control)
5. [Communication Bridge Approaches](#5-communication-bridge-approaches)
6. [C# Code Generation Patterns](#6-c-code-generation-patterns)
7. [Unity Package Manager (UPM)](#7-unity-package-manager-upm)
8. [Unity Version Considerations](#8-unity-version-considerations)
9. [Technical Recommendations](#9-technical-recommendations)

---

## 1. Existing Unity MCP Server Implementations

### 1.1 CoderGamester/mcp-unity (Most Mature)

**Repository**: https://github.com/CoderGamester/mcp-unity
**License**: MIT
**Architecture**: Node.js MCP Server + Unity C# WebSocket Bridge

**Key Characteristics**:
- **Three-tier architecture**: AI Client -> Node.js MCP Server (TypeScript) -> Unity WebSocket Server (C#)
- Node.js server implements MCP protocol (JSON-RPC 2.0 over stdio/SSE) and forwards requests to Unity via WebSocket
- Unity-side WebSocket server listens on configurable port (default 8090)
- **32+ tools** covering scene management, GameObject CRUD, component editing, prefab creation, material management, test running, package management
- **7 resource endpoints**: `unity://menu-items`, `unity://scenes-hierarchy`, `unity://gameobject/{id}`, `unity://logs`, `unity://packages`, `unity://assets`, `unity://tests/{testMode}`
- Supports batch operations with rollback
- Requires Unity 6+ and Node.js 18+
- Configurable via Unity Editor window (Tools > MCP Unity > Server Window)

**Tool Categories**:
- Scene & GameObject: select, update, duplicate, delete, reparent, move, rotate, scale, set_transform
- Scene Operations: create, load, unload, delete, save, get_scene_info
- Components & Assets: update_component, create_prefab, add_asset_to_scene, create/assign/modify_material
- Project: execute_menu_item, add_package, recompile_scripts, run_tests, batch_execute, console logs

**Package Structure**:
```
mcp-unity/
├── Editor/                    # Unity Editor C# code
│   ├── Tools/                 # McpToolBase implementations
│   ├── Resources/             # McpResourceBase implementations
│   ├── UnityBridge/           # WebSocket server + message handling
│   └── Services/              # Core services
├── Server~/                   # Node.js MCP server (~ hides from Unity)
│   ├── src/tools/             # MCP tool definitions
│   ├── src/resources/         # MCP resource definitions
│   ├── src/unity/             # WebSocket client logic
│   └── build/                 # Compiled JS
└── package.json               # UPM manifest
```

**Strengths**: Most comprehensive tool set, well-documented, MIT license, active development, UPM-installable
**Limitations**: Requires Node.js runtime, Unity 6+ only, no spaces in project path

---

### 1.2 IvanMurzak/Unity-MCP (Most Extensible)

**Repository**: https://github.com/IvanMurzak/Unity-MCP
**License**: MIT
**Architecture**: C# ASP.NET Core MCP Server + Unity Plugin (TCP/WebSocket bridge)

**Key Characteristics**:
- **50+ built-in tools** organized by category
- C# MCP server (not Node.js) using the official MCP C# SDK
- Supports both **stdio** and **streamableHttp** transports
- **Attribute-based tool discovery** using reflection: `[McpPluginToolType]` on classes, `[McpPluginTool]` on methods
- SignalR hub for routing requests via RpcRouter
- Cross-platform binaries provided (Windows, macOS, Linux)
- Roslyn-based code compilation and validation
- Docker deployment support

**Tool System Design**:
```csharp
[McpPluginToolType]
public class MyCustomTools
{
    [McpPluginTool("my_tool_name")]
    [Description("Description for LLM")]
    public static string MyTool(
        [Description("param description")] string param1,
        int? optionalParam = null)
    {
        return MainThread.Instance.Run(() => {
            // Unity API calls here (main thread)
            return "result";
        });
    }
}
```

**Tool Categories**:
- Project & Assets: copy, create folders, delete, find, materials, prefabs, shaders, packages
- Scene & Hierarchy: components (add/destroy/get/list/modify), GameObjects (create/destroy/duplicate/find), scenes (create/open/save/list/unload), screenshots
- Scripting & Editor: console logs, editor state, selection, reflection (find/call methods), scripts (read/write/execute/delete), tests

**Strengths**: Pure C# stack, attribute-based extensibility, reflection-powered tool discovery, most tools, Docker support
**Limitations**: More complex setup, heavier runtime

---

### 1.3 CoplayDev/unity-mcp (Best Performance)

**Repository**: https://github.com/CoplayDev/unity-mcp
**License**: MIT
**Architecture**: Python MCP Server + Unity HTTP Bridge

**Key Characteristics**:
- Python-based server using `uv` package manager
- HTTP transport (localhost:8080 default) with optional LAN/remote binding
- **30+ tools** with emphasis on batch operations
- `batch_execute` claimed to be **10-100x faster** than individual calls
- Roslyn-based script validation with strict type checking
- Supports **multiple simultaneous Unity instances**
- Auto-configures MCP clients (Claude Desktop, Cursor, Windsurf, VS Code)
- Supports Unity 2021.3 LTS+ (broadest compatibility)
- Privacy-focused telemetry with opt-out

**Strengths**: Broadest Unity version support (2021.3+), batch performance, multi-instance support, Python ecosystem
**Limitations**: Requires Python 3.10+, fewer tools than IvanMurzak

---

### 1.4 NoSpoonLab/unity-mcp

**Repository**: https://github.com/NoSpoonLab/unity-mcp
**Architecture**: Bridge between Unity Editor and external LLMs
**Status**: Less mature, fewer features compared to the top three

---

### Comparison Matrix

| Feature | CoderGamester | IvanMurzak | CoplayDev |
|---------|--------------|------------|-----------|
| Language | TypeScript/C# | C#/C# | Python/C# |
| Tools Count | 32+ | 50+ | 30+ |
| Transport | stdio/SSE + WebSocket | stdio/streamableHttp | HTTP/stdio |
| Unity Version | 6+ | Not specified (broad) | 2021.3+ |
| Extensibility | McpToolBase inheritance | Attribute-based reflection | Standard |
| Batch Operations | Yes (with rollback) | Standard | Yes (10-100x faster) |
| Custom Tools | C# class inheritance | Attribute decoration | Standard |
| Installation | UPM via Git URL | OpenUPM / .unitypackage | UPM via Git URL |
| License | MIT | MIT | MIT |

---

## 2. MCP Protocol Overview

### 2.1 Protocol Foundation

- **Specification**: Current version is **2025-11-25** (https://modelcontextprotocol.io/specification/2025-11-25)
- **Communication**: JSON-RPC 2.0 over various transports
- **Architecture**: Host (LLM app) -> Client (connector) -> Server (capabilities provider)
- **Inspiration**: Modeled after Language Server Protocol (LSP) for AI tool integration

### 2.2 Core Primitives

**Tools** (Model-controlled):
- Executable functions exposed by servers
- Defined with name, description, and JSON Schema for input parameters
- Invoked via `tools/call` request, returns results or errors
- AI model decides when and how to use them

**Resources** (Application-controlled):
- Data sources providing context to the model
- Identified by URI (e.g., `file://`, `db://`, custom schemes like `unity://`)
- Application decides when to fetch and pass as context
- Accessed via `resources/read` request

**Prompts** (User-controlled):
- Templated messages and workflows
- User selects which prompts to use
- Can include dynamic parameters

### 2.3 Transport Mechanisms

**stdio** (Standard Input/Output):
- Newline-delimited JSON-RPC on stdin/stdout
- Client spawns server as child process
- Logging via stderr
- Best for: local integrations, CLI tools, development

**Streamable HTTP** (Current standard, introduced 2025-03-26):
- Single endpoint supporting POST (client->server) and GET (server->client SSE)
- Supports both local and remote deployment
- Multi-client capable
- Session management via `Mcp-Session-Id` header
- Resumability through SSE event IDs
- Requires HTTPS for production

**SSE** (Legacy, deprecated but supported):
- Server-Sent Events for server->client streaming
- HTTP POST for client->server
- Being superseded by Streamable HTTP

### 2.4 Message Formats

```json
// Request
{"jsonrpc": "2.0", "id": 1, "method": "tools/call", "params": {"name": "tool_name", "arguments": {...}}}

// Success Response
{"jsonrpc": "2.0", "id": 1, "result": {"content": [{"type": "text", "text": "..."}]}}

// Error Response
{"jsonrpc": "2.0", "id": 1, "error": {"code": -32602, "message": "Invalid params"}}

// Notification (no id)
{"jsonrpc": "2.0", "method": "notifications/tools/list_changed"}
```

### 2.5 Capability Negotiation

1. Client sends `initialize` with supported `protocolVersion` and client `capabilities`
2. Server responds with chosen protocol version, server `capabilities`, and optional `instructions`
3. Client sends `initialized` notification
4. Only `ping` and logging permitted before initialization completes

### 2.6 Additional Features (2025-11-25 spec)

- **Tasks primitive**: Experimental "call-now, fetch-later" pattern for long-running operations
- **Canonical tool names**: Standardized format for display and reference
- **Extension negotiation**: Formal mechanism for optional extensions
- **Sampling**: Server-initiated LLM interactions (reverse calls)
- **Elicitation**: Server-initiated user information requests
- **Roots**: Server queries about filesystem/URI boundaries

---

## 3. Unity Editor Scripting APIs

### 3.1 Core Editor APIs for Automation

**AssetDatabase** (`UnityEditor.AssetDatabase`):
- `CreateAsset()` / `DeleteAsset()` - Create/delete assets
- `CreateFolder()` - Create folder structure
- `ImportAsset()` / `Refresh()` - Import and refresh assets
- `LoadAssetAtPath<T>()` - Load assets by path
- `FindAssets()` - Search assets by filter
- `GenerateUniqueAssetPath()` - Avoid naming conflicts
- `MoveAsset()` / `CopyAsset()` - File operations
- `SaveAssets()` - Persist changes to disk

**PrefabUtility** (`UnityEditor.PrefabUtility`):
- `SaveAsPrefabAsset()` / `SaveAsPrefabAssetAndConnect()` - Create prefabs
- `LoadPrefabContents()` / `UnloadPrefabContents()` - Edit prefab internals
- `InstantiatePrefab()` - Instantiate maintaining prefab connection
- `ApplyPrefabInstance()` / `RevertPrefabInstance()` - Override management
- `GetPrefabAssetType()` - Query prefab status

**EditorSceneManager** (`UnityEditor.SceneManagement.EditorSceneManager`):
- `OpenScene()` / `CloseScene()` - Scene management
- `NewScene()` - Create new scene
- `SaveScene()` / `SaveOpenScenes()` - Persist scenes
- `GetActiveScene()` / `SetActiveScene()` - Active scene control
- Extends runtime SceneManager with Editor-specific logic

**EditorApplication** (`UnityEditor.EditorApplication`):
- `isPlaying` / `isPaused` - Play mode control
- `isCompiling` - Check compilation status
- `isUpdating` - Check AssetDatabase refresh
- `ExecuteMenuItem()` - Programmatically invoke menu items
- `EnterPlaymode()` / `ExitPlaymode()` - Play mode transitions
- `update` event - Per-frame editor callback
- `delayCall` - Execute after current event processing

**EditorWindow** (`UnityEditor.EditorWindow`):
- Base class for custom editor panels
- `GetWindow<T>()` - Open/focus window
- `OnGUI()` - ImGui-style rendering
- Dockable, resizable, custom toolbar support

**CompilationPipeline** (`UnityEditor.Compilation.CompilationPipeline`):
- `RequestScriptCompilation()` - Force recompilation
- `assemblyCompilationFinished` event - Compilation callbacks
- `GetAssemblies()` - List project assemblies
- Essential for code generation workflows (write file -> recompile -> verify)

### 3.2 Undo System

**Undo** (`UnityEditor.Undo`):
- `RecordObject(obj, name)` - Record property changes (most common)
- `RegisterCompleteObjectUndo(obj, name)` - Snapshot entire object state
- `RegisterFullObjectHierarchyUndo(obj, name)` - Snapshot hierarchy
- `RegisterCreatedObjectUndo(obj, name)` - Track new object creation
- `DestroyObjectImmediate(obj)` - Undoable destruction
- `AddComponent<T>(go)` - Undoable component addition
- `SetTransformParent(transform, parent, name)` - Undoable reparenting
- `SetCurrentGroupName(name)` - Name undo groups for UI
- `IncrementCurrentGroup()` - Separate undo operations
- **Critical for editor tools**: All modifications should go through Undo for proper editor integration

### 3.3 MenuItem Attribute

```csharp
[MenuItem("Tools/My Tool")]
static void MyToolMethod() { /* ... */ }

[MenuItem("GameObject/Create Custom Object", false, 10)]
static void CreateCustom() { /* ... */ }
```

- Adds items to Unity menus
- Priority parameter controls ordering
- Validation methods with `true` second parameter
- `EditorApplication.ExecuteMenuItem()` can invoke programmatically

---

## 4. Programmatic Unity Control

### 4.1 Creating and Modifying GameObjects

```csharp
// Create
var go = new GameObject("MyObject");
Undo.RegisterCreatedObjectUndo(go, "Create MyObject");

// Add components
var rb = Undo.AddComponent<Rigidbody>(go);
var renderer = Undo.AddComponent<MeshRenderer>(go);

// Modify transform
Undo.RecordObject(go.transform, "Move Object");
go.transform.position = new Vector3(1, 2, 3);
go.transform.rotation = Quaternion.Euler(0, 90, 0);
go.transform.localScale = Vector3.one * 2;

// Set properties
go.tag = "Player";
go.layer = LayerMask.NameToLayer("Default");
go.isStatic = true;

// Hierarchy
Undo.SetTransformParent(child.transform, parent.transform, "Reparent");

// Destroy
Undo.DestroyObjectImmediate(go);
```

### 4.2 Generating C# Scripts

```csharp
string scriptContent = @"using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private float speed = 5f;
    [SerializeField] private float jumpForce = 10f;

    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        float h = Input.GetAxis(""Horizontal"");
        float v = Input.GetAxis(""Vertical"");
        rb.velocity = new Vector3(h * speed, rb.velocity.y, v * speed);
    }
}";

string path = "Assets/Scripts/PlayerController.cs";
System.IO.File.WriteAllText(path, scriptContent);
AssetDatabase.ImportAsset(path);
// Wait for compilation...
CompilationPipeline.RequestScriptCompilation();
```

### 4.3 Managing Scenes

```csharp
// Create new scene
var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

// Save scene
EditorSceneManager.SaveScene(scene, "Assets/Scenes/MyScene.unity");

// Load additively
EditorSceneManager.OpenScene("Assets/Scenes/Level2.unity", OpenSceneMode.Additive);

// Set active
SceneManager.SetActiveScene(scene);
```

### 4.4 Asset Import and Configuration

```csharp
// Create material
var material = new Material(Shader.Find("Standard"));
material.color = Color.red;
AssetDatabase.CreateAsset(material, "Assets/Materials/RedMaterial.mat");

// Create ScriptableObject
var config = ScriptableObject.CreateInstance<GameConfig>();
config.playerSpeed = 5f;
AssetDatabase.CreateAsset(config, "Assets/Config/GameConfig.asset");

// Import texture settings
var importer = AssetImporter.GetAtPath("Assets/Textures/sprite.png") as TextureImporter;
importer.textureType = TextureImporterType.Sprite;
importer.spritePixelsPerUnit = 32;
importer.SaveAndReimport();
```

### 4.5 Project Settings

```csharp
// Player settings
PlayerSettings.companyName = "MyCompany";
PlayerSettings.productName = "MyGame";
PlayerSettings.bundleVersion = "1.0.0";

// Quality settings
QualitySettings.SetQualityLevel(2);

// Physics
Physics.gravity = new Vector3(0, -9.81f, 0);

// Tags and layers
// Use SerializedObject with TagManager asset for programmatic control
```

---

## 5. Communication Bridge Approaches

### 5.1 Approach Comparison

| Approach | Latency | Complexity | Cross-Platform | Reliability |
|----------|---------|------------|----------------|-------------|
| TCP Socket | Low | Medium | Yes | High |
| WebSocket | Low | Medium | Yes | High |
| Named Pipes | Very Low | Medium | Windows/macOS | High |
| HTTP REST | Medium | Low | Yes | High |
| Unity CLI args | N/A | Low | Yes | Limited |

### 5.2 WebSocket (Recommended - Used by CoderGamester & IvanMurzak)

**Advantages**:
- Bidirectional, full-duplex communication
- Low overhead after handshake
- Native C# support via `System.Net.WebSockets`
- Well-understood protocol, many libraries
- Works across processes on same machine
- Can expose to LAN if needed

**Unity-side Implementation Pattern**:
```csharp
// Simplified WebSocket server in Unity Editor
using System.Net.WebSockets;
using System.Net;

public class UnityWebSocketServer
{
    private HttpListener listener;

    public async void Start(int port = 8090)
    {
        listener = new HttpListener();
        listener.Prefixes.Add($"http://localhost:{port}/");
        listener.Start();

        while (true)
        {
            var context = await listener.GetContextAsync();
            if (context.Request.IsWebSocketRequest)
            {
                var wsContext = await context.AcceptWebSocketAsync(null);
                _ = HandleClient(wsContext.WebSocket);
            }
        }
    }

    private async Task HandleClient(WebSocket ws)
    {
        var buffer = new byte[4096];
        while (ws.State == WebSocketState.Open)
        {
            var result = await ws.ReceiveAsync(buffer, CancellationToken.None);
            var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
            // Parse JSON-RPC, dispatch to main thread, return result
        }
    }
}
```

### 5.3 TCP Socket

**Advantages**:
- Lowest-level, most control
- Minimal overhead
- `System.Net.Sockets.TcpListener` / `TcpClient` available in Unity

**Use Case**: When WebSocket overhead is unnecessary and both sides are local

### 5.4 Named Pipes

**Library**: https://github.com/starburst997/Unity.IPC.NamedPipes
**Advantages**: Fastest IPC on same machine, kernel-level routing
**Disadvantages**: Platform-specific behavior, less portable

### 5.5 HTTP REST

**Advantages**: Simplest to implement, stateless, easy debugging
**Disadvantages**: Higher latency per request, no server push
**Use Case**: CoplayDev uses this approach; works well with batch operations

### 5.6 Main Thread Constraint

Unity's API is **not thread-safe**. All Unity Engine API calls must execute on the main thread. Solutions:

1. **EditorApplication.update callback**: Queue actions and execute in update loop
2. **SynchronizationContext**: Post work to Unity's synchronization context
3. **Custom dispatcher**: Thread-safe queue with main-thread consumer (most common in MCP implementations)

```csharp
// Common pattern: Main thread dispatcher
public class MainThreadDispatcher
{
    private static readonly ConcurrentQueue<Action> queue = new();

    [InitializeOnLoadMethod]
    static void Init()
    {
        EditorApplication.update += ProcessQueue;
    }

    public static Task<T> RunOnMainThread<T>(Func<T> action)
    {
        var tcs = new TaskCompletionSource<T>();
        queue.Enqueue(() => {
            try { tcs.SetResult(action()); }
            catch (Exception e) { tcs.SetException(e); }
        });
        return tcs.Task;
    }

    private static void ProcessQueue()
    {
        while (queue.TryDequeue(out var action))
            action();
    }
}
```

---

## 6. C# Code Generation Patterns

### 6.1 String-Based Generation (Simplest)

```csharp
public static string GenerateMonoBehaviour(string className, List<FieldDef> fields)
{
    var sb = new StringBuilder();
    sb.AppendLine("using UnityEngine;");
    sb.AppendLine();
    sb.AppendLine($"public class {className} : MonoBehaviour");
    sb.AppendLine("{");

    foreach (var field in fields)
    {
        sb.AppendLine($"    [SerializeField] private {field.Type} {field.Name};");
    }

    sb.AppendLine();
    sb.AppendLine("    void Start() { }");
    sb.AppendLine("    void Update() { }");
    sb.AppendLine("}");

    return sb.ToString();
}
```

### 6.2 Template-Based Generation

Unity supports script templates in `Assets/ScriptTemplates/`:
- `#SCRIPTNAME#` - Replaced with filename
- `#NOTRIM#` - Preserves whitespace
- Custom templates placed in `Assets/ScriptTemplates/` directory

### 6.3 Roslyn Source Generators

- Available in Unity 2021.3+ via `Microsoft.CodeAnalysis`
- Compile-time code generation
- Used by IvanMurzak/Unity-MCP for script validation
- More complex but type-safe

### 6.4 Common Script Templates Needed

**MonoBehaviour**: Player controllers, enemy AI, game managers, UI controllers
**ScriptableObject**: Game config, item databases, ability definitions, dialogue data
**Editor Scripts**: Custom inspectors, property drawers, editor windows
**Interfaces**: IInteractable, IDamageable, IPoolable

### 6.5 Best Practices

- Always use `AssetDatabase.ImportAsset()` after writing files
- Call `CompilationPipeline.RequestScriptCompilation()` to trigger recompilation
- Wait for `EditorApplication.isCompiling` to become false before using new types
- Use `AssetDatabase.GenerateUniqueAssetPath()` to avoid overwrites
- Validate generated code with Roslyn before writing (IvanMurzak approach)

---

## 7. Unity Package Manager (UPM)

### 7.1 Package Structure

```
com.company.package-name/
├── package.json                    # Package manifest (required)
├── README.md                       # Documentation
├── CHANGELOG.md                    # Version history
├── LICENSE.md                      # License
├── Editor/                         # Editor-only code
│   ├── com.company.package.editor.asmdef
│   └── *.cs
├── Runtime/                        # Runtime code
│   ├── com.company.package.runtime.asmdef
│   └── *.cs
├── Tests/                          # Test assemblies
│   ├── Editor/
│   │   └── com.company.package.editor.tests.asmdef
│   └── Runtime/
│       └── com.company.package.runtime.tests.asmdef
├── Samples~/                       # Optional samples (~ hides from Unity)
├── Documentation~/                 # Optional docs
└── Server~/                        # External server code (hidden from Unity)
```

### 7.2 Package Manifest (package.json)

```json
{
    "name": "com.company.unity-mcp",
    "version": "1.0.0",
    "displayName": "Unity MCP Server",
    "description": "MCP server for AI-assisted Unity development",
    "unity": "2022.3",
    "unityRelease": "0f1",
    "dependencies": {},
    "keywords": ["mcp", "ai", "editor", "tools"],
    "author": {
        "name": "Author Name",
        "email": "author@example.com",
        "url": "https://example.com"
    },
    "type": "tool",
    "samples": [
        {
            "displayName": "Basic Setup",
            "description": "Basic MCP server configuration",
            "path": "Samples~/BasicSetup"
        }
    ]
}
```

### 7.3 Assembly Definition Files (.asmdef)

**Editor Assembly** (Editor/com.company.package.editor.asmdef):
```json
{
    "name": "com.company.package.editor",
    "rootNamespace": "Company.Package.Editor",
    "references": ["com.company.package.runtime"],
    "includePlatforms": ["Editor"],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "autoReferenced": true
}
```

**Runtime Assembly** (Runtime/com.company.package.runtime.asmdef):
```json
{
    "name": "com.company.package.runtime",
    "rootNamespace": "Company.Package.Runtime",
    "references": [],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "autoReferenced": true
}
```

### 7.4 Installation Methods

1. **Git URL**: `Window > Package Manager > + > Add package from git URL`
   - URL: `https://github.com/user/repo.git`
   - Specific path: `https://github.com/user/repo.git?path=/PackageFolder`

2. **OpenUPM**: `openupm add com.company.package-name`

3. **Local disk**: `Add package from disk` -> select `package.json`

4. **Tarball**: `Add package from tarball` -> select `.tgz` file

5. **manifest.json** (direct edit):
   ```json
   {
     "dependencies": {
       "com.company.package": "https://github.com/user/repo.git"
     }
   }
   ```

### 7.5 Server~ Convention

The `~` suffix in folder names (e.g., `Server~`) tells Unity to **ignore** the folder during import. This is the standard pattern for including non-Unity code (Node.js, Python servers) inside a UPM package without Unity attempting to compile or import it.

---

## 8. Unity Version Considerations

### 8.1 Unity 6 (6000.x) vs Unity 2022.3 LTS

**Unity 6** (released 2024):
- Internal version: 6000.0.x
- New rendering pipeline improvements
- Updated UI Toolkit
- `ExecuteDefaultAction` / `ExecuteDefaultActionAtTarget` deprecated on VisualElement
- Reorganized Assets/Create menu
- Addressables 2.x (breaking change from 1.x)
- LightingSettings properties changed to float
- Better AI/ML integration support

**Unity 2022.3 LTS** (supported until mid-2025):
- Last traditional version numbering
- Addressables 1.x
- Stable, widely adopted
- Most third-party packages target this
- Roslyn analyzers and source generators supported

### 8.2 Compatibility Strategy

**Target Unity 2022.3 LTS as minimum** for maximum adoption:
- Core Editor APIs (AssetDatabase, PrefabUtility, EditorSceneManager, Undo) are stable across both versions
- `System.Net.WebSockets` available in both
- CompilationPipeline API unchanged
- EditorApplication API unchanged
- Main breaking changes are in rendering pipelines and UI Toolkit, which MCP servers typically don't touch

**Conditional compilation** for version-specific features:
```csharp
#if UNITY_6000_0_OR_NEWER
    // Unity 6+ specific code
#else
    // Unity 2022.3 fallback
#endif
```

### 8.3 API Stability Assessment for MCP Use

| API | Stability | Notes |
|-----|-----------|-------|
| AssetDatabase | Stable | Core API, minimal changes |
| PrefabUtility | Stable | Well-established |
| EditorSceneManager | Stable | Minor additions only |
| Undo | Stable | No breaking changes |
| EditorApplication | Stable | New events added, none removed |
| CompilationPipeline | Stable | RequestScriptCompilation unchanged |
| System.Net.WebSockets | Stable | .NET Standard 2.1 in both |
| EditorWindow | Stable | ImGui-based, long-standing |
| SerializedObject/Property | Stable | Core serialization system |

---

## 9. Technical Recommendations

### 9.1 Architecture Recommendation

**Hybrid approach inspired by all three major implementations**:

1. **C# WebSocket Server inside Unity Editor** (from CoderGamester):
   - WebSocket is the best balance of performance, cross-platform support, and bidirectional communication
   - Runs as an Editor-only component using `[InitializeOnLoad]` or Editor window
   - Listens on configurable localhost port (default: 8090)
   - Handles main-thread dispatch via `EditorApplication.update` queue

2. **External MCP Server Process** (hybrid):
   - **TypeScript/Node.js** for MCP protocol handling (best ecosystem support, official SDK is most mature)
   - OR **C#** if wanting to keep the entire stack in one language (IvanMurzak approach)
   - Connects to Unity WebSocket as a client
   - Exposes stdio transport for IDE integration (Claude Code, Cursor, etc.)

3. **Attribute-based tool registration** (from IvanMurzak):
   - Most developer-friendly extensibility pattern
   - Automatic tool discovery via reflection
   - Clean separation of tool logic

### 9.2 Recommended Tool Set (MVP)

**Phase 1 - Core (Must Have)**:
- `get_scene_hierarchy` - Read current scene structure
- `create_gameobject` - Create with optional components
- `modify_gameobject` - Update transform, properties
- `add_component` - Add components to GameObjects
- `modify_component` - Update component fields via SerializedProperty
- `create_script` - Generate C# MonoBehaviour/ScriptableObject files
- `read_script` - Read existing script contents
- `get_console_logs` - Read Unity console output
- `execute_menu_item` - Invoke any menu command

**Phase 2 - Extended**:
- `create_prefab` / `modify_prefab` - Prefab workflow
- `create_material` / `modify_material` - Material management
- `manage_scene` - Create/load/save scenes
- `run_tests` - Execute Test Runner
- `import_asset` - Import external assets
- `get_project_structure` - Asset database overview
- `batch_execute` - Multiple operations in one call

**Phase 3 - Advanced**:
- `generate_editor_script` - Custom inspector/window generation
- `configure_project_settings` - Player, quality, physics settings
- `manage_packages` - UPM package operations
- `capture_screenshot` - Scene/game view screenshots
- `search_assets` - Find assets by type/name/label
- `validate_script` - Roslyn-based code validation

### 9.3 Transport Recommendation

**Primary**: stdio (for IDE/CLI integration - this is what Claude Code, Cursor, etc. use)
**Internal Bridge**: WebSocket (Unity <-> MCP Server)
**Future**: Streamable HTTP for remote/cloud deployments

### 9.4 Package Structure Recommendation

```
com.yourcompany.unity-mcp/
├── package.json                     # UPM manifest (unity: "2022.3")
├── Editor/
│   ├── com.yourcompany.unity-mcp.editor.asmdef
│   ├── Core/
│   │   ├── McpBridge.cs            # WebSocket server + message routing
│   │   ├── MainThreadDispatcher.cs  # Thread-safe Unity API access
│   │   └── McpServerWindow.cs      # EditorWindow for config/status
│   ├── Tools/
│   │   ├── SceneTools.cs           # Scene/hierarchy operations
│   │   ├── GameObjectTools.cs      # GameObject CRUD
│   │   ├── ComponentTools.cs       # Component management
│   │   ├── ScriptTools.cs          # Script generation/reading
│   │   ├── AssetTools.cs           # Asset database operations
│   │   └── ProjectTools.cs         # Project settings, packages
│   └── Resources/
│       ├── SceneHierarchyResource.cs
│       ├── ConsoleLogResource.cs
│       └── ProjectStructureResource.cs
├── Server~/                         # Hidden from Unity
│   ├── package.json                 # Node.js dependencies
│   ├── tsconfig.json
│   └── src/
│       ├── index.ts                 # MCP server entry point
│       ├── transport.ts             # stdio/SSE/HTTP transport
│       ├── unity-bridge.ts          # WebSocket client to Unity
│       ├── tools/                   # MCP tool definitions
│       └── resources/               # MCP resource definitions
├── README.md
└── CHANGELOG.md
```

### 9.5 Key Implementation Patterns

**1. Main Thread Safety**:
Every Unity API call MUST be dispatched to the main thread. Use a concurrent queue + `EditorApplication.update` pattern.

**2. Undo Integration**:
All modification operations should use `Undo.*` methods so users can revert AI-generated changes.

**3. Error Handling**:
Return structured JSON-RPC errors with meaningful messages. Unity operations can fail silently; always verify results.

**4. Compilation Awareness**:
After generating scripts, wait for compilation to complete before attempting to use new types. Monitor `EditorApplication.isCompiling`.

**5. Batch Operations**:
Support batching multiple operations in a single tool call for performance (CoplayDev reports 10-100x improvement).

**6. Resource Caching**:
Cache scene hierarchy and asset database queries; invalidate on relevant events (`EditorApplication.hierarchyChanged`, `AssetDatabase.importCompleted`).

### 9.6 Accessibility and Voice Integration Considerations

For the voice-based I/O use case:
- Tools should return concise, structured responses suitable for text-to-speech
- Error messages should be human-readable (not just error codes)
- Consider adding a `describe_scene` tool that provides natural language descriptions
- Tool names should be voice-friendly (clear, unambiguous when spoken)
- Support confirmation flows for destructive operations (delete, overwrite)

### 9.7 Security Considerations

- Bind WebSocket to localhost only by default
- Validate all incoming tool parameters
- Sanitize file paths to prevent directory traversal
- Rate-limit operations to prevent runaway AI loops
- Log all operations for auditability
- Never execute arbitrary code without explicit tool definition

---

## Sources

- [CoderGamester/mcp-unity](https://github.com/CoderGamester/mcp-unity) - Most mature Unity MCP implementation
- [IvanMurzak/Unity-MCP](https://github.com/IvanMurzak/Unity-MCP) - Most extensible, C#-native implementation
- [CoplayDev/unity-mcp](https://github.com/CoplayDev/unity-mcp) - Best performance, batch operations
- [NoSpoonLab/unity-mcp](https://github.com/NoSpoonLab/unity-mcp) - Alternative implementation
- [MCP Specification 2025-11-25](https://modelcontextprotocol.io/specification/2025-11-25) - Current protocol spec
- [MCP Server Development Guide](https://github.com/cyanheads/model-context-protocol-resources/blob/main/guides/mcp-server-development-guide.md) - Implementation reference
- [MCP Transports](https://modelcontextprotocol.io/legacy/concepts/transports) - Transport layer details
- [Unity PrefabUtility API](https://docs.unity3d.com/ScriptReference/PrefabUtility.html)
- [Unity EditorApplication API](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/EditorApplication.html)
- [Unity EditorSceneManager API](https://docs.unity3d.com/ScriptReference/SceneManagement.EditorSceneManager.html)
- [Unity CompilationPipeline API](https://docs.unity3d.com/ScriptReference/Compilation.CompilationPipeline.html)
- [Unity Undo API](https://docs.unity3d.com/ScriptReference/Undo.html)
- [Unity Package Development](https://docs.unity3d.com/Manual/CustomPackages.html)
- [Unity Package Manifest](https://docs.unity3d.com/Manual/upm-manifestPkg.html)
- [Unity Upgrade Guide to 6.0](https://docs.unity3d.com/6000.0/Documentation/Manual/UpgradeGuideUnity6.html)
- [Unity.IPC.NamedPipes](https://github.com/starburst997/Unity.IPC.NamedPipes) - Named pipes for Unity
- [MCP SSE Deprecation Analysis](https://blog.fka.dev/blog/2025-06-06-why-mcp-deprecated-sse-and-go-with-streamable-http/)
- [Deep Dive: CoderGamester Unity MCP](https://skywork.ai/skypage/en/codergamester-unity-mcp-server-guide/1979040252012306432)
- [IvanMurzak: How I Made Unity-MCP](https://levelup.gitconnected.com/how-i-made-unity-mcp-bridging-ai-and-game-development-4abaf4a84310)
