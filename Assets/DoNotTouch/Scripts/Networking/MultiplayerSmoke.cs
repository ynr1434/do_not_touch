#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Linq;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
namespace DoNotTouch {
 // Opt-in development-build harness. Never runs during normal gameplay.
 public class MultiplayerSmoke : MonoBehaviour {
  bool host;
  int expectedPlayers=4;
  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
  static void Boot() {
   var args=Environment.GetCommandLineArgs();
   if(!args.Contains("-dnt-smoke-host")&&!args.Contains("-dnt-smoke-client"))return;
   var go=new GameObject("Multiplayer smoke harness");var run=go.AddComponent<MultiplayerSmoke>();run.host=args.Contains("-dnt-smoke-host");
   if(args.Contains("-dnt-smoke-two-players"))run.expectedPlayers=2;
  }
  IEnumerator Start() {
   yield return null;
   var session=FindFirstObjectByType<NetworkSession>();
   if(host)session.Host();else session.Join("127.0.0.1");
   float deadline=Time.realtimeSinceStartup+75;
   while(!PlayerController.Local && Time.realtimeSinceStartup<deadline)yield return null;
   if(!PlayerController.Local){Fail("connection timeout");yield break;}
   PlayerController.Local.SetPause(true);
   yield return new WaitForSeconds(1.2f);
   if(!ShiftManager.Instance.exitZone.Contains(PlayerController.Local.transform.position)) {Fail("player spawned outside Staff Room");yield break;}
   if(Vector3.Distance(PlayerController.Local.Eye,PlayerController.Local.viewCamera.transform.position)>.15f) {Fail("camera detached from player");yield break;}
   Debug.Log("DNT_SMOKE_SPAWN_PASS: player and camera in Staff Room");
   var manager=NetworkManager.Singleton;var shift=ShiftManager.Instance;
   var night=FindFirstObjectByType<NightShiftDirector>();
   if(host) {
    // The final client always joins after the blackout is staged.
    while(manager.ConnectedClients.Count<expectedPlayers-1 && Time.realtimeSinceStartup<deadline)yield return null;
    if(manager.ConnectedClients.Count<expectedPlayers-1){Fail("initial clients missing");yield break;}
    yield return new WaitForSeconds(2);
    PlayerController.Local.FlashlightOn.Value=true;
    Debug.Log("DNT_FLASHLIGHT_HOST_ON: authoritative flashlight state enabled");
    foreach(var c in manager.ConnectedClients.Values) Place(c.PlayerObject,(night?new Vector3(5,1,-5):new Vector3(0,1,-6))+(Vector3.right*(int)c.ClientId));
    if(night) {
     night.ForcePhaseForTests(NightPhase.RestrictedArea);night.PowerRestored.Value=false;night.FuseInserted.Value=true;night.SecurityChecked.Value=true;night.UnknownScanned.Value=true;night.KeycardFound.Value=true;
     var watcher=night.watcher;foreach(var c in manager.ConnectedClients.Values)c.PlayerObject.GetComponent<PlayerController>().DebugAim(180);
     float movementDeadline=Time.realtimeSinceStartup+12;while(watcher.MoveCount.Value==0&&Time.realtimeSinceStartup<movementDeadline)yield return null;if(watcher.MoveCount.Value==0){Fail("Watcher did not move using normal server timer");yield break;}Debug.Log("DNT_WATCHER_REAL_MOVE_PASS");
     yield return new WaitForSeconds(2.6f);
     var eventDirector=FindFirstObjectByType<HorrorDirector>();if(!eventDirector.ServerBeginEvent(HorrorEventKind.SectionBlackout,8,12)){Fail("Night 01 blackout did not start");yield break;}
     Debug.Log("DNT_SMOKE_STAGED");
    } else {
    int index=0;foreach(var item in PickupObject.Active){
     Vector3 point=item.kind=="crate"?shift.crateZone.transform.position+Vector3.right*(index++-1):shift.statueZone.transform.position;
     item.GetComponent<NetworkTransform>().Teleport(point,Quaternion.identity,item.transform.localScale);
     item.Body.position=point;item.Body.linearVelocity=Vector3.zero;
    }
    foreach(var display in FindObjectsByType<DisplayCase>(FindObjectsSortMode.None))display.Interact(PlayerController.Local);
    foreach(var door in FindObjectsByType<MuseumDoor>(FindObjectsSortMode.None))door.Interact(PlayerController.Local);
    var watcher=FindFirstObjectByType<Watcher>();watcher.Activated.Value=true;watcher.MoveCount.Value=1;
    watcher.GetComponent<NetworkTransform>().Teleport(watcher.positions[1].position,Quaternion.identity,Vector3.one);
    watcher.PositionIndex.Value=1;
    shift.Elapsed.Value=120;
    var horror=FindFirstObjectByType<HorrorDirector>();
    if(horror && !horror.ServerBeginEvent(HorrorEventKind.SectionBlackout,4,12)){Fail("horror event did not start");yield break;}
    Debug.Log("DNT_SMOKE_STAGED");
    }
    while(manager.ConnectedClients.Count<expectedPlayers && Time.realtimeSinceStartup<deadline)yield return null;
    if(manager.ConnectedClients.Count<expectedPlayers){Fail("late client missing");yield break;}
    yield return new WaitForSeconds(3);
    PlayerController.Local.FlashlightOn.Value=false;
    Debug.Log("DNT_FLASHLIGHT_HOST_OFF: authoritative flashlight state disabled");
    yield return new WaitForSeconds(night?10:1);
    if(night){night.PowerRestored.Value=true;night.ArchiveScanned.Value=true;night.ArchiveReturned.Value=true;night.FinalCaseSecured.Value=true;night.FinalScanned.Value=true;night.FinalObjectsReturned.Value=true;night.ForcePhaseForTests(NightPhase.Escape);}
    foreach(var c in manager.ConnectedClients.Values) Place(c.PlayerObject,shift.exitZone.transform.position+Vector3.right*((int)c.ClientId-1.5f)*.5f);
    yield return new WaitForSeconds(3);
    if(shift.Result.Value!=1){Fail("completion not reached");yield break;}
    Debug.Log($"DNT_SMOKE_HOST_PASS: {expectedPlayers} workers, objectives, completion");
    yield return new WaitForSeconds(2);Application.Quit(0);
   } else {
    bool snapshot=false;var horror=FindFirstObjectByType<HorrorDirector>();bool horrorSnapshot=!horror;bool flashlightOnSnapshot=false,flashlightOffSnapshot=false;
    while(Time.realtimeSinceStartup<deadline) {
     var remote=PlayerController.Active.FirstOrDefault(p=>!p.IsOwner);
     if(!flashlightOnSnapshot&&remote&&remote.FlashlightOn.Value&&remote.flashlight&&remote.flashlight.enabled){flashlightOnSnapshot=true;Debug.Log("DNT_FLASHLIGHT_ON_SYNC_PASS: remote flashlight ON state and Light component replicated");}
     if(flashlightOnSnapshot&&!flashlightOffSnapshot&&remote&&!remote.FlashlightOn.Value&&remote.flashlight&&!remote.flashlight.enabled){flashlightOffSnapshot=true;Debug.Log("DNT_FLASHLIGHT_OFF_SYNC_PASS: remote flashlight OFF state and Light component replicated");}
     if(night&&night.Phase.Value==(int)NightPhase.RestrictedArea)PlayerController.Local.DebugAim(180);
     int outageRoom=night?8:4;
     if(horror && !horrorSnapshot && horror.EventActive && horror.EventKind.Value==(int)HorrorEventKind.SectionBlackout &&
       horror.EventRoom.Value==outageRoom && horror.lamps.Where(l=>l.room==outageRoom&&l.normallyOn).All(l=>l.source.intensity<l.intensity*.03f) &&
       horror.lamps.Where(l=>l.room==0).All(l=>Mathf.Abs(l.source.intensity-l.intensity)<.02f) && (night||FindFirstObjectByType<WatcherStaging>()?.IsSpawned==true)) {
      horrorSnapshot=true;Debug.Log("DNT_HORROR_EVENT_PASS: shared blackout timeline, safe room, staged Watcher and late join");
     }
     if(night && night.Phase.Value==(int)NightPhase.RestrictedArea&&!night.PowerRestored.Value&&night.FuseInserted.Value&&night.watcher.State.Value==(int)WatcherState.Stalking&&night.watcher.MoveCount.Value>=1&&Vector3.Distance(night.watcher.transform.position,night.watcher.positions[night.watcher.PositionIndex.Value].position)<.3f){if(!snapshot)Debug.Log("DNT_NIGHT01_SNAPSHOT_PASS: phase, power, Watcher state and anchor synchronized");snapshot=true;}
     if(!night && shift.Crates.Value==3 && shift.Inspections.Value==3 && shift.Statues.Value==1 && shift.Elapsed.Value>=120 &&
        FindFirstObjectByType<Watcher>().Activated.Value && FindFirstObjectByType<Watcher>().MoveCount.Value>0 &&
        FindObjectsByType<MuseumDoor>(FindObjectsSortMode.None).All(d=>d.Open.Value) &&
        PickupObject.Active.All(p=>p.Body.isKinematic) &&
        PickupObject.Active.Where(p=>p.kind=="crate").All(p=>shift.crateZone.Contains(p.transform.position))) {
      if(!snapshot)Debug.Log("DNT_SMOKE_SNAPSHOT_PASS: tasks, clock, doors, Watcher, server-owned physics");snapshot=true;
     }
     bool restored=!night||(night.PowerRestored.Value&&night.Phase.Value==(int)NightPhase.Escape&&horror.lamps.Where(l=>l.room==8&&l.normallyOn).All(l=>Mathf.Abs(l.source.intensity-l.intensity)<.02f));
     if(shift.Result.Value==1 && snapshot && horrorSnapshot && restored && flashlightOnSnapshot && flashlightOffSnapshot && PlayerController.Active.Count==expectedPlayers){if(night)Debug.Log("DNT_NIGHT01_RESTORE_PASS: power, lamps, Escape phase and completion replicated");Debug.Log($"DNT_SMOKE_CLIENT_PASS: {expectedPlayers} players and completion replicated");Application.Quit(0);yield break;}
     yield return null;
    }
    Fail("replication timeout");
   }
  }
  static void Place(NetworkObject player,Vector3 point) {
   player.GetComponent<NetworkTransform>().Teleport(point,Quaternion.identity,Vector3.one);
   player.GetComponent<Rigidbody>().position=point;
  }
  static void Fail(string why){Debug.LogError("DNT_SMOKE_FAIL: "+why);Application.Quit(2);}
 }
}
#endif
