# @focusrgb/plugin-sdk

Official Node.js 22+ SDK for FocusRGB process plugins. It provides typed protocol messages,
`PluginServer`, and the `focusrgb-node-runner` executable.

```ts
import type { FocusRgbPlugin } from "@focusrgb/plugin-sdk";

const plugin = {
  // Implement the six lifecycle methods shown in templates/node-plugin.
} satisfies FocusRgbPlugin;

export default plugin;
```

A runner-based manifest uses contract 1.1 and points at the compiled plugin module:

```json
{
  "contractVersion": "1.1",
  "entryPoint": "dist/plugin.js",
  "runtime": "node"
}
```

FocusRGB resolves this package's runner at `dist/focusrgb-node-runner.js`; do not move it.
The SDK identity is added automatically during initialization and is diagnostic only.
