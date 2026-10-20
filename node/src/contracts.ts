export const ContractVersion = {
  major: 1,
  minor: 1,
  current: "1.1",
} as const;

export const PluginMethods = {
  initialize: "initialize",
  statusGet: "status/get",
  catalogList: "catalog/list",
  bindingApply: "binding/apply",
  bindingRelease: "binding/release",
  shutdown: "shutdown",
  statusChanged: "status/changed",
  cancel: "$/cancel",
} as const;

export type Support = "supported" | "unverified" | "unsupported";
export type CatalogKind = "effect" | "layout";

export interface PluginManifest {
  schemaVersion: 1;
  id: string;
  version: string;
  contractVersion: string;
  entryPoint: string;
  runtime?: "node" | "python";
  operatingSystems: Array<"windows" | "linux" | "macos">;
  architectures: Array<"x64" | "arm64">;
  roles: Array<"detector" | "discovery" | "integration" | "output" | "effect">;
  resources: Array<"process.read" | "process.start" | "filesystem.read">;
  license: string;
  source?: string;
}

export interface PluginHostInfo {
  name: string;
  version: string;
}

export interface InitializeParameters {
  contractVersion: string;
  host: PluginHostInfo;
}

export interface InitializeResult {
  pluginId: string;
  pluginVersion: string;
  contractVersion: string;
  capabilities: PluginCapability[];
  sdk?: PluginSdkInfo;
}

export interface PluginSdkInfo {
  name: string;
  language: string;
  version: string;
}

export interface PluginCapability {
  id: string;
  support: Support;
  reason?: PluginReason;
}

export interface PluginReason {
  code: string;
  message: string;
}

export interface PluginStatusResult {
  availability: string;
  reason?: PluginReason;
  current?: PluginSelection;
}

export interface PluginSelection {
  effect?: PluginResourceReference;
  layout?: PluginResourceReference;
}

export interface PluginResourceReference {
  resourceId?: string;
  displayName: string;
}

export interface CatalogListParameters {
  kind: CatalogKind;
}

export interface PluginCatalogResult {
  support: Support;
  refreshedAt?: string;
  items: PluginCatalogItem[];
  reason?: PluginReason;
}

export interface PluginCatalogItem {
  resourceId: string;
  displayName: string;
  available: boolean;
  reason?: PluginReason;
}

export interface PluginDeviceScope {
  kind: string;
  deviceIds?: string[];
}

export interface BindingApplyParameters {
  profile: string;
  bindingId: string;
  renderingAuthority: string;
  deviceScope: PluginDeviceScope;
  effect?: PluginResourceReference;
  layout?: PluginResourceReference;
}

export interface BindingApplyResult {
  outcome: string;
  reason?: PluginReason;
  actual?: PluginSelection;
}

export interface BindingReleaseParameters {
  profile: string;
  bindingId: string;
}

export interface BindingReleaseResult {
  outcome: string;
}

export interface CancelParameters {
  id: number | bigint;
}

export interface PluginHost {
  notifyStatusChanged(): Promise<void>;
  log(message: string): Promise<void>;
}

export type MaybePromise<T> = T | Promise<T>;

export interface FocusRgbPlugin {
  connect?(host: PluginHost): void;
  initialize(parameters: InitializeParameters, signal: AbortSignal): MaybePromise<InitializeResult>;
  getStatus(signal: AbortSignal): MaybePromise<PluginStatusResult>;
  listCatalog(parameters: CatalogListParameters, signal: AbortSignal): MaybePromise<PluginCatalogResult>;
  applyBinding(parameters: BindingApplyParameters, signal: AbortSignal): MaybePromise<BindingApplyResult>;
  releaseBinding(parameters: BindingReleaseParameters, signal: AbortSignal): MaybePromise<BindingReleaseResult>;
  shutdown(signal: AbortSignal): MaybePromise<void>;
}
