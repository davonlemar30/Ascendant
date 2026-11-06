using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Ascendant.Build
{
    // game-ci already owns Unity's first -logfile argument. Capture the fixture's
    // completion message directly, without changing its checks or its exit code.
    [InitializeOnLoad]
    public static class ReleaseFixtureEvidence
    {
        static ReleaseFixtureEvidence()
        {
            if (!Environment.GetCommandLineArgs().Contains("-releaseFixtureEvidence")) return;
            Application.logMessageReceived += (message, stack, type) =>
            {
                if (!message.StartsWith("[SlicePlayValidation] PASS: ", StringComparison.Ordinal)) return;
                Directory.CreateDirectory("Logs");
                File.AppendAllText("Logs/release-fixture-completion.txt", message + Environment.NewLine);
            };
        }
    }
}
