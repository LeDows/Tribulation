using System.IO;
using UnityEditor;
using UnityEngine;

namespace Tribulation.Editor
{
    [InitializeOnLoad]
    internal static class McpForUnityLocalServerSetup
    {
        private const string UvxPathPrefKey = "MCPForUnity.UvxPath";
        private const string ProjectScopedToolsPrefKey = "MCPForUnity.ProjectScopedTools.LocalHttp";

        static McpForUnityLocalServerSetup()
        {
            var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            var shimPath = Path.Combine(projectRoot, "Tools", "MCP", "mcp-for-unity-uvx-shim.cmd");

            if (!File.Exists(shimPath))
            {
                return;
            }

            EditorPrefs.SetString(UvxPathPrefKey, shimPath);
            EditorPrefs.SetBool(ProjectScopedToolsPrefKey, true);
        }
    }
}
