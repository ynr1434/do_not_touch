using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
namespace DoNotTouch {
 public class ShiftManager : NetworkBehaviour {
  public static ShiftManager Instance {get; private set;}
  public ShiftDefinition definition;
  public ObjectiveZone crateZone, statueZone, exitZone;
  public Transform[] spawnPoints;
  public NetworkVariable<int> Crates=new(), Inspections=new(), Statues=new(), Damaged=new(), Witnessed=new();
  public NetworkVariable<float> Elapsed=new();
  // 0 = working, 1 = complete, 2 = timed out
  public NetworkVariable<int> Result=new();
  readonly HashSet<ulong> inspected=new();
  float tick;
  public bool AllTasks => NightShiftDirector.Instance?NightShiftDirector.Instance.MandatoryComplete:Crates.Value>=definition.crateTarget && Inspections.Value>=definition.inspectionTarget && Statues.Value>=definition.statueTarget;
  public int TasksCompleted => (Crates.Value>=definition.crateTarget?1:0)+(Inspections.Value>=definition.inspectionTarget?1:0)+(Statues.Value>=definition.statueTarget?1:0);
  void Awake(){ Instance=this; }
  public override void OnDestroy(){if(Instance==this) Instance=null;base.OnDestroy();}
  public bool Inspect(ulong id) {
   if(!IsServer || Result.Value!=0 || !inspected.Add(id)) return false;
   Inspections.Value=ShiftRules.Progress(inspected.Count,definition.inspectionTarget); return true;
  }
  void Update() {
   if(!IsServer || !IsSpawned || Result.Value!=0) return;
   tick+=Time.deltaTime; if(tick<.2f) return;
   Elapsed.Value=Mathf.Min(definition.durationSeconds,Elapsed.Value+tick); tick=0;
   bool allHome=NetworkManager.ConnectedClients.Count>0;
   foreach(var c in NetworkManager.ConnectedClients.Values)
    allHome &= c.PlayerObject!=null && exitZone.Contains(c.PlayerObject.transform.position);
   if(NightShiftDirector.Instance)NightShiftDirector.Instance.ServerTick(this,allHome);
   else {Crates.Value=ShiftRules.Progress(crateZone.CountDelivered(),definition.crateTarget);Statues.Value=ShiftRules.Progress(statueZone.CountDelivered(),definition.statueTarget);if(AllTasks && allHome)Result.Value=1;}
   if(Result.Value==0 && Elapsed.Value>=definition.durationSeconds) Result.Value=2;
  }
 }
}
