using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Ascendant.Build;
using Ascendant.CelestialDial;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Ascendant.EraDemo
{
    // Scripted Play Mode pass through the walkable-era demo (task 86bcg8az2): DEV Mode opens it over the Wing-whole save, the Places list and a
    // tap on the map both walk the Keeper, Caspar follows, each person talks through the chat box, a choice waits, reduced motion jumps, and
    // the door home fades back to the Atrium. Captures each step. Not a human playtest.
    [InitializeOnLoad]
    public static class EraDemoPlayValidation
    {
        const string Flag = "AscendantEraDemoValidation", ReportPath = "Logs/era-demo-validation.txt", Evidence = "Logs/Evidence/era/";
        static readonly Queue<(string what, Func<bool> ready, Action act)> Steps = new Queue<(string, Func<bool>, Action)>();
        static readonly List<string> Report = new List<string>(), Errors = new List<string>();
        static double nextAt, limit; static float startRow;
        static SliceView View => UnityEngine.Object.FindFirstObjectByType<SliceView>();
        static EraDemoView Era => View != null ? View.Era : null;

        static EraDemoPlayValidation()
        {
            EditorApplication.playModeStateChanged += state =>
            {
                if (state == PlayModeStateChange.EnteredEditMode && Environment.GetCommandLineArgs().Contains("-eraAutoExit")) EditorApplication.Exit(File.Exists(ReportPath) && File.ReadAllText(ReportPath).StartsWith("PASS") ? 0 : 1);
                if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Flag, false)) Queue();
            };
        }
        [MenuItem("Ascendant/Era demo/Run Play Mode validation")]
        public static void Begin()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/VerticalSlice.unity");
            for (int i = 1; i <= SaveSlots.Count; i++) PlayerPrefs.DeleteKey(SaveSlots.Key(i)); PlayerPrefs.DeleteKey(SaveSlots.LastKey); PlayerPrefs.Save();
            Slots.Request(null, null);
            GreyboxPlayValidation.SetSize(390, 844); SessionState.SetBool(Flag, true);
            EditorApplication.EnterPlaymode();
        }
        static void Check(bool ok, string text) { if (!ok) throw new Exception(text); Report.Add("PASS: " + text); }
        static void Capture(string file) { Directory.CreateDirectory(Evidence); ScreenCapture.CaptureScreenshot(Evidence + file); }
        static void Step(string what, Func<bool> ready, Action act) => Steps.Enqueue((what, ready, act));
        static void Click(string path) { var t = Era.transform.Find(path); if (t == null) throw new Exception("no " + path); t.GetComponent<Button>().onClick.Invoke(); }
        static void Talked(string speaker, int lines)
        {
            for (int i = 0; i < lines; i++) { Check(Era.Talk.Open && Era.Speaker == speaker && Era.LineShown.StartsWith("[Placeholder]"), speaker + " line " + (i + 1) + ": " + Era.LineShown); Era.Next(); }
            Check(!Era.Talk.Open && Era.LineShown == "", speaker + ": the talk ends and the chat box closes");
        }

        static void Queue()
        {
            Report.Clear(); Steps.Clear(); Errors.Clear();
            Application.logMessageReceived += (m, s, t) => { if ((t == LogType.Error || t == LogType.Exception) && !s.Contains("UnityEditor.Search")) Errors.Add(m); };
            Step("the game", () => View != null && !View.Busy, () => View.Dial.WebAction("era-demo"));
            Step("the era demo after the reload", () => Era != null, () =>
            {
                Check(View.Flow.WingWhole && View.Flow.AtHub, "DEV Mode's era demo writes the Wing-whole save and reloads into the Atrium under it");
                var m = Era.Map; var cas = m.Point("caspar");
                Check(Era.Walker.At == m.Arrival && cas.At.Distance(Era.Walker.At) == 1, "the Keeper arrives at the door, Caspar beside him (" + cas.At + ")");
                Check(View.GearShown, "Settings' gear stays over the demo");
                Check(!View.GetComponentsInChildren<Canvas>(true).Any(c => c.enabled && c.gameObject.name == "Slice Canvas"), "the slice's canvas hides under the demo");
                startRow = -Era.Camera.transform.position.y;
                Capture("era-01-arrival.png");
            });
            Step("Places", () => true, () => { Click("Era Canvas/Era Portrait/Places"); Check(Era.PlacesOpen, "Places opens the list"); });
            Step("the list's capture", () => true, () => Capture("era-02-places.png"));
            Step("a row", () => true, () =>
            {
                Click("Era Canvas/Era Portrait/Places box/Place baker");
                Check(!Era.PlacesOpen && Era.Walker.Walking && Era.Walker.TargetId == "baker", "a Places row walks the Keeper to the baker");
            });
            Step("the baker", () => Era.Talk.Open, () =>
            {
                Check(Era.Map.Beside(Era.Map.Point("baker").At).Contains(Era.Walker.At) && Era.Map.Point("caspar").At.Distance(Era.Walker.At) == 1, "beside the baker, Caspar one square behind");
                Capture("era-03-baker.png");
            });
            Step("the baker's talk", () => true, () =>
            {
                Talked("THE BAKER", 2);
                // a tap on the map: the square three above the well, through the screen, as a finger would
                var target = new Cell(5, 9); var screen = Era.ScreenOf(target); Era.GetComponentInChildren<TapCatcher>().Tapped(screen);
                Check(Era.Walker.Walking, "a tap on the map at " + target + " walks him there (screen " + screen + ")");
            });
            Step("the tapped square", () => !Era.Walker.Walking, () =>
            {
                Check(Era.Walker.At == new Cell(5, 9), "he stops on the tapped square: " + Era.Walker.At);
                Capture("era-04-square.png");
                Check(Era.GoTo("teacher"), "the teacher, on the roof up the stairs");
            });
            Step("the teacher", () => Era.Talk.Open, () =>
            {
                float camY = -Era.Camera.transform.position.y, half = Era.Camera.orthographicSize;
                Check(camY < startRow - 3 && Mathf.Abs(camY - Era.Walker.Y) <= half - .5f, "the camera has followed him up the map and holds him in view (camera row " + camY.ToString("0.0") + " from " + startRow.ToString("0.0") + ", Keeper row " + Era.Walker.Y + ", half height " + half.ToString("0.0") + ")");
                Check(Era.Speaker == "THE TEACHER", "the teacher talks"); Era.Next();
                Check(Era.Talk.Waiting && !Era.Next(), "a choice waits: Continue does nothing");
                Capture("era-05-choice.png");
            });
            Step("the choice", () => true, () =>
            {
                Check(Era.Choose(0) && Era.Talk.Open && !Era.Talk.Waiting, "choice A leads on"); Era.Next();
                Check(!Era.Talk.Open, "the teacher's talk ends");
                View.Dial.WebAction("motion-on"); Check(Era.GoTo("copyist") && !Era.Walker.Walking, "reduced motion: he is simply there");
            });
            Step("the copyist", () => Era.Talk.Open, () =>
            {
                Capture("era-06-copyist.png");
            });
            Step("the copyist's talk", () => true, () =>
            {
                Talked("THE COPYIST", 2);
                View.Dial.WebAction("motion"); Check(Era.GoTo("caspar"), "Caspar, who follows");
            });
            Step("Caspar", () => Era.Talk.Open, () => { Talked("CASPAR", 1); Check(Era.GoTo("portal") && Era.Walker.Walking, "the door home"); });
            Step("home", () => Era == null && View != null && !View.Busy, () =>
            {
                Check(View.Flow.AtHub && View.Flow.WingWhole, "the door home fades back to the Atrium, the Wing whole");
                Capture("era-07-home.png");
            });
            Step("done", () => true, () => { Check(Errors.Count == 0, "no errors in the log" + (Errors.Count > 0 ? ": " + string.Join(" | ", Errors.Take(3)) : "")); Finish("PASS: " + Report.Count + " checks"); });
            nextAt = EditorApplication.timeSinceStartup + 2; limit = nextAt + 30; EditorApplication.update += Tick;
        }
        static void Tick()
        {
            if (EditorApplication.isPaused) EditorApplication.isPaused = false;
            EditorApplication.QueuePlayerLoopUpdate();
            if (Steps.Count == 0 || EditorApplication.timeSinceStartup < nextAt) return;
            var (what, ready, act) = Steps.Peek();
            bool ok = false; try { ok = ready(); } catch (Exception) { }
            if (!ok) { if (EditorApplication.timeSinceStartup > limit) Finish("FAIL: timed out waiting for " + what); return; }
            Steps.Dequeue();
            try { act(); } catch (Exception e) { Debug.LogException(e); Finish("FAIL: " + e.Message); return; }
            nextAt = EditorApplication.timeSinceStartup + .8; limit = nextAt + 30;
        }
        static void Finish(string head)
        {
            Steps.Clear(); EditorApplication.update -= Tick; SessionState.SetBool(Flag, false);
            Directory.CreateDirectory("Logs"); File.WriteAllText(ReportPath, head + "\n" + string.Join("\n", Report) + "\n");
            Debug.Log("[EraDemoPlayValidation] " + head);
            EditorApplication.ExitPlaymode();
        }
    }
}
