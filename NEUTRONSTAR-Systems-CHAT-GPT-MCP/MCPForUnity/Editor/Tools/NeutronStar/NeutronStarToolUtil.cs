using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace MCPForUnity.Editor.Tools.NeutronStar
{
    internal static class NeutronStarToolUtil
    {
        public static UnityEngine.Object ResolveObject(JToken token)
        {
            if (token == null || token.Type == JTokenType.Null) return null;

            if (token.Type == JTokenType.Integer)
                return EditorUtility.InstanceIDToObject(token.Value<int>());

            if (token.Type == JTokenType.String)
            {
                string s = token.Value<string>();
                if (string.IsNullOrWhiteSpace(s)) return null;

                if (int.TryParse(s, out int instanceId))
                {
                    var byId = EditorUtility.InstanceIDToObject(instanceId);
                    if (byId != null) return byId;
                }

                if (GlobalObjectId.TryParse(s, out var globalId))
                {
                    var byGlobal = GlobalObjectId.GlobalObjectIdentifierToObjectSlow(globalId);
                    if (byGlobal != null) return byGlobal;
                }

                if (s.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase) ||
                    s.StartsWith("Packages/", StringComparison.OrdinalIgnoreCase) ||
                    s.StartsWith("ProjectSettings/", StringComparison.OrdinalIgnoreCase))
                {
                    var byPath = AssetDatabase.LoadMainAssetAtPath(s);
                    if (byPath != null) return byPath;
                }

                string guidPath = AssetDatabase.GUIDToAssetPath(s);
                if (!string.IsNullOrEmpty(guidPath))
                {
                    var byGuid = AssetDatabase.LoadMainAssetAtPath(guidPath);
                    if (byGuid != null) return byGuid;
                }

                var go = GameObject.Find(s);
                if (go != null) return go;
            }

            if (token is JObject obj)
            {
                if (obj["global_id"] != null || obj["globalId"] != null)
                    return ResolveObject(obj["global_id"] ?? obj["globalId"]);
                if (obj["instance_id"] != null || obj["instanceId"] != null)
                    return ResolveObject(obj["instance_id"] ?? obj["instanceId"]);
                if (obj["asset_path"] != null || obj["assetPath"] != null)
                    return ResolveObject(obj["asset_path"] ?? obj["assetPath"]);
                if (obj["guid"] != null)
                    return ResolveObject(obj["guid"]);
                if (obj["hierarchy_path"] != null || obj["hierarchyPath"] != null)
                    return ResolveObject(obj["hierarchy_path"] ?? obj["hierarchyPath"]);
            }

            return null;
        }

        public static object DescribeObject(UnityEngine.Object obj)
        {
            if (obj == null) return null;
            string path = AssetDatabase.GetAssetPath(obj);
            string global = null;
            try { global = GlobalObjectId.GetGlobalObjectIdSlow(obj).ToString(); } catch { }

            string hierarchyPath = null;
            if (obj is GameObject go) hierarchyPath = GetHierarchyPath(go.transform);
            else if (obj is Component c) hierarchyPath = GetHierarchyPath(c.transform);

            return new
            {
                name = obj.name,
                type = obj.GetType().FullName,
                instance_id = obj.GetInstanceID(),
                global_id = global,
                asset_path = string.IsNullOrEmpty(path) ? null : path,
                hierarchy_path = hierarchyPath
            };
        }

        public static string GetHierarchyPath(Transform transform)
        {
            if (transform == null) return null;
            var names = new Stack<string>();
            var t = transform;
            while (t != null)
            {
                names.Push(t.name);
                t = t.parent;
            }
            return string.Join("/", names);
        }

        public static object ReadProperty(SerializedProperty p)
        {
            if (p == null) return null;
            switch (p.propertyType)
            {
                case SerializedPropertyType.Integer: return p.longValue;
                case SerializedPropertyType.Boolean: return p.boolValue;
                case SerializedPropertyType.Float: return p.doubleValue;
                case SerializedPropertyType.String: return p.stringValue;
                case SerializedPropertyType.Color:
                    var c = p.colorValue; return new { r = c.r, g = c.g, b = c.b, a = c.a };
                case SerializedPropertyType.ObjectReference: return DescribeObject(p.objectReferenceValue);
                case SerializedPropertyType.LayerMask: return p.intValue;
                case SerializedPropertyType.Enum:
                    return new { index = p.enumValueIndex, value = p.enumDisplayNames != null && p.enumValueIndex >= 0 && p.enumValueIndex < p.enumDisplayNames.Length ? p.enumDisplayNames[p.enumValueIndex] : null, options = p.enumDisplayNames };
                case SerializedPropertyType.Vector2:
                    var v2 = p.vector2Value; return new { x = v2.x, y = v2.y };
                case SerializedPropertyType.Vector3:
                    var v3 = p.vector3Value; return new { x = v3.x, y = v3.y, z = v3.z };
                case SerializedPropertyType.Vector4:
                    var v4 = p.vector4Value; return new { x = v4.x, y = v4.y, z = v4.z, w = v4.w };
                case SerializedPropertyType.Quaternion:
                    var q = p.quaternionValue; return new { x = q.x, y = q.y, z = q.z, w = q.w };
                case SerializedPropertyType.Rect:
                    var r = p.rectValue; return new { x = r.x, y = r.y, width = r.width, height = r.height };
                case SerializedPropertyType.Bounds:
                    var b = p.boundsValue; return new { center = new { x = b.center.x, y = b.center.y, z = b.center.z }, size = new { x = b.size.x, y = b.size.y, z = b.size.z } };
                case SerializedPropertyType.Vector2Int:
                    var v2i = p.vector2IntValue; return new { x = v2i.x, y = v2i.y };
                case SerializedPropertyType.Vector3Int:
                    var v3i = p.vector3IntValue; return new { x = v3i.x, y = v3i.y, z = v3i.z };
                case SerializedPropertyType.RectInt:
                    var ri = p.rectIntValue; return new { x = ri.x, y = ri.y, width = ri.width, height = ri.height };
                case SerializedPropertyType.BoundsInt:
                    var bi = p.boundsIntValue; return new { position = new { x = bi.position.x, y = bi.position.y, z = bi.position.z }, size = new { x = bi.size.x, y = bi.size.y, z = bi.size.z } };
                case SerializedPropertyType.AnimationCurve: return p.animationCurveValue?.ToString();
                case SerializedPropertyType.ArraySize: return p.intValue;
                case SerializedPropertyType.Character: return (char)p.intValue;
                case SerializedPropertyType.ExposedReference: return DescribeObject(p.exposedReferenceValue);
                case SerializedPropertyType.FixedBufferSize: return p.fixedBufferSize;
                case SerializedPropertyType.ManagedReference:
                    return new { type = p.managedReferenceFullTypename, value = p.managedReferenceValue?.ToString() };
                default: return p.isArray ? new { array_size = p.arraySize } : null;
            }
        }

        public static bool WriteProperty(SerializedProperty p, JToken value, out string error)
        {
            error = null;
            try
            {
                switch (p.propertyType)
                {
                    case SerializedPropertyType.Integer: p.longValue = value.Value<long>(); break;
                    case SerializedPropertyType.Boolean: p.boolValue = value.Value<bool>(); break;
                    case SerializedPropertyType.Float: p.doubleValue = value.Value<double>(); break;
                    case SerializedPropertyType.String: p.stringValue = value.Type == JTokenType.Null ? null : value.Value<string>(); break;
                    case SerializedPropertyType.LayerMask: p.intValue = value.Value<int>(); break;
                    case SerializedPropertyType.Enum:
                        if (value.Type == JTokenType.Integer) p.enumValueIndex = value.Value<int>();
                        else
                        {
                            string enumName = value.Value<string>();
                            int idx = Array.FindIndex(p.enumDisplayNames, n => string.Equals(n, enumName, StringComparison.OrdinalIgnoreCase));
                            if (idx < 0) { error = $"Enum value '{enumName}' not found."; return false; }
                            p.enumValueIndex = idx;
                        }
                        break;
                    case SerializedPropertyType.ObjectReference: p.objectReferenceValue = ResolveObject(value); break;
                    case SerializedPropertyType.Vector2:
                        p.vector2Value = new Vector2(value["x"]?.Value<float>() ?? 0f, value["y"]?.Value<float>() ?? 0f); break;
                    case SerializedPropertyType.Vector3:
                        p.vector3Value = new Vector3(value["x"]?.Value<float>() ?? 0f, value["y"]?.Value<float>() ?? 0f, value["z"]?.Value<float>() ?? 0f); break;
                    case SerializedPropertyType.Vector4:
                        p.vector4Value = new Vector4(value["x"]?.Value<float>() ?? 0f, value["y"]?.Value<float>() ?? 0f, value["z"]?.Value<float>() ?? 0f, value["w"]?.Value<float>() ?? 0f); break;
                    case SerializedPropertyType.Quaternion:
                        p.quaternionValue = new Quaternion(value["x"]?.Value<float>() ?? 0f, value["y"]?.Value<float>() ?? 0f, value["z"]?.Value<float>() ?? 0f, value["w"]?.Value<float>() ?? 1f); break;
                    case SerializedPropertyType.Color:
                        p.colorValue = new Color(value["r"]?.Value<float>() ?? 0f, value["g"]?.Value<float>() ?? 0f, value["b"]?.Value<float>() ?? 0f, value["a"]?.Value<float>() ?? 1f); break;
                    case SerializedPropertyType.Rect:
                        p.rectValue = new Rect(value["x"]?.Value<float>() ?? 0f, value["y"]?.Value<float>() ?? 0f, value["width"]?.Value<float>() ?? 0f, value["height"]?.Value<float>() ?? 0f); break;
                    case SerializedPropertyType.Vector2Int:
                        p.vector2IntValue = new Vector2Int(value["x"]?.Value<int>() ?? 0, value["y"]?.Value<int>() ?? 0); break;
                    case SerializedPropertyType.Vector3Int:
                        p.vector3IntValue = new Vector3Int(value["x"]?.Value<int>() ?? 0, value["y"]?.Value<int>() ?? 0, value["z"]?.Value<int>() ?? 0); break;
                    case SerializedPropertyType.ArraySize: p.intValue = value.Value<int>(); break;
                    case SerializedPropertyType.Character:
                        string s = value.Value<string>(); p.intValue = string.IsNullOrEmpty(s) ? 0 : s[0]; break;
                    default:
                        error = $"Writing SerializedPropertyType '{p.propertyType}' is not implemented by this tool.";
                        return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        public static IEnumerable<object> EnumerateProperties(SerializedObject so, bool visibleOnly, int limit)
        {
            var iterator = so.GetIterator();
            bool enter = true;
            int count = 0;
            while (count < limit && (visibleOnly ? iterator.NextVisible(enter) : iterator.Next(enter)))
            {
                enter = false;
                count++;
                yield return new
                {
                    path = iterator.propertyPath,
                    name = iterator.name,
                    display_name = iterator.displayName,
                    type = iterator.propertyType.ToString(),
                    depth = iterator.depth,
                    is_array = iterator.isArray,
                    array_size = iterator.isArray ? iterator.arraySize : (int?)null,
                    editable = iterator.editable,
                    value = ReadProperty(iterator)
                };
            }
        }
    }
}
