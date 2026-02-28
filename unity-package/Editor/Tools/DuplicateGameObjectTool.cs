using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace Kiln.MCP.Editor
{
    public class DuplicateGameObjectTool : ToolBase
    {
        public override string Name => "duplicate_gameobject";
        public override string Description => "Duplicate an existing GameObject (undoable)";

        public override async Task<JObject> Execute(JObject parameters)
        {
            return await MainThreadDispatcher.RunOnMainThread(() =>
            {
                var name = parameters["name"]?.ToString();
                if (string.IsNullOrEmpty(name))
                    return Error("'name' is required");

                var go = GameObject.Find(name);
                if (go == null)
                    return Error($"GameObject '{name}' not found");

                var clone = Object.Instantiate(go);
                Undo.RegisterCreatedObjectUndo(clone, $"Duplicate {go.name}");

                // Preserve parent hierarchy
                if (go.transform.parent != null)
                {
                    Undo.SetTransformParent(clone.transform, go.transform.parent, $"Parent {clone.name}");
                }

                // Apply offset (default: 1 unit on X)
                if (parameters["offset"] is JObject off)
                {
                    clone.transform.position = go.transform.position + new Vector3(
                        off["x"]?.Value<float>() ?? 0,
                        off["y"]?.Value<float>() ?? 0,
                        off["z"]?.Value<float>() ?? 0
                    );
                }
                else
                {
                    clone.transform.position = go.transform.position + new Vector3(1, 0, 0);
                }

                // Rename
                var newName = parameters["newName"]?.ToString();
                if (!string.IsNullOrEmpty(newName))
                {
                    clone.name = newName;
                }
                else
                {
                    clone.name = $"{go.name} (Copy)";
                }

                var posStr = $"{clone.transform.position.x}, {clone.transform.position.y}, {clone.transform.position.z}";
                var detail = $"Duplicated '{go.name}' as '{clone.name}' at ({posStr})";
                var spoken = $"Done! Duplicated {go.name} as {clone.name} at position {posStr}.";
                return Success(detail, spoken);
            });
        }
    }
}
