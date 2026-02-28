using System;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace Kiln.MCP.Editor
{
    public class ScreenshotTool : ToolBase
    {
        public override string Name => "screenshot";
        public override string Description => "Capture a screenshot of the Game or Scene view";

        private const int MaxDimension = 768;

        public override async Task<JObject> Execute(JObject parameters)
        {
            return await MainThreadDispatcher.RunOnMainThread(() =>
            {
                var view = parameters["view"]?.ToString() ?? "game";
                var width = parameters["width"]?.Value<int>() ?? 512;
                var height = parameters["height"]?.Value<int>() ?? 512;

                width = Mathf.Clamp(width, 64, MaxDimension);
                height = Mathf.Clamp(height, 64, MaxDimension);

                Camera camera;
                string viewLabel;

                if (string.Equals(view, "scene", StringComparison.OrdinalIgnoreCase))
                {
                    var sceneView = SceneView.lastActiveSceneView;
                    if (sceneView == null)
                    {
                        return Failure(
                            "No active Scene view found. Open a Scene view in the editor.",
                            "I could not find an open Scene view."
                        );
                    }
                    camera = sceneView.camera;
                    viewLabel = "Scene";
                }
                else
                {
                    camera = Camera.main;
                    if (camera == null)
                        camera = UnityEngine.Object.FindObjectOfType<Camera>();
                    if (camera == null)
                    {
                        return Failure(
                            "No camera found in the scene. Add a Camera to capture a screenshot.",
                            "There is no camera in the scene to take a picture with."
                        );
                    }
                    viewLabel = "Game";
                }

                RenderTexture rt = null;
                Texture2D tex = null;

                try
                {
                    rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
                    rt.Create();

                    var previousTarget = camera.targetTexture;
                    camera.targetTexture = rt;
                    camera.Render();
                    camera.targetTexture = previousTarget;

                    var previousActive = RenderTexture.active;
                    RenderTexture.active = rt;
                    tex = new Texture2D(width, height, TextureFormat.RGB24, false);
                    tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                    tex.Apply();
                    RenderTexture.active = previousActive;

                    var pngBytes = tex.EncodeToPNG();
                    var base64 = Convert.ToBase64String(pngBytes);

                    var data = new JObject
                    {
                        ["imageBase64"] = base64,
                        ["mimeType"] = "image/png",
                        ["width"] = width,
                        ["height"] = height
                    };

                    var sizeKB = pngBytes.Length / 1024;
                    return Success(
                        $"Captured {width}x{height} {viewLabel} view screenshot ({sizeKB} KB).",
                        $"Here is a {width} by {height} screenshot of the {viewLabel} view.",
                        data
                    );
                }
                catch (Exception ex)
                {
                    return Failure(
                        $"Screenshot failed: {ex.Message}",
                        $"Something went wrong taking the screenshot. {ex.Message}"
                    );
                }
                finally
                {
                    if (tex != null)
                        UnityEngine.Object.DestroyImmediate(tex);
                    if (rt != null)
                    {
                        rt.Release();
                        UnityEngine.Object.DestroyImmediate(rt);
                    }
                }
            });
        }
    }
}
