/**
 * MCP tool: read_script
 *
 * Reads the contents of a C# script from the Unity project by asset path
 * or class name.
 */

import { z } from 'zod';
import type { McpServer } from '@modelcontextprotocol/sdk/server/mcp.js';
import type { UnityConnection } from '../unity-bridge/connection.js';

const ReadScriptSchema = {
  path: z
    .string()
    .optional()
    .describe('Asset path, e.g. "Assets/Scripts/MyScript.cs"'),
  className: z
    .string()
    .optional()
    .describe('Find the script by its class name'),
};

export function registerReadScript(
  server: McpServer,
  connection: UnityConnection,
): void {
  server.tool(
    'read_script',
    'Read the contents of a C# script from the Unity project by path or class name. At least one of path or className must be provided.',
    ReadScriptSchema,
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

      const result = await connection.sendRequest('read_script', params as Record<string, unknown>);

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
