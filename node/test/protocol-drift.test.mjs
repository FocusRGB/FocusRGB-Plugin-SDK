import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import { join } from "node:path";
import test from "node:test";
import { ContractVersion, manifestShape, PluginMethods, protocolShapes } from "../dist/index.js";

const specRoot = process.env.FOCUSRGB_SPEC_ROOT;
assert.ok(specRoot, "FOCUSRGB_SPEC_ROOT must point to FocusRGB tag spec-v1.1.0");
const protocol = readSchema("plugin-protocol.schema.json");
const manifest = readSchema("plugin-manifest.schema.json");

test("contract and schema IDs are pinned to spec-v1.1.0", () => {
  assert.equal(ContractVersion.current, "1.1");
  assert.equal(
    protocol.$id,
    "https://raw.githubusercontent.com/FocusRGB/FocusRGB/spec-v1.1.0/sdk/spec/plugin-protocol.schema.json",
  );
  assert.equal(
    manifest.$id,
    "https://raw.githubusercontent.com/FocusRGB/FocusRGB/spec-v1.1.0/sdk/spec/plugin-manifest.schema.json",
  );
});

test("method constants match the protocol schema", () => {
  const schemaMethods = new Set([
    ...protocol.$defs.request.properties.method.enum,
    ...protocol.$defs.notification.properties.method.enum,
  ]);
  assert.deepEqual(schemaMethods, new Set(Object.values(PluginMethods)));
});

test("typed protocol shapes match the protocol schema", () => {
  for (const [name, shape] of Object.entries(protocolShapes)) {
    assertShape(protocol.$defs[name], shape, name);
  }
});

test("typed manifest shape matches the manifest schema", () => {
  assertShape(manifest, manifestShape, "manifest");
});

function assertShape(schema, shape, name) {
  assert.deepEqual(
    new Set(Object.keys(schema.properties)),
    new Set([...shape.required, ...shape.optional]),
    `${name} properties drifted`,
  );
  assert.deepEqual(new Set(schema.required ?? []), new Set(shape.required), `${name} required fields drifted`);
  for (const [property, expected] of Object.entries(shape.types)) {
    assert.equal(schemaKind(schema.properties[property]), expected, `${name}.${property} type drifted`);
  }
}

function schemaKind(schema) {
  if (schema.$ref) return "object";
  if (Object.hasOwn(schema, "const")) {
    return Number.isInteger(schema.const) ? "integer" : typeof schema.const;
  }
  if (schema.enum) return typeof schema.enum[0];
  return schema.type;
}

function readSchema(name) {
  return JSON.parse(readFileSync(join(specRoot, "sdk", "spec", name), "utf8"));
}
