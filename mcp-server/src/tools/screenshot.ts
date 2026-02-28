/**
 * MCP tool: screenshot
 *
 * Captures a screenshot of the Unity Game or Scene view and returns it
 * as an MCP image content block.
 */

import { z } from 'zod';
import type { McpServer } from '@modelcontextprotocol/sdk/server/mcp.js';
import type { UnityConnection } from '../unity-bridge/connection.js';

const ScreenshotSchema = {
  view: z
    .enum(['game', 'scene'])
    .optional()
    .default('game')
    .describe('Which view to capture: "game" (default) or "scene"'),
  width: z
    .number()
    .int()
    .min(64)
    .max(768)
    .optional()
    .default(512)
    .describe('Image width in pixels (64–768, default 512)'),
  height: z
    .number()
    .int()
    .min(64)
    .max(768)
    .optional()
    .default(512)
    .describe('Image height in pixels (64–768, default 512)'),
};

export function registerScreenshot(
  server: McpServer,
  connection: UnityConnection,
): void {
  server.tool(
    'screenshot',
    'Capture a screenshot of the Unity Game or Scene view.',
    ScreenshotSchema,
    async (params) => {
      const result = await connection.sendRequest('screenshot', params as Record<string, unknown>);

      if (!result.success || !result.data?.imageBase64) {
        return {
          content: [
            {
              type: 'text' as const,
              text: result.message ?? 'Screenshot failed with no error message.',
            },
          ],
          isError: true,
        };
      }

      const imageBase64 = result.data.imageBase64 as string;
      const mimeType = (result.data.mimeType as string) ?? 'image/png';

      return {
        content: [
          {
            type: 'image' as const,
            data: imageBase64,
            mimeType,
          },
          {
            type: 'text' as const,
            text: result.message,
          },
        ],
      };
    },
  );
}
