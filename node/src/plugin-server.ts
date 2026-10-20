import { createInterface } from "node:readline";
import type { Readable, Writable } from "node:stream";
import {
  PluginMethods,
  type BindingApplyParameters,
  type BindingReleaseParameters,
  type CancelParameters,
  type CatalogListParameters,
  type FocusRgbPlugin,
  type InitializeParameters,
  type PluginHost,
} from "./contracts.js";

const MAX_LINE_BYTES = 1024 * 1024;
const SDK_VERSION = "1.0.0";
type RpcId = number | bigint;

interface RpcRequest {
  jsonrpc?: unknown;
  id?: unknown;
  method?: unknown;
  params?: unknown;
}

interface JsonWithRawNumbers {
  rawJSON(value: string): unknown;
}

export interface PluginServerStreams {
  input?: Readable;
  output?: Writable;
  error?: Writable;
}

export class PluginServer {
  readonly #plugin: FocusRgbPlugin;
  readonly #input: Readable;
  readonly #output: Writable;
  readonly #error: Writable;
  readonly #pending = new Map<RpcId, AbortController>();
  readonly #tasks = new Set<Promise<void>>();
  #outputQueue = Promise.resolve();
  #errorQueue = Promise.resolve();

  public constructor(plugin: FocusRgbPlugin, streams: PluginServerStreams = {}) {
    this.#plugin = plugin;
    this.#input = streams.input ?? process.stdin;
    this.#output = streams.output ?? process.stdout;
    this.#error = streams.error ?? process.stderr;
    this.#plugin.connect?.(this.#host());
  }

  public async run(): Promise<void> {
    const lines = createInterface({ input: this.#input, crlfDelay: Infinity });
    try {
      for await (const line of lines) {
        if (Buffer.byteLength(line, "utf8") > MAX_LINE_BYTES) {
          await this.#writeError(null, -32600, "JSON-RPC request exceeded 1 MiB.");
          continue;
        }

        let request: RpcRequest;
        try {
          request = parseRequest(line);
        } catch (error) {
          const code = error instanceof RpcError ? error.code : -32700;
          await this.#writeError(null, code, errorMessage(error));
          continue;
        }

        if (request.jsonrpc !== "2.0" || typeof request.method !== "string" || !request.method) {
          await this.#writeError(null, -32600, "Invalid JSON-RPC request.");
          continue;
        }

        if (request.method === PluginMethods.cancel) {
          const parameters = request.params as Partial<CancelParameters> | undefined;
          if (isRpcId(parameters?.id)) {
            this.#pending.get(parameters.id)?.abort();
          }
          continue;
        }

        if (!isRpcId(request.id)) {
          await this.#writeError(null, -32600, "Invalid JSON-RPC request.");
          continue;
        }

        const id = request.id;
        const controller = new AbortController();
        this.#pending.set(id, controller);
        const task = this.#handle(id, request.method, request.params, controller.signal)
          .finally(() => {
            this.#pending.delete(id);
            this.#tasks.delete(task);
          });
        this.#tasks.add(task);

        if (request.method === PluginMethods.shutdown) {
          await task;
          break;
        }

        void task.catch((error: unknown) => this.#writeLog(errorMessage(error)));
      }
    } finally {
      lines.close();
      for (const controller of this.#pending.values()) {
        controller.abort();
      }
      await Promise.allSettled(this.#tasks);
    }
  }

  async #handle(id: RpcId, method: string, rawParameters: unknown, signal: AbortSignal): Promise<void> {
    try {
      const parameters = requireParameters(rawParameters);
      let result: object;
      switch (method) {
        case PluginMethods.initialize: {
          const initialized = await this.#plugin.initialize(parameters as unknown as InitializeParameters, signal);
          result = {
            ...initialized,
            sdk: { name: "@focusrgb/plugin-sdk", language: "node", version: SDK_VERSION },
          };
          break;
        }
        case PluginMethods.statusGet:
          result = await this.#plugin.getStatus(signal);
          break;
        case PluginMethods.catalogList:
          result = await this.#plugin.listCatalog(parameters as unknown as CatalogListParameters, signal);
          break;
        case PluginMethods.bindingApply:
          result = await this.#plugin.applyBinding(parameters as unknown as BindingApplyParameters, signal);
          break;
        case PluginMethods.bindingRelease:
          result = await this.#plugin.releaseBinding(parameters as unknown as BindingReleaseParameters, signal);
          break;
        case PluginMethods.shutdown:
          await this.#plugin.shutdown(signal);
          result = {};
          break;
        default:
          throw new RpcError(-32601, `Unknown method '${method}'.`);
      }
      await this.#write({ jsonrpc: "2.0", id, result });
    } catch (error) {
      if (signal.aborted || isAbortError(error)) {
        await this.#writeError(id, -32800, "Request cancelled.");
      } else if (error instanceof RpcError) {
        await this.#writeError(id, error.code, error.message);
      } else {
        await this.#writeLog(error instanceof Error ? error.stack ?? error.message : String(error));
        await this.#writeError(id, -32603, "Plugin request failed.");
      }
    }
  }

  #host(): PluginHost {
    return {
      notifyStatusChanged: () => this.#write({
        jsonrpc: "2.0",
        method: PluginMethods.statusChanged,
        params: {},
      }),
      log: (message: string) => {
        if (!message.trim()) {
          return Promise.reject(new TypeError("Log message must not be empty."));
        }
        return this.#writeLog(message);
      },
    };
  }

