using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Kiln.MCP.Editor
{
    public class MessageRouter
    {
        private readonly Dictionary<string, ToolBase> _tools = new Dictionary<string, ToolBase>();

        public void RegisterTool(ToolBase tool)
        {
            _tools[tool.Name] = tool;
            Debug.Log($"[Kiln] Registered tool: {tool.Name}");
        }

        public async Task<string> RouteMessage(string message)
        {
            JObject request;
            try
            {
                request = JObject.Parse(message);
            }
            catch (Exception ex)
            {
                return CreateErrorResponse(null, -32700, $"Parse error: {ex.Message}");
            }

            var id = request["id"]?.ToString();
            var method = request["method"]?.ToString();
            var parameters = request["params"] as JObject ?? new JObject();

            if (string.IsNullOrEmpty(method))
            {
                return CreateErrorResponse(id, -32600, "Invalid request: missing method");
            }

            if (!_tools.TryGetValue(method, out var tool))
            {
                return CreateErrorResponse(id, -32601, $"Method not found: {method}");
            }

            try
            {
                var result = await tool.Execute(parameters);
                var response = new JObject
                {
                    ["id"] = id,
                    ["result"] = result
                };
                return response.ToString(Newtonsoft.Json.Formatting.None);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Kiln] Tool '{method}' error: {ex}");
                return CreateErrorResponse(id, -32000, ex.Message);
            }
        }

        private string CreateErrorResponse(string id, int code, string message)
        {
            var response = new JObject
            {
                ["id"] = id,
                ["error"] = new JObject
                {
                    ["code"] = code,
                    ["message"] = message
                }
            };
            return response.ToString(Newtonsoft.Json.Formatting.None);
        }
    }
}
