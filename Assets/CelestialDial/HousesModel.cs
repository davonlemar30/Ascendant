using System;
using System.Linq;
namespace Ascendant.CelestialDial
{
    public static class Houses
    {
        public static readonly string[] Angles={HousesCopy.AC,HousesCopy.DC,HousesCopy.MC,HousesCopy.IC};
        public static readonly string[] AngleNames={HousesCopy.AscendantName,HousesCopy.DescendantName,HousesCopy.MidheavenName,HousesCopy.ImumCoeliName};
        public static readonly string[] Points={HousesCopy.East,HousesCopy.West,HousesCopy.Upper,HousesCopy.Lower};
        public static readonly string[] Groups={HousesCopy.Angular,HousesCopy.Succedent,HousesCopy.Cadent};
        public static readonly string[] Topics={HousesCopy.Topic1,HousesCopy.Topic2,HousesCopy.Topic3,HousesCopy.Topic4,HousesCopy.Topic5,HousesCopy.Topic6,HousesCopy.Topic7,HousesCopy.Topic8,HousesCopy.Topic9,HousesCopy.Topic10,HousesCopy.Topic11,HousesCopy.Topic12};
        public static readonly string[] Journal={HousesCopy.Journal1,HousesCopy.Journal2,HousesCopy.Journal3,HousesCopy.Journal4,HousesCopy.Journal5,HousesCopy.Journal6,HousesCopy.Journal7,HousesCopy.Journal8,HousesCopy.Journal9,HousesCopy.Journal10,HousesCopy.Journal11,HousesCopy.Journal12};
        public static readonly int[] AxisOrder={1,7,10,4,2,8,5,11,3,9,6,12};
        public static readonly int[] OverlaySigns={0,6,9};
        public static readonly int[] Requests={7,10,4,2,8,5,11,3,6};
        public static int SignAt(int rising,int house)=>Zodiac.Wrap(rising+house-1);
        public static int HouseOf(int rising,int sign)=>Zodiac.Wrap(sign-rising)+1;
        public static int Group(int house)=>(house-1)%3;
        public static string Ordinal(int n)=>n==1?"1st":n==2?"2nd":n==3?"3rd":n+"th";
        public static string TopicWhy(int house)=>HousesCopy.TopicWhy.Replace("{house}",house.ToString()).Replace("{topics}",Journal[house-1]);
        public static string GroupWhy(int house)=>HousesCopy.GroupWhy.Replace("{house}",house.ToString()).Replace("{group}",Groups[Group(house)].ToLowerInvariant());
    }
    // Historical example computed through the shipped chart/time-zone code; no named resident is implied.
    public sealed class HousesExample
    {
        public readonly BirthChart Chart;
        public readonly double Ascendant,Midheaven;
        public readonly int Rising,McSign,McHouse;
        public HousesExample()
        {
            var place=Places.Find("London, Britain (UK)");
            if(place==null)throw new InvalidOperationException("historical example requires bundled London");
            Chart=BirthChart.Work(1900,1,1,14*60,place);
            var ut=Places.UtOf(Places.LocalToUt(place.Zone,1900,1,1,14*60));
            double jd=Sky.JulianDay(ut.Year,ut.Month,ut.Day,ut.Hour+ut.Minute/60.0);
            Ascendant=Sky.Ascendant(jd,place.Latitude,place.Longitude);Midheaven=Sky.Midheaven(jd,place.Longitude);
            Rising=Chart.Rising;McSign=Sky.SignOf(Midheaven);McHouse=Houses.HouseOf(Rising,McSign);
        }
        public string Why=>HousesCopy.ExampleWhy.Replace("{rising}",Zodiac.Seats[Rising].Name).Replace("{mc}",Zodiac.Seats[McSign].Name).Replace("{house}",McHouse.ToString());
    }
    public enum HousesPhase { Dormant,AnglesIntro,Angles,OverlayIntro,Overlay,Key9,Paused9,HousesIntro,HouseWheel,TopicsIntro,Topics,GroupsIntro,Groups,Example,Distinction,Key10,Paused10,NeedsRising }
    [Serializable] public sealed class HousesSave
    {
        public bool doorSeen,firstSeen,overlayEvidence,houseEvidence,example,distinction;
        public bool[] angles=new bool[4],overlays=new bool[3],houses=new bool[9],topics=new bool[12],groups=new bool[12];
        public int[] ascendants=new int[0];public string lastAddress="";
    }
    public sealed class HousesModel
    {
        readonly SliceFlow flow;
        public HousesSave Progress {get;private set;}=new HousesSave();
        public HousesPhase Phase {get;private set;}
        public int Target {get;private set;}
        public int SelectedAngle {get;private set;}=-1;
        public int Turn {get;private set;}
        public int Hint {get;private set;}
        public int Misses {get;private set;}
        public bool AwaitingNext {get;private set;}
        public bool Reveal {get;private set;}
        public string Message {get;private set;}="";
        HousesExample example;
        public HousesExample Example=>example??(example=new HousesExample());
        public event Action Changed;
        public HousesModel(SliceFlow f){flow=f;}
        public string Line(string text){string previous=Progress.lastAddress;string line=CorridorCopy.Speak(text,ref previous);Progress.lastAddress=previous;return line;}
        void Commit(){Message=Line(Message);Changed?.Invoke();}
        void Problem(int target)
        {
            Target=target;Hint=Misses=0;AwaitingNext=Reveal=false;Message="";
            if(Phase==HousesPhase.Angles){SelectedAngle=target;if(target>=0)flow.Deck.RecordLesson(target,false,flow.Sitting,ItemKind.Angle);}
            if(Phase==HousesPhase.HouseWheel&&target>=0)Turn=Rising;
            if(Phase==HousesPhase.Overlay&&target>=0)Turn=Zodiac.Wrap(Houses.OverlaySigns[target]+2);
            if(Phase==HousesPhase.Topics&&target>=0)flow.Deck.RecordLesson(target,false,flow.Sitting,ItemKind.HouseTopic);
            if(Phase==HousesPhase.Groups&&target>=0)flow.Deck.RecordLesson(target,false,flow.Sitting,ItemKind.HouseGroup);
        }
        void Intro(HousesPhase phase,string copy){Phase=phase;Problem(0);Message=copy;}
        static bool[] Fit(bool[] a,int count)=>Enumerable.Range(0,count).Select(i=>a!=null&&i<a.Length&&a[i]).ToArray();
        public void Restore(HousesSave save,int version)
        {
            Progress=version>=7&&save!=null?save:new HousesSave();Progress.angles=Fit(Progress.angles,4);Progress.overlays=Fit(Progress.overlays,3);Progress.houses=Fit(Progress.houses,9);Progress.topics=Fit(Progress.topics,12);Progress.groups=Fit(Progress.groups,12);
            if(Progress.ascendants==null||Progress.ascendants.Length!=3||Progress.ascendants.Any(n=>n<0||n>11)||Progress.ascendants.Distinct().Count()!=3){Progress.ascendants=new int[0];Array.Clear(Progress.houses,0,9);Progress.houseEvidence=false;}
            Phase=HousesPhase.Dormant;Problem(0);
        }
        bool PrepareAscendants()
        {
            if(flow.RisingSign<0)return false;
            if(Progress.ascendants.Length!=3)Progress.ascendants=new[]{flow.RisingSign,Zodiac.Wrap(flow.RisingSign+4),Zodiac.Wrap(flow.RisingSign+8)};
            return true;
        }
        public bool Begin()
        {
            if(!flow.HousesOpen)return false;
            if(flow.Keys>=10)Intro(HousesPhase.Key10,HousesCopy.Key10);
            else if(flow.Keys>=9){if(flow.KeysSpent<9)return false;if(!PrepareAscendants())Intro(HousesPhase.NeedsRising,HousesCopy.NeedRising);else Intro(HousesPhase.HousesIntro,HousesCopy.HousesIntro);}
            else Intro(HousesPhase.AnglesIntro,HousesCopy.AnglesIntro);
            Commit();return true;
        }
        public bool CanContinue=>AwaitingNext||Phase==HousesPhase.AnglesIntro||Phase==HousesPhase.OverlayIntro||Phase==HousesPhase.Paused9||Phase==HousesPhase.HousesIntro||Phase==HousesPhase.TopicsIntro||Phase==HousesPhase.GroupsIntro||Phase==HousesPhase.Paused10;
        public bool Continue()
        {
            if(AwaitingNext){AwaitingNext=false;Advance();Commit();return true;}
            switch(Phase){
                case HousesPhase.AnglesIntro:Phase=HousesPhase.Angles;Problem(Array.FindIndex(Progress.angles,x=>!x));if(Target<0)Advance();break;
                case HousesPhase.Paused9:Intro(HousesPhase.OverlayIntro,HousesCopy.OverlayIntro);break;
                case HousesPhase.OverlayIntro:if(Progress.overlays.All(x=>x)&&!Progress.overlayEvidence)Array.Clear(Progress.overlays,0,3);Phase=HousesPhase.Overlay;Problem(Array.FindIndex(Progress.overlays,x=>!x));if(Target<0)Advance();break;
                case HousesPhase.Paused10:Intro(HousesPhase.HousesIntro,HousesCopy.HousesIntro);break;
                case HousesPhase.HousesIntro:if(!PrepareAscendants()){Intro(HousesPhase.NeedsRising,HousesCopy.NeedRising);break;}if(Progress.houses.All(x=>x)&&!Progress.houseEvidence)Array.Clear(Progress.houses,0,9);Phase=HousesPhase.HouseWheel;Problem(Array.FindIndex(Progress.houses,x=>!x));if(Target<0)Advance();break;
                case HousesPhase.TopicsIntro:Phase=HousesPhase.Topics;Problem(Houses.AxisOrder.Where(h=>!Progress.topics[h-1]).Select(h=>h-1).DefaultIfEmpty(-1).First());if(Target<0)Advance();break;
                case HousesPhase.GroupsIntro:Phase=HousesPhase.Groups;Problem(Array.FindIndex(Progress.groups,x=>!x));if(Target<0)Advance();break;
                default:return false;
            }Commit();return true;
        }
        public int Rising=>Progress.ascendants.Length==3&&Target>=0&&Target<9?Progress.ascendants[Target/3]:-1;
        public int RequestedHouse=>Houses.Requests[Target];
        public int WheelAnswer=>Phase==HousesPhase.Overlay?Houses.OverlaySigns[Target]:Houses.SignAt(Rising,RequestedHouse);
        public string Prompt=>Phase==HousesPhase.Angles?HousesCopy.AnglesAsk.Replace("{angle}",Houses.Angles[SelectedAngle]):Phase==HousesPhase.Overlay?HousesCopy.OverlayAsk.Replace("{sign}",Zodiac.Seats[WheelAnswer].Name):Phase==HousesPhase.HouseWheel?HousesCopy.HouseAsk.Replace("{house}",Houses.Ordinal(RequestedHouse)):Phase==HousesPhase.Topics?HousesCopy.TopicAsk.Replace("{topic}",Houses.Topics[Target]):Phase==HousesPhase.Groups?HousesCopy.GroupAsk.Replace("{house}",(Target+1).ToString()):Phase==HousesPhase.Example?HousesCopy.ExampleAsk:Phase==HousesPhase.Distinction?HousesCopy.DistinctionAsk:"";
        public bool SelectAngle(int angle){if(Phase!=HousesPhase.Angles||AwaitingNext||angle<0||angle>3||Progress.angles[angle]||Misses>0&&angle!=SelectedAngle)return false;SelectedAngle=angle;Hint=Misses=0;Commit();return true;}
        public bool PlaceAngle(int point)
        {
            if(Phase!=HousesPhase.Angles||SelectedAngle<0||AwaitingNext||point<0||point>3)return false;
            bool right=SelectedAngle==point;
            if(!right){Hint=++Misses;if(Misses<3){Message=Misses==1?HousesCopy.AngleNudge:HousesCopy.AngleRule;Commit();return false;}Reveal=true;Message=HousesCopy.AngleShow;}
            Progress.angles[SelectedAngle]=true;flow.Deck.RecordLesson(SelectedAngle,right&&Hint<=1,flow.Sitting,ItemKind.Angle);AwaitingNext=true;Commit();return right;
        }
        public bool Select(int sign){if(AwaitingNext||sign<0||sign>11||Phase!=HousesPhase.Overlay&&Phase!=HousesPhase.HouseWheel)return false;Turn=sign;Commit();return true;}
        public bool Rotate(int delta)=>Select(Zodiac.Wrap(Turn+delta));
        public bool Ask(){if(Phase!=HousesPhase.Overlay&&Phase!=HousesPhase.HouseWheel||AwaitingNext||Hint>=2)return false;Hint=2;Message=Phase==HousesPhase.Overlay?HousesCopy.OverlayRule:HousesCopy.HouseRule;Commit();return true;}
        public bool Seal()
        {
            if(Phase!=HousesPhase.Overlay&&Phase!=HousesPhase.HouseWheel||AwaitingNext||Hint==3&&Turn!=WheelAnswer)return false;
            if(Turn==WheelAnswer){if(Phase==HousesPhase.Overlay){Progress.overlays[Target]=true;Progress.overlayEvidence|=Hint<=1;}else{Progress.houses[Target]=true;Progress.houseEvidence|=Hint<=1;}AwaitingNext=true;Commit();return true;}
            Misses++;if(Hint>=2){Hint=3;Reveal=true;Message=(Phase==HousesPhase.Overlay?HousesCopy.OverlayShow:HousesCopy.HouseShow).Replace("{sign}",Zodiac.Seats[WheelAnswer].Name).Replace("{house}",Phase==HousesPhase.HouseWheel?Houses.Ordinal(RequestedHouse):"");}else{Hint=Misses==1?1:2;Message=Phase==HousesPhase.Overlay?(Hint==1?HousesCopy.OverlayNudge:HousesCopy.OverlayRule):(Hint==1?HousesCopy.HouseNudge:HousesCopy.HouseRule);}Commit();return false;
        }
        public bool PickHouse(int house)
        {
            if(AwaitingNext||house<1||house>12||Phase!=HousesPhase.Topics&&Phase!=HousesPhase.Example)return false;
            int answer=Phase==HousesPhase.Topics?Target+1:Example.McHouse;bool right=house==answer;
            if(!right){Hint=++Misses;if(Misses<3){Message=Phase==HousesPhase.Topics?(Misses==1?HousesCopy.TopicNudge:Houses.TopicWhy(answer)):Example.Why;Commit();return false;}Reveal=true;}
            if(Phase==HousesPhase.Topics){Progress.topics[Target]=true;flow.Deck.RecordLesson(Target,right&&Hint<=1,flow.Sitting,ItemKind.HouseTopic);Message=Houses.TopicWhy(answer);}else{Progress.example=true;Message=Example.Why;}AwaitingNext=true;Commit();return right;
        }
        public bool PickGroup(int group)
        {
            if(Phase!=HousesPhase.Groups||AwaitingNext||group<0||group>2)return false;bool right=group==Houses.Group(Target+1);
            if(!right){Hint=++Misses;if(Misses<3){Message=Misses==1?HousesCopy.GroupNudge:HousesCopy.GroupsIntro;Commit();return false;}Reveal=true;}
            Progress.groups[Target]=true;flow.Deck.RecordLesson(Target,right&&Hint<=1,flow.Sitting,ItemKind.HouseGroup);Message=Houses.GroupWhy(Target+1);AwaitingNext=true;Commit();return right;
        }
        public int[] CardOrder=>Enumerable.Range(0,4).OrderBy(i=>(i+1)%4).ToArray();
        public string[] DistinctionChoices=>CardOrder.Select(i=>new[]{HousesCopy.DistinctionRight,HousesCopy.DistinctionWrong1,HousesCopy.DistinctionWrong2,HousesCopy.DistinctionWrong3}[i]).ToArray();
        public bool AnswerDistinction(int slot)
        {
            if(Phase!=HousesPhase.Distinction||AwaitingNext||slot<0||slot>3)return false;bool right=CardOrder[slot]==0;Message=HousesCopy.DistinctionWhy;
            if(!right){Hint=++Misses==1?2:3;if(Misses<2){Commit();return false;}Reveal=true;}Progress.distinction=true;AwaitingNext=true;Commit();return right;
        }
        void Finish10(){if(Progress.houseEvidence){flow.MarkHousesKey(10);Intro(HousesPhase.Key10,HousesCopy.Key10);}else Intro(HousesPhase.Paused10,HousesCopy.Paused10);}
        void Advance()
        {
            if(Phase==HousesPhase.Angles){int next=Array.FindIndex(Progress.angles,x=>!x);if(next>=0){Problem(next);return;}Intro(HousesPhase.OverlayIntro,HousesCopy.OverlayIntro);}
            else if(Phase==HousesPhase.Overlay){int next=Array.FindIndex(Progress.overlays,x=>!x);if(next>=0){Problem(next);return;}if(Progress.overlayEvidence){flow.MarkHousesKey(9);Intro(HousesPhase.Key9,HousesCopy.Key9);}else Intro(HousesPhase.Paused9,HousesCopy.Paused9);}
            else if(Phase==HousesPhase.HouseWheel){int next=Array.FindIndex(Progress.houses,x=>!x);if(next>=0){Problem(next);return;}Intro(HousesPhase.TopicsIntro,HousesCopy.TopicsIntro);}
            else if(Phase==HousesPhase.Topics){int next=Houses.AxisOrder.Where(h=>!Progress.topics[h-1]).Select(h=>h-1).DefaultIfEmpty(-1).First();if(next>=0){Problem(next);return;}Intro(HousesPhase.GroupsIntro,HousesCopy.GroupsIntro);}
            else if(Phase==HousesPhase.Groups){int next=Array.FindIndex(Progress.groups,x=>!x);if(next>=0){Problem(next);return;}if(!Progress.example){Phase=HousesPhase.Example;Problem(0);Message=HousesCopy.Example;}else if(!Progress.distinction){Phase=HousesPhase.Distinction;Problem(0);}else Finish10();}
            else if(Phase==HousesPhase.Example){if(!Progress.distinction){Phase=HousesPhase.Distinction;Problem(0);}else Finish10();}
            else if(Phase==HousesPhase.Distinction)Finish10();
        }
    }
}
