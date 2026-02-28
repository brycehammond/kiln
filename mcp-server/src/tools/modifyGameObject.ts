/**
 * MCP tool: modify_gameobject
 *
 * Modifies an existing GameObject in the Unity scene — transform, name,
 * active state, color, or parent.
 */

import { z } from 'zod';
import type { McpServer } from '@modelcontextprotocol/sdk/server/mcp.js';
import type { UnityConnection } from '../unity-bridge/connection.js';

const Vector3Schema = z.object({
  x: z.number(),
  y: z.number(),
  z: z.number(),
});

const ModifyGameObjectSchema = {
  name: z.string().describe('Name or hierarchy path of the target GameObject'),
  position: Vector3Schema.optional().describe('New world-space position'),
  rotation: Vector3Schema.optional().describe('New euler rotation in degrees'),
  scale: Vector3Schema.optional().describe('New local scale'),
  newName: z.string().optional().describe('Rename the object'),
  active: z.boolean().optional().describe('Enable or disable the object'),
  color: z
    .string()
    .optional()
    .describe('Named color ("red") or hex ("#FF0000")'),
  parentPath: z
    .string()
    .optional()
    .describe('Reparent to this object (empty string = unparent to root)'),
};

export function registerModifyGameObject(
  server: McpServer,
  connection: UnityConnection,
): void {
  server.tool(
    'modify_gameobject',
    'Modify an existing GameObject — move, rotate, scale, rename, reparent, recolor, or toggle active state.',
    ModifyGameObjectSchema,
    async (params) => {
      const result = await connection.sendRequest('modify_gameobject', params as Record<string, unknown>);

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
