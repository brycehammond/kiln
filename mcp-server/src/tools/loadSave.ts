/**
 * MCP tool: load_save
 *
 * Restores a previously saved scene checkpoint by name.
 */

import { z } from 'zod';
import type { McpServer } from '@modelcontextprotocol/sdk/server/mcp.js';
import type { UnityConnection } from '../unity-bridge/connection.js';

const LoadSaveSchema = {
  name: z.string().optional().describe('Name of the save checkpoint to restore'),
};

export function registerLoadSave(
  server: McpServer,
  connection: UnityConnection,
): void {
  server.tool(
    'load_save',
    'Restore a previously saved scene checkpoint.',
    LoadSaveSchema,
    async (params) => {
      const result = await connection.sendRequest('load_save', params as Record<string, unknown>);

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
