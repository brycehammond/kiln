/**
 * MCP tool: get_project_summary
 *
 * Returns high-level information about the Unity project: scenes, scripts,
 * assets, packages, and project settings.
 */

import type { McpServer } from '@modelcontextprotocol/sdk/server/mcp.js';
import type { UnityConnection } from '../unity-bridge/connection.js';

export function registerGetProjectSummary(
  server: McpServer,
  connection: UnityConnection,
): void {
  server.tool(
    'get_project_summary',
    'Get a high-level summary of the Unity project including scenes, scripts, assets, and settings.',
    {},
    async () => {
      const result = await connection.sendRequest('get_project_summary', {});

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
