using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace DevFramework.MCP.Editor
{
    public abstract class ToolBase
    {
        public abstract string Name { get; }
        public abstract string Description { get; }

        public abstract Task<JObject> Execute(JObject parameters);

        protected JObject Success(string message, string spokenSummary, JObject data = null)
        {
            var result = new JObject
            {
                ["success"] = true,
                ["message"] = message,
                ["spokenSummary"] = spokenSummary
            };
            if (data != null)
                result["data"] = data;
            return result;
        }

        protected JObject Failure(string message, string spokenSummary)
        {
            return new JObject
            {
                ["success"] = false,
                ["message"] = message,
                ["spokenSummary"] = spokenSummary
            };
        }
    }
}
