using System.Collections.Generic;
using UnityEngine;
namespace DoNotTouch {
 [RequireComponent(typeof(BoxCollider))]
 public class ObjectiveZone : MonoBehaviour {
  public string requiredKind="crate";
  BoxCollider volume;
  void Awake(){volume=GetComponent<BoxCollider>();}
  public bool Contains(Vector3 point) {
   if(!volume) volume=GetComponent<BoxCollider>();
   Vector3 local=transform.InverseTransformPoint(point)-volume.center;
   Vector3 half=volume.size*.5f;
   return Mathf.Abs(local.x)<=half.x && Mathf.Abs(local.y)<=half.y && Mathf.Abs(local.z)<=half.z;
  }
  public bool Accepts(PickupObject item) => item && item.kind==requiredKind && !item.IsHeld && Contains(item.Body.position);
  public int CountDelivered() {
   int count=0;
   foreach(var item in PickupObject.Active) if(Accepts(item)) count++;
   return count;
  }
 }
}
