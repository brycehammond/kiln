using System;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace Kiln.MCP.Editor
{
    public class EditScriptTool : ToolBase
    {
        public override string Name => "edit_script";
        public override string Description => "Replace the contents of an existing C# script";

        public override async Task<JObject> Execute(JObject parameters)
        {
            return await MainThreadDispatcher.RunOnMainThread(() =>
            {
                var path = parameters["path"]?.ToString();
                var className = parameters["className"]?.ToString();
                var code = parameters["code"]?.ToString();

                if (string.IsNullOrEmpty(path) && string.IsNullOrEmpty(className))
                {
                    return Failure(
                        "Either 'path' or 'className' must be provided.",
                        "I need either a file path or a class name to find the script."
                    );
                }

                if (string.IsNullOrEmpty(code))
                {
                    return Failure(
                        "'code' is required.",
                        "I need the replacement code for the script."
                    );
                }

                // Find by class name if no path given
                if (string.IsNullOrEmpty(path) && !string.IsNullOrEmpty(className))
                {
                    var guids = AssetDatabase.FindAssets($"t:MonoScript {className}");
                    foreach (var guid in guids)
                    {
                        var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                        var script = AssetDatabase.LoadAssetAtPath<MonoScript>(assetPath);
                        if (script != null && script.name == className)
                        {
                            path = assetPath;
                            break;
                        }
                    }

                    if (string.IsNullOrEmpty(path))
                    {
                        return Failure(
                            $"Could not find a script with class name '{className}'.",
                            $"I couldn't find a script called {className}."
                        );
                    }
                }

                try
                {
                    var fullPath = Path.Combine(Application.dataPath, "..", path);
                    fullPath = Path.GetFullPath(fullPath);

                    if (!File.Exists(fullPath))
                    {
                        return Failure(
                            $"File not found: {path}",
                            $"I couldn't find the file at {path}."
                        );
                    }

                    File.WriteAllText(fullPath, code);
                    AssetDatabase.ImportAsset(path);
                    AssetDatabase.Refresh();

                    var lineCount = code.Split('\n').Length;

                    var data = new JObject
                    {
                        ["path"] = path,
                        ["lineCount"] = lineCount
                    };

                    return Success(
                        $"Updated {path} ({lineCount} lines)",
                        $"I updated the script at {path}. It now has {lineCount} lines.",
                        data
                    );
                }
                catch (Exception ex)
                {
                    return Failure(
                        $"Error writing file: {ex.Message}",
                        $"Something went wrong writing the file. {ex.Message}"
                    );
                }
            });
        }
    }
}
