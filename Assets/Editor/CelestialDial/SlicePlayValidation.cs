using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Ascendant.CelestialDial;

namespace Ascendant.Build
{
    // Scripted Play Mode pass through the whole v0.1 slice: identity, birth prompt, atrium, Dial, key reveal,
    // atrium return, chamber ending. Captures each screen. Not a human playtest.
    [InitializeOnLoad]
    public static class SlicePlayValidation
    {
        static readonly Queue<Action> Steps=new Queue<Action>();
        static readonly List<string> Report=new List<string>();
        static readonly List<string> RuntimeErrors=new List<string>();
        // Faster checks (Sept 25): the pause between steps was a fixed 4 s; every step already waits for !Busy, so the gap is a
        // setting (-sliceStep seconds on the command line), 1.5 s by default.
        static double StepGap { get { var args = System.Environment.GetCommandLineArgs(); int i = System.Array.IndexOf(args, "-sliceStep"); return i >= 0 && i + 1 < args.Length && double.TryParse(args[i + 1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var v) ? v : 1.5; } }
        static double nextAt; static string stashedSave=""; static int playedBefore;
        const string ReportPath="Logs/slice-play-validation.txt";
        static SliceView View => UnityEngine.Object.FindFirstObjectByType<SliceView>();
        static SlicePlayValidation()
        {
            EditorApplication.playModeStateChanged+=state=>{
                if(state==PlayModeStateChange.EnteredEditMode && Environment.GetCommandLineArgs().Contains("-sliceAutoExit")) EditorApplication.Exit(File.Exists(ReportPath) && File.ReadAllText(ReportPath).StartsWith("PASS") ? 0 : 1);
                if(state==PlayModeStateChange.EnteredPlayMode && SessionState.GetBool("AscendantSlicePlayValidation",false)) QueueChecks();
            };
        }
        [MenuItem("Ascendant/Greybox/Run vertical slice Play Mode validation")]
        public static void Begin()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/VerticalSlice.unity");
            PlayerPrefs.DeleteKey(SliceView.SaveKey);PlayerPrefs.Save(); // a stale local save from an earlier run would resume at the Hub
            SlotMenus.ClearForFixture();Slots.Request(null,null); // Build E: the run plays on the Art folder; the test set comes in at the end
            int scale=Environment.GetCommandLineArgs().Contains("-sliceScale2") ? 2 : 1; // phone pixel density: the same layout at twice the pixels
            GreyboxPlayValidation.SetSize(390*scale,844*scale);SessionState.SetBool("AscendantSlicePlayValidation",true);
            EditorApplication.EnterPlaymode();
        }
        static void Check(bool value,string text) { if(!value)throw new Exception(text);Report.Add("PASS: "+text); }
        static void Act(string command) => View.Dial.WebAction(command);
        static void AnswerReview(bool captureGlyph)
        {
            var task=View.Flow.CurrentReview;if(task==null||task.done)return;
            if(task.Mode==ReviewMode.Dial){Act("seat:"+Zodiac.Destination(task.seat));Act("seal");}
            else if(task.Mode==ReviewMode.DialModality){Act("seat:"+Zodiac.Destination(task.seat,3));Act("seal");}
            else if(task.Mode==ReviewMode.Tap){Act("element:"+System.Array.IndexOf(new[]{"Fire","Earth","Air","Water"},Zodiac.Seats[task.seat].Element));}
            else if(task.Mode==ReviewMode.TapModality){if(View.Flow.ReviewIndex==1)Capture("slice-390-practice-modality.png");Act("modality:"+(task.seat%3));}
            else{int slot=System.Array.IndexOf(View.Flow.GlyphReviewOptions(task.seat),task.seat);if(captureGlyph && View.Flow.ReviewIndex==0)Capture("slice-390-practice-glyph.png");Act("glyph-name:"+slot);}
        }
        static void QueueChecks()
        {
            Report.Clear();Steps.Clear();RuntimeErrors.Clear();Application.logMessageReceived+=CaptureLog;
            Steps.Enqueue(()=>{Check(View!=null && View.Flow.Screen==SliceScreen.Identity,"slice opens on the identity screen");Capture("slice-390-identity.png");});
            Steps.Enqueue(()=>{Act("name:Tester");Check(View.Flow.DisplayName=="Tester","name reaches the flow through the bridge");Act("next-screen");});
            Steps.Enqueue(()=>{Check(View.Flow.Screen==SliceScreen.Birth,"birth prompt follows identity");Capture("slice-390-birth.png");});
            Steps.Enqueue(()=>{Act("next-screen");Check(View.Flow.Screen==SliceScreen.Birth,"birth prompt blocks continue until a choice");Act("birth:unknown");Check(View.Flow.HasSunSign && View.Flow.Note.Contains("choose one for you"),"I don't know assigns a sun sign");Act("birth:");Check(!View.Flow.HasSunSign,"the answer can be changed");Act("birth:known");Act("sign:1");Check(View.Flow.SunSign==1 && View.Flow.Note.Contains("Taurus"),"a known sign is accepted");Act("next-screen");});
            Steps.Enqueue(()=>{Check(View.Flow.Screen==SliceScreen.Atrium && !View.Busy,"white light leads to the atrium");Capture("slice-390-atrium.png");});
            Steps.Enqueue(()=>{for(int i=0;i<SliceView.AtriumPages.Length;i++)Act("next-screen");});
            Steps.Enqueue(()=>{Check(View.Flow.Screen==SliceScreen.Wing && View.Dial.UiCanvas.gameObject.activeSelf && View.Dial.Lesson.DialDormant && View.Dial.Lesson.Sun==1,"atrium continues into the wing; the Dial is dormant and knows the sun sign");Capture("slice-390-wing.png");});
            for(int i=0;i<7;i++) Steps.Enqueue(()=>Act("continue"));
            Steps.Enqueue(()=>{Check(View.Dial.Lesson.Phase==LessonPhase.Guided && !View.Dial.Lesson.DialDormant,"seven intro beats reach the guided problem");Capture("slice-390-wing-guided.png");});
            Steps.Enqueue(()=>{Act("seat:5");Act("seal");});
            Steps.Enqueue(()=>{Act("seat:9");Act("seal");});
            Steps.Enqueue(()=>{Check(View.Dial.Lesson.Phase==LessonPhase.Transfer,"guided family completes inside the slice");Act("continue");for(int i=0;i<4;i++)Act("keyboard-forward");Act("seal");});
            Steps.Enqueue(()=>{Act("seat:8");Act("seal");});
            Steps.Enqueue(()=>{Check(View.Dial.Lesson.KeyEarned,"six-seat lesson earns Key 1 inside the slice");Check(View.Dial.Lesson.Message.StartsWith("Two of four") || View.Flow.KeyRevealed,"locked completion line precedes the reveal");});
            Steps.Enqueue(()=>{Check(View.Flow.KeyRevealed && View.Flow.Keys==1 && !View.Busy,"the Dial reveals the Key after completion");Check(View.Dial.Lesson.Message.StartsWith("Aah"),"Caspar's reveal line follows the locked line");Capture("slice-390-wing-reveal.png");});
            Steps.Enqueue(()=>{Act("next-screen");});
            Steps.Enqueue(()=>{Check(View.Flow.Screen==SliceScreen.AtriumReturn,"wing continues to the atrium return");Capture("slice-390-atrium-return.png");});
            Steps.Enqueue(()=>{for(int i=0;i<SliceView.ReturnPages.Length;i++)Act("next-screen");});
            Steps.Enqueue(()=>{Check(View.Flow.Screen==SliceScreen.Chamber,"atrium return leads to the chamber");Act("insert");Check(!View.Flow.KeyInserted,"insert waits for Caspar to finish");Check(View.ChamberPaging,"a visible Continue turns the pages and Insert is not yet shown");for(int i=1;i<SliceView.ChamberPages.Length;i++)Act("next-screen");Check(!View.ChamberPaging && View.Flow.Screen==SliceScreen.Chamber,"on the last page Continue gives way to Insert the Key");Capture("slice-390-chamber.png");});
            Steps.Enqueue(()=>{Act("insert");});
            Steps.Enqueue(()=>{Check(View.Flow.KeyInserted && View.Flow.LocksFilled==1 && View.Flow.Ended && !View.Busy,"one key fills one of three locks and ends the prototype");Capture("slice-390-chamber-end.png");});
            // ---- v0.2: the return ----
            Steps.Enqueue(()=>{Act("next-screen");Check(View.Flow.Screen==SliceScreen.Hub && View.Flow.AtriumStage==2 && View.Flow.DueCount>=6 && View.Flow.DueCount==12-View.Flow.Deck.Practicing && UnityEngine.PlayerPrefs.HasKey(SliceView.SaveKey),"the Chamber leads to the Hub in Stage 2 with a save written and twelve items ready for practice at once");Check(View.Flow.Walk.Room==Room.Atrium && View.Flow.Walk.At=="entry","the marker stands where you came in");Capture("slice-390-hub.png");Act("walk:sealed-left");Check(View.Flow.Note.StartsWith("Sealed") && !View.Busy,"a sealed door only says it is sealed");});
            // ---- Build F: the desk is dressing; the journal is in hand; the fork on the Dial; practice is the sitting ----
            Steps.Enqueue(()=>{Act("walk:desk");Check(View.Flow.Walk.TargetId=="desk","the desk is still a place to walk to");});
            Steps.Enqueue(()=>{Check(View.Flow.Screen==SliceScreen.Hub && View.Flow.Note==SliceFlow.DeskLine && !View.Busy && View.Flow.CanOpenJournal,"the desk only speaks (Check the Seals is gone); the journal is in hand at the Atrium");Act("open-journal");});
            // Build J: the journal is a book: contents, a page per sign met, the sections; no state word on screen
            Steps.Enqueue(()=>{var snap=View.Dial.Snapshot();Check(View.Flow.Screen==SliceScreen.Journal && snap.journal && snap.journalView=="contents" && snap.journalContents.SequenceEqual(new[]{"The Elements","The Signs"}) && snap.journalTabs.SequenceEqual(new[]{true,true}) && !snap.canJournalNext && !snap.canJournalPrev && !snap.canJournalContents,"the journal opens from the Atrium on its contents: the elements and the signs met, both flagged (every element is due)");Capture("slice-390-journal-contents.png");});
            Steps.Enqueue(()=>Act("journal-entry:1")); // captures land at the end of the frame: the action waits its own step
            Steps.Enqueue(()=>{var snap=View.Dial.Snapshot();Check(snap.journalView=="sign" && snap.journalSign=="Aries" && snap.journalFacts.SequenceEqual(new[]{"Element: Fire"}) && snap.journalGlyph=="" && !snap.journalTable && snap.journalRibbonOut && snap.canJournalNext && !snap.canJournalPrev && snap.canJournalContents,"The Signs opens on Aries with only its element (no symbol, no table cell yet), its ribbon pulled out: due");
                Check(snap.journalShader && !snap.journalText.Contains("introduced") && !snap.journalText.Contains("practicing") && snap.journalText.Contains("Fire"),"the Illumination shader loaded; no state word on the page, the fact is: "+snap.journalText);Capture("slice-390-journal-sign-early.png");});
            Steps.Enqueue(()=>Act("journal-contents"));
            Steps.Enqueue(()=>{Check(View.Dial.Snapshot().journalView=="contents","Contents returns to the contents page");Act("journal-entry:0");});
            Steps.Enqueue(()=>{var snap=View.Dial.Snapshot();Check(snap.journalView=="section" && snap.journalSection=="The Elements" && snap.journalEntries.Length==12 && snap.journalEntries[0]=="Aries — Fire" && !snap.journalText.Contains("introduced") && !snap.journalText.Contains("practicing"),"the elements section: twelve entries in wheel order, no state word on screen");Capture("slice-390-journal-elements.png");});
            Steps.Enqueue(()=>Act("close-journal"));
            Steps.Enqueue(()=>{Check(View.Flow.Screen==SliceScreen.Hub && !View.Busy && View.Flow.Walk.At=="desk","the journal closes back to the Atrium where the Keeper stood");Act("walk:wing-door");});
            Steps.Enqueue(()=>{Check(View.Flow.Screen==SliceScreen.WingRoom && !View.Busy && View.Flow.CanOpenJournal,"the Wing room offers the journal too");Act("walk:dial");});
            Steps.Enqueue(()=>{var snap=View.Dial.Snapshot();Check(View.Flow.Screen==SliceScreen.Wing && !View.Busy && snap.fork=="both" && snap.canEnterPractice && snap.canContinueLesson && !View.Dial.Lesson.Dial.Active && View.Dial.Lesson.Phase!=LessonPhase.Continuation,"on the Dial the fork offers the lesson and practice; nothing starts until the player chooses");Capture("slice-390-fork.png");});
            Steps.Enqueue(()=>Act("enter-practice"));
            Steps.Enqueue(()=>{Check(View.Flow.Screen==SliceScreen.Practice && View.Flow.Sittings==1 && View.Flow.ReviewQueue.Count==6 && View.Dial.Lesson.Phase==LessonPhase.Review && View.Dial.UiCanvas.gameObject.activeSelf && View.Dial.Lesson.ReviewHeader=="Practice · 1 of 6","practice opens at once as the first sitting with a compressed Dial item, headed as practice");Check(!View.Dial.Root.Find("Slice Continue").gameObject.activeSelf,"the Wing's Back button is not on the practice's Seal");Capture("slice-390-practice-dial.png");});
            Steps.Enqueue(()=>Act("reload"));
            Steps.Enqueue(()=>{Check(View!=null && View.Resumed && View.Flow.Screen==SliceScreen.Hub && View.Flow.Sittings==1 && View.Flow.DueCount>=6 && View.Flow.Deck.Items.Count(i=>i.entered)==12,"a reload during practice keeps the deck and the sitting: the same items are still ready");Act("enter-wing");});
            Steps.Enqueue(()=>{Check(View.Flow.Screen==SliceScreen.WingRoom && !View.Busy,"back in the Wing room");Act("walk:dial");});
            Steps.Enqueue(()=>{Check(View.Flow.Screen==SliceScreen.Wing && View.Dial.Snapshot().fork=="both","the fork again after the reload");Act("enter-practice");});
            Steps.Enqueue(()=>{Check(View.Flow.Screen==SliceScreen.Practice && View.Flow.ReviewQueue.Count==6 && View.Flow.Sittings==2,"practice opens again after the reload, a second sitting");});
            for(int i=0;i<6;i++) Steps.Enqueue(()=>{var task=View.Flow.CurrentReview;Check(task!=null && !task.done,"a practice item is waiting");if(task.Mode==ReviewMode.Dial){Act("seat:"+Zodiac.Destination(task.seat));Act("seal");}else{if(View.Flow.ReviewIndex==1)Capture("slice-390-practice-tap.png");Act("element:"+System.Array.IndexOf(new[]{"Fire","Earth","Air","Water"},Zodiac.Seats[task.seat].Element));}});
            Steps.Enqueue(()=>{Check(View.Flow.PracticeDone && View.Flow.PracticeSummary.StartsWith("6 of 6") && View.Flow.Deck.Practicing>=3,"six remembered; eligible answers advance their items");Capture("slice-390-practice-done.png");});
            Steps.Enqueue(()=>{Act("leave-practice");Check(View.Flow.Screen==SliceScreen.Wing && View.Flow.Sittings==2,"back to the Dial after practice; the sitting was counted on entry");});
            Steps.Enqueue(()=>{Check(View.Dial.Snapshot().fork=="both" && !View.Busy,"the fork returns after practice");Act("enter-practice");});
            Steps.Enqueue(()=>{Check(View.Flow.Screen==SliceScreen.Practice && View.Flow.Sittings==3 && View.Flow.ReviewQueue.Count==6,"the other six are due at the third sitting");});
            for(int i=0;i<6;i++) Steps.Enqueue(()=>AnswerReview(false));
            Steps.Enqueue(()=>{Check(View.Flow.PracticeDone,"the third sitting completes");Act("leave-practice");});
            // the sitting rule: nothing due still counts; then the three-strikes gate, the journal from the room, re-entry with fresh strikes
            Steps.Enqueue(()=>{Check(View.Flow.Screen==SliceScreen.Wing && View.Dial.Snapshot().fork=="both" && !View.Busy,"the fork again");Act("enter-practice");});
            Steps.Enqueue(()=>{Check(View.Flow.Screen==SliceScreen.Wing && View.Flow.Sittings==4 && View.Dial.Lesson.Message==SliceFlow.NothingDueLine && View.Dial.Snapshot().fork=="both","with every item scheduled ahead the entry still counts a sitting; Caspar says nothing is due; the fork stays");Capture("slice-390-nothing-due.png");});
            Steps.Enqueue(()=>Act("enter-practice"));
            Steps.Enqueue(()=>{Check(View.Flow.Screen==SliceScreen.Practice && View.Flow.Sittings==5 && View.Flow.ReviewQueue.Count==6 && View.Flow.Strikes==0,"one sitting on, six items are due again");});
            for(int i=0;i<3;i++) Steps.Enqueue(()=>{var task=View.Flow.CurrentReview;Check(task!=null && !task.done && !View.Flow.Gated,"an item waits for a wrong answer");if(task.Mode==ReviewMode.Dial){Act("seat:"+Zodiac.Wrap(Zodiac.Destination(task.seat)+1));Act("seal");}else{Act("element:"+((System.Array.IndexOf(new[]{"Fire","Earth","Air","Water"},Zodiac.Seats[task.seat].Element)+1)%4));}});
            Steps.Enqueue(()=>{Check(View.Flow.Screen==SliceScreen.WingRoom && View.Flow.Note=="gated" && View.Flow.Strikes==3 && !View.Busy && View.Dial.Lesson.Phase!=LessonPhase.Review && View.Flow.CanOpenJournal,"the third wrong answer closes the instrument: the Keeper is back in the room with the journal offered, not locked out");Capture("slice-390-gate.png");});
            Steps.Enqueue(()=>Act("open-journal"));
            Steps.Enqueue(()=>{Check(View.Flow.Screen==SliceScreen.Journal && View.Flow.JournalFrom==SliceScreen.WingRoom,"the journal opens from the Wing room after the gate");Capture("slice-390-journal-wing.png");});
            Steps.Enqueue(()=>Act("close-journal"));
            Steps.Enqueue(()=>{Check(View.Flow.Screen==SliceScreen.WingRoom && View.Flow.Note=="" && !View.Busy,"closing the journal returns to the room and clears the gate");Act("walk:dial");});
            Steps.Enqueue(()=>{Check(View.Flow.Screen==SliceScreen.Wing && View.Dial.Snapshot().fork=="both","the fork is back after the gate");Act("enter-practice");});
            Steps.Enqueue(()=>{Check(View.Flow.Screen==SliceScreen.Practice && View.Flow.Strikes==0 && View.Flow.Sittings==6,"re-entry is immediate with three fresh strikes and a new sitting");Act("leave-practice");});
            Steps.Enqueue(()=>{Check(View.Flow.Screen==SliceScreen.Wing && !View.Busy,"an exit at any point");Act("leave-wing");});
            Steps.Enqueue(()=>{Check(View.Flow.Screen==SliceScreen.Hub && !View.Busy,"one press from the Dial walks out through the room to the Atrium");});
            Steps.Enqueue(()=>{Act("walk:wing-door");Check(View.Flow.Walk.TargetId=="wing-door","tapping the Wing doorway walks the marker");});
            Steps.Enqueue(()=>{Check(View.Flow.Screen==SliceScreen.WingRoom && View.Flow.Walk.Room==Room.Wing && View.Flow.Walk.At=="atrium-door" && !View.Busy,"the doorway fades into the Wing room at its doorway");Capture("slice-390-wing-room.png");Act("walk:grid");Check(View.Flow.Note=="grid-dark" && !View.Flow.Walk.Walking && View.Flow.Screen==SliceScreen.WingRoom && !View.Flow.CanOpenGrid,"before the modality unit the table is dark: a note, no walk");Act("enter-dial");});
            Steps.Enqueue(()=>{Check(View.Flow.Screen==SliceScreen.Wing && View.Dial.Snapshot().fork=="both" && View.Dial.Lesson.Phase!=LessonPhase.Continuation,"the fork waits before the third family");Act("continue-lesson");});
            Steps.Enqueue(()=>{Check(View.Flow.Screen==SliceScreen.Wing && View.Flow.Walk.At=="dial" && View.Dial.Lesson.Phase==LessonPhase.Continuation && View.Dial.Lesson.Dial.Start==2 && View.Dial.Lesson.Dial.HintLevel==0,"the Wing continues with the third family on the player's own");Capture("slice-390-wing-unit11.png");});
            for(int i=0;i<4;i++) Steps.Enqueue(()=>{Act("seat:"+Zodiac.Destination(View.Dial.Lesson.Dial.Start));Act("seal");});
            Steps.Enqueue(()=>{Check(View.Dial.Lesson.Phase==LessonPhase.AllLit && View.Flow.WheelComplete && View.Dial.Lesson.Lit.All(v=>v) && View.Dial.Lesson.KeyEarned && !View.Dial.Lesson.Key2Earned && View.Flow.Keys==1,"twelve seats lit with no second Key");Capture("slice-390-wing-lit.png");Act("leave-wing");});
            Steps.Enqueue(()=>{Check(View.Flow.Screen==SliceScreen.Hub && View.Flow.AtriumStage==3 && View.Flow.V02Complete,"one more return completes v0.2");Capture("slice-390-hub-complete.png");});
            // ---- v0.3: glyphs and Key 2 ----
            Steps.Enqueue(()=>Act("enter-wing"));
            Steps.Enqueue(()=>{Check(View.Flow.Screen==SliceScreen.WingRoom && !View.Busy && View.Flow.CanOpenBook,"the Wing button walks through the same doorway; the lit wheel wakes the shelf");Capture("slice-390-wing-room-lit.png");Act("walk:dial");});
            Steps.Enqueue(()=>{Check(View.Flow.Screen==SliceScreen.Wing && View.Dial.Lesson.Phase!=LessonPhase.GlyphNames && View.Dial.Lesson.Message==DialLesson.ShelfFirst,"the Dial before the book only points at the shelf");Act("leave-wing");});
            Steps.Enqueue(()=>{Check(View.Flow.Screen==SliceScreen.Hub && !View.Busy,"back out to the Atrium");Act("enter-wing");});
            Steps.Enqueue(()=>{Check(View.Flow.Screen==SliceScreen.WingRoom && !View.Busy,"and in again");Act("walk:shelf");});
            Steps.Enqueue(()=>{Check(View.Flow.Screen==SliceScreen.Book && View.Dial.Lesson.Phase==LessonPhase.GlyphNames && View.Flow.GlyphsStarted && View.Flow.Deck.Items.Count(i=>i.Kind==ItemKind.Glyph && i.entered)==12,"the shelf opens the book: Part A, and introduces the symbol items");Capture("slice-390-glyphs-a.png");});
            for(int i=0;i<3;i++) Steps.Enqueue(()=>{var l=View.Dial.Lesson;Act("glyph-name:"+Array.IndexOf(l.GlyphOptions(l.CurrentGlyph),l.CurrentGlyph));});
            Steps.Enqueue(()=>{var saved=JsonUtility.FromJson<SaveData>(PlayerPrefs.GetString(SliceView.SaveKey));Check(saved.glyphStage==0 && saved.glyphIndex==3,"the live naming checkpoint stores the next card after three answers");Act("reload");});
            Steps.Enqueue(()=>{Check(View.Resumed && View.Dial.Lesson.GlyphNamed.Count(v=>v)==3,"reload retains the first three named symbols");Act("enter-wing");});
            Steps.Enqueue(()=>Act("walk:shelf"));
            Steps.Enqueue(()=>{Check(View.Dial.Lesson.CurrentGlyph==3,"mid-naming reload resumes on Cancer, not Aries");Capture("slice-390-symbol-naming-restored.png");});
            for(int i=0;i<14;i++) Steps.Enqueue(()=>{var l=View.Dial.Lesson;if(l.Phase!=LessonPhase.GlyphNames)return;int target=l.CurrentGlyph;int slot=System.Array.IndexOf(l.GlyphOptions(target),target);if(l.GlyphIndex==5)Act("glyph-name:"+((slot+1)%4));Act("glyph-name:"+slot);});
            Steps.Enqueue(()=>{Check(View.Flow.Screen==SliceScreen.WingRoom && View.Dial.Lesson.Phase==LessonPhase.GlyphWheel && !View.Busy,"the twelfth name closes the book and returns to the room");Act("walk:dial");});
            Steps.Enqueue(()=>{Check(View.Flow.Screen==SliceScreen.Wing && View.Dial.Lesson.Phase==LessonPhase.GlyphWheel && View.Dial.Lesson.NamesHidden && View.Dial.Lesson.Dial.Target==0,"the Dial runs Part B: names hidden, Aries first");Capture("slice-390-glyphs-b.png");});
            for(int i=0;i<3;i++) Steps.Enqueue(()=>{Act("seat:"+View.Dial.Lesson.Dial.Target);Act("seal");});
            Steps.Enqueue(()=>{var saved=JsonUtility.FromJson<SaveData>(PlayerPrefs.GetString(SliceView.SaveKey));Check(saved.glyphStage==1 && saved.glyphIndex==3,"the live placement checkpoint stores three placed symbols");Act("reload");});
            Steps.Enqueue(()=>{Check(View.Resumed && View.Dial.Lesson.GlyphPlaced.Count(v=>v)==3,"reload retains three placed symbols");Act("enter-wing");});
            Steps.Enqueue(()=>Act("walk:dial"));
            Steps.Enqueue(()=>{Check(View.Dial.Lesson.GlyphIndex==3 && View.Dial.Lesson.Dial.Target==3,"mid-placement reload resumes at Cancer with Aries, Taurus, and Gemini placed");Capture("slice-390-symbol-placement-restored.png");});
            bool missedOnce=false; // one deliberate miss on the fourth mark; a second would climb the ladder and skip the placement
            for(int i=0;i<12;i++) Steps.Enqueue(()=>{var l=View.Dial.Lesson;if(l.Phase!=LessonPhase.GlyphWheel)return;int target=l.Dial.Target;if(l.GlyphIndex==3 && !missedOnce){missedOnce=true;Act("seat:"+Zodiac.Wrap(target+2));Act("seal");return;}Act("seat:"+target);Act("seal");});
            Steps.Enqueue(()=>{var l=View.Dial.Lesson;if(l.Phase==LessonPhase.GlyphWheel){Act("seat:"+l.Dial.Target);Act("seal");}});
            Steps.Enqueue(()=>{Check(View.Dial.Lesson.Key2Earned && View.Flow.Keys==2 && View.Dial.Lesson.Dial.Events.Count(e=>e.event_name=="key2_earned")==1 && View.LastCeremony==2,"twelve marks placed earns Key 2 once, with its Key ceremony (Build I)");Capture("slice-390-key2.png");});
            Steps.Enqueue(()=>Act("leave-wing"));
            // ---- Build D: the finished loop. Keys earned are spent in the Chamber; the Atrium restores on the return. ----
            Steps.Enqueue(()=>{Check(View.Flow.Screen==SliceScreen.Hub && View.Flow.AtriumStage==3 && View.Flow.KeysInHand==1 && View.Flow.CanEnterChamber,"one more return: Key 2 in hand, the Atrium waits for it to be spent");Capture("slice-390-hub-key-in-hand.png");Act("walk:chamber-door");});
            Steps.Enqueue(()=>{Check(View.Flow.Screen==SliceScreen.ChamberRoom && View.Flow.Walk.Room==Room.Chamber && View.Flow.Walk.At=="atrium-door" && !View.Busy && View.Flow.CanSpend,"the Chamber doorway fades into the Chamber as a room, a Key in hand");Capture("slice-390-chamber-room.png");Act("walk:books");});
            Steps.Enqueue(()=>{Check(View.Flow.Walk.At=="books" && View.Flow.CanSpend && !View.Busy,"at the Books a Key can be spent");Act("insert");});
            Steps.Enqueue(()=>{Check(View.Flow.LocksFilled==2 && View.Flow.KeysInHand==0 && View.Flow.BooksOpen==0 && !View.Flow.CanSpend,"Key 2 fills the second lock; the Book stays shut; nothing more to spend");Capture("slice-390-chamber-lock2.png");Act("leave-chamber");});
            Steps.Enqueue(()=>{Check(View.Flow.Screen==SliceScreen.Hub && View.Flow.AtriumStage==4 && View.Flow.V03Complete && View.Flow.Walk.At=="chamber-door" && !View.Busy,"the return after spending takes the Atrium to Stage 4");Capture("slice-390-hub-v03.png");Act("enter-wing");});
            Steps.Enqueue(()=>{Check(View.Flow.Screen==SliceScreen.WingRoom && !View.Busy,"into the Wing room for practice");Act("walk:dial");});
            Steps.Enqueue(()=>{Check(View.Flow.Screen==SliceScreen.Wing && View.Dial.Snapshot().fork=="both","the fork with the symbols entered");Act("enter-practice");});
            Steps.Enqueue(()=>{Check(View.Flow.Screen==SliceScreen.Practice && View.Flow.ReviewQueue.Count==6,"a practice with the deck open begins");});
            for(int i=0;i<6;i++) Steps.Enqueue(()=>AnswerReview(true));
            Steps.Enqueue(()=>{Check(View.Flow.PracticeDone,"a practice with the deck open completes");Act("leave-practice");});
            Steps.Enqueue(()=>{Check(View.Flow.Screen==SliceScreen.Wing && !View.Busy,"back on the Dial");Act("leave-wing");});
            // ---- v0.3 revision, build 3: the book tests again after Key 2; a clean replay hardens the next one ----
            Steps.Enqueue(()=>{Check(View.Flow.Screen==SliceScreen.Hub && !View.Busy,"back at the Hub with two Keys");Act("enter-wing");});
            Steps.Enqueue(()=>{Check(View.Flow.Screen==SliceScreen.WingRoom && !View.Busy && View.Dial.Lesson.CanPractice,"after Key 2 the shelf offers practice");Act("walk:shelf");});
            Steps.Enqueue(()=>{Check(View.Flow.Screen==SliceScreen.Book && View.Dial.Lesson.Practice && View.Dial.Lesson.Phase==LessonPhase.GlyphNames && !View.Dial.Lesson.Hard && View.Dial.Lesson.SeatAt(0)==0,"the first replay runs in order with the usual names");});
            for(int i=0;i<12;i++) Steps.Enqueue(()=>{var l=View.Dial.Lesson;if(l.Phase!=LessonPhase.GlyphNames)return;int target=l.CurrentGlyph;Act("glyph-name:"+System.Array.IndexOf(l.GlyphOptions(target),target));});
            Steps.Enqueue(()=>{Check(View.Flow.Screen==SliceScreen.WingRoom && View.Dial.Lesson.Phase==LessonPhase.GlyphWheel && !View.Busy,"the replayed book closes and hands to the wheel");Act("walk:dial");});
            for(int i=0;i<12;i++) Steps.Enqueue(()=>{var l=View.Dial.Lesson;if(l.Phase!=LessonPhase.GlyphWheel)return;Act("seat:"+l.Dial.Target);Act("seal");});
            Steps.Enqueue(()=>{var l=View.Dial.Lesson;if(l.Phase==LessonPhase.GlyphWheel){Act("seat:"+l.Dial.Target);Act("seal");}});
            Steps.Enqueue(()=>{Check(!View.Dial.Lesson.Practice && View.Dial.Lesson.CleanRuns==1 && View.Flow.CleanRuns==1 && View.Dial.Lesson.Hard && View.Flow.Keys==2 && View.Dial.Lesson.Dial.Events.Count(e=>e.event_name=="key2_earned")==1,"a clean replay is recorded once; Key 2 is not re-earned; the next replay is hard");Capture("slice-390-practice-clean.png");});
            Steps.Enqueue(()=>Act("leave-wing"));
            Steps.Enqueue(()=>{Check(View.Flow.Screen==SliceScreen.Hub && !View.Busy,"back at the Hub");Act("enter-wing");});
            Steps.Enqueue(()=>{Check(View.Flow.Screen==SliceScreen.WingRoom && !View.Busy,"in the Wing room again");Act("walk:shelf");});
            Steps.Enqueue(()=>{var l=View.Dial.Lesson;Check(View.Flow.Screen==SliceScreen.Book && l.Practice && l.Hard && l.SeatAt(0)!=0 && l.GlyphOptions(l.CurrentGlyph).Length==4 && l.GlyphOptions(l.CurrentGlyph).Distinct().Count()==4 && l.GlyphOptions(l.CurrentGlyph).Contains(l.CurrentGlyph),"the hard replay shuffles the order and keeps four distinct names including the answer");Capture("slice-390-practice-hard.png");Act("close-book");});
            Steps.Enqueue(()=>{Check(View.Flow.Screen==SliceScreen.WingRoom && !View.Busy,"the book can be closed mid-practice");Act("walk:dial");});
            Steps.Enqueue(()=>{Check(View.Flow.Screen==SliceScreen.Wing && View.Dial.Snapshot().fork=="both","the fork before the second pattern");Act("continue-lesson");});
            // ---- Build A: the modalities on the Dial ----
            Steps.Enqueue(()=>{var l=View.Dial.Lesson;Check(View.Flow.Screen==SliceScreen.Wing && l.Phase==LessonPhase.ModalityGuided && l.Dial.Forward==3 && l.Dial.Start==l.Sun && l.LitMod[l.Sun] && View.Flow.ModalitiesStarted && View.Flow.Deck.Items.Count(i=>i.Kind==ItemKind.Modality && i.entered)==12,"after Key 2 the Dial opens the modality unit at the sun sign, three forward, and introduces twelve modality items");Capture("slice-390-modalities.png");});
            for(int i=0;i<9;i++) Steps.Enqueue(()=>{var l=View.Dial.Lesson;if(!(l.Phase==LessonPhase.ModalityGuided||l.Phase==LessonPhase.ModalityOwn)||!l.Dial.Active)return;if(l.LitMod.Count(v=>v)==6 && l.Phase==LessonPhase.ModalityOwn && l.Dial.Attempts==0){Act("seat:"+Zodiac.Wrap(l.Dial.Start+2));Act("seal");return;}Act("seat:"+Zodiac.Destination(l.Dial.Start,3));Act("seal");});
            Steps.Enqueue(()=>{var l=View.Dial.Lesson;if((l.Phase==LessonPhase.ModalityGuided||l.Phase==LessonPhase.ModalityOwn) && l.Dial.Active){Act("seat:"+Zodiac.Destination(l.Dial.Start,3));Act("seal");}});
            Steps.Enqueue(()=>{var l=View.Dial.Lesson;Check(l.Phase==LessonPhase.ModalityComplete && l.ModalitiesComplete && View.Flow.Keys==2 && l.Dial.Events.Count(e=>e.event_name=="modality_family_completed")==3,"three modality families of four complete with no new Key");Capture("slice-390-modalities-complete.png");});
            Steps.Enqueue(()=>Act("leave-wing"));
            Steps.Enqueue(()=>{Check(View.Flow.Screen==SliceScreen.Hub && !View.Busy && View.Flow.Deck.Due(View.Flow.Sitting).Any(i=>i.Kind==ItemKind.Modality),"back at the Hub with modality items ready");});
            bool sawModality=false; // older items come first (three kinds, thirty-six items), so the modality items may wait a few sittings
            for(int round=0;round<6;round++)
            {
                Steps.Enqueue(()=>{if(sawModality||View.Flow.Screen!=SliceScreen.Hub)return;Act("enter-wing");});
                Steps.Enqueue(()=>{if(sawModality||View.Flow.Screen!=SliceScreen.WingRoom)return;Act("walk:dial");});
                Steps.Enqueue(()=>{if(sawModality||View.Flow.Screen!=SliceScreen.Wing)return;Act("enter-practice");});
                for(int i=0;i<6;i++) Steps.Enqueue(()=>{if(View.Flow.Screen==SliceScreen.Practice)AnswerReview(false);});
                Steps.Enqueue(()=>{if(View.Flow.Screen!=SliceScreen.Practice)return;if(View.Flow.ReviewQueue.Any(t=>t.Mode==ReviewMode.TapModality||t.Mode==ReviewMode.DialModality))sawModality=true;Act("leave-practice");});
                Steps.Enqueue(()=>{if(View.Flow.Screen!=SliceScreen.Wing)return;Act("leave-wing");});
                Steps.Enqueue(()=>{if(View.Flow.Screen!=SliceScreen.WingRoom)return;Act("leave-wing");});
            }
            Steps.Enqueue(()=>{Check(sawModality && View.Flow.Screen==SliceScreen.Hub,"a practice carries modality items within six sittings");});
            Steps.Enqueue(()=>{Check(View.Flow.Screen==SliceScreen.Hub && !View.Busy,"back at the Hub");Act("reload");});
            Steps.Enqueue(()=>{Check(View!=null && View.Resumed && View.Dial.Lesson.ModalitiesComplete && View.Flow.ModalitiesStarted && View.Flow.ModalitiesComplete,"a reload keeps the modality unit complete");Act("enter-wing");});
            // ---- Build B: the table and Key 3 ----
            Steps.Enqueue(()=>{Check(View.Flow.Screen==SliceScreen.WingRoom && !View.Busy && View.Flow.CanOpenGrid && !View.Flow.GridStarted,"the finished modality unit wakes the table in the Wing room");Capture("slice-390-wing-room-table.png");Act("enter-grid");});
            Steps.Enqueue(()=>{var g=View.Grid;Check(View.Flow.Screen==SliceScreen.Grid && View.Flow.Walk.At=="grid" && g.Active && g.Message==GridModel.IntroLine && View.Flow.GridStarted && View.Flow.Deck.Items.Count(i=>i.Kind==ItemKind.Grid && i.entered)==12 && View.Flow.Deck.Due(View.Flow.Sitting).Count(i=>i.Kind==ItemKind.Grid)==12 && View.Flow.Deck.DueForReview(View.Flow.Sitting).All(i=>i.Kind!=ItemKind.Grid),"the room button walks to the table and opens it; twelve sign → cell items enter the deck as data");Capture("slice-390-grid.png");});
            Steps.Enqueue(()=>{Act("grid-sign:0");Act("grid-cell:"+GridModel.CellOf(0));Act("grid-seal");});
            Steps.Enqueue(()=>{var g=View.Grid;Check(g.Placed[0] && g.Evidence && g.Events.Last(e=>e.event_name=="grid_placed").evidence_eligible,"Aries seats at Level 0 with evidence");Act("grid-sign:1");Act("grid-cell:"+GridModel.CellOf(8));Act("grid-seal");Check(g.HintLevel==1 && g.Locked && g.Message=="Not that square, acolyte. Taurus is an Earth sign. Find the Earth row." && g.CanAsk,"a wrong row nudges with the element and offers Ask Caspar");Capture("slice-390-grid-miss.png");}); // captures land at the end of the frame: the seating waits for the next step
            Steps.Enqueue(()=>{Act("grid-cell:"+GridModel.CellOf(1));Act("grid-seal");});
            Steps.Enqueue(()=>{var g=View.Grid;Check(g.Placed[1] && g.Events.Last(e=>e.event_name=="grid_placed").evidence_eligible,"the right cell after one nudge seats Taurus as Level 1 evidence");Act("grid-sign:2");Act("grid-cell:"+GridModel.CellOf(6));Act("grid-seal");Check(g.HintLevel==1 && g.Message=="Not that square, acolyte. Gemini is mutable. Find the mutable column.","a wrong column nudges with the kind");Act("grid-ask");Check(g.Asked && g.HintLevel==2 && g.Message.StartsWith("Gemini is Air and it is mutable, acolyte.") && !g.CanAsk,"Ask Caspar states the rule once");Capture("slice-390-grid-ask.png");});
            Steps.Enqueue(()=>{Act("grid-cell:"+GridModel.CellOf(2));Act("grid-seal");});
            Steps.Enqueue(()=>{var g=View.Grid;Check(g.Placed[2] && !g.Events.Last(e=>e.event_name=="grid_placed").evidence_eligible,"a seating after asking earns no evidence");Act("grid-sign:3");Act("grid-cell:"+GridModel.CellOf(3));Act("grid-seal");});
            Steps.Enqueue(()=>{Act("grid-sign:4");Act("grid-cell:"+GridModel.CellOf(4));Act("grid-seal");});
            Steps.Enqueue(()=>{var g=View.Grid;Check(g.PlacedCount==5 && View.Flow.Deck.Items.Count(i=>i.Kind==ItemKind.Grid && i.State==ItemState.Practicing)==4,"five seated; four eligible seatings made their items Practicing");Act("reload");});
            Steps.Enqueue(()=>{Check(View!=null && View.Resumed && View.Grid.PlacedCount==5 && View.Grid.Evidence && View.Flow.GridStarted && View.Flow.ModalitiesComplete && !View.Grid.Active && View.Flow.Deck.Items.Count(i=>i.Kind==ItemKind.Grid && i.entered)==12 && View.Flow.Deck.Items.Count(i=>i.Kind==ItemKind.Grid && i.State==ItemState.Practicing)==4,"a reload mid-table keeps the five seated signs, the evidence, and the grid items as data");Act("enter-wing");});
            Steps.Enqueue(()=>{Check(View.Flow.Screen==SliceScreen.WingRoom && !View.Busy && View.Flow.CanOpenGrid,"in the Wing room after the reload, the table still open");Act("walk:grid");});
            Steps.Enqueue(()=>{var g=View.Grid;Check(View.Flow.Screen==SliceScreen.Grid && g.Active && g.PlacedCount==5 && g.Message.Contains("5 of twelve"),"tapping the table resumes it with its seated signs");Act("grid-sign:5");Act("grid-cell:"+GridModel.CellOf(6));Act("grid-seal");Act("grid-cell:"+GridModel.CellOf(7));Act("grid-seal");Act("grid-cell:"+GridModel.CellOf(8));Act("grid-seal");Check(g.HintLevel==3 && g.Demonstrating,"three wrong cells hand the sign to Caspar");});
            Steps.Enqueue(()=>{var g=View.Grid;Check(g.Placed[5] && g.Assisted[5] && g.AssistedThisSitting==1 && g.Active && g.Message.Contains("Virgo is placed"),"Caspar seats it without evidence and the table stays open");Capture("slice-390-grid-demo.png");});
            for(int i=0;i<6;i++) Steps.Enqueue(()=>{var g=View.Grid;if(!g.Active)return;int seat=Enumerable.Range(0,12).First(k=>!g.Placed[k]);Act("grid-sign:"+seat);Act("grid-cell:"+GridModel.CellOf(seat));Act("grid-seal");});
            Steps.Enqueue(()=>{var g=View.Grid;Check(g.Complete && g.Key3Earned && g.Phase==GridPhase.Complete && View.Flow.Keys==3 && g.Events.Count(e=>e.event_name=="key3_earned")==1 && !g.Active && View.LastCeremony==3,"twelve seated earns Key 3 once, with its Key ceremony on the table (Build I)");Capture("slice-390-grid-key3.png");});
            Steps.Enqueue(()=>Act("leave-grid"));
            Steps.Enqueue(()=>{Check(View.Flow.Screen==SliceScreen.WingRoom && !View.Busy,"the table closes back to the room");Act("walk:atrium-door");});
            Steps.Enqueue(()=>{Check(View.Flow.Screen==SliceScreen.Hub && !View.Busy && View.Flow.AtriumStage==4 && View.Flow.Keys==3 && View.Flow.KeysInHand==1,"back in the Atrium with Key 3 in hand");Act("walk:chamber-door");});
            Steps.Enqueue(()=>{Check(View.Flow.Screen==SliceScreen.ChamberRoom && !View.Busy,"the Chamber again");Act("walk:books");});
            Steps.Enqueue(()=>{Check(View.Flow.CanSpend && !View.Busy,"a Key to spend");Act("insert");});
            Steps.Enqueue(()=>{Check(View.Flow.LocksFilled==3 && View.Flow.BooksOpen==1 && !View.Flow.WingWhole && View.Flow.KeysInHand==0,"Key 3 fills the third lock and Book 1 opens");Capture("slice-390-book-opens.png");Act("leave-chamber");});
            Steps.Enqueue(()=>{Check(View.Flow.Screen==SliceScreen.Hub && !View.Busy && View.Flow.AtriumStage==5 && View.Flow.Keys==3,"the return takes the Atrium to Stage 5 with three Keys spent");Capture("slice-390-hub-key3.png");});
            Steps.Enqueue(()=>Act("reload"));
            Steps.Enqueue(()=>{Check(View!=null && View.Resumed && View.Flow.Screen==SliceScreen.Hub && View.Flow.WheelComplete && View.Flow.AtriumStage==5 && View.Flow.Keys==3 && View.Flow.LocksFilled==3 && View.Flow.BooksOpen==1 && View.Dial.Lesson.Key2Earned && View.Grid.Key3Earned && View.Grid.Complete && View.Flow.CleanRuns==1 && View.Dial.Lesson.Hard && View.Flow.Walk.At=="entry","a reload resumes at the Hub from the local save with three Keys spent, Book 1 open, and the full table");Act("enter-wing");});
            // ---- Build C: polarity, the six opposite pairs, the builder, and Key 4 ----
            Steps.Enqueue(()=>{Check(View.Flow.Screen==SliceScreen.WingRoom && !View.Busy && !View.Flow.OppositesStarted,"in the Wing room with three Keys, the last pattern waiting");Act("walk:dial");});
            Steps.Enqueue(()=>{Check(View.Flow.Screen==SliceScreen.Wing && View.Dial.Snapshot().fork=="both","the fork before the last pattern");Act("continue-lesson");});
            Steps.Enqueue(()=>{var l=View.Dial.Lesson;Check(View.Flow.Screen==SliceScreen.Wing && l.Phase==LessonPhase.Polarity && !l.PolarityShown && l.Key3Held && View.Flow.OppositesStarted && View.Flow.Deck.Items.Count(i=>i.Kind==ItemKind.Opposite && i.entered)==6 && View.Flow.Deck.DueForReview(View.Flow.Sitting).All(i=>i.Kind!=ItemKind.Opposite),"after Key 3 the Dial opens the polarity beat and introduces six pair items as data");Capture("slice-390-polarity.png");});
            Steps.Enqueue(()=>Act("continue"));
            Steps.Enqueue(()=>{var l=View.Dial.Lesson;Check(l.Phase==LessonPhase.Polarity && l.PolarityShown && l.SeatLabel(0).Contains(", Yang") && l.SeatLabel(1).Contains(", Yin"),"the second line shows every seat's side");Capture("slice-390-polarity-shown.png");});
            Steps.Enqueue(()=>Act("continue"));
            Steps.Enqueue(()=>{var l=View.Dial.Lesson;Check(l.Phase==LessonPhase.OppositeGuided && l.Dial.Forward==6 && l.Dial.Start==l.Sun && l.Dial.HintLevel==2 && !l.CountBeatPending,"the first pair is guided from the sun sign; the six-count has played");Capture("slice-390-opposites.png");});
            Steps.Enqueue(()=>{var l=View.Dial.Lesson;Act("seat:"+Zodiac.Opposite(l.Dial.Start));Act("seal");});
            Steps.Enqueue(()=>{var l=View.Dial.Lesson;Check(l.PairsKnown==1 && l.Phase==LessonPhase.OppositeOwn && l.Dial.HintLevel==0,"the guided pair is known; the next is on the player's own");Act("seat:"+Zodiac.Wrap(l.Dial.Start+3));Act("seal");Check(l.Dial.HintLevel==1 && l.CanAsk,"a wrong turn nudges and offers Ask Caspar");Act("ask-caspar");Check(l.Dial.HintLevel==2 && l.Message.Contains("six signs forward"),"Ask Caspar gives the six-seat rule");});
            Steps.Enqueue(()=>{var l=View.Dial.Lesson;Act("seat:"+Zodiac.Opposite(l.Dial.Start));Act("seal");});
            for(int i=0;i<4;i++) Steps.Enqueue(()=>{var l=View.Dial.Lesson;if(!l.InOppositeProblem||!l.Dial.Active)return;Act("seat:"+Zodiac.Opposite(l.Dial.Start));Act("seal");});
            Steps.Enqueue(()=>{var l=View.Dial.Lesson;Check(l.OppositesComplete && l.Phase==LessonPhase.OppositesComplete && l.PairsKnown==6 && l.Dial.Events.Count(e=>e.event_name=="opposites_completed")==1,"six pairs complete the last pattern");Capture("slice-390-opposites-complete.png");});
            Steps.Enqueue(()=>Act("continue"));
            Steps.Enqueue(()=>{var l=View.Dial.Lesson;Check(l.Phase==LessonPhase.BuilderName && l.BuilderStep==1 && l.Built==0 && l.BuilderTarget==l.Sun,"Continue opens the builder on the sun sign's parts");Capture("slice-390-builder-name.png");});
            Steps.Enqueue(()=>{var l=View.Dial.Lesson;Act("builder-name:"+System.Array.IndexOf(l.BuilderOptions,l.BuilderTarget));});
            Steps.Enqueue(()=>{var l=View.Dial.Lesson;Check(l.Phase==LessonPhase.BuilderOpposite && l.Dial.Active && l.Dial.Forward==6,"the right name hands to the wheel");Act("seat:"+Zodiac.Opposite(l.BuilderTarget));Act("seal");});
            Steps.Enqueue(()=>{var l=View.Dial.Lesson;Check(l.Phase==LessonPhase.BuilderShare && l.BuilderStep==3,"the opposite found, the share step asks what the two share");Capture("slice-390-builder-share.png");});
            Steps.Enqueue(()=>{var l=View.Dial.Lesson;Act("builder-share:2");Check(l.Message.Contains("Not the element"),"tapping the element is nudged once");Act("builder-share:0");Act("builder-share:1");});
            Steps.Enqueue(()=>{var l=View.Dial.Lesson;Check(l.Built==1 && l.BuilderEvidence && l.Phase==LessonPhase.BuilderName && l.BuilderTarget==Zodiac.Wrap(l.Sun+5),"the first sign is built unassisted; the second begins");int right=System.Array.IndexOf(l.BuilderOptions,l.BuilderTarget);Act("builder-name:"+((right+1)%4));Check(l.Phase==LessonPhase.BuilderName && l.Message.StartsWith("Not that one"),"a wrong name is nudged");Act("builder-name:"+right);});
            Steps.Enqueue(()=>{var l=View.Dial.Lesson;Check(l.Phase==LessonPhase.BuilderOpposite,"the second sign reaches the wheel");Act("seat:"+Zodiac.Wrap(l.BuilderTarget+3));Act("seal");Check(l.Dial.HintLevel==1 && l.CanAsk,"a wrong turn in the builder nudges");Act("ask-caspar");Check(l.Dial.HintLevel==2,"and Ask Caspar gives the rule");});
            Steps.Enqueue(()=>{var l=View.Dial.Lesson;Act("seat:"+Zodiac.Opposite(l.BuilderTarget));Act("seal");});
            Steps.Enqueue(()=>{var l=View.Dial.Lesson;Check(l.Phase==LessonPhase.BuilderShare,"the share step again");Act("builder-share:0");Act("builder-share:1");});
            Steps.Enqueue(()=>{var l=View.Dial.Lesson;Check(l.Built==2 && l.Phase==LessonPhase.BuilderName,"the second sign is built with help; the third begins");int right=System.Array.IndexOf(l.BuilderOptions,l.BuilderTarget);Act("builder-name:"+((right+1)%4));Act("builder-name:"+((right+2)%4));});
            Steps.Enqueue(()=>{var l=View.Dial.Lesson;Check(l.Phase==LessonPhase.BuilderOpposite && l.Message.StartsWith("It is "),"two wrong names reveal the sign");Act("seat:"+Zodiac.Opposite(l.BuilderTarget));Act("seal");});
            Steps.Enqueue(()=>{var l=View.Dial.Lesson;Check(l.Phase==LessonPhase.BuilderShare,"the last share step");Act("builder-share:1");Act("builder-share:0");});
            Steps.Enqueue(()=>{var l=View.Dial.Lesson;Check(l.Key4Earned && l.Phase==LessonPhase.Key4 && View.Flow.Keys==4 && l.Dial.Events.Count(e=>e.event_name=="key4_earned")==1 && View.LastCeremony==4,"three signs built with one unassisted earns Key 4 once, with its Key ceremony (Build I)");Capture("slice-390-key4.png");});
            Steps.Enqueue(()=>Act("leave-wing"));
            Steps.Enqueue(()=>{Check(View.Flow.Screen==SliceScreen.Hub && !View.Busy && View.Flow.AtriumStage==5 && View.Flow.Keys==4 && View.Flow.KeysInHand==1,"back in the Atrium with Key 4 in hand");Act("walk:chamber-door");});
            Steps.Enqueue(()=>{Check(View.Flow.Screen==SliceScreen.ChamberRoom && !View.Busy,"the Chamber, one last time");Act("walk:books");});
            Steps.Enqueue(()=>{Check(View.Flow.CanSpend && !View.Busy,"the last Key to spend");Act("insert");});
            Steps.Enqueue(()=>{Check(View.Flow.LocksFilled==4 && View.Flow.BooksOpen==1 && View.Flow.WingWhole && !View.Flow.CanSpend && View.Flow.KeysInHand==0,"Key 4 fills Book 2's first lock: the Wing is whole");Capture("slice-390-wing-whole.png");Act("leave-chamber");});
            Steps.Enqueue(()=>{Check(View.Flow.Screen==SliceScreen.Hub && !View.Busy && View.Flow.AtriumStage==6 && View.Flow.Keys==4,"the return takes the Atrium to Stage 6 with four Keys spent; the Wing's end card shows");Capture("slice-390-hub-key4.png");});
            // Build J: the late sign page on the Art folder (the files in Resources/Art, or the greybox without them), every fact learned;
            // Aries staged to full colour, then back to line art, for the owner's look at Illumination on the real picture
            Steps.Enqueue(()=>Act("open-journal"));
            Steps.Enqueue(()=>Act("journal-entry:5"));
            Steps.Enqueue(()=>{foreach(var it in View.Flow.SignItems(0)){it.state=(int)ItemState.Practicing;it.streak=3;it.dueDay=999;}Act("journal-next");Act("journal-prev");});
            Steps.Enqueue(()=>{var snap=View.Dial.Snapshot();Check(snap.journalSign=="Aries" && snap.journalColour==1 && snap.journalGilt && snap.journalFacts.Length==4 && snap.journalGlyph!="" && snap.journalTable,"at the Wing's end Aries' page holds every fact; staged, it is in full colour with the gilt edge");Capture("slice-390-journal-sign-late.png");});
            Steps.Enqueue(()=>{foreach(var it in View.Flow.SignItems(0)){it.state=(int)ItemState.Introduced;it.streak=0;}Act("journal-next");Act("journal-prev");});
            Steps.Enqueue(()=>{Check(View.Dial.Snapshot().journalInk==SliceFlow.IntroducedInk && View.Dial.Snapshot().journalColour==0,"staged back to introduced: line art");Capture("slice-390-journal-sign-lineart.png");});
            Steps.Enqueue(()=>Act("journal-contents"));
            Steps.Enqueue(()=>{Check(View.Dial.Snapshot().journalView=="contents","the contents at the Wing's end");Capture("slice-390-journal-contents-late.png");});
            Steps.Enqueue(()=>Act("close-journal"));
            Steps.Enqueue(()=>Act("reload"));
            Steps.Enqueue(()=>{Check(View!=null && View.Resumed && View.Flow.Screen==SliceScreen.Hub && View.Flow.AtriumStage==6 && View.Flow.Keys==4 && View.Flow.LocksFilled==4 && View.Flow.WingWhole && View.Dial.Lesson.Key4Earned && View.Dial.Lesson.PolarityShown && View.Dial.Lesson.OppositesComplete && View.Flow.Deck.Items.Count(i=>i.Kind==ItemKind.Opposite && i.entered)==6 && View.Grid.Key3Earned && View.Flow.Walk.At=="entry","a reload resumes at the Hub from the local save with four Keys spent, the Books, the sides, the six pairs, and the pair items as data");stashedSave=PlayerPrefs.GetString(SliceView.SaveKey);Act("restart");});
            Steps.Enqueue(()=>{Check(View!=null && !View.Resumed && View.Flow.Screen==SliceScreen.Identity && !UnityEngine.PlayerPrefs.HasKey(SliceView.SaveKey),"Start over wipes the save and begins again");GreyboxPlayValidation.SetSize(360,800);});
            Steps.Enqueue(()=>{Check(UnityEngine.Object.FindFirstObjectByType<Canvas>().pixelRect.size==new Vector2(360,800),"small portrait viewport");Capture("slice-360-identity.png");});
            // Build E: the same save with the test set in every slot, at both viewports; the cues; then the style page on both sets.
            Steps.Enqueue(()=>{Check(Slots.Set=="" && !View.StyleShown && Sound.LastCue!="","the run so far played on the Art folder and the sound hooks fired, files or not (last cue: "+Sound.LastCue+")");PlayerPrefs.SetString(SliceView.SaveKey,stashedSave);PlayerPrefs.Save();Slots.Request(Slots.TestSet,false);Act("reload");});
            Steps.Enqueue(()=>{Check(View!=null && View.Resumed && View.Flow.Screen==SliceScreen.Hub && Slots.Set==Slots.TestSet && Slots.ArtFiles==124 && Slots.SoundFiles==7,"?art=test: the save resumes at the Hub with the test set, every slot with a file");
                Check(new[]{"atrium","atrium-light","atrium-grime","caspar","akit-desk-restored","akit-shelf-restored","akit-door-open","akit-door-locked","akit-plate-clean","keeper-idle"}.All(Slots.IsDressed) && View.LightAlpha==SliceView.LightAlphaFor(View.Flow.AtriumStage) && View.LightAlpha>0,"the Atrium takes its files: the shell, its light and grime, Caspar, the kit (desk, shelf, doors locked and unlocked, the name plates), the Keeper (Build N); missing: "+string.Join(",",new[]{"atrium","atrium-light","atrium-grime","caspar","akit-desk-restored","akit-shelf-restored","akit-door-open","akit-door-locked","akit-plate-clean","keeper-idle"}.Where(x=>!Slots.IsDressed(x))));Capture("slice-360-art-hub.png");Debug.Log("[SlicePlayValidation] ambient loop playing: "+Sound.AmbientPlaying);});
            Steps.Enqueue(()=>{playedBefore=Sound.Played;Act("walk:wing-door");});
            Steps.Enqueue(()=>{Check(View.Flow.Screen==SliceScreen.WingRoom && !View.Busy && Sound.LastCue=="door" && Sound.Played>playedBefore,"the doorway plays the door cue from the test set (cues played: "+Sound.Played+")");
                Check(new[]{"wing","wing-light"}.All(Slots.IsDressed) && !Slots.IsDressed("table") && (Slots.IsDressed("keeper-idle") || Slots.IsDressed("keeper-walk")),"the Wing room takes its files: the composed room (its Dial, table, chair, and shelf painted in, so no separate table file is drawn), its light, the Keeper (Build L; the shelf and the Dial's face are checked on the Dial screen next)");Capture("slice-360-art-wing-room.png");Act("walk:dial");});
            Steps.Enqueue(()=>{Check(View.Flow.Screen==SliceScreen.Wing && new[]{"dial-face","seat","bracket","floor-markings","shelf","chair","candle"}.All(Slots.IsDressed),"the Dial takes its files: face, seats, bracket, floor markings, and the props beside it");Capture("slice-360-art-dial.png");});
            Steps.Enqueue(()=>Act("leave-wing")); // captures land at the end of the frame: the next action waits its own step
            Steps.Enqueue(()=>Act("walk:atrium-door"));
            Steps.Enqueue(()=>{Check(View.Flow.Screen==SliceScreen.Hub && !View.Busy,"back in the Atrium with the test set");Act("walk:chamber-door");});
            Steps.Enqueue(()=>{Check(View.Flow.Screen==SliceScreen.ChamberRoom && new[]{"chamber","chamber-light","chamber-grime","lock","ckit-mechanism-restored","ckit-candle-restored","ckit-banner-restored","ckit-crystal-worn"}.All(Slots.IsDressed),"the Chamber takes its files: the shell, its light and grime, the locks, the kit (mechanism, candles, banners, crystals; Build O); missing: "+string.Join(",",new[]{"chamber","chamber-light","chamber-grime","lock","ckit-mechanism-restored","ckit-candle-restored","ckit-banner-restored","ckit-crystal-worn"}.Where(x=>!Slots.IsDressed(x))));Capture("slice-360-art-chamber.png");});
            Steps.Enqueue(()=>GreyboxPlayValidation.SetSize(390,844));
            Steps.Enqueue(()=>{Check(UnityEngine.Object.FindFirstObjectByType<Canvas>().pixelRect.size==new Vector2(390,844),"the larger viewport again");Capture("slice-390-art-chamber.png");});
            Steps.Enqueue(()=>Act("leave-chamber"));
            Steps.Enqueue(()=>{Check(View.Flow.Screen==SliceScreen.Hub && !View.Busy,"the same files at 390 wide");Capture("slice-390-art-hub.png");});
            // Build J with the test set: the book's files, then Illumination and the ribbon read from the screen (the deck staged for the captures)
            Steps.Enqueue(()=>Act("open-journal"));
            Steps.Enqueue(()=>{var snap=View.Dial.Snapshot();Check(snap.journalView=="contents" && snap.journalContents.SequenceEqual(new[]{"The Elements","The Symbols","The Modalities","The Table","The Opposites","The Signs"}) && new[]{"journal-page","journal-contents","journal-cover","journal-ribbon"}.All(Slots.IsDressed),"at the Wing's end the contents list all five sections and the signs, on the contents page's files");Capture("slice-390-art-journal-contents.png");});
            Steps.Enqueue(()=>Act("journal-entry:5"));
            Steps.Enqueue(()=>{var snap=View.Dial.Snapshot();Check(snap.journalView=="sign" && snap.journalSign=="Aries" && snap.journalFacts.SequenceEqual(new[]{"Element: Fire","Modality: Cardinal","Polarity: Yang","Opposite: Libra"}) && snap.journalGlyph!="" && snap.journalTable && snap.journalArt=="test set" && Slots.IsDressed("journal-plate"),"Aries at the Wing's end: every fact learned, the symbol and table cell drawn, the picture from the test set");
                foreach(var it in View.Flow.SignItems(0)){it.state=(int)ItemState.Practicing;it.streak=0;it.interval=0;it.dueDay=999;}Act("journal-next");Act("journal-prev");});
            Steps.Enqueue(()=>{var snap=View.Dial.Snapshot();Check(snap.journalSign=="Aries" && Mathf.Approximately(snap.journalColour,.2f) && snap.journalInk==1 && !snap.journalGilt && !snap.journalRibbonOut && RibbonTop()<=-SliceView.JournalTop+.5f,"staged: Aries practiced with no streak, colour at 20%, its ribbon lying on the page (not due)");Capture("slice-390-art-journal-sign-low.png");});
            Steps.Enqueue(()=>{lowSaturation=PictureSaturation("slice-390-art-journal-sign-low.png");var lead=View.Flow.Deck.Item(0,ItemKind.Element);lead.streak=3;lead.interval=4;lead.dueDay=View.Flow.Sitting;Act("journal-next");Act("journal-prev");});
            Steps.Enqueue(()=>{var snap=View.Dial.Snapshot();Check(Mathf.Approximately(snap.journalColour,SliceFlow.DueFade) && snap.journalGilt && snap.journalRibbon==4 && snap.journalRibbonOut && RibbonTop()>-SliceView.JournalTop+20,"staged: a streak of three, the gilt edge, the ribbon at the ladder's top and pulled out past the page's head (due), the colour a little faded");Capture("slice-390-art-journal-sign-due.png");});
            Steps.Enqueue(()=>{View.Flow.Deck.Item(0,ItemKind.Element).dueDay=999;Act("journal-next");Act("journal-prev");});
            Steps.Enqueue(()=>{var snap=View.Dial.Snapshot();Check(snap.journalColour==1 && snap.journalGilt && !snap.journalRibbonOut,"staged: not due, full colour");Capture("slice-390-art-journal-sign-full.png");});
            Steps.Enqueue(()=>{float full=PictureSaturation("slice-390-art-journal-sign-full.png");Check(full>.4f && lowSaturation<.25f,"Illumination colours the picture from the deck (a shader, not a tint that keeps the hue): saturation "+lowSaturation.ToString("0.00")+" at 20%, "+full.ToString("0.00")+" at full");});
            for(int e=0;e<5;e++){int entry=e; // the owner's question (Sept 25): is there room? Every entry must fit on its line, one line to a rule
                Steps.Enqueue(()=>{Act("journal-contents");Act("journal-entry:"+entry);});
                Steps.Enqueue(()=>{var lines=GameObject.Find("Journal").GetComponentsInChildren<UnityEngine.UI.Text>(false).Where(t=>t.horizontalOverflow==HorizontalWrapMode.Wrap && t.verticalOverflow==VerticalWrapMode.Overflow && t.text!="").ToList();var over=lines.Where(t=>t.preferredWidth>t.rectTransform.rect.width+1).Select(t=>t.text).ToList();
                    Check(View.Dial.Snapshot().journalView=="section" && lines.Count==12 && over.Count==0,"every line of "+View.Dial.Snapshot().journalSection+" fits on its rule ("+lines.Count+" lines)"+(over.Count>0?"; too long: "+string.Join(" / ",over):""));if(entry==4)Capture("slice-390-art-journal-opposites.png");});
            }
            Steps.Enqueue(()=>Act("close-journal"));
            Steps.Enqueue(()=>{Slots.Request(Slots.TestSet,true);Act("reload");});
            Steps.Enqueue(()=>{var s=View.Dial.Snapshot();Check(View.StyleShown && s.screen=="style" && s.style && s.styleSlots.Length==124 && s.styleSounds.Length==7 && s.styleSlots.All(t=>t.EndsWith(": test set")) && s.styleSounds.All(t=>t.EndsWith(": test set")) && s.artSet==Slots.TestSet,"?style=test: the style page lists every slot on the test set, each with its source");Capture("slice-390-style-test.png");});
            Steps.Enqueue(()=>{int played=Sound.Played;Act("sound:seal");Check(Sound.LastCue=="seal" && Sound.Played==played+1,"a sound slot plays from the style page");Act("mute");Check(Sound.Muted && View.Dial.Snapshot().muted,"the test mute toggle silences the game");Act("mute");Check(!Sound.Muted,"and back");Slots.Request("",true);Act("reload");});
            Steps.Enqueue(()=>{var s=View.Dial.Snapshot();Check(View.StyleShown && s.styleSlots.Length==124 && s.styleSounds.Length==7 && s.styleSlots.All(t=>t.EndsWith(": file") || t.EndsWith(": placeholder")) && s.styleSounds.All(t=>t.EndsWith(": file") || t.EndsWith(": silent")) && s.artSet=="" && (Slots.DressedCount==0 || Slots.ArtFiles>0),"?style: the style page on the Art folder lists every slot as file or placeholder; nothing is dressed without a file");Capture("slice-390-style.png");});
            Steps.Enqueue(()=>{Slots.Request(null,null);PlayerPrefs.DeleteKey(SliceView.SaveKey);PlayerPrefs.Save();});
            Steps.Enqueue(()=>{
                Check(!PlayerPrefs.HasKey(SliceView.SaveKey) && Slots.Set=="" && !Slots.StyleRequested,"the fixture leaves no save and no set request behind");
                Check(RuntimeErrors.Count==0,"no runtime errors: "+string.Join("; ",RuntimeErrors));
                Application.logMessageReceived-=CaptureLog;
                Directory.CreateDirectory("Logs");File.WriteAllLines(ReportPath,Report);
                Debug.Log("[SlicePlayValidation] PASS: "+Report.Count+" checks.");
                SessionState.SetBool("AscendantSlicePlayValidation",false);EditorApplication.update-=Tick;EditorApplication.ExitPlaymode();
            });
            nextAt=EditorApplication.timeSinceStartup+2;EditorApplication.update+=Tick;
        }
        static void CaptureLog(string message,string stack,LogType type)
        { if((type==LogType.Error || type==LogType.Exception) && !stack.Contains("UnityEditor.Search.SearchDatabase")) RuntimeErrors.Add(message); }
        static void Tick()
        {
            if(EditorApplication.isPaused){Debug.LogWarning("[SlicePlayValidation] Resuming paused Editor for fixture.");EditorApplication.isPaused=false;}
            EditorApplication.QueuePlayerLoopUpdate();
            if(EditorApplication.timeSinceStartup<nextAt || Steps.Count==0)return;
            var view=View; if(view!=null && (view.Busy || view.Dial.Busy))return; // Beats own the frame.
            nextAt=EditorApplication.timeSinceStartup+StepGap; // each step also waits until no beat owns the frame
            try{Steps.Dequeue()();}
            catch(Exception e)
            {
                Debug.LogException(e);Directory.CreateDirectory("Logs");File.WriteAllText(ReportPath,"FAIL: "+e);
                SessionState.SetBool("AscendantSlicePlayValidation",false);EditorApplication.update-=Tick;EditorApplication.ExitPlaymode();
            }
        }
        static void Capture(string file) { Directory.CreateDirectory("Logs/Evidence");ScreenCapture.CaptureScreenshot("Logs/Evidence/"+file); }
        // Build J: read Illumination off a capture. The median HSV saturation inside the sign's picture: a tint keeps it, the shader takes it away.
        static float lowSaturation;
        static float PictureSaturation(string file)
        {
            var capture=new Texture2D(2,2);capture.LoadImage(File.ReadAllBytes("Logs/Evidence/"+file));
            float w=capture.width,h=capture.height,s=Mathf.Min(w/360f,h/800f);var samples=new List<float>(); // the canvas fits the 360 x 800 layout, centred (SliceView.ScaleCanvas)
            for(int i=2;i<=8;i++)for(int j=2;j<=8;j++){float x=SliceView.JournalX+SliceView.SignPictureSize*(i/10f-.5f),top=SliceView.SignPictureTop+SliceView.SignPictureSize*(j/10f-.5f);var c=capture.GetPixel((int)(w/2+x*s),(int)(h/2+(400-top)*s));float max=Mathf.Max(c.r,Mathf.Max(c.g,c.b)),min=Mathf.Min(c.r,Mathf.Min(c.g,c.b));samples.Add(max>0?(max-min)/max:0);} // texture rows count from the bottom
            UnityEngine.Object.DestroyImmediate(capture);samples.Sort();return samples[samples.Count/2];
        }
        static float RibbonTop() { var r=GameObject.Find("Ribbon").GetComponent<RectTransform>();return r.anchoredPosition.y+r.sizeDelta.y/2; } // on the 360 x 800 layout: -40 is the page's head edge
    }
}
