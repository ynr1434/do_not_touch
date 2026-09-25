using Unity.Netcode;
namespace DoNotTouch {
 public class DisplayCase : NetworkBehaviour, IInteractable {
  public NetworkVariable<bool> Inspected=new();
  public bool CanInteract(PlayerController p)=>!Inspected.Value;
  public string GetInteractionPrompt(PlayerController p)=>Inspected.Value?"Осмотрено":"[E] Осмотреть витрину";
  public void Interact(PlayerController p) {
   if(IsServer && CanInteract(p) && ShiftManager.Instance.Inspect(NetworkObjectId)) Inspected.Value=true;
  }
 }
}
