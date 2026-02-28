using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UnityEditor;

namespace Kiln.MCP.Editor
{
    public class EnterPlayModeTool : ToolBase
    {
        public override string Name => "enter_play_mode";
        public override string Description => "Enter Play Mode in the Unity Editor";

        public override async Task<JObject> Execute(JObject parameters)
        {
            return await MainThreadDispatcher.RunOnMainThread(() =>
            {
                if (EditorApplication.isPlaying)
                {
                    return Failure(
                        "Already in Play Mode.",
                        "Unity is already in Play Mode."
                    );
                }

                EditorApplication.EnterPlaymode();

                return Success(
                    "Entered Play Mode.",
                    "Unity is now in Play Mode."
                );
            });
        }
    }
}
