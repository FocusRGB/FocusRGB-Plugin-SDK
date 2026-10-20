import type {
  BindingApplyParameters,
  BindingApplyResult,
  BindingReleaseParameters,
  BindingReleaseResult,
  CancelParameters,
  CatalogListParameters,
  InitializeParameters,
  InitializeResult,
  PluginCapability,
  PluginCatalogItem,
  PluginCatalogResult,
  PluginDeviceScope,
  PluginHostInfo,
  PluginManifest,
  PluginReason,
  PluginResourceReference,
  PluginSdkInfo,
  PluginSelection,
  PluginStatusResult,
} from "./contracts.js";

function defineShape<T>() {
  return <
    Required extends readonly (keyof T & string)[],
    Optional extends readonly (keyof T & string)[],
  >(
    required: Required,
    optional: Optional,
    types: { [Property in keyof T]-?: SchemaKind<T[Property]> },
    ...complete: Exclude<keyof T, Required[number] | Optional[number]> extends never
      ? []
      : ["Shape is missing a property", Exclude<keyof T, Required[number] | Optional[number]>]
  ) => ({ required, optional, types });
}

type SchemaKind<T> = NonNullable<T> extends string
  ? "string"
  : NonNullable<T> extends number | bigint
    ? "integer"
    : NonNullable<T> extends boolean
      ? "boolean"
      : NonNullable<T> extends readonly unknown[]
        ? "array"
        : NonNullable<T> extends object
          ? "object"
          : never;

export const protocolShapes = {
  pluginHostInfo: defineShape<PluginHostInfo>()(
    ["name", "version"], [], { name: "string", version: "string" },
  ),
  initializeParameters: defineShape<InitializeParameters>()(
    ["contractVersion", "host"], [], { contractVersion: "string", host: "object" },
  ),
  initializeResult: defineShape<InitializeResult>()(
    ["pluginId", "pluginVersion", "contractVersion", "capabilities"],
    ["sdk"],
    {
      pluginId: "string",
      pluginVersion: "string",
      contractVersion: "string",
      capabilities: "array",
      sdk: "object",
    },
  ),
  pluginSdkInfo: defineShape<PluginSdkInfo>()(
    ["name", "language", "version"], [], { name: "string", language: "string", version: "string" },
  ),
  pluginCapability: defineShape<PluginCapability>()(
    ["id", "support"], ["reason"], { id: "string", support: "string", reason: "object" },
  ),
  pluginReason: defineShape<PluginReason>()(
    ["code", "message"], [], { code: "string", message: "string" },
  ),
  pluginStatusResult: defineShape<PluginStatusResult>()(
    ["availability"], ["reason", "current"],
    { availability: "string", reason: "object", current: "object" },
  ),
  pluginSelection: defineShape<PluginSelection>()(
    [], ["effect", "layout"], { effect: "object", layout: "object" },
  ),
  pluginResourceReference: defineShape<PluginResourceReference>()(
    ["displayName"], ["resourceId"], { resourceId: "string", displayName: "string" },
  ),
  catalogListParameters: defineShape<CatalogListParameters>()(
    ["kind"], [], { kind: "string" },
  ),
  pluginCatalogResult: defineShape<PluginCatalogResult>()(
    ["support", "items"], ["refreshedAt", "reason"],
    { support: "string", refreshedAt: "string", items: "array", reason: "object" },
  ),
  pluginCatalogItem: defineShape<PluginCatalogItem>()(
    ["resourceId", "displayName", "available"], ["reason"],
    { resourceId: "string", displayName: "string", available: "boolean", reason: "object" },
  ),
  pluginDeviceScope: defineShape<PluginDeviceScope>()(
    ["kind"], ["deviceIds"], { kind: "string", deviceIds: "array" },
  ),
  bindingApplyParameters: defineShape<BindingApplyParameters>()(
    ["profile", "bindingId", "renderingAuthority", "deviceScope"],
    ["effect", "layout"],
    {
      profile: "string",
      bindingId: "string",
      renderingAuthority: "string",
      deviceScope: "object",
      effect: "object",
      layout: "object",
    },
  ),
  bindingApplyResult: defineShape<BindingApplyResult>()(
    ["outcome"], ["reason", "actual"], { outcome: "string", reason: "object", actual: "object" },
  ),
  bindingReleaseParameters: defineShape<BindingReleaseParameters>()(
    ["profile", "bindingId"], [], { profile: "string", bindingId: "string" },
  ),
  bindingReleaseResult: defineShape<BindingReleaseResult>()(
    ["outcome"], [], { outcome: "string" },
  ),
  cancelParameters: defineShape<CancelParameters>()(["id"], [], { id: "integer" }),
} as const;

export const manifestShape = defineShape<PluginManifest>()(
  [
    "schemaVersion",
    "id",
    "version",
    "contractVersion",
    "entryPoint",
    "operatingSystems",
    "architectures",
    "roles",
    "resources",
    "license",
  ],
  ["runtime", "source"],
  {
    schemaVersion: "integer",
    id: "string",
    version: "string",
    contractVersion: "string",
    entryPoint: "string",
    runtime: "string",
    operatingSystems: "array",
    architectures: "array",
    roles: "array",
    resources: "array",
    license: "string",
    source: "string",
  },
);
