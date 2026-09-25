using System.Collections;
using System.Linq;
using NUnit.Framework;
using Unity.Netcode;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
namespace DoNotTouch.Tests {
 [TestFixture("PrototypeMuseum")]
 [TestFixture("PrototypeMuseum_Horror")]
 public class PrototypePlayTests {
  readonly string sceneName;
  public PrototypePlayTests(string sceneName){this.sceneName=sceneName;}
  NetworkManager manager;
  PlayerController player;
  [UnitySetUp] public IEnumerator StartShift() {
   yield return SceneManager.LoadSceneAsync(sceneName,LoadSceneMode.Single);
   manager=Object.FindFirstObjectByType<NetworkManager>();
   Assert.IsTrue(manager.StartHost());
   for(int i=0;i<60 && !PlayerController.Local;i++)yield return null;
   player=PlayerController.Local;Assert.IsNotNull(player);
   player.SetPause(true);
   yield return new WaitForFixedUpdate();
  }
  [UnityTearDown] public IEnumerator StopShift() {
   if(manager){manager.Shutdown();yield return null;Object.Destroy(manager.gameObject);}
   yield return null;
  }
  void PlacePlayer(Vector3 position,Quaternion rotation) {
   player.GetComponent<Rigidbody>().constraints=RigidbodyConstraints.FreezeAll;
   player.GetComponent<Rigidbody>().position=position;player.transform.SetPositionAndRotation(position,rotation);
   UnityEngine.Physics.SyncTransforms();
  }
  [UnityTest] public IEnumerator InputMovesLooksSprintsCrouchesAndJumps() {
   var settings=InputSystem.settings;var originalBackground=settings.backgroundBehavior;
   settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
#if UNITY_EDITOR
   var originalEditor=settings.editorInputBehaviorInPlayMode;
   settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
   var keyboard=InputSystem.AddDevice<Keyboard>();var mouse=InputSystem.AddDevice<Mouse>();
   try {
    player.GetComponent<Rigidbody>().position=new Vector3(0,1,-7);player.SetPause(false);
    yield return new WaitForSeconds(.2f);
    Vector3 start=player.transform.position;
    InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.W));
    yield return new WaitForSeconds(.3f);Assert.Greater(player.transform.position.z,start.z+.5f);
    InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.W,Key.LeftShift));
    yield return new WaitForSeconds(.15f);Assert.Greater(player.GetComponent<Rigidbody>().linearVelocity.z,5);
    InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.C));
    yield return new WaitForSeconds(.15f);Assert.IsTrue(player.Crouched.Value);
    InputSystem.QueueStateEvent(keyboard,new KeyboardState());
    yield return new WaitForSeconds(.15f);Assert.IsFalse(player.Crouched.Value);
    float height=player.transform.position.y;
    InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Space));
    yield return new WaitForSeconds(.15f);Assert.Greater(player.transform.position.y,height+.2f);
    Quaternion view=player.head.rotation;
    InputSystem.QueueStateEvent(mouse,new MouseState{delta=new Vector2(100,30)});
    yield return null;yield return null;Assert.Greater(Quaternion.Angle(view,player.head.rotation),1);
   } finally {InputSystem.RemoveDevice(keyboard);InputSystem.RemoveDevice(mouse);settings.backgroundBehavior=originalBackground;
#if UNITY_EDITOR
    settings.editorInputBehaviorInPlayMode=originalEditor;
#endif
   }
  }
  [UnityTest] public IEnumerator InteractionRayFindsDoorAndStopsAtWall() {
   PlacePlayer(new Vector3(0,1,-2),Quaternion.identity);
   Assert.IsTrue(player.RayTarget(player.Eye,Vector3.forward,out var target));
   Assert.IsInstanceOf<MuseumDoor>(target);StringAssert.Contains("[E]",target.GetInteractionPrompt(player));
   PlacePlayer(new Vector3(4,1,-2),Quaternion.identity);
   Assert.IsFalse(player.RayTarget(player.Eye,Vector3.forward,out _));yield return null;
  }
  [UnityTest] public IEnumerator PlayerSpawnsInStaffRoomAndKeepsPhysicalPosition() {
   var spawn=ShiftManager.Instance.spawnPoints[0].position;
   yield return new WaitForSeconds(.4f);
   Assert.Less(Vector2.Distance(new Vector2(player.transform.position.x,player.transform.position.z),new Vector2(spawn.x,spawn.z)),.15f);
   Assert.Greater(player.GetComponent<Rigidbody>().position.y,.7f);
   Assert.IsTrue(ShiftManager.Instance.exitZone.Contains(player.transform.position));
  }
  [UnityTest] public IEnumerator SessionCanRestartAndHostAgain() {
   var session=Object.FindFirstObjectByType<NetworkSession>();session.Restart();
   yield return new WaitForSeconds(.5f);
   manager=Object.FindFirstObjectByType<NetworkManager>();Assert.IsNotNull(manager);
   Assert.AreEqual(1,Object.FindObjectsByType<NetworkManager>(FindObjectsSortMode.None).Length);
   Assert.IsTrue(manager.StartHost());yield return null;Assert.IsNotNull(PlayerController.Local);
  }
  [UnityTest] public IEnumerator PickupCanGrabReleaseAndThrow() {
   PlacePlayer(new Vector3(0,1,-5),Quaternion.identity);
   var crate=PickupObject.Active.First(p=>p.kind=="crate");crate.Body.position=player.Eye+Vector3.forward*2;crate.Body.linearVelocity=Vector3.zero;
   UnityEngine.Physics.SyncTransforms();
   Assert.IsTrue(crate.TryGrab(player));Assert.AreEqual(player.OwnerClientId,crate.Holder.Value);
   Assert.IsFalse(crate.Body.isKinematic);Assert.IsFalse(crate.TryGrab(player));
   crate.Release(false);Assert.IsFalse(crate.IsHeld);
   Assert.IsTrue(crate.TryGrab(player));crate.Release(true);
   yield return new WaitForFixedUpdate();Assert.Greater(crate.Body.linearVelocity.z,0);
  }
  [UnityTest] public IEnumerator DeliveryCountsOnlyRequiredReleasedObjectsAndRemovesDepartures() {
   var zone=ShiftManager.Instance.crateZone;
   var crate=PickupObject.Active.First(p=>p.kind=="crate");
   crate.transform.position=crate.Body.position=zone.transform.position;
   UnityEngine.Physics.SyncTransforms();Assert.AreEqual(1,zone.CountDelivered());
   crate.Holder.Value=player.OwnerClientId;Assert.AreEqual(0,zone.CountDelivered());crate.Holder.Value=ulong.MaxValue;
   var statue=PickupObject.Active.First(p=>p.kind=="statue");statue.transform.position=statue.Body.position=zone.transform.position;UnityEngine.Physics.SyncTransforms();Assert.AreEqual(1,zone.CountDelivered());
   crate.transform.position=crate.Body.position=new Vector3(-12,1,4);UnityEngine.Physics.SyncTransforms();Assert.AreEqual(0,zone.CountDelivered());
   yield return null;
  }
  [UnityTest] public IEnumerator WatcherStaysWhenObservedAndMovesWhenUnobserved() {
   PlacePlayer(new Vector3(0,1,2),Quaternion.identity);
   var watcher=Object.FindFirstObjectByType<Watcher>();watcher.Activated.Value=true;
   // Activation deadline was chosen during spawn; advance it without waiting a minute.
   typeof(Watcher).GetField("nextMove",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).SetValue(watcher,0f);
   Assert.IsTrue(watcher.IsObserved());var position=watcher.transform.position;
   yield return new WaitForSeconds(.35f);Assert.AreEqual(position,watcher.transform.position);
   PlacePlayer(new Vector3(-12,1,-7),Quaternion.identity);
   Assert.IsFalse(watcher.IsObserved());
   yield return new WaitForSeconds(.4f);Assert.Greater(watcher.MoveCount.Value,0);
  }
  [UnityTest] public IEnumerator InspectDoorAndCompleteShift() {
   var shift=ShiftManager.Instance;
   var display=Object.FindObjectsByType<DisplayCase>(FindObjectsSortMode.None);
   foreach(var d in display){d.Interact(player);d.Interact(player);}Assert.AreEqual(3,shift.Inspections.Value);
   var door=Object.FindFirstObjectByType<MuseumDoor>();door.Interact(player);Assert.IsTrue(door.Open.Value);door.Interact(player);Assert.IsFalse(door.Open.Value);
   int i=0;foreach(var item in PickupObject.Active){item.transform.position=item.Body.position=item.kind=="crate"?shift.crateZone.transform.position+Vector3.right*(i++-1):shift.statueZone.transform.position;item.Body.isKinematic=true;}
   PlacePlayer(shift.exitZone.transform.position,Quaternion.identity);UnityEngine.Physics.SyncTransforms();
   yield return new WaitForSeconds(.35f);Assert.IsTrue(shift.AllTasks);Assert.AreEqual(1,shift.Result.Value);Assert.Greater(shift.Elapsed.Value,0);
  }
 }
}
