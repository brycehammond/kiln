using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Kiln.MCP.Editor
{
    public class SetTransformTool : ToolBase
    {
        public override string Name => "set_transform";
        public override string Description => "Set the position, rotation, and/or scale of an existing GameObject";

        public override async Task<JObject> Execute(JObject parameters)
        {
            return await MainThreadDispatcher.RunOnMainThread(() =>
            {
                var name = parameters["name"]?.ToString();
                if (string.IsNullOrEmpty(name))
                    return Failure("No name provided.", "I need a name to find the object.");

                var go = GameObject.Find(name);
                if (go == null)
                    return Failure($"GameObject '{name}' not found.", $"I couldn't find an object called {name}.");

                Undo.RecordObject(go.transform, "Set Transform");

                if (parameters["position"] is JObject pos)
                {
                    go.transform.position = new Vector3(
                        pos["x"]?.Value<float>() ?? 0,
                        pos["y"]?.Value<float>() ?? 0,
                        pos["z"]?.Value<float>() ?? 0
                    );
                }

                if (parameters["rotation"] is JObject rot)
                {
                    go.transform.eulerAngles = new Vector3(
                        rot["x"]?.Value<float>() ?? 0,
                        rot["y"]?.Value<float>() ?? 0,
                        rot["z"]?.Value<float>() ?? 0
                    );
                }

                if (parameters["scale"] is JObject scl)
                {
                    go.transform.localScale = new Vector3(
                        scl["x"]?.Value<float>() ?? 1,
                        scl["y"]?.Value<float>() ?? 1,
                        scl["z"]?.Value<float>() ?? 1
                    );
                }

                var p = go.transform.position;
                var r = go.transform.eulerAngles;
                var s = go.transform.localScale;

                var detail = $"Updated transform of '{name}': pos=({p.x}, {p.y}, {p.z}), rot=({r.x}, {r.y}, {r.z}), scale=({s.x}, {s.y}, {s.z})";
                var spoken = $"Updated the transform of {name}.";

                return Success(detail, spoken);
            });
        }
    }
}
