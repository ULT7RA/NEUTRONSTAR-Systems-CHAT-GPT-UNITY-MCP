# NEUTRONSTAR Systems CHAT GPT MCP changes

Base: CoplayDev/unity-mcp 10.2.1 beta lineage.

## Branding / packaging

- Unity package display name: `NEUTRONSTAR Systems CHAT GPT MCP`
- Unity package ID: `com.neutronstar.systems.chat-gpt-mcp`
- Fork version: `10.2.1-neutronstar.1`
- Editor menu root: `Window/NEUTRONSTAR Systems CHAT GPT MCP`
- Original MIT license retained.
- Original upstream README retained as `UPSTREAM_README.md`.

## Added MCP tools

### `serialized_object`
Universal Unity serialization access. Supports describing targets, listing serialized properties, reading/writing property paths, and resizing serialized arrays.

### `object_identity`
Resolves objects through instance ID, `GlobalObjectId`, GUID, asset path, and scene hierarchy path, and returns stable identity metadata.

### `editor_selection`
Reads or mutates Unity Editor selection; also supports object ping and Scene View framing.

### `asset_relations`
Returns direct/recursive dependencies, reverse dependencies, GUIDs, labels, and basic asset metadata.

### `asset_importer`
Exposes serialized `AssetImporter` settings generically and supports reimport.

### `scene_visibility`
Controls Scene View visibility and pickability independently from GameObject active state.

### `editor_windows`
Lists, focuses, repaints, and closes `EditorWindow` instances.

### `navmesh_control`
Reflectively accesses `UnityEditor.AI.NavMeshBuilder` so the fork does not require a hard NavMesh package dependency.

### `invoke_unity`
Reflection escape hatch for public static Unity APIs. Supports type inspection, method listing, static property reads/writes, and static method invocation.

## Architecture

All added tools are C# editor-side tools marked with `[McpForUnityTool]`. They use the existing tool discovery and dynamic registration pipeline, so separate Python wrappers are not required.

### `project_settings`
Generic serialized access to Unity `ProjectSettings/*.asset` files, including listing settings files and reading/writing individual property paths.

### `prefab_overrides`
Inspects property modifications and applies/reverts overrides at property, object, or entire prefab-instance scope.

- Fixed EditorWindowsTool namespace collision by fully qualifying UnityEngine.Resources.
