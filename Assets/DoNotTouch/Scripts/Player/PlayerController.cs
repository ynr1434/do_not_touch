using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.InputSystem;
namespace DoNotTouch {
 [RequireComponent(typeof(Rigidbody),typeof(CapsuleCollider))]
 public class PlayerController : NetworkBehaviour {
  public static readonly HashSet<PlayerController> Active=new();
  public static PlayerController Local {get;private set;}
  public float walkSpeed=4, sprintSpeed=6, jumpSpeed=5, lookSensitivity=.12f;
  public float interactionDistance=3.2f, holdDistance=2.1f, throwForce=10, maxMass=30;
  public Camera viewCamera;
  public Transform head;
  public Light flashlight;
  public Renderer bodyRenderer;
  public PickupObject Held {get;set;}
  public bool Paused {get;private set;}
  public bool ShowObjectives {get;private set;}
  public string Prompt {get;private set;}="";
  public NetworkVariable<float> Pitch=new();
  public NetworkVariable<bool> Crouched=new();
  public NetworkVariable<bool> ScannerEquipped=new(), HasFuse=new();
  public NetworkVariable<bool> FlashlightOn=new();
  public NetworkVariable<double> IncapacitatedUntil=new();
  public string ScanMessage {get;private set;}="";
  public float ScanMessageUntil {get;private set;}
  public Vector3 Eye=>transform.position+Vector3.up*(Crouched.Value?.2f:.78f);
  public Vector3 ViewForward=>Quaternion.Euler(Pitch.Value,transform.eulerAngles.y,0)*Vector3.forward;
  public Vector3 HoldPoint=>Eye+ViewForward*holdDistance;
  Rigidbody body; CapsuleCollider capsule;
  Vector2 move; float yaw,pitch,sendTimer,lastInput; bool sprint,crouch,jump;
  InputAction moveAction,lookAction,sprintAction,jumpAction,crouchAction,interactAction,grabAction,throwAction,flashlightAction,tabAction,pauseAction;
  public override void OnNetworkSpawn() {
   Active.Add(this); body=GetComponent<Rigidbody>(); capsule=GetComponent<CapsuleCollider>();
   if(IsServer) {
    var points=ShiftManager.Instance.spawnPoints;
    Vector3 spawn=points[(int)(OwnerClientId%(ulong)points.Length)].position;
    // NGO positions the Transform after Instantiate. Synchronize the physical
    // body explicitly before its first simulation tick (also in standalone).
    body.position=spawn;body.rotation=Quaternion.identity;
    transform.SetPositionAndRotation(spawn,Quaternion.identity);
    GetComponent<NetworkTransform>().Teleport(spawn,Quaternion.identity,Vector3.one);
   }
   EnsureFlashlight();FlashlightOn.OnValueChanged+=FlashlightChanged;FlashlightChanged(false,FlashlightOn.Value);
   viewCamera.gameObject.SetActive(IsOwner);
   if(!IsOwner) return;
   Local=this; bodyRenderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
   yaw=transform.eulerAngles.y;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
   StartCoroutine(ReportSpawn());
#endif
   moveAction=new InputAction("Move",InputActionType.Value); moveAction.AddCompositeBinding("2DVector").With("Up","<Keyboard>/w").With("Down","<Keyboard>/s").With("Left","<Keyboard>/a").With("Right","<Keyboard>/d");
   lookAction=new InputAction("Look",binding:"<Mouse>/delta");
   sprintAction=new InputAction("Sprint",binding:"<Keyboard>/leftShift");
   jumpAction=new InputAction("Jump",binding:"<Keyboard>/space");
   crouchAction=new InputAction("Crouch",binding:"<Keyboard>/leftCtrl"); crouchAction.AddBinding("<Keyboard>/c");
   interactAction=new InputAction("Interact",binding:"<Keyboard>/e");
   grabAction=new InputAction("Grab",binding:"<Mouse>/leftButton");
   throwAction=new InputAction("Throw",binding:"<Mouse>/rightButton");
   flashlightAction=new InputAction("Flashlight",binding:"<Keyboard>/f");
   tabAction=new InputAction("Objectives",binding:"<Keyboard>/tab");
   pauseAction=new InputAction("Pause",binding:"<Keyboard>/escape");
   foreach(var a in Actions()) a.Enable(); SetPause(false);
  }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
  System.Collections.IEnumerator ReportSpawn() {
   yield return new WaitForSeconds(1);
   Debug.Log($"DNT_SPAWN owner={OwnerClientId} root={transform.position} body={body.position} camera={viewCamera.transform.position} eye={Eye} head={head.position}");
  }
#endif
  IEnumerable<InputAction> Actions(){return new[]{moveAction,lookAction,sprintAction,jumpAction,crouchAction,interactAction,grabAction,throwAction,flashlightAction,tabAction,pauseAction};}
  public override void OnNetworkDespawn() {
   FlashlightOn.OnValueChanged-=FlashlightChanged;
   if(IsServer && Held) Held.Release(false);
   Active.Remove(this);
   if(Local==this){Local=null; foreach(var a in Actions()) a?.Dispose(); Cursor.lockState=CursorLockMode.None; Cursor.visible=true;}
  }
  public void SetPause(bool value){Paused=value; Cursor.lockState=value?CursorLockMode.None:CursorLockMode.Locked; Cursor.visible=value;}
  void Update() {
   if(!IsSpawned || !IsOwner) return;
   if(pauseAction.WasPressedThisFrame()) SetPause(!Paused);
   if(tabAction.WasPressedThisFrame()) ShowObjectives=!ShowObjectives;
   bool enabledInput=!Paused && ShiftManager.Instance.Result.Value==0 && (!IsSpawned||NetworkManager.ServerTime.Time>=IncapacitatedUntil.Value);
   if(enabledInput) { var look=lookAction.ReadValue<Vector2>()*lookSensitivity; yaw+=look.x; pitch=Mathf.Clamp(pitch-look.y,-80,80); }
   head.localPosition=new Vector3(0,Crouched.Value?.2f:.78f,0);
   // Owner camera turns immediately; translation remains server authoritative.
   head.rotation=Quaternion.Euler(pitch,yaw,0);
   jump |= enabledInput && jumpAction.WasPressedThisFrame();
   sendTimer+=Time.deltaTime;
   if(sendTimer>=1f/30) {
    InputServerRpc(enabledInput?moveAction.ReadValue<Vector2>():Vector2.zero,yaw,pitch,enabledInput&&sprintAction.IsPressed(),enabledInput&&crouchAction.IsPressed(),jump);
    jump=false;sendTimer=0;
   }
   Prompt="";
   if(enabledInput && RayTarget(Eye,viewCamera.transform.forward,out var target)) Prompt=target.GetInteractionPrompt(this);
   if(enabledInput && (interactAction.WasPressedThisFrame() || grabAction.WasPressedThisFrame())) InteractServerRpc(false);
   if(enabledInput && throwAction.WasPressedThisFrame()) InteractServerRpc(true);
   if(enabledInput && flashlightAction.WasPressedThisFrame())ToggleFlashlightServerRpc();
  }
  public void EnsureFlashlight(){if(flashlight)return;var go=new GameObject("Player flashlight");go.transform.SetParent(head,false);go.transform.localPosition=new Vector3(.12f,-.05f,.08f);go.transform.localRotation=Quaternion.identity;flashlight=go.AddComponent<Light>();flashlight.type=LightType.Spot;flashlight.range=18;flashlight.spotAngle=64;flashlight.innerSpotAngle=34;flashlight.intensity=7;flashlight.color=new Color(1f,.92f,.78f);flashlight.shadows=LightShadows.Soft;flashlight.shadowStrength=.65f;flashlight.cullingMask=~(1<<8);flashlight.enabled=false;}
  void FlashlightChanged(bool previous,bool current){EnsureFlashlight();flashlight.enabled=current;}
  [ServerRpc] void ToggleFlashlightServerRpc(){FlashlightOn.Value=!FlashlightOn.Value;}
  [ServerRpc]
  void InputServerRpc(Vector2 input,float y,float p,bool run,bool duck,bool hop) {
   if(!float.IsFinite(input.x)||!float.IsFinite(input.y)||!float.IsFinite(y)||!float.IsFinite(p)) return;
   move=Vector2.ClampMagnitude(input,1); yaw=y%360; Pitch.Value=Mathf.Clamp(p,-80,80); sprint=run;crouch=duck;jumpRequested|=hop;lastInput=Time.time;
  }
  bool jumpRequested;
  void FixedUpdate() {
   if(!IsSpawned || !IsServer) return;
   if(Time.time-lastInput>.3f || ShiftManager.Instance.Result.Value!=0 || NetworkManager.ServerTime.Time<IncapacitatedUntil.Value) {move=Vector2.zero;sprint=false;jumpRequested=false;}
   body.MoveRotation(Quaternion.Euler(0,yaw,0));
   if(crouch) Crouched.Value=true;
   else if(Crouched.Value && !UnityEngine.Physics.SphereCast(transform.position+Vector3.up*.25f,.28f,Vector3.up,out _, .8f,~(1<<8),QueryTriggerInteraction.Ignore)) Crouched.Value=false;
   capsule.height=Crouched.Value?1.2f:1.8f; capsule.center=new Vector3(0,Crouched.Value?-.3f:0,0);
   Vector3 direction=Quaternion.Euler(0,yaw,0)*new Vector3(move.x,0,move.y);
   float carry=Held&&Held.heavy?NightRules.HeavyCarrySpeed(Mathf.Max(1,NetworkManager.ConnectedClients.Count)):1;
   Vector3 velocity=direction*(Crouched.Value?walkSpeed*.5f:sprint?sprintSpeed:walkSpeed)*carry;
   velocity.y=body.linearVelocity.y;
   if(jumpRequested && UnityEngine.Physics.SphereCast(transform.position,.25f,Vector3.down,out _, .72f,~(1<<8),QueryTriggerInteraction.Ignore)) velocity.y=jumpSpeed;
   jumpRequested=false;body.linearVelocity=velocity;
   if(body.position.y < -8) {if(Held) Held.Release(false);body.position=ShiftManager.Instance.spawnPoints[0].position;body.linearVelocity=Vector3.zero;}
  }
  public bool RayTarget(Vector3 origin,Vector3 direction,out IInteractable target) {
   target=null;
   if(!UnityEngine.Physics.Raycast(origin,direction,out var hit,interactionDistance,~(1<<8),QueryTriggerInteraction.Ignore)) return false;
   target=hit.collider.GetComponentInParent<IInteractable>();return target!=null;
  }
  public bool CanReach(PickupObject item) => RayTarget(Eye,ViewForward,out var target) && ReferenceEquals(target,item);
  [ServerRpc]
  void InteractServerRpc(bool thrown) {
   if(ShiftManager.Instance.Result.Value!=0) return;
   if(Held){Held.Release(thrown);return;}
   if(!thrown && RayTarget(Eye,ViewForward,out var target) && target.CanInteract(this)) target.Interact(this);
  }
  public void SendScanResult(string message){if(IsServer)ScanResultClientRpc(message);}
#if UNITY_EDITOR || DEVELOPMENT_BUILD
  public void DebugAim(float y,float p=0){if(!IsServer&&!IsOwner)return;yaw=y;pitch=p;if(IsServer){Pitch.Value=p;body.rotation=Quaternion.Euler(0,y,0);transform.rotation=body.rotation;}head.rotation=Quaternion.Euler(p,y,0);}
#endif
  [ClientRpc] void ScanResultClientRpc(string message){if(IsOwner){ScanMessage=message;ScanMessageUntil=Time.unscaledTime+4;if(message.Contains("установлен"))PlayPlacementChime();}}
  static AudioClip placementChime;
  void PlayPlacementChime(){if(!placementChime){const int rate=22050,count=3969;var data=new float[count];for(int i=0;i<count;i++){float t=(float)i/rate;float envelope=Mathf.Exp(-t*18);data[i]=(Mathf.Sin(2*Mathf.PI*660*t)+.45f*Mathf.Sin(2*Mathf.PI*990*t))*envelope*.16f;}placementChime=AudioClip.Create("Objective placement",count,1,rate,false);placementChime.SetData(data,0);}AudioSource.PlayClipAtPoint(placementChime,viewCamera.transform.position,.55f);}
  public void ServerWatcherAttack(Vector3 destination,double duration) {
   if(!IsServer)return;
   if(Held)Held.Release(false);
   IncapacitatedUntil.Value=NetworkManager.ServerTime.Time+duration;
   body.position=destination;body.linearVelocity=Vector3.zero;body.angularVelocity=Vector3.zero;
   transform.SetPositionAndRotation(destination,Quaternion.identity);
   GetComponent<NetworkTransform>().Teleport(destination,Quaternion.identity,Vector3.one);
  }
 }
}
