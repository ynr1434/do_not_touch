using Unity.Netcode;
using UnityEngine;
namespace DoNotTouch {
 public class MuseumDoor : NetworkBehaviour, IInteractable {
  public Transform leaf;
  public NetworkVariable<bool> Open=new();
  public NetworkVariable<bool> Locked=new();
  public int unlockPhase;
  public string accessLabel="Для персонала";
  public bool CanInteract(PlayerController p)=>!Locked.Value && (!NightShiftDirector.Instance||NightShiftDirector.Instance.Phase.Value>=unlockPhase);
  public string GetInteractionPrompt(PlayerController p)=>CanInteract(p)?Open.Value?"[E] Закрыть дверь":"[E] Открыть дверь":$"Доступ закрыт. {accessLabel}";
  public void Interact(PlayerController p){if(IsServer&&CanInteract(p)) Open.Value=!Open.Value;}
  void Update(){if(leaf) leaf.localRotation=Quaternion.RotateTowards(leaf.localRotation,Quaternion.Euler(0,Open.Value?100:0,0),Time.deltaTime*160);}
 }
}
