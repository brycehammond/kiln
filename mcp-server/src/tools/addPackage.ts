/**
 * MCP tool: add_package
 *
 * Installs a Unity Package Manager package by forwarding the identifier
 * to the Unity-side AddPackageTool.
 */

import { z } from 'zod';
import type { McpServer } from '@modelcontextprotocol/sdk/server/mcp.js';
import type { UnityConnection } from '../unity-bridge/connection.js';

const AddPackageSchema = {
  identifier: z
    .string()
    .describe(
      'UPM package identifier — a package name (e.g. "com.unity.cloud.gltfast"), ' +
        'name@version, git URL, or file: path',
    ),
};

export function registerAddPackage(server: McpServer, connection: UnityConnection): void {
  server.tool(
    'add_package',
    'Install a Unity Package Manager (UPM) package by name, version, git URL, or file path.',
    AddPackageSchema,
    async (params) => {
      const result = await connection.sendRequest('add_package', params as Record<string, unknown>);

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
