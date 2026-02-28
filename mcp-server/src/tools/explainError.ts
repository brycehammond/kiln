/**
 * MCP tool: explain_error
 *
 * Sends a Unity error message to the Unity-side assistant for a plain-English
 * explanation, optionally enriched with user-provided context.
 */

import { z } from 'zod';
import type { McpServer } from '@modelcontextprotocol/sdk/server/mcp.js';
import type { UnityConnection } from '../unity-bridge/connection.js';

const ExplainErrorSchema = {
  errorMessage: z.string().describe('The Unity error or exception text'),
  context: z
    .string()
    .optional()
    .describe('Additional context about what the user was doing when the error occurred'),
};

export function registerExplainError(
  server: McpServer,
  connection: UnityConnection,
): void {
  server.tool(
    'explain_error',
    'Explain a Unity error message in plain English with suggested fixes.',
    ExplainErrorSchema,
    async (params) => {
      const result = await connection.sendRequest('explain_error', params as Record<string, unknown>);

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
