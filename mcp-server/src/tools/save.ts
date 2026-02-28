/**
 * MCP tool: save
 *
 * Saves the current Unity scene state as a named checkpoint that can be
 * restored later with load_save.
 */

import { z } from 'zod';
import type { McpServer } from '@modelcontextprotocol/sdk/server/mcp.js';
import type { UnityConnection } from '../unity-bridge/connection.js';

const SaveSchema = {
  name: z.string().optional().describe('A short name for this save checkpoint'),
  description: z.string().optional().describe('Optional description of what this save captures'),
};

export function registerSave(
  server: McpServer,
  connection: UnityConnection,
): void {
  server.tool(
    'save',
    'Save the current Unity scene state as a named checkpoint.',
    SaveSchema,
    async (params) => {
      const result = await connection.sendRequest('save', params as Record<string, unknown>);

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
