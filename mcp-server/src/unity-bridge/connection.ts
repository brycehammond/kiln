/**
 * WebSocket client that connects to the Unity Editor and bridges
 * MCP tool calls to Unity via JSON request/response correlation.
 */

import WebSocket from 'ws';
import { v4 as uuidv4 } from 'uuid';
import type { UnityRequest, UnityResponse, ToolResult } from './types.js';

/** Default WebSocket endpoint exposed by the Unity Editor bridge. */
const UNITY_WS_URL = 'ws://localhost:8091';

/** Maximum time (ms) to wait for a response before rejecting. */
const REQUEST_TIMEOUT_MS = 30_000;

/** Maximum reconnect backoff delay (ms). */
const MAX_RECONNECT_DELAY_MS = 30_000;

interface PendingRequest {
  resolve: (result: ToolResult) => void;
  reject: (error: Error) => void;
  timer: ReturnType<typeof setTimeout>;
}

export class UnityConnection {
  private ws: WebSocket | null = null;
  private connected = false;
  private closing = false;
  private reconnectDelay = 1000;
  private reconnectTimer: ReturnType<typeof setTimeout> | null = null;
  private pendingRequests: Map<string, PendingRequest> = new Map();

  private readonly url: string;

  constructor(url: string = UNITY_WS_URL) {
    this.url = url;
  }

  // ---------------------------------------------------------------------------
  // Public API
  // ---------------------------------------------------------------------------

  /** Start the WebSocket connection (non-blocking). */
  connect(): void {
    this.closing = false;
    this.attemptConnection();
  }

  /** Gracefully close the connection and stop reconnect attempts. */
  close(): void {
    this.closing = true;

    if (this.reconnectTimer) {
      clearTimeout(this.reconnectTimer);
      this.reconnectTimer = null;
    }

    // Reject all pending requests.
    for (const [id, pending] of this.pendingRequests) {
      clearTimeout(pending.timer);
      pending.reject(new Error('Connection closed'));
      this.pendingRequests.delete(id);
    }

    if (this.ws) {
      this.ws.close();
      this.ws = null;
    }

    this.connected = false;
  }

  /** Whether the WebSocket is currently open and ready. */
  isConnected(): boolean {
    return this.connected;
  }

  /**
   * Send a request to Unity and wait for the correlated response.
   *
   * @param method  The Unity method name (e.g. "create_gameobject").
   * @param params  Arbitrary parameters forwarded to Unity.
   * @returns       The {@link ToolResult} from Unity.
   */
  sendRequest(method: string, params: Record<string, unknown>): Promise<ToolResult> {
    return new Promise<ToolResult>((resolve, reject) => {
      if (!this.ws || !this.connected) {
        reject(new Error('Not connected to Unity Editor. Is the Unity project open with the Kiln package installed?'));
        return;
      }

      const id = uuidv4();

      const timer = setTimeout(() => {
        this.pendingRequests.delete(id);
        reject(new Error(`Request "${method}" timed out after ${REQUEST_TIMEOUT_MS / 1000}s`));
      }, REQUEST_TIMEOUT_MS);

      this.pendingRequests.set(id, { resolve, reject, timer });

      const request: UnityRequest = { id, method, params };

      this.ws.send(JSON.stringify(request), (err) => {
        if (err) {
          clearTimeout(timer);
          this.pendingRequests.delete(id);
          reject(new Error(`Failed to send request: ${err.message}`));
        }
      });
    });
  }

  // ---------------------------------------------------------------------------
  // Internal helpers
  // ---------------------------------------------------------------------------

  private attemptConnection(): void {
    if (this.closing) return;

    this.log('Connecting to Unity Editor at', this.url);

    const ws = new WebSocket(this.url);

    ws.on('open', () => {
      this.ws = ws;
      this.connected = true;
      this.reconnectDelay = 1000; // reset backoff
      this.log('Connected to Unity Editor');
    });

    ws.on('message', (data: WebSocket.RawData) => {
      this.handleMessage(data);
    });

    ws.on('close', () => {
      const wasConnected = this.connected;
      this.connected = false;
      this.ws = null;

      if (wasConnected) {
        this.log('Disconnected from Unity Editor');
      }

      this.scheduleReconnect();
    });

    ws.on('error', (err: Error) => {
      // Suppress noisy ECONNREFUSED during reconnect cycles.
      if (!this.connected) {
        this.log('Connection attempt failed:', err.message);
      } else {
        this.log('WebSocket error:', err.message);
      }

      // `close` event will fire after `error`, so reconnect is handled there.
    });
  }

  private handleMessage(data: WebSocket.RawData): void {
    let response: UnityResponse;

    try {
      response = JSON.parse(data.toString()) as UnityResponse;
    } catch {
      this.log('Received non-JSON message from Unity, ignoring');
      return;
    }

    const pending = this.pendingRequests.get(response.id);
    if (!pending) {
      // Could be a stale or unsolicited message.
      return;
    }

    clearTimeout(pending.timer);
    this.pendingRequests.delete(response.id);

    if (response.error) {
      pending.reject(
        new Error(`Unity error ${response.error.code}: ${response.error.message}`),
      );
    } else if (response.result) {
      pending.resolve(response.result);
    } else {
      pending.reject(new Error('Unity response contained neither result nor error'));
    }
  }

  private scheduleReconnect(): void {
    if (this.closing) return;

    this.log(`Reconnecting in ${this.reconnectDelay / 1000}s...`);

    this.reconnectTimer = setTimeout(() => {
      this.reconnectTimer = null;
      this.attemptConnection();
    }, this.reconnectDelay);

    // Exponential backoff capped at MAX_RECONNECT_DELAY_MS.
    this.reconnectDelay = Math.min(this.reconnectDelay * 2, MAX_RECONNECT_DELAY_MS);
  }

  /** Write to stderr so we never pollute the stdio MCP transport on stdout. */
  private log(...args: unknown[]): void {
    process.stderr.write(`[UnityConnection] ${args.join(' ')}\n`);
  }
}
