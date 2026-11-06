using System.Linq;
namespace Ascendant.CelestialDial
{
    public sealed partial class SliceFlow
    {
        public bool RulerReview=>AtPractice&&CurrentReview!=null&&(CurrentReview.Mode==ReviewMode.TraditionalRuler||CurrentReview.Mode==ReviewMode.ModernRuler);
        int ReviewRuler=>CurrentReview.Mode==ReviewMode.ModernRuler?Rulership.ModernRuler(CurrentReview.seat):Rulership.TraditionalRuler(CurrentReview.seat);
        public int[] RulerReviewOptions=>new[]{ReviewRuler,(ReviewRuler+1)%10,(ReviewRuler+3)%10,(ReviewRuler+5)%10}.OrderBy(n=>(n+Sitting)%10).ToArray();
        public string RulerReviewPrompt=>Zodiac.Seats[CurrentReview.seat].Name+". "+(CurrentReview.Mode==ReviewMode.ModernRuler?CorridorCopy.ModernRuler:CorridorCopy.Ruler)+"?";
        public string[] RulerReviewChoices=>RulerReviewOptions.Select(Planets.Sentence).ToArray();
        public void ClearRulerReviewNote(){if(RulerReview)Note="";}
        public bool AnswerRulerReview(int slot)
        {
            var task=CurrentReview;if(!RulerReview||task.done||slot<0||slot>3)return false;
            bool right=RulerReviewOptions[slot]==ReviewRuler;
            if(!right){task.misses++;RecordStrike();if(task.misses<2){Note=Corridor.Line(CorridorCopy.Nudge);return false;}}
            task.done=true;task.correct=right;Deck.RecordReview(task.seat,right,right&&task.misses<=1,Sitting,task.Mode==ReviewMode.ModernRuler?ItemKind.ModernRuler:ItemKind.TraditionalRuler);
            Note=Corridor.Line((task.Mode==ReviewMode.ModernRuler?CorridorCopy.ModernRule:CorridorCopy.Rule).Replace("{planet}",Planets.Spoken(ReviewRuler)).Replace("{signs}",Rulership.Signs(ReviewRuler)));
            ReviewIndex++;if(ReviewIndex>=ReviewQueue.Count)PracticeSummary=ReviewQueue.Count(x=>x.correct)+" of "+ReviewQueue.Count+" proven at the wheel.";return right;
        }
        public bool AtCorridor=>Screen==SliceScreen.Corridor;
        public bool CorridorOpen=>KeysSpent>=6&&Corridor.Progress.doorSeen;
        public int CorridorLevel=>Keys>=8?4:Keys>=7?2:0;
        public bool RevealCorridorDoor(){if(!AtObservatory||KeysSpent<6||Corridor.Progress.doorSeen)return false;Corridor.Progress.doorSeen=true;return true;}
        public bool EnterCorridor(){if(!AtObservatory||!CorridorOpen)return false;Enter(SliceScreen.Corridor);Walk.Enter(Room.Corridor,"observatory-door");return true;}
        public bool LeaveCorridor(){if(!AtCorridor)return false;Enter(SliceScreen.Observatory);Walk.Enter(Room.Observatory,"corridor-door");return true;}
        public void MarkCorridorKey(int n){if(n==7&&Keys==6||n==8&&Keys==7)Keys=n;}
    }
}
