using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
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

        // ── WebGL player settings ─────────────────────────────────────────
        PlayerSettings.WebGL.compressionFormat     = WebGLCompressionFormat.Gzip;
        PlayerSettings.WebGL.decompressionFallback = true;
        PlayerSettings.WebGL.exceptionSupport      = WebGLExceptionSupport.FullWithStacktrace;
        PlayerSettings.WebGL.dataCaching           = true;
        PlayerSettings.WebGL.linkerTarget          = WebGLLinkerTarget.Wasm;

        // Memory — 512 MB initial, up to 2 GB via geometric growth (Unity 6)
        PlayerSettings.WebGL.initialMemorySize  = 512;
        PlayerSettings.WebGL.maximumMemorySize  = 2048;
        PlayerSettings.WebGL.memoryGrowthMode   = WebGLMemoryGrowthMode.Geometric;

        // ── Unity 6 API: use NamedBuildTarget instead of BuildTargetGroup ─
        var webgl = NamedBuildTarget.WebGL;

        PlayerSettings.SetScriptingBackend(webgl, ScriptingImplementation.IL2CPP);
        PlayerSettings.SetScriptingDefineSymbols(webgl, "EK_CLIENT;EK_WEBGL");

        // ── Build ─────────────────────────────────────────────────────────
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

    // ── helpers ───────────────────────────────────────────────────────────

    private static BuildPlayerOptions GetDefaultOptions(BuildTarget target)
    {
        string[] scenes = EditorBuildSettings.scenes
            .Where(s => s.enabled)
            .Select(s => s.path)
            .ToArray();

        if (scenes.Length == 0)
            throw new Exception("[BuildScript] No enabled scenes in EditorBuildSettings.");

        string outputPath = Environment.GetEnvironmentVariable("BUILD_PATH") ?? BuildPath;
        Directory.CreateDirectory(outputPath);

        Debug.Log($"[BuildScript] Building {scenes.Length} scene(s): {string.Join(", ", scenes)}");
        Debug.Log($"[BuildScript] Output: {outputPath}");

        return new BuildPlayerOptions
        {
            scenes           = scenes,
            locationPathName = outputPath,
            target           = target,
            options          = BuildOptions.None,
        };
    }
}
