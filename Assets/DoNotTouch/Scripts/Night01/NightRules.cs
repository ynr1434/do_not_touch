using UnityEngine;
namespace DoNotTouch {
 public enum NightPhase { NormalShift, FirstAnomalies, PowerProblem, WatcherActive, RestrictedArea, FinalTask, Escape }
 public enum WatcherState { Dormant, Awake, Stalking, Hunting, CloseThreat, Disabled }
 public static class NightRules {
  public static int ShipmentTarget(int players)=>players<=1?4:players==2?5:6;
  public static float InteractionDuration(int players)=>players<=1?.8f:players==2?1f:1.2f;
  public static float WatcherCooldown(int players,WatcherState state) {
   float value=state==WatcherState.Hunting?5.5f:state==WatcherState.CloseThreat?3.5f:9f;
   return value*(players<=1?1.55f:players>=3?.8f:1f);
  }
  public static float HeavyCarrySpeed(int players)=>players<=1?.48f:players==2?.68f:.78f;
  public static WatcherState StateForPhase(NightPhase phase)=>phase==NightPhase.NormalShift?WatcherState.Dormant:phase<NightPhase.WatcherActive?WatcherState.Awake:phase<NightPhase.FinalTask?WatcherState.Stalking:WatcherState.Hunting;
  public static bool CanRestorePower(bool fuseInserted,bool alreadyPowered)=>fuseInserted&&!alreadyPowered;
  public static bool CanAdvance(NightPhase phase,int crates,int crateTarget,int placements,int inspections,bool fuse,bool breaker,bool sculpture,bool security,bool unknown,bool archiveScan,bool archiveReturn,bool finalObjects,bool finalCase,bool finalScan) {
   return phase switch {
    NightPhase.NormalShift => crates>=crateTarget && placements>=3 && inspections>=5,
    NightPhase.FirstAnomalies => true,
    NightPhase.PowerProblem => fuse && breaker,
    NightPhase.WatcherActive => sculpture && security && unknown,
    NightPhase.RestrictedArea => archiveScan && archiveReturn,
    NightPhase.FinalTask => finalObjects && finalCase && finalScan && breaker,
    _ => false
   };
  }
 }
}
