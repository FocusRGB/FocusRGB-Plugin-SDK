export function assertSupportedNodeVersion(version = process.versions.node): void {
  const major = Number.parseInt(version.split(".")[0] ?? "0", 10);
  if (major < 22) {
    throw new Error("focusrgb-node-runner requires Node.js 22 or later.");
  }
}
