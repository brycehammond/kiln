/**
 * MCP tool: search_poly_pizza
 *
 * Searches Poly Pizza for free CC0 3D models.
 * This is a TS-only tool — it does not communicate with Unity.
 */

import { z } from 'zod';
import type { McpServer } from '@modelcontextprotocol/sdk/server/mcp.js';

const SearchPolyPizzaSchema = {
  query: z.string().describe('Search query (e.g. "low poly tree")'),
  limit: z
    .number()
    .int()
    .min(1)
    .max(20)
    .optional()
    .default(5)
    .describe('Maximum number of results to return'),
};

interface PolyPizzaResult {
  Title: string;
  Creator: string;
  Licence: string;
  Download: string;
  Thumbnail: string;
}

interface PolyPizzaResponse {
  results: PolyPizzaResult[];
  total: number;
}

export function registerSearchPolyPizza(server: McpServer): void {
  server.tool(
    'search_poly_pizza',
    'Search Poly Pizza for free CC0 3D models. Returns download URLs you can pass to import_asset.',
    SearchPolyPizzaSchema,
    async (params) => {
      const apiKey = process.env.POLY_PIZZA_API_KEY;
      if (!apiKey) {
        return {
          content: [
            {
              type: 'text' as const,
              text:
                'Poly Pizza API key not configured.\n\n' +
                'To enable search:\n' +
                '1. Get a free API key at https://poly.pizza/developer\n' +
                '2. Set the POLY_PIZZA_API_KEY environment variable\n' +
                '3. Restart the MCP server',
            },
          ],
        };
      }

      const { query, limit } = params;

      try {
        const url = `https://api.poly.pizza/v1.1/search/${encodeURIComponent(query)}?Limit=${limit}`;
        const response = await fetch(url, {
          headers: { 'x-auth-token': apiKey },
        });

        if (!response.ok) {
          throw new Error(`Poly Pizza API error: HTTP ${response.status} ${response.statusText}`);
        }

        const data = (await response.json()) as PolyPizzaResponse;

        if (!data.results || data.results.length === 0) {
          return {
            content: [{ type: 'text' as const, text: `No results found for "${query}".` }],
          };
        }

        const lines = data.results.map((r, i) => {
          return [
            `${i + 1}. **${r.Title}**`,
            `   Author: ${r.Creator}`,
            `   License: ${r.Licence}`,
            `   Download URL: ${r.Download}`,
            `   Thumbnail: ${r.Thumbnail}`,
          ].join('\n');
        });

        const text = [
          `Found ${data.total} result(s) for "${query}" (showing ${data.results.length}):`,
          '',
          ...lines,
          '',
          'To import a model, use: import_asset(url="<Download URL>")',
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
