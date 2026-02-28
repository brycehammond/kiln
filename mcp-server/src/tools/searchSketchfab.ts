/**
 * MCP tool: search_sketchfab
 *
 * Searches Sketchfab for free downloadable 3D models.
 * This is a TS-only tool — it does not communicate with Unity.
 */

import { z } from 'zod';
import type { McpServer } from '@modelcontextprotocol/sdk/server/mcp.js';

const SearchSketchfabSchema = {
  query: z.string().describe('Search query (e.g. "low poly character")'),
  limit: z
    .number()
    .int()
    .min(1)
    .max(20)
    .optional()
    .default(5)
    .describe('Maximum number of results to return'),
};

interface SketchfabModel {
  uid: string;
  name: string;
  user: { displayName: string };
  license: { label: string };
  thumbnails?: { images?: Array<{ url: string; width: number }> };
  vertexCount: number;
}

interface SketchfabResponse {
  results: SketchfabModel[];
}

export function registerSearchSketchfab(server: McpServer): void {
  server.tool(
    'search_sketchfab',
    'Search Sketchfab for free downloadable 3D models. Returns UIDs you can pass to import_asset(sketchfabUid=...).',
    SearchSketchfabSchema,
    async (params) => {
      const apiKey = process.env.SKETCHFAB_API_KEY;
      if (!apiKey) {
        return {
          content: [
            {
              type: 'text' as const,
              text:
                'Sketchfab API key not configured.\n\n' +
                'To enable search:\n' +
                '1. Get a free API key at https://sketchfab.com/settings/password\n' +
                '2. Set the SKETCHFAB_API_KEY environment variable\n' +
                '3. Restart the MCP server',
            },
          ],
        };
      }

      const { query, limit } = params;

      try {
        const searchParams = new URLSearchParams({
          q: query,
          downloadable: 'true',
          type: 'models',
          count: String(limit),
        });
        const url = `https://api.sketchfab.com/v3/models?${searchParams}`;
        const response = await fetch(url, {
          headers: { Authorization: `Token ${apiKey}` },
        });

        if (!response.ok) {
          throw new Error(`Sketchfab API error: HTTP ${response.status} ${response.statusText}`);
        }

        const data = (await response.json()) as SketchfabResponse;

        if (!data.results || data.results.length === 0) {
          return {
            content: [{ type: 'text' as const, text: `No results found for "${query}".` }],
          };
        }

        const lines = data.results.map((m, i) => {
          // Pick the largest thumbnail available
          const thumb = m.thumbnails?.images
            ?.sort((a, b) => b.width - a.width)?.[0]?.url ?? 'N/A';

          return [
            `${i + 1}. **${m.name}**`,
            `   Author: ${m.user.displayName}`,
            `   License: ${m.license.label}`,
            `   UID: ${m.uid}`,
            `   Vertices: ${m.vertexCount.toLocaleString()}`,
            `   Thumbnail: ${thumb}`,
          ].join('\n');
        });

        const text = [
          `Found results for "${query}" (showing ${data.results.length}):`,
          '',
          ...lines,
          '',
          'To import a model, use: import_asset(sketchfabUid="<UID>")',
        ].join('\n');

        return { content: [{ type: 'text' as const, text }] };
      } catch (err: unknown) {
        const msg = err instanceof Error ? err.message : String(err);
        return {
          content: [{ type: 'text' as const, text: `Search failed: ${msg}` }],
          isError: true,
        };
      }
    },
  );
}
