using System;
using System.Linq;

namespace Ascendant.CelestialDial
{
    public sealed class PlanetFact
    {
        public readonly string Name, Glyph, Work, Company, Pace;
        public readonly int Ring, Band;
        public PlanetFact(string n,string g,string w,int b,int r,string p) { Name=n;Glyph=g;Work=w;Band=b;Company=Planets.Companies[b];Ring=r;Pace=p; }
    }
    public static class Planets
    {
        public static readonly string[] Companies={"the lights","personal","social","outer"};
        public static readonly string[] BandLabels={"The lights","Personal","Social","Outer"};
        public static readonly string[] SpokenCompanies={"the lights","the personal planets","the social pair","the outer three"};
        public static readonly PlanetFact[] All={
            new PlanetFact("Moon","\u263d","to feel",0,1,"about 2½ days"),new PlanetFact("Sun","\u2609","to be",0,2,"about a month"),
            new PlanetFact("Mercury","\u263f","to think",1,2,"about a month (it travels with the Sun)"),new PlanetFact("Venus","\u2640","to value",1,2,"about a month (it travels with the Sun)"),
            new PlanetFact("Mars","\u2642","to act",1,3,"about two months"),new PlanetFact("Jupiter","\u2643","to expand",2,4,"about a year"),
            new PlanetFact("Saturn","\u2644","to structure",2,5,"about 2½ years"),new PlanetFact("Uranus","\u2645","to awaken",3,6,"about 7 years"),
            new PlanetFact("Neptune","\u2646","to dissolve",3,7,"about 14 years"),new PlanetFact("Pluto","\u2bd3","to transform",3,8,"12 to 31 years")};
        public static int Wrap(int n)=>(n%10+10)%10;
        public static string Spoken(int n)=>n==0?"the Moon":n==1?"the Sun":All[n].Name;
        public static string Sentence(int n)=>n==0?"The Moon":n==1?"The Sun":All[n].Name;
        public static int[] Options(int seat,int shift=0)
        {
            int twin=seat==0?1:seat==1?0:seat==2?3:seat==3?2:seat==5?8:seat==8?5:Wrap(seat+1);
            return new[]{seat,twin,Wrap(seat+3),Wrap(seat+6)}.Distinct().Concat(Enumerable.Range(0,10)).Distinct().Take(4).OrderBy(n=>Wrap(n-seat+shift+3)).ToArray();
        }
    }
    public enum ObservatoryPhase { Dormant, NamesIntro, Names, WorkIntro, Work, Key5, Paused5, RingsIntro, Rings, Retro, BandsIntro, Bands, Questions, Key6, Paused6 }
    [Serializable] public sealed class ObservatorySave
    {
        public bool doorSeen, firstSeen, retro, workEvidence, ringEvidence;
        public bool[] named=new bool[10], worked=new bool[10], placed=new bool[10], banded=new bool[10];
        public int question;
    }
    // Pure model. Per-problem misses end after the second reappearance; only levels 0/1 are evidence.
    public sealed class ObservatoryModel
    {
        readonly SliceFlow flow;
        public ObservatorySave Progress {get;private set;}=new ObservatorySave();
        public ObservatoryPhase Phase {get;private set;}
        public int Target {get;private set;}
        public int Selected {get;private set;}=-1;
        public int[] TrayOrder {get;private set;}=Enumerable.Range(0,10).ToArray();
        public int Turn {get;private set;}
        public int Hint {get;private set;}
        public int Misses {get;private set;}
        public bool AwaitingNext {get;private set;}
        public bool Reveal {get;private set;}
        public string Message {get;private set;}="";
        public string CopyId {get;private set;}="ObsFirst";
        public event Action Changed;
        public ObservatoryModel(SliceFlow f) {flow=f;}
        void Say(string id,string text) {CopyId=id;Message=text;}
        void Commit(){Changed?.Invoke();}
        void Problem(int target){Target=target;Misses=Hint=0;Selected=-1;AwaitingNext=Reveal=false;if(Phase==ObservatoryPhase.Names&&target>=0)flow.Deck.RecordLesson(target,false,flow.Sitting,ItemKind.PlanetGlyph);}
        public void Restore(ObservatorySave s,int version)
        {
            Progress=version>=6&&s!=null?s:new ObservatorySave();
            Progress.named=Ten(Progress.named);Progress.worked=Ten(Progress.worked);Progress.placed=Ten(Progress.placed);Progress.banded=Ten(Progress.banded);
            Progress.question=Math.Max(0,Math.Min(2,Progress.question));Phase=ObservatoryPhase.Dormant;Problem(0);
        }
        static bool[] Ten(bool[] a)=>Enumerable.Range(0,10).Select(i=>a!=null&&i<a.Length&&a[i]).ToArray();
        public bool Begin()
        {
            if(!flow.ObservatoryOpen)return false;
            if(flow.Keys>=6){Phase=ObservatoryPhase.Key6;Say("Key6Line",ObservatoryCopy.Key6Line);Commit();return true;}
            if(flow.Keys>=5){if(flow.KeysSpent<5)return false;Phase=ObservatoryPhase.RingsIntro;Say("U22Intro",ObservatoryCopy.U22Intro);}
            else if(Progress.named.All(x=>x)){Phase=ObservatoryPhase.WorkIntro;Say("U21PartB",ObservatoryCopy.U21PartB);}
            else{Phase=ObservatoryPhase.NamesIntro;Say("U21Intro",ObservatoryCopy.U21Intro);}
            Problem(0);Commit();return true;
        }
        int First(bool[] a)=>Array.FindIndex(a,x=>!x);
        public bool Continue()
        {
            if(AwaitingNext){AwaitingNext=false;Message="";Advance();Commit();return true;}
            switch(Phase){
                case ObservatoryPhase.NamesIntro:Phase=ObservatoryPhase.Names;Problem(First(Progress.named));break;
                case ObservatoryPhase.WorkIntro:if(Progress.worked.All(x=>x)&&!Progress.workEvidence)Array.Clear(Progress.worked,0,10);Phase=ObservatoryPhase.Work;Problem(First(Progress.worked));break;
                case ObservatoryPhase.Paused5:Phase=ObservatoryPhase.WorkIntro;Say("U21PartB",ObservatoryCopy.U21PartB);break;
                case ObservatoryPhase.RingsIntro:
                    var random=new Random(flow.Sitting+flow.KeysSpent*17);TrayOrder=Enumerable.Range(0,10).OrderBy(i=>random.Next()).ToArray();
                    if(Progress.placed.All(x=>x)&&!Progress.ringEvidence){Array.Clear(Progress.placed,0,10);Array.Clear(Progress.banded,0,10);Progress.question=0;}
                    Phase=ObservatoryPhase.Rings;Problem(First(Progress.placed));if(Target<0)Advance();break;
                case ObservatoryPhase.Retro:Phase=ObservatoryPhase.BandsIntro;Say("U22Bands",ObservatoryCopy.U22Bands);break;
                case ObservatoryPhase.BandsIntro:Phase=ObservatoryPhase.Bands;Problem(First(Progress.banded));if(Target<0)Advance();break;
                case ObservatoryPhase.Paused6:Phase=ObservatoryPhase.RingsIntro;Say("U22Intro",ObservatoryCopy.U22Intro);break;
                default:return false;
            }Commit();return true;
        }
        public bool CanContinue=>AwaitingNext||Phase==ObservatoryPhase.NamesIntro||Phase==ObservatoryPhase.WorkIntro||Phase==ObservatoryPhase.RingsIntro||Phase==ObservatoryPhase.Retro||Phase==ObservatoryPhase.BandsIntro||Phase==ObservatoryPhase.Paused5||Phase==ObservatoryPhase.Paused6;
        public bool AnswerName(int seat)
        {
            if(Phase!=ObservatoryPhase.Names||AwaitingNext||seat<0||seat>=10)return false;
            bool right=seat==Target;
            if(!right&&++Misses<2){Say("U21Nudge",ObservatoryCopy.U21Nudge);Commit();return false;}
            flow.Deck.RecordLesson(Target,right&&Misses<=1,flow.Sitting,ItemKind.PlanetGlyph);Progress.named[Target]=true;
            Reveal=!right;AwaitingNext=true;if(!right)Say("U21Reveal",ObservatoryCopy.U21Reveal.Replace("{planet}",Planets.Spoken(Target)));Commit();return right;
        }
        public bool Select(int seat){if(AwaitingNext||seat<0||seat>=10)return false;if(Phase==ObservatoryPhase.Work){Turn=seat;Commit();return true;}if(Phase!=ObservatoryPhase.Rings&&Phase!=ObservatoryPhase.Bands)return false;if((Phase==ObservatoryPhase.Rings?Progress.placed:Progress.banded)[seat]||(Misses>0&&Selected!=seat))return false;if(Selected!=seat)Problem(seat);Selected=seat;Commit();return true;}
        public bool Rotate(int delta){return Phase==ObservatoryPhase.Work&&!AwaitingNext&&Select(Planets.Wrap(Turn+delta));}
        public bool Ask(){if(Phase!=ObservatoryPhase.Work||AwaitingNext||Hint>=2)return false;Hint=2;Say("U21BRule",ObservatoryCopy.U21BRule);Commit();return true;}
        public bool Seal()
        {
            if(Phase!=ObservatoryPhase.Work||AwaitingNext||Hint==3&&Turn!=Target)return false;
            if(Turn==Target){Progress.worked[Target]=true;bool evidence=Hint<=1;Progress.workEvidence|=evidence;flow.Deck.RecordLesson(Target,evidence,flow.Sitting,ItemKind.PlanetFunction);AwaitingNext=true;Commit();return true;}
            Misses++;if(Hint>=2){Hint=3;Reveal=true;flow.Deck.RecordLesson(Target,false,flow.Sitting,ItemKind.PlanetFunction);Say("U21BShow",ObservatoryCopy.U21BShow.Replace("{verb}",Planets.All[Target].Work.Substring(3)).Replace("{planet}",Planets.Spoken(Target)));}
            else if(Misses==1){Hint=1;Say("U21BNudge",ObservatoryCopy.U21BNudge);}else{Hint=2;Say("U21BRule",ObservatoryCopy.U21BRule);}Commit();return false;
        }
        public bool Ring(int ring)
        {
            if(Phase!=ObservatoryPhase.Rings||Selected<0||AwaitingNext||ring<1||ring>8)return false;
            int s=Selected;bool right=ring==Planets.All[s].Ring;
            if(!right){Misses++;Hint=Misses;if(Misses<3){Say(Misses==1?"U22Nudge":"U22Rule",Misses==1?ObservatoryCopy.U22Nudge:ObservatoryCopy.U22Rule);Commit();return false;}Reveal=true;Say("U22Show",ObservatoryCopy.U22Show);}
            bool eligible=right&&Hint<=1;Progress.ringEvidence|=eligible;Progress.placed[s]=true;AwaitingNext=true;Commit();return right;
        }
        public bool Band(int band)
        {
            if(Phase!=ObservatoryPhase.Bands||Selected<0||AwaitingNext||band<0||band>3)return false;
            int s=Selected;bool right=band==Planets.All[s].Band;
            if(!right&&++Misses<2){Hint=1;Say("U22BandNudge",ObservatoryCopy.U22BandNudge);Commit();return false;}
            Progress.banded[s]=true;flow.Deck.RecordLesson(s,right&&Misses<=1,flow.Sitting,ItemKind.PlanetCompany);
            Reveal=!right;AwaitingNext=true;if(!right)Say("U22BandReveal",ObservatoryCopy.U22BandReveal.Replace("{Planet}",Planets.Sentence(s)).Replace("{company}",Planets.SpokenCompanies[Planets.All[s].Band]));Commit();return right;
        }
        public PracticeQuestion Question=>PracticeBook.Data.concepts.First(c=>c.id=="pace").questions[Progress.question==0?4:3];
        public int[] QuestionOrder=>Enumerable.Range(0,4).OrderBy(n=>(n+Misses+Progress.question+1)%4).ToArray();
        public bool AnswerQuestion(int originalChoice)
        {
            if(Phase!=ObservatoryPhase.Questions||AwaitingNext||originalChoice<0||originalChoice>3)return false;
            bool right=originalChoice==Question.answer;
            if(!right){Misses++;Message=Question.why;if(Misses<2){Commit();return false;}Reveal=true;}
            AwaitingNext=true;Message=Question.why;Commit();return right;
        }
        void Advance()
        {
            if(Phase==ObservatoryPhase.Names){int n=First(Progress.named);if(n>=0){Problem(n);return;}Phase=ObservatoryPhase.WorkIntro;Say("U21PartB",ObservatoryCopy.U21PartB);}
            else if(Phase==ObservatoryPhase.Work){int n=First(Progress.worked);if(n>=0){Problem(n);return;}if(Progress.workEvidence){flow.MarkObservatoryKey(5);Phase=ObservatoryPhase.Key5;Say("Key5Line",ObservatoryCopy.Key5Line);}else{Phase=ObservatoryPhase.Paused5;Say("Key5Paused",ObservatoryCopy.Key5Paused);}}
            else if(Phase==ObservatoryPhase.Rings){int n=First(Progress.placed);if(n>=0){Problem(n);return;}Progress.retro=true;Phase=ObservatoryPhase.Retro;Say("Retro",ObservatoryCopy.Retro);}
            else if(Phase==ObservatoryPhase.Bands){int n=First(Progress.banded);if(n>=0){Problem(n);return;}Phase=ObservatoryPhase.Questions;Problem(0);}
            else if(Phase==ObservatoryPhase.Questions){Progress.question++;if(Progress.question<2){Problem(0);return;}if(Progress.ringEvidence){flow.MarkObservatoryKey(6);Phase=ObservatoryPhase.Key6;Say("Key6Line",ObservatoryCopy.Key6Line);}else{Phase=ObservatoryPhase.Paused6;Say("Key6Paused",ObservatoryCopy.Key6Paused);}}
        }
    }
}
