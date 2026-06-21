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

        // ── Path resolution — read this before changing anything ─────────────
        //
        // game-ci/unity-builder@v4 passes these env vars into Docker (confirmed
        // from CI logs):
        //
        //   BUILD_PATH = ci-build/WebGL       ← buildsPath/targetPlatform (RELATIVE)
        //   BUILD_FILE = EternalKingdoms      ← buildName
        //
        // CRITICAL: Unity changes its CWD to the project path on startup.
        // Inside the container that means CWD = /github/workspace/unity-client.
        // Path.GetFullPath("ci-build/WebGL/EternalKingdoms") would therefore
        // resolve to /github/workspace/unity-client/ci-build/... (WRONG).
        //
        // The ONLY reliable absolute anchor inside Unity is Application.dataPath:
        //   Application.dataPath = /github/workspace/unity-client/Assets  (always absolute)
        //   ../..                = /github/workspace                       (workspace root)
        //
        // We ALWAYS derive the workspace root from Application.dataPath and
        // treat BUILD_PATH as relative to that root — never relative to CWD.
        //
        string gameCiBuildPath = Environment.GetEnvironmentVariable("BUILD_PATH");
        string gameCiBuildFile = Environment.GetEnvironmentVariable("BUILD_FILE")
                              ?? Environment.GetEnvironmentVariable("BUILD_NAME");

        // Application.dataPath = <projectPath>/Assets  (guaranteed absolute)
        // parent of Assets = <projectPath>
        // parent of <projectPath> = workspace root  (= /github/workspace in CI)
        string workspaceRootAbs = Path.GetFullPath(
            Path.Combine(Application.dataPath, "..", ".."));

        Debug.Log($"[BuildScript] Application.dataPath   : {Application.dataPath}");
        Debug.Log($"[BuildScript] Workspace root (abs)   : {workspaceRootAbs}");
        Debug.Log($"[BuildScript] CWD at build time      : {Directory.GetCurrentDirectory()}");
        Debug.Log($"[BuildScript] BUILD_PATH env          : {gameCiBuildPath ?? "(not set)"}");
        Debug.Log($"[BuildScript] BUILD_FILE env          : {gameCiBuildFile ?? "(not set)"}");

        string outputPath;
        if (!string.IsNullOrEmpty(gameCiBuildPath))
        {
            // Anchor the RELATIVE BUILD_PATH against the workspace root, not CWD.
            string rel = string.IsNullOrEmpty(gameCiBuildFile)
                ? gameCiBuildPath
                : Path.Combine(gameCiBuildPath, gameCiBuildFile);
            outputPath = Path.GetFullPath(Path.Combine(workspaceRootAbs, rel));
        }
        else
        {
            // Local / editor fallback: DefaultBuildSubPath is also relative to
            // the workspace root, so anchor it the same way.
            outputPath = Path.GetFullPath(Path.Combine(workspaceRootAbs, DefaultBuildSubPath));
        }

        Debug.Log($"[BuildScript] Final output path      : {outputPath}");
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
