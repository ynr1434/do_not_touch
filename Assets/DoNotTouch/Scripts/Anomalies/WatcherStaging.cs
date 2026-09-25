using Unity.Netcode;
using UnityEngine;
namespace DoNotTouch {
 // Optional presentation policy. The original Watcher's visibility/authority gate remains in control.
 public class WatcherStaging : NetworkBehaviour {
  public Watcher watcher;
  public HorrorDirector director;
  public NetworkVariable<int> Stage=new();
  public float maximumApproach=4;
  public bool TryChoose(out int index,out Quaternion rotation) {
   index=watcher.PositionIndex.Value;rotation=transform.rotation;
   if(!IsServer || PlayerController.Active.Count==0)return false;
   Stage.Value=director.Tension.Value;
   float nearest=Nearest(transform.position,out _);
   float min=Stage.Value==0?11:Stage.Value==1?7:Stage.Value==2?4:2.4f;
   int start=Random.Range(0,watcher.positions.Length);
   for(int n=0;n<watcher.positions.Length;n++) {
    int candidate=(start+n)%watcher.positions.Length;
    if(candidate==watcher.PositionIndex.Value)continue;
    Vector3 point=watcher.positions[candidate].position;
    float distance=Nearest(point,out var worker);
    if(distance<min || distance<nearest-maximumApproach || VisibleAt(point))continue;
    if(UnityEngine.Physics.CheckCapsule(point+Vector3.up*.48f,point+Vector3.up*1.8f,.43f,~0,QueryTriggerInteraction.Ignore))continue;
    index=candidate;
    Vector3 direction=worker.transform.position-point;direction.y=0;
    rotation=Quaternion.LookRotation(direction.normalized)*Quaternion.Euler(0,Random.Range(-18,19),0);
    return true;
   }
   // A small change of posture is also forbidden under observation by Watcher.Update.
   if(Random.value<.18f)rotation=transform.rotation*Quaternion.Euler(0,Random.Range(-20,21),0);
   return rotation!=transform.rotation;
  }
  public bool VisibleAt(Vector3 position) {
   foreach(var p in PlayerController.Active) {
    if(!p || !p.IsSpawned)continue;
    Vector3 target=position+Vector3.up*1.95f;
    if(!ShiftRules.InView(p.Eye,p.ViewForward,target,watcher.detectionAngle,watcher.maxDetectionDistance))continue;
    if(!UnityEngine.Physics.Linecast(p.Eye,target,out var hit,~(1<<8),QueryTriggerInteraction.Ignore) || hit.transform.IsChildOf(transform))return true;
   }
   return false;
  }
  float Nearest(Vector3 point,out PlayerController player) {
   float best=float.MaxValue;player=null;
   foreach(var p in PlayerController.Active)if(p && p.IsSpawned){float distance=Vector3.Distance(point,p.transform.position);if(distance<best){best=distance;player=p;}}
   return best;
  }
 }
}
