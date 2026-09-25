using Unity.Netcode;
using UnityEngine;
namespace DoNotTouch {
 public class ScannableExhibit : NetworkBehaviour, IInteractable {
  public string exhibitId="0241";
  public ExhibitRegistry registry;
  public bool unknownObjective, archiveObjective, finalObjective;
  public NetworkVariable<bool> Scanned=new();
  public bool CanInteract(PlayerController player)=>player && player.ScannerEquipped.Value;
  public string GetInteractionPrompt(PlayerController player)=>player.ScannerEquipped.Value?"[E] Сканировать экспонат":"Нужен сканер из комнаты персонала";
  public void Interact(PlayerController player) {
   if(!IsServer||!CanInteract(player))return;
   Scanned.Value=true;var identity=GetComponent<ExhibitIdentity>();player.SendScanResult(identity?identity.Read():registry?registry.Read(exhibitId):"НЕИЗВЕСТНЫЙ ОБЪЕКТ\nЗапись в реестре отсутствует.");
   if(NightShiftDirector.Instance)NightShiftDirector.Instance.RegisterScan(unknownObjective,archiveObjective,finalObjective);
  }
 }
}
