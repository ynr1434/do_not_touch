using Unity.Netcode;
using UnityEngine;
namespace DoNotTouch {
 public enum NightInteraction { FusePickup, FuseSocket, Breaker, SecurityConsole, Keycard, FinalCase, Radio, ShiftBoard }
 public class NightInteractable : NetworkBehaviour, IInteractable {
  public NightInteraction mode;
  public NetworkVariable<bool> Used=new();
  NightShiftDirector Director=>NightShiftDirector.Instance;
  public bool CanInteract(PlayerController player) {
   if(!Director||(Used.Value&&mode!=NightInteraction.SecurityConsole))return false;
   return mode switch {
    NightInteraction.FusePickup => Director.Phase.Value>=(int)NightPhase.PowerProblem&&!player.HasFuse.Value,
    NightInteraction.FuseSocket => player.HasFuse.Value&&!Director.FuseInserted.Value,
    NightInteraction.Breaker => Director.FuseInserted.Value&&!Director.PowerRestored.Value,
    NightInteraction.SecurityConsole => Director.Phase.Value>=(int)NightPhase.WatcherActive,
    NightInteraction.Keycard => Director.Phase.Value>=(int)NightPhase.WatcherActive,
    NightInteraction.FinalCase => Director.Phase.Value>=(int)NightPhase.FinalTask,
    NightInteraction.ShiftBoard => !Director.OnboardingStarted.Value,
    _ => true
   };
  }
  public string GetInteractionPrompt(PlayerController player) {
   if(Used.Value&&mode!=NightInteraction.SecurityConsole)return mode==NightInteraction.Breaker?"Питание восстановлено":"Готово";
   return mode switch {
    NightInteraction.FusePickup => CanInteract(player)?"[E] Взять предохранитель":"Запасной предохранитель: на случай аварии",
    NightInteraction.FuseSocket => player.HasFuse.Value?"[E] Вставить предохранитель":"Нужен запасной предохранитель",
    NightInteraction.Breaker => Director&&Director.FuseInserted.Value?"[E] Включить рубильник Галереи B":"Сначала вставьте предохранитель",
    NightInteraction.SecurityConsole => "[E] Проверить камеры",
    NightInteraction.Keycard => "[E] Взять пропуск в архив",
    NightInteraction.FinalCase => "[E] Закрыть витрину",
    NightInteraction.ShiftBoard => "[E] Начать смену",
    _ => "[E] Проверить радио"
   };
  }
  public void Interact(PlayerController player) {
   if(!IsServer||!CanInteract(player))return;
   Used.Value=true;
   switch(mode) {
    case NightInteraction.FusePickup: player.HasFuse.Value=true;player.SendScanResult("Предохранитель взят.");break;
    case NightInteraction.FuseSocket: player.HasFuse.Value=false;Director.FuseInserted.Value=true;player.SendScanResult("Предохранитель установлен.");break;
    case NightInteraction.Breaker: Director.RestorePower();player.SendScanResult("Питание восстановлено.");break;
    case NightInteraction.SecurityConsole: Director.SecurityChecked.Value=true;Director.CameraFeed.Value=(Director.CameraFeed.Value+1)%5;player.SendScanResult($"КАМЕРА {Director.CameraFeed.Value+1:00}\nПРОВЕРКА ПОЗИЦИИ СТАТУИ");break;
    case NightInteraction.Keycard: Director.KeycardFound.Value=true;break;
    case NightInteraction.FinalCase: Director.FinalCaseSecured.Value=true;break;
    case NightInteraction.ShiftBoard: Director.BeginOnboarding();break;
   }
  }
 }
}
