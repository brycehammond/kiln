using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace Kiln.MCP.Editor
{
    public class DeleteGameObjectTool : ToolBase
    {
        public override string Name => "delete_gameobject";
        public override string Description => "Delete a GameObject from the scene (undoable)";

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

                var objectName = go.name;
                Undo.DestroyObjectImmediate(go);

                var detail = $"Deleted GameObject '{objectName}'";
                var spoken = $"Done! Deleted {objectName} from the scene.";
                return Success(detail, spoken);
            });
        }
    }
}
