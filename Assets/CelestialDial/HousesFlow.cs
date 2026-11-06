using System;
using System.Linq;
namespace Ascendant.CelestialDial
{
    public sealed partial class SliceFlow
    {
        public int JournalHouse {get;private set;}=-1;
        public bool HouseMet(int house)=>house>=1&&house<=12&&Deck.Item(house-1,ItemKind.HouseTopic).entered;
        public bool OpenHouse(int house){if(!AtJournal||Keys<9||JournalAt!=JournalView.Houses&&JournalAt!=JournalView.House||!HouseMet(house))return false;JournalHouse=house;JournalAt=JournalView.House;return true;}
        public bool HouseIndex(){if(!AtJournal||JournalAt!=JournalView.House)return false;JournalAt=JournalView.Houses;return true;}
        public bool AtHouses=>Screen==SliceScreen.Houses;
        public bool HousesOpen=>KeysSpent>=8&&Houses.Progress.doorSeen;
        public int HousesLevel=>Keys>=10?4:Keys>=9?2:0;
        public bool RevealHousesDoor(){if(!AtHub||KeysSpent<8||Houses.Progress.doorSeen)return false;Houses.Progress.doorSeen=true;return true;}
        public bool EnterHouses(){if(!AtHub||!HousesOpen)return false;Enter(SliceScreen.Houses);Walk.Enter(Room.Houses,"atrium-door");return true;}
        public bool LeaveHouses(){if(!AtHouses)return false;Enter(SliceScreen.Hub);Walk.Enter(Room.Atrium,"houses-door");return true;}
        public void MarkHousesKey(int n){if(n==9&&Keys==8||n==10&&Keys==9)Keys=n;}
        public bool HousesReview=>AtPractice&&CurrentReview!=null&&(CurrentReview.Mode==ReviewMode.HouseTopic||CurrentReview.Mode==ReviewMode.HouseGroup||CurrentReview.Mode==ReviewMode.Angle);
        public string HousesReviewPrompt=>CurrentReview.Mode==ReviewMode.Angle?Ascendant.CelestialDial.Houses.AngleNames[CurrentReview.seat]:CurrentReview.Mode==ReviewMode.HouseGroup?HousesCopy.GroupAsk.Replace("{house}",(CurrentReview.seat+1).ToString()):HousesCopy.TopicAsk.Replace("{topic}",Ascendant.CelestialDial.Houses.Topics[CurrentReview.seat]);
        public int[] HousesReviewOptions=>CurrentReview.Mode==ReviewMode.HouseTopic?new[]{CurrentReview.seat,(CurrentReview.seat+3)%12,(CurrentReview.seat+6)%12,(CurrentReview.seat+9)%12}.OrderBy(i=>(i+Sitting)%12).ToArray():CurrentReview.Mode==ReviewMode.HouseGroup?new[]{0,1,2}:new[]{0,1,2,3};
        public string[] HousesReviewChoices=>CurrentReview.Mode==ReviewMode.HouseTopic?HousesReviewOptions.Select(i=>(i+1).ToString()).ToArray():CurrentReview.Mode==ReviewMode.HouseGroup?Ascendant.CelestialDial.Houses.Groups:Ascendant.CelestialDial.Houses.Points;
        public void ClearHousesReviewNote(){if(HousesReview)Note="";}
        public bool AnswerHousesReview(int slot)
        {
            var t=CurrentReview;if(!HousesReview||t.done||slot<0||slot>=HousesReviewOptions.Length)return false;
            bool group=t.Mode==ReviewMode.HouseGroup;int answer=group?Ascendant.CelestialDial.Houses.Group(t.seat+1):t.seat;
            bool right=HousesReviewOptions[slot]==answer;
            if(!right){t.misses++;RecordStrike();if(t.misses<2){Note=Houses.Line(t.Mode==ReviewMode.Angle?HousesCopy.AngleNudge:group?HousesCopy.GroupNudge:HousesCopy.TopicNudge);return false;}}
            t.done=true;t.correct=right;Deck.RecordReview(t.seat,right,right&&t.misses<=1,Sitting,t.Mode==ReviewMode.Angle?ItemKind.Angle:group?ItemKind.HouseGroup:ItemKind.HouseTopic);
            Note=Houses.Line(t.Mode==ReviewMode.Angle?HousesCopy.AngleRule:group?Ascendant.CelestialDial.Houses.GroupWhy(t.seat+1):Ascendant.CelestialDial.Houses.TopicWhy(t.seat+1));
            ReviewIndex++;if(ReviewIndex>=ReviewQueue.Count)PracticeSummary=ReviewQueue.Count(x=>x.correct)+" of "+ReviewQueue.Count+" proven at the wheel.";return right;
        }
    }
}
