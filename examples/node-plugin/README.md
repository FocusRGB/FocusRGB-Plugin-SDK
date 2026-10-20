# Node plugin example

This complete TypeScript plugin uses `@focusrgb/plugin-sdk` and runs through the official
`focusrgb-node-runner`. From the SDK repository root:

```powershell
npm run build:node
node node/dist/focusrgb-node-runner.js examples/node-plugin/dist/plugin.js
```

The process then expects newline-delimited FocusRGB JSON-RPC on standard input.
