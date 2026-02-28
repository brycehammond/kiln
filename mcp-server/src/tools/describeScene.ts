/**
 * MCP tool: describe_scene
 *
 * Returns a natural-language description of the current Unity scene hierarchy.
 */

import { z } from 'zod';
import type { McpServer } from '@modelcontextprotocol/sdk/server/mcp.js';
import type { UnityConnection } from '../unity-bridge/connection.js';

const DescribeSceneSchema = {
  maxDepth: z
    .number()
    .optional()
    .default(3)
    .describe('How many levels deep to traverse the hierarchy'),
};

export function registerDescribeScene(
  server: McpServer,
  connection: UnityConnection,
): void {
  server.tool(
    'describe_scene',
    'Describe the current Unity scene hierarchy in natural language.',
    DescribeSceneSchema,
    async (params) => {
      const result = await connection.sendRequest('describe_scene', params as Record<string, unknown>);

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
