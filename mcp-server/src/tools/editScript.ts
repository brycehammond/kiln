/**
 * MCP tool: edit_script
 *
 * Replaces the contents of an existing C# script in the Unity project,
 * locating it by asset path or class name.
 */

import { z } from 'zod';
import type { McpServer } from '@modelcontextprotocol/sdk/server/mcp.js';
import type { UnityConnection } from '../unity-bridge/connection.js';

const EditScriptSchema = {
  path: z
    .string()
    .optional()
    .describe('Asset path, e.g. "Assets/Scripts/MyScript.cs"'),
  className: z
    .string()
    .optional()
    .describe('Find the script by its class name'),
  code: z
    .string()
    .describe('Full replacement C# source code'),
};

export function registerEditScript(
  server: McpServer,
  connection: UnityConnection,
): void {
  server.tool(
    'edit_script',
    'Replace the contents of an existing C# script by path or class name. At least one of path or className must be provided.',
    EditScriptSchema,
    async (params) => {
      if (!params.path && !params.className) {
        return {
          content: [
            {
              type: 'text' as const,
              text: 'Error: You must provide at least one of "path" or "className".',
            },
          ],
        };
      }

      const result = await connection.sendRequest('edit_script', params as Record<string, unknown>);

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
