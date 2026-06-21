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
    // Relative-path fallback — only used when BUILD_PATH env var is not set.
    // DO NOT rely on this path being resolved relative to Unity's CWD (which
    // is the Unity editor install dir, not the project or workspace root).
    // Instead we compute an absolute path from Application.dataPath at runtime.
    // This const is kept only as the leaf structure hint.
    private const string DefaultBuildSubPath = "ci-build/WebGL/EternalKingdoms";

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

        // game-ci/unity-builder@v4 passes TWO separate env vars into the Docker
        // container (confirmed from CI logs — the docker run command shows):
        //
        //   BUILD_PATH=ci-build/WebGL        ← buildsPath/targetPlatform
        //   BUILD_FILE=EternalKingdoms       ← buildName (the output folder name)
        //
        // The full output path is BUILD_PATH/BUILD_FILE.
        // game-ci OVERRIDES any BUILD_PATH set in the workflow's env: block,
        // so setting it there has no effect — this code is the only fix point.
        //
        // When running locally (no game-ci), we derive the workspace root from
        // Application.dataPath so output lands at the repo root rather than
        // Unity's install dir (where bare relative paths resolve to).
        //
        string gameCiBuildPath = Environment.GetEnvironmentVariable("BUILD_PATH");
        string gameCiBuildFile = Environment.GetEnvironmentVariable("BUILD_FILE")
                              ?? Environment.GetEnvironmentVariable("BUILD_NAME");

        string outputPath;
        if (!string.IsNullOrEmpty(gameCiBuildPath))
        {
            // Combine BUILD_PATH + BUILD_FILE, then resolve to absolute.
            // Docker workdir = /github/workspace, so relative paths resolve there.
            string combined = string.IsNullOrEmpty(gameCiBuildFile)
                ? gameCiBuildPath
                : Path.Combine(gameCiBuildPath, gameCiBuildFile);
            outputPath = Path.GetFullPath(combined);
            Debug.Log($"[BuildScript] game-ci env  BUILD_PATH : {gameCiBuildPath}");
            Debug.Log($"[BuildScript] game-ci env  BUILD_FILE : {gameCiBuildFile ?? "(not set)"}");
            Debug.Log($"[BuildScript] Resolved output path    : {outputPath}");
        }
        else
        {
            // Local / editor fallback:
            //   Application.dataPath = <workspace>/unity-client/Assets
            //   two levels up        = <workspace>   (repo root)
            string workspaceRoot = Path.GetFullPath(
                Path.Combine(Application.dataPath, "..", ".."));
            outputPath = Path.Combine(workspaceRoot, DefaultBuildSubPath);
            Debug.Log($"[BuildScript] BUILD_PATH not set — derived path: {outputPath}");
        }
        Directory.CreateDirectory(outputPath);

        Debug.Log($"[BuildScript] Building {scenes.Length} scene(s): {string.Join(", ", scenes)}");
        Debug.Log($"[BuildScript] Output (absolute): {outputPath}");

        return new BuildPlayerOptions
        {
            scenes           = scenes,
            locationPathName = outputPath,
            target           = target,
            options          = BuildOptions.None,
        };
    }
}
