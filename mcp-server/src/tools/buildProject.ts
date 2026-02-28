/**
 * MCP tool: build_project
 *
 * Triggers a Unity player build for the specified platform and output path.
 */

import { z } from 'zod';
import type { McpServer } from '@modelcontextprotocol/sdk/server/mcp.js';
import type { UnityConnection } from '../unity-bridge/connection.js';

const BuildProjectSchema = {
  target: z
    .string()
    .optional()
    .describe('Build target platform (e.g. "Windows", "Mac", "Linux", "WebGL", "Android", "iOS"). Defaults to the active build target.'),
  outputPath: z
    .string()
    .optional()
    .describe('Output directory for the build. Defaults to Builds/{target}/ in the project root.'),
};

export function registerBuildProject(
  server: McpServer,
  connection: UnityConnection,
): void {
  server.tool(
    'build_project',
    'Build the Unity project for a target platform.',
    BuildProjectSchema,
    async (params) => {
      const result = await connection.sendRequest('build_project', params as Record<string, unknown>);

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
