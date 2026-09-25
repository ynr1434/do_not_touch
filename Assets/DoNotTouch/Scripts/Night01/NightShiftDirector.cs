using System.Linq;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
namespace DoNotTouch {
 public class NightShiftDirector : NetworkBehaviour {
  public static NightShiftDirector Instance {get;private set;}
  public NightObjectiveZone[] shipmentZones, placementZones;
  public NightObjectiveZone sculptureZone, archiveReturnZone, finalReturnZone;
  public MuseumDoor restrictedDoor;
  public HorrorDirector horror;
  public Watcher watcher;
  public Transform[] recoveryPoints;
  public NightAnomalyProp[] anomalies;
  public NetworkVariable<int> Phase=new(), Placements=new(), CameraFeed=new(), Workers=new(1);
  public NetworkVariable<bool> OnboardingStarted=new(), ScannerOnboardingShown=new();
  public NetworkVariable<bool> FuseInserted=new(),PowerRestored=new(true),SecurityChecked=new(),UnknownScanned=new(),ArchiveScanned=new(),ArchiveReturned=new(),KeycardFound=new(),FinalCaseSecured=new(),FinalScanned=new(),FinalObjectsReturned=new();
  public NetworkVariable<double> PhaseStarted=new();
  double nextTick;
  readonly System.Collections.Generic.Dictionary<ulong,Vector3> initialItemPositions=new();
  public int PlayerCount=>Workers.Value;
  public int CrateTarget=>NightRules.ShipmentTarget(PlayerCount);
  public bool MandatoryComplete=>Phase.Value>=(int)NightPhase.Escape;
  void Awake(){Instance=this;}
  public override void OnDestroy(){if(Instance==this)Instance=null;base.OnDestroy();}
  public override void OnNetworkSpawn(){if(IsServer){Phase.Value=0;PhaseStarted.Value=NetworkManager.ServerTime.Time;EnsureObjectiveAccess();}}
  public void BeginOnboarding(){if(IsServer)OnboardingStarted.Value=true;}
  public void ScannerPickedUp(PlayerController player){if(!IsServer||ScannerOnboardingShown.Value)return;ScannerOnboardingShown.Value=true;player.SendScanResult("Наведите сканер на маркировку экспоната и нажмите [E].");}
  public void ResetOnboarding(){if(!IsServer)return;Phase.Value=0;OnboardingStarted.Value=false;ScannerOnboardingShown.Value=false;FuseInserted.Value=false;PowerRestored.Value=true;SecurityChecked.Value=false;UnknownScanned.Value=false;ArchiveScanned.Value=false;ArchiveReturned.Value=false;KeycardFound.Value=false;FinalCaseSecured.Value=false;FinalScanned.Value=false;FinalObjectsReturned.Value=false;Placements.Value=0;var shift=ShiftManager.Instance;if(shift){shift.Crates.Value=0;shift.Inspections.Value=0;shift.Statues.Value=0;shift.Result.Value=0;shift.Witnessed.Value=0;}foreach(var board in FindObjectsByType<NightInteractable>(FindObjectsSortMode.None))if(board.mode==NightInteraction.ShiftBoard)board.Used.Value=false;foreach(var scanner in FindObjectsByType<InventoryScanner>(FindObjectsSortMode.None))scanner.Taken.Value=false;foreach(var player in PlayerController.Active)if(player){player.ScannerEquipped.Value=false;player.HasFuse.Value=false;}foreach(var item in PickupObject.Active)if(item&&initialItemPositions.TryGetValue(item.NetworkObjectId,out var p)){item.Release(false);item.Body.position=p;item.transform.position=p;item.Body.linearVelocity=Vector3.zero;}watcher?.ResetForOnboarding();}
  public void RegisterScan(bool unknown,bool archive,bool final){if(!IsServer)return;if(unknown&&Phase.Value>=(int)NightPhase.WatcherActive)UnknownScanned.Value=true;if(archive&&Phase.Value>=(int)NightPhase.RestrictedArea)ArchiveScanned.Value=true;if(final&&Phase.Value>=(int)NightPhase.FinalTask)FinalScanned.Value=true;}
  public void RestorePower(){if(!IsServer||!FuseInserted.Value)return;PowerRestored.Value=true;Advance(NightPhase.WatcherActive);}
  public void Advance(NightPhase phase){if(!IsServer||(int)phase<=Phase.Value)return;Phase.Value=(int)phase;PhaseStarted.Value=NetworkManager.ServerTime.Time;if(phase==NightPhase.FirstAnomalies){foreach(var anomaly in anomalies)if(anomaly)anomaly.Revealed.Value=true;horror?.ServerBeginEvent(HorrorEventKind.Creak,6,4);}EnsureObjectiveAccess();}
  public void ForcePhaseForTests(NightPhase phase){if(IsServer){Phase.Value=(int)phase;PhaseStarted.Value=NetworkManager.ServerTime.Time;}}
  public void ServerTick(ShiftManager shift,bool allHome) {
   if(!IsServer||NetworkManager.ServerTime.Time<nextTick)return;nextTick=NetworkManager.ServerTime.Time+.2;
   EnsureObjectiveAccess();
   Workers.Value=Mathf.Max(1,NetworkManager.ConnectedClients.Count);
   foreach(var item in PickupObject.Active)if(item&&!initialItemPositions.ContainsKey(item.NetworkObjectId))initialItemPositions[item.NetworkObjectId]=item.Body.position;
   int crates=RequiredShipmentZones().Sum(z=>z?z.CountDelivered():0);shift.Crates.Value=Mathf.Min(crates,CrateTarget);
   Placements.Value=placementZones.Count(z=>z&&z.CountDelivered()>0);
   if(sculptureZone&&sculptureZone.CountDelivered()>0)shift.Statues.Value=1;
   ArchiveReturned.Value=archiveReturnZone&&archiveReturnZone.CountDelivered()>0;
   FinalObjectsReturned.Value=finalReturnZone&&finalReturnZone.CountDelivered()>=2;
   var phase=(NightPhase)Phase.Value;
   if(OnboardingStarted.Value&&phase==NightPhase.NormalShift&&NightRules.CanAdvance(phase,shift.Crates.Value,CrateTarget,Placements.Value,shift.Inspections.Value,false,false,false,false,false,false,false,false,false,false))Advance(NightPhase.FirstAnomalies);
   else if(phase==NightPhase.FirstAnomalies&&NetworkManager.ServerTime.Time-PhaseStarted.Value>=35){PowerRestored.Value=false;horror?.ServerBeginEvent(HorrorEventKind.SectionBlackout,8,12);Advance(NightPhase.PowerProblem);}
   else if(phase==NightPhase.WatcherActive&&shift.Statues.Value>0&&SecurityChecked.Value&&UnknownScanned.Value)Advance(NightPhase.RestrictedArea);
   else if(phase==NightPhase.RestrictedArea&&KeycardFound.Value&&ArchiveScanned.Value&&ArchiveReturned.Value)Advance(NightPhase.FinalTask);
   else if(phase==NightPhase.FinalTask&&FinalObjectsReturned.Value&&FinalCaseSecured.Value&&FinalScanned.Value&&PowerRestored.Value)Advance(NightPhase.Escape);
   if(Phase.Value==(int)NightPhase.Escape&&allHome)shift.Result.Value=1;
  }
  IEnumerable<NightObjectiveZone> ObjectiveZones(){foreach(var zone in shipmentZones??System.Array.Empty<NightObjectiveZone>())if(zone)yield return zone;foreach(var zone in placementZones??System.Array.Empty<NightObjectiveZone>())if(zone)yield return zone;if(sculptureZone)yield return sculptureZone;if(archiveReturnZone)yield return archiveReturnZone;if(finalReturnZone)yield return finalReturnZone;}
  void EnsureObjectiveAccess(){foreach(var zone in ObjectiveZones())if(zone.accessDoor&&zone.DependencyAllowsPhase(Phase.Value))zone.accessDoor.Locked.Value=false;}
  public string CurrentStep(ShiftManager shift){
   return (NightPhase)Phase.Value switch {
    NightPhase.NormalShift=>!OnboardingStarted.Value?"start":shift.Crates.Value<CrateTarget?"shipments":Placements.Value<3?"placements":"inspect",
    NightPhase.FirstAnomalies=>"anomaly",
    NightPhase.PowerProblem=>!FuseInserted.Value?"fuse":"breaker",
    NightPhase.WatcherActive=>shift.Statues.Value<1?"sculpture":!SecurityChecked.Value?"security":"unknown",
    NightPhase.RestrictedArea=>!KeycardFound.Value?"key":!ArchiveScanned.Value?"archive_scan":"archive_return",
    NightPhase.FinalTask=>!FinalObjectsReturned.Value?"final_objects":!FinalCaseSecured.Value?"final_case":"final_scan",
    _=>"exit"
   };
  }
  IEnumerable<NightObjectiveZone> RequiredShipmentZones()=>shipmentZones.Where(z=>z).OrderBy(z=>z.expectedExhibitId).Take(CrateTarget);
  public NightObjectiveZone CurrentShipmentZone()=>RequiredShipmentZones().FirstOrDefault(z=>z.CountDelivered()==0);
  public bool IsCurrentDestination(NightObjectiveZone zone){if(!zone||!zone.AccessReady(Phase.Value))return false;var shift=ShiftManager.Instance;if(!shift)return false;string step=CurrentStep(shift);if(step=="shipments")return zone==CurrentShipmentZone();return step switch {"placements"=>zone.placement,"sculpture"=>zone.sculpture,"archive_return"=>zone.archiveReturn,"final_objects"=>zone.finalReturn,_=>false};}
  public bool ObjectiveAccessReady(string step)=>step switch {"shipments"=>CurrentShipmentZone()?.AccessReady(Phase.Value)!=false,"placements"=>placementZones.All(z=>z&&z.AccessReady(Phase.Value)),"sculpture"=>sculptureZone&&sculptureZone.AccessReady(Phase.Value),"archive_scan" or "archive_return"=>restrictedDoor&&!restrictedDoor.Locked.Value&&Phase.Value>=restrictedDoor.unlockPhase,"final_objects"=>finalReturnZone&&finalReturnZone.AccessReady(Phase.Value),_=>true};
  public bool ActiveDestination(string kind){var shift=ShiftManager.Instance;if(!shift)return false;return CurrentStep(shift) switch {"shipments"=>CurrentShipmentZone()?.requiredKind==kind,"placements"=>kind.StartsWith("placement_"),"sculpture"=>kind=="missing_sculpture","archive_return"=>kind=="archive_artifact","final_objects"=>kind=="final_object",_=>false};}
  public string CurrentObjectiveLine(ShiftManager shift){var step=CurrentStep(shift);if(step=="shipments"){var zone=CurrentShipmentZone();return zone?$"Перенести {zone.expectedExhibitId} в {zone.label}":"Принять поставку";}return step switch {"start"=>"Начать смену","placements"=>"Расставить экспонаты","inspect"=>"Осмотреть витрины","anomaly"=>"Проверить изменения","fuse"=>"Найти предохранитель","breaker"=>"Включить питание","sculpture"=>"Вернуть скульптуру","security"=>"Проверить пульт охраны","unknown"=>"Просканировать неизвестный экспонат","key"=>"Взять пропуск","archive_scan"=>"Просканировать маску","archive_return"=>"Вернуть маску","final_objects"=>"Перенести два предмета","final_case"=>"Закрыть витрину","final_scan"=>"Проверить печать",_=>"Вернуться к выходу"};}
  public string HistoryText(ShiftManager shift){var lines=new List<string>();if(OnboardingStarted.Value)lines.Add("✓ Начать смену");foreach(var zone in shipmentZones.Where(z=>z).OrderBy(z=>z.expectedExhibitId))if(zone.CountDelivered()>0)lines.Add("✓ Перенести "+zone.expectedExhibitId);foreach(var zone in placementZones.Where(z=>z))if(zone.CountDelivered()>0)lines.Add("✓ Установить "+ExhibitIdentity.Prefix(zone.requiredKind));if(shift.Inspections.Value>=5)lines.Add("✓ Осмотреть витрины");if(FuseInserted.Value)lines.Add("✓ Установить предохранитель");if(Phase.Value>=(int)NightPhase.WatcherActive&&PowerRestored.Value)lines.Add("✓ Восстановить питание");return "ЗАВЕРШЕНО\n"+(lines.Count>0?string.Join("\n",lines):"—")+"\n\nТЕКУЩАЯ\n→ "+CurrentObjectiveLine(shift);}
  public string ObjectiveText(ShiftManager shift) {
   string text=CurrentStep(shift) switch {
    "start"=>"Осмотрите доску смены\nМесто: Комната персонала\nНажмите [E], чтобы начать.",
    "shipments"=>CurrentShipmentZone() is NightObjectiveZone shipment?$"Перенесите ящик {shipment.expectedExhibitId} в {shipment.label}\n\nМесто: {shipment.label}\nПодсказка: найдите площадку {shipment.expectedExhibitId}\n\n{shift.Crates.Value} / {CrateTarget} ящиков":"Поставка принята.",
    "placements"=>$"Расставьте три экспоната с приёмки\n{Placements.Value} / 3\nМесто: Галерея A, Археология, Главный зал\nК — картина, АР — сосуд, ГЗ — бюст.",
    "inspect"=>$"Осмотрите витрины\n{shift.Inspections.Value} / 5\nМесто: Галереи A, B, Главный зал, Археология\nПодойдите к витрине и нажмите [E].",
    "anomaly"=>"Проверьте изменившиеся экспонаты\nМесто: Главный зал и галереи\nОсмотрите открытую витрину и портрет.",
    "fuse"=>"Замените предохранитель Галереи B\nМесто: Электрощитовая\nЗапасной: Склад, Охрана или Электрощитовая.",
    "breaker"=>"Включите рубильник\nМесто: Электрощитовая\nПредохранитель установлен.",
    "sculpture"=>"Верните скульптуру С на место\nМесто: из Археологии в Галерею B\nСверьте маркировку С на постаменте.",
    "security"=>"Проверьте пульт наблюдения\nМесто: Охрана\nНажмите [E] у мониторов.",
    "unknown"=>"Просканируйте неизвестный экспонат\nМесто: Галерея A\nВозьмите сканер у персонала. [E] — сканировать.",
    "key"=>"Возьмите пропуск в архив\nМесто: стол в Охране\nПосле проверки камер открыт доступ.",
    "archive_scan"=>"Просканируйте маску МА\nМесто: Архив\nНаведите сканер на маску и нажмите [E].",
    "archive_return"=>"Верните маску МА на место\nМесто: Архив\nИщите табличку МА.",
    "final_objects"=>"Перенесите два предмета ХР на приёмку\nМесто: из Склада в Приёмку\nТяжёлую плиту можно тащить одному.",
    "final_case"=>"Закройте финальную витрину\nМесто: Галерея B\nНажмите [E] у замка витрины.",
    "final_scan"=>"Проверьте печать финального экспоната\nМесто: Галерея B\nНажмите [E] у печати.",
    _=>"Вернитесь к выходу\nМесто: Комната персонала\nВсе сотрудники должны собраться вместе."
   };
   return "ТЕКУЩАЯ ЗАДАЧА\n\n"+text;
  }
 }
}
