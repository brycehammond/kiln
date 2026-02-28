/**
 * MCP tool: create_gameobject
 *
 * Creates a new GameObject in the Unity scene with optional primitive mesh,
 * transform overrides, color, parenting, and component attachments.
 */

import { z } from 'zod';
import type { McpServer } from '@modelcontextprotocol/sdk/server/mcp.js';
import type { UnityConnection } from '../unity-bridge/connection.js';

const Vector3Schema = z.object({
  x: z.number(),
  y: z.number(),
  z: z.number(),
});

const ComponentSpecSchema = z.object({
  type: z.string(),
  properties: z.record(z.unknown()).optional(),
});

const CreateGameObjectSchema = {
  name: z.string().describe('Name for the new GameObject'),
  primitiveType: z
    .enum(['Cube', 'Sphere', 'Cylinder', 'Capsule', 'Plane', 'Quad', 'Sprite'])
    .optional()
    .describe('Primitive type: 3D mesh (Cube, Sphere, etc.) or 2D Sprite'),
  position: Vector3Schema.optional().describe('World-space position'),
  rotation: Vector3Schema.optional().describe('Euler rotation in degrees'),
  scale: Vector3Schema.optional().describe('Local scale'),
  color: z
    .string()
    .optional()
    .describe('Named color ("red") or hex ("#FF0000")'),
  parentPath: z
    .string()
    .optional()
    .describe('Hierarchy path to parent, e.g. "Environment/Walls"'),
  components: z
    .array(ComponentSpecSchema)
    .optional()
    .describe('Additional components to attach (e.g. Rigidbody, BoxCollider2D, Rigidbody2D)'),
  sortingLayer: z
    .string()
    .optional()
    .describe('Sorting layer name for 2D rendering order'),
  sortingOrder: z
    .number()
    .optional()
    .describe('Order within the sorting layer for 2D rendering'),
};

export function registerCreateGameObject(
  server: McpServer,
  connection: UnityConnection,
): void {
  server.tool(
    'create_gameobject',
    'Create a new GameObject in the Unity scene. Supports primitives, transforms, colors, parenting, and component attachment.',
    CreateGameObjectSchema,
    async (params) => {
      const result = await connection.sendRequest('create_gameobject', params as Record<string, unknown>);

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
