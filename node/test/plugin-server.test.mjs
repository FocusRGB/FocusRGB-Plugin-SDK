import assert from "node:assert/strict";
import { PassThrough } from "node:stream";
import test from "node:test";
import { PluginServer } from "../dist/index.js";

test("initialize adds SDK identity without dropping plugin fields", async () => {
  const session = start(plugin());
  session.send({
    jsonrpc: "2.0",
    id: 1,
    method: "initialize",
    params: { contractVersion: "1.1", host: { name: "test", version: "1.0.0" } },
  });

  const response = await session.next();
  assert.deepEqual(response.result, {
    pluginId: "node-test",
    pluginVersion: "1.0.0",
    contractVersion: "1.1",
    capabilities: [{ id: "provider.effect.catalog", support: "supported" }],
    sdk: { name: "@focusrgb/plugin-sdk", language: "node", version: "1.0.0" },
  });

  await session.shutdown();
});

test("cancel aborts only the matching request", async () => {
  const implementation = plugin();
  implementation.getStatus = (signal) => new Promise((_resolve, reject) => {
    signal.addEventListener("abort", () => reject(new DOMException("cancelled", "AbortError")), { once: true });
  });
  const session = start(implementation);
  session.send({ jsonrpc: "2.0", id: 9, method: "status/get", params: {} });
  session.send({ jsonrpc: "2.0", method: "$/cancel", params: { id: 9 } });

  const response = await session.next();
  assert.equal(response.id, 9);
  assert.deepEqual(response.error, { code: -32800, message: "Request cancelled." });

  await session.shutdown();
});

test("unknown methods return the standard JSON-RPC error", async () => {
  const session = start(plugin());
  session.send({ jsonrpc: "2.0", id: 2, method: "unknown", params: {} });

  const response = await session.next();
  assert.deepEqual(response.error, { code: -32601, message: "Unknown method 'unknown'." });

  await session.shutdown();
});

test("plugin TypeErrors remain internal errors", async () => {
  const implementation = plugin();
  implementation.getStatus = () => { throw new TypeError("private implementation detail"); };
  const session = start(implementation);
  session.send({ jsonrpc: "2.0", id: 3, method: "status/get", params: {} });

  const response = await session.next();
  assert.deepEqual(response.error, { code: -32603, message: "Plugin request failed." });

  await session.shutdown();
});

test("signed 64-bit request IDs are echoed exactly", async () => {
  const session = start(plugin());
  session.sendRaw('{"jsonrpc":"2.0","id":9223372036854775807,"method":"status/get","params":{}}');

  const line = await session.nextRaw();
  assert.match(line, /"id":9223372036854775807/);

  await session.shutdown();
});

function plugin() {
  return {
    initialize: async () => ({
      pluginId: "node-test",
      pluginVersion: "1.0.0",
      contractVersion: "1.1",
      capabilities: [{ id: "provider.effect.catalog", support: "supported" }],
    }),
    getStatus: async () => ({ availability: "available" }),
    listCatalog: async () => ({ support: "unsupported", items: [] }),
    applyBinding: async () => ({ outcome: "unsupported" }),
    releaseBinding: async () => ({ outcome: "released" }),
    shutdown: async () => {},
  };
}

function start(implementation) {
  const input = new PassThrough();
  const output = new PassThrough();
  const error = new PassThrough();
  const lines = [];
  const waiters = [];
  let buffer = "";
  output.setEncoding("utf8");
  output.on("data", (chunk) => {
    buffer += chunk;
    while (buffer.includes("\n")) {
      const index = buffer.indexOf("\n");
      const line = buffer.slice(0, index);
      buffer = buffer.slice(index + 1);
      const waiter = waiters.shift();
      if (waiter) waiter(line);
      else lines.push(line);
    }
  });
  const running = new PluginServer(implementation, { input, output, error }).run();
  return {
    send(value) { input.write(`${JSON.stringify(value)}\n`); },
    sendRaw(value) { input.write(`${value}\n`); },
    async nextRaw() {
      if (lines.length) return lines.shift();
      return Promise.race([
        new Promise((resolve) => waiters.push(resolve)),
        new Promise((_resolve, reject) => setTimeout(() => reject(new Error("response timed out")), 1000)),
      ]);
    },
    async next() { return JSON.parse(await this.nextRaw()); },
    async shutdown() {
      this.send({ jsonrpc: "2.0", id: 100, method: "shutdown", params: {} });
      await this.next();
      input.end();
      await running;
    },
  };
}
