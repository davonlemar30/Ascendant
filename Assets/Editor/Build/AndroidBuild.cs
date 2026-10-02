using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Ascendant.Build
{
    /// <summary>
    /// Headless Android playtest build: an APK for the owner's own phone, sideloaded. It is not the
    /// store package, which the Sept 13 scope ruling defers to the full game (task 86bc8ddy2).
    ///
    /// Raw Unity batch mode (needs the Android Build Support module; the project must not be open in the Editor):
    ///   Unity -batchmode -nographics -quit -projectPath . -buildTarget Android \
    ///         -executeMethod Ascendant.Build.AndroidBuild.Build -buildOutput Builds/Android/Ascendant.apk -logFile Logs/android-build.log
    ///
    /// Builds the same scenes as WebBuild.Build after the same greybox checks. The playtest settings are set
    /// here, not left to the Editor: a development build signed with the debug keystore, IL2CPP for ARM64,
    /// portrait only, and fullscreen drawn behind the cutout (Platform fit, Part 1, owner Oct 1: "Hide the bar
    /// and draw behind it"; task 86bcbn6mf). The bars hide in immersive fullscreen (SafeArea.Immersive) and the
    /// top row moves down by the safe area's band (SafeArea.TopInset), so nothing tappable sits under the cutout.
    /// Until Oct 2 this file lived only on the codex/android-apk branch, kept inside the safe area; on the owner's
    /// phone that showed Android's grey status bar above the game, with the frame pillarboxed under it.
    /// The package name is the owner's GitHub Pages domain reversed (delegated to Claude, Sept 28), so every
    /// playtest APK installs as an update of the last; a store build may still choose another before its first upload.
    /// The output path comes from -buildOutput, falling back to Builds/Android/Ascendant.apk.
    /// </summary>
    public static class AndroidBuild
    {
        const string DefaultOutputPath = "Builds/Android/Ascendant.apk";
        const string PackageName = "io.github.davonlemar30.ascendant";

        public static void Build()
        {
            // The greybox domain checks run before every player build.
            GreyboxValidation.Run();

            string[] args = Environment.GetCommandLineArgs();
            string outputPath = GetArgValue(args, "-buildOutput") ?? DefaultOutputPath;

            string[] scenes = EditorBuildSettings.scenes
                .Where(scene => scene.enabled)
                .Select(scene => scene.path)
                .ToArray();

            if (scenes.Length == 0)
            {
                Fail("No scenes are enabled in Build Settings.");
                return;
            }

            var android = NamedBuildTarget.Android;
            PlayerSettings.SetApplicationIdentifier(android, PackageName);
            PlayerSettings.SetScriptingBackend(android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.useCustomKeystore = false;
            PlayerSettings.Android.renderOutsideSafeArea = true; // Part 1: the art fills the cutout's strip; SafeArea moves the top row below it
            PlayerSettings.Android.startInFullscreen = true; PlayerSettings.Android.fullscreenMode = FullScreenMode.FullScreenWindow; // immersive from the first frame
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            EditorUserBuildSettings.buildAppBundle = false;

            string directory = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            Debug.Log($"[AndroidBuild] Building {scenes.Length} scene(s) for Android to '{outputPath}': {string.Join(", ", scenes)} | " +
                      $"id: {PlayerSettings.GetApplicationIdentifier(android)} | version: {PlayerSettings.bundleVersion} ({PlayerSettings.Android.bundleVersionCode}) | " +
                      $"min SDK: {PlayerSettings.Android.minSdkVersion}");

            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = outputPath,
                target = BuildTarget.Android,
                targetGroup = BuildTargetGroup.Android,
                options = BuildOptions.Development,
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;

            Debug.Log($"[AndroidBuild] Result: {summary.result} | output: {summary.outputPath} | " +
                      $"size: {summary.totalSize} bytes | errors: {summary.totalErrors} | " +
                      $"warnings: {summary.totalWarnings} | time: {summary.totalTime}");

            if (summary.result != BuildResult.Succeeded)
            {
                Fail($"Android build finished with result {summary.result} ({summary.totalErrors} error(s)).");
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
            Debug.LogError("[AndroidBuild] " + message);

            if (Application.isBatchMode)
            {
                EditorApplication.Exit(1);
            }

            throw new BuildFailedException(message);
        }
    }
}
