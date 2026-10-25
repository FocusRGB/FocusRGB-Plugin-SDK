# FocusRgb.PluginSdk

Optional .NET helpers for implementing a FocusRGB process plugin. Plugins in other languages can
implement the newline-delimited JSON-RPC protocol directly.

The package includes the public protocol message types in the `FocusRgb.Contracts` namespace, so
plugin projects need only this package.

```csharp
await PluginServer.RunAsync(
    new MyPlugin(),
    Console.In,
    Console.Out,
    Console.Error);
```

Implement `IFocusRgbPlugin`, write protocol messages only through `PluginServer`, and keep logs on
standard error. The language-neutral protocol and schemas are documented at
<https://github.com/FocusRGB/FocusRGB/blob/spec-v1.3.0/sdk/spec/plugin-protocol.md>.
`PluginServer` automatically reports `FocusRgb.PluginSdk`, `dotnet`, and its package version in
the optional diagnostic SDK identity; plugin implementations do not set that field themselves.

Protocol 1.3 plugins can register self-describing telemetry through the fluent helpers:

```csharp
var item = TelemetryDescriptor.Input(
        new GameReference { Launcher = "example", Id = "sample-game" },
        new TelemetrySectionDescriptor { Id = "status", DisplayName = "Status", Order = 0 },
        TelemetryValueType.Number,
        "Current synthetic level.")
    .WithRange(0, 100)
    .ToCatalogItem("example.status.level", "Level");
```

Publish full telemetry snapshots through `IPluginHost.PublishGameTelemetryAsync`. Output plugins
override `IFocusRgbPlugin.UpdateEffectInputsAsync` to consume mapped values and events; its default
implementation returns `unsupported`, so existing 1.2 implementations remain source-compatible.
`PluginJson.Serialize` and `PluginJson.Deserialize` use the exact SDK wire settings.
