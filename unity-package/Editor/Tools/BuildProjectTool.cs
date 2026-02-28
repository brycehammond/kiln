using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Kiln.MCP.Editor
{
    public class BuildProjectTool : ToolBase
    {
        public override string Name => "build_project";
        public override string Description => "Build the Unity project for a target platform";

        private static readonly Dictionary<string, BuildTarget> TargetMap = new Dictionary<string, BuildTarget>(StringComparer.OrdinalIgnoreCase)
        {
            {"windows", BuildTarget.StandaloneWindows64},
            {"win", BuildTarget.StandaloneWindows64},
            {"win64", BuildTarget.StandaloneWindows64},
            {"mac", BuildTarget.StandaloneOSX},
            {"osx", BuildTarget.StandaloneOSX},
            {"macos", BuildTarget.StandaloneOSX},
            {"linux", BuildTarget.StandaloneLinux64},
            {"webgl", BuildTarget.WebGL},
            {"android", BuildTarget.Android},
            {"ios", BuildTarget.iOS},
        };

        public override async Task<JObject> Execute(JObject parameters)
        {
            return await MainThreadDispatcher.RunOnMainThread(() =>
            {
                // Resolve build target
                var targetStr = parameters["target"]?.ToString();
                BuildTarget target;

                if (!string.IsNullOrEmpty(targetStr))
                {
                    if (!TargetMap.TryGetValue(targetStr, out target))
                        return Failure($"Unknown build target '{targetStr}'. Use: Windows, Mac, Linux, WebGL, Android, or iOS.",
                            $"I don't recognize the build target {targetStr}.");
                }
                else
                {
                    target = EditorUserBuildSettings.activeBuildTarget;
                }

                // Gather enabled scenes
                var scenePaths = new List<string>();
                foreach (var scene in EditorBuildSettings.scenes)
                {
                    if (scene.enabled)
                        scenePaths.Add(scene.path);
                }

                if (scenePaths.Count == 0)
                    return Failure("No scenes are enabled in Build Settings.",
                        "There are no scenes enabled in the build settings. Add scenes first.");

                // Resolve output path
                var outputPath = parameters["outputPath"]?.ToString();
                if (string.IsNullOrEmpty(outputPath))
                {
                    var projectRoot = Path.GetDirectoryName(Application.dataPath);
                    outputPath = Path.Combine(projectRoot, "Builds", target.ToString());
                }

                Directory.CreateDirectory(outputPath);

                // Build
                var report = BuildPipeline.BuildPlayer(scenePaths.ToArray(), outputPath, target, BuildOptions.None);
                var summary = report.summary;

                if (summary.result == BuildResult.Succeeded)
                {
                    var detail = $"Build succeeded for {target}. Output: {outputPath}. " +
                                 $"Duration: {summary.totalTime.TotalSeconds:F1}s, Size: {summary.totalSize / (1024 * 1024):F1} MB, " +
                                 $"Warnings: {summary.totalWarnings}, Errors: {summary.totalErrors}.";
                    var spoken = $"Build succeeded for {target}. It took {summary.totalTime.TotalSeconds:F0} seconds.";
                    return Success(detail, spoken);
                }
                else
                {
                    var detail = $"Build failed for {target}. Result: {summary.result}. " +
                                 $"Errors: {summary.totalErrors}, Warnings: {summary.totalWarnings}.";
                    var spoken = $"The build for {target} failed with {summary.totalErrors} errors.";
                    return Failure(detail, spoken);
                }
            });
        }
    }
}
