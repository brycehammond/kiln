using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Kiln.MCP.Editor
{
    public class GetProjectSummaryTool : ToolBase
    {
        public override string Name => "get_project_summary";
        public override string Description => "Get an overview of the current Unity project";

        public override async Task<JObject> Execute(JObject parameters)
        {
            return await MainThreadDispatcher.RunOnMainThread(() =>
            {
                var sb = new StringBuilder();

                // Project info
                sb.AppendLine($"Project: {Application.productName}");
                sb.AppendLine($"Unity: {Application.unityVersion}");
                sb.AppendLine();

                // Asset counts
                var scenes = AssetDatabase.FindAssets("t:Scene");
                var scripts = AssetDatabase.FindAssets("t:MonoScript");
                var prefabs = AssetDatabase.FindAssets("t:Prefab");
                var materials = AssetDatabase.FindAssets("t:Material");
                var sprites = AssetDatabase.FindAssets("t:Sprite");
                var tilemaps = AssetDatabase.FindAssets("t:Tile");

                sb.AppendLine("Assets:");
                sb.AppendLine($"  Scenes: {scenes.Length}");
                sb.AppendLine($"  Scripts: {scripts.Length}");
                sb.AppendLine($"  Prefabs: {prefabs.Length}");
                sb.AppendLine($"  Materials: {materials.Length}");
                sb.AppendLine($"  Sprites: {sprites.Length}");
                sb.AppendLine($"  Tiles: {tilemaps.Length}");
                sb.AppendLine();

                // Open scenes
                sb.AppendLine("Open scenes:");
                for (int i = 0; i < SceneManager.sceneCount; i++)
                {
                    var scene = SceneManager.GetSceneAt(i);
                    var roots = scene.GetRootGameObjects();
                    sb.AppendLine($"  {scene.name} ({roots.Length} root objects)");
                }
                sb.AppendLine();

                // Build spoken summary
                var activeScene = SceneManager.GetActiveScene();
                var rootCount = activeScene.GetRootGameObjects().Length;
                var spoken = $"The project {Application.productName} is running Unity {Application.unityVersion}. " +
                             $"It has {scripts.Length} scripts, {prefabs.Length} prefabs, {sprites.Length} sprites, and {scenes.Length} scenes. " +
                             $"The current scene is {activeScene.name} with {rootCount} root objects.";

                var data = new JObject
                {
                    ["projectName"] = Application.productName,
                    ["unityVersion"] = Application.unityVersion,
                    ["sceneCount"] = scenes.Length,
                    ["scriptCount"] = scripts.Length,
                    ["prefabCount"] = prefabs.Length,
                    ["materialCount"] = materials.Length,
                    ["spriteCount"] = sprites.Length,
                    ["tileCount"] = tilemaps.Length,
                    ["activeScene"] = activeScene.name,
                    ["rootObjectCount"] = rootCount
                };

                return Success(sb.ToString(), spoken, data);
            });
        }
    }
}
