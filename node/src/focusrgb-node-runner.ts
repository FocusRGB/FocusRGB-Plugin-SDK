#!/usr/bin/env node

import { resolve } from "node:path";
import { pathToFileURL } from "node:url";
import type { FocusRgbPlugin } from "./contracts.js";
import { PluginServer } from "./plugin-server.js";
import { assertSupportedNodeVersion } from "./runtime.js";

async function main(): Promise<void> {
  assertSupportedNodeVersion();

  const entryPoint = process.argv[2];
  if (!entryPoint || process.argv.length !== 3) {
    throw new Error("Usage: focusrgb-node-runner <entry-point>");
  }

  const module = await import(pathToFileURL(resolve(entryPoint)).href) as {
    default?: unknown;
    plugin?: unknown;
  };
  const plugin = module.default ?? module.plugin;
  if (!isPlugin(plugin)) {
    throw new Error("The entry point must export a FocusRgbPlugin as default or 'plugin'.");
  }

  await new PluginServer(plugin).run();
}

function isPlugin(value: unknown): value is FocusRgbPlugin {
  if (typeof value !== "object" || value === null) {
    return false;
  }
  const candidate = value as Record<string, unknown>;
  return ["initialize", "getStatus", "listCatalog", "applyBinding", "releaseBinding", "shutdown"]
    .every((method) => typeof candidate[method] === "function");
}

main().catch((error: unknown) => {
  console.error(error instanceof Error ? error.stack ?? error.message : String(error));
  process.exitCode = 1;
});
