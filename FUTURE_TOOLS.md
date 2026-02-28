# Future Kiln Tool Ideas

## Screenshot (ready to implement)

Capture the Game or Scene view and return it as an MCP image content block so Claude can see what it's building.

**Architecture:** Unity renders camera to RenderTexture → EncodeToPNG → base64 → MCP `{ type: 'image' }` content block.

**Files:**
- `unity-package/Editor/Tools/ScreenshotTool.cs` — render camera, encode PNG, return base64 in `data.imageBase64`
- `mcp-server/src/tools/screenshot.ts` — forward to Unity, return MCP image content block
- Register in `KilnServer.cs` and `index.ts`

**Key details:**
- Params: `view` ("game"|"scene", default "game"), `width`/`height` (64–768, default 512)
- Scene view: `SceneView.lastActiveSceneView.camera`
- Game view: `Camera.main` with fallback to `FindObjectOfType<Camera>()`
- Max 768px to keep base64 under the 1MB WebSocket limit
- First tool to use MCP's image content type
- Read-only, no Undo needed

---

## Other Ideas

### Scene Building
- **`modify_gameobject`** — move, rotate, scale, rename, or reparent objects by name
- **`delete_gameobject`** — remove objects (with confirmation)
- **`duplicate_gameobject`** — clone with optional offset

### Materials & Visuals
- **`create_material`** — set shader, color, texture, metallic/smoothness
- **`apply_material`** — assign a material to an existing object by name

### Playtesting
- **`enter_play_mode` / `exit_play_mode`** — let Claude test things in-editor
- **`read_console`** — pull recent logs/warnings/errors for debugging

### Script Editing
- **`edit_script`** — modify an existing script (not just create new ones)

### Scene Navigation
- **`focus_gameobject`** — frame an object in the Scene view

### Project Management
- **`list_assets`** — browse Assets/ by folder/type
