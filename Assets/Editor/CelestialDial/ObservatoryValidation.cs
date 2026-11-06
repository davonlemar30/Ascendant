using System;
using System.Linq;
using UnityEngine;
using Ascendant.CelestialDial;
namespace Ascendant.Build
{
    public static partial class GreyboxValidation
    {
        static SliceFlow ObservatoryReady()
        {
            var f=new SliceFlow();Check(f.Restore(DevCheckpoints.Play("whole","Tester",1)),"Stage 2: restore Wing checkpoint");
            Check(!f.ObservatoryOpen&&f.RevealObservatoryDoor()&&f.ObservatoryOpen&&f.EnterObservatory(),"Stage 2: Wing whole gets the door scene before entry");return f;
        }
        static void Names(ObservatoryModel o,bool help=false)
        {
            Check(o.Begin()&&o.Continue(),"Stage 2: symbols begin");
            for(int i=0;i<10;i++){Check(o.Target==i,"Stage 2: naming seat "+i);if(help){o.AnswerName((i+1)%10);o.AnswerName((i+1)%10);}else o.AnswerName(i);o.Continue();}
            Check(o.Phase==ObservatoryPhase.WorkIntro&&o.Continue(),"Stage 2: ten names lead to work");
        }
        static void Works(ObservatoryModel o,bool help=false)
        {
            for(int i=0;i<10;i++){if(help)o.Ask();o.Select(i);Check(o.Seal(),"Stage 2: work seat "+i);o.Continue();}
        }
        static void RingsAndBands(SliceFlow f,bool help=false)
        {
            var o=f.Observatory;Check(f.LeaveObservatory()&&f.EnterChamber()&&f.SpendKey()&&f.LeaveChamber()&&f.EnterObservatory()&&o.Begin()&&o.Continue(),"Stage 2: spend Key 5 and open ring lesson");
            for(int i=0;i<10;i++)
            {
                o.Select(i);if(help){int wrong=Planets.All[i].Ring%8+1;o.Ring(wrong);Check(o.Hint==1&&!o.Progress.placed[i],"Stage 2: first ring miss nudges");o.Ring(wrong);Check(o.Hint==2&&!o.Progress.placed[i],"Stage 2: second ring miss gives rule");o.Ring(wrong);Check(o.Hint==3&&o.Progress.placed[i]&&o.Reveal,"Stage 2: third miss seats sphere, two reappearances maximum");}
                else Check(o.Ring(Planets.All[i].Ring),"Stage 2: correct ring "+i);o.Continue();
            }
            Check(o.Phase==ObservatoryPhase.Retro&&o.Progress.retro&&o.Message==ObservatoryCopy.Retro,"Stage 2: retrograde recognition beat, exact copy");o.Continue();o.Continue();
            for(int i=0;i<10;i++){o.Select(i);int wrong=(Planets.All[i].Band+1)%4;o.Band(wrong);Check(o.Misses==1&&o.Message==ObservatoryCopy.U22BandNudge,"Stage 2: first company miss nudges");o.Band(wrong);Check(o.Reveal&&o.Progress.banded[i]&&!f.Deck.Item(i,ItemKind.PlanetCompany).State.Equals(ItemState.Practicing),"Stage 2: second company miss reveals without evidence");o.Continue();}
            Check(o.Phase==ObservatoryPhase.Questions,"Stage 2: two individuality cards follow companies");
            for(int i=0;i<2;i++){var before=o.QuestionOrder;o.AnswerQuestion(1);Check(o.Message==o.Question.why&&!o.QuestionOrder.SequenceEqual(before)&&!o.AwaitingNext,"Stage 2: wrong card explains and reshuffles");o.AnswerQuestion(1);Check(o.Reveal&&o.AwaitingNext,"Stage 2: second card miss reveals");o.Continue();}
        }
        public static void RunStage2() {FontSetup.Apply();Run();}
        static void ValidatePlanetPracticeGeometry()
        {
            var probe=new GameObject("Planet Practice probe",typeof(RectTransform)).AddComponent<UnityEngine.UI.Text>();probe.horizontalOverflow=HorizontalWrapMode.Overflow;
            int Lines(string text,Font font,int size,float width){probe.font=font;probe.fontSize=size;return SliceView.WrapWords(probe,text,width).Count;}
            var italic=Resources.Load<Font>(SliceView.ItalicFont);var bold=ButtonLook.EngravedFont;
            foreach(var c in PracticeBook.Data.concepts.Skip(5))foreach(var q in c.questions){int ask=Lines(q.ask,italic,17,SliceView.QuizAskWidth),why=Math.Max(Lines(PracticeBook.Data.right+" "+q.why,italic,14,SliceView.QuizAskWidth),Lines(PracticeBook.Data.wrong+" "+q.why,italic,14,SliceView.QuizAskWidth));var layout=SliceView.PlanetQuizLayout(q.planet>=0,ask,4,why);Check(ask<=4&&why<=6&&layout.w+44<=SliceView.QuizBottom,"Stage 2: complete ask/why and Next fit new Practice page: "+q.ask);Check(q.choices.All(x=>Lines(x,bold,11,SliceView.ChoiceWidth-60)*13<=44),"Stage 2: wrapped choices fit 44px frame at minimum11px: "+q.ask);}
            UnityEngine.Object.DestroyImmediate(probe.gameObject);
        }
        static void ValidateObservatory()
        {
            Check(new float[]{20,75,95,109,123,137,151,165}.Select(SliceView.ObservatoryRingAt).SequenceEqual(Enumerable.Range(1,8))&&SliceView.ObservatoryRingAt(190)==-1,"Stage 2: drawn rings and radial tap/drop targets have the same ordinal positions");
            Check(Planets.All.Length==10&&Planets.All.Select(p=>p.Glyph).Distinct().Count()==10&&Planets.All.Select(p=>p.Ring).SequenceEqual(new[]{1,2,2,2,3,4,5,6,7,8}),"Stage 2: ten distinct glyphs, pace rings including shared Sun/Mercury/Venus ring");
            Check(Planets.All.Select(p=>p.Work).SequenceEqual(new[]{"to feel","to be","to think","to value","to act","to expand","to structure","to awaken","to dissolve","to transform"}),"Stage 2: approved atomic works");
            Check(Planets.All.Select(p=>p.Band).SequenceEqual(new[]{0,0,1,1,1,2,2,3,3,3}),"Stage 2: modern four companies");
            foreach(int s in Enumerable.Range(0,10))Check(Planets.Options(s).Length==4&&Planets.Options(s).Contains(s)&&Planets.Options(s).Distinct().Count()==4,"Stage 2: four distinct names including answer "+s);
            Check(Planets.Options(2).Contains(3)&&Planets.Options(3).Contains(2)&&Planets.Options(5).Contains(8)&&Planets.Options(8).Contains(5)&&Planets.Options(0).Contains(1)&&Planets.Options(1).Contains(0),"Stage 2: every confusable twin included");
            for(int s=0;s<10;s++)Check(Resources.Load<Font>(s==1||s==9?"Fonts/NotoSansSymbols2-Regular":"Fonts/NotoSansSymbols").HasCharacter(Planets.All[s].Glyph[0]),"Stage 2: bundled font draws planet "+s);
            Check(Resources.Load<Font>("Fonts/EBGaramond-Bold").HasCharacter('\u211e'),"Stage 2: bundled Garamond draws retrograde; no added font");
            var f=ObservatoryReady();var o=f.Observatory;Names(o);o.Select(1);o.Seal();Check(o.Hint==1&&o.Message==ObservatoryCopy.U21BNudge,"Stage 2: first work miss, level 1");o.Select(0);o.Seal();Check(o.Progress.workEvidence&&f.Deck.Item(0,ItemKind.PlanetFunction).State==ItemState.Practicing,"Stage 2: level 1 earns evidence");o.Continue();
            o.Select(0);o.Seal();o.Select(0);o.Seal();Check(o.Hint==2&&o.Message==ObservatoryCopy.U21BRule,"Stage 2: second work miss, rule level 2");o.Select(0);o.Seal();Check(o.Hint==3&&o.Reveal,"Stage 2: miss after level 2 reveals sphere");int capped=o.Misses;o.Select(0);o.Seal();Check(o.Misses==capped&&o.Hint==3&&!o.AwaitingNext,"Stage 2: Level 3 reconstruction accepts only the shown sphere, no further missed encounters");o.Select(1);o.Seal();Check(f.Deck.Item(1,ItemKind.PlanetFunction).State==ItemState.Introduced,"Stage 2: level 3 is not evidence");o.Continue();
            for(int i=2;i<10;i++){o.Select(i);o.Seal();o.Continue();}Check(f.Keys==5&&o.Phase==ObservatoryPhase.Key5,"Stage 2: eligible work earns Key 5");
            RingsAndBands(f);Check(f.Keys==6&&o.Phase==ObservatoryPhase.Key6,"Stage 2: eligible ring placement earns Key 6");
            Check(f.LeaveObservatory()&&f.EnterChamber()&&f.SpendKey()&&f.BooksOpen==2&&f.KeysSpent==6&&f.ObservatoryWhole,"Stage 2: lock 6 opens Book 2, end state");
            var no=ObservatoryReady();Names(no.Observatory,true);Works(no.Observatory,true);Check(no.Keys==4&&no.Observatory.Phase==ObservatoryPhase.Paused5&&!no.Observatory.Progress.workEvidence&&no.Observatory.Message==ObservatoryCopy.Key5Paused,"Stage 2: all assisted pauses without Key 5");
            no.Observatory.Continue();no.Observatory.Continue();Works(no.Observatory);Check(no.Keys==5,"Stage 2: replay Part B earns permanent Key 5");RingsAndBands(no,true);Check(no.Keys==5&&no.Observatory.Phase==ObservatoryPhase.Paused6&&no.Observatory.Message==ObservatoryCopy.Key6Paused,"Stage 2: all assisted rings pause without Key 6");
            foreach(string id in new[]{"key5","key6","obswhole"}){var save=DevCheckpoints.Play(id,"Tester",1);var back=new SliceFlow();Check(back.Restore(JsonUtility.FromJson<SaveData>(JsonUtility.ToJson(save)))&&back.Keys==(id=="key5"?5:6)&&back.KeysSpent==(id=="key5"?4:id=="key6"?5:6),"Stage 2: DEV "+id+" through real model calls, JSON roundtrip");}
            foreach(string id in new[]{"key2","whole"}){var save=DevCheckpoints.Play(id,"Old",1);save.version=5;save.observatory=null;var back=new SliceFlow();Check(back.Restore(save)&&back.Keys==save.keys&&back.KeysSpent==save.locksFilled&&!back.Observatory.Progress.doorSeen&&!back.Deck.Items.Any(i=>i.kind>=5&&i.entered)&&back.Deck.Items.Take(60).Select(i=>JsonUtility.ToJson(i)).SequenceEqual(save.deck.Take(60).Select(i=>JsonUtility.ToJson(i))),"Stage 2: v5 "+id+" keeps every Wing item and waits for door scene");}
            foreach(var kind in new[]{ItemKind.PlanetGlyph,ItemKind.PlanetFunction,ItemKind.PlanetCompany}){
                var save=DevCheckpoints.Play("obswhole","Review",1);foreach(var item in save.deck){item.entered=item.kind==(int)kind&&item.seat==2;item.state=0;item.streak=0;item.dueDay=0;}
                var review=new SliceFlow();Check(review.Restore(save)&&review.EnterWing()&&review.EnterDial()&&review.EnterPractice()&&review.Sittings==1&&review.PlanetReview,"Stage 2: "+kind+" uses only the Dial's existing sitting fork");int slot=kind==ItemKind.PlanetCompany?Planets.All[2].Band:Array.IndexOf(review.PlanetReviewOptions,2);Check(review.AnswerPlanetReview(slot)&&review.PracticeDone&&review.Deck.Item(2,kind).State==ItemState.Practicing&&review.Keys==6,"Stage 2: independent "+kind+" review records eligible evidence, Keys unchanged");
            }
            Check(PracticeBook.Data.concepts.Take(5).SelectMany(c=>c.questions).All(q=>q.planet==-1),"Stage 2: old Practice questions default to no planet glyph");
            Check(PracticeBook.Data.concepts.Take(7).Count()==7&&PracticeBook.Data.concepts.Skip(5).Take(2).All(c=>c.questions.Length==6),"Stage 2: two new Practice concepts, twelve exact questions");
            ValidatePlanetPracticeGeometry();
            Check(SliceView.PracticeRowTop+58*7<=SliceView.PageOrnamentTop,"Stage 2: seven concept rows fit the page");
        }
    }
}
