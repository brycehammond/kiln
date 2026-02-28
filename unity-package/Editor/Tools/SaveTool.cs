using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;

namespace Kiln.MCP.Editor
{
    public class SaveTool : ToolBase
    {
        public override string Name => "save";
        public override string Description => "Save the current scene and project state with a name and optional description. Creates a git commit as a restore point.";

        public override async Task<JObject> Execute(JObject parameters)
        {
            var name = parameters["name"]?.ToString();
            var description = parameters["description"]?.ToString();

            // Determine save name
            if (string.IsNullOrEmpty(name))
            {
                var manifest = LoadManifest();
                var counter = manifest.Count(e => e["name"]?.ToString()?.StartsWith("Save ") == true);
                name = $"Save {counter + 1}";
            }

            var sanitizedName = SanitizeFileName(name);
            var savePath = $"Assets/_KilnSaves/{sanitizedName}.unity";

            // Save scene on main thread
            var sceneResult = await MainThreadDispatcher.RunOnMainThread(() =>
            {
                try
                {
                    // Ensure _KilnSaves directory exists
                    var saveDir = Path.Combine(Application.dataPath, "_KilnSaves");
                    if (!Directory.Exists(saveDir))
                        Directory.CreateDirectory(saveDir);

                    // Save the active scene to the save path
                    var scene = SceneManager.GetActiveScene();
                    var success = EditorSceneManager.SaveScene(scene, savePath);
                    if (!success)
                        return (false, "Failed to save scene.", "");

                    AssetDatabase.Refresh();
                    return (true, "", scene.name);
                }
                catch (Exception ex)
                {
                    return (false, ex.Message, "");
                }
            });

            if (!sceneResult.Item1)
                return Failure($"Scene save failed: {sceneResult.Item2}", "Something went wrong saving the scene.");

            // Git commit (off main thread is fine)
            string commitHash;
            try
            {
                var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
                RunGit("add -A", projectRoot);
                RunGit($"commit -m \"Kiln save: {name}\"", projectRoot);
                commitHash = RunGit("rev-parse HEAD", projectRoot).Trim();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Kiln] Git commit failed: {ex.Message}");
                commitHash = "none";
            }

            // Update manifest
            try
            {
                var manifest = LoadManifest();
                var entry = new JObject
                {
                    ["name"] = name,
                    ["description"] = description ?? "",
                    ["timestamp"] = DateTime.UtcNow.ToString("o"),
                    ["scenePath"] = savePath,
                    ["commitHash"] = commitHash
                };
                manifest.Add(entry);
                SaveManifest(manifest);
            }
            catch (Exception ex)
            {
                return Failure($"Saved scene but failed to update manifest: {ex.Message}",
                    "Saved the scene but had trouble updating the save list.");
            }

            var spoken = string.IsNullOrEmpty(parameters["name"]?.ToString()) ? "Saved." : $"Saved as {name}.";
            return Success(
                $"Saved '{name}' at {savePath} (commit: {commitHash})",
                spoken,
                new JObject { ["name"] = name, ["commitHash"] = commitHash, ["scenePath"] = savePath }
            );
        }

        internal static JArray LoadManifest()
        {
            var manifestPath = GetManifestPath();
            if (File.Exists(manifestPath))
            {
                var json = File.ReadAllText(manifestPath);
                return JArray.Parse(json);
            }
            return new JArray();
        }

        internal static void SaveManifest(JArray manifest)
        {
            var manifestPath = GetManifestPath();
            var dir = Path.GetDirectoryName(manifestPath);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            File.WriteAllText(manifestPath, manifest.ToString(Formatting.Indented));
        }

        internal static string GetManifestPath()
        {
            return Path.Combine(Application.dataPath, "_KilnSaves", "manifest.json");
        }

        internal static string RunGit(string arguments, string workingDirectory)
        {
            var psi = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = arguments,
                WorkingDirectory = workingDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (var process = Process.Start(psi))
            {
                var output = process.StandardOutput.ReadToEnd();
                var error = process.StandardError.ReadToEnd();
                process.WaitForExit();

                if (process.ExitCode != 0)
                    throw new Exception($"git {arguments} failed (exit {process.ExitCode}): {error}");

                return output;
            }
        }

        private static string SanitizeFileName(string name)
        {
            var invalid = Path.GetInvalidFileNameChars();
            var sanitized = new string(name.Select(c => Array.IndexOf(invalid, c) >= 0 ? '_' : c).ToArray());
            return sanitized;
        }
    }
}
