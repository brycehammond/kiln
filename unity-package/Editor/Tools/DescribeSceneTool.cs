using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DevFramework.MCP.Editor
{
    public class DescribeSceneTool : ToolBase
    {
        public override string Name => "describe_scene";
        public override string Description => "Describe the current scene hierarchy in natural language";

        public override async Task<JObject> Execute(JObject parameters)
        {
            return await MainThreadDispatcher.RunOnMainThread(() =>
            {
                var maxDepth = parameters["maxDepth"]?.Value<int>() ?? 3;
                var scene = SceneManager.GetActiveScene();
                var roots = scene.GetRootGameObjects();

                if (roots.Length == 0)
                {
                    return Success(
                        "The scene is empty.",
                        "The scene is empty. There are no objects."
                    );
                }

                var sb = new StringBuilder();
                var objectNames = new List<string>();
                var totalObjects = 0;
                var detailBuilder = new StringBuilder();

                sb.AppendLine($"Scene: {scene.name}");
                sb.AppendLine($"Root objects: {roots.Length}");
                sb.AppendLine();

                foreach (var root in roots)
                {
                    objectNames.Add(root.name);
                    totalObjects += CountChildren(root) + 1;
                    DescribeObject(root, detailBuilder, 0, maxDepth);
                    detailBuilder.AppendLine();
                }

                sb.Append(detailBuilder);

                // Build spoken summary
                var nameList = string.Join(", ", objectNames.GetRange(0, System.Math.Min(objectNames.Count, 5)));
                if (objectNames.Count > 5)
                    nameList += $", and {objectNames.Count - 5} more";

                var spoken = $"The scene {scene.name} has {roots.Length} root objects: {nameList}. " +
                             $"There are {totalObjects} total objects.";

                return Success(sb.ToString(), spoken);
            });
        }

        private void DescribeObject(GameObject go, StringBuilder sb, int depth, int maxDepth)
        {
            var indent = new string(' ', depth * 2);
            var activeStr = go.activeSelf ? "" : " [inactive]";
            sb.Append($"{indent}- {go.name}{activeStr}");

            // List key components (skip Transform as everything has it)
            var components = go.GetComponents<Component>();
            var compNames = new List<string>();
            foreach (var comp in components)
            {
                if (comp == null) continue;
                var typeName = comp.GetType().Name;
                if (typeName == "Transform" || typeName == "RectTransform") continue;
                compNames.Add(typeName);
            }

            if (compNames.Count > 0)
                sb.Append($" ({string.Join(", ", compNames)})");

            // Position info
            var pos = go.transform.position;
            sb.AppendLine($" at ({pos.x:F1}, {pos.y:F1}, {pos.z:F1})");

            // Recurse children
            if (depth < maxDepth)
            {
                for (int i = 0; i < go.transform.childCount; i++)
                {
                    DescribeObject(go.transform.GetChild(i).gameObject, sb, depth + 1, maxDepth);
                }
            }
            else if (go.transform.childCount > 0)
            {
                sb.AppendLine($"{indent}  ... {go.transform.childCount} children");
            }
        }

        private int CountChildren(GameObject go)
        {
            int count = 0;
            for (int i = 0; i < go.transform.childCount; i++)
            {
                count += 1 + CountChildren(go.transform.GetChild(i).gameObject);
            }
            return count;
        }
    }
}
