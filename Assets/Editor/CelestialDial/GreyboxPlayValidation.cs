using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Ascendant.CelestialDial;

namespace Ascendant.Build
{
    [InitializeOnLoad]
    public static class GreyboxPlayValidation
    {
        static readonly Queue<Action> Steps=new Queue<Action>();
        static readonly List<string> Report=new List<string>();
        static readonly List<string> RuntimeErrors=new List<string>();
        static double nextAt;
        static DialView View => UnityEngine.Object.FindFirstObjectByType<DialView>();
        static GreyboxPlayValidation()
        {
            EditorApplication.playModeStateChanged+=state=>{
                if(state==PlayModeStateChange.EnteredEditMode && System.Environment.GetCommandLineArgs().Contains("-greyboxAutoExit")) EditorApplication.Exit(File.Exists("Logs/greybox-play-validation.txt") && File.ReadAllText("Logs/greybox-play-validation.txt").StartsWith("PASS") ? 0 : 1);
                if(state==PlayModeStateChange.EnteredPlayMode && SessionState.GetBool("AscendantGreyboxPlayValidation",false)) QueueChecks();
            };
        }
        [MenuItem("Ascendant/Greybox/Run Play Mode validation")]
        public static void Begin()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/CelestialDial.unity");
            SetSize(390,844);SessionState.SetBool("AscendantGreyboxPlayValidation",true);
            EditorApplication.EnterPlaymode();
        }
        static void Check(bool value,string text) { if(!value)throw new Exception(text);Report.Add("PASS: "+text); }
        static void Click(string name) => UnityEngine.Object.FindObjectsByType<Button>(FindObjectsSortMode.None).First(b=>b.name==name).onClick.Invoke();
        static void QueueChecks()
        {
            Report.Clear();Steps.Clear();RuntimeErrors.Clear();Application.logMessageReceived+=CaptureLog;
            Steps.Enqueue(()=>{Check(View!=null,"scene creates Dial view");var c=UnityEngine.Object.FindFirstObjectByType<Canvas>(); Debug.Log("[GreyboxDimensions] screen="+Screen.width+"x"+Screen.height+" canvas="+c.pixelRect+" scale="+c.scaleFactor+" root="+c.transform.Find("Portrait").localScale);Capture("editor-390-encounter.png");});
            Steps.Enqueue(()=>Click("Continue"));
            Steps.Enqueue(()=>{Check(View.Lesson.Lit[1],"teaching sign reveal");Click("Continue");});
            Steps.Enqueue(()=>{for(int i=0;i<5;i++)Click("Next");Click("Previous");Check(View.Lesson.Dial.Selected==5 && View.Lesson.Dial.Attempts==0,"button overshoot/correction does not submit");Click("KEEPER'S\nSEAL");});
            Steps.Enqueue(()=>{Check(View.Lesson.Dial.Start==5,"correct Seal advances after silent home");View.WebAction("seat:9");Check(View.Lesson.Dial.Attempts==0,"accessible direct select does not submit");View.WebAction("seal");});
            Steps.Enqueue(()=>{Check(View.Lesson.Phase==LessonPhase.Transfer,"guided family completes");Capture("editor-390-guided.png");Click("Continue");});
            Steps.Enqueue(()=>{View.WebAction("motion");Check(!View.Lesson.Dial.CanInertia,"reduced motion disables inertia");for(int i=0;i<4;i++)View.WebAction("keyboard-forward");View.WebAction("seal");});
            Steps.Enqueue(()=>{Check(View.Lesson.Dial.Start==4,"first independent problem completes");View.WebAction("count");View.WebAction("seat:8");View.WebAction("seal");});
            Steps.Enqueue(()=>{Check(View.Lesson.KeyEarned && View.Lesson.Lit.Count(v=>v)==6,"full Play Mode flow ends at six lit seats and eligible Key");Capture("editor-390-complete.png");SetSize(360,800);});
            Steps.Enqueue(()=>{Check(UnityEngine.Object.FindFirstObjectByType<Canvas>().pixelRect.size==new Vector2(360,800),"small portrait viewport");Capture("editor-360-complete.png");});
            Steps.Enqueue(()=>{
                var seats=UnityEngine.Object.FindObjectsByType<Button>(FindObjectsInactive.Exclude,FindObjectsSortMode.None).Where(b=>b.GetComponent<DialDrag>()!=null).ToArray();
                Check(seats.Length==12,"all twelve seat objects exist");
                foreach(var b in seats){var r=(RectTransform)b.transform;Check(r.rect.width>=48 && r.rect.height>=48,"seat target floor: "+b.name);}
                Check(RuntimeErrors.Count==0,"no runtime errors: "+string.Join("; ",RuntimeErrors));
                Application.logMessageReceived-=CaptureLog;
                Directory.CreateDirectory("Logs");File.WriteAllLines("Logs/greybox-play-validation.txt",Report);
                Debug.Log("[GreyboxPlayValidation] PASS: "+Report.Count+" checks.");
                SessionState.SetBool("AscendantGreyboxPlayValidation",false);EditorApplication.update-=Tick;EditorApplication.ExitPlaymode();
            });
            nextAt=EditorApplication.timeSinceStartup+2;EditorApplication.update+=Tick;
        }
        static void CaptureLog(string message,string stack,LogType type)
        {
            if((type==LogType.Error || type==LogType.Exception) && !stack.Contains("UnityEditor.Search.SearchDatabase")) RuntimeErrors.Add(message);
        }
        static void Tick()
        {
            // A fresh-worktree Search indexing error can leave the Editor paused before gameplay starts.
            // Record that tooling state, then resume the fixture; runtime failures still fail its assertions.
            if(EditorApplication.isPaused){Debug.LogWarning("[GreyboxPlayValidation] Resuming paused Editor for fixture.");EditorApplication.isPaused=false;}
            EditorApplication.QueuePlayerLoopUpdate();
            if(EditorApplication.timeSinceStartup<nextAt || Steps.Count==0)return;
            nextAt=EditorApplication.timeSinceStartup+4;
            try{Steps.Dequeue()();}
            catch(Exception e)
            {
                Debug.LogException(e);Directory.CreateDirectory("Logs");File.WriteAllText("Logs/greybox-play-validation.txt","FAIL: "+e);
                SessionState.SetBool("AscendantGreyboxPlayValidation",false);EditorApplication.update-=Tick;EditorApplication.ExitPlaymode();
            }
        }
        static void Capture(string file)
        {Directory.CreateDirectory("Logs/Evidence");ScreenCapture.CaptureScreenshot("Logs/Evidence/"+file);}
        static void SetSize(int width,int height)
        {
            // Editor-only test fixture: add a fixed Game View size through the Editor API.
            var assembly=typeof(Editor).Assembly;
            var sizes=assembly.GetType("UnityEditor.GameViewSizes");
            var singleton=typeof(ScriptableSingleton<>).MakeGenericType(sizes);
            var instance=singleton.GetProperty("instance").GetValue(null);
            var group=sizes.GetMethod("GetGroup").Invoke(instance,new object[]{0});
            var sizeType=assembly.GetType("UnityEditor.GameViewSize");
            var kind=assembly.GetType("UnityEditor.GameViewSizeType");
            var size=Activator.CreateInstance(sizeType,BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance,null,
                new object[]{Enum.ToObject(kind,1),width,height,"Ascendant "+width+"x"+height},null);
            group.GetType().GetMethod("AddCustomSize").Invoke(group,new[]{size});
            int total=(int)group.GetType().GetMethod("GetTotalCount").Invoke(group,null);
            var game=EditorWindow.GetWindow(assembly.GetType("UnityEditor.GameView"));
            game.GetType().GetProperty("selectedSizeIndex",BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance).SetValue(game,total-1);
            game.Focus();
        }
    }
}
