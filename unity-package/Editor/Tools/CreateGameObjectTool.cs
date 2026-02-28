using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace DevFramework.MCP.Editor
{
    public class CreateGameObjectTool : ToolBase
    {
        public override string Name => "create_gameobject";
        public override string Description => "Create a new GameObject in the scene (supports 3D primitives, 2D sprites, components, and colors)";

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
                var name = parameters["name"]?.ToString() ?? "GameObject";
                var primitiveType = parameters["primitiveType"]?.ToString();
                var colorStr = parameters["color"]?.ToString();
                var parentPath = parameters["parentPath"]?.ToString();
                var sortingLayer = parameters["sortingLayer"]?.ToString();
                var sortingOrder = parameters["sortingOrder"]?.Value<int>() ?? 0;

                GameObject go;

                // Create the appropriate type
                if (string.Equals(primitiveType, "Sprite", StringComparison.OrdinalIgnoreCase))
                {
                    // 2D sprite object
                    go = new GameObject(name);
                    var sr = go.AddComponent<SpriteRenderer>();

                    // Set default white sprite so color is visible
                    sr.sprite = CreateDefaultSprite();

                    if (!string.IsNullOrEmpty(sortingLayer))
                        sr.sortingLayerName = sortingLayer;
                    sr.sortingOrder = sortingOrder;
                }
                else if (!string.IsNullOrEmpty(primitiveType) && Enum.TryParse<PrimitiveType>(primitiveType, true, out var pt))
                {
                    go = GameObject.CreatePrimitive(pt);
                    go.name = name;
                }
                else
                {
                    go = new GameObject(name);
                }

                Undo.RegisterCreatedObjectUndo(go, $"Create {name}");

                // Set transform
                if (parameters["position"] is JObject pos)
                {
                    go.transform.position = new Vector3(
                        pos["x"]?.Value<float>() ?? 0,
                        pos["y"]?.Value<float>() ?? 0,
                        pos["z"]?.Value<float>() ?? 0
                    );
                }

                if (parameters["rotation"] is JObject rot)
                {
                    go.transform.eulerAngles = new Vector3(
                        rot["x"]?.Value<float>() ?? 0,
                        rot["y"]?.Value<float>() ?? 0,
                        rot["z"]?.Value<float>() ?? 0
                    );
                }

                if (parameters["scale"] is JObject scl)
                {
                    go.transform.localScale = new Vector3(
                        scl["x"]?.Value<float>() ?? 1,
                        scl["y"]?.Value<float>() ?? 1,
                        scl["z"]?.Value<float>() ?? 1
                    );
                }

                // Set parent
                if (!string.IsNullOrEmpty(parentPath))
                {
                    var parent = GameObject.Find(parentPath);
                    if (parent != null)
                    {
                        Undo.SetTransformParent(go.transform, parent.transform, $"Parent {name}");
                    }
                }

                // Set color
                if (!string.IsNullOrEmpty(colorStr))
                {
                    var color = ParseColor(colorStr);
                    ApplyColor(go, color);
                }

                // Add components
                if (parameters["components"] is JArray components)
                {
                    foreach (var comp in components)
                    {
                        var typeName = comp["type"]?.ToString();
                        if (string.IsNullOrEmpty(typeName)) continue;

                        var componentType = FindComponentType(typeName);
                        if (componentType != null)
                        {
                            Undo.AddComponent(go, componentType);
                        }
                        else
                        {
                            Debug.LogWarning($"[DevFramework] Component type not found: {typeName}");
                        }
                    }
                }

                // Build response
                var posStr = $"{go.transform.position.x}, {go.transform.position.y}, {go.transform.position.z}";
                var typeStr = !string.IsNullOrEmpty(primitiveType) ? primitiveType.ToLower() : "empty object";
                var colorPart = !string.IsNullOrEmpty(colorStr) ? $"{colorStr} " : "";

                var spoken = $"Created a {colorPart}{typeStr} called {name} at position {posStr}.";
                var detail = $"Created GameObject '{name}' (type: {typeStr}) at ({posStr})";

                if (!string.IsNullOrEmpty(colorStr))
                    detail += $", color: {colorStr}";
                if (!string.IsNullOrEmpty(parentPath))
                    detail += $", parent: {parentPath}";

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

            Debug.LogWarning($"[DevFramework] Could not parse color '{colorStr}', defaulting to white");
            return Color.white;
        }

        private static void ApplyColor(GameObject go, Color color)
        {
            // For SpriteRenderer (2D)
            var sr = go.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                Undo.RecordObject(sr, "Set sprite color");
                sr.color = color;
                return;
            }

            // For MeshRenderer (3D)
            var mr = go.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                var mat = new Material(Shader.Find("Standard"));
                mat.color = color;
                Undo.RecordObject(mr, "Set material color");
                mr.sharedMaterial = mat;
            }
        }

        private static Sprite CreateDefaultSprite()
        {
            // Create a simple 1x1 white texture as default sprite
            var tex = new Texture2D(4, 4);
            var pixels = new Color[16];
            for (int i = 0; i < 16; i++) pixels[i] = Color.white;
            tex.SetPixels(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4);
        }

        private static Type FindComponentType(string typeName)
        {
            // Try common Unity types first
            var type = Type.GetType($"UnityEngine.{typeName}, UnityEngine");
            if (type != null) return type;

            // Try with UnityEngine.UI
            type = Type.GetType($"UnityEngine.UI.{typeName}, UnityEngine.UI");
            if (type != null) return type;

            // Try direct name
            type = Type.GetType(typeName);
            if (type != null) return type;

            // Search all loaded assemblies
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                type = asm.GetType(typeName);
                if (type != null) return type;

                type = asm.GetType($"UnityEngine.{typeName}");
                if (type != null) return type;
            }

            return null;
        }
    }
}
