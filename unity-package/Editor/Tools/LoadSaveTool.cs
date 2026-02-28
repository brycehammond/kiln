using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Kiln.MCP.Editor
{
    public class LoadSaveTool : ToolBase
    {
        public override string Name => "load_save";
        public override string Description => "Load a previously saved project state by name. Auto-saves current state first. If no name is given, loads the most recent save.";

        public override async Task<JObject> Execute(JObject parameters)
        {
            var name = parameters["name"]?.ToString();
            var manifest = SaveTool.LoadManifest();

            if (manifest.Count == 0)
                return Failure("No saves found.", "There are no saves to load.");

            // Find the target save
            JToken targetSave;
            if (string.IsNullOrEmpty(name))
            {
                targetSave = manifest.Last;
            }
            else
            {
                targetSave = manifest.FirstOrDefault(e =>
                    string.Equals(e["name"]?.ToString(), name, StringComparison.OrdinalIgnoreCase));
            }

            if (targetSave == null)
                return Failure($"Save '{name}' not found.", $"I couldn't find a save called {name}.");

            var targetName = targetSave["name"]?.ToString();
            var commitHash = targetSave["commitHash"]?.ToString();
            var scenePath = targetSave["scenePath"]?.ToString();

            // Auto-save current state first
            var autoSaveTool = new SaveTool();
            var autoSaveParams = new JObject
            {
                ["name"] = "autosave before load",
                ["description"] = $"Automatic save before loading '{targetName}'"
            };
            var autoSaveResult = await autoSaveTool.Execute(autoSaveParams);

            if (autoSaveResult["success"]?.Value<bool>() != true)
            {
                Debug.LogWarning($"[Kiln] Auto-save before load failed: {autoSaveResult["message"]}");
                // Continue anyway - loading is more important
            }

            // Restore git state if we have a valid commit hash
            if (!string.IsNullOrEmpty(commitHash) && commitHash != "none")
            {
                try
                {
                    var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
                    SaveTool.RunGit($"checkout {commitHash} -- .", projectRoot);
                }
                catch (Exception ex)
                {
                    return Failure(
                        $"Failed to restore git state: {ex.Message}",
                        "Something went wrong restoring the saved files."
                    );
                }
            }

            // Open the saved scene on main thread
            var loadResult = await MainThreadDispatcher.RunOnMainThread(() =>
            {
                try
                {
                    AssetDatabase.Refresh();

                    if (!string.IsNullOrEmpty(scenePath) && File.Exists(
                        Path.Combine(Application.dataPath, "..", scenePath)))
                    {
                        EditorSceneManager.OpenScene(scenePath);
                    }

                    return (true, "");
                }
                catch (Exception ex)
                {
                    return (false, ex.Message);
                }
            });

            if (!loadResult.Item1)
                return Failure($"Failed to open scene: {loadResult.Item2}",
                    "Restored the files but had trouble opening the scene.");

            return Success(
                $"Loaded save '{targetName}' (commit: {commitHash})",
                $"Loaded {targetName}. I auto-saved your current state first just in case.",
                new JObject { ["name"] = targetName, ["commitHash"] = commitHash, ["scenePath"] = scenePath }
            );
        }
    }
}
