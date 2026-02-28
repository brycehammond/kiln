/**
 * MCP tool: delete_gameobject
 *
 * Deletes a GameObject from the Unity scene by name or hierarchy path.
 * The deletion is undoable via Unity's Undo system.
 */

import { z } from 'zod';
import type { McpServer } from '@modelcontextprotocol/sdk/server/mcp.js';
import type { UnityConnection } from '../unity-bridge/connection.js';

const DeleteGameObjectSchema = {
  name: z.string().describe('Name or hierarchy path of the GameObject to delete'),
};

export function registerDeleteGameObject(
  server: McpServer,
  connection: UnityConnection,
): void {
  server.tool(
    'delete_gameobject',
    'Delete a GameObject from the Unity scene. The deletion can be undone with Ctrl+Z.',
    DeleteGameObjectSchema,
    async (params) => {
      const result = await connection.sendRequest('delete_gameobject', params as Record<string, unknown>);

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
