/**
 * MCP tool: set_transform
 *
 * Sets the position, rotation, and/or scale of an existing GameObject by name.
 */

import { z } from 'zod';
import type { McpServer } from '@modelcontextprotocol/sdk/server/mcp.js';
import type { UnityConnection } from '../unity-bridge/connection.js';

const Vector3Schema = z.object({
  x: z.number(),
  y: z.number(),
  z: z.number(),
});

const SetTransformSchema = {
  name: z.string().describe('Name of the GameObject to modify'),
  position: Vector3Schema.optional().describe('New world-space position'),
  rotation: Vector3Schema.optional().describe('New Euler rotation in degrees'),
  scale: Vector3Schema.optional().describe('New local scale'),
};

export function registerSetTransform(
  server: McpServer,
  connection: UnityConnection,
): void {
  server.tool(
    'set_transform',
    'Set the position, rotation, and/or scale of an existing GameObject.',
    SetTransformSchema,
    async (params) => {
      const result = await connection.sendRequest('set_transform', params as Record<string, unknown>);

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
