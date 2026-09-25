using Unity.Netcode;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using System.Linq;
namespace DoNotTouch {
 public enum HorrorEventKind { Creak, DistantImpact, Flicker, SectionBlackout, BriefBlackout }
 // One server timeline; clients derive light curves from the same network clock.
 public class HorrorDirector : NetworkBehaviour {
  public float calmSeconds=110;
  public Vector2 eventInterval=new(45,85);
  public HorrorLamp[] lamps;
  public MuseumAmbience ambience;
  public Watcher watcher;
  public NetworkVariable<int> EventSerial=new(), EventKind=new(), EventRoom=new(4), Tension=new();
  public NetworkVariable<double> EventStart=new();
  public NetworkVariable<float> EventDuration=new();
  double nextEvent, nextCheck; int restored;
  Camera configuredCamera;
  public double Now=>IsSpawned?NetworkManager.ServerTime.Time:0;
  public bool EventActive=>IsSpawned && EventSerial.Value>0 && Now>=EventStart.Value && Now<EventStart.Value+EventDuration.Value;
  public override void OnNetworkSpawn(){if(IsServer)nextEvent=Now+calmSeconds;ambience.Begin(this);}
  public override void OnNetworkDespawn(){ambience.Stop();}
  public static int Stage(float elapsed,float duration) {
   float phase=elapsed/Mathf.Max(1,duration);
   return phase<.22f?0:phase<.48f?1:phase<.76f?2:3;
  }
  public bool ServerBeginEvent(HorrorEventKind kind,int room,float duration) {
   if(!IsServer || !IsSpawned || (EventSerial.Value>0 && Now<EventStart.Value+EventDuration.Value) || ShiftManager.Instance.Result.Value!=0) return false;
   EventKind.Value=(int)kind;EventRoom.Value=Mathf.Clamp(room,1,Mathf.Max(1,lamps.Length==0?5:System.Array.ConvertAll(lamps,l=>l.room).Max()));
   EventDuration.Value=Mathf.Clamp(duration,2,12);EventStart.Value=Now+.25;EventSerial.Value++;
   return true;
  }
  public static float LightLevel(int lampRoom,bool emergency,bool normallyOn,int kind,int eventRoom,double age,float duration,int serial) {
   if(!normallyOn)return 0;
   if(emergency || lampRoom==0 || age<0 || age>=duration || kind<(int)HorrorEventKind.Flicker)return 1;
   bool affected=lampRoom==eventRoom || (kind==(int)HorrorEventKind.BriefBlackout && lampRoom!=0);
   if(!affected)return 1;
   if(kind>=(int)HorrorEventKind.SectionBlackout)return .015f;
   int pulse=(int)(age*13)+serial*17+lampRoom*31;
   return (pulse%11==0 || pulse%7<2)? .05f: .78f+.22f*Mathf.Sin((float)age*37);
  }
  void Update() {
   if(PlayerController.Local && configuredCamera!=PlayerController.Local.viewCamera) {
    configuredCamera=PlayerController.Local.viewCamera;
    configuredCamera.GetUniversalAdditionalCameraData().renderPostProcessing=true;
    configuredCamera.allowHDR=true;
   }
   if(!IsSpawned)return;
   double age=Now-EventStart.Value;
   foreach(var lamp in lamps)if(lamp){float level=LightLevel(lamp.room,lamp.emergency,lamp.normallyOn,EventKind.Value,EventRoom.Value,age,EventDuration.Value,EventSerial.Value);if(NightShiftDirector.Instance&&!NightShiftDirector.Instance.PowerRestored.Value&&lamp.room==8&&!lamp.emergency)level=.015f;lamp.Apply(level);}
   ambience.Tick(this);
   if(!IsServer || Now<nextCheck)return;nextCheck=Now+.25;
   var shift=ShiftManager.Instance;if(shift.Result.Value!=0)return;
   Tension.Value=Stage(shift.Elapsed.Value,shift.definition.durationSeconds);
   if(EventSerial.Value>restored && Now>=EventStart.Value+EventDuration.Value) {
    restored=EventSerial.Value;
    if(EventKind.Value>=(int)HorrorEventKind.Flicker && watcher && Random.value<.65f)watcher.RequestUnobservedMove();
   }
   var night=NightShiftDirector.Instance;if(night&&night.Phase.Value==(int)NightPhase.NormalShift)return;bool finale=night&&night.Phase.Value>=(int)NightPhase.FinalTask;
   if(Now<nextEvent)return;
   int stage=Tension.Value;
   var kind=stage==0?(Random.value<.8f?HorrorEventKind.Creak:HorrorEventKind.Flicker):
    stage==1?(Random.value<.55f?HorrorEventKind.DistantImpact:HorrorEventKind.Flicker):
    Random.value<.55f?HorrorEventKind.Flicker:stage==3&&Random.value<.3f?HorrorEventKind.BriefBlackout:HorrorEventKind.SectionBlackout;
   ServerBeginEvent(kind,Random.Range(1,6),kind==HorrorEventKind.BriefBlackout?3:Random.Range(4,8));
   nextEvent=Now+(finale?Random.Range(18,32):Random.Range(eventInterval.x,eventInterval.y)*(stage==0?1.3f:1));
  }
 }
}
