using UnityEngine;
namespace DoNotTouch {
 public class ExhibitIdentity : MonoBehaviour {
  public string exhibitId,destinationId;
  public static string Prefix(string kind)=>kind switch {"crate_a"=>"A","crate_b"=>"B","placement_a"=>"К","placement_b"=>"АР","placement_c"=>"ГЗ","missing_sculpture"=>"С","archive_artifact"=>"МА","final_object"=>"ХР",_=>"?"};
  public static string Destination(string kind)=>kind switch {"crate_a"=>"Галерея A","crate_b"=>"Галерея B","placement_a"=>"Галерея A","placement_b"=>"Археология","placement_c"=>"Главный зал","missing_sculpture"=>"Галерея B","archive_artifact"=>"Архив","final_object"=>"Приёмка",_=>"Неизвестно"};
  public string Read(){bool installed=false;var item=GetComponent<PickupObject>();foreach(var zone in FindObjectsByType<NightObjectiveZone>(FindObjectsSortMode.None))if(zone.Accepts(item)&&!item.IsHeld&&zone.Contains(item.transform.position))installed=true;return $"ЭКСПОНАТ {exhibitId}\nЗал: {Destination(destinationId)}\nСтатус: {(installed?"НА МЕСТЕ":"НЕ УСТАНОВЛЕН")}";}
 }
}
