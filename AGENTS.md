# Working in FocusRGB-Plugin-SDK

This repository holds the official FocusRGB plugin SDKs. The protocol they implement is **not**
defined here. It lives in the main repository under `sdk/spec/`, which holds the normative spec
and JSON schemas, together with the language-neutral conformance kit. Never change wire behaviour
here first: change the spec in `FocusRGB/FocusRGB`, tag it, then update the SDKs against that tag.

Follow the repository workflow, attribution rules, conventions and slice documentation from the
peer `FocusRGB/FocusRGB` repository (`docs/git-workflow.md`, `.ai/AGENTS.md`). Those instructions
take precedence over assistant skills when they conflict. Branch from `develop`, open pull
requests into `develop`, and end commits written with an assistant with an `Assisted-by:` trailer.

Rules specific to this repository:

- Publish one package per language: `FocusRgb.PluginSdk` (nuget.org), `@focusrgb/plugin-sdk`
  (npm) and `focusrgb-plugin-sdk` (PyPI). Never publish or tag by hand. Publishing runs only
  through the gated `sdk-<language>-v*` workflows, and only after the owner approves.
- Use Devantix as the author, company, and copyright owner in every .NET, Node, and Python
  package's metadata; the FocusRGB organization, package names, and domain do not change.
- Each SDK is tested against a **tagged** spec release from the main repository, never against
  its moving `develop` branch.
- An SDK reports its identity in the `initialize` result (`sdk { name, language, version }`),
  for diagnostics only.

The code graph is workspace-wide at the workspace folder's `build/graphify-out/` (the parent of
this clone; see the workspace `AGENTS.md`). Query from the workspace folder, or from the repository
root pass `--graph ../build/graphify-out/graph.json`. Confirm graph findings in source because the
graph can lag. Never run `graphify update` inside a clone; run it only from the workspace folder.
Never run `graphify hook install` in this repo. Never run `graphify extract` or semantic/deep modes
unless the owner asks because they call an LLM. Never commit graph output.

Assisted-by: Claude
