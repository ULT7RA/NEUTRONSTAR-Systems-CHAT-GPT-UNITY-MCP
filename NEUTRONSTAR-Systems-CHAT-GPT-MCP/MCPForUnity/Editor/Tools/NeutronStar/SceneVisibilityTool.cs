using MCPForUnity.Editor.Helpers;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace MCPForUnity.Editor.Tools.NeutronStar
{
    [McpForUnityTool("scene_visibility", Description = "Control Scene View visibility and picking for GameObjects without changing runtime active state.")]
    public static class SceneVisibilityTool
    {
        public class Parameters
        {
            [ToolParameter("Action: get, hide, show, show_all, disable_picking, enable_picking.")] public string action { get; set; }
            [ToolParameter("Target GameObject locator.", Required = false)] public object target { get; set; }
            [ToolParameter("Apply recursively to children.", Required = false, DefaultValue = "true")] public bool include_children { get; set; }
        }

        private static GameObject ResolveGo(JToken token)
        {
            var o = NeutronStarToolUtil.ResolveObject(token);
            return o as GameObject ?? (o as Component)?.gameObject;
        }

        public static object HandleCommand(JObject p)
        {
            string action = (p?["action"]?.Value<string>() ?? "get").ToLowerInvariant();
            var manager = SceneVisibilityManager.instance;
            if (action == "show_all")
            {
                manager.ShowAll();
                return new SuccessResponse("All Scene View hidden objects were shown.");
            }
            var go = ResolveGo(p?["target"]);
            if (go == null) return new ErrorResponse("Target GameObject could not be resolved.");
            bool children = p?["include_children"]?.Value<bool?>() ?? p?["includeChildren"]?.Value<bool?>() ?? true;
            switch (action)
            {
                case "get":
                    return new SuccessResponse("Scene visibility read.", new { target = NeutronStarToolUtil.DescribeObject(go), hidden = manager.IsHidden(go, children), picking_disabled = manager.IsPickingDisabled(go, children) });
                case "hide": manager.Hide(go, children); break;
                case "show": manager.Show(go, children); break;
                case "disable_picking": manager.DisablePicking(go, children); break;
                case "enable_picking": manager.EnablePicking(go, children); break;
                default: return new ErrorResponse("Unknown action. Use get, hide, show, show_all, disable_picking, enable_picking.");
            }
            SceneView.RepaintAll();
            return new SuccessResponse("Scene visibility updated.", new { target = NeutronStarToolUtil.DescribeObject(go), hidden = manager.IsHidden(go, children), picking_disabled = manager.IsPickingDisabled(go, children) });
        }
    }
}
