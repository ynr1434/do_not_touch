#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode.Components;
namespace DoNotTouch {
 // Explicit opt-in visual review, never enabled in ordinary gameplay.
 public class AtmosphereReview : MonoBehaviour {
  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
  static void Boot(){if(Environment.GetCommandLineArgs().Contains("-dnt-atmosphere-review"))new GameObject("Atmosphere visual review").AddComponent<AtmosphereReview>();}
  IEnumerator Start(){yield return null;FindFirstObjectByType<NetworkSession>().Host();while(!PlayerController.Local)yield return null;PlayerController.Local.SetPause(true);Place(0);}
  void Update() {
   var k=Keyboard.current;if(k==null || !PlayerController.Local)return;
   if(k.rightArrowKey.wasPressedThisFrame)PlayerController.Local.DebugAim(PlayerController.Local.transform.eulerAngles.y+90);
   if(k.leftArrowKey.wasPressedThisFrame)PlayerController.Local.DebugAim(PlayerController.Local.transform.eulerAngles.y-90);
   if(FindFirstObjectByType<NightShiftDirector>()){if(k.f1Key.wasPressedThisFrame)Place(0);if(k.f2Key.wasPressedThisFrame)Place(1);if(k.f3Key.wasPressedThisFrame)Place(2);if(k.f4Key.wasPressedThisFrame)Place(3);if(k.f5Key.wasPressedThisFrame)Place(4);if(k.f6Key.wasPressedThisFrame)Place(5);if(k.f7Key.wasPressedThisFrame)Place(6);if(k.f8Key.wasPressedThisFrame)Place(7);if(k.f9Key.wasPressedThisFrame)Place(8);if(k.f10Key.wasPressedThisFrame)Place(9);if(k.f11Key.wasPressedThisFrame)Place(10);if(k.f12Key.wasPressedThisFrame){var n=FindFirstObjectByType<NightShiftDirector>();n.ForcePhaseForTests(NightPhase.RestrictedArea);n.PowerRestored.Value=false;FindFirstObjectByType<HorrorDirector>().ServerBeginEvent(HorrorEventKind.SectionBlackout,8,5);}return;}
   if(k.f1Key.wasPressedThisFrame)Place(0);if(k.f2Key.wasPressedThisFrame)Place(1);if(k.f3Key.wasPressedThisFrame)Place(2);
   if(k.f4Key.wasPressedThisFrame)Place(3);if(k.f5Key.wasPressedThisFrame)Place(4);if(k.f6Key.wasPressedThisFrame)Place(5);
   if(k.f7Key.wasPressedThisFrame)FindFirstObjectByType<HorrorDirector>().ServerBeginEvent(HorrorEventKind.SectionBlackout,4,5);
  }
  void Place(int index) {
   var positions=FindFirstObjectByType<NightShiftDirector>()?new[]{new Vector3(-25,1,-20),new Vector3(-10,1,-20),new Vector3(-24,1,-8),new Vector3(5,1,-7),new Vector3(24,1,-6),new Vector3(11,1,14),new Vector3(-14,1,18),new Vector3(-11,1,-8),new Vector3(25,1,-20),new Vector3(-26,1,11),new Vector3(8,1,-20)}:new[]{new Vector3(-12,1,-7),new Vector3(0,1,1.5f),new Vector3(10,1,1.5f),new Vector3(-10,1,1.5f),new Vector3(0,1,-8),new Vector3(10,1,-9)};
   var player=PlayerController.Local;var body=player.GetComponent<Rigidbody>();body.constraints=RigidbodyConstraints.FreezeAll;
   player.GetComponent<NetworkTransform>().Teleport(positions[index],Quaternion.identity,Vector3.one);body.position=positions[index];player.transform.position=positions[index];
   // The review hides menus via Canvas only; it doesn't replace the gameplay camera.
   player.SetPause(false);player.DebugAim(0);FindFirstObjectByType<PrototypeUI>().GetComponent<Canvas>().enabled=true;
   Debug.Log("DNT_ATMOSPHERE_REVIEW "+index);
  }
 }
}
#endif
