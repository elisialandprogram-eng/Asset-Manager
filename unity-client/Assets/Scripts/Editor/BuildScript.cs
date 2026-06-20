using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// Headless build entry point for GameCI / GitHub Actions.
/// Invoked via: -buildMethod BuildScript.BuildWebGL
/// </summary>
public static class BuildScript
{
    private const string BuildPath = "ci-build/WebGL/EternalKingdoms";

    public static void BuildWebGL()
    {
        BuildPlayerOptions opts = GetDefaultOptions(BuildTarget.WebGL);

        // WebGL-specific settings
        PlayerSettings.WebGL.compressionFormat   = WebGLCompressionFormat.Gzip;
        PlayerSettings.WebGL.decompressionFallback = true;
        PlayerSettings.WebGL.exceptionSupport    = WebGLExceptionSupport.FullWithStacktrace;
        PlayerSettings.WebGL.dataCaching          = true;
        PlayerSettings.WebGL.linkerTarget         = WebGLLinkerTarget.Wasm;

        // Memory — 512 MB initial, 2 GB max
        PlayerSettings.WebGL.initialMemorySize   = 32;  // in MB (Unity 6 uses growth mode)
        PlayerSettings.WebGL.maximumMemorySize   = 2048;
        PlayerSettings.WebGL.memoryGrowthMode    = WebGLMemoryGrowthMode.Geometric;

        // Optimisation
        PlayerSettings.SetScriptingBackend(BuildTargetGroup.WebGL, ScriptingImplementation.IL2CPP);

        // Scripting defines for WebGL
        PlayerSettings.SetScriptingDefineSymbolsForGroup(
            BuildTargetGroup.WebGL,
            "EK_CLIENT;EK_WEBGL"
        );

        BuildReport  report  = BuildPipeline.BuildPlayer(opts);
        BuildSummary summary = report.summary;

        if (summary.result == BuildResult.Succeeded)
        {
            Debug.Log($"[BuildScript] WebGL build succeeded. Size: {summary.totalSize / 1024 / 1024} MB");
        }
        else
        {
            Debug.LogError($"[BuildScript] WebGL build FAILED: {summary.result}");
            EditorApplication.Exit(1);
        }
    }

    // ── helpers ────────────────────────────────────────────────────────────

    private static BuildPlayerOptions GetDefaultOptions(BuildTarget target)
    {
        string[] scenes = EditorBuildSettings.scenes
            .Where(s => s.enabled)
            .Select(s => s.path)
            .ToArray();

        if (scenes.Length == 0)
            throw new Exception("[BuildScript] No scenes found in EditorBuildSettings.");

        string outputPath = Environment.GetEnvironmentVariable("BUILD_PATH") ?? BuildPath;
        Directory.CreateDirectory(outputPath);

        Debug.Log($"[BuildScript] Building scenes: {string.Join(", ", scenes)}");
        Debug.Log($"[BuildScript] Output: {outputPath}");

        return new BuildPlayerOptions
        {
            scenes      = scenes,
            locationPathName = outputPath,
            target      = target,
            options     = BuildOptions.None,
        };
    }
}
