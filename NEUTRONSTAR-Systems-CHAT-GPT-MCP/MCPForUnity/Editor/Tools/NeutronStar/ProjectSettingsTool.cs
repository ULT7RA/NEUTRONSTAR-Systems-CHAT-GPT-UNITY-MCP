using System;
using System.IO;
using System.Linq;
using MCPForUnity.Editor.Helpers;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace MCPForUnity.Editor.Tools.NeutronStar
{
    [McpForUnityTool("project_settings", Description = "Inspect and edit Unity ProjectSettings serialized assets generically, including ProjectSettings.asset, TagManager.asset, TimeManager.asset, Physics settings, AudioManager, GraphicsSettings, and other serialized settings files.")]
    public static class ProjectSettingsTool
    {
        public class Parameters
        {
            [ToolParameter("Action: files, list, get, set.")] public string action { get; set; }
            [ToolParameter("ProjectSettings asset filename or path, e.g. ProjectSettings.asset or ProjectSettings/TimeManager.asset.", Required = false)] public string file { get; set; }
            [ToolParameter("Serialized property path for get/set.", Required = false)] public string property_path { get; set; }
            [ToolParameter("Value for set.", Required = false)] public object value { get; set; }
            [ToolParameter("Maximum properties returned by list.", Required = false, DefaultValue = "500")] public int limit { get; set; }
        }

        private static string Normalize(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return null;
            string name = input.Replace('\\', '/');
            if (!name.StartsWith("ProjectSettings/", StringComparison.OrdinalIgnoreCase)) name = "ProjectSettings/" + name.TrimStart('/');
            return name;
        }

        private static UnityEngine.Object Load(string path)
        {
            var objects = AssetDatabase.LoadAllAssetsAtPath(path);
            return objects != null && objects.Length > 0 ? objects[0] : null;
        }

        public static object HandleCommand(JObject p)
        {
            string action = (p?["action"]?.Value<string>() ?? "files").ToLowerInvariant();
            if (action == "files")
            {
                string root = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "ProjectSettings"));
                if (!Directory.Exists(root)) return new ErrorResponse("ProjectSettings directory was not found.");
                var files = Directory.GetFiles(root, "*.asset", SearchOption.TopDirectoryOnly).Select(Path.GetFileName).OrderBy(x => x).ToArray();
                return new SuccessResponse("ProjectSettings files listed.", new { count = files.Length, files });
            }

            string path = Normalize(p?["file"]?.Value<string>());
            if (string.IsNullOrEmpty(path)) return new ErrorResponse("file is required for list/get/set.");
            var target = Load(path);
            if (target == null) return new ErrorResponse($"Could not load '{path}' as a serialized ProjectSettings asset.");

            using var so = new SerializedObject(target);
            so.UpdateIfRequiredOrScript();
            string propertyPath = p?["property_path"]?.Value<string>() ?? p?["propertyPath"]?.Value<string>();
            if (action == "list")
            {
                int limit = Math.Max(1, Math.Min(5000, p?["limit"]?.Value<int?>() ?? 500));
                return new SuccessResponse("Project settings read.", new { file = path, type = target.GetType().FullName, properties = NeutronStarToolUtil.EnumerateProperties(so, false, limit).ToArray() });
            }

            if (string.IsNullOrEmpty(propertyPath)) return new ErrorResponse("property_path is required for get/set.");
            var prop = so.FindProperty(propertyPath);
            if (prop == null) return new ErrorResponse($"Property '{propertyPath}' was not found in '{path}'.");
            if (action == "get") return new SuccessResponse("Project setting read.", new { file = path, path = propertyPath, type = prop.propertyType.ToString(), value = NeutronStarToolUtil.ReadProperty(prop) });
            if (action == "set")
            {
                Undo.RecordObject(target, $"MCP project setting {propertyPath}");
                if (!NeutronStarToolUtil.WriteProperty(prop, p?["value"], out string error)) return new ErrorResponse(error);
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(target);
                AssetDatabase.SaveAssets();
                return new SuccessResponse("Project setting updated.", new { file = path, path = propertyPath, value = NeutronStarToolUtil.ReadProperty(prop) });
            }
            return new ErrorResponse("Unknown action. Use files, list, get, set.");
        }
    }
}
