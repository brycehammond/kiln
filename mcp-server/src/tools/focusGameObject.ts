/**
 * MCP tool: focus_gameobject
 *
 * Selects a GameObject and frames it in the Scene view.
 */

import { z } from 'zod';
import type { McpServer } from '@modelcontextprotocol/sdk/server/mcp.js';
import type { UnityConnection } from '../unity-bridge/connection.js';

const FocusGameObjectSchema = {
  name: z
    .string()
    .describe('Name or hierarchy path of the GameObject to focus'),
};

export function registerFocusGameObject(
  server: McpServer,
  connection: UnityConnection,
): void {
  server.tool(
    'focus_gameobject',
    'Select a GameObject and frame it in the Scene view.',
    FocusGameObjectSchema,
    async (params) => {
      const result = await connection.sendRequest('focus_gameobject', params as Record<string, unknown>);

      const text = [
        result.message,
        '',
        `Spoken summary: ${result.spokenSummary}`,
      ].join('\n');

      return {
        content: [{ type: 'text' as const, text }],
      };
    },
  );
}
