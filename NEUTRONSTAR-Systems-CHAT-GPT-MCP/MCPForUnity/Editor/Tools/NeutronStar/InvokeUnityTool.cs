using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MCPForUnity.Editor.Helpers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MCPForUnity.Editor.Tools.NeutronStar
{
    [McpForUnityTool("invoke_unity", Description = "Advanced reflection escape hatch for UnityEngine/UnityEditor APIs: inspect types/methods, read/write static properties, or invoke public static methods. Use dedicated tools when available.", Group = "scripting_ext")]
    public static class InvokeUnityTool
    {
        public class Parameters
        {
            [ToolParameter("Action: type_info, methods, get_static, set_static, invoke_static.")] public string action { get; set; }
            [ToolParameter("Full type name, e.g. UnityEditor.EditorApplication.")] public string type { get; set; }
            [ToolParameter("Method or property name.", Required = false)] public string member { get; set; }
            [ToolParameter("Arguments array for invoke_static.", Required = false)] public object[] args { get; set; }
            [ToolParameter("Value for set_static.", Required = false)] public object value { get; set; }
        }

        private static Type ResolveType(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return null;
            return Type.GetType(name, false) ?? AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType(name, false)).FirstOrDefault(t => t != null) ??
                   AppDomain.CurrentDomain.GetAssemblies().SelectMany(a => { try { return a.GetTypes(); } catch { return Type.EmptyTypes; } }).FirstOrDefault(t => t.Name == name);
        }

        private static object Simplify(object value)
        {
            if (value == null) return null;
            if (value is UnityEngine.Object uo) return NeutronStarToolUtil.DescribeObject(uo);
            Type t = value.GetType();
            if (t.IsPrimitive || value is string || value is decimal || value is Enum) return value;
            try { return JToken.FromObject(value, JsonSerializer.CreateDefault()); }
            catch { return value.ToString(); }
        }

        private static object ConvertArg(JToken token, Type targetType)
        {
            if (token == null || token.Type == JTokenType.Null) return null;
            if (typeof(UnityEngine.Object).IsAssignableFrom(targetType)) return NeutronStarToolUtil.ResolveObject(token);
            if (targetType.IsEnum)
            {
                if (token.Type == JTokenType.Integer) return Enum.ToObject(targetType, token.Value<int>());
                return Enum.Parse(targetType, token.Value<string>(), true);
            }
            return token.ToObject(targetType, JsonSerializer.CreateDefault());
        }

        public static object HandleCommand(JObject p)
        {
            string action = (p?["action"]?.Value<string>() ?? "type_info").ToLowerInvariant();
            string typeName = p?["type"]?.Value<string>();
            Type type = ResolveType(typeName);
            if (type == null) return new ErrorResponse($"Type '{typeName}' could not be resolved.");
            string member = p?["member"]?.Value<string>();
            const BindingFlags flags = BindingFlags.Public | BindingFlags.Static;

            try
            {
                if (action == "type_info")
                    return new SuccessResponse("Type resolved.", new { type = type.FullName, assembly = type.Assembly.GetName().Name, properties = type.GetProperties(flags).Select(x => x.Name).Distinct().OrderBy(x => x).ToArray(), methods = type.GetMethods(flags).Select(x => x.Name).Distinct().OrderBy(x => x).ToArray() });
                if (action == "methods")
                    return new SuccessResponse("Methods listed.", new { methods = type.GetMethods(flags).Where(m => string.IsNullOrEmpty(member) || m.Name.IndexOf(member, StringComparison.OrdinalIgnoreCase) >= 0).Select(m => new { name = m.Name, returns = m.ReturnType.FullName, parameters = m.GetParameters().Select(x => new { name = x.Name, type = x.ParameterType.FullName, optional = x.IsOptional }).ToArray() }).ToArray() });
                if (action == "get_static")
                {
                    var prop = type.GetProperty(member ?? "", flags);
                    if (prop == null || !prop.CanRead) return new ErrorResponse($"Readable public static property '{member}' was not found.");
                    return new SuccessResponse("Static property read.", new { member, value = Simplify(prop.GetValue(null)) });
                }
                if (action == "set_static")
                {
                    var prop = type.GetProperty(member ?? "", flags);
                    if (prop == null || !prop.CanWrite) return new ErrorResponse($"Writable public static property '{member}' was not found.");
                    prop.SetValue(null, ConvertArg(p?["value"], prop.PropertyType));
                    return new SuccessResponse("Static property written.", new { member, value = Simplify(prop.GetValue(null)) });
                }
                if (action == "invoke_static")
                {
                    var argTokens = p?["args"] as JArray ?? new JArray();
                    var candidates = type.GetMethods(flags).Where(m => m.Name == member && m.GetParameters().Length == argTokens.Count).ToArray();
                    if (candidates.Length == 0) return new ErrorResponse($"No public static overload '{member}' with {argTokens.Count} arguments was found.");
                    Exception last = null;
                    foreach (var method in candidates)
                    {
                        try
                        {
                            var pars = method.GetParameters();
                            var args = new object[pars.Length];
                            for (int i = 0; i < pars.Length; i++) args[i] = ConvertArg(argTokens[i], pars[i].ParameterType);
                            return new SuccessResponse("Static method invoked.", new { member, result = Simplify(method.Invoke(null, args)) });
                        }
                        catch (Exception ex) { last = ex; }
                    }
                    return new ErrorResponse($"All overload attempts failed: {last?.GetBaseException().Message}");
                }
                return new ErrorResponse("Unknown action. Use type_info, methods, get_static, set_static, invoke_static.");
            }
            catch (Exception ex) { return new ErrorResponse($"Reflection operation failed: {ex.GetBaseException().Message}"); }
        }
    }
}
