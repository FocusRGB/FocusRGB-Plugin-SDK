import type {
  FocusRgbPlugin,
  PluginResourceReference,
} from "@focusrgb/plugin-sdk";

const effect = { resourceId: "node-static-blue", displayName: "Static Blue" };
let current: PluginResourceReference | undefined;

const plugin = {
  initialize(parameters) {
    return {
      pluginId: "node-example",
      pluginVersion: "1.0.0",
      contractVersion: negotiate(parameters.contractVersion),
      capabilities: [
        { id: "provider.effect.catalog", support: "supported" },
        { id: "provider.effect.select", support: "supported" },
        {
          id: "provider.layout.catalog",
          support: "unsupported",
          reason: { code: "capability-unsupported", message: "This example has no layouts." },
        },
      ],
    };
  },
  getStatus() {
    return current
      ? { availability: "available", current: { effect: current } }
      : { availability: "available" };
  },
  listCatalog(parameters) {
    return parameters.kind === "effect"
      ? { support: "supported", items: [{ ...effect, available: true }] }
      : {
          support: "unsupported",
          items: [],
          reason: { code: "capability-unsupported", message: "This example has no layouts." },
        };
  },
  applyBinding(parameters) {
    current = parameters.effect;
    return { outcome: "applied", actual: current ? { effect: current } : {} };
  },
  releaseBinding() {
    current = undefined;
    return { outcome: "released" };
  },
  shutdown() {},
} satisfies FocusRgbPlugin;

function negotiate(offered: string): string {
  const match = /^(\d+)\.(\d+)$/.exec(offered);
  if (!match || match[1] !== "1") {
    throw new TypeError("Contract major 1 is required.");
  }
  return `1.${Math.min(Number(match[2]), 1)}`;
}

export default plugin;
