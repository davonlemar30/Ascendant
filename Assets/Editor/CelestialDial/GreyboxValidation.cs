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
            Check(deck.Items.All(i=>i.entered && i.State==ItemState.Introduced && i.dueDay==1) && deck.Due(0).Count==0 && deck.Due(1).Count==12,"twelve sign-element items enter the deck as Introduced, due next day");
            deck.RecordLesson(5,true,0);Check(deck.Items[5].State==ItemState.Practicing && deck.Items[5].streak==1 && deck.Practicing==1,"a Level 0/1 lesson answer makes the item Practicing and starts its streak");
            deck.RecordLesson(9,false,0);Check(deck.Items[9].State==ItemState.Introduced,"an assisted lesson answer stays Introduced");
            deck.RecordReview(5,true,true,1);Check(deck.Items[5].interval==1 && deck.Items[5].dueDay==4 && deck.Items[5].streak==2,"an eligible review success moves one interval forward: 1 to 3 days");
            deck.RecordReview(5,false,false,4);Check(deck.Items[5].interval==0 && deck.Items[5].dueDay==5 && deck.Items[5].streak==1,"a miss steps back one interval and one streak step");
            deck.RecordReview(9,false,false,1);Check(deck.Items[9].interval==0 && deck.Items[9].streak==0 && deck.Items[9].dueDay==2,"a miss at the first interval stays at one day with streak floor zero");
            for(int n=0;n<6;n++)deck.RecordReview(2,true,true,n*30);Check(deck.Items[2].interval==4,"the ladder caps at 30 days");
            deck.RecordReview(3,true,false,1);Check(deck.Items[3].State==ItemState.Introduced && deck.Items[3].interval==0,"an assisted correct review neither advances nor sets back");
            int day=0;var loop=new SliceFlow(()=>1,()=>day);loop.Continue();loop.ChooseBirth("known");loop.SetKnownSign(1);loop.Continue();loop.Continue();loop.RevealKey();loop.Continue();loop.Continue();loop.InsertKey();loop.End();
            Check(loop.Screen==SliceScreen.Chamber && loop.CanContinue && loop.Continue() && loop.Screen==SliceScreen.Hub && loop.AtriumStage==2 && loop.Deck.Items.All(i=>i.entered),"after the ending, Continue reaches the Hub in Stage 2 and the deck opens");
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
            Directory.CreateDirectory("Logs");File.WriteAllLines("Logs/greybox-mechanical-validation.txt",Passed);
            Debug.Log("[GreyboxValidation] PASS: "+Passed.Count+" checks. Report: Logs/greybox-mechanical-validation.txt");
        }
    }
}
