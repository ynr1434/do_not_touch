using System;
using System.Collections.Generic;
using UnityEngine;
namespace DoNotTouch {
 [CreateAssetMenu(menuName="Do Not Touch/Exhibit Registry")]
 public class ExhibitRegistry : ScriptableObject {
  [Serializable] public struct Entry { public string id; public string label; public bool registered; }
  public List<Entry> entries=new();
  public bool TryLookup(string id,out Entry entry) {
   foreach(var candidate in entries)if(candidate.id==id){entry=candidate;return true;}
   entry=default;return false;
  }
  public string Read(string id) {
   if(!TryLookup(id,out var entry)||!entry.registered)return "НЕИЗВЕСТНЫЙ ОБЪЕКТ\nЗапись в реестре отсутствует.";
   return $"ЭКСПОНАТ {entry.id}\n{entry.label}\nСтатус: ЗАРЕГИСТРИРОВАН";
  }
 }
}
