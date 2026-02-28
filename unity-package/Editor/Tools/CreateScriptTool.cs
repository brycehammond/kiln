using System;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

namespace Kiln.MCP.Editor
{
    public class CreateScriptTool : ToolBase
    {
        public override string Name => "create_script";
        public override string Description => "Create a new C# script file and optionally attach it to a GameObject";

        public override async Task<JObject> Execute(JObject parameters)
        {
            var scriptName = parameters["scriptName"]?.ToString();
            var scriptType = parameters["scriptType"]?.ToString() ?? "MonoBehaviour";
            var code = parameters["code"]?.ToString();
            var directory = parameters["directory"]?.ToString() ?? "Assets/Scripts";
            var attachTo = parameters["attachTo"]?.ToString();

            if (string.IsNullOrEmpty(scriptName))
                return Failure("Script name is required.", "I need a name for the script.");

            // Generate template code if none provided
            if (string.IsNullOrEmpty(code))
                code = GenerateTemplate(scriptName, scriptType);

            // Create the file on main thread (for AssetDatabase operations)
            return await MainThreadDispatcher.RunOnMainThread(() =>
            {
                try
                {
                    // Ensure directory exists
                    var fullDir = Path.Combine(Application.dataPath, "..", directory);
                    fullDir = Path.GetFullPath(fullDir);
                    if (!Directory.Exists(fullDir))
                        Directory.CreateDirectory(fullDir);

                    var filePath = Path.Combine(directory, $"{scriptName}.cs");
                    var fullPath = Path.Combine(Application.dataPath, "..", filePath);
                    fullPath = Path.GetFullPath(fullPath);

                    File.WriteAllText(fullPath, code);
                    AssetDatabase.ImportAsset(filePath);
                    AssetDatabase.Refresh();

                    var detail = $"Created script '{scriptName}.cs' at {filePath}";
                    var spoken = $"Created the {scriptName} script.";

                    // Attach to GameObject if specified
                    if (!string.IsNullOrEmpty(attachTo))
                    {
                        var go = GameObject.Find(attachTo);
                        if (go != null)
                        {
                            // We need to wait for compilation, so we'll try to add it
                            // Note: The script may not be compiled yet right after creation
                            var monoScript = AssetDatabase.LoadAssetAtPath<MonoScript>(filePath);
                            if (monoScript != null)
                            {
                                var scriptClass = monoScript.GetClass();
                                if (scriptClass != null && typeof(MonoBehaviour).IsAssignableFrom(scriptClass))
                                {
                                    Undo.AddComponent(go, scriptClass);
                                    detail += $", attached to '{attachTo}'";
                                    spoken += $" I attached it to {attachTo}.";
                                }
                                else
                                {
                                    detail += $". Note: could not attach yet — Unity may still be compiling.";
                                    spoken += $" Unity needs to compile it before I can attach it to {attachTo}. Try again in a moment.";
                                }
                            }
                            else
                            {
                                detail += $". Note: Script needs to compile before attaching to '{attachTo}'.";
                                spoken += $" Unity needs to compile it before I can attach it to {attachTo}. Try again in a moment.";
                            }
                        }
                        else
                        {
                            detail += $". Warning: could not find GameObject '{attachTo}'";
                            spoken += $" But I couldn't find an object called {attachTo} to attach it to.";
                        }
                    }

                    return Success(detail, spoken);
                }
                catch (Exception ex)
                {
                    return Failure(
                        $"Failed to create script: {ex.Message}",
                        $"Something went wrong creating the script. {ex.Message}"
                    );
                }
            });
        }

        private string GenerateTemplate(string className, string scriptType)
        {
            switch (scriptType)
            {
                case "ScriptableObject":
                    return $@"using UnityEngine;

[CreateAssetMenu(fileName = ""{className}"", menuName = ""Custom/{className}"")]
public class {className} : ScriptableObject
{{

}}
";
                case "EditorWindow":
                    return $@"using UnityEditor;
using UnityEngine;

public class {className} : EditorWindow
{{
    [MenuItem(""Window/{className}"")]
    public static void ShowWindow()
    {{
        GetWindow<{className}>(""{className}"");
    }}

    private void OnGUI()
    {{

    }}
}}
";
                case "Plain":
                    return $@"using System;

public class {className}
{{

}}
";
                case "MonoBehaviour":
                default:
                    return $@"using UnityEngine;

public class {className} : MonoBehaviour
{{
    void Start()
    {{

    }}

    void Update()
    {{

    }}
}}
";
            }
        }
    }
}
