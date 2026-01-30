using UnityEngine;
using System.Runtime.InteropServices;

/// <summary>
/// Initializes console warning filtering for WebGL builds.
/// This filters out known non-critical WebGL warnings like
/// "INVALID_ENUM: getInternalformatParameter: invalid internalformat"
/// which is a known browser/Unity interaction issue that doesn't affect functionality.
/// </summary>
public static class WebGLConsoleFilter
{
#if !UNITY_EDITOR && UNITY_WEBGL
    [DllImport("__Internal")]
    private static extern void InitializeConsoleWarningFilter();
#endif

    /// <summary>
    /// Automatically called before the first scene loads.
    /// This ensures the console filter is active as early as possible.
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        // Initialize the console warning filter as early as possible
#if !UNITY_EDITOR && UNITY_WEBGL
        try
        {
            InitializeConsoleWarningFilter();
            Debug.Log("[WebGLConsoleFilter] Console warning filter initialized successfully");
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[WebGLConsoleFilter] Failed to initialize console filter: {e.Message}");
        }
#endif
    }
}
