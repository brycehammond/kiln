using UnityEditor;
using UnityEngine;

namespace DevFramework.MCP.Editor
{
    public class StatusWindow : EditorWindow
    {
        [MenuItem("Window/Dev Framework/Status")]
        public static void ShowWindow()
        {
            GetWindow<StatusWindow>("Dev Framework");
        }

        private void OnGUI()
        {
            GUILayout.Label("Dev Framework MCP", EditorStyles.boldLabel);
            GUILayout.Space(10);

            var server = DevFrameworkServer.Instance;

            // Server status
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Server:");
            var serverStatus = server != null && server.IsRunning ? "Running (port 8091)" : "Stopped";
            var serverColor = server != null && server.IsRunning ? Color.green : Color.red;
            var oldColor = GUI.color;
            GUI.color = serverColor;
            GUILayout.Label(serverStatus, EditorStyles.boldLabel);
            GUI.color = oldColor;
            EditorGUILayout.EndHorizontal();

            // Client status
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("MCP Client:");
            var clientStatus = server != null && server.HasClient ? "Connected" : "Not connected";
            var clientColor = server != null && server.HasClient ? Color.green : Color.yellow;
            GUI.color = clientColor;
            GUILayout.Label(clientStatus, EditorStyles.boldLabel);
            GUI.color = oldColor;
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(20);

            // Manual controls
            if (server != null && !server.IsRunning)
            {
                if (GUILayout.Button("Start Server"))
                    server.Start();
            }
            else if (server != null && server.IsRunning)
            {
                if (GUILayout.Button("Restart Server"))
                {
                    server.Stop();
                    server.Start();
                }
            }

            GUILayout.Space(10);
            GUILayout.Label("Use Claude Code CLI to send commands.", EditorStyles.miniLabel);
        }

        private void OnInspectorUpdate()
        {
            Repaint();
        }
    }
}
