using System;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Kiln.MCP.Editor
{
    public class ListSavesTool : ToolBase
    {
        public override string Name => "list_saves";
        public override string Description => "List all saved project states with names, descriptions, and timestamps.";

        public override Task<JObject> Execute(JObject parameters)
        {
            var manifest = SaveTool.LoadManifest();

            if (manifest.Count == 0)
            {
                return Task.FromResult(Success(
                    "No saves found.",
                    "There are no saves yet.",
                    new JObject { ["saves"] = new JArray() }
                ));
            }

            // Build response with relative time descriptions
            var saves = new JArray();
            JToken mostRecent = null;

            foreach (var entry in manifest)
            {
                var save = new JObject
                {
                    ["name"] = entry["name"],
                    ["description"] = entry["description"],
                    ["timestamp"] = entry["timestamp"],
                    ["scenePath"] = entry["scenePath"],
                    ["commitHash"] = entry["commitHash"]
                };

                if (DateTime.TryParse(entry["timestamp"]?.ToString(), out var ts))
                {
                    save["relativeTime"] = GetRelativeTime(ts.ToUniversalTime());
                }

                saves.Add(save);
                mostRecent = entry;
            }

            var mostRecentName = mostRecent?["name"]?.ToString() ?? "unknown";
            var mostRecentTime = "";
            if (DateTime.TryParse(mostRecent?["timestamp"]?.ToString(), out var mrTs))
            {
                mostRecentTime = $", {GetRelativeTime(mrTs.ToUniversalTime())}";
            }

            var spoken = $"You have {manifest.Count} save{(manifest.Count == 1 ? "" : "s")}. Most recent is {mostRecentName}{mostRecentTime}.";

            return Task.FromResult(Success(
                $"{manifest.Count} save(s) found.",
                spoken,
                new JObject { ["saves"] = saves }
            ));
        }

        private static string GetRelativeTime(DateTime utcTime)
        {
            var diff = DateTime.UtcNow - utcTime;

            if (diff.TotalSeconds < 60)
                return "just now";
            if (diff.TotalMinutes < 60)
            {
                var mins = (int)diff.TotalMinutes;
                return $"{mins} minute{(mins == 1 ? "" : "s")} ago";
            }
            if (diff.TotalHours < 24)
            {
                var hours = (int)diff.TotalHours;
                return $"{hours} hour{(hours == 1 ? "" : "s")} ago";
            }
            var days = (int)diff.TotalDays;
            return $"{days} day{(days == 1 ? "" : "s")} ago";
        }
    }
}
