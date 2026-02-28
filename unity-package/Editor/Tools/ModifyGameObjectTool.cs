using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace Kiln.MCP.Editor
{
    public class ModifyGameObjectTool : ToolBase
    {
        public override string Name => "modify_gameobject";
        public override string Description => "Modify an existing GameObject's transform, name, active state, color, or parent";

        private static readonly Dictionary<string, Color> NamedColors = new Dictionary<string, Color>(StringComparer.OrdinalIgnoreCase)
        {
            {"red", Color.red}, {"blue", Color.blue}, {"green", Color.green},
            {"yellow", Color.yellow}, {"white", Color.white}, {"black", Color.black},
            {"cyan", Color.cyan}, {"magenta", Color.magenta}, {"gray", Color.gray},
            {"grey", Color.gray},
            {"orange", new Color(1f, 0.647f, 0f)},
            {"purple", new Color(0.5f, 0f, 0.5f)},
            {"brown", new Color(0.647f, 0.165f, 0.165f)},
            {"pink", new Color(1f, 0.753f, 0.796f)}
        };

        public override async Task<JObject> Execute(JObject parameters)
        {
            return await MainThreadDispatcher.RunOnMainThread(() =>
            {
                var name = parameters["name"]?.ToString();
                if (string.IsNullOrEmpty(name))
                    return Error("'name' is required");

                var go = GameObject.Find(name);
                if (go == null)
                    return Error($"GameObject '{name}' not found");

                var changes = new List<string>();

                // Position
                if (parameters["position"] is JObject pos)
                {
                    Undo.RecordObject(go.transform, $"Move {name}");
                    go.transform.position = new Vector3(
                        pos["x"]?.Value<float>() ?? 0,
                        pos["y"]?.Value<float>() ?? 0,
                        pos["z"]?.Value<float>() ?? 0
                    );
                    changes.Add($"position=({go.transform.position.x}, {go.transform.position.y}, {go.transform.position.z})");
                }

                // Rotation
                if (parameters["rotation"] is JObject rot)
                {
                    Undo.RecordObject(go.transform, $"Rotate {name}");
                    go.transform.eulerAngles = new Vector3(
                        rot["x"]?.Value<float>() ?? 0,
                        rot["y"]?.Value<float>() ?? 0,
                        rot["z"]?.Value<float>() ?? 0
                    );
                    changes.Add($"rotation=({go.transform.eulerAngles.x}, {go.transform.eulerAngles.y}, {go.transform.eulerAngles.z})");
                }

                // Scale
                if (parameters["scale"] is JObject scl)
                {
                    Undo.RecordObject(go.transform, $"Scale {name}");
                    go.transform.localScale = new Vector3(
                        scl["x"]?.Value<float>() ?? 1,
                        scl["y"]?.Value<float>() ?? 1,
                        scl["z"]?.Value<float>() ?? 1
                    );
                    changes.Add($"scale=({go.transform.localScale.x}, {go.transform.localScale.y}, {go.transform.localScale.z})");
                }

                // Active state
                if (parameters["active"] != null)
                {
                    var active = parameters["active"].Value<bool>();
                    Undo.RecordObject(go, $"Set {name} active={active}");
                    go.SetActive(active);
                    changes.Add($"active={active}");
                }

                // Color
                var colorStr = parameters["color"]?.ToString();
                if (!string.IsNullOrEmpty(colorStr))
                {
                    var color = ParseColor(colorStr);
                    ApplyColor(go, color);
                    changes.Add($"color={colorStr}");
                }

                // Reparent
                if (parameters["parentPath"] != null)
                {
                    var parentPath = parameters["parentPath"].ToString();
                    if (string.IsNullOrEmpty(parentPath))
                    {
                        Undo.SetTransformParent(go.transform, null, $"Unparent {name}");
                        changes.Add("unparented to root");
                    }
                    else
                    {
                        var parent = GameObject.Find(parentPath);
                        if (parent != null)
                        {
                            Undo.SetTransformParent(go.transform, parent.transform, $"Reparent {name}");
                            changes.Add($"parent={parentPath}");
                        }
                        else
                        {
                            return Error($"Parent '{parentPath}' not found");
                        }
                    }
                }

                // Rename (do last so earlier Undo descriptions use the original name)
                var newName = parameters["newName"]?.ToString();
                if (!string.IsNullOrEmpty(newName))
                {
                    Undo.RecordObject(go, $"Rename {name} to {newName}");
                    go.name = newName;
                    changes.Add($"renamed to '{newName}'");
                }

                if (changes.Count == 0)
                    return Error("No modifications specified");

                var detail = $"Modified '{name}': {string.Join(", ", changes)}";
                var spoken = $"Done! Updated {name}: {string.Join(", ", changes)}.";
                return Success(detail, spoken);
            });
        }

        private static Color ParseColor(string colorStr)
        {
            if (NamedColors.TryGetValue(colorStr, out var namedColor))
                return namedColor;

            if (ColorUtility.TryParseHtmlString(colorStr, out var parsed))
                return parsed;
            if (ColorUtility.TryParseHtmlString("#" + colorStr, out var parsed2))
                return parsed2;

            Debug.LogWarning($"[Kiln] Could not parse color '{colorStr}', defaulting to white");
            return Color.white;
        }

        private static void ApplyColor(GameObject go, Color color)
        {
            var sr = go.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                Undo.RecordObject(sr, "Set sprite color");
                sr.color = color;
                return;
            }

            var mr = go.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                var mat = new Material(Shader.Find("Standard"));
                mat.color = color;
                Undo.RecordObject(mr, "Set material color");
                mr.sharedMaterial = mat;
            }
        }
    }
}
