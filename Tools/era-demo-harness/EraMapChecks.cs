// The era demo's pure model checks (task 86bcg8az2). Run with Tools/era-demo-harness/run.sh; not part of the Unity project.
using System; using System.Linq; using Ascendant.EraDemo;
static class T {
  static int fails=0; static void Check(bool ok,string what){Console.WriteLine((ok?"PASS ":"FAIL ")+what); if(!ok)fails++;}
  static int Main(){
    var map=EraDemoContent.Build(); var a=map.Arrival;
    Check(map.Width==12&&map.Height==29,"map 12x29 "+map.Width+"x"+map.Height);
    Check(map.Kind(map.Find('D'))==CellKind.Wall && map.Beside(map.Find('D')).Count==1,"portal is in the wall, one square beside it");
    var w=new EraWalker(map,a); var cas=map.Point("caspar"); w.PlaceFollower(cas);
    string arrived=null; w.Arrived+=id=>arrived=id; var log=new System.Collections.Generic.List<string>(); w.Logged+=log.Add;
    Check(w.GoTo("baker"),"route to the baker"); int n=w.Route.Count;
    for(int i=0;i<1000&&w.Walking;i++) w.Tick(1/60f);
    Check(arrived=="baker" && map.Beside(map.Point("baker").At).Contains(w.At),"arrives beside the baker at "+w.At+" after "+n+" squares");
    Check(cas.At.Distance(w.At)==1,"Caspar trails one square behind: "+cas.At);
    arrived=null; Check(w.GoTo("teacher"),"route to the teacher (through the square, the courtyard, the stairs)"); int steps=w.Route.Count; Check(w.Jump() && arrived=="teacher","reduced motion jumps there");
    Check(w.Facing==Facing.Left||w.Facing==Facing.Right||w.Facing==Facing.Up||w.Facing==Facing.Down,"faces "+w.Facing+"; route "+steps);
    var t=map.Point("teacher").At; Check(t.Distance(w.At)==1,"beside the teacher");
    // facing toward the teacher
    var dir=w.Facing; int dx=t.X-w.At.X, dy=t.Y-w.At.Y; Check((dx==1&&dir==Facing.Right)||(dx==-1&&dir==Facing.Left)||(dy==1&&dir==Facing.Down)||(dy==-1&&dir==Facing.Up),"turns to face the teacher ("+dir+")");
    Check(!w.Tap(new Cell(0,0)),"a wall square does nothing");
    Check(!w.Tap(map.Find('o')),"a prop square does nothing");
    Check(w.Tap(new Cell(1,1)) && w.Jump() && w.At==new Cell(1,1),"a floor square walks there");
    arrived=null; Check(w.GoTo("caspar") && (w.Jump()||true) && arrived=="caspar" && w.At.Distance(map.Point("caspar").At)==1,"walk to Caspar (beside already: arrives at once)");
    var talk=new EraTalk(); string ended=null; talk.Ended+=p=>ended=p.Id;
    Check(talk.Begin(map.Point("teacher")) && talk.Current.Text.StartsWith("[Placeholder]"),"talk begins");
    Check(talk.Next() && talk.Waiting && !talk.Next(),"a choice line waits");
    Check(talk.Choose(1) && talk.Chosen=="[Placeholder] Choice B" && !talk.Waiting,"choose B");
    Check(talk.Next() && ended=="teacher" && !talk.Open,"talk ends");
    Check(!talk.Begin(map.Point("portal")),"the portal has no talk");
    Check(w.GoTo("portal") && w.Jump() && arrived=="portal" && w.At==map.Beside(map.Find('D'))[0],"walks to the door home");
    // mid-step retarget
    var w2=new EraWalker(map,a); w2.GoTo("baker"); w2.Tick(0.05f); var first=w2.Route[0]; w2.GoTo("portal"); Check(w2.Route.Count>0 && w2.Route[0]==first,"a new tap mid-step finishes the step first");
    for(int i=0;i<2000&&w2.Walking;i++) w2.Tick(1/60f); Check(!w2.Walking && Math.Abs(w2.X-w2.At.X)<1e-4 && Math.Abs(w2.Y-w2.At.Y)<1e-4,"ends exactly on a square centre "+w2.X+","+w2.Y);
    // speed
    var w3=new EraWalker(map,a); w3.SetSpeed(170); w3.Tap(new Cell(7,18)); int sq=w3.Route.Count; float tt=0; while(w3.Walking){w3.Tick(0.01f);tt+=0.01f;} Check(Math.Abs(tt-sq*40/170f)<0.05,sq+" squares at 170 px/s take "+tt.ToString("0.00")+" s");
    Console.WriteLine(fails==0?"ALL PASS":fails+" FAILED"); return fails;
  }
}
