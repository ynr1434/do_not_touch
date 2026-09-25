using UnityEngine;
using System.Collections.Generic;
using Unity.Netcode;
namespace DoNotTouch {
 [RequireComponent(typeof(BoxCollider))]
 public class NightObjectiveZone : MonoBehaviour {
  public string requiredKind;
  public string destinationId,expectedExhibitId,label;
  public GameObject marker;
  public ObjectiveWaypoint waypoint;
  public TextMesh plaque;
  public MuseumDoor accessDoor;
  public int requiredUnlockPhase;
  public bool placement, sculpture, archiveReturn, finalReturn;
  BoxCollider volume;
  readonly Dictionary<ulong,ulong> pendingPlayers=new();
  readonly HashSet<ulong> confirmed=new();
  void Awake(){volume=GetComponent<BoxCollider>();}
  public bool Contains(Vector3 point) {
   if(!volume)volume=GetComponent<BoxCollider>();
   Vector3 p=transform.InverseTransformPoint(point)-volume.center,h=volume.size*.5f;
   return Mathf.Abs(p.x)<=h.x&&Mathf.Abs(p.y)<=h.y&&Mathf.Abs(p.z)<=h.z;
  }
  public bool Accepts(PickupObject item){if(!item||item.kind!=requiredKind)return false;var identity=item.GetComponent<ExhibitIdentity>();return (string.IsNullOrEmpty(destinationId)||identity&&identity.destinationId==destinationId)&&(string.IsNullOrEmpty(expectedExhibitId)||identity&&identity.exhibitId==expectedExhibitId);}
  public bool DependencyAllowsPhase(int phase)=>phase>=requiredUnlockPhase&&(!accessDoor||phase>=accessDoor.unlockPhase);
  public bool AccessReady(int phase)=>DependencyAllowsPhase(phase)&&(!accessDoor||!accessDoor.Locked.Value);
  public string WrongFeedback(PickupObject item){var identity=item?item.GetComponent<ExhibitIdentity>():null;return !string.IsNullOrEmpty(expectedExhibitId)?$"Это место для {expectedExhibitId}.":identity?$"Этот экспонат должен находиться в {ExhibitIdentity.Destination(identity.destinationId)}.":$"Это место для {ExhibitIdentity.Prefix(requiredKind)}.";}
  public int CountDelivered(){var director=NightShiftDirector.Instance;if(director&&!AccessReady(director.Phase.Value))return 0;int count=0;foreach(var item in PickupObject.Active)if(Accepts(item)&&!item.IsHeld&&Contains(item.Body.position))count++;return count;}
  void Update(){
   var director=NightShiftDirector.Instance;var local=PlayerController.Local;PickupObject held=null;
   if(local)foreach(var item in PickupObject.Active)if(item.IsHeld&&item.Holder.Value==local.OwnerClientId){held=item;break;}
   if(marker)marker.SetActive(director&&director.IsCurrentDestination(this)&&AccessReady(director.Phase.Value)&&Accepts(held));
   if(!NetworkManager.Singleton||!NetworkManager.Singleton.IsServer)return;
   foreach(var item in PickupObject.Active){if(!Accepts(item))continue;bool delivered=!item.IsHeld&&Contains(item.Body.position);if(!delivered){confirmed.Remove(item.NetworkObjectId);continue;}if(!confirmed.Add(item.NetworkObjectId))continue;var id=item.GetComponent<ExhibitIdentity>();if(pendingPlayers.TryGetValue(item.NetworkObjectId,out var owner))foreach(var player in PlayerController.Active)if(player&&player.OwnerClientId==owner)player.SendScanResult($"Ящик {id?.exhibitId??""} установлен.");}
  }
  void OnTriggerEnter(Collider other){var item=other.GetComponentInParent<PickupObject>();if(!item||!item.IsServer||!item.IsHeld)return;var owner=item.Holder.Value;var id=item.GetComponent<ExhibitIdentity>();var director=NightShiftDirector.Instance;foreach(var player in PlayerController.Active)if(player&&player.OwnerClientId==owner){if(director&&!AccessReady(director.Phase.Value))player.SendScanResult("Доступ закрыт.");else if(Accepts(item)){pendingPlayers[item.NetworkObjectId]=owner;player.SendScanResult($"Место {id?.exhibitId??""}. Отпустите ящик.");}else player.SendScanResult(WrongFeedback(item));}}
 }
}
