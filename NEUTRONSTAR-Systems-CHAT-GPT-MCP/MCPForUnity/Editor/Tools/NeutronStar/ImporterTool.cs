using System;
using System.Linq;
using MCPForUnity.Editor.Helpers;
using Newtonsoft.Json.Linq;
using UnityEditor;

namespace MCPForUnity.Editor.Tools.NeutronStar
{
    [McpForUnityTool("asset_importer", Description = "Universal AssetImporter inspector/editor. Read or modify serialized importer settings for textures, models, audio, video, and other Unity assets, then reimport.")]
    public static class ImporterTool
    {
        public class Parameters
        {
            [ToolParameter("Action: list, get, set, reimport.")] public string action { get; set; }
            [ToolParameter("Unity asset path.")] public string path { get; set; }
            [ToolParameter("Serialized importer property path.", Required = false)] public string property_path { get; set; }
            [ToolParameter("Value for set.", Required = false)] public object value { get; set; }
            [ToolParameter("Maximum properties returned by list.", Required = false, DefaultValue = "500")] public int limit { get; set; }
        }

        public static object HandleCommand(JObject p)
        {
            string path = p?["path"]?.Value<string>();
            if (string.IsNullOrWhiteSpace(path)) return new ErrorResponse("path is required.");
            var importer = AssetImporter.GetAtPath(path);
            if (importer == null) return new ErrorResponse($"No AssetImporter exists for '{path}'.");
            string action = (p?["action"]?.Value<string>() ?? "list").ToLowerInvariant();

            if (action == "reimport")
            {
                importer.SaveAndReimport();
                return new SuccessResponse("Asset reimported.", new { path, importer_type = importer.GetType().FullName });
            }

            using var so = new SerializedObject(importer);
            so.UpdateIfRequiredOrScript();
            string propPath = p?["property_path"]?.Value<string>() ?? p?["propertyPath"]?.Value<string>();
            if (action == "list")
            {
                int limit = Math.Max(1, Math.Min(5000, p?["limit"]?.Value<int?>() ?? 500));
                return new SuccessResponse("Importer properties read.", new { path, importer_type = importer.GetType().FullName, properties = NeutronStarToolUtil.EnumerateProperties(so, false, limit).ToArray() });
            }
            var prop = string.IsNullOrEmpty(propPath) ? null : so.FindProperty(propPath);
            if (prop == null) return new ErrorResponse($"Importer property '{propPath}' was not found.");
            if (action == "get") return new SuccessResponse("Importer property read.", new { path = propPath, type = prop.propertyType.ToString(), value = NeutronStarToolUtil.ReadProperty(prop) });
            if (action == "set")
            {
                Undo.RecordObject(importer, $"MCP importer {propPath}");
                if (!NeutronStarToolUtil.WriteProperty(prop, p?["value"], out string error)) return new ErrorResponse(error);
                so.ApplyModifiedProperties();
                importer.SaveAndReimport();
                return new SuccessResponse("Importer property updated and asset reimported.", new { path = propPath, value = NeutronStarToolUtil.ReadProperty(prop) });
            }
            return new ErrorResponse("Unknown action. Use list, get, set, reimport.");
        }
    }
}
