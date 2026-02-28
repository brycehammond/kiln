using System;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace DevFramework.MCP.Editor
{
    public class ReadScriptTool : ToolBase
    {
        public override string Name => "read_script";
        public override string Description => "Read the contents of a C# script by path or class name";

        public override async Task<JObject> Execute(JObject parameters)
        {
            return await MainThreadDispatcher.RunOnMainThread(() =>
            {
                var path = parameters["path"]?.ToString();
                var className = parameters["className"]?.ToString();

                if (string.IsNullOrEmpty(path) && string.IsNullOrEmpty(className))
                {
                    return Failure(
                        "Either 'path' or 'className' must be provided.",
                        "I need either a file path or a class name to find the script."
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

                // Read the file
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

                    var contents = File.ReadAllText(fullPath);
                    var lineCount = contents.Split('\n').Length;

                    var data = new JObject
                    {
                        ["path"] = path,
                        ["contents"] = contents,
                        ["lineCount"] = lineCount
                    };

                    return Success(
                        $"Read {path} ({lineCount} lines)",
                        $"I read the script at {path}. It has {lineCount} lines.",
                        data
                    );
                }
                catch (Exception ex)
                {
                    return Failure(
                        $"Error reading file: {ex.Message}",
                        $"Something went wrong reading the file. {ex.Message}"
                    );
                }
            });
        }
    }
}
