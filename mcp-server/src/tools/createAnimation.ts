/**
 * MCP tool: create_animation
 *
 * Creates a simple keyframe animation clip in Unity with friendly property
 * names (position, rotation, scale, color) mapped to Unity property paths.
 */

import { z } from 'zod';
import type { McpServer } from '@modelcontextprotocol/sdk/server/mcp.js';
import type { UnityConnection } from '../unity-bridge/connection.js';

const Vector3Value = z.object({
  x: z.number(),
  y: z.number(),
  z: z.number(),
});

const ColorValue = z.object({
  r: z.number(),
  g: z.number(),
  b: z.number(),
  a: z.number().optional().default(1),
});

const KeyframeSchema = z.object({
  time: z.number().describe('Time in seconds'),
  value: z.union([Vector3Value, ColorValue]).describe('Keyframe value — {x,y,z} for transform properties, {r,g,b,a} for color'),
});

const AnimPropertySchema = z.object({
  property: z
    .enum(['position', 'rotation', 'scale', 'color'])
    .describe('The property to animate'),
  keyframes: z
    .array(KeyframeSchema)
    .min(1)
    .describe('Array of keyframes with time and value'),
});

const CreateAnimationSchema = {
  name: z.string().describe('Name for the animation clip, e.g. "Bounce"'),
  gameObjectName: z
    .string()
    .optional()
    .describe('Attach the clip to this GameObject (optional — if omitted, just creates the clip asset)'),
  loop: z
    .boolean()
    .optional()
    .default(true)
    .describe('Whether the animation loops (default true)'),
  properties: z
    .array(AnimPropertySchema)
    .min(1)
    .describe('Properties to animate with their keyframes'),
};

export function registerCreateAnimation(
  server: McpServer,
  connection: UnityConnection,
): void {
  server.tool(
    'create_animation',
    'Create a keyframe animation clip. Supports position, rotation, scale, and color properties with friendly names.',
    CreateAnimationSchema,
    async (params) => {
      const result = await connection.sendRequest('create_animation', params as Record<string, unknown>);

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
