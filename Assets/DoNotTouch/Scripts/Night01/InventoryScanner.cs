using Unity.Netcode;
using UnityEngine;
namespace DoNotTouch {
 public class InventoryScanner : NetworkBehaviour, IInteractable {
  public NetworkVariable<bool> Taken=new();
  public bool CanInteract(PlayerController player)=>!player.ScannerEquipped.Value;
  public string GetInteractionPrompt(PlayerController player)=>player.ScannerEquipped.Value?"Сканер выдан. [E] — сканировать":"[E] Взять сканер";
  public void Interact(PlayerController player){if(IsServer&&CanInteract(player)){Taken.Value=true;player.ScannerEquipped.Value=true;NightShiftDirector.Instance?.ScannerPickedUp(player);}}
 }
}
