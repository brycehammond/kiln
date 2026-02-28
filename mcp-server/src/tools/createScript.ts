/**
 * MCP tool: create_script
 *
 * Creates a new C# script file in the Unity project and optionally attaches
 * it to an existing GameObject.
 */

import { z } from 'zod';
import type { McpServer } from '@modelcontextprotocol/sdk/server/mcp.js';
import type { UnityConnection } from '../unity-bridge/connection.js';

const CreateScriptSchema = {
  scriptName: z.string().describe('Class name for the new script'),
  scriptType: z
    .enum(['MonoBehaviour', 'ScriptableObject', 'EditorWindow', 'Plain'])
    .optional()
    .default('MonoBehaviour')
    .describe('Base type of the C# script'),
  code: z
    .string()
    .optional()
    .describe('Full C# source code. If omitted a template is generated.'),
  directory: z
    .string()
    .optional()
    .default('Assets/Scripts')
    .describe('Asset directory in which to save the script'),
  attachTo: z
    .string()
    .optional()
    .describe('Name of a GameObject to attach the script to after creation'),
};

export function registerCreateScript(
  server: McpServer,
  connection: UnityConnection,
): void {
  server.tool(
    'create_script',
    'Create a new C# script in the Unity project, optionally attaching it to a GameObject.',
    CreateScriptSchema,
    async (params) => {
      const result = await connection.sendRequest('create_script', params as Record<string, unknown>);

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
