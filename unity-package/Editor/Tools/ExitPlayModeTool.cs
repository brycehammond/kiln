using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UnityEditor;

namespace Kiln.MCP.Editor
{
    public class ExitPlayModeTool : ToolBase
    {
        public override string Name => "exit_play_mode";
        public override string Description => "Exit Play Mode in the Unity Editor";

        public override async Task<JObject> Execute(JObject parameters)
        {
            return await MainThreadDispatcher.RunOnMainThread(() =>
            {
                if (!EditorApplication.isPlaying)
                {
                    return Failure(
                        "Not in Play Mode.",
                        "Unity is not in Play Mode."
                    );
                }

                EditorApplication.ExitPlaymode();

                return Success(
                    "Exited Play Mode.",
                    "Unity has stopped Play Mode."
                );
            });
        }
    }
}