  #writeError(id: RpcId | null, code: number, message: string): Promise<void> {
    return this.#write({ jsonrpc: "2.0", id, error: { code, message } });
  }

  #write(message: object): Promise<void> {
    const line = stringify(message) + "\n";
    if (Buffer.byteLength(line, "utf8") > MAX_LINE_BYTES) {
      return Promise.reject(new RangeError("JSON-RPC response exceeded 1 MiB."));
    }
    const write = this.#outputQueue.then(() => writeLine(this.#output, line));
    this.#outputQueue = write.catch(() => undefined);
    return write;
  }

  #writeLog(message: string): Promise<void> {
    const write = this.#errorQueue.then(() => writeLine(this.#error, message + "\n"));
    this.#errorQueue = write.catch(() => undefined);
    return write;
  }
}

function parseRequest(line: string): RpcRequest {
  const parse = JSON.parse as (
    text: string,
    reviver: (this: unknown, key: string, value: unknown, context: { source: string }) => unknown,
  ) => unknown;
  const value = parse(line, (key, current, context) =>
    key === "id" && typeof current === "number" && Number.isInteger(current) && !Number.isSafeInteger(current)
      ? BigInt(context.source)
      : current);
  if (typeof value !== "object" || value === null || Array.isArray(value)) {
    throw new RpcError(-32600, "JSON-RPC request must be an object.");
  }
  return value as RpcRequest;
}

function stringify(value: object): string {
  const raw = JSON as unknown as JsonWithRawNumbers;
  return JSON.stringify(value, (_key, current: unknown) =>
    typeof current === "bigint" ? raw.rawJSON(current.toString()) : current);
}

function requireParameters(value: unknown): Record<string, unknown> {
  if (typeof value !== "object" || value === null || Array.isArray(value)) {
    throw new RpcError(-32602, "Request params must be an object.");
  }
  return value as Record<string, unknown>;
}

function isRpcId(value: unknown): value is RpcId {
  if (typeof value === "number") {
    return Number.isSafeInteger(value);
  }
  return typeof value === "bigint"
    && value >= -(1n << 63n)
    && value <= (1n << 63n) - 1n;
}

function isAbortError(error: unknown): boolean {
  return error instanceof Error && error.name === "AbortError";
}

function errorMessage(error: unknown): string {
  return error instanceof Error ? error.message : String(error);
}

function writeLine(stream: Writable, value: string): Promise<void> {
  return new Promise((resolve, reject) => {
    stream.write(value, "utf8", (error) => error ? reject(error) : resolve());
  });
}

class RpcError extends Error {
  public constructor(public readonly code: number, message: string) {
    super(message);
  }
}
