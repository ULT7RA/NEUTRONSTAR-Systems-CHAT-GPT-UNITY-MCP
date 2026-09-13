using System;
using System.Linq;
using System.Reflection;
using MCPForUnity.Editor.Helpers;
using Newtonsoft.Json.Linq;

namespace MCPForUnity.Editor.Tools.NeutronStar
{
    [McpForUnityTool("navmesh_control", Description = "Build or clear the legacy/editor NavMesh through UnityEditor.AI using reflection, avoiding a hard package dependency.")]
    public static class NavMeshTool
    {
        public class Parameters
        {
            [ToolParameter("Action: availability, build, clear.")] public string action { get; set; }
        }

        private static Type FindType()
        {
            return Type.GetType("UnityEditor.AI.NavMeshBuilder, UnityEditor") ??
                   AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("UnityEditor.AI.NavMeshBuilder", false)).FirstOrDefault(t => t != null);
        }

        public static object HandleCommand(JObject p)
        {
            string action = (p?["action"]?.Value<string>() ?? "availability").ToLowerInvariant();
            Type type = FindType();
            if (action == "availability")
                return new SuccessResponse("NavMesh API checked.", new { available = type != null, type = type?.AssemblyQualifiedName });
            if (type == null) return new ErrorResponse("UnityEditor.AI.NavMeshBuilder is not available in this Unity installation/project.");
            string methodName = action == "build" ? "BuildNavMesh" : action == "clear" ? "ClearAllNavMeshes" : null;
            if (methodName == null) return new ErrorResponse("Unknown action. Use availability, build, clear.");
            var method = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static, null, Type.EmptyTypes, null);
            if (method == null) return new ErrorResponse($"NavMesh method '{methodName}' is not available.");
            try
            {
                method.Invoke(null, null);
                return new SuccessResponse($"NavMesh {action} completed.");
            }
            catch (TargetInvocationException ex) { return new ErrorResponse($"NavMesh {action} failed: {ex.InnerException?.Message ?? ex.Message}"); }
        }
    }
}
