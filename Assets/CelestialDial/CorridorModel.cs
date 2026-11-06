using System;
using System.Linq;
namespace Ascendant.CelestialDial
{
    public static class Rulership
    {
        // Planet seats are the shipped Observatory's order; sign seats are Zodiac order.
        public static readonly int[][] Traditional = { new[]{3},new[]{4},new[]{2,5},new[]{1,6},new[]{0,7},new[]{8,11},new[]{9,10} };
        public static readonly int[] ModernSigns = {10,11,7};
        public static int TraditionalRuler(int sign) => Array.FindIndex(Traditional, pair => pair.Contains(Zodiac.Wrap(sign)));
        public static int ModernRuler(int sign) {int n=Array.IndexOf(ModernSigns,Zodiac.Wrap(sign));return n<0?-1:n+7;}
        public static string Signs(int planet) => string.Join(" and ", (planet<7?Traditional[planet]:new[]{ModernSigns[planet-7]}).Select(s=>Zodiac.Seats[s].Name));
        public static readonly int[] ExplainSigns = {2,1,7};
        public static string Explain(int sign, int variant=0)
        {
            int ruler=TraditionalRuler(sign);int elementSign=Zodiac.Wrap(sign+(variant==2?1:0));int modalitySign=Zodiac.Wrap(sign+(variant==3?1:0));
            if(variant==1)ruler=(ruler+1)%7;
            return CorridorCopy.ExplainLine.Replace("{planet}",Planets.Sentence(ruler)).Replace("{work}",Planets.All[ruler].Work).Replace("{element}",Zodiac.Seats[elementSign].Element).Replace("{modality}",Zodiac.ModalityAt(modalitySign));
        }
        public static string ExplainWhy(int sign)=>CorridorCopy.ExplainWhy.Replace("{sign}",Zodiac.Seats[sign].Name).Replace("{planet}",Planets.Spoken(TraditionalRuler(sign))).Replace("{element}",Zodiac.Seats[sign].Element).Replace("{modality}",Zodiac.ModalityAt(sign));
    }
    public enum CorridorPhase { Dormant, TraditionalIntro, Traditional, Key7, Paused7, ModernIntro, Modern, DetrimentIntro, Detriment, CardsIntro, Cards, Key8, Paused8 }
    [Serializable] public sealed class CorridorSave
    {
        public bool doorSeen,firstSeen,placementEvidence,detrimentEvidence;
        public string lastAddress="";
        public bool[] traditional=new bool[12],modern=new bool[3],detriments=new bool[6];
        public int[] detrimentPlanets=new int[0],detrimentRules=new int[0];
        public int cards;
    }
    public sealed class CorridorModel
    {
        readonly SliceFlow flow;
        public CorridorSave Progress {get;private set;}=new CorridorSave();
        public CorridorPhase Phase {get;private set;}
        public int Target {get;private set;}
        public int Turn {get;private set;}
        public int Hint {get;private set;}
        public int Misses {get;private set;}
        public bool AwaitingNext {get;private set;}
        public bool Reveal {get;private set;}
        public string Message {get;private set;}="";
        public event Action Changed;
        public CorridorModel(SliceFlow f){flow=f;}
        public string Line(string text){string previous=Progress.lastAddress;string line=CorridorCopy.Speak(text,ref previous);Progress.lastAddress=previous;return line;}
        void Commit(){Message=Line(Message);Changed?.Invoke();}
        void Problem(int target){Target=target;Hint=Misses=0;AwaitingNext=Reveal=false;Message="";}
        void Intro(CorridorPhase phase,string text){Phase=phase;Problem(0);Message=text;}
        static bool[] Fit(bool[] a,int count)=>Enumerable.Range(0,count).Select(i=>a!=null&&i<a.Length&&a[i]).ToArray();
        public void Restore(CorridorSave save,int version)
        {
            Progress=version>=7&&save!=null?save:new CorridorSave();Progress.traditional=Fit(Progress.traditional,12);Progress.modern=Fit(Progress.modern,3);Progress.detriments=Fit(Progress.detriments,6);Progress.cards=Math.Max(0,Math.Min(3,Progress.cards));
            if(Progress.detrimentPlanets==null||Progress.detrimentPlanets.Length!=6||Progress.detrimentRules==null||Progress.detrimentRules.Length!=6||Enumerable.Range(0,6).Any(i=>Progress.detrimentPlanets[i]<0||Progress.detrimentPlanets[i]>6||!Rulership.Traditional[Progress.detrimentPlanets[i]].Contains(Progress.detrimentRules[i]))) {Progress.detrimentPlanets=new int[0];Progress.detrimentRules=new int[0];Array.Clear(Progress.detriments,0,6);Progress.detrimentEvidence=false;}
            Phase=CorridorPhase.Dormant;Problem(0);
        }
        void PrepareDetriments()
        {
            if(Progress.detrimentPlanets.Length==6)return;
            var rng=new Random(flow.Sitting+37);Progress.detrimentPlanets=Enumerable.Range(0,7).OrderBy(_=>rng.Next()).Take(6).ToArray();Progress.detrimentRules=Progress.detrimentPlanets.Select(p=>Rulership.Traditional[p][rng.Next(Rulership.Traditional[p].Length)]).ToArray();
        }
        int NextPlanet()=>Array.FindIndex(Rulership.Traditional,pair=>pair.Any(s=>!Progress.traditional[s]));
        public bool Begin()
        {
            if(!flow.CorridorOpen)return false;
            if(flow.Keys>=8){Intro(CorridorPhase.Key8,CorridorCopy.Key8);}
            else if(flow.Keys>=7){if(flow.KeysSpent<7)return false;Intro(CorridorPhase.ModernIntro,CorridorCopy.ModernIntro);PrepareDetriments();}
            else Intro(CorridorPhase.TraditionalIntro,CorridorCopy.TraditionalIntro);
            Commit();return true;
        }
        public bool CanContinue=>AwaitingNext||Phase==CorridorPhase.TraditionalIntro||Phase==CorridorPhase.Paused7||Phase==CorridorPhase.ModernIntro||Phase==CorridorPhase.DetrimentIntro||Phase==CorridorPhase.CardsIntro||Phase==CorridorPhase.Paused8;
        public bool Continue()
        {
            if(AwaitingNext){AwaitingNext=false;Advance();Commit();return true;}
            switch(Phase){
                case CorridorPhase.Paused7:Intro(CorridorPhase.TraditionalIntro,CorridorCopy.TraditionalIntro);break;
                case CorridorPhase.TraditionalIntro:
                    if(Progress.traditional.All(x=>x)&&!Progress.placementEvidence)Array.Clear(Progress.traditional,0,12);
                    Phase=CorridorPhase.Traditional;Problem(NextPlanet());if(Target<0)Advance();break;
                case CorridorPhase.ModernIntro:Phase=CorridorPhase.Modern;Problem(Array.FindIndex(Progress.modern,x=>!x));if(Target<0)Advance();break;
                case CorridorPhase.Paused8:Intro(CorridorPhase.DetrimentIntro,CorridorCopy.DetrimentIntro);break;
                case CorridorPhase.DetrimentIntro:
                    PrepareDetriments();if(Progress.detriments.All(x=>x)&&!Progress.detrimentEvidence){Array.Clear(Progress.detriments,0,6);Progress.cards=0;}
                    Phase=CorridorPhase.Detriment;Problem(Array.FindIndex(Progress.detriments,x=>!x));if(Target<0)Advance();break;
                case CorridorPhase.CardsIntro:Phase=CorridorPhase.Cards;Problem(Progress.cards);if(Target>=3)Finish8();break;
                default:return false;
            }Commit();return true;
        }
        public int Planet=>Phase==CorridorPhase.Modern?Target+7:Phase==CorridorPhase.Detriment&&Target>=0?Progress.detrimentPlanets[Target]:Target;
        public int RuledSign=>Phase==CorridorPhase.Detriment?Progress.detrimentRules[Target]:-1;
        public int DetrimentAnswer=>Zodiac.Opposite(RuledSign);
        public string Prompt=>Phase==CorridorPhase.Traditional?CorridorCopy.Place.Replace("{planet}",Planets.Spoken(Planet)):Phase==CorridorPhase.Modern?CorridorCopy.ModernPlace.Replace("{planet}",Planets.Spoken(Planet)):Phase==CorridorPhase.Detriment?CorridorCopy.DetrimentAsk.Replace("{planet}",Planets.Sentence(Planet)).Replace("{sign}",Zodiac.Seats[RuledSign].Name):Phase==CorridorPhase.Cards?CorridorCopy.ExplainAsk.Replace("{sign}",Zodiac.Seats[Rulership.ExplainSigns[Target]].Name):"";
        public bool Select(int sign){if(AwaitingNext||sign<0||sign>=12||Phase!=CorridorPhase.Traditional&&Phase!=CorridorPhase.Modern&&Phase!=CorridorPhase.Detriment)return false;Turn=sign;Commit();return true;}
        public bool Rotate(int delta)=>Select(Zodiac.Wrap(Turn+delta));
        public bool Place(int sign)
        {
            if(AwaitingNext||sign<0||sign>=12||Phase!=CorridorPhase.Traditional&&Phase!=CorridorPhase.Modern)return false;
            bool traditional=Phase==CorridorPhase.Traditional;
            bool right=traditional?Rulership.Traditional[Target].Contains(sign)&&!Progress.traditional[sign]:Rulership.ModernSigns[Target]==sign;
            if(!right){Hint=++Misses;if(Misses<3){Message=Misses==1?CorridorCopy.Nudge:(traditional?CorridorCopy.Rule:CorridorCopy.ModernRule).Replace("{planet}",Planets.Spoken(Planet)).Replace("{signs}",Rulership.Signs(Planet));Commit();return false;}Reveal=true;Message=CorridorCopy.Placed;sign=traditional?Rulership.Traditional[Target].First(s=>!Progress.traditional[s]):Rulership.ModernSigns[Target];}
            bool evidence=right&&Hint<=1;
            if(traditional){Progress.traditional[sign]=true;Progress.placementEvidence|=evidence;}else Progress.modern[Target]=true;
            flow.Deck.RecordLesson(sign,evidence,flow.Sitting,traditional?ItemKind.TraditionalRuler:ItemKind.ModernRuler);Turn=sign;AwaitingNext=true;Commit();return right;
        }
        public bool Ask(){if(Phase!=CorridorPhase.Detriment||AwaitingNext||Hint>=2)return false;Hint=2;Message=CorridorCopy.DetrimentRule;Commit();return true;}
        public bool Seal()
        {
            if(Phase==CorridorPhase.Traditional||Phase==CorridorPhase.Modern)return Place(Turn);
            if(Phase!=CorridorPhase.Detriment||AwaitingNext||Hint==3&&Turn!=DetrimentAnswer)return false;
            if(Turn==DetrimentAnswer){Progress.detriments[Target]=true;Progress.detrimentEvidence|=Hint<=1;AwaitingNext=true;Commit();return true;}
            Misses++;if(Hint>=2){Hint=3;Reveal=true;Message=CorridorCopy.DetrimentShow.Replace("{sign}",Zodiac.Seats[DetrimentAnswer].Name);}else{Hint=Misses==1?1:2;Message=Hint==1?CorridorCopy.DetrimentNudge:CorridorCopy.DetrimentRule;}Commit();return false;
        }
        public int[] CardOrder=>Enumerable.Range(0,4).OrderBy(n=>(n+Target+1)%4).ToArray();
        public string[] CardChoices=>CardOrder.Select(n=>Rulership.Explain(Rulership.ExplainSigns[Target],n)).ToArray();
        public bool AnswerCard(int slot)
        {
            if(Phase!=CorridorPhase.Cards||AwaitingNext||slot<0||slot>3)return false;
            bool right=CardOrder[slot]==0;Message=Rulership.ExplainWhy(Rulership.ExplainSigns[Target]);if(!right){Hint=++Misses==1?2:3;if(Misses<2){Commit();return false;}Reveal=true;}
            Progress.cards++;AwaitingNext=true;Commit();return right;
        }
        void Finish8(){if(Progress.detrimentEvidence){flow.MarkCorridorKey(8);Intro(CorridorPhase.Key8,CorridorCopy.Key8);}else Intro(CorridorPhase.Paused8,CorridorCopy.Paused8);}
        void Advance()
        {
            if(Phase==CorridorPhase.Traditional){int next=NextPlanet();if(next>=0){Problem(next);return;}if(Progress.placementEvidence){flow.MarkCorridorKey(7);Intro(CorridorPhase.Key7,CorridorCopy.Key7);}else Intro(CorridorPhase.Paused7,CorridorCopy.Paused7);}
            else if(Phase==CorridorPhase.Modern){int next=Array.FindIndex(Progress.modern,x=>!x);if(next>=0){Problem(next);return;}Intro(CorridorPhase.DetrimentIntro,CorridorCopy.DetrimentIntro);}
            else if(Phase==CorridorPhase.Detriment){int next=Array.FindIndex(Progress.detriments,x=>!x);if(next>=0){Problem(next);return;}Intro(CorridorPhase.CardsIntro,CorridorCopy.CardsIntro);}
            else if(Phase==CorridorPhase.Cards){if(Progress.cards<3){Problem(Progress.cards);return;}Finish8();}
        }
    }
}
