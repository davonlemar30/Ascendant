using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
namespace Ascendant.CelestialDial
{
    public sealed partial class SliceView
    {
        RectTransform hohRoom,hohInstrument,hohJournal,hohArchPanel;
        RoomKit hohKit,hohDoorKit;Image hohWheel,hohMc,hohMcRay;
        Text hohPrompt,hohMessage,hohReadout,hohRoomLine,hohDetails,hohJournalNumber,hohMcLabel;
        readonly Text[] hohSignLabels=new Text[12],hohHouseNumbers=new Text[12];
        readonly Button[] hohAngles=new Button[4],hohPoints=new Button[4],hohNumbers=new Button[12],hohGroups=new Button[3],hohCards=new Button[4],hohHousePages=new Button[12];
        Button hohNext,hohBack,hohLeft,hohRight,hohSeal,hohAsk,hohArch,hohJournalBack,hohNeedsJournal;
        GameObject[] hohSealedChapterParts,hohChapterParts;
        bool hohPlaying,hohDoorBeat,hohGreetingSet;string hohGreeting="";int hohLastKey;
        HousesModel Hoh=>Flow.Houses;
        string HousesMilestoneLine=>Flow.KeysSpent>=10?HousesCopy.End:Flow.Keys>=10?HousesCopy.Hub10:Flow.KeysSpent>=9?HousesCopy.Book3:Flow.Keys>=9?HousesCopy.Hub9:null;
        static readonly Vector2[] HohPoints={new Vector2(-143,324),new Vector2(143,324),new Vector2(0,181),new Vector2(0,467)};
        Image HohLine(Transform p,string name,Vector2 a,Vector2 b,Color colour)
        {
            var line=Rect(name,p,(a.x+b.x)/2,(a.y+b.y)/2,Vector2.Distance(a,b),2).gameObject.AddComponent<Image>();line.color=colour;line.raycastTarget=false;line.rectTransform.localRotation=Quaternion.Euler(0,0,-Mathf.Atan2(b.y-a.y,b.x-a.x)*Mathf.Rad2Deg);return line;
        }
        void BuildHouses()
        {
            hohLastKey=Flow.Keys;
            hohRoom=ScreenPanel("Hall of Houses","hoh-room");var light=LightOverlay(hohRoom,"hoh-light");lightOverlays.Remove(light);
            hohKit=BuildKit(hohRoom,"hoh-",new[]{new KitPlacement("shell",0,800,1),new KitPlacement("lectern",-100,470,1,.25f),new KitPlacement("bench",112,490,2,.4f)},"hoh-grime",light,new[]{1f,.7f,.4f,.2f,0},new[]{.5f,.4f,.25f,.1f,0},new[]{0f,.25f,.5f,.75f,1f},()=>Flow.HousesLevel,p=>Flow.HousesLevel>=p.Key);FinishKit(hohRoom,hohKit);
            var back=Picture(hohRoom,"door-open",-138,305,48,140);back.name="walk:atrium-door";Tappable(back.rectTransform,()=>Walk("atrium-door"));
            var instrument=Picture(hohRoom,"hoh-instrument",40,355,195,195);instrument.name="walk:local-sky";Tappable(instrument.rectTransform,()=>Walk("local-sky"));
            var stem=Rect("Local sky pedestal",hohRoom,40,494,26,84);stem.gameObject.AddComponent<Image>().color=new Color(.45f,.31f,.12f);Frame(stem,new Color(.16f,.1f,.08f));
            var foot=Rect("Local sky base",hohRoom,40,538,64,16);foot.gameObject.AddComponent<Image>().color=new Color(.55f,.38f,.15f);Frame(foot,new Color(.16f,.1f,.08f));
            hohRoomLine=Label(hohRoom,"",0,610,310,140,16);hohRoomLine.resizeTextForBestFit=true;hohRoomLine.resizeTextMinSize=13;hohRoomLine.resizeTextMaxSize=16;
            CorButton(hohRoom,HousesCopy.Journal,"hoh-journal",0,715,280,48,OpenJournal);
            // The Atrium kit has only its three shipped doors. The brief authorizes this next door on the right wall.
            hohArchPanel=Rect("Hall door kit",hub,0,400,360,800);
            hohDoorKit=new RoomKit{Prefix="akit-",Container=hohArchPanel,Placements=new KitPlacement[0],Grime=new[]{0f},Veil=new[]{0f},Level=()=>0,RestoredNow=p=>false,DoorUnlocked=id=>Flow.HousesOpen};
            AddDoor(hohDoorKit,"houses-door",HousesCopy.Title.ToUpperInvariant(),143,512,48,104,410,108);
            hohArch=CorButton(hub,"","hoh-door",143,460,60,114,()=>Walk("houses-door"));hohArch.GetComponent<Image>().color=Color.clear;
            hohInstrument=ScreenPanel("Local sky","hoh-room");Label(hohInstrument,HousesCopy.Title,0,64,320,42,23);
            hohPrompt=Label(hohInstrument,"",0,119,314,70,17);hohPrompt.resizeTextForBestFit=true;hohPrompt.resizeTextMinSize=13;hohPrompt.resizeTextMaxSize=17;
            hohWheel=Picture(hohInstrument,"hoh-instrument",0,324,310,310);
            HohLine(hohInstrument,"Horizon",new Vector2(-143,324),new Vector2(143,324),Gilt);HohLine(hohInstrument,"Meridian",new Vector2(0,181),new Vector2(0,467),Gilt);
            for(int i=0;i<12;i++){
                hohSignLabels[i]=Label(hohInstrument,Zodiac.Seats[i].Glyph,0,0,34,32,25);hohSignLabels[i].font=Dial.GlyphFont;hohSignLabels[i].horizontalOverflow=HorizontalWrapMode.Overflow;hohSignLabels[i].verticalOverflow=VerticalWrapMode.Overflow;hohSignLabels[i].color=Gilt;
                hohHouseNumbers[i]=Label(hohInstrument,(i+1).ToString(),0,0,25,23,13);hohHouseNumbers[i].color=SilverInk;
                int house=i+1;hohNumbers[i]=CorButton(hohInstrument,house.ToString(),"hoh-house:"+house,-114+(i%4)*76,500+(i/4)*48,70,44,()=>HohChange(()=>Hoh.PickHouse(house)));
                hohHousePages[i]=null;
            }
            for(int i=0;i<4;i++){
                int point=i;var at=HohPoints[i];hohPoints[i]=CorButton(hohInstrument,Houses.Points[i],"hoh-point:"+i,at.x,at.y,66,44,()=>HohChange(()=>Hoh.PlaceAngle(point)));hohPoints[i].GetComponentInChildren<Text>().fontSize=11;
                hohAngles[i]=CorButton(hohInstrument,Houses.Angles[i],"hoh-angle:"+i,-99+i*66,526,60,44,()=>HohChange(()=>Hoh.SelectAngle(point)));
                var trigger=hohAngles[i].gameObject.AddComponent<EventTrigger>();var begin=new EventTrigger.Entry{eventID=EventTriggerType.BeginDrag};begin.callback.AddListener(_=>{if(!busy)Hoh.SelectAngle(point);});trigger.triggers.Add(begin);var drag=new EventTrigger.Entry{eventID=EventTriggerType.Drag};drag.callback.AddListener(d=>{if(busy||Hoh.Phase!=HousesPhase.Angles||Hoh.SelectedAngle!=point)return;Vector2 at;if(RectTransformUtility.ScreenPointToLocalPointInRectangle(hohInstrument,((PointerEventData)d).position,null,out at))hohAngles[point].GetComponent<RectTransform>().anchoredPosition=new Vector2(at.x,at.y-400);});trigger.triggers.Add(drag);var end=new EventTrigger.Entry{eventID=EventTriggerType.EndDrag};end.callback.AddListener(d=>{if(busy)return;for(int n=0;n<4;n++)if(RectTransformUtility.RectangleContainsScreenPoint(hohPoints[n].GetComponent<RectTransform>(),((PointerEventData)d).position,null)){int destination=n;HohChange(()=>Hoh.PlaceAngle(destination));break;}ShowHouses();});trigger.triggers.Add(end);
                hohCards[i]=CorButton(hohInstrument,"","hoh-card:"+i,0,230+i*66,312,60,()=>HohCard(point));var t=hohCards[i].GetComponentInChildren<Text>();t.resizeTextForBestFit=true;t.resizeTextMinSize=12;t.resizeTextMaxSize=16;
            }
            for(int i=0;i<3;i++){int group=i;hohGroups[i]=CorButton(hohInstrument,Houses.Groups[i],"hoh-group:"+i,-106+i*106,540,100,44,()=>HohChange(()=>Hoh.PickGroup(group)));hohGroups[i].GetComponentInChildren<Text>().fontSize=14;}
            hohReadout=Label(hohInstrument,"",0,325,180,80,15);hohReadout.resizeTextForBestFit=true;hohReadout.resizeTextMinSize=12;hohReadout.resizeTextMaxSize=15;
            hohMc=Rect("MC point",hohInstrument,0,0,10,10).gameObject.AddComponent<Image>();hohMc.color=Gilt;hohMc.raycastTarget=false;hohMc.rectTransform.localRotation=Quaternion.Euler(0,0,45);hohMcLabel=Label(hohInstrument,HousesCopy.MC,0,0,32,24,13);hohMcLabel.color=Gilt;hohMcRay=HohLine(hohInstrument,"MC connector",new Vector2(0,171),new Vector2(0,220),Gilt);
            hohLeft=CorButton(hohInstrument,"<","hoh-left",-90,506,52,44,()=>HohChange(()=>Hoh.Rotate(-1)));hohRight=CorButton(hohInstrument,">","hoh-right",90,506,52,44,()=>HohChange(()=>Hoh.Rotate(1)));
            hohSeal=CorButton(hohInstrument,HousesCopy.Seal,"hoh-seal",0,555,130,44,()=>HohChange(()=>Hoh.Seal()));hohAsk=CorButton(hohInstrument,HousesCopy.Ask,"hoh-ask",0,600,150,44,()=>HohChange(()=>Hoh.Ask()));
            var panel=Rect("Hall teaching",hohInstrument,0,695,328,137);panel.gameObject.AddComponent<Image>().color=PanelColor;hohMessage=Label(panel,"",0,65,306,125,15);hohMessage.resizeTextForBestFit=true;hohMessage.resizeTextMinSize=12;hohMessage.resizeTextMaxSize=15;
            hohNext=CorButton(hohInstrument,HousesCopy.Continue,"hoh-next",80,774,140,44,()=>HohChange(()=>Hoh.Continue()));hohBack=CorButton(hohInstrument,HousesCopy.Back,"hoh-back",-90,774,120,44,HohClose);
            hohNeedsJournal=CorButton(hohInstrument,HousesCopy.Journal,"hoh-journal",0,525,240,48,OpenJournal);
            BuildHouseJournal();Hoh.Changed+=Save;hohRoom.gameObject.SetActive(false);hohInstrument.gameObject.SetActive(false);
        }
        void HohStart(bool fromWalk=false){if(!Flow.AtHouses||busy&&!fromWalk)return;hohPlaying=Hoh.Begin();HohRefresh();}
        void HohChange(Func<bool> action){if(busy)return;action();HohRefresh();}
        void HohRefresh(){Save();Show();Publish();if(Flow.Keys>hohLastKey&&Flow.Keys>=9&&Flow.Keys<=10){hohLastKey=Flow.Keys;StartCoroutine(HohKeyCeremony(Flow.Keys));}}
        IEnumerator HohKeyCeremony(int key){var parent=gridKey.parent;gridKeyLabel.text="Keeper Key "+key;gridKey.SetParent(hohInstrument,false);yield return KeyCeremony(key);gridKey.SetParent(parent,false);gridKeyLabel.text="Keeper Key 3";}
        void HohClose(){if(busy)return;if(Flow.HousesReview){LeavePractice();return;}hohPlaying=false;Show();Publish();}
        void HohCard(int slot){if(busy)return;if(Flow.HousesReview){var t=Flow.CurrentReview;Flow.AnswerHousesReview(slot);hohMessage.text=Flow.Note;Save();Publish();if(t.done)StartCoroutine(AfterTap());}else HohChange(()=>Hoh.AnswerDistinction(slot));}
        void ShowHouses()
        {
            if(hohRoom==null)return;bool room=Flow.AtHouses&&!hohPlaying,review=Flow.HousesReview;
            hohArchPanel.gameObject.SetActive(Flow.AtHub&&Flow.KeysSpent>=8);if(hohArchPanel.gameObject.activeSelf)ApplyKit(hohDoorKit,true);hohArch.gameObject.SetActive(Flow.AtHub&&Flow.KeysSpent>=8);hohArch.interactable=Flow.HousesOpen&&!busy;
            hohRoom.gameObject.SetActive(room);hohInstrument.gameObject.SetActive(Flow.AtHouses&&hohPlaying||review);
            if(room){ApplyKit(hohKit,true);if(!hohGreetingSet){hohGreetingSet=true;hohGreeting=Hoh.Progress.firstSeen?"":Hoh.Line(HousesCopy.First);Hoh.Progress.firstSeen=true;Save();}hohRoomLine.text=Flow.KeysSpent>=10?HousesCopy.End:hohGreeting;}
            if(!Flow.AtHouses){hohGreetingSet=false;hohGreeting="";}if(!Flow.AtHouses||!hohPlaying)hohLastKey=Flow.Keys;
            if(!hohInstrument.gameObject.activeSelf)return;
            bool angles=!review&&Hoh.Phase==HousesPhase.Angles,overlay=!review&&Hoh.Phase==HousesPhase.Overlay,house=!review&&Hoh.Phase==HousesPhase.HouseWheel,topics=!review&&Hoh.Phase==HousesPhase.Topics,groups=!review&&Hoh.Phase==HousesPhase.Groups,example=!review&&Hoh.Phase==HousesPhase.Example,distinction=!review&&Hoh.Phase==HousesPhase.Distinction;
            hohPrompt.text=review?Flow.HousesReviewPrompt:Hoh.Prompt;hohMessage.text=review?Flow.Note:Hoh.Message;
            hohReadout.text=overlay?Zodiac.Seats[Hoh.Turn].Name:house?HousesCopy.Ascendant.Replace("{sign}",Zodiac.Seats[Hoh.Rising].Name)+"\n"+Zodiac.Seats[Hoh.Turn].Name:groups?(Hoh.Target+1).ToString():example?HousesCopy.ExamplePositions.Replace("{ac}",(Hoh.Example.Ascendant%30).ToString("0.00")+"°").Replace("{rising}",Zodiac.Seats[Hoh.Example.Rising].Name).Replace("{mc}",(Hoh.Example.Midheaven%30).ToString("0.00")+"°").Replace("{mcSign}",Zodiac.Seats[Hoh.Example.McSign].Name):"";
            for(int i=0;i<12;i++){
                bool wheel=overlay||house||example;hohSignLabels[i].gameObject.SetActive(wheel);hohHouseNumbers[i].gameObject.SetActive(example);
                double offset=example?i*30+15-Hoh.Example.Ascendant:(i-Hoh.Turn)*30;float a=(float)offset*Mathf.Deg2Rad;var pos=new Vector2(-Mathf.Cos(a)*123,324+Mathf.Sin(a)*123);hohSignLabels[i].rectTransform.anchoredPosition=new Vector2(pos.x,-pos.y);
                if(example){int n=Houses.HouseOf(Hoh.Example.Rising,i);hohHouseNumbers[i].text=n.ToString();hohHouseNumbers[i].rectTransform.anchoredPosition=new Vector2(-Mathf.Cos(a)*92,-(324+Mathf.Sin(a)*92));}
                hohNumbers[i].gameObject.SetActive(topics||example);hohNumbers[i].interactable=!busy&&!Hoh.AwaitingNext;
            }
            hohWheel.rectTransform.localRotation=Quaternion.identity;
            for(int i=0;i<4;i++){
                hohAngles[i].GetComponent<RectTransform>().anchoredPosition=new Vector2(-99+i*66,-526);hohAngles[i].gameObject.SetActive(angles&&!Hoh.Progress.angles[i]);hohAngles[i].interactable=!busy&&!Hoh.AwaitingNext&&(Hoh.Misses==0||Hoh.SelectedAngle==i);hohAngles[i].GetComponent<Image>().color=Hoh.SelectedAngle==i?Gilt:Dim;
                hohPoints[i].gameObject.SetActive(angles||overlay||example);hohPoints[i].GetComponentInChildren<Text>().text=angles?Houses.Points[i]+(Hoh.Progress.angles[i]?" "+Houses.Angles[i]:""):Houses.Angles[i];hohPoints[i].interactable=angles&&!busy&&!Hoh.AwaitingNext;
                bool card=review||distinction;var choices=review?Flow.HousesReviewChoices:distinction?Hoh.DistinctionChoices:new string[0];hohCards[i].gameObject.SetActive(card&&i<choices.Length);hohCards[i].interactable=!busy&&(review||!Hoh.AwaitingNext);hohCards[i].GetComponentInChildren<Text>().text=i<choices.Length?choices[i]:"";
            }
            for(int i=0;i<3;i++){hohGroups[i].gameObject.SetActive(groups);hohGroups[i].interactable=!busy&&!Hoh.AwaitingNext;}
            hohMc.gameObject.SetActive(example);hohMcRay.gameObject.SetActive(example);hohMcLabel.gameObject.SetActive(example);
            if(example){float a=(float)(Hoh.Example.Midheaven-Hoh.Example.Ascendant)*Mathf.Deg2Rad;Vector2 end=new Vector2(-Mathf.Cos(a)*135,324+Mathf.Sin(a)*135),start=new Vector2(0,171);hohMc.rectTransform.anchoredPosition=new Vector2(end.x,-end.y);hohMcLabel.rectTransform.anchoredPosition=new Vector2(end.x+20,-end.y-12);hohMcRay.rectTransform.anchoredPosition=new Vector2((start.x+end.x)/2,-(start.y+end.y)/2);hohMcRay.rectTransform.sizeDelta=new Vector2(Vector2.Distance(start,end),2);hohMcRay.rectTransform.localRotation=Quaternion.Euler(0,0,-Mathf.Atan2(end.y-start.y,end.x-start.x)*Mathf.Rad2Deg);}
            hohLeft.gameObject.SetActive(overlay||house);hohRight.gameObject.SetActive(overlay||house);hohSeal.gameObject.SetActive(overlay||house);hohAsk.gameObject.SetActive((overlay||house)&&Hoh.Hint<2&&!Hoh.AwaitingNext);hohLeft.interactable=hohRight.interactable=!busy&&!Hoh.AwaitingNext;hohSeal.interactable=!busy&&!Hoh.AwaitingNext&&(Hoh.Hint<3||Hoh.Turn==Hoh.WheelAnswer);
            hohNext.gameObject.SetActive(!review&&Hoh.CanContinue);hohNext.interactable=!busy;hohNeedsJournal.gameObject.SetActive(!review&&Hoh.Phase==HousesPhase.NeedsRising);
        }
        void UpdateHouses(){if(!busy&&!hohDoorBeat&&Flow.AtHub&&Flow.KeysSpent>=8&&!Hoh.Progress.doorSeen)StartCoroutine(HousesDoorBeat());}
        IEnumerator HousesDoorBeat(){hohDoorBeat=true;busy=true;Publish();yield return new WaitForSecondsRealtime(ReducedMotion?0:1f);Flow.RevealHousesDoor();Save();Show();hubText.text=Hoh.Line(HousesCopy.Door);Publish();yield return new WaitForSecondsRealtime(ReducedMotion?1.5f:5f);hohDoorBeat=false;busy=false;Show();Publish();}
        bool HohAction(string command)
        {
            if(command=="hoh-start"){HohStart();return true;}if(command=="hoh-back"){HohClose();return true;}if(command=="hoh-next"){HohChange(()=>Hoh.Continue());return true;}if(command=="hoh-left"||command=="hoh-right"){HohChange(()=>Hoh.Rotate(command=="hoh-left"?-1:1));return true;}if(command=="hoh-seal"){HohChange(()=>Hoh.Seal());return true;}if(command=="hoh-ask"){HohChange(()=>Hoh.Ask());return true;}if(command=="hoh-door"){Walk("houses-door");return true;}if(command=="hoh-journal"){OpenJournal();return true;}if(command=="house-index"){if(Flow.HouseIndex()){Show();Publish();}return true;}if(command=="journal-chapter:houses"){if(Flow.OpenChapter("houses")){Show();Publish();}return true;}
            var bits=command.Split(':');if(bits.Length!=2||!int.TryParse(bits[1],out int n))return false;switch(bits[0]){case "hoh-angle":HohChange(()=>Hoh.SelectAngle(n));return true;case "hoh-point":HohChange(()=>Hoh.PlaceAngle(n));return true;case "hoh-house":HohChange(()=>Hoh.PickHouse(n));return true;case "hoh-group":HohChange(()=>Hoh.PickGroup(n));return true;case "hoh-card":HohCard(n);return true;case "house-page":if(Flow.OpenHouse(n)){Show();Publish();}return true;}return false;
        }
        void BuildHouseJournal()
        {
            int before=journalContents.childCount;
            hohSealedChapterParts=Enumerable.Range(0,before).Select(i=>journalContents.GetChild(i).gameObject).Where(g=>g.name==SliceFlow.SealedChapter+" (sealed)"||g.name=="Rule"&&Mathf.Abs(g.GetComponent<RectTransform>().anchoredPosition.y+(ChapterTop[3]+ChapterHeight[3]))<.1f).ToArray();
            ChapterRow(3,HousesCopy.Chapter,"","journal-emblem-wheel",new Vector2(48,48),()=>JournalChapter("houses"));journalChapterRows[3].name="journal-chapter:houses";
            hohChapterParts=Enumerable.Range(before,journalContents.childCount-before).Select(i=>journalContents.GetChild(i).gameObject).ToArray();
            hohJournal=Rect("The Houses",journal,0,400,360,800);
            hohDetails=Label(hohJournal,"",JournalX,225,276,230,16);hohDetails.font=Resources.Load<Font>("Fonts/EBGaramond-Bold");hohDetails.color=SilverInk;hohDetails.resizeTextForBestFit=true;hohDetails.resizeTextMinSize=13;hohDetails.resizeTextMaxSize=18;
            hohJournalNumber=Label(hohJournal,"",JournalX,160,100,70,54);hohJournalNumber.color=GiltInk;
            for(int i=0;i<12;i++){int house=i+1;hohHousePages[i]=CorButton(hohJournal,house.ToString(),"house-page:"+house,-70+(i%2)*140,380+(i/2)*48,130,44,()=>{if(Flow.OpenHouse(house)){Show();Publish();}});var t=hohHousePages[i].GetComponentInChildren<Text>();t.resizeTextForBestFit=true;t.resizeTextMinSize=11;t.resizeTextMaxSize=15;}
            hohJournalBack=CorButton(hohJournal,HousesCopy.Chapter,"house-index",JournalX,550,200,44,()=>{if(Flow.HouseIndex()){Show();Publish();}});
        }
        void ShowHouseJournal()
        {
            if(hohJournal==null)return;bool unlocked=Flow.Keys>=9;foreach(var p in hohChapterParts)p.SetActive(unlocked);foreach(var p in hohSealedChapterParts)p.SetActive(!unlocked);
            bool index=Flow.AtJournal&&Flow.JournalAt==JournalView.Houses,page=Flow.AtJournal&&Flow.JournalAt==JournalView.House;hohJournal.gameObject.SetActive(index||page);
            hohJournalNumber.gameObject.SetActive(page);hohJournalBack.gameObject.SetActive(page);hohDetails.rectTransform.anchoredPosition=new Vector2(JournalX,-(page?330:225));
            for(int i=0;i<12;i++){hohHousePages[i].gameObject.SetActive(index);bool met=Flow.HouseMet(i+1);hohHousePages[i].interactable=met&&!busy;hohHousePages[i].GetComponentInChildren<Text>().text=(i+1)+(met?" "+Houses.Topics[i]:"");}
            if(page){int house=Flow.JournalHouse;hohJournalNumber.text=house.ToString();hohDetails.text=Houses.Journal[house-1]+"\n\n"+Houses.Groups[Houses.Group(house)];}else hohDetails.text=HousesCopy.JournalIntro;
        }
        void FillHouses(DialView.WebState s)
        {
            s.houses=Flow.AtHouses;s.housesOpen=Flow.HousesOpen;s.housesPlaying=Flow.AtHouses&&hohPlaying;s.housesPhase=Hoh.Phase.ToString().ToLowerInvariant();s.housesTarget=Hoh.Target;s.housesSelectedAngle=Hoh.SelectedAngle;s.housesTurn=Hoh.Turn;s.housesHint=Hoh.Hint;s.housesNext=Hoh.CanContinue;s.housesRevealed=Hoh.Reveal;s.housesEvidence9=Hoh.Progress.overlayEvidence;s.housesEvidence10=Hoh.Progress.houseEvidence;s.housesAngles=Hoh.Progress.angles;s.housesOverlays=Hoh.Progress.overlays;s.housesAssigned=Hoh.Progress.houses;s.housesTopics=Hoh.Progress.topics;s.housesGroups=Hoh.Progress.groups;s.housesAscendants=Hoh.Progress.ascendants;s.housesRising=Hoh.Phase==HousesPhase.HouseWheel?Hoh.Rising:-1;s.housesRequested=Hoh.Phase==HousesPhase.HouseWheel?Hoh.RequestedHouse:0;s.housesPrompt=Hoh.Prompt;s.housesMessage=Hoh.Message;s.journalHouse=Flow.JournalHouse;
            if(Hoh.Progress.example||Hoh.Phase==HousesPhase.Example){s.housesExampleAsc=Hoh.Example.Ascendant;s.housesExampleMc=Hoh.Example.Midheaven;s.housesExampleRising=Hoh.Example.Rising;s.housesExampleMcSign=Hoh.Example.McSign;s.housesExampleMcHouse=Hoh.Example.McHouse;}
            var controls=(hohInstrument.gameObject.activeInHierarchy?hohInstrument.GetComponentsInChildren<Button>():hohRoom.gameObject.activeInHierarchy?hohRoom.GetComponentsInChildren<Button>():hohJournal.gameObject.activeInHierarchy?hohJournal.GetComponentsInChildren<Button>():hohArch.gameObject.activeInHierarchy?new[]{hohArch}:Flow.AtJournal&&Flow.JournalAt==JournalView.Contents&&Flow.Keys>=9?new[]{journalChapterRows[3]}:new Button[0]).Where(b=>b.gameObject.activeInHierarchy&&b.interactable&&!busy).ToArray();
            s.housesControlIds=controls.Select(b=>b.name).ToArray();s.housesControlLabels=controls.Select(b=>b==hohArch?HousesCopy.Title:b.name=="walk:local-sky"?HousesCopy.Instrument:b.name=="walk:atrium-door"?HousesCopy.BackToAtrium:b.GetComponentInChildren<Text>()!=null?b.GetComponentInChildren<Text>().text:b.name).ToArray();s.housesControls=controls.SelectMany(PageBox).ToArray();
            if(Flow.AtHouses||Flow.HousesReview)s.caspar=Flow.AtHouses&&!hohPlaying?hohRoomLine.text:hohPrompt.text+" "+hohMessage.text;
            if(Flow.AtJournal&&(Flow.JournalAt==JournalView.Houses||Flow.JournalAt==JournalView.House))s.caspar=HousesCopy.Chapter+". "+hohDetails.text;
        }
    }
}
