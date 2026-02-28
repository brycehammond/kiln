/**
 * MCP tools: enter_play_mode / exit_play_mode
 *
 * Starts or stops Play Mode in the Unity Editor.
 */

import type { McpServer } from '@modelcontextprotocol/sdk/server/mcp.js';
import type { UnityConnection } from '../unity-bridge/connection.js';

export function registerPlayMode(
  server: McpServer,
  connection: UnityConnection,
): void {
  server.tool(
    'enter_play_mode',
    'Enter Play Mode in the Unity Editor to test the game.',
    {},
    async () => {
      const result = await connection.sendRequest('enter_play_mode', {});

      const text = [
        result.message,
        '',
        `Spoken summary: ${result.spokenSummary}`,
      ].join('\n');

      return {
        content: [{ type: 'text' as const, text }],
      };
    },
  );

  server.tool(
    'exit_play_mode',
    'Exit Play Mode in the Unity Editor to stop testing.',
    {},
    async () => {
      const result = await connection.sendRequest('exit_play_mode', {});

      const text = [
        result.message,
        '',
        `Spoken summary: ${result.spokenSummary}`,
      ].join('\n');

      return {
        content: [{ type: 'text' as const, text }],
      };
    },
  );
}
