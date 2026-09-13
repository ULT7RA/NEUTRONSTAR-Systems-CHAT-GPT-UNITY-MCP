using System.Collections.Generic;
using System.Linq;
using MCPForUnity.Editor.Helpers;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace MCPForUnity.Editor.Tools.NeutronStar
{
    [McpForUnityTool("editor_selection", Description = "Read and control Unity Editor selection, ping assets/objects, and frame the current selection in Scene View.")]
    public static class EditorSelectionTool
    {
        public class Parameters
        {
            [ToolParameter("Action: get, set, add, clear, ping, frame.")] public string action { get; set; }
            [ToolParameter("Single target locator.", Required = false)] public object target { get; set; }
            [ToolParameter("Array of target locators for set/add.", Required = false)] public object[] targets { get; set; }
        }

        public static object HandleCommand(JObject p)
        {
            string action = (p?["action"]?.Value<string>() ?? "get").ToLowerInvariant();
            if (action == "get")
                return new SuccessResponse("Selection read.", new { active = NeutronStarToolUtil.DescribeObject(Selection.activeObject), objects = Selection.objects.Select(NeutronStarToolUtil.DescribeObject).ToArray() });

            if (action == "clear")
            {
                Selection.objects = System.Array.Empty<UnityEngine.Object>();
                return new SuccessResponse("Selection cleared.");
            }

            if (action == "ping")
            {
                var obj = NeutronStarToolUtil.ResolveObject(p?["target"]);
                if (obj == null) return new ErrorResponse("Target could not be resolved.");
                EditorGUIUtility.PingObject(obj);
                return new SuccessResponse("Object pinged.", NeutronStarToolUtil.DescribeObject(obj));
            }

            if (action == "frame")
            {
                if (p?["target"] != null)
                {
                    var obj = NeutronStarToolUtil.ResolveObject(p["target"]);
                    if (obj == null) return new ErrorResponse("Target could not be resolved.");
                    Selection.activeObject = obj;
                }
                if (SceneView.lastActiveSceneView == null) return new ErrorResponse("No active Scene View is available.");
                SceneView.lastActiveSceneView.FrameSelected();
                return new SuccessResponse("Scene View framed on selection.");
            }

            var resolved = new List<UnityEngine.Object>();
            if (p?["targets"] is JArray arr)
                foreach (var token in arr) { var obj = NeutronStarToolUtil.ResolveObject(token); if (obj != null) resolved.Add(obj); }
            if (p?["target"] != null) { var obj = NeutronStarToolUtil.ResolveObject(p["target"]); if (obj != null) resolved.Add(obj); }
            if (resolved.Count == 0) return new ErrorResponse("No targets could be resolved.");

            if (action == "set") Selection.objects = resolved.Distinct().ToArray();
            else if (action == "add") Selection.objects = Selection.objects.Concat(resolved).Distinct().ToArray();
            else return new ErrorResponse("Unknown action. Use get, set, add, clear, ping, frame.");

            Selection.activeObject = resolved[0];
            return new SuccessResponse("Selection updated.", new { objects = Selection.objects.Select(NeutronStarToolUtil.DescribeObject).ToArray() });
        }
    }
}
