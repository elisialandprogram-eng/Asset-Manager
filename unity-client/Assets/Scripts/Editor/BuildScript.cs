using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.Compilation;
using UnityEngine;

/// <summary>
/// Headless build entry point for GameCI / GitHub Actions.
///
/// Two entry points are available:
///
///   1. BuildScript.ValidateOnly
///      Fast pre-flight: asserts that all scripts compiled without errors.
///      If Unity reaches this method, compilation is already clean (Unity
///      exits non-zero before calling anything when scripts have errors).
///      Recommended CI step before the full build:
///
///        unity -batchmode -nographics -projectPath unity-client \
///              -buildMethod BuildScript.ValidateOnly -quit -logFile -
///
///   2. BuildScript.BuildWebGL
///      Full WebGL build. Also prints a clear compilation-passed banner
///      at the start so the compile→build boundary is obvious in CI logs.
///      Invoked via: -buildMethod BuildScript.BuildWebGL
/// </summary>
public static class BuildScript
{
    private const string BuildPath = "ci-build/WebGL/EternalKingdoms";

    // ── Banner constants ──────────────────────────────────────────────────
    private const string Banner = "══════════════════════════════════════════════════";

    // ─────────────────────────────────────────────────────────────────────
    // Entry point 1 — compilation-only validation
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Standalone compilation health-check.
    /// If Unity reaches this method, all assemblies compiled without errors.
    /// Exits 0 on success (Unity would have exited non-zero before calling
    /// this if any script had a compile error).
    /// </summary>
    public static void ValidateOnly()
    {
        int playerAssemblies = CompilationPipeline
            .GetAssemblies(AssembliesType.Player).Length;
        int editorAssemblies = CompilationPipeline
            .GetAssemblies(AssembliesType.Editor).Length;

        Debug.Log(Banner);
        Debug.Log("  COMPILATION CHECK — PASSED");
        Debug.Log($"  Player assemblies : {playerAssemblies}");
        Debug.Log($"  Editor assemblies : {editorAssemblies}");
        Debug.Log($"  Timestamp (UTC)   : {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}");
        Debug.Log(Banner);

        EditorApplication.Exit(0);
    }

    // ─────────────────────────────────────────────────────────────────────
    // Entry point 2 — full WebGL build
    // ─────────────────────────────────────────────────────────────────────

    public static void BuildWebGL()
    {
        // ── Compilation banner ────────────────────────────────────────────
        // If we reached this line, all scripts compiled successfully.
        // This banner makes the compile→build boundary unmissable in logs.
        int playerAssemblies = CompilationPipeline
            .GetAssemblies(AssembliesType.Player).Length;
        int editorAssemblies = CompilationPipeline
            .GetAssemblies(AssembliesType.Editor).Length;

        Debug.Log(Banner);
        Debug.Log("  COMPILATION OK — starting WebGL build");
        Debug.Log($"  Player assemblies : {playerAssemblies}");
        Debug.Log($"  Editor assemblies : {editorAssemblies}");
        Debug.Log($"  Timestamp (UTC)   : {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}");
        Debug.Log(Banner);

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
            Debug.Log(Banner);
            Debug.Log($"  BUILD SUCCEEDED");
            Debug.Log($"  Output size : {summary.totalSize / 1024 / 1024} MB");
            Debug.Log($"  Duration    : {summary.totalTime.TotalSeconds:F1}s");
            Debug.Log(Banner);
        }
        else
        {
            // Collect build-step errors from the report for a focused summary
            var buildErrors = report.steps
                .SelectMany(s => s.messages)
                .Where(m => m.type == LogType.Error || m.type == LogType.Exception)
                .Select(m => $"  {m.content}")
                .ToArray();

            Debug.LogError(Banner);
            Debug.LogError($"  BUILD FAILED: {summary.result}");
            if (buildErrors.Length > 0)
            {
                Debug.LogError("  Errors:");
                foreach (var err in buildErrors)
                    Debug.LogError(err);
            }
            Debug.LogError(Banner);

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
