using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace Kiln.MCP.Editor
{
    public class FocusGameObjectTool : ToolBase
    {
        public override string Name => "focus_gameobject";
        public override string Description => "Select a GameObject and frame it in the Scene view";

        public override async Task<JObject> Execute(JObject parameters)
        {
            return await MainThreadDispatcher.RunOnMainThread(() =>
            {
                var name = parameters["name"]?.ToString();

                if (string.IsNullOrEmpty(name))
                {
                    return Failure(
                        "'name' is required.",
                        "I need the name of the object to focus."
                    );
                }

                var go = GameObject.Find(name);
                if (go == null)
                {
                    return Failure(
                        $"GameObject '{name}' not found.",
                        $"I couldn't find an object called {name}."
                    );
                }

                Selection.activeGameObject = go;
                SceneView.FrameLastActiveSceneView();

                return Success(
                    $"Focused on '{go.name}'.",
                    $"I selected and focused on {go.name} in the Scene view."
                );
            });
        }
    }
}
