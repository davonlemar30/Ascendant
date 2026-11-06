using System;
using System.Linq;
using UnityEngine;
using Ascendant.CelestialDial;
namespace Ascendant.Build
{
    public static partial class GreyboxValidation
    {
        static SliceFlow HousesReady()
        {
            var f=new SliceFlow();Check(f.Restore(DevCheckpoints.Play("key8","Tester",1)),"Houses: restore Key 8 in hand");
            Check(!f.HousesOpen&&f.EnterChamber()&&f.SpendKey()&&f.LeaveChamber()&&f.RevealHousesDoor()&&f.HousesOpen&&f.EnterHouses(),"Houses: next Atrium door opens after Key 8 spent");return f;
        }
        static void AnglesAndOverlay(SliceFlow f,bool assisted)
        {
            var h=f.Houses;Check(h.Begin()&&h.Continue(),"Houses: local sky begins with angles");
            for(int i=0;i<4;i++){h.SelectAngle(i);if(assisted){int wrong=(i+1)%4;h.PlaceAngle(wrong);Check(h.Hint==1&&!h.AwaitingNext,"Houses: first angle miss nudges");h.PlaceAngle(wrong);Check(h.Hint==2&&!h.AwaitingNext,"Houses: second angle miss gives rule");h.PlaceAngle(wrong);Check(h.Hint==3&&h.Reveal&&h.Progress.angles[i],"Houses: third angle miss places label, two reappearances");}else Check(h.PlaceAngle(i),"Houses: angle "+Houses.Angles[i]);h.Continue();}
            h.Continue();for(int i=0;i<3;i++){if(assisted)h.Ask();h.Select(Houses.OverlaySigns[i]);Check(h.Seal(),"Houses: named rising overlay "+i);h.Continue();}
        }
        static void HouseAssignmentsAndCards(SliceFlow f,bool assisted)
        {
            var h=f.Houses;Check(f.LeaveHouses()&&f.EnterChamber()&&f.SpendKey()&&f.KeysSpent==9&&f.BooksOpen==3&&f.LeaveChamber()&&f.EnterHouses()&&h.Begin()&&h.Continue(),"Houses: spending Key 9 opens Book 3 and house assignment");
            Check(h.Progress.ascendants.Distinct().Count()==3&&h.Progress.ascendants[0]==f.RisingSign,"Houses: one own rising and two distinct others");
            for(int i=0;i<9;i++){Check(h.Rising==h.Progress.ascendants[i/3]&&h.RequestedHouse==Houses.Requests[i],"Houses: three problems per Ascendant "+i);if(assisted)h.Ask();h.Select(Houses.SignAt(h.Rising,h.RequestedHouse));Check(h.Seal(),"Houses: house answer "+i);h.Continue();}
            h.Continue();foreach(int house in new[]{1,7,10,4,2,8,5,11,3,9,6,12}){Check(h.Target==house-1,"Houses: topic axis order "+house);Check(h.PickHouse(house),"Houses: topic matches house "+house);h.Continue();}
            h.Continue();for(int house=1;house<=12;house++){Check(h.PickGroup((house-1)%3),"Houses: group "+house);h.Continue();}
            Check(h.Phase==HousesPhase.Example&&h.Example.McHouse==9&&h.Example.Rising==2&&h.Example.McSign==10,"Houses: computed historical MC in ninth whole-sign house");
            Check(h.PickHouse(h.Example.McHouse)&&h.Continue()&&h.AnswerDistinction(Array.IndexOf(h.CardOrder,0))&&h.Continue(),"Houses: worked example and house/sign distinction finish");
        }
        static void ValidateHouses()
        {
            for(int rising=0;rising<12;rising++)for(int house=1;house<=12;house++)Check(Houses.SignAt(rising,house)==(rising+house-1)%12&&Houses.HouseOf(rising,Houses.SignAt(rising,house))==house,"Houses: whole-sign identity "+rising+"/"+house);
            Check(Houses.AxisOrder.SequenceEqual(new[]{1,7,10,4,2,8,5,11,3,9,6,12}),"Houses: exact canon axis-pair order");
            Check(Houses.Topics.SequenceEqual(new[]{"self","possessions","communication","home","creativity","daily work","partners","shared resources","beliefs","career","friends","retreat"}),"Houses: all twelve review topics, including shared resources and retreat");
            Check(Enumerable.Range(1,12).Select(Houses.Group).SequenceEqual(new[]{0,1,2,0,1,2,0,1,2,0,1,2}),"Houses: angular, succedent and cadent groups");
            Check(Houses.Angles.SequenceEqual(new[]{"AC","DC","MC","IC"})&&Houses.Points.SequenceEqual(new[]{"Eastern horizon","Western horizon","Upper meridian","Lower meridian"}),"Houses: conventional east-left west-right upper-top lower-bottom labels");
            double jd=Sky.JulianDay(1900,1,1,14);foreach(int quadrant in new[]{0,90,180,270}){double mc=Sky.Midheaven(jd,quadrant-Sky.Gmst(jd));Check(Math.Abs(mc-quadrant)<.000001,"Houses: MC coordinate quadrant "+quadrant);}
            var example=new HousesExample();Check(example.Rising==2&&example.McSign==10&&example.McHouse==9&&example.McSign!=Houses.SignAt(example.Rising,10)&&Math.Abs(example.Ascendant-73.270994)<.001&&Math.Abs(example.Midheaven-308.210586)<.001,"Houses: historical date/place computed through game chart and MC code, outside tenth sign");
            var f=HousesReady();AnglesAndOverlay(f,false);Check(f.Keys==9&&f.Houses.Progress.overlayEvidence,"Houses: eligible overlay earns Key 9");HouseAssignmentsAndCards(f,false);Check(f.Keys==10&&f.Houses.Progress.houseEvidence,"Houses: eligible house answer earns Key 10");Check(f.LeaveHouses()&&f.EnterChamber()&&f.SpendKey()&&f.KeysSpent==10&&f.BooksOpen==3&&f.LeaveChamber(),"Houses: lock 10 begins Book 4, three Books remain open");
            var no=HousesReady();AnglesAndOverlay(no,true);Check(no.Keys==8&&no.Houses.Phase==HousesPhase.Paused9&&!no.Houses.Progress.overlayEvidence,"Houses: all assisted overlays pause Key 9");no.Houses.Continue();no.Houses.Continue();for(int i=0;i<3;i++){no.Houses.Select(no.Houses.WheelAnswer);no.Houses.Seal();no.Houses.Continue();}Check(no.Keys==9,"Houses: independent overlay replay earns Key 9");HouseAssignmentsAndCards(no,true);Check(no.Keys==9&&no.Houses.Phase==HousesPhase.Paused10&&!no.Houses.Progress.houseEvidence,"Houses: all assisted house answers pause Key 10");
            no.Houses.Continue();no.Houses.Continue();for(int i=0;i<9;i++){no.Houses.Select(no.Houses.WheelAnswer);no.Houses.Seal();no.Houses.Continue();}no.Houses.Continue();no.Houses.Continue();Check(no.Keys==10,"Houses: independent house replay earns permanent Key 10 without replaying completed cards");
            var ladder=HousesReady();AnglesAndOverlay(ladder,false);ladder.LeaveHouses();ladder.EnterChamber();ladder.SpendKey();ladder.LeaveChamber();ladder.EnterHouses();var h=ladder.Houses;h.Begin();h.Continue();h.Select(Zodiac.Wrap(h.WheelAnswer+1));h.Seal();Check(h.Hint==1,"Houses: first house miss level 1");h.Seal();Check(h.Hint==2,"Houses: second house miss level 2");h.Seal();Check(h.Hint==3&&h.Reveal,"Houses: third house miss shows reconstruction");int misses=h.Misses;h.Seal();Check(h.Misses==misses,"Houses: reconstruction has no extra failed encounters");h.Select(h.WheelAnswer);h.Seal();Check(!h.Progress.houseEvidence,"Houses: reconstruction never counts as evidence");
            foreach(string id in new[]{"key9","key10","houseswhole"}){var save=DevCheckpoints.Play(id,"Tester",1);var back=new SliceFlow();Check(back.Restore(JsonUtility.FromJson<SaveData>(JsonUtility.ToJson(save)))&&back.Keys==(id=="key9"?9:10)&&back.KeysSpent==(id=="key9"?8:id=="key10"?9:10)&&back.BooksOpen==(id=="key9"?2:3),"Houses: DEV "+id+" via model calls and save roundtrip");Check(back.OpenJournal()&&back.JournalLand()&&back.JournalToContents()&&back.OpenChapter("houses")&&back.JournalAt==JournalView.Houses,"Houses: chapter opens after Key 9");}
            var old=DevCheckpoints.Play("obswhole","Old",1);old.version=6;old.corridor=null;old.houses=null;var migrated=new SliceFlow();Check(migrated.Restore(JsonUtility.FromJson<SaveData>(JsonUtility.ToJson(old)))&&migrated.Keys==old.keys&&migrated.KeysSpent==old.locksFilled&&!migrated.Houses.Progress.doorSeen&&migrated.Deck.Items.Take(96).Select(i=>JsonUtility.ToJson(i)).SequenceEqual(old.deck.Take(96).Select(i=>JsonUtility.ToJson(i)))&&JsonUtility.ToJson(migrated.Observatory.Progress)==JsonUtility.ToJson(old.observatory)&&JsonUtility.ToJson(migrated.Chart)==JsonUtility.ToJson(old.chart),"Houses: v6 migration preserves every prior deck item, chart and Observatory field");
            foreach(var kind in new[]{ItemKind.HouseTopic,ItemKind.HouseGroup,ItemKind.Angle}){var save=DevCheckpoints.Play("key10","Review",1);foreach(var item in save.deck){item.entered=item.kind==(int)kind&&item.seat==2;item.state=item.streak=item.dueDay=0;}var review=new SliceFlow();Check(review.Restore(save)&&review.EnterWing()&&review.EnterDial()&&review.EnterPractice()&&review.HousesReview&&review.Sittings==1,"Houses: atomic card in existing Dial sitting "+kind);int answer=kind==ItemKind.HouseGroup?2:2;Check(review.AnswerHousesReview(Array.IndexOf(review.HousesReviewOptions,answer))&&review.PracticeDone&&review.Deck.Item(2,kind).State==ItemState.Practicing,"Houses: atomic independent review evidence "+kind);}
            Check(PracticeBook.Data.concepts.Skip(9).Take(2).Select(c=>c.unlock).SequenceEqual(new[]{"key9","key10"})&&PracticeBook.Data.concepts.Skip(9).Take(2).All(c=>c.questions.Length==6&&c.questions.All(q=>q.why.Length>0)),"Houses: two concepts with six explained questions each");
        }
    }
}
