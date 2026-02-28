using System;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace Kiln.MCP.Editor
{
    public class ImportAssetTool : ToolBase
    {
        public override string Name => "import_asset";
        public override string Description => "Copy a downloaded file into the Unity project and import it via AssetDatabase";

        public override async Task<JObject> Execute(JObject parameters)
        {
            var sourcePath = parameters["sourcePath"]?.ToString();
            var targetDirectory = parameters["targetDirectory"]?.ToString() ?? "Assets/Imports";
            var fileName = parameters["fileName"]?.ToString();

            if (string.IsNullOrEmpty(sourcePath))
                return Failure("sourcePath is required.", "I need a file path to import.");

            if (!File.Exists(sourcePath))
                return Failure($"Source file not found: {sourcePath}", "The downloaded file was not found.");

            if (string.IsNullOrEmpty(fileName))
                fileName = Path.GetFileName(sourcePath);

            return await MainThreadDispatcher.RunOnMainThread(() =>
            {
                try
                {
                    // Ensure target directory exists
                    var fullDir = Path.Combine(Application.dataPath, "..", targetDirectory);
                    fullDir = Path.GetFullPath(fullDir);
                    if (!Directory.Exists(fullDir))
                        Directory.CreateDirectory(fullDir);

                    var assetPath = Path.Combine(targetDirectory, fileName);
                    var fullPath = Path.Combine(Application.dataPath, "..", assetPath);
                    fullPath = Path.GetFullPath(fullPath);

                    // Copy file from temp to project
                    File.Copy(sourcePath, fullPath, overwrite: true);

                    // Import via AssetDatabase
                    AssetDatabase.ImportAsset(assetPath);
                    AssetDatabase.Refresh();

                    // Check if glTF support is needed
                    var ext = Path.GetExtension(fileName).ToLowerInvariant();
                    var warning = "";
                    if (ext == ".gltf" || ext == ".glb")
                    {
                        if (!IsPackageInstalled("com.unity.cloud.gltfast"))
                        {
                            warning = "\n\nNote: This is a glTF file. You may need to install the glTFast package " +
                                      "for Unity to import it correctly. Use add_package(identifier=\"com.unity.cloud.gltfast\").";
                        }
                    }

                    var detail = $"Imported '{fileName}' to {assetPath}{warning}";
                    var spoken = $"Imported {fileName} into the project.";

                    return Success(detail, spoken);
                }
                catch (Exception ex)
                {
                    return Failure(
                        $"Failed to import asset: {ex.Message}",
                        $"Something went wrong importing the file. {ex.Message}"
                    );
                }
            });
        }

        private static bool IsPackageInstalled(string packageName)
        {
            var manifestPath = Path.Combine(Application.dataPath, "..", "Packages", "manifest.json");
            if (!File.Exists(manifestPath)) return false;

            try
            {
                var json = File.ReadAllText(manifestPath);
                return json.Contains($"\"{packageName}\"");
            }
            catch
            {
                return false;
            }
        }
    }
}
