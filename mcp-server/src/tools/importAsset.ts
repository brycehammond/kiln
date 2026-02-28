/**
 * MCP tool: import_asset
 *
 * Downloads a file from a URL (or resolves a Sketchfab UID) and imports it
 * into the Unity project via the Unity-side ImportAssetTool.
 */

import { z } from 'zod';
import { createWriteStream } from 'fs';
import { mkdir, readdir, rename, rm } from 'fs/promises';
import { tmpdir } from 'os';
import { join, basename, extname } from 'path';
import { pipeline } from 'stream/promises';
import { Readable } from 'stream';
import { createUnzip } from 'zlib';
import type { McpServer } from '@modelcontextprotocol/sdk/server/mcp.js';
import type { UnityConnection } from '../unity-bridge/connection.js';

const TEMP_DIR = join(tmpdir(), 'kiln-imports');

const ImportAssetSchema = {
  url: z.string().url().optional().describe('Direct download URL for the asset file'),
  sketchfabUid: z
    .string()
    .optional()
    .describe('Sketchfab model UID — resolves the download URL automatically'),
  targetDirectory: z
    .string()
    .optional()
    .default('Assets/Imports')
    .describe('Asset directory inside the Unity project'),
  fileName: z
    .string()
    .optional()
    .describe('File name for the imported asset. Derived from the URL if omitted.'),
};

/**
 * Download a URL to a local temp file and return the path.
 */
async function downloadToTemp(url: string, fileName: string): Promise<string> {
  await mkdir(TEMP_DIR, { recursive: true });
  const destPath = join(TEMP_DIR, fileName);

  const response = await fetch(url);
  if (!response.ok) {
    throw new Error(`Download failed: HTTP ${response.status} ${response.statusText}`);
  }
  if (!response.body) {
    throw new Error('Download failed: empty response body');
  }

  const nodeStream = Readable.fromWeb(response.body as import('stream/web').ReadableStream);
  const fileStream = createWriteStream(destPath);
  await pipeline(nodeStream, fileStream);

  return destPath;
}

/**
 * Resolve a Sketchfab UID to a download URL, download the ZIP, and extract the
 * first .gltf or .glb file found inside.
 */
async function downloadFromSketchfab(uid: string): Promise<{ path: string; fileName: string }> {
  const apiKey = process.env.SKETCHFAB_API_KEY;
  if (!apiKey) {
    throw new Error(
      'SKETCHFAB_API_KEY environment variable is not set. ' +
        'Get a free API key at https://sketchfab.com/settings/password',
    );
  }

  // Step 1: Request download link
  const dlResponse = await fetch(`https://api.sketchfab.com/v3/models/${uid}/download`, {
    headers: { Authorization: `Token ${apiKey}` },
  });
  if (!dlResponse.ok) {
    throw new Error(`Sketchfab download API error: HTTP ${dlResponse.status} ${dlResponse.statusText}`);
  }
  const dlData = (await dlResponse.json()) as Record<string, { url: string }>;

  // Prefer glTF format
  const format = dlData['gltf'] ?? dlData['glb'] ?? Object.values(dlData)[0];
  if (!format?.url) {
    throw new Error('Sketchfab returned no downloadable format for this model.');
  }

  // Step 2: Download ZIP
  const zipPath = await downloadToTemp(format.url, `${uid}.zip`);

  // Step 3: Extract using yauzl
  const extractDir = join(TEMP_DIR, `${uid}-extracted`);
  await mkdir(extractDir, { recursive: true });

  const yauzl = await import('yauzl');
  const modelFile = await new Promise<string>((resolve, reject) => {
    yauzl.open(zipPath, { lazyEntries: true }, (err, zipfile) => {
      if (err || !zipfile) return reject(err ?? new Error('Failed to open ZIP'));

      let found: string | null = null;

      zipfile.readEntry();
      zipfile.on('entry', (entry) => {
        const ext = extname(entry.fileName).toLowerCase();
        if ((ext === '.glb' || ext === '.gltf') && !found) {
          found = entry.fileName;
          const outPath = join(extractDir, basename(entry.fileName));
          zipfile.openReadStream(entry, (rsErr, readStream) => {
            if (rsErr || !readStream) return reject(rsErr ?? new Error('Failed to read ZIP entry'));
            const ws = createWriteStream(outPath);
            readStream.pipe(ws);
            ws.on('finish', () => {
              resolve(outPath);
              zipfile.close();
            });
            ws.on('error', reject);
          });
        } else {
          zipfile.readEntry();
        }
      });

      zipfile.on('end', () => {
        if (!found) reject(new Error('No .gltf or .glb file found in Sketchfab ZIP'));
      });

      zipfile.on('error', reject);
    });
  });

  // Clean up ZIP
  await rm(zipPath, { force: true });

  return { path: modelFile, fileName: basename(modelFile) };
}

/**
 * Derive a file name from a URL if one wasn't provided.
 */
function fileNameFromUrl(url: string): string {
  try {
    const pathname = new URL(url).pathname;
    const name = basename(pathname);
    // If URL has no extension or looks like a query-only path, fall back
    if (name && extname(name)) return name;
  } catch {
    // ignore
  }
  return `imported-asset-${Date.now()}`;
}

export function registerImportAsset(server: McpServer, connection: UnityConnection): void {
  server.tool(
    'import_asset',
    'Download a file from a URL (or Sketchfab UID) and import it into the Unity project.',
    ImportAssetSchema,
    async (params) => {
      const { url, sketchfabUid, targetDirectory, fileName: userFileName } = params;

      if (!url && !sketchfabUid) {
        return {
          content: [{ type: 'text' as const, text: 'Error: provide either `url` or `sketchfabUid`.' }],
          isError: true,
        };
      }

      try {
        let sourcePath: string;
        let resolvedFileName: string;

        if (sketchfabUid) {
          const result = await downloadFromSketchfab(sketchfabUid);
          sourcePath = result.path;
          resolvedFileName = userFileName ?? result.fileName;
        } else {
          resolvedFileName = userFileName ?? fileNameFromUrl(url!);
          sourcePath = await downloadToTemp(url!, resolvedFileName);
        }

        // Send to Unity for file placement and AssetDatabase import
        const result = await connection.sendRequest('import_asset', {
          sourcePath,
          targetDirectory,
          fileName: resolvedFileName,
        });

        const text = [
          result.message,
          '',
          `Spoken summary: ${result.spokenSummary}`,
          ...(result.data ? [`\nData: ${JSON.stringify(result.data, null, 2)}`] : []),
        ].join('\n');

        return { content: [{ type: 'text' as const, text }] };
      } catch (err: unknown) {
        const msg = err instanceof Error ? err.message : String(err);
        return {
          content: [{ type: 'text' as const, text: `Import failed: ${msg}` }],
          isError: true,
        };
      }
    },
  );
}
