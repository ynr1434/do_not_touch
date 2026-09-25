using System.Collections;
using System.Linq;
using NUnit.Framework;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
namespace DoNotTouch.Tests {
 public class HorrorPlayTests {
  NetworkManager manager;
  [UnitySetUp] public IEnumerator StartMuseum(){
   yield return SceneManager.LoadSceneAsync("PrototypeMuseum_Horror");manager=Object.FindFirstObjectByType<NetworkManager>();Assert.IsTrue(manager.StartHost());yield return null;PlayerController.Local.SetPause(true);
  }
  [UnityTearDown] public IEnumerator StopMuseum(){if(manager){manager.Shutdown();yield return null;Object.Destroy(manager.gameObject);}yield return null;}
  [UnityTest] public IEnumerator SectionLightsRecoverWithoutBlackeningStaffRoom() {
   var d=Object.FindFirstObjectByType<HorrorDirector>();Assert.IsTrue(d.ambience.Ready);
   Assert.IsTrue(d.ServerBeginEvent(HorrorEventKind.SectionBlackout,4,2));
   Assert.IsFalse(d.ServerBeginEvent(HorrorEventKind.Flicker,5,2));
   yield return new WaitForSeconds(.6f);
   Assert.IsTrue(d.EventActive);
   foreach(var lamp in d.lamps.Where(l=>l.room==4&&l.normallyOn))Assert.Less(lamp.source.intensity,lamp.intensity*.03f);
   foreach(var lamp in d.lamps.Where(l=>l.room==0))Assert.AreEqual(lamp.intensity,lamp.source.intensity,.02f);
   yield return new WaitForSeconds(2);
   foreach(var lamp in d.lamps.Where(l=>l.room==4&&l.normallyOn))Assert.AreEqual(lamp.intensity,lamp.source.intensity,.02f);
  }
  [UnityTest] public IEnumerator VisibleHeadPreventsMovementBehindLowExhibit() {
   var d=Object.FindFirstObjectByType<HorrorDirector>();var p=PlayerController.Local;var body=p.GetComponent<Rigidbody>();
   body.constraints=RigidbodyConstraints.FreezeAll;body.position=new Vector3(0,1,2);p.transform.SetPositionAndRotation(body.position,Quaternion.identity);
   var cover=GameObject.CreatePrimitive(PrimitiveType.Cube);cover.transform.position=new Vector3(0,.84f,4);cover.transform.localScale=new Vector3(2,1.68f,.1f);
   UnityEngine.Physics.SyncTransforms();
   try {
    Assert.IsTrue(UnityEngine.Physics.Linecast(p.Eye,d.watcher.transform.position+Vector3.up*1.3f,out var hit,~(1<<8),QueryTriggerInteraction.Ignore));
    Assert.AreEqual(cover,hit.collider.gameObject);Assert.IsTrue(d.watcher.IsObserved());
    d.watcher.Activated.Value=true;d.watcher.RequestUnobservedMove();var position=d.watcher.transform.position;
    yield return new WaitForSeconds(.7f);Assert.AreEqual(position,d.watcher.transform.position);
   } finally {Object.Destroy(cover);}
  }
  [UnityTest] public IEnumerator BlackoutNeverOverridesWatcherVisibility() {
   var d=Object.FindFirstObjectByType<HorrorDirector>();var watcher=d.watcher;var p=PlayerController.Local;
   var body=p.GetComponent<Rigidbody>();body.constraints=RigidbodyConstraints.FreezeAll;body.position=new Vector3(0,1,2);p.transform.SetPositionAndRotation(body.position,Quaternion.identity);UnityEngine.Physics.SyncTransforms();
   watcher.Activated.Value=true;watcher.RequestUnobservedMove();Assert.IsTrue(watcher.IsObserved());
   var point=watcher.transform.position;var rotation=watcher.transform.rotation;
   Assert.IsTrue(d.ServerBeginEvent(HorrorEventKind.BriefBlackout,4,2));
   yield return new WaitForSeconds(2.9f);
   Assert.AreEqual(point,watcher.transform.position);Assert.AreEqual(rotation,watcher.transform.rotation);
  }
 }
}
