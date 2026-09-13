using System.Linq;
using MCPForUnity.Editor.Helpers;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace MCPForUnity.Editor.Tools.NeutronStar
{
    [McpForUnityTool("object_identity", Description = "Resolve Unity objects and convert between instance IDs, GlobalObjectIds, asset paths, GUIDs, and scene hierarchy paths.")]
    public static class ObjectIdentityTool
    {
        public class Parameters
        {
            [ToolParameter("Action: resolve, selection, guid_to_path, path_to_guid.")] public string action { get; set; }
            [ToolParameter("Object locator or path/GUID depending on action.", Required = false)] public object target { get; set; }
        }

        public static object HandleCommand(JObject p)
        {
            string action = (p?["action"]?.Value<string>() ?? "resolve").ToLowerInvariant();
            switch (action)
            {
                case "resolve":
                    var obj = NeutronStarToolUtil.ResolveObject(p?["target"]);
                    return obj == null ? new ErrorResponse("Object could not be resolved.") : new SuccessResponse("Object resolved.", NeutronStarToolUtil.DescribeObject(obj));
                case "selection":
                    return new SuccessResponse("Current Unity selection.", new { active = NeutronStarToolUtil.DescribeObject(Selection.activeObject), objects = Selection.objects.Select(NeutronStarToolUtil.DescribeObject).ToArray() });
                case "guid_to_path":
                    string guid = p?["target"]?.Value<string>();
                    string path = AssetDatabase.GUIDToAssetPath(guid ?? "");
                    return string.IsNullOrEmpty(path) ? new ErrorResponse("GUID was not found.") : new SuccessResponse("GUID resolved.", new { guid, path });
                case "path_to_guid":
                    string assetPath = p?["target"]?.Value<string>();
                    string assetGuid = AssetDatabase.AssetPathToGUID(assetPath ?? "");
                    return string.IsNullOrEmpty(assetGuid) ? new ErrorResponse("Asset path was not found.") : new SuccessResponse("Asset path resolved.", new { path = assetPath, guid = assetGuid });
                default:
                    return new ErrorResponse("Unknown action. Use resolve, selection, guid_to_path, path_to_guid.");
            }
        }
    }
}
