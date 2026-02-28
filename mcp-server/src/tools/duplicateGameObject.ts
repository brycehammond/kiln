/**
 * MCP tool: duplicate_gameobject
 *
 * Duplicates an existing GameObject in the Unity scene with optional
 * rename and position offset.
 */

import { z } from 'zod';
import type { McpServer } from '@modelcontextprotocol/sdk/server/mcp.js';
import type { UnityConnection } from '../unity-bridge/connection.js';

const Vector3Schema = z.object({
  x: z.number(),
  y: z.number(),
  z: z.number(),
});

const DuplicateGameObjectSchema = {
  name: z.string().describe('Name or hierarchy path of the GameObject to duplicate'),
  newName: z
    .string()
    .optional()
    .describe('Name for the clone (defaults to "ObjectName (Copy)")'),
  offset: Vector3Schema.optional().describe(
    'Position offset from the original (defaults to 1 unit on X)',
  ),
};

export function registerDuplicateGameObject(
  server: McpServer,
  connection: UnityConnection,
): void {
  server.tool(
    'duplicate_gameobject',
    'Duplicate an existing GameObject in the Unity scene with optional rename and position offset.',
    DuplicateGameObjectSchema,
    async (params) => {
      const result = await connection.sendRequest('duplicate_gameobject', params as Record<string, unknown>);

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
