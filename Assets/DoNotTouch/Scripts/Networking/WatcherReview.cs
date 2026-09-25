using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
namespace DoNotTouch {
 // Opt-in development harness. It drives normal server Update, never the movement timer.
 public class WatcherReview : MonoBehaviour {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
  Watcher watcher;PlayerController player;bool automatic,visible;string stage="Ожидание";
  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)] static void Install(){var args=Environment.GetCommandLineArgs();if(Array.IndexOf(args,"-dnt-watcher-debug")>=0||Array.IndexOf(args,"-dnt-watcher-solo")>=0)new GameObject("Watcher review").AddComponent<WatcherReview>();}
  IEnumerator Start(){var args=Environment.GetCommandLineArgs();bool solo=Array.IndexOf(args,"-dnt-watcher-solo")>=0;automatic=Array.IndexOf(args,"-dnt-watcher-auto")>=0;yield return null;if(solo)FindFirstObjectByType<NetworkSession>().Host();while(!PlayerController.Local)yield return null;player=PlayerController.Local;watcher=NightShiftDirector.Instance.watcher;if(!solo)yield break;
   var anchors=new Transform[4];for(int i=0;i<4;i++){anchors[i]=new GameObject("Solo anchor "+i).transform;anchors[i].position=new Vector3(5,0,7-i*4);}
   watcher.positions=anchors;watcher.EnableSoloTest();var body=player.GetComponent<Rigidbody>();body.position=player.transform.position=new Vector3(5,1,-8);body.constraints=RigidbodyConstraints.FreezeAll;player.SetPause(false);player.DebugAim(0);Physics.SyncTransforms();stage="Наблюдение: активация через 10 секунд";
   if(!automatic)yield break;yield return new WaitForSeconds(15);if(!watcher.Activated.Value||watcher.MoveCount.Value!=0){Fail("activation/freeze");yield break;}Debug.Log("WATCHER_SOLO_ACTIVATION_PASS");stage="Отворачиваемся";player.DebugAim(180);yield return new WaitForSeconds(4.5f);int moves=watcher.MoveCount.Value;if(moves!=1){Fail("first movement "+moves);yield break;}Debug.Log("WATCHER_SOLO_MOVE_PASS position="+watcher.transform.position);player.DebugAim(0);stage="Наблюдение: стоять";yield return new WaitForSeconds(5);if(watcher.MoveCount.Value!=moves){Fail("observed movement");yield break;}Debug.Log("WATCHER_SOLO_FREEZE_PASS");player.DebugAim(180);stage="Отворачиваемся повторно";yield return new WaitForSeconds(4.5f);if(watcher.MoveCount.Value<=moves){Fail("resume");yield break;}Debug.Log("WATCHER_SOLO_RESUME_PASS position="+watcher.transform.position);Debug.Log("WATCHER_SOLO_PASS");Application.Quit(0);
  }
  void Fail(string reason){Debug.LogError("WATCHER_SOLO_FAIL "+reason);Application.Quit(2);}
  void Update(){if(!player||automatic||Keyboard.current==null)return;if(Keyboard.current.f1Key.wasPressedThisFrame)player.DebugAim(0);if(Keyboard.current.f2Key.wasPressedThisFrame)player.DebugAim(180);if(Keyboard.current.f3Key.wasPressedThisFrame)visible=!visible;}
  void OnGUI(){if(!visible||!watcher||!player)return;GUI.Box(new Rect(760,80,480,255),"WATCHER DEBUG");GUI.Label(new Rect(780,110,445,220),$"Состояние: {(WatcherState)watcher.State.Value}\nНаблюдается: {watcher.IsObserved()}\nРасстояние: {Vector3.Distance(watcher.transform.position,player.transform.position):F1}\nТочка: {watcher.PositionIndex.Value} → {watcher.NextAnchor}\nДо шага: {watcher.Cooldown:F1} с\nФаза активации: {NightShiftDirector.Instance?.CurrentStep(ShiftManager.Instance)}\nПеремещений: {watcher.MoveCount.Value}\n{stage}\nF1 — смотреть; F2 — отвернуться; F3 — панель");}
#endif
 }
}
