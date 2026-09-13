using System;
using System.Linq;
using MCPForUnity.Editor.Helpers;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace MCPForUnity.Editor.Tools.NeutronStar
{
    [McpForUnityTool("serialized_object", Description = "Universal SerializedObject inspector/editor. Read or set serialized fields on GameObjects, Components, assets, ScriptableObjects, importers, and editor objects using Unity property paths.")]
    public static class SerializedObjectTool
    {
        public class Parameters
        {
            [ToolParameter("Action: describe, list, get, set, resize_array.")] public string action { get; set; }
            [ToolParameter("Target object: instance ID, GlobalObjectId, asset path, GUID, hierarchy path, or descriptor object.")] public object target { get; set; }
            [ToolParameter("Serialized property path, e.g. m_Enabled or m_LocalPosition.x.", Required = false)] public string property_path { get; set; }
            [ToolParameter("Value to assign for set.", Required = false)] public object value { get; set; }
            [ToolParameter("Maximum number of properties returned by list.", Required = false, DefaultValue = "500")] public int limit { get; set; }
            [ToolParameter("Only return visible serialized properties.", Required = false, DefaultValue = "true")] public bool visible_only { get; set; }
            [ToolParameter("New array size for resize_array.", Required = false)] public int array_size { get; set; }
        }

        public static object HandleCommand(JObject p)
        {
            string action = (p?["action"]?.Value<string>() ?? "list").ToLowerInvariant();
            var target = NeutronStarToolUtil.ResolveObject(p?["target"]);
            if (target == null) return new ErrorResponse("Target object could not be resolved.");

            try
            {
                using var so = new SerializedObject(target);
                so.UpdateIfRequiredOrScript();
                string path = p?["property_path"]?.Value<string>() ?? p?["propertyPath"]?.Value<string>();

                switch (action)
                {
                    case "describe":
                        return new SuccessResponse("Object resolved.", new { target = NeutronStarToolUtil.DescribeObject(target), serialized_object = so.targetObject != null });
                    case "list":
                        int limit = Math.Max(1, Math.Min(5000, p?["limit"]?.Value<int?>() ?? 500));
                        bool visibleOnly = p?["visible_only"]?.Value<bool?>() ?? p?["visibleOnly"]?.Value<bool?>() ?? true;
                        return new SuccessResponse("Serialized properties read.", new { target = NeutronStarToolUtil.DescribeObject(target), properties = NeutronStarToolUtil.EnumerateProperties(so, visibleOnly, limit).ToArray() });
                    case "get":
                        if (string.IsNullOrEmpty(path)) return new ErrorResponse("property_path is required for get.");
                        var getProp = so.FindProperty(path);
                        if (getProp == null) return new ErrorResponse($"Serialized property '{path}' was not found.");
                        return new SuccessResponse("Serialized property read.", new { path, type = getProp.propertyType.ToString(), value = NeutronStarToolUtil.ReadProperty(getProp), editable = getProp.editable });
                    case "set":
                        if (string.IsNullOrEmpty(path)) return new ErrorResponse("property_path is required for set.");
                        var setProp = so.FindProperty(path);
                        if (setProp == null) return new ErrorResponse($"Serialized property '{path}' was not found.");
                        Undo.RecordObject(target, $"MCP set {path}");
                        if (!NeutronStarToolUtil.WriteProperty(setProp, p?["value"], out string error)) return new ErrorResponse(error);
                        so.ApplyModifiedProperties();
                        EditorUtility.SetDirty(target);
                        return new SuccessResponse("Serialized property updated.", new { path, value = NeutronStarToolUtil.ReadProperty(setProp) });
                    case "resize_array":
                        if (string.IsNullOrEmpty(path)) return new ErrorResponse("property_path is required for resize_array.");
                        var arrayProp = so.FindProperty(path);
                        if (arrayProp == null || !arrayProp.isArray) return new ErrorResponse($"'{path}' is not a serialized array.");
                        int size = Math.Max(0, p?["array_size"]?.Value<int?>() ?? p?["arraySize"]?.Value<int?>() ?? 0);
                        Undo.RecordObject(target, $"MCP resize {path}");
                        arrayProp.arraySize = size;
                        so.ApplyModifiedProperties();
                        EditorUtility.SetDirty(target);
                        return new SuccessResponse("Serialized array resized.", new { path, array_size = size });
                    default:
                        return new ErrorResponse("Unknown action. Use describe, list, get, set, resize_array.");
                }
            }
            catch (Exception ex) { return new ErrorResponse($"SerializedObject operation failed: {ex.Message}"); }
        }
    }
}
