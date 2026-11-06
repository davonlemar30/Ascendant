using System;
using System.Linq;
namespace Ascendant.CelestialDial
{
    public sealed partial class SliceFlow
    {
        public bool ObservatoryOpen => WingWhole && Observatory.Progress.doorSeen;
        public bool AtObservatory => Screen==SliceScreen.Observatory;
        public bool ObservatoryWhole => KeysSpent>=6;
        public int ObservatoryLevel=>Keys>=8?4:Keys>=7?3:Keys>=6?2:Keys>=5?1:0;
        public bool RevealObservatoryDoor(){if(!AtHub||!WingWhole||Observatory.Progress.doorSeen)return false;Observatory.Progress.doorSeen=true;return true;}
        public bool EnterObservatory(){if(!AtHub||!ObservatoryOpen)return false;Enter(SliceScreen.Observatory);Walk.Enter(Room.Observatory,"atrium-door");return true;}
        public bool LeaveObservatory(){if(!AtObservatory)return false;Enter(SliceScreen.Hub);Walk.Enter(Room.Atrium,"sealed-left");return true;}
        public void MarkObservatoryKey(int n){if(n==5&&Keys==4||n==6&&Keys==5)Keys=n;}
        public int JournalPlanet {get;private set;}=-1;
        public bool PlanetMet(int seat)=>seat>=0&&seat<10&&Deck.Item(seat,ItemKind.PlanetGlyph).entered;
        public float PlanetInk(int seat)=>new[]{ItemKind.PlanetGlyph,ItemKind.PlanetFunction,ItemKind.PlanetCompany}.Select(k=>Ink(Deck.Item(seat,k))).Max();
        public bool OpenPlanet(int seat){if(!AtJournal||(JournalAt!=JournalView.Planets&&JournalAt!=JournalView.Planet)||!PlanetMet(seat))return false;JournalPlanet=seat;JournalAt=JournalView.Planet;return true;}
        public bool PlanetIndex(){if(!AtJournal||JournalAt!=JournalView.Planet)return false;JournalAt=JournalView.Planets;return true;}
        public bool PlanetReview=>AtPractice&&CurrentReview!=null&&(int)CurrentReview.Mode>=(int)ReviewMode.PlanetGlyph&&(int)CurrentReview.Mode<=(int)ReviewMode.PlanetCompany;
        public int[] PlanetReviewOptions=>PlanetReview?Planets.Options(CurrentReview.seat,Sittings):new int[0];
        public void ClearPlanetReviewNote(){if(PlanetReview)Note="";}
        public bool AnswerPlanetReview(int slot)
        {
            var t=CurrentReview;if(!PlanetReview||t.done||slot<0||slot>3)return false;
            bool company=t.Mode==ReviewMode.PlanetCompany;bool right=company?slot==Planets.All[t.seat].Band:PlanetReviewOptions[slot]==t.seat;
            if(!right){t.misses++;RecordStrike();if(t.misses<2){Note=company?ObservatoryCopy.U22BandNudge:ObservatoryCopy.U21Nudge;return false;}}
            t.done=true;t.correct=right;
            var kind=t.Mode==ReviewMode.PlanetGlyph?ItemKind.PlanetGlyph:t.Mode==ReviewMode.PlanetFunction?ItemKind.PlanetFunction:ItemKind.PlanetCompany;
            Deck.RecordReview(t.seat,right,right&&t.misses<=1,Sitting,kind);
            Note=company?ObservatoryCopy.U22BandReveal.Replace("{Planet}",Planets.Sentence(t.seat)).Replace("{company}",Planets.SpokenCompanies[Planets.All[t.seat].Band]):ObservatoryCopy.U21Reveal.Replace("{planet}",Planets.Spoken(t.seat));
            ReviewIndex++;
            if(ReviewIndex>=ReviewQueue.Count)PracticeSummary=ReviewQueue.Count(x=>x.correct)+" of "+ReviewQueue.Count+" proven at the wheel.";
            return right;
        }
    }
}
