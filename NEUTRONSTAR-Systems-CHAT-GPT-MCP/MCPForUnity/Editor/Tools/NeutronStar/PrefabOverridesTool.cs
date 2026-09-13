using System;
using System.Linq;
using MCPForUnity.Editor.Helpers;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace MCPForUnity.Editor.Tools.NeutronStar
{
    [McpForUnityTool("prefab_overrides", Description = "Inspect, apply, and revert prefab instance overrides at property, object, or entire-instance scope.")]
    public static class PrefabOverridesTool
    {
        public class Parameters
        {
            [ToolParameter("Action: info, list, apply_property, revert_property, apply_object, revert_object, apply_all, revert_all.")] public string action { get; set; }
            [ToolParameter("Prefab instance GameObject or Component locator.")] public object target { get; set; }
            [ToolParameter("Serialized property path for apply_property/revert_property.", Required = false)] public string property_path { get; set; }
        }

        private static GameObject AsGameObject(UnityEngine.Object obj) => obj as GameObject ?? (obj as Component)?.gameObject;

        public static object HandleCommand(JObject p)
        {
            string action = (p?["action"]?.Value<string>() ?? "info").ToLowerInvariant();
            var target = NeutronStarToolUtil.ResolveObject(p?["target"]);
            var go = AsGameObject(target);
            if (target == null || go == null) return new ErrorResponse("Target prefab instance GameObject or Component could not be resolved.");
            if (!PrefabUtility.IsPartOfPrefabInstance(go)) return new ErrorResponse("Target is not part of a prefab instance.");

            var root = PrefabUtility.GetOutermostPrefabInstanceRoot(go);
            string assetPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(go);
            if (action == "info")
                return new SuccessResponse("Prefab instance info read.", new { target = NeutronStarToolUtil.DescribeObject(target), root = NeutronStarToolUtil.DescribeObject(root), prefab_asset_path = assetPath, status = PrefabUtility.GetPrefabInstanceStatus(go).ToString() });

            if (action == "list")
            {
                var mods = PrefabUtility.GetPropertyModifications(root) ?? Array.Empty<PropertyModification>();
                return new SuccessResponse("Prefab property overrides listed.", new
                {
                    prefab_asset_path = assetPath,
                    count = mods.Length,
                    modifications = mods.Select(m => new
                    {
                        target = NeutronStarToolUtil.DescribeObject(m.target),
                        property_path = m.propertyPath,
                        value = m.value,
                        object_reference = NeutronStarToolUtil.DescribeObject(m.objectReference)
                    }).ToArray()
                });
            }

            if (action == "apply_all")
            {
                PrefabUtility.ApplyPrefabInstance(root, InteractionMode.AutomatedAction);
                return new SuccessResponse("All prefab instance overrides applied.", new { prefab_asset_path = assetPath });
            }
            if (action == "revert_all")
            {
                PrefabUtility.RevertPrefabInstance(root, InteractionMode.AutomatedAction);
                return new SuccessResponse("All prefab instance overrides reverted.", new { prefab_asset_path = assetPath });
            }
            if (action == "apply_object")
            {
                PrefabUtility.ApplyObjectOverride(target, assetPath, InteractionMode.AutomatedAction);
                return new SuccessResponse("Object overrides applied.", new { target = NeutronStarToolUtil.DescribeObject(target), prefab_asset_path = assetPath });
            }
            if (action == "revert_object")
            {
                PrefabUtility.RevertObjectOverride(target, InteractionMode.AutomatedAction);
                return new SuccessResponse("Object overrides reverted.", NeutronStarToolUtil.DescribeObject(target));
            }

            string propertyPath = p?["property_path"]?.Value<string>() ?? p?["propertyPath"]?.Value<string>();
            if (string.IsNullOrEmpty(propertyPath)) return new ErrorResponse("property_path is required for property-level operations.");
            using var so = new SerializedObject(target);
            so.UpdateIfRequiredOrScript();
            var prop = so.FindProperty(propertyPath);
            if (prop == null) return new ErrorResponse($"Serialized property '{propertyPath}' was not found on target.");
            if (action == "apply_property")
            {
                PrefabUtility.ApplyPropertyOverride(prop, assetPath, InteractionMode.AutomatedAction);
                return new SuccessResponse("Prefab property override applied.", new { property_path = propertyPath, prefab_asset_path = assetPath });
            }
            if (action == "revert_property")
            {
                PrefabUtility.RevertPropertyOverride(prop, InteractionMode.AutomatedAction);
                return new SuccessResponse("Prefab property override reverted.", new { property_path = propertyPath });
            }
            return new ErrorResponse("Unknown action. Use info, list, apply_property, revert_property, apply_object, revert_object, apply_all, revert_all.");
        }
    }
}
