using System;
using System.Collections.Generic;
using System.Linq;
using MCPForUnity.Editor.Helpers;
using Newtonsoft.Json.Linq;
using UnityEditor;

namespace MCPForUnity.Editor.Tools.NeutronStar
{
    [McpForUnityTool("asset_relations", Description = "Inspect asset dependencies, recursive dependencies, reverse references, GUIDs, and asset metadata across the Unity project.")]
    public static class AssetRelationsTool
    {
        public class Parameters
        {
            [ToolParameter("Action: dependencies, reverse_dependencies, info.")] public string action { get; set; }
            [ToolParameter("Unity asset path or GUID.")] public string path { get; set; }
            [ToolParameter("Use recursive dependency traversal.", Required = false, DefaultValue = "true")] public bool recursive { get; set; }
            [ToolParameter("Maximum reverse-dependency results.", Required = false, DefaultValue = "200")] public int limit { get; set; }
        }

        private static string ResolvePath(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return null;
            if (input.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase) || input.StartsWith("Packages/", StringComparison.OrdinalIgnoreCase)) return input;
            string byGuid = AssetDatabase.GUIDToAssetPath(input);
            return string.IsNullOrEmpty(byGuid) ? input : byGuid;
        }

        public static object HandleCommand(JObject p)
        {
            string action = (p?["action"]?.Value<string>() ?? "dependencies").ToLowerInvariant();
            string path = ResolvePath(p?["path"]?.Value<string>());
            if (string.IsNullOrEmpty(path)) return new ErrorResponse("path is required.");

            if (action == "dependencies")
            {
                bool recursive = p?["recursive"]?.Value<bool?>() ?? true;
                var deps = AssetDatabase.GetDependencies(path, recursive).Where(x => !string.Equals(x, path, StringComparison.OrdinalIgnoreCase)).ToArray();
                return new SuccessResponse("Dependencies read.", new { path, recursive, count = deps.Length, dependencies = deps });
            }
            if (action == "reverse_dependencies")
            {
                int limit = Math.Max(1, Math.Min(5000, p?["limit"]?.Value<int?>() ?? 200));
                var hits = new List<string>();
                foreach (string candidate in AssetDatabase.GetAllAssetPaths())
                {
                    if (hits.Count >= limit) break;
                    if (!(candidate.StartsWith("Assets/") || candidate.StartsWith("Packages/"))) continue;
                    if (string.Equals(candidate, path, StringComparison.OrdinalIgnoreCase)) continue;
                    try
                    {
                        if (AssetDatabase.GetDependencies(candidate, false).Any(d => string.Equals(d, path, StringComparison.OrdinalIgnoreCase))) hits.Add(candidate);
                    }
                    catch { }
                }
                return new SuccessResponse("Reverse dependencies read.", new { path, count = hits.Count, truncated = hits.Count >= limit, references = hits });
            }
            if (action == "info")
            {
                var main = AssetDatabase.LoadMainAssetAtPath(path);
                string guid = AssetDatabase.AssetPathToGUID(path);
                return main == null ? new ErrorResponse("Asset not found.") : new SuccessResponse("Asset info read.", new { path, guid, asset = NeutronStarToolUtil.DescribeObject(main), labels = AssetDatabase.GetLabels(main) });
            }
            return new ErrorResponse("Unknown action. Use dependencies, reverse_dependencies, info.");
        }
    }
}
