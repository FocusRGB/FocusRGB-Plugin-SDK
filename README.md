# FocusRGB Plugin SDK

<img src="assets/logo-128.png" alt="FocusRGB logo" width="128">

Official SDKs for writing [FocusRGB](https://github.com/FocusRGB/FocusRGB) plugins. A plugin is a
separate process that FocusRGB starts and talks to directly over stdin/stdout using
newline-delimited JSON-RPC. These SDKs implement that protocol so plugin authors don't have to.

The protocol itself is defined in the main repository, not here:
[`sdk/spec/`](https://github.com/FocusRGB/FocusRGB/tree/spec-v1.0.0/sdk/spec) holds the normative
spec and JSON schemas, and its conformance kit checks any plugin in any language. Each SDK here is
tested against a tagged version of that spec.

| Language | Package | Status |
|---|---|---|
| .NET (C#) | `FocusRgb.PluginSdk` on nuget.org | 1.0 being prepared |
| Node.js | `@focusrgb/plugin-sdk` on npm, with `focusrgb-node-runner` | planned |
| Python | `focusrgb-plugin-sdk` on PyPI, with `focusrgb-python-runner` | planned |

> **Migration in progress.** This repository was split out of `FocusRGB/FocusRGB` with its
> history. The .NET SDK, its public protocol types, template, tests and package tooling are here;
> the Node and Python SDKs will follow. See
> [FocusRGB#84](https://github.com/FocusRGB/FocusRGB/issues/84).

## Layout

```text
dotnet/      FocusRgb.PluginSdk (+ its message types) and tests
templates/   dotnet new template for a standalone plugin repository
tools/       package reproducibility and consumer checks
```

Licensed under MIT.

Assisted-by: Claude
Assisted-by: OpenAI Codex
