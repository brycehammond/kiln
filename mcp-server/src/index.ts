/**
 * Main entry point for the Unity DevFramework MCP server.
 *
 * This Node.js process communicates with Claude Code over stdio (MCP transport)
 * and with the Unity Editor over a WebSocket bridge.
 */

import { McpServer } from '@modelcontextprotocol/sdk/server/mcp.js';
import { StdioServerTransport } from '@modelcontextprotocol/sdk/server/stdio.js';

import { UnityConnection } from './unity-bridge/connection.js';
import { registerCreateGameObject } from './tools/createGameObject.js';
import { registerDescribeScene } from './tools/describeScene.js';
import { registerExplainError } from './tools/explainError.js';
import { registerCreateScript } from './tools/createScript.js';
import { registerReadScript } from './tools/readScript.js';
import { registerGetProjectSummary } from './tools/getProjectSummary.js';

// ---------------------------------------------------------------------------
// Bootstrap
// ---------------------------------------------------------------------------

async function main(): Promise<void> {
  const server = new McpServer({
    name: 'devframework',
    version: '0.1.0',
  });

  const connection = new UnityConnection();
  connection.connect();

  // Register all tools.
  registerCreateGameObject(server, connection);
  registerDescribeScene(server, connection);
  registerExplainError(server, connection);
  registerCreateScript(server, connection);
  registerReadScript(server, connection);
  registerGetProjectSummary(server, connection);

  // Start the MCP stdio transport.
  const transport = new StdioServerTransport();
  await server.connect(transport);

  process.stderr.write('[DevFramework] MCP server running on stdio\n');
}

// ---------------------------------------------------------------------------
// Graceful shutdown
// ---------------------------------------------------------------------------

function shutdown(): void {
  process.stderr.write('[DevFramework] Shutting down...\n');
  process.exit(0);
}

process.on('SIGINT', shutdown);
process.on('SIGTERM', shutdown);

// ---------------------------------------------------------------------------
// Run
// ---------------------------------------------------------------------------

main().catch((err) => {
  process.stderr.write(`[DevFramework] Fatal error: ${err}\n`);
  process.exit(1);
});
