# NEUTRONSTAR Systems CHAT GPT MCP

A Unity Editor MCP bridge built for **ChatGPT Codex** and other Model Context Protocol clients, with deeper low-level editor access than the upstream package exposes by default.

This repository is based on **CoplayDev/unity-mcp** and retains its MIT license and attribution. The original upstream README is preserved as [`UPSTREAM_README.md`](UPSTREAM_README.md).

## What this fork adds

Alongside the upstream toolset for scenes, GameObjects, components, prefabs, materials, shaders, VFX, physics, animation, UI Toolkit, cameras, builds, packages, profiling, tests, screenshots, and arbitrary C# execution, this fork adds:

- `serialized_object` — inspect and modify arbitrary Unity `SerializedObject` / `SerializedProperty` data by property path.
- `object_identity` — resolve instance IDs, `GlobalObjectId`s, GUIDs, asset paths, and scene hierarchy paths.
- `editor_selection` — read/set/add/clear selection, ping objects, and frame selection in Scene View.
- `asset_relations` — direct dependencies, recursive dependencies, reverse references, GUIDs, labels, and metadata.
- `asset_importer` — inspect/edit serialized importer settings and reimport assets.
- `scene_visibility` — hide/show objects and control Scene View picking independently of runtime active state.
- `editor_windows` — list, focus, repaint, and close editor windows.
- `navmesh_control` — detect/build/clear Unity editor NavMesh APIs without a hard package dependency.
- `invoke_unity` — public static UnityEngine/UnityEditor reflection escape hatch for APIs that do not yet have a dedicated MCP tool.

These tools auto-register through the existing MCP discovery system. No matching Python wrapper is required for each new C# tool.

## Unity Package Manager install

After pushing this repository to GitHub, install the package using your repository URL with the package path:

```text
https://github.com/YOUR-ACCOUNT/YOUR-REPO.git?path=/MCPForUnity
```

The Unity package ID is:

```text
com.neutronstar.systems.chat-gpt-mcp
```

The editor menu is:

```text
Window > NEUTRONSTAR Systems CHAT GPT MCP
```

## Codex

Use the package's client configuration UI to configure Codex, or point Codex at the server manually through its MCP configuration. The upstream Python server package/executable names are intentionally retained for transport compatibility.

## License / upstream

MIT licensed. Original CoplayDev attribution and license are retained in `LICENSE`. See `UPSTREAM_README.md` for the upstream project documentation and links.
