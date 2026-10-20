import assert from "node:assert/strict";
import { existsSync, readFileSync } from "node:fs";
import test from "node:test";
import { assertSupportedNodeVersion } from "../dist/index.js";

test("runner rejects Node versions below 22", () => {
  assert.throws(() => assertSupportedNodeVersion("21.9.0"), /Node\.js 22 or later/);
  assert.doesNotThrow(() => assertSupportedNodeVersion("22.0.0"));
});

test("package exposes the runner at the Windows host path", () => {
  const packageJson = JSON.parse(readFileSync(new URL("../package.json", import.meta.url), "utf8"));
  assert.equal(packageJson.bin["focusrgb-node-runner"], "./dist/focusrgb-node-runner.js");
  assert.ok(existsSync(new URL("../dist/focusrgb-node-runner.js", import.meta.url)));
});
