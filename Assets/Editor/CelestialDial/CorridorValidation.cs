using System;
using System.Linq;
using UnityEngine;
using Ascendant.CelestialDial;
namespace Ascendant.Build
{
    public static partial class GreyboxValidation
    {
        static SliceFlow CorridorReady()
        {
            var f=new SliceFlow();Check(f.Restore(DevCheckpoints.Play("obswhole","Tester",1)),"Corridor: restore six spent Keys");
            Check(!f.CorridorOpen&&f.EnterObservatory()&&f.RevealCorridorDoor()&&f.CorridorOpen&&f.EnterCorridor(),"Corridor: back arch opens only after Key 6 spent");return f;
        }
        static void Traditional(CorridorModel c,bool assisted)
        {
            Check(c.Begin()&&c.Continue(),"Corridor: traditional wheel begins");
            foreach(var pair in Rulership.Traditional)foreach(int sign in pair){
                if(assisted){int wrong=Enumerable.Range(0,12).First(s=>!pair.Contains(s));c.Place(wrong);Check(c.Hint==1&&!c.AwaitingNext,"Corridor: first placement miss nudges");c.Place(wrong);Check(c.Hint==2&&!c.AwaitingNext,"Corridor: second placement miss gives rule");c.Place(wrong);Check(c.Hint==3&&c.Reveal&&c.Progress.traditional[sign],"Corridor: third placement miss places token, two reappearances");}
                else Check(c.Place(sign),"Corridor: place traditional sign "+sign);
                c.Continue();
            }
        }
        static void FinishCorridor(SliceFlow f,bool assisted)
        {
            var c=f.Corridor;Check(f.LeaveCorridor()&&f.LeaveObservatory()&&f.EnterChamber()&&f.SpendKey()&&f.KeysSpent==7&&f.BooksOpen==2&&f.LeaveChamber()&&f.EnterObservatory()&&f.EnterCorridor()&&c.Begin()&&c.Continue(),"Corridor: Key 7 spends into Book 3 first lock");
            foreach(int sign in Rulership.ModernSigns){Check(c.Place(sign),"Corridor: modern ruler beside traditional "+sign);c.Continue();}
            c.Continue();
            for(int i=0;i<6;i++){
                Check(c.Planet>=0&&c.Planet<7&&c.DetrimentAnswer==Zodiac.Opposite(c.RuledSign),"Corridor: detriment uses a traditional ruled sign "+i);
                if(assisted)c.Ask();c.Select(c.DetrimentAnswer);Check(c.Seal(),"Corridor: detriment Seal "+i);c.Continue();
            }
            c.Continue();for(int i=0;i<3;i++){Check(c.CardChoices.Length==4&&c.CardChoices.Distinct().Count()==4,"Corridor: four distinct ruler explanations");c.AnswerCard(Array.IndexOf(c.CardOrder,0));c.Continue();}
        }
        static void ValidateCorridor()
        {
            Check(Enumerable.Range(0,12).Select(Rulership.TraditionalRuler).SequenceEqual(new[]{4,3,2,0,1,2,3,4,5,6,6,5}),"Corridor: complete canon traditional rulership table");
            Check(Rulership.ModernSigns.SequenceEqual(new[]{10,11,7})&&Enumerable.Range(0,12).Select(Rulership.ModernRuler).SequenceEqual(new[]{-1,-1,-1,-1,-1,-1,-1,9,-1,-1,7,8}),"Corridor: three modern rulers, no replacement of traditional rulers");
            int[][] detriments={new[]{9},new[]{10},new[]{8,11},new[]{7,0},new[]{6,1},new[]{2,5},new[]{3,4}};
            for(int p=0;p<7;p++)Check(Rulership.Traditional[p].Select(Zodiac.Opposite).SequenceEqual(detriments[p]),"Corridor: detriment equals opposite of ruled signs for "+Planets.All[p].Name);
            var f=CorridorReady();Traditional(f.Corridor,false);Check(f.Keys==7&&f.Corridor.Progress.placementEvidence,"Corridor: independent placements earn Key 7");FinishCorridor(f,false);Check(f.Keys==8&&f.Corridor.Progress.detrimentEvidence,"Corridor: independent detriment earns Key 8");
            Check(f.ObservatoryLevel==4&&f.LeaveCorridor()&&f.LeaveObservatory()&&f.EnterChamber()&&f.SpendKey()&&f.KeysSpent==8&&f.BooksOpen==2,"Corridor: lock 8, lower Observatory lit, Book 3 remains sealed");
            var no=CorridorReady();Traditional(no.Corridor,true);Check(no.Keys==6&&no.Corridor.Phase==CorridorPhase.Paused7&&!no.Corridor.Progress.placementEvidence,"Corridor: reveals never award Key 7");no.Corridor.Continue();Traditional(no.Corridor,false);Check(no.Keys==7,"Corridor: replay independent work earns Key 7");FinishCorridor(no,true);Check(no.Keys==7&&no.Corridor.Phase==CorridorPhase.Paused8&&!no.Corridor.Progress.detrimentEvidence,"Corridor: level 2 detriments never award Key 8");
            no.Corridor.Continue();no.Corridor.Continue();for(int i=0;i<6;i++){no.Corridor.Select(no.Corridor.DetrimentAnswer);no.Corridor.Seal();no.Corridor.Continue();}no.Corridor.Continue();for(int i=0;i<3;i++){no.Corridor.AnswerCard(Array.IndexOf(no.Corridor.CardOrder,0));no.Corridor.Continue();}Check(no.Keys==8,"Corridor: replay detriments can earn permanent Key 8");
            var ladder=CorridorReady();Traditional(ladder.Corridor,false);var c=ladder.Corridor;ladder.LeaveCorridor();ladder.LeaveObservatory();ladder.EnterChamber();ladder.SpendKey();ladder.LeaveChamber();ladder.EnterObservatory();ladder.EnterCorridor();c.Begin();c.Continue();foreach(int s in Rulership.ModernSigns){c.Place(s);c.Continue();}c.Continue();int answer=c.DetrimentAnswer;c.Select(Zodiac.Wrap(answer+1));c.Seal();Check(c.Hint==1,"Corridor: first detriment miss gives level 1");c.Seal();Check(c.Hint==2,"Corridor: second detriment miss gives level 2");c.Seal();Check(c.Hint==3&&c.Reveal&&!c.AwaitingNext,"Corridor: third detriment miss reveals reconstruction");int misses=c.Misses;c.Seal();Check(c.Misses==misses,"Corridor: revealed detriment has no further wrong attempts");c.Select(answer);c.Seal();Check(!c.Progress.detrimentEvidence,"Corridor: reconstruction is never evidence");
            foreach(string id in new[]{"key7","key8"}){var save=DevCheckpoints.Play(id,"Tester",1);var back=new SliceFlow();Check(back.Restore(JsonUtility.FromJson<SaveData>(JsonUtility.ToJson(save)))&&back.Keys==(id=="key7"?7:8)&&back.KeysSpent==(id=="key7"?6:7),"Corridor: DEV "+id+" uses real model calls and JSON roundtrip");Check(back.SignFacts(7).Any(row=>row[0]==CorridorCopy.Ruler&&row[1]=="Mars")&&back.SignFacts(7).Any(row=>row[0]==CorridorCopy.ModernRuler&&row[1]=="Pluto"),"Corridor: sign journal rulers unlocked at Key 7");}
            var old=DevCheckpoints.Play("obswhole","Old",1);old.version=6;old.corridor=null;var migrated=new SliceFlow();Check(migrated.Restore(JsonUtility.FromJson<SaveData>(JsonUtility.ToJson(old)))&&migrated.Keys==6&&migrated.KeysSpent==6&&!migrated.Corridor.Progress.doorSeen&&migrated.Deck.Items.Take(96).Select(i=>JsonUtility.ToJson(i)).SequenceEqual(old.deck.Take(96).Select(i=>JsonUtility.ToJson(i)))&&JsonUtility.ToJson(migrated.Observatory.Progress)==JsonUtility.ToJson(old.observatory)&&JsonUtility.ToJson(migrated.Chart)==JsonUtility.ToJson(old.chart),"Corridor: v6 migration retains every existing deck item, chart and Observatory field");
            foreach(var kind in new[]{ItemKind.TraditionalRuler,ItemKind.ModernRuler}){var save=DevCheckpoints.Play("key8","Review",1);foreach(var item in save.deck){item.entered=item.kind==(int)kind&&item.seat==7;item.state=item.streak=item.dueDay=0;}var review=new SliceFlow();Check(review.Restore(save)&&review.EnterWing()&&review.EnterDial()&&review.EnterPractice()&&review.RulerReview&&review.Sittings==1,"Corridor: ruler cards use existing practice sitting");int ruler=kind==ItemKind.TraditionalRuler?4:9;Check(review.AnswerRulerReview(Array.IndexOf(review.RulerReviewOptions,ruler))&&review.PracticeDone&&review.Deck.Item(7,kind).State==ItemState.Practicing,"Corridor: ruler review records independent evidence");}
            Check(PracticeBook.Data.concepts.Skip(7).Take(2).Select(cpt=>cpt.unlock).SequenceEqual(new[]{"key7","key8"})&&PracticeBook.Data.concepts.Skip(7).Take(2).All(cpt=>cpt.questions.Length==6&&cpt.questions.All(q=>q.why.Length>0)),"Corridor: two six-question Practice concepts with explanations");
        }
    }
}
