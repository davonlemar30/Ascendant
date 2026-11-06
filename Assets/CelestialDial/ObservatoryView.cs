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
        public static Vector4 PlanetQuizLayout(bool mark,int asks,int choices,int whys){float first=mark?220:175,top=first+20*(Mathf.Max(1,asks)-1)+24;return new Vector4(first,top,46,top+46*(choices-1)+44+24+18*(Mathf.Max(1,whys)-1)+26);}
        RectTransform obsRoom,obsInstrument,obsJournal;
        RoomKit obsKit; Image obsLight,obsPose; Text obsMessage,obsAsk,obsGlyph,obsTitle,obsDetails,obsJournalGlyph;
        Button obsContinue,obsSeal,obsAskCaspar,obsLeft,obsRight,obsExit,obsJournalBack;
        readonly Button[] obsNames=new Button[4],obsSpheres=new Button[10],obsRingButtons=new Button[8],obsBands=new Button[4],obsPlanetPages=new Button[10];
        readonly Text[] obsSphereGlyphs=new Text[10],obsSphereBands=new Text[10];readonly Image[] obsRoomSpheres=new Image[10];
        bool obsPlaying,obsDoorBeat; int obsLastKey;
        ObservatoryModel Obs=>Flow.Observatory;
        static Font PlanetFont(int seat)=>Resources.Load<Font>(seat==0||seat>1&&seat<9?"Fonts/NotoSansSymbols":"Fonts/NotoSansSymbols2-Regular");
        static void PutPlanet(Text text,int seat,int size=44){text.font=PlanetFont(seat);text.text=Planets.All[seat].Glyph;text.fontSize=size;text.horizontalOverflow=HorizontalWrapMode.Overflow;text.verticalOverflow=VerticalWrapMode.Overflow;}
        Button ObsButton(Transform p,string name,float x,float y,float w,float h,Action a){var b=MakeButton(p,name,x,y,w,h,()=>a());b.GetComponent<Image>().canvasRenderer.cullTransparentMesh=false;return b;}
        void BuildObservatory()
        {
            obsLastKey=Flow.Keys;
            obsRoom=ScreenPanel("Observatory","obs-room");obsLight=LightOverlay(obsRoom,"obs-light");lightOverlays.Remove(obsLight);
            obsKit=BuildKit(obsRoom,"obs-",new[]{new KitPlacement("shell",0,800,1),new KitPlacement("lectern",-100,430,1),new KitPlacement("charts",100,320,2)},"obs-grime",obsLight,new[]{1f,.7f,.4f,.2f,0},new[]{.55f,.4f,.25f,.1f,0},new[]{0f,.3f,.5f,.75f,1f},()=>Flow.ObservatoryLevel,p=>Flow.ObservatoryLevel>=p.Key);
            FinishKit(obsRoom,obsKit);
            var door=Block(obsRoom,"Doorway back",-138,292,48,145,"door-open");Tappable(door,()=>Walk("atrium-door"));
            var orrery=Block(obsRoom,"The orrery",40,300,200,200,"obs-orrery-frame");Tappable(orrery,()=>Walk("orrery"));
            ObsButton(obsRoom,"Your journal",0,710,280,48,OpenJournal);
            Picture(obsRoom,"caspar-explain",110,480,70,125);
            for(int i=0;i<10;i++){float a=i*Mathf.PI*2/10;obsRoomSpheres[i]=Picture(obsRoom,"obs-sphere-"+Planets.All[i].Name.ToLowerInvariant(),40+Mathf.Sin(a)*76,300-Mathf.Cos(a)*76,28,28);}
            for(int i=0;i<3;i++)Picture(obsRoom,"obs-sphere-dark",20+i*20,416,16,16);
            var first=Label(obsRoom,"",0,580,316,180,15); first.font=font;first.resizeTextForBestFit=true;first.resizeTextMinSize=13;first.resizeTextMaxSize=15;obsRoomLine=first;
            obsInstrument=ScreenPanel("Orrery","obs-room");
            obsTitle=Label(obsInstrument,"The Observatory",0,72,280,36,24);
            obsAsk=Label(obsInstrument,"",0,117,312,52,19);obsAsk.resizeTextForBestFit=true;obsAsk.resizeTextMinSize=13;obsAsk.resizeTextMaxSize=19;
            Picture(obsInstrument,"obs-orrery-frame",0,290,300,300);Picture(obsInstrument,"obs-rings",0,290,300,300);Picture(obsInstrument,"bracket",0,145,48,48);
            for(int i=0;i<8;i++){var ring=Rect("Orbit "+(i+1),obsInstrument,0,290,36+26*i,36+26*i).gameObject.AddComponent<Image>();ring.sprite=ChapterRingSprite();ring.color=new Color(Gilt.r,Gilt.g,Gilt.b,.6f);ring.raycastTarget=false;}
            obsGlyph=Label(obsInstrument,"",0,270,100,90,54);
            for(int i=0;i<10;i++){int s=i;obsSpheres[i]=ObsButton(obsInstrument,"",0,0,48,48,()=>ObsChange(()=>Obs.Select(s)));obsSpheres[i].name="obs-sphere:"+i;Slots.Dress(obsSpheres[i].GetComponent<Image>(),"obs-sphere-"+Planets.All[i].Name.ToLowerInvariant());obsSphereGlyphs[i]=obsSpheres[i].GetComponentInChildren<Text>();PutPlanet(obsSphereGlyphs[i],i);obsSphereBands[i]=Label(obsSpheres[i].transform,"",0,56,65,18,10);obsSphereBands[i].color=Gilt;}
            for(int i=0;i<3;i++){var d=Picture(obsInstrument,"obs-sphere-dark",-25+i*25,440,20,20);d.raycastTarget=false;}
            for(int i=0;i<4;i++){int n=i;obsNames[i]=ObsButton(obsInstrument,"",-78+(i%2)*156,440+(i/2)*52,150,48,()=>ObsCard(n));obsNames[i].name="obs-card:"+i;obsBands[i]=ObsButton(obsInstrument,Planets.BandLabels[i],-78+(i%2)*156,440+(i/2)*52,150,48,()=>ObsChange(()=>Obs.Band(n)));obsBands[i].name="obs-band:"+i;}
            for(int i=0;i<8;i++){int ring=i+1;obsRingButtons[i]=ObsButton(obsInstrument,ring.ToString(),-114+(i%4)*76,175+(i/4)*50,70,46,()=>ObsChange(()=>Obs.Ring(ring)));obsRingButtons[i].GetComponent<RectTransform>().anchoredPosition=new Vector2(i<4?-145:145,-(180+(i%4)*55));obsRingButtons[i].GetComponent<RectTransform>().sizeDelta=new Vector2(48,44);obsRingButtons[i].name="obs-ring:"+ring;}
            obsLeft=ObsButton(obsInstrument,"<",-115,370,50,44,()=>ObsChange(()=>Obs.Rotate(-1)));obsRight=ObsButton(obsInstrument,">",115,370,50,44,()=>ObsChange(()=>Obs.Rotate(1)));
            obsSeal=ObsButton(obsInstrument,"Seal",0,510,130,48,()=>ObsChange(()=>Obs.Seal()));obsAskCaspar=ObsButton(obsInstrument,"Ask Caspar",0,559,150,44,()=>ObsChange(()=>Obs.Ask()));
            var panel=Rect("Caspar",obsInstrument,0,675,328,170);panel.gameObject.AddComponent<Image>().color=PanelColor;
            obsPose=Picture(obsInstrument,"caspar-explain",133,585,60,100);obsPose.raycastTarget=false;
            obsMessage=Label(panel,"",0,75,304,130,14);obsMessage.resizeTextForBestFit=true;obsMessage.resizeTextMinSize=12;obsMessage.resizeTextMaxSize=14;
            // The retrograde mark uses bundled Garamond, which contains U+211E (fontTools preflight).
            obsMessage.font=Resources.Load<Font>("Fonts/EBGaramond-Bold");
            obsContinue=ObsButton(obsInstrument,"Continue",72,770,140,44,()=>ObsChange(()=>Obs.Continue()));
            obsExit=ObsButton(obsInstrument,"Back",-90,770,120,44,ObsClose);
            obsContinue.name="obs-next";obsExit.name="obs-back";obsSeal.name="obs-seal";obsAskCaspar.name="obs-ask";obsLeft.name="obs-left";obsRight.name="obs-right";
            for(int i=0;i<10;i++)ObsDrag(i);
            BuildPlanetJournal();Obs.Changed+=ObsCommitted;
            obsRoom.gameObject.SetActive(false);obsInstrument.gameObject.SetActive(false);
        }
        Text obsRoomLine;
        void ObsDrag(int seat)
        {
            var button=obsSpheres[seat];var trigger=button.gameObject.AddComponent<EventTrigger>();
            void Hook(EventTriggerType type,Action<PointerEventData> action){var e=new EventTrigger.Entry{eventID=type};e.callback.AddListener(d=>action((PointerEventData)d));trigger.triggers.Add(e);}
            Hook(EventTriggerType.BeginDrag,d=>{if(!busy&&Obs.Phase==ObservatoryPhase.Rings)Obs.Select(seat);});
            Hook(EventTriggerType.Drag,d=>{if(busy||Obs.Phase!=ObservatoryPhase.Rings||Obs.Selected!=seat||Obs.Progress.placed[seat])return;Vector2 p;if(RectTransformUtility.ScreenPointToLocalPointInRectangle(obsInstrument,d.position,null,out p))button.GetComponent<RectTransform>().anchoredPosition=new Vector2(p.x,p.y-400);});
            Hook(EventTriggerType.EndDrag,d=>{if(busy||Obs.Phase!=ObservatoryPhase.Rings||Obs.Selected!=seat)return;for(int i=0;i<8;i++)if(RectTransformUtility.RectangleContainsScreenPoint(obsRingButtons[i].GetComponent<RectTransform>(),d.position,null)){Obs.Ring(i+1);break;}ObsRefresh();});
        }
        void ObsCommitted(){Save();}
        void ObsChange(Func<bool> action){if(busy)return;action();ObsRefresh();}
        void ObsRefresh()
        {
            Save();Show();Publish();
            if(Flow.Keys>=5&&Flow.Keys>obsLastKey){obsLastKey=Flow.Keys;StartCoroutine(ObsKeyCeremony(Flow.Keys));}
        }
        IEnumerator ObsKeyCeremony(int n)
        {
            // Reuse the shipped Key ceremony with the table's Key visual on the orrery.
            var parent=gridKey.parent;gridKeyLabel.text="Keeper Key "+n;gridKey.SetParent(obsInstrument,false);yield return KeyCeremony(n);gridKey.SetParent(parent,false);gridKeyLabel.text="Keeper Key 3";
        }
        void ObsCard(int slot)
        {
            if(busy)return;
            if(Flow.PlanetReview){var t=Flow.CurrentReview;Flow.AnswerPlanetReview(slot);Save();Show();Publish();if(t.done)StartCoroutine(AfterTap());return;}
            if(Obs.Phase==ObservatoryPhase.Names)ObsChange(()=>Obs.AnswerName(Planets.Options(Obs.Target)[slot]));
            else if(Obs.Phase==ObservatoryPhase.Questions)ObsChange(()=>Obs.AnswerQuestion(Obs.QuestionOrder[slot]));
        }
        void ObsClose(){if(busy)return;if(Flow.PlanetReview){LeavePractice();return;}obsPlaying=false;Show();Publish();}
        void ObsStart(bool fromWalk=false){if(!Flow.AtObservatory||busy&&!fromWalk)return;obsPlaying=Obs.Begin();ObsRefresh();}
        void ShowObservatory()
        {
            if(obsRoom==null)return;
            bool on=Flow.AtObservatory,reviewing=Flow.PlanetReview;if(!on||!obsPlaying)obsLastKey=Flow.Keys;
            obsRoom.gameObject.SetActive(on&&!obsPlaying);obsInstrument.gameObject.SetActive(on&&obsPlaying||reviewing);
            if(on&&!obsPlaying){ApplyKit(obsKit,true);for(int i=0;i<10;i++)obsRoomSpheres[i].color=Obs.Progress.named[i]?new Color(1,1,1,Flow.ObservatoryLevel>0?1:.55f):new Color(.15f,.15f,.18f);obsRoomLine.text=Flow.ObservatoryWhole?ObservatoryCopy.HubObsWhole:Flow.Keys>=6?ObservatoryCopy.HubKey6:Flow.Keys>=5?Flow.KeysSpent>=5?ObservatoryCopy.HubSpent5:ObservatoryCopy.HubKey5:ObservatoryCopy.ObsFirst;if(!Obs.Progress.firstSeen){Obs.Progress.firstSeen=true;Save();}}
            if(!on&&!reviewing)return;
            bool names=!reviewing&&Obs.Phase==ObservatoryPhase.Names,work=!reviewing&&Obs.Phase==ObservatoryPhase.Work,rings=!reviewing&&Obs.Phase==ObservatoryPhase.Rings,bands=!reviewing&&Obs.Phase==ObservatoryPhase.Bands,question=!reviewing&&Obs.Phase==ObservatoryPhase.Questions;
            var task=Flow.CurrentReview;bool rg=reviewing&&task.Mode==ReviewMode.PlanetGlyph,rf=reviewing&&task.Mode==ReviewMode.PlanetFunction,rc=reviewing&&task.Mode==ReviewMode.PlanetCompany;
            obsTitle.text="The Observatory";
            obsAsk.text=rg||names?"Whose symbol is this?":rf?Planets.All[task.seat].Work:rc?Planets.Sentence(task.seat):work?Planets.All[Obs.Target].Work:question?Obs.Question.ask:"";
            obsGlyph.gameObject.SetActive(names||rg||rc);if(names||rg||rc)PutPlanet(obsGlyph,reviewing?task.seat:Obs.Target,54);
            obsMessage.text=reviewing?Flow.Note:Obs.Message;var pose=Slots.Image("caspar-"+(reviewing?"explain":ObservatoryCopy.Pose(Obs.CopyId)));obsPose.sprite=pose;obsPose.gameObject.SetActive(pose!=null);
            for(int i=0;i<10;i++)
            {
                var b=obsSpheres[i];b.gameObject.SetActive(work||rings||bands);
                float a=(i-(work?Obs.Turn:0))*Mathf.PI*2/10;var pos=work?new Vector2(Mathf.Sin(a)*120,290-Mathf.Cos(a)*120):new Vector2(-120+((rings?Array.IndexOf(Obs.TrayOrder,i):i)%5)*60,(rings?455:300)+((rings?Array.IndexOf(Obs.TrayOrder,i):i)/5)*60);
                if((rings||bands)&&Obs.Progress.placed[i]){float angle=i==0?Mathf.PI:i==1?0:i==2?2.094f:i==3?4.189f:i*.61f;float radius=18+Planets.All[i].Ring*13;pos=new Vector2(Mathf.Sin(angle)*radius,290-Mathf.Cos(angle)*radius);}
                b.GetComponent<RectTransform>().anchoredPosition=new Vector2(pos.x,-pos.y);
                b.interactable=!busy&&!Obs.AwaitingNext&&(!rings||!Obs.Progress.placed[i])&&(!bands||!Obs.Progress.banded[i]);
                b.GetComponent<Image>().raycastTarget=b.interactable;
                obsSphereGlyphs[i].fontSize=(rings||bands)&&Obs.Progress.placed[i]?30:44;obsSphereBands[i].text=work&&Obs.Reveal&&i==Obs.Target?Planets.Sentence(i):bands&&Obs.Progress.banded[i]?Planets.BandLabels[Planets.All[i].Band]:"";
                obsSphereGlyphs[i].color=Obs.Selected==i||work&&Obs.Turn==i?Gilt:Bone;
            }
            for(int i=0;i<4;i++)
            {
                var b=obsNames[i];b.gameObject.SetActive(names||question||rg||rf);
                string label=names?Planets.Sentence(Planets.Options(Obs.Target)[i]):question?Obs.Question.choices[Obs.QuestionOrder[i]]:rg||rf?Planets.Sentence(Flow.PlanetReviewOptions[i]):"";
                b.GetComponentInChildren<Text>().text=label;b.GetComponentInChildren<Text>().resizeTextForBestFit=true;b.GetComponentInChildren<Text>().resizeTextMinSize=10;b.GetComponentInChildren<Text>().resizeTextMaxSize=15;
                if(question){b.GetComponent<RectTransform>().anchoredPosition=new Vector2(0,-(245+64*i));b.GetComponent<RectTransform>().sizeDelta=new Vector2(308,60);}else{b.GetComponent<RectTransform>().anchoredPosition=new Vector2(-78+(i%2)*156,-(440+(i/2)*52));b.GetComponent<RectTransform>().sizeDelta=new Vector2(150,48);}
                b.GetComponent<Image>().color=(question&&Obs.AwaitingNext&&Obs.QuestionOrder[i]==Obs.Question.answer||names&&Obs.Reveal&&Planets.Options(Obs.Target)[i]==Obs.Target)?Gilt:Dim;
                b.interactable=!busy&&(reviewing||!Obs.AwaitingNext);
                obsBands[i].gameObject.SetActive(bands||rc);obsBands[i].interactable=!busy&&(reviewing||Obs.Selected>=0&&!Obs.AwaitingNext);
                if(rc){obsBands[i].onClick.RemoveAllListeners();int n=i;obsBands[i].onClick.AddListener(()=>ObsCard(n));}else{obsBands[i].onClick.RemoveAllListeners();int n=i;obsBands[i].onClick.AddListener(()=>ObsChange(()=>Obs.Band(n)));obsBands[i].name="obs-band:"+i;}
            }
            for(int i=0;i<8;i++){obsRingButtons[i].gameObject.SetActive(rings);obsRingButtons[i].interactable=!busy&&Obs.Selected>=0&&!Obs.AwaitingNext;}
            obsLeft.gameObject.SetActive(work);obsRight.gameObject.SetActive(work);obsSeal.gameObject.SetActive(work);obsAskCaspar.gameObject.SetActive(work&&Obs.Hint<2&&!Obs.AwaitingNext);
            obsLeft.interactable=obsRight.interactable=!busy&&!Obs.AwaitingNext;obsSeal.interactable=!busy&&!Obs.AwaitingNext&&(Obs.Hint<3||Obs.Turn==Obs.Target);
            obsContinue.gameObject.SetActive(!reviewing&&Obs.CanContinue);obsContinue.interactable=!busy;
            
            if(bands&&Obs.Reveal&&Obs.Selected>=0)obsAsk.text=Planets.BandLabels[Planets.All[Obs.Selected].Band];
        }
        void UpdateObservatory()
        {
            if(!busy&&!obsDoorBeat&&Flow.AtHub&&Flow.WingWhole&&!Obs.Progress.doorSeen)StartCoroutine(ObservatoryDoorBeat());
        }
        IEnumerator ObservatoryDoorBeat()
        {
            obsDoorBeat=true;busy=true;hubText.text=HubWholeLine;Publish();
            yield return new WaitForSecondsRealtime(ReducedMotion?1.5f:5f);
            Flow.RevealObservatoryDoor();Save();foreach(var k in atriumKits)if(k.Container.gameObject.activeInHierarchy)ApplyKit(k,true);
            yield return new WaitForSecondsRealtime(ReducedMotion?0:1f);
            hubText.text=ObservatoryCopy.ObsDoor;Publish();yield return new WaitForSecondsRealtime(ReducedMotion?1.5f:5f);
            busy=false;obsDoorBeat=false;Show();Publish();
        }
        bool ObsAction(string command)
        {
            if(command=="obs-start"){ObsStart();return true;}if(command=="obs-back"){ObsClose();return true;}
            if(command=="obs-next"){ObsChange(()=>Obs.Continue());return true;}if(command=="obs-seal"){ObsChange(()=>Obs.Seal());return true;}if(command=="obs-ask"){ObsChange(()=>Obs.Ask());return true;}
            if(command=="obs-left"||command=="obs-right"){ObsChange(()=>Obs.Rotate(command=="obs-left"?-1:1));return true;}
            var p=command.Split(':');if(p.Length==2&&int.TryParse(p[1],out int n))switch(p[0]){case "obs-card":ObsCard(n);return true;case "obs-sphere":ObsChange(()=>Obs.Select(n));return true;case "obs-ring":ObsChange(()=>Obs.Ring(n));return true;case "obs-band":ObsChange(()=>Obs.Band(n));return true;case "planet-page":if(Flow.OpenPlanet(n)){Show();Publish();}return true;}
            if(command=="planet-index"){if(Flow.PlanetIndex()){Show();Publish();}return true;}return false;
        }
        void FillObservatory(DialView.WebState s)
        {
            s.observatory=Flow.AtObservatory;s.observatoryPlaying=obsPlaying&&s.observatory;s.observatoryOpen=Flow.ObservatoryOpen;s.observatoryDoorSeen=Obs.Progress.doorSeen;s.observatoryWhole=Flow.ObservatoryWhole;s.observatoryLevel=Flow.ObservatoryLevel;
            s.orreryCardLabels=obsNames.Select(b=>b.GetComponentInChildren<Text>().text).ToArray();s.orreryQuestionOrder=Obs.QuestionOrder;
            s.orreryPhase=Obs.Phase.ToString().ToLowerInvariant();s.orreryTarget=Obs.Target;s.orrerySelected=Obs.Selected;s.orreryTurn=Obs.Turn;s.orreryHint=Obs.Hint;s.orreryMisses=Obs.Misses;s.orreryNext=Obs.CanContinue&&!busy;s.orreryRevealed=Obs.Reveal;s.orreryPlaced=Obs.Progress.placed;s.orreryNamed=Obs.Progress.named;s.orreryWorked=Obs.Progress.worked;s.orreryBanded=Obs.Progress.banded;s.orreryEvidence5=Obs.Progress.workEvidence;s.orreryEvidence6=Obs.Progress.ringEvidence;s.orreryMessage=Obs.Message;s.orreryWork=Obs.Target>=0?Planets.All[Obs.Target].Work:"";s.orreryGlyphs=Planets.All.Select(p=>p.Glyph).ToArray();s.orreryNames=Planets.All.Select(p=>p.Name).ToArray();
            var controls=(obsInstrument.gameObject.activeInHierarchy?obsInstrument.GetComponentsInChildren<Button>():obsJournal.gameObject.activeInHierarchy?obsJournal.GetComponentsInChildren<Button>():new Button[0]).Where(b=>b.gameObject.activeInHierarchy&&b.interactable).ToArray();s.observatoryControlIds=controls.Select(b=>b.name).ToArray();s.orreryControls=controls.SelectMany(b=>PageBox(b)).ToArray();
            s.planetPage=Flow.JournalPlanet;s.planetMet=Enumerable.Range(0,10).Select(Flow.PlanetMet).ToArray();s.planetInks=Enumerable.Range(0,10).Select(Flow.PlanetInk).ToArray();s.planetRetro=Obs.Progress.retro;s.planetDetails=obsDetails!=null?obsDetails.text:"";
        }
        void BuildPlanetJournal()
        {
            obsJournal=Rect("The Planets",journal,0,400,360,800);
            for(int i=0;i<10;i++){int n=i;obsPlanetPages[i]=ObsButton(obsJournal,Planets.All[i].Name,-72+(i%2)*150,155+(i/2)*74,140,64,()=>{if(Flow.OpenPlanet(n)){Show();Publish();}});obsPlanetPages[i].name="planet-page:"+i;PutPlanet(obsPlanetPages[i].GetComponentInChildren<Text>(),i,44);}
            obsJournalGlyph=Label(obsJournal,"",JournalX,185,110,90,64);obsDetails=Label(obsJournal,"",JournalX,350,260,260,18);obsDetails.font=Resources.Load<Font>("Fonts/EBGaramond-Bold");obsDetails.color=JournalInk;
            obsJournalBack=ObsButton(obsJournal,"The Planets",JournalX,550,200,44,()=>{if(Flow.PlanetIndex()){Show();Publish();}});obsJournalBack.name="planet-index";
        }
        void ShowPlanetJournal()
        {
            if(obsJournal==null)return;bool index=Flow.AtJournal&&Flow.JournalAt==JournalView.Planets,page=Flow.AtJournal&&Flow.JournalAt==JournalView.Planet;obsJournal.gameObject.SetActive(index||page);
            for(int i=0;i<10;i++){obsPlanetPages[i].gameObject.SetActive(index);obsPlanetPages[i].interactable=Flow.PlanetMet(i)&&!busy;obsPlanetPages[i].GetComponentInChildren<Text>().color=new Color(Gilt.r,Gilt.g,Gilt.b,Flow.PlanetMet(i)?Flow.PlanetInk(i):.15f);}
            obsJournalGlyph.gameObject.SetActive(page);obsJournalBack.gameObject.SetActive(page);obsDetails.gameObject.SetActive(page||index&&Obs.Progress.retro);
            obsDetails.rectTransform.anchoredPosition=new Vector2(JournalX,-(page?350:565));
            if(page){int n=Flow.JournalPlanet;PutPlanet(obsJournalGlyph,n,64);var p=Planets.All[n];obsJournalGlyph.color=new Color(Gilt.r,Gilt.g,Gilt.b,Flow.PlanetInk(n));obsDetails.text=p.Name+"\n\nWork: "+p.Work+"\nCompany: "+p.Company+"\nPace: "+p.Pace+" a sign";}
            else obsDetails.text=Obs.Progress.retro?"℞ means the planet seems to move backward, seen from Earth.":"";
        }
    }
}
