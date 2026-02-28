/**
 * Shared TypeScript interfaces for the Unity MCP bridge.
 */

/** A request sent from the MCP server to Unity Editor via WebSocket. */
export interface UnityRequest {
  id: string;
  method: string;
  params: Record<string, unknown>;
}

/** A response received from Unity Editor via WebSocket. */
export interface UnityResponse {
  id: string;
  result?: ToolResult;
  error?: { code: number; message: string };
}

/** The result payload returned by a Unity tool invocation. */
export interface ToolResult {
  success: boolean;
  message: string;
  spokenSummary: string;
  data?: Record<string, unknown>;
}

/** A 3-component vector used for position, rotation, and scale. */
export interface Vector3 {
  x: number;
  y: number;
  z: number;
}

/** Specification for a component to attach to a GameObject. */
export interface ComponentSpec {
  type: string;
  properties?: Record<string, unknown>;
}
