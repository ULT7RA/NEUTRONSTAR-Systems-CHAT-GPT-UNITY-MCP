using System;
using System.Linq;
using MCPForUnity.Editor.Helpers;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace MCPForUnity.Editor.Tools.NeutronStar
{
    [McpForUnityTool("editor_windows", Description = "Inspect, focus, repaint, and close Unity Editor windows by title or type.")]
    public static class EditorWindowsTool
    {
        public class Parameters
        {
            [ToolParameter("Action: list, focus, repaint, close.")] public string action { get; set; }
            [ToolParameter("Window title or type-name substring for focus/repaint/close.", Required = false)] public string query { get; set; }
        }

        private static EditorWindow[] All() => UnityEngine.Resources.FindObjectsOfTypeAll<EditorWindow>();
        private static EditorWindow Find(string query) => All().FirstOrDefault(w =>
            (!string.IsNullOrEmpty(w.titleContent?.text) && w.titleContent.text.IndexOf(query ?? "", StringComparison.OrdinalIgnoreCase) >= 0) ||
            w.GetType().FullName.IndexOf(query ?? "", StringComparison.OrdinalIgnoreCase) >= 0);

        public static object HandleCommand(JObject p)
        {
            string action = (p?["action"]?.Value<string>() ?? "list").ToLowerInvariant();
            if (action == "list")
                return new SuccessResponse("Editor windows listed.", new { windows = All().Select(w => new { title = w.titleContent?.text, type = w.GetType().FullName, focused = EditorWindow.focusedWindow == w, position = new { x = w.position.x, y = w.position.y, width = w.position.width, height = w.position.height } }).ToArray() });

            string query = p?["query"]?.Value<string>();
            if (string.IsNullOrWhiteSpace(query)) return new ErrorResponse("query is required.");
            var window = Find(query);
            if (window == null) return new ErrorResponse($"No EditorWindow matched '{query}'.");
            switch (action)
            {
                case "focus": window.Focus(); break;
                case "repaint": window.Repaint(); break;
                case "close": window.Close(); break;
                default: return new ErrorResponse("Unknown action. Use list, focus, repaint, close.");
            }
            return new SuccessResponse($"Editor window {action} completed.", new { title = window.titleContent?.text, type = window.GetType().FullName });
        }
    }
}
