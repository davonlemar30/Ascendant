using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnityEditor;
using UnityEngine;
using Ascendant.CelestialDial;

namespace Ascendant.Build
{
    // Deterministic domain checks runnable by the same pinned Editor, without adding packages.
    public static class GreyboxValidation
    {
        static readonly List<string> Passed=new List<string>();
        static void Check(bool condition,string name) { if(!condition)throw new Exception("FAIL: "+name);Passed.Add(name); }
        static DialLesson Transfer()
        {
            var lesson=new DialLesson(()=>10);
            EnterGuided(lesson);
            Answer(lesson);Answer(lesson);lesson.Continue();return lesson;
        }
        static bool LessonMessageIsLocked(DialLesson lesson)=>lesson.Message.StartsWith("Two families down. You are halfway through the wheel.");
        static void EnterGuided(DialLesson l){ while(l.Phase==LessonPhase.Encounter) l.Continue(); l.Continue(); }
        static DialEvent Answer(DialLesson lesson)
        {
            lesson.Dial.Select(Zodiac.Destination(lesson.Dial.Start),DialInput.DirectSeat);
            var result=lesson.Seal();lesson.AfterCorrect(result);return result;
        }
        static void Wrong(DialLesson lesson,int times)
        {
            for(int i=0;i<times;i++){lesson.Dial.Select(lesson.Dial.Start,DialInput.DirectSeat);lesson.Seal();}
        }
        [MenuItem("Ascendant/Greybox/Run mechanical validation")]
        public static void Run()
        {
            Passed.Clear();
            Check(Zodiac.Seats.Count==12 && Zodiac.Seats.Select(x=>x.Name).Distinct().Count()==12,"12 unique ordered signs");
            Check(Zodiac.Seats[0].Name=="Aries" && Zodiac.Seats[11].Name=="Pisces","Aries home and Pisces boundary");
            foreach(int start in Enumerable.Range(0,12))
            {
                int end=Zodiac.Destination(start);
                Check(Zodiac.Seats[start].Element==Zodiac.Seats[end].Element,"same-element offset from "+start);
                Check(Zodiac.Evaluate(start,4,(start+4)%12),"four forward with zero start and wrap from "+start);
                Check(!Zodiac.Evaluate(start,4,(start+3)%12),"inclusive-counting error rejected from "+start);
            }
            foreach(DialInput method in Enum.GetValues(typeof(DialInput)))
            {
                var dial=new DialModel(()=>5);dial.Begin(10,0);
                dial.Step(5,method);dial.Step(-1,method);dial.Frame();
                Check(dial.Selected==2 && dial.Attempts==0 && dial.Active,"selection and corrected overshoot do not submit: "+method);
                var answer=dial.Commit();Check(answer.correctness && answer.evidence_eligible && answer.input_method==method.ToString(),"equivalent evaluator and independent evidence: "+method);
            }
            var direct=new DialModel(()=>5);direct.Begin(11,0);direct.Select(3,DialInput.DirectSeat);
            Check(direct.Attempts==0 && direct.Selected==3,"direct tap frames without sealing");
            direct.Count();Check(direct.HintLevel==1 && direct.Counting,"Level 1 is neutral counting");
            Check(direct.Commit().evidence_eligible,"Level 1 eligible evidence");
            var errors=new DialModel(()=>5);errors.Begin(0,0);errors.Select(2,DialInput.Keyboard);errors.Commit();
            Check(errors.Selected==2 && errors.HintLevel==1 && errors.Active,"first rejection stays in place with nudge");
            errors.Commit();Check(errors.Selected==2 && errors.HintLevel==2 && !errors.CanInertia,"second rejection stays in place at Level 2 without inertia");
            errors.Count();Check(errors.HintLevel==2,"counter never downgrades Level 2");
            errors.Commit();Check(errors.Selected==2 && errors.HintLevel==3 && !errors.Active && !errors.CanInertia,"third rejection requests worked recovery");
            var silence=new DialModel(()=>0);silence.Begin(7,0);silence.Home();silence.Begin(9,0);
            Check(silence.Events.All(e=>e.event_name=="problem_started"),"automatic start and home emit no detent/frame events");
            silence.ReducedMotion=true;Check(!silence.CanInertia,"reduced motion disables inertia at Level 0");
            var neutral=new DialModel(()=>0);neutral.Begin(0,0);neutral.Count();neutral.Step(6,DialInput.Drag);
            Check(neutral.Events.Where(e=>e.event_name=="dial_rotated").Select(e=>e.movement_count).SequenceEqual(new[]{1,2,3,4,5,6}),"all counting detents use identical event path");
            var happy=Transfer();Check(happy.Lit.Count(v=>v)==4 && !happy.KeyEarned,"guided family plus transfer start, no premature Key");
            Check(happy.Dial.Events.Where(e=>e.event_name=="answer_correct").All(e=>!e.evidence_eligible),"guided rule exposure is never independent evidence");
            Answer(happy);Answer(happy);
            Check(happy.Phase==LessonPhase.Complete && happy.Lit.Count(v=>v)==6 && happy.Kin.Count(v=>v)==6,"two completed families; six lit and six dormant");
            Check(happy.KeyEarned && happy.IndependentEvidence,"Key 1 requires challenge completion and independent evidence");
            Check(happy.Dial.Selected==0,"completion returns home");
            Check(LessonMessageIsLocked(happy),"six-seat completion speaks the amended completion line");
            var assisted=Transfer();Wrong(assisted,2);var assistedAnswer=Answer(assisted);
            Check(!assistedAnswer.evidence_eligible && assisted.Dial.Start!=assistedAnswer.start_seat && assisted.Dial.HintLevel==0,"Level 2 completion queues a different fresh Level 0 problem");
            Answer(assisted);Check(assisted.KeyEarned,"fresh equivalent can earn Key after assisted completion");
            var recovery=Transfer();int firstStart=recovery.Dial.Start;Wrong(recovery,3);recovery.RevealDemonstration();recovery.AfterDemonstration();
            Check(recovery.Dial.Start!=firstStart && recovery.RecoveryEncounters==2 && recovery.Dial.HintLevel==0,"worked recovery resets to a fresh equivalent, encounter two");
            Wrong(recovery,3);recovery.RevealDemonstration();recovery.AfterDemonstration();
            Check(recovery.RecoveryEncounters==3,"third encounter uses final shared recovery slot");
            Wrong(recovery,3);recovery.RevealDemonstration();recovery.AfterDemonstration();
            Check(recovery.Phase==LessonPhase.Paused && !recovery.KeyEarned && !recovery.Dial.Active,"cap pauses without a Key or extra fourth encounter");
            Check(recovery.Dial.Events.All(e=>!e.evidence_eligible),"demonstrations never create evidence");
            happy.BeginOptional();Check(happy.Phase==LessonPhase.Optional && happy.Dial.Start==2,"optional third-family probe offered and accepted");
            Answer(happy);Check(happy.Lit.Count(v=>v)==6 && happy.Phase==LessonPhase.Complete,"optional probe preserves the completed six-seat record");
            Check(happy.Dial.Events.Count(e=>e.event_name=="key1_earned")==1,"optional probe does not award another Key");
            double time=2;var timed=new DialModel(()=>time);timed.Begin(0,0);time=5;timed.Step(4,DialInput.Keyboard);var logged=timed.Commit();
            Check(logged.response_time==3 && logged.attempt_number==1 && logged.start_sign=="Aries" && logged.destination_sign=="Leo" && logged.requested_relationship=="forward_offset_4","answer log carries timing, attempt, relationship and seats");
            var flow=new SliceFlow();Check(flow.Screen==SliceScreen.Identity && flow.DisplayName=="Keeper","slice starts at identity with a default Keeper name");
            flow.SetName("  Astra ");Check(flow.DisplayName=="Astra" && flow.Continue() && flow.Screen==SliceScreen.Birth,"name trimmed; identity continues to birth prompt");
            Check(!flow.Continue(),"birth prompt requires a choice");
            flow.ChooseBirth("chart");Check(!flow.CanContinue && !flow.SetBirthDate(13,1) && flow.SetBirthDate(4,25) && flow.SunSign==1 && flow.Note.Contains("Taurus") && flow.Continue() && flow.Screen==SliceScreen.Atrium,"birth date derives the sun sign and unlocks continue");
            Check(!flow.InsertKey() && flow.Continue() && flow.Screen==SliceScreen.Wing,"atrium continues to the wing; no key insertion outside the chamber");
            Check(!flow.Continue() && flow.RevealKey() && !flow.RevealKey() && flow.Continue() && flow.Screen==SliceScreen.AtriumReturn,"wing needs the key reveal once before continuing");
            Check(flow.Continue() && flow.Screen==SliceScreen.Chamber && !flow.Continue(),"chamber is the last screen");
            Check(flow.InsertKey() && flow.LocksFilled==1 && !flow.InsertKey() && flow.End() && flow.Ended && !flow.End(),"one key fills one lock of three and ends the prototype once");
            var unknown=new SliceFlow(()=>4);unknown.Continue();unknown.ChooseBirth("unknown");Check(unknown.SunSign==4 && unknown.Note.Contains("choose one for you") && unknown.Note.Contains("Leo") && unknown.CanContinue,"I don't know assigns a sun sign and Caspar says so");
            var known=new SliceFlow();known.Continue();known.ChooseBirth("known");Check(!known.CanContinue && known.SetKnownSign(7) && known.SunSign==7 && known.CanContinue,"a known sign is accepted directly");
            Check(Zodiac.SunSign(3,21)==0 && Zodiac.SunSign(3,20)==11 && Zodiac.SunSign(1,19)==9 && Zodiac.SunSign(1,20)==10 && Zodiac.SunSign(12,22)==9 && Zodiac.SunSign(12,21)==8 && Zodiac.SunSign(8,23)==5 && Zodiac.SunSign(2,30)==11 && Zodiac.SunSign(0,5)==-1 && Zodiac.SunSign(5,32)==-1,"sun sign date table covers every boundary and rejects bad input");
            var leo=new DialLesson(()=>0);leo.SetSunSign(4);Check(leo.Sun==4 && leo.GuidedFamily==0 && leo.SecondFamily==3 && leo.OptionalFamily==1,"sun sign picks the guided, second, and optional families");
            Check(leo.IntroStep==0 && leo.DialDormant && !leo.IntroAuto,"wing entrance starts with a dormant Dial");
            leo.Continue();Check(leo.IntroStep==1 && leo.IntroAuto && leo.DialDormant,"first continue starts the wake beat, no input asked");
            leo.Continue();Check(leo.IntroStep==2 && !leo.IntroAuto && !leo.DialDormant && leo.Message.StartsWith("It responds to you"),"Dial awake, Caspar reacts");
            leo.Continue();leo.Continue();Check(leo.IntroStep==4 && leo.IntroAuto,"simulated hesitation is automatic");
            leo.Continue();Check(leo.Message.StartsWith("You... do not know astrology"),"disbelief follows the hesitation");
            leo.Continue();leo.Continue();Check(leo.IntroStep==DialLesson.IntroTeaching && leo.Lit[4] && leo.Dial.Selected==4 && leo.Message.Contains("Leo is a Fire sign"),"teaching names the player's sun sign and lights it");
            leo.Continue();Check(leo.Phase==LessonPhase.Rule && leo.Message.Contains("The Fire family is Aries, Leo, and Sagittarius"),"rule names the sun sign's family members");
            leo.Continue();Check(leo.Phase==LessonPhase.Guided && leo.Dial.Start==4 && leo.CountBeatPending,"guided problem starts at the sun sign and shows the count once");
            leo.CountBeatShown();Check(!leo.CountBeatPending,"count beat is shown once");
            leo.SetSunSign(2);Check(leo.Sun==4,"sun sign cannot change after the intro begins");
            Answer(leo);Answer(leo);Check(leo.Phase==LessonPhase.Transfer && leo.Message.Contains("Three Fire signs") && leo.Message.Contains("Now Water"),"Fire family done; Caspar hands over Water");
            leo.Continue();Check(leo.Dial.Start==3 && leo.Lit[3],"second family starts at its first sign");
            Answer(leo);Answer(leo);Check(leo.KeyEarned && leo.Lit.Count(v=>v)==6 && LessonMessageIsLocked(leo),"a Leo player also ends at six lit seats with the amended line");
            leo.Dial.Select(leo.Dial.Start,DialInput.DirectSeat);
            var wrongTwice=new DialLesson(()=>0);EnterGuided(wrongTwice);wrongTwice.CountBeatShown();wrongTwice.Seal();Check(!wrongTwice.CountBeatPending,"first wrong seal shows no count");wrongTwice.Seal();Check(wrongTwice.CountBeatPending && wrongTwice.Dial.HintLevel==2,"second wrong seal shows the count once");
            Check(wrongTwice.SeatLabel(wrongTwice.Dial.Selected).Contains(", selected") && wrongTwice.SeatLabel(Zodiac.Wrap(wrongTwice.Dial.Selected+1)).Contains(", not selected"),"screen reader labels say selected");
            // ---- v0.2: the return (Q05) ----
            var deck=new ReviewDeck();deck.IntroduceAll(0);
            Check(deck.Items.Where(i=>i.Kind==ItemKind.Element).All(i=>i.entered && i.State==ItemState.Introduced && i.dueDay==1) && deck.Items.Where(i=>i.Kind==ItemKind.Glyph).All(i=>!i.entered) && deck.Due(0).Count==0 && deck.Due(1).Count==12,"twelve sign-element items enter the deck as Introduced, due next day; glyph items wait");
            deck.RecordLesson(5,true,0);Check(deck.Items[5].State==ItemState.Practicing && deck.Items[5].streak==1 && deck.Practicing==1,"a Level 0/1 lesson answer makes the item Practicing and starts its streak");
            deck.RecordLesson(9,false,0);Check(deck.Items[9].State==ItemState.Introduced,"an assisted lesson answer stays Introduced");
            deck.RecordReview(5,true,true,1);Check(deck.Items[5].interval==1 && deck.Items[5].dueDay==4 && deck.Items[5].streak==2,"an eligible review success moves one interval forward: 1 to 3 days");
            deck.RecordReview(5,false,false,4);Check(deck.Items[5].interval==0 && deck.Items[5].dueDay==5 && deck.Items[5].streak==1,"a miss steps back one interval and one streak step");
            deck.RecordReview(9,false,false,1);Check(deck.Items[9].interval==0 && deck.Items[9].streak==0 && deck.Items[9].dueDay==2,"a miss at the first interval stays at one day with streak floor zero");
            for(int n=0;n<6;n++)deck.RecordReview(2,true,true,n*30);Check(deck.Items[2].interval==4,"the ladder caps at 30 days");
            deck.RecordReview(3,true,false,1);Check(deck.Items[3].State==ItemState.Introduced && deck.Items[3].interval==0,"an assisted correct review neither advances nor sets back");
            int day=0;var loop=new SliceFlow(()=>1,()=>day);loop.Continue();loop.ChooseBirth("known");loop.SetKnownSign(1);loop.Continue();loop.Continue();loop.RevealKey();loop.Continue();loop.Continue();loop.InsertKey();loop.End();
            Check(loop.Screen==SliceScreen.Chamber && loop.CanContinue && loop.Continue() && loop.Screen==SliceScreen.Hub && loop.AtriumStage==2 && loop.Deck.Items.Where(i=>i.Kind==ItemKind.Element).All(i=>i.entered),"after the ending, Continue reaches the Hub in Stage 2 and the deck opens");
            Check(!loop.EnterSeals() && loop.Note.Contains("Nothing is due") && loop.Screen==SliceScreen.Hub,"nothing is due on the first sitting; the Seals do not block");
            loop.AdvanceDay();Check(loop.Day==1 && loop.DueCount==12 && loop.EnterSeals() && loop.Screen==SliceScreen.Review && loop.ReviewQueue.Count==ReviewDeck.BatchSize,"advance one day (test) makes items due; a batch of six begins");
            Check(loop.ReviewQueue[0].Mode==ReviewMode.Dial && loop.ReviewQueue[1].Mode==ReviewMode.Tap,"review alternates compressed Dial and direct tap");
            loop.FinishReview(true,true);Check(loop.ReviewIndex==1 && loop.Deck.Items[loop.ReviewQueue[0].seat].State==ItemState.Practicing,"a compressed Dial review success advances its item");
            var tapTask=loop.CurrentReview;string wrong=Zodiac.Seats[tapTask.seat].Element=="Fire" ? "Water" : "Fire";
            Check(!loop.AnswerTap(wrong) && !tapTask.done && loop.Note.Contains("Try once more"),"a direct-tap miss gives one nudge and a retry");
            Check(loop.AnswerTap(Zodiac.Seats[tapTask.seat].Element) && tapTask.done && tapTask.correct && loop.Deck.Items[tapTask.seat].State==ItemState.Introduced,"a correct tap after a nudge counts as correct but not as eligible evidence");
            var tap3=loop.ReviewQueue[3];loop.FinishReview(false,false);Check(loop.ReviewIndex==3,"a Dial miss moves on");
            loop.AnswerTap(Zodiac.Seats[tap3.seat].Element=="Air" ? "Water" : "Air");loop.AnswerTap(Zodiac.Seats[tap3.seat].Element=="Air" ? "Water" : "Air");
            Check(tap3.done && !tap3.correct && loop.Note.Contains("We will come back"),"two tap misses reveal the element and move on");
            loop.FinishReview(true,true);var last=loop.CurrentReview;loop.AnswerTap(Zodiac.Seats[last.seat].Element);
            Check(loop.ReviewDone && loop.ReviewSummary.EndsWith("of 6 seals held.") && loop.ReviewsChecked==1 && loop.LeaveReview() && loop.Screen==SliceScreen.Hub,"six items finish the batch with a summary; back to the Hub");
            Check(loop.EnterWing() && loop.Screen==SliceScreen.Wing && loop.LeaveWing() && loop.Screen==SliceScreen.Hub && loop.AtriumStage==2,"the Wing can be entered from the Hub and left back to it");
            loop.MarkWheelComplete();loop.EnterWing();loop.LeaveWing();Check(loop.AtriumStage==3 && loop.V02Complete,"twelve lit seats and one more return complete v0.2");
            var save=loop.ToSave(new bool[12],new bool[12],true);var resumed=new SliceFlow(()=>1,()=>day);
            Check(resumed.Restore(save) && resumed.Screen==SliceScreen.Hub && resumed.SunSign==1 && resumed.AtriumStage==3 && resumed.WheelComplete && resumed.DayOffset==1 && resumed.Deck.Items[5].State==loop.Deck.Items[5].State && resumed.ReviewsChecked==1,"a saved session resumes at the Hub with deck, day, and stage");
            Check(!new SliceFlow().Restore(new SaveData{atriumStage=1,sunSign=1}),"a save from before the Hub does not resume");
            var unit=new DialLesson(()=>0);unit.SetSunSign(1);EnterGuided(unit);Answer(unit);Answer(unit);unit.Continue();Answer(unit);Answer(unit);
            Check(unit.KeyEarned && unit.FamiliesComplete==2 && unit.CanContinueUnit && unit.BeginContinuation() && unit.Phase==LessonPhase.Continuation && unit.Dial.Start==2 && unit.Lit[2] && unit.Message.Contains("done this twice"),"Unit 1.1 continues with the third family at Level 0");
            Answer(unit);Answer(unit);Check(unit.FamiliesComplete==3 && unit.Phase==LessonPhase.Continuation && unit.Dial.Start==3 && unit.Message.Contains("last family"),"third family done; the fourth begins on its own");
            Answer(unit);Answer(unit);Check(unit.Phase==LessonPhase.AllLit && unit.WheelComplete && unit.Lit.All(v=>v) && unit.Kin.All(v=>v) && !unit.CanContinueUnit,"twelve seats lit, four families, no Key");
            Check(unit.Dial.Events.Count(e=>e.event_name=="key1_earned")==1 && unit.Dial.Events.Any(e=>e.event_name=="wheel_completed"),"the continuation awards no second Key and logs wheel_completed");
            var rv=new DialLesson(()=>0);rv.RestoreProgress(4,Enumerable.Range(0,12).Select(i=>i%4==0).ToArray(),Enumerable.Range(0,12).Select(i=>i%4==0).ToArray(),true);
            Check(rv.Sun==4 && rv.KeyEarned && rv.Phase==LessonPhase.Complete && !rv.DialDormant && rv.FamiliesComplete==1 && rv.CanContinueUnit,"restored progress skips the intro and can continue");
            int rvSeat=-1;bool rvCorrect=false,rvEligible=false;rv.ReviewFinished+=(seat,c,e)=>{rvSeat=seat;rvCorrect=c;rvEligible=e;};
            Check(rv.BeginReview(4) && rv.Phase==LessonPhase.Review && rv.Dial.Start==4 && rv.Dial.Active && rv.Message.StartsWith("Find the next sign"),"a compressed review starts framed on the item's sign");
            rv.Dial.Select(8,DialInput.DirectSeat);rv.Seal();Check(rvSeat==4 && rvCorrect && rvEligible && !rv.Dial.Active,"a correct review Seal reports eligible success and returns home");
            rv.EndReview();Check(rv.Phase==LessonPhase.Complete && rv.BeginReview(0),"review ends back in the prior phase and another can begin");
            rv.Dial.Select(1,DialInput.DirectSeat);rv.Seal();Check(rv.Dial.Active && rv.Message.Contains("Try once more"),"first review miss nudges and allows a retry");
            rv.Seal();Check(rvSeat==0 && !rvCorrect && rv.Message.Contains("It is Leo"),"second review miss reveals the answer and moves on");
            // ---- v0.3: glyphs and Key 2 ----
            var glyphFont=Resources.Load<Font>("Fonts/NotoSansSymbols");Check(glyphFont!=null && Enumerable.Range(0,12).All(i=>glyphFont.HasCharacter(Zodiac.Seats[i].Glyph[0])),"the placeholder glyph font carries all twelve zodiac symbols");
            var g=new DialLesson(()=>0);g.SetSunSign(1);EnterGuided(g);Answer(g);Answer(g);g.Continue();Answer(g);Answer(g);g.BeginContinuation();Answer(g);Answer(g);Answer(g);Answer(g);
            Check(g.WheelComplete && g.CanBeginGlyphs && g.BeginGlyphs() && g.Phase==LessonPhase.GlyphNames && g.CurrentGlyph==0 && g.GlyphsShown,"glyph unit begins after the wheel is lit, Part A first");
            Check(Enumerable.Range(0,12).All(seat=>{var o=g.GlyphOptions(seat);return o.Length==4 && o.Distinct().Count()==4 && o.Contains(seat);}),"every glyph offers four distinct names including its own");
            Check(g.AnswerGlyphName(0) && g.GlyphNamed[0] && g.CurrentGlyph==1 && g.Dial.Events.Last(e=>e.event_name=="glyph_named").evidence_eligible,"a correct first tap names the glyph at Level 0");
            Check(!g.AnswerGlyphName(5) && g.GlyphMisses==1 && g.Message.Contains("Not that one") && g.CurrentGlyph==1,"first miss nudges without naming");
            Check(g.AnswerGlyphName(1) && g.CurrentGlyph==2 && g.Dial.Events.Last(e=>e.event_name=="glyph_named").evidence_eligible,"a correct tap after one nudge is Level 1 evidence");
            Check(!g.AnswerGlyphName(6) && !g.AnswerGlyphName(7) && g.GlyphNamed[2] && g.CurrentGlyph==3 && g.Message.Contains("mark of Gemini") && !g.Dial.Events.Last(e=>e.event_name=="glyph_named").evidence_eligible,"second miss reveals the name, no evidence, and moves on");
            for(int seat=3;seat<12;seat++) g.AnswerGlyphName(seat);
            Check(g.Phase==LessonPhase.GlyphWheel && g.Dial.Active && g.Dial.Start==0 && g.Dial.Target==0 && g.NamesHidden && g.SeatLabel(5).StartsWith("Mark") && !g.SeatLabel(5).Contains("Virgo") && g.Dial.Relationship=="seat_of_sign","Part B starts with names hidden, Aries first, labels that do not leak names, and the seat-of-sign relationship");
            void Place(DialLesson l,int seat){l.Dial.Select(seat,DialInput.DirectSeat);var r=l.Seal();l.AfterCorrect(r);}
            Place(g,0);Check(g.GlyphPlaced[0] && g.GlyphEvidence && g.Dial.Target==1,"sealing on the target places the glyph and counts as evidence");
            g.Dial.Select(5,DialInput.DirectSeat);g.Seal();Check(g.Dial.Active && g.Dial.HintLevel==1 && g.Message.Contains("count forward"),"first wrong Seal in Part B nudges");
            g.Seal();Check(g.NameRevealed[1] && g.Dial.HintLevel==2 && !g.SeatLabel(1).StartsWith("Mark"),"second wrong Seal reveals the name on its seat and in its label");
            g.Dial.Select(1,DialInput.DirectSeat);var placed=g.Seal();g.AfterCorrect(placed);Check(placed.correctness && !placed.evidence_eligible && g.GlyphPlaced[1] && g.Dial.Target==2,"a Level 2 placement counts for completion, not evidence");
            g.Dial.Select(8,DialInput.DirectSeat);g.Seal();g.Seal();g.Seal();Check(!g.Dial.Active && g.Dial.HintLevel==3,"third wrong Seal in Part B asks for a demonstration");
            g.RevealDemonstration();g.AfterDemonstration();Check(g.GlyphPlaced[2] && g.NameRevealed[2] && g.Dial.Target==3 && g.Dial.Active,"the demonstration places the glyph without evidence and moves to the next mark");
            for(int seat=3;seat<12;seat++) Place(g,seat);
            Check(g.Key2Earned && g.Phase==LessonPhase.Key2 && g.Keys==2 && g.GlyphsShown && !g.NamesHidden && g.Dial.Events.Count(e=>e.event_name=="key2_earned")==1,"twelve placed with at least one Level 0/1 answer earns Key 2 once");
            var noEvidence=new DialLesson(()=>0);noEvidence.SetSunSign(1);EnterGuided(noEvidence);Answer(noEvidence);Answer(noEvidence);noEvidence.Continue();Answer(noEvidence);Answer(noEvidence);noEvidence.BeginContinuation();Answer(noEvidence);Answer(noEvidence);Answer(noEvidence);Answer(noEvidence);noEvidence.BeginGlyphs();
            for(int seat=0;seat<12;seat++) noEvidence.AnswerGlyphName(seat);
            for(int seat=0;seat<12;seat++){noEvidence.Dial.Select(Zodiac.Wrap(seat+1),DialInput.DirectSeat);noEvidence.Seal();noEvidence.Seal();Place(noEvidence,seat);}
            Check(!noEvidence.Key2Earned && noEvidence.Phase==LessonPhase.Paused && noEvidence.Keys==1,"twelve assisted placements pause cleanly without Key 2");
            var gd=new ReviewDeck();gd.IntroduceAll(0);gd.IntroduceAll(0,ItemKind.Glyph);Check(gd.Items.Length==24 && gd.Due(1).Count==24 && gd.Item(3,ItemKind.Glyph).Kind==ItemKind.Glyph,"the deck holds twelve element and twelve glyph items");
            gd.RecordLesson(4,true,0,ItemKind.Glyph);Check(gd.Item(4,ItemKind.Glyph).State==ItemState.Practicing && gd.Item(4,ItemKind.Element).State==ItemState.Introduced,"glyph evidence advances only the glyph item");
            int gday=0;var gflow=new SliceFlow(()=>1,()=>gday);gflow.Continue();gflow.ChooseBirth("known");gflow.SetKnownSign(1);gflow.Continue();gflow.Continue();gflow.RevealKey();gflow.Continue();gflow.Continue();gflow.InsertKey();gflow.End();gflow.Continue();
            gflow.MarkWheelComplete();gflow.EnterWing();gflow.LeaveWing();gflow.StartGlyphs();Check(gflow.GlyphsStarted && gflow.Deck.Due(gflow.Day+1).Count(i=>i.Kind==ItemKind.Glyph)==12,"starting the glyph unit introduces twelve glyph items");
            gflow.MarkKey2();gflow.EnterWing();gflow.LeaveWing();Check(gflow.Keys==2 && gflow.AtriumStage==4 && gflow.V03Complete,"Key 2 and one more return complete v0.3");
            gflow.AdvanceDay();Check(gflow.EnterSeals() && gflow.ReviewQueue.Count==6 && gflow.ReviewQueue.All(t=>t.Mode!=ReviewMode.Glyph),"element items are due before glyph items in the batch order");
            var gsave=gflow.ToSave(new bool[12],new bool[12],true);var gres=new SliceFlow(()=>1,()=>gday);Check(gres.Restore(gsave) && gres.Keys==2 && gres.GlyphStage==2 && gres.V03Complete && gres.Deck.Item(0,ItemKind.Glyph).entered,"a save carries Keys, glyph stage, and glyph items");
            var greview=new SliceFlow(()=>1,()=>gday);greview.Continue();greview.ChooseBirth("known");greview.SetKnownSign(1);greview.Continue();greview.Continue();greview.RevealKey();greview.Continue();greview.Continue();greview.InsertKey();greview.End();greview.Continue();
            foreach(var it in greview.Deck.Items) if(it.Kind==ItemKind.Element){it.dueDay=99;} greview.StartGlyphs();greview.AdvanceDay();
            Check(greview.EnterSeals() && greview.ReviewQueue.All(t=>t.Mode==ReviewMode.Glyph),"glyph items review in the glyph form");
            var gt=greview.CurrentReview;var gopts=greview.GlyphReviewOptions(gt.seat);int wrongSeat=gopts.First(o=>o!=gt.seat);
            Check(!greview.AnswerGlyph(wrongSeat) && greview.Note.Contains("Try once more") && greview.AnswerGlyph(gt.seat) && gt.done && gt.correct && greview.Note.Contains("mark of"),"a glyph review nudges once then accepts the name");
            var grl=new DialLesson(()=>0);grl.RestoreProgress(1,Enumerable.Repeat(true,12).ToArray(),Enumerable.Repeat(true,12).ToArray(),true);grl.RestoreGlyphs(2,12,true);Check(grl.Key2Earned && grl.Keys==2 && grl.Phase==LessonPhase.Key2 && !grl.CanBeginGlyphs,"restored Key 2 does not reopen the glyph unit");
            var grl2=new DialLesson(()=>0);grl2.RestoreProgress(1,Enumerable.Repeat(true,12).ToArray(),Enumerable.Repeat(true,12).ToArray(),true);grl2.RestoreGlyphs(0,5,false);Check(grl2.CanBeginGlyphs && grl2.BeginGlyphs() && grl2.CurrentGlyph==5,"restored Part A progress resumes at the next mark");
            Directory.CreateDirectory("Logs");File.WriteAllLines("Logs/greybox-mechanical-validation.txt",Passed);
            Debug.Log("[GreyboxValidation] PASS: "+Passed.Count+" checks. Report: Logs/greybox-mechanical-validation.txt");
        }
    }
}
