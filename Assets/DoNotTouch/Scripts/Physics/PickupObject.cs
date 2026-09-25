using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
namespace DoNotTouch {
 [RequireComponent(typeof(Rigidbody))]
 public class PickupObject : NetworkBehaviour, IInteractable {
  public static readonly HashSet<PickupObject> Active=new();
  public string kind="crate";
  public bool heavy;
  public float spring=65, damping=14, maxAcceleration=90, breakDistance=4, damageSpeed=9;
  public NetworkVariable<ulong> Holder=new(ulong.MaxValue);
  public NetworkVariable<bool> Damaged=new();
  public bool IsHeld=>Holder.Value!=ulong.MaxValue;
  public Rigidbody Body {get; private set;}
  PlayerController holder;
  Vector3 start;
  Quaternion heldRotation;
  void Awake(){Body=GetComponent<Rigidbody>(); start=transform.position;}
  void OnEnable(){Active.Add(this);}
  void OnDisable(){Active.Remove(this);}
  public bool CanInteract(PlayerController player)=>!IsHeld && (heavy||Body.mass<=player.maxMass);
  public string GetInteractionPrompt(PlayerController p)=>IsHeld?"Предмет занят":heavy?"[E / ЛКМ] Тащить тяжёлый экспонат":"[E / ЛКМ] Взять";
  public void Interact(PlayerController p){TryGrab(p);}
  public bool TryGrab(PlayerController p) {
   if(!IsServer || !p || !CanInteract(p) || p.Held || !p.CanReach(this)) return false;
   holder=p; Holder.Value=p.OwnerClientId; p.Held=this; heldRotation=Body.rotation;
   UnityEngine.Physics.IgnoreCollision(GetComponent<Collider>(),p.GetComponent<Collider>(),true);
   return true;
  }
  public void Release(bool thrown) {
   if(!IsServer || !IsHeld) return;
   if(holder) {
    UnityEngine.Physics.IgnoreCollision(GetComponent<Collider>(),holder.GetComponent<Collider>(),false);
    if(thrown) Body.AddForce(holder.ViewForward*holder.throwForce,ForceMode.Impulse);
    holder.Held=null;
   }
   holder=null; Holder.Value=ulong.MaxValue;
  }
  void FixedUpdate() {
   if(!IsServer || !IsSpawned) return;
   if(transform.position.y < -8){Release(false); Body.position=start; Body.linearVelocity=Vector3.zero; Body.angularVelocity=Vector3.zero;}
   if(!IsHeld) return;
   if(!holder || !holder.IsSpawned){Release(false);return;}
   Vector3 delta=holder.HoldPoint-Body.position;
   if(delta.magnitude>breakDistance){Release(false);return;}
   int helpers=0;if(heavy)foreach(var p in PlayerController.Active)if(p&&p!=holder&&Vector3.Distance(p.transform.position,Body.position)<2.5f)helpers++;
   float strength=heavy?maxAcceleration*(helpers>0?.72f:.42f):maxAcceleration;
   Body.AddForce(Vector3.ClampMagnitude(delta*spring-Body.linearVelocity*damping,strength),ForceMode.Acceleration);
   Quaternion q=heldRotation*Quaternion.Inverse(Body.rotation); q.ToAngleAxis(out float angle,out Vector3 axis);
   if(angle>180) angle-=360;
   if(axis.sqrMagnitude>.001f) Body.AddTorque(Vector3.ClampMagnitude(axis*(angle*Mathf.Deg2Rad*15)-Body.angularVelocity*5,30),ForceMode.Acceleration);
  }
  void OnCollisionEnter(Collision c) {
   if(IsServer && IsSpawned && !Damaged.Value && c.relativeVelocity.magnitude>=damageSpeed && ShiftManager.Instance && ShiftManager.Instance.Result.Value==0) {
    Damaged.Value=true; ShiftManager.Instance.Damaged.Value++;
   }
  }
 }
}
