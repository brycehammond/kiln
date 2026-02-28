/**
 * MCP tool: explain_scene
 *
 * Returns a gameplay-focused explanation of what the current scene would do
 * when played, including physics, colliders, and custom scripts.
 */

import type { McpServer } from '@modelcontextprotocol/sdk/server/mcp.js';
import type { UnityConnection } from '../unity-bridge/connection.js';

export function registerExplainScene(
  server: McpServer,
  connection: UnityConnection,
): void {
  server.tool(
    'explain_scene',
    'Explain what the current scene would do at runtime — physics, colliders, scripts, and gameplay behavior.',
    {},
    async () => {
      const result = await connection.sendRequest('explain_scene', {});

      const text = [
        result.message,
        '',
        `Spoken summary: ${result.spokenSummary}`,
        ...(result.data ? [`\nData: ${JSON.stringify(result.data, null, 2)}`] : []),
      ].join('\n');

      return {
        content: [{ type: 'text' as const, text }],
      };
    },
  );
}
