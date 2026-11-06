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
        RectTransform corRoom,corInstrument,corArchPanel;RoomKit corKit,corDoorKit;
        Button corBack,corNext,corSeal,corAsk,corLeft,corRight,corArch;
        string CorridorMilestoneLine=>Flow.KeysSpent>=8?CorridorCopy.End:Flow.Keys>=8?CorridorCopy.Hub8:Flow.KeysSpent>=7?CorridorCopy.Spent7:Flow.Keys>=7?CorridorCopy.Hub7:null;
        string corGreeting="";bool corGreetingSet;Image corWheel;
        Text corPrompt,corMessage,corRoomLine,corRulers;Image corRoomWheel;
        readonly Text[] corPlaquePlanets=new Text[7],corPlaqueSigns=new Text[7],corModernPlaques=new Text[3];
        readonly Button[] corSigns=new Button[12],corTray=new Button[10],corCards=new Button[4];
        readonly Text[] corPlaced=new Text[12],corModernPlaced=new Text[12];
        bool corPlaying,corDoorBeat;int corLastKey;
        CorridorModel Cor=>Flow.Corridor;
        Button CorButton(Transform p,string label,string id,float x,float y,float w,float h,Action action){var b=ObsButton(p,label,x,y,w,h,action);b.name=id;return b;}
        void BuildCorridor()
        {
            corLastKey=Flow.Keys;
            corRoom=ScreenPanel("Corridor","cor-room");var light=LightOverlay(corRoom,"cor-light");lightOverlays.Remove(light);
            corKit=BuildKit(corRoom,"cor-",new[]{new KitPlacement("shell",0,800,1),new KitPlacement("lectern",110,470,1,.25f),new KitPlacement("cabinet",-105,440,2,.4f)},"cor-grime",light,new[]{1f,.7f,.4f,.2f,0},new[]{.5f,.4f,.25f,.1f,0},new[]{0f,.25f,.5f,.75f,1f},()=>Flow.CorridorLevel,p=>Flow.CorridorLevel>=p.Key);
            FinishKit(corRoom,corKit);
            var back=Picture(corRoom,"door-open",-138,300,48,140);back.name="walk:observatory-door";Tappable(back.rectTransform,()=>Walk("observatory-door"));
            corRoomWheel=Picture(corRoom,"cor-wheel",40,340,190,190);corRoomWheel.name="walk:rulership-wheel";Tappable(corRoomWheel.rectTransform,()=>Walk("rulership-wheel"));
            var stem=Rect("Wheel pedestal",corRoom,40,475,24,80);stem.gameObject.AddComponent<Image>().color=new Color(.45f,.31f,.12f);Frame(stem,new Color(.16f,.1f,.08f));
            var foot=Rect("Wheel pedestal base",corRoom,40,515,60,16);foot.gameObject.AddComponent<Image>().color=new Color(.55f,.38f,.15f);Frame(foot,new Color(.16f,.1f,.08f));
            float[] wallX={-144,144,-93,93,-55,55,-26},wallY={345,345,370,370,390,390,405};
            for(int i=0;i<7;i++){int size=i<2?18:i<4?14:11;corPlaquePlanets[i]=Label(corRoom,"",wallX[i]-8,wallY[i],24,28,size);PutPlanet(corPlaquePlanets[i],i,size);corPlaqueSigns[i]=Label(corRoom,string.Concat(Rulership.Traditional[i].Select(s=>Zodiac.Seats[s].Glyph)),wallX[i]+10,wallY[i],40,28,size);corPlaqueSigns[i].font=Dial.GlyphFont;}
            for(int i=0;i<3;i++){int planet=i+7,traditional=Rulership.TraditionalRuler(Rulership.ModernSigns[i]);corModernPlaques[i]=Label(corRoom,"",wallX[traditional],wallY[traditional]+17,38,25,10);PutPlanet(corModernPlaques[i],planet,10);}
            var sealedDoor=Picture(corRoom,"door-sealed",0,218,40,90);sealedDoor.name=CorridorCopy.Sealed;
            corRoomLine=Label(corRoom,"",0,610,310,140,16);corRoomLine.resizeTextForBestFit=true;corRoomLine.resizeTextMinSize=13;corRoomLine.resizeTextMaxSize=16;
            CorButton(corRoom,CorridorCopy.Journal,"cor-journal",0,715,280,48,OpenJournal);
            // The arch is added to the Observatory only after its approved Key 6 threshold.
            corArchPanel=Rect("Corridor arch kit",obsRoom,0,400,360,800);
            corDoorKit=new RoomKit{Prefix="akit-",Container=corArchPanel,Placements=new KitPlacement[0],Grime=new[]{0f},Veil=new[]{0f},Level=()=>0,RestoredNow=p=>false,DoorUnlocked=id=>Flow.CorridorOpen};
            AddDoor(corDoorKit,"corridor-door",CorridorCopy.Title.ToUpperInvariant(),116,280,54,112,170,112);
            corArch=CorButton(obsRoom,CorridorCopy.Title,"cor-door",116,215,112,52,()=>Walk("corridor-door"));corArch.GetComponentInChildren<Text>().text="";corArch.GetComponent<Image>().color=Color.clear;
            corInstrument=ScreenPanel("Rulership wheel","cor-room");
            Label(corInstrument,CorridorCopy.Title,0,64,320,42,23);
            corPrompt=Label(corInstrument,"",0,119,316,70,17);corPrompt.resizeTextForBestFit=true;corPrompt.resizeTextMinSize=13;corPrompt.resizeTextMaxSize=17;
            corWheel=Picture(corInstrument,"cor-wheel",0,324,310,310);
            for(int i=0;i<12;i++){
                int sign=i;float a=i*Mathf.PI*2/12;
                corSigns[i]=CorButton(corInstrument,Zodiac.Seats[i].Name,"cor-sign:"+i,Mathf.Sin(a)*133,324-Mathf.Cos(a)*133,62,44,()=>CorChange(()=>Cor.Place(sign)));
                corSigns[i].GetComponentInChildren<Text>().fontSize=12;
                corPlaced[i]=Label(corSigns[i].transform,"",-10,33,28,25,20);corModernPlaced[i]=Label(corSigns[i].transform,"",14,33,28,25,20);
            }
            corRulers=Label(corInstrument,"",0,330,145,75,14);
            for(int i=0;i<10;i++){
                int planet=i;corTray[i]=CorButton(corInstrument,"","cor-planet:"+i,-132+(i%7)*44,510+(i/7)*48,44,44,()=>{});PutPlanet(corTray[i].GetComponentInChildren<Text>(),i,30);
                var trigger=corTray[i].gameObject.AddComponent<EventTrigger>();
                var end=new EventTrigger.Entry{eventID=EventTriggerType.EndDrag};end.callback.AddListener(d=>{if(busy||Cor.Planet!=planet)return;var data=(PointerEventData)d;for(int n=0;n<12;n++)if(RectTransformUtility.RectangleContainsScreenPoint(corSigns[n].GetComponent<RectTransform>(),data.position,null)){int sign=n;CorChange(()=>Cor.Place(sign));break;}ShowCorridor();});trigger.triggers.Add(end);
                var drag=new EventTrigger.Entry{eventID=EventTriggerType.Drag};drag.callback.AddListener(d=>{if(busy||Cor.Planet!=planet||Cor.AwaitingNext)return;var data=(PointerEventData)d;Vector2 point;if(RectTransformUtility.ScreenPointToLocalPointInRectangle(corInstrument,data.position,null,out point))corTray[planet].GetComponent<RectTransform>().anchoredPosition=new Vector2(point.x,point.y-400);});trigger.triggers.Add(drag);
            }
            for(int i=0;i<4;i++){int slot=i;corCards[i]=CorButton(corInstrument,"","cor-card:"+i,0,235+i*66,312,60,()=>CorCard(slot));var text=corCards[i].GetComponentInChildren<Text>();text.resizeTextForBestFit=true;text.resizeTextMinSize=12;text.resizeTextMaxSize=16;}
            corLeft=CorButton(corInstrument,"<","cor-left",-90,494,52,44,()=>CorChange(()=>Cor.Rotate(-1)));
            corRight=CorButton(corInstrument,">","cor-right",90,494,52,44,()=>CorChange(()=>Cor.Rotate(1)));
            corSeal=CorButton(corInstrument,CorridorCopy.Seal,"cor-seal",0,545,130,44,()=>CorChange(()=>Cor.Seal()));
            corAsk=CorButton(corInstrument,CorridorCopy.Ask,"cor-ask",0,590,150,44,()=>CorChange(()=>Cor.Ask()));
            var panel=Rect("Corridor teaching",corInstrument,0,691,328,145);panel.gameObject.AddComponent<Image>().color=PanelColor;
            corMessage=Label(panel,"",0,68,306,130,15);corMessage.resizeTextForBestFit=true;corMessage.resizeTextMinSize=12;corMessage.resizeTextMaxSize=15;
            corBack=CorButton(corInstrument,CorridorCopy.Back,"cor-back",-90,774,120,44,CorClose);
            corNext=CorButton(corInstrument,CorridorCopy.Continue,"cor-next",80,774,140,44,()=>CorChange(()=>Cor.Continue()));
            Cor.Changed+=Save;corRoom.gameObject.SetActive(false);corInstrument.gameObject.SetActive(false);
        }
        void CorStart(bool fromWalk=false){if(!Flow.AtCorridor||busy&&!fromWalk)return;corPlaying=Cor.Begin();CorRefresh();}
        void CorChange(Func<bool> action){if(busy)return;action();CorRefresh();}
        void CorRefresh(){Save();Show();Publish();if(Flow.Keys>corLastKey&&Flow.Keys>=7&&Flow.Keys<=8){corLastKey=Flow.Keys;StartCoroutine(CorKeyCeremony(Flow.Keys));}}
        IEnumerator CorKeyCeremony(int key){var parent=gridKey.parent;gridKeyLabel.text="Keeper Key "+key;gridKey.SetParent(corInstrument,false);yield return KeyCeremony(key);gridKey.SetParent(parent,false);gridKeyLabel.text="Keeper Key 3";}
        void CorClose(){if(busy)return;if(Flow.RulerReview){LeavePractice();return;}corPlaying=false;Show();Publish();}
        void CorCard(int slot){if(busy)return;if(Flow.RulerReview){var task=Flow.CurrentReview;Flow.AnswerRulerReview(slot);corMessage.text=Flow.Note;Save();Publish();if(task.done)StartCoroutine(AfterTap());}else CorChange(()=>Cor.AnswerCard(slot));}
        void ShowCorridor()
        {
            if(corRoom==null)return;bool room=Flow.AtCorridor&&!corPlaying,review=Flow.RulerReview;
            corArchPanel.gameObject.SetActive(Flow.AtObservatory&&!obsPlaying&&Flow.KeysSpent>=6);if(corArchPanel.gameObject.activeSelf)ApplyKit(corDoorKit,true);
            corArch.gameObject.SetActive(Flow.AtObservatory&&!obsPlaying&&Flow.KeysSpent>=6);corArch.interactable=Flow.CorridorOpen&&!busy;
            corRoom.gameObject.SetActive(room);corInstrument.gameObject.SetActive(Flow.AtCorridor&&corPlaying||review);
            if(room){ApplyKit(corKit,true);foreach(var text in corPlaquePlanets.Concat(corPlaqueSigns))text.color=Flow.Keys>=7?Gilt:new Color(.12f,.14f,.2f);foreach(var text in corModernPlaques)text.gameObject.SetActive(Flow.Keys>=8);if(!corGreetingSet){corGreetingSet=true;corGreeting=Cor.Progress.firstSeen?"":Cor.Line(CorridorCopy.First);Cor.Progress.firstSeen=true;Save();}corRoomLine.text=corGreeting;}
            if(!Flow.AtCorridor){corGreetingSet=false;corGreeting="";}
            if(!corPlaying||!Flow.AtCorridor)corLastKey=Flow.Keys;
            if(!corInstrument.gameObject.activeSelf)return;
            bool traditional=!review&&Cor.Phase==CorridorPhase.Traditional,modern=!review&&Cor.Phase==CorridorPhase.Modern,detriment=!review&&Cor.Phase==CorridorPhase.Detriment,cards=!review&&Cor.Phase==CorridorPhase.Cards;
            corPrompt.text=review?Flow.RulerReviewPrompt:Cor.Prompt;corMessage.text=review?Flow.Note:Cor.Message;
            corWheel.rectTransform.localRotation=Quaternion.Euler(0,0,detriment?Cor.Turn*30:0);
            corRulers.text=traditional||modern?Planets.Sentence(Cor.Planet)+(modern?"\n"+CorridorCopy.Modern:""):detriment?Zodiac.Seats[Cor.Turn].Name:"";
            for(int i=0;i<12;i++){
                float angle=(i-(detriment?Cor.Turn:0))*Mathf.PI*2/12;corSigns[i].GetComponent<RectTransform>().anchoredPosition=new Vector2(Mathf.Sin(angle)*133,-(324-Mathf.Cos(angle)*133));
                var b=corSigns[i];b.gameObject.SetActive(traditional||modern||detriment);b.interactable=!busy&&!Cor.AwaitingNext&&(traditional||modern);
                b.GetComponent<Image>().color=detriment&&Cor.Turn==i?Gilt:Dim;
                corPlaced[i].gameObject.SetActive(Cor.Progress.traditional[i]);if(Cor.Progress.traditional[i])PutPlanet(corPlaced[i],Rulership.TraditionalRuler(i),18);
                int m=Rulership.ModernRuler(i);bool placed=m>=7&&Cor.Progress.modern[m-7];corModernPlaced[i].gameObject.SetActive(placed);if(placed)PutPlanet(corModernPlaced[i],m,18);
            }
            for(int i=0;i<10;i++){corTray[i].GetComponent<RectTransform>().anchoredPosition=new Vector2(-132+(i%7)*44,-(510+(i/7)*48));corTray[i].gameObject.SetActive(traditional&&i<7||modern&&i>=7);corTray[i].interactable=!busy&&!Cor.AwaitingNext&&Cor.Planet==i;}
            var choices=review?Flow.RulerReviewChoices:cards?Cor.CardChoices:new string[0];
            for(int i=0;i<4;i++){corCards[i].gameObject.SetActive(review||cards);corCards[i].GetComponentInChildren<Text>().text=choices.Length>i?choices[i]:"";corCards[i].interactable=!busy&&(review||!Cor.AwaitingNext);}
            corLeft.gameObject.SetActive(detriment);corRight.gameObject.SetActive(detriment);corSeal.gameObject.SetActive(detriment);corAsk.gameObject.SetActive(detriment&&Cor.Hint<2&&!Cor.AwaitingNext);
            corLeft.interactable=corRight.interactable=!busy&&!Cor.AwaitingNext;corSeal.interactable=!busy&&!Cor.AwaitingNext&&(Cor.Hint<3||Cor.Turn==Cor.DetrimentAnswer);
            corNext.gameObject.SetActive(!review&&Cor.CanContinue);corNext.interactable=!busy;
        }
        void UpdateCorridor(){if(!busy&&!corDoorBeat&&Flow.AtObservatory&&!obsPlaying&&Flow.KeysSpent>=6&&!Cor.Progress.doorSeen)StartCoroutine(CorridorDoorBeat());}
        IEnumerator CorridorDoorBeat(){corDoorBeat=true;busy=true;Publish();yield return new WaitForSecondsRealtime(ReducedMotion?0:1f);Flow.RevealCorridorDoor();Save();Show();obsRoomLine.text=Cor.Line(CorridorCopy.Door);Publish();yield return new WaitForSecondsRealtime(ReducedMotion?1.5f:5f);corDoorBeat=false;busy=false;Show();Publish();}
        bool CorAction(string command)
        {
            if(command=="cor-start"){CorStart();return true;}if(command=="cor-back"){CorClose();return true;}if(command=="cor-next"){CorChange(()=>Cor.Continue());return true;}if(command=="cor-left"||command=="cor-right"){CorChange(()=>Cor.Rotate(command=="cor-left"?-1:1));return true;}if(command=="cor-seal"){CorChange(()=>Cor.Seal());return true;}if(command=="cor-ask"){CorChange(()=>Cor.Ask());return true;}if(command=="cor-door"){Walk("corridor-door");return true;}if(command=="cor-journal"){OpenJournal();return true;}
            var bits=command.Split(':');if(bits.Length!=2||!int.TryParse(bits[1],out int n))return false;
            if(bits[0]=="cor-sign"){CorChange(()=>Cor.Place(n));return true;}if(bits[0]=="cor-card"){CorCard(n);return true;}if(bits[0]=="cor-planet")return true;return false;
        }
        void FillCorridor(DialView.WebState s)
        {
            s.corridorPrompt=Cor.Prompt;s.corridorMessage=Cor.Message;s.corridorPlanet=Cor.Planet;s.corridorRuledSign=Cor.RuledSign;s.corridor=Flow.AtCorridor;s.corridorOpen=Flow.CorridorOpen;s.corridorPlaying=Flow.AtCorridor&&corPlaying;s.corridorPhase=Cor.Phase.ToString().ToLowerInvariant();s.corridorTarget=Cor.Target;s.corridorTurn=Cor.Turn;s.corridorHint=Cor.Hint;s.corridorNext=Cor.CanContinue;s.corridorRevealed=Cor.Reveal;s.corridorEvidence7=Cor.Progress.placementEvidence;s.corridorEvidence8=Cor.Progress.detrimentEvidence;s.corridorTraditional=Cor.Progress.traditional;s.corridorModern=Cor.Progress.modern;s.corridorDetriments=Cor.Progress.detriments;s.corridorCards=Cor.Progress.cards;
            var controls=(corInstrument.gameObject.activeInHierarchy?corInstrument.GetComponentsInChildren<Button>():corRoom.gameObject.activeInHierarchy?corRoom.GetComponentsInChildren<Button>():corArch.gameObject.activeInHierarchy?new[]{corArch}:new Button[0]).Where(b=>b.gameObject.activeInHierarchy&&b.interactable&&!busy).ToArray();
            s.corridorControlIds=controls.Select(b=>b.name).ToArray();s.corridorControlLabels=controls.Select(b=>b==corArch?CorridorCopy.Title:b.name=="walk:rulership-wheel"?CorridorCopy.Wheel:b.name=="walk:observatory-door"?CorridorCopy.BackToObservatory:b.GetComponentInChildren<Text>()!=null?b.GetComponentInChildren<Text>().text:b.name).ToArray();s.corridorControls=controls.SelectMany(PageBox).ToArray();
            if(Flow.AtCorridor||Flow.RulerReview)s.caspar=Flow.AtCorridor&&!corPlaying?corRoomLine.text:corPrompt.text+" "+corMessage.text;
        }
    }
}
