using UnityEngine;
namespace DoNotTouch {
 public class ObjectiveWaypoint : MonoBehaviour {
  public NightObjectiveZone zone; public Transform target; public string displayName;public Vector2 roomCenter,roomSize;TextMesh label;Renderer labelRenderer;
  void Awake(){label=gameObject.AddComponent<TextMesh>();label.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");label.fontSize=64;label.characterSize=.035f;label.anchor=TextAnchor.MiddleCenter;label.alignment=TextAlignment.Center;label.color=new Color(.72f,.68f,.5f);labelRenderer=label.GetComponent<Renderer>();}
  bool InRoom(Vector3 point)=>roomSize.x>0&&Mathf.Abs(point.x-roomCenter.x)<=roomSize.x*.5f&&Mathf.Abs(point.z-roomCenter.y)<=roomSize.y*.5f;
  void LateUpdate(){var player=PlayerController.Local;var director=NightShiftDirector.Instance;bool active=director&&zone&&zone.AccessReady(director.Phase.Value)&&director.IsCurrentDestination(zone)&&player&&!InRoom(player.transform.position);if(active&&(zone.requiredKind=="crate_a"||zone.requiredKind=="crate_b")){PickupObject held=null;foreach(var item in PickupObject.Active)if(item.IsHeld&&item.Holder.Value==player.OwnerClientId){held=item;break;}active=zone.Accepts(held);}if(!labelRenderer)labelRenderer=label.GetComponent<Renderer>();labelRenderer.enabled=active;if(!active)return;var point=target?target:zone.transform;transform.position=point.position+Vector3.up*1.8f;transform.forward=transform.position-player.viewCamera.transform.position;label.text=$"{(string.IsNullOrEmpty(displayName)?zone.label:displayName)}\n{Vector3.Distance(player.transform.position,point.position):F0} м";}
 }
}
