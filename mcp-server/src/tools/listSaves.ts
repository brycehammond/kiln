/**
 * MCP tool: list_saves
 *
 * Lists all available save checkpoints for the current project.
 */

import type { McpServer } from '@modelcontextprotocol/sdk/server/mcp.js';
import type { UnityConnection } from '../unity-bridge/connection.js';

export function registerListSaves(
  server: McpServer,
  connection: UnityConnection,
): void {
  server.tool(
    'list_saves',
    'List all available save checkpoints.',
    {},
    async () => {
      const result = await connection.sendRequest('list_saves', {});

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
