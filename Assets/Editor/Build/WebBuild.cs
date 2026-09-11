using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Ascendant.Build
{
    /// <summary>
    /// Headless Web (WebGL) player build entry point, shared by the local Unity CLI build and
    /// the GitHub Actions build (game-ci/unity-builder).
    ///
    /// Local (Unity CLI):
    ///   unity build . --target WebGL --execute-method Ascendant.Build.WebBuild.Build --output-path Builds/Web
    /// Local (raw Unity batch mode, what the CLI and game-ci wrap; the project must not be open in the Editor):
    ///   Unity -batchmode -nographics -quit -projectPath . -buildTarget WebGL \
    ///         -executeMethod Ascendant.Build.WebBuild.Build -buildOutput Builds/Web -logFile Logs/web-build.log
    ///
    /// Builds the scenes enabled in Build Settings with the project's Web player settings.
    /// The output directory comes from -buildOutput (Unity CLI) or -customBuildPath (game-ci),
    /// falling back to Builds/Web.
    /// </summary>
    public static class WebBuild
    {
        const string DefaultOutputPath = "Builds/Web";

        public static void Build()
        {
            string[] args = Environment.GetCommandLineArgs();
            string outputPath = GetArgValue(args, "-buildOutput")
                ?? GetArgValue(args, "-customBuildPath")
                ?? DefaultOutputPath;

            string[] scenes = EditorBuildSettings.scenes
                .Where(scene => scene.enabled)
                .Select(scene => scene.path)
                .ToArray();

            if (scenes.Length == 0)
            {
                Fail("No scenes are enabled in Build Settings.");
                return;
            }

            Debug.Log($"[WebBuild] Building {scenes.Length} scene(s) for WebGL to '{outputPath}': {string.Join(", ", scenes)}");

            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = outputPath,
                target = BuildTarget.WebGL,
                targetGroup = BuildTargetGroup.WebGL,
                options = BuildOptions.None,
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;

            Debug.Log($"[WebBuild] Result: {summary.result} | output: {summary.outputPath} | " +
                      $"size: {summary.totalSize} bytes | errors: {summary.totalErrors} | " +
                      $"warnings: {summary.totalWarnings} | time: {summary.totalTime}");

            if (summary.result != BuildResult.Succeeded)
            {
                Fail($"Web build finished with result {summary.result} ({summary.totalErrors} error(s)).");
            }
        }

        static string GetArgValue(string[] args, string name)
        {
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase))
                {
                    return args[i + 1];
                }
            }

            return null;
        }

        static void Fail(string message)
        {
            Debug.LogError("[WebBuild] " + message);

            if (Application.isBatchMode)
            {
                EditorApplication.Exit(1);
            }

            throw new BuildFailedException(message);
        }
    }
}
