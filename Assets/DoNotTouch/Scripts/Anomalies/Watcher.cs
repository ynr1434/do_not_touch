using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using System.Collections.Generic;
namespace DoNotTouch {
 public class Watcher : NetworkBehaviour {
  public Vector2 activationDelay=new(35,75);
  public float moveCooldown=12, detectionAngle=110, maxDetectionDistance=30;
  public Transform[] positions;
  public NetworkVariable<bool> Activated=new();
  public NetworkVariable<int> PositionIndex=new(), MoveCount=new();
  public NetworkVariable<int> State=new((int)WatcherState.Dormant);
  float activateAt, nextMove, nextCheck; int witnessedMove;
  public bool SoloTestMode {get; private set;}
  public int NextAnchor {get; private set;}=-1;
  public float Cooldown=>Mathf.Max(0,nextMove-Time.time);
  public void EnableSoloTest(){SoloTestMode=true;activateAt=Time.time+10;nextMove=activateAt+4;}
  public void ResetForOnboarding(){if(!IsServer)return;Activated.Value=false;State.Value=(int)WatcherState.Dormant;PositionIndex.Value=0;MoveCount.Value=0;NextAnchor=-1;activateAt=Time.time+99999;nextMove=activateAt;}
  public override void OnNetworkSpawn(){if(IsServer){activateAt=Time.time+Random.Range(activationDelay.x,activationDelay.y);nextMove=activateAt;}}
  public bool ObservedBy(Vector3 eye, Vector3 forward) {
   return PointObserved(eye,forward,transform.position+Vector3.up*1.3f) ||
    PointObserved(eye,forward,transform.position+Vector3.up*1.95f);
  }
  bool PointObserved(Vector3 eye,Vector3 forward,Vector3 target) {
   if(!ShiftRules.InView(eye,forward,target,detectionAngle,maxDetectionDistance)) return false;
   if(UnityEngine.Physics.Linecast(eye,target,out var hit,~(1<<8),QueryTriggerInteraction.Ignore)) return hit.transform.IsChildOf(transform);
   return true;
  }
  public bool IsObserved() {
   foreach(var p in PlayerController.Active) if(p && p.IsSpawned && p.IncapacitatedUntil.Value<=NetworkManager.ServerTime.Time && ObservedBy(p.Eye,p.ViewForward)) return true;
   return false;
  }
  public void RequestUnobservedMove(){if(IsServer && Activated.Value && !NightShiftDirector.Instance)nextMove=Mathf.Min(nextMove,Time.time+.5f);}
  public static bool MayMove(bool active,bool observed,float now,float next) => active && !observed && now>=next;
  void Update() {
   if(!IsServer || !IsSpawned || !ShiftManager.Instance || ShiftManager.Instance.Result.Value!=0 || Time.time<nextCheck) return;
   nextCheck=Time.time+.1f;
   var night=NightShiftDirector.Instance;
   if(night){NightUpdate(night);return;}
   if(!Activated.Value && Time.time>=activateAt) Activated.Value=true;
   bool observed=IsObserved();
   if(observed && MoveCount.Value>witnessedMove){witnessedMove=MoveCount.Value;ShiftManager.Instance.Witnessed.Value++;}
   if(!MayMove(Activated.Value,observed,Time.time,nextMove) || positions==null || positions.Length<2) return;
   int next=(PositionIndex.Value+Random.Range(1,positions.Length))%positions.Length;
   Quaternion rotation=positions[next].rotation;
   var staging=GetComponent<WatcherStaging>();
   if(staging && !staging.TryChoose(out next,out rotation)){nextMove=Time.time+3;return;}
   // Avoid appearing inside a worker or a delivered object.
   if(next!=PositionIndex.Value && UnityEngine.Physics.CheckCapsule(positions[next].position+Vector3.up*.4f,positions[next].position+Vector3.up*1.7f,.4f,~0,QueryTriggerInteraction.Ignore)) {nextMove=Time.time+1;return;}
   GetComponent<NetworkTransform>().Teleport(positions[next].position,rotation,Vector3.one);
   PositionIndex.Value=next;MoveCount.Value++;nextMove=Time.time+moveCooldown;
  }
  void NightUpdate(NightShiftDirector night) {
   var phase=(NightPhase)night.Phase.Value;
   var desired=SoloTestMode?(Time.time<activateAt?WatcherState.Dormant:WatcherState.Stalking):NightRules.StateForPhase(phase);
   if(State.Value!=(int)desired){State.Value=(int)desired;nextMove=Time.time+4;}
   Activated.Value=State.Value!=(int)WatcherState.Dormant&&State.Value!=(int)WatcherState.Disabled;
   if(!Activated.Value)return;
   bool observed=IsObserved();if(observed){if(SoloTestMode)nextMove=Time.time+4;return;}
   PlayerController nearest=null;float distance=float.MaxValue;
   foreach(var p in PlayerController.Active)if(p&&p.IsSpawned){float d=Vector3.Distance(transform.position,p.transform.position);if(d<distance){distance=d;nearest=p;}}
   if(!nearest)return;
   if(distance<1.9f && !SoloTestMode && State.Value>=(int)WatcherState.Hunting) {
    var points=night.recoveryPoints;if(points!=null&&points.Length>0)nearest.ServerWatcherAttack(points[(int)(nearest.OwnerClientId%(ulong)points.Length)].position,2.5);
    int reset=FarthestAnchor(nearest.transform.position);MoveTo(reset,positions[reset].rotation);State.Value=(int)WatcherState.Awake;nextMove=Time.time+NightRules.WatcherCooldown(night.PlayerCount,WatcherState.Awake);return;
   }
   if(Time.time<nextMove||positions==null||positions.Length<2)return;
   int best=ChooseStep(nearest.transform.position,distance);
   NextAnchor=best;
   if(best!=PositionIndex.Value)MoveTo(best,Quaternion.LookRotation(Vector3.ProjectOnPlane(nearest.transform.position-positions[best].position,Vector3.up)));
   nextMove=Time.time+(SoloTestMode?4:NightRules.WatcherCooldown(night.PlayerCount,(WatcherState)State.Value));
  }
  int ChooseStep(Vector3 target,float distance) {
   // Search reachable anchors: the next step may go around a corner, never through a wall.
   int start=Mathf.Clamp(PositionIndex.Value,0,positions.Length-1),goal=start;
   var parent=new int[positions.Length];System.Array.Fill(parent,-1);parent[start]=start;
   var queue=new Queue<int>();queue.Enqueue(start);float closest=distance;
   float clearance=SoloTestMode?1.2f:State.Value==(int)WatcherState.Awake?7:State.Value==(int)WatcherState.Stalking?3:1.1f;
   while(queue.Count>0){int current=queue.Dequeue();for(int i=0;i<positions.Length;i++){
    if(parent[i]!=-1 || Vector3.Distance(positions[current].position,positions[i].position)>4.3f || AnchorBlocked(i) || !ClearSegment(positions[current].position,positions[i].position))continue;
    parent[i]=current;queue.Enqueue(i);float d=Vector3.Distance(positions[i].position,target);
    if(d<closest-.25f&&d>=clearance){closest=d;goal=i;}
   }}
   if(goal==start)return start;
   while(parent[goal]!=start)goal=parent[goal];
   return VisiblePosition(positions[goal].position)?start:goal;
  }
  bool ClearSegment(Vector3 from,Vector3 to){Vector3 delta=to-from;foreach(var hit in UnityEngine.Physics.SphereCastAll(from+Vector3.up*1.1f,.44f,delta.normalized,delta.magnitude,~(1<<8),QueryTriggerInteraction.Ignore))if(!hit.transform.IsChildOf(transform))return false;return true;}
  bool VisiblePosition(Vector3 point){foreach(var p in PlayerController.Active)if(p&&p.IsSpawned&&ShiftRules.InView(p.Eye,p.ViewForward,point+Vector3.up*1.6f,detectionAngle,maxDetectionDistance)&&!UnityEngine.Physics.Linecast(p.Eye,point+Vector3.up*1.6f,out _,~(1<<8),QueryTriggerInteraction.Ignore))return true;return false;}
  bool AnchorBlocked(int index)=>UnityEngine.Physics.CheckCapsule(positions[index].position+Vector3.up*.48f,positions[index].position+Vector3.up*1.8f,.43f,~0,QueryTriggerInteraction.Ignore);
  int FarthestAnchor(Vector3 point){int best=0;float d=-1;for(int i=0;i<positions.Length;i++){float n=(positions[i].position-point).sqrMagnitude;if(n>d&&!AnchorBlocked(i)){d=n;best=i;}}return best;}
  void MoveTo(int index,Quaternion rotation){GetComponent<NetworkTransform>().Teleport(positions[index].position,rotation,Vector3.one);PositionIndex.Value=index;MoveCount.Value++;if(NightShiftDirector.Instance){var h=NightShiftDirector.Instance.horror;int room=6;float distance=float.MaxValue;if(h&&h.ambience)for(int i=1;i<h.ambience.roomSources.Length;i++){float d=(h.ambience.roomSources[i].position-transform.position).sqrMagnitude;if(d<distance){distance=d;room=i;}}h?.ServerBeginEvent(HorrorEventKind.Flicker,room,2);}}
  void OnDrawGizmosSelected(){if(positions==null)return;Gizmos.color=Color.magenta;foreach(var p in positions)if(p)Gizmos.DrawWireSphere(p.position+Vector3.up, .4f);}
 }
}
