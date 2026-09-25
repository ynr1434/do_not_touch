using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Unity.Netcode;
using Unity.Netcode.Components;
using Unity.Netcode.Transports.UTP;
namespace DoNotTouch.Editor {
 public static class PrototypeBuilder {
  public static void Build()=>HorrorBuilder.Build();
  public static void Rebuild()=>HorrorBuilder.Rebuild();
  public static void Open()=>HorrorBuilder.Open();
  public static void BuildMac()=>HorrorBuilder.BuildMac();
  public const string Root="Assets/DoNotTouch", ScenePath=Root+"/Scenes/PrototypeMuseum.unity";
  static Material wall,floor,wood,teal,stone,metal;
  [MenuItem("Tools/Do Not Touch/Legacy/Build Original Prototype")]
  public static void BuildBase() {
   if(File.Exists(ScenePath)){OpenBase();FinalizeNetworkAssets();Validate();return;}
   Generate();
  }
  [MenuItem("Tools/Do Not Touch/Legacy/Rebuild Original Prototype")]
  public static void RebuildBase() {
   if(!Application.isBatchMode && !EditorUtility.DisplayDialog("Rebuild generated prototype?","This replaces the generated museum scene and player prefab. Other assets are preserved.","Rebuild","Cancel")) return;
   Generate();
  }
  [MenuItem("Tools/Do Not Touch/Legacy/Open Original Prototype")]
  public static void OpenBase(){if(!File.Exists(ScenePath)){BuildBase();return;}if(!Application.isBatchMode&&!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;EditorSceneManager.OpenScene(ScenePath);}
  static void Generate() {
   if(!Application.isBatchMode&&!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
   foreach(var p in new[]{"Scenes","Prefabs","Materials","ScriptableObjects"})Directory.CreateDirectory(Root+"/"+p);
   EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
   wall=Mat("Plaster",new Color(.22f,.29f,.3f)); floor=Mat("Floor",new Color(.12f,.15f,.17f));wood=Mat("Crate",new Color(.5f,.3f,.12f));teal=Mat("Safety teal",new Color(.12f,.65f,.5f));stone=Mat("Stone",new Color(.65f,.65f,.59f));metal=Mat("Metal",new Color(.07f,.1f,.13f));
   var definition=AssetDatabase.LoadAssetAtPath<ShiftDefinition>(Root+"/ScriptableObjects/NightShift.asset");
   if(!definition){definition=ScriptableObject.CreateInstance<ShiftDefinition>();AssetDatabase.CreateAsset(definition,Root+"/ScriptableObjects/NightShift.asset");}
   GameObject player=MakePlayer();
   var network=new GameObject("NetworkManager");var manager=network.AddComponent<NetworkManager>();var transport=network.AddComponent<UnityTransport>();
   manager.NetworkConfig=new NetworkConfig { NetworkTransport=transport, PlayerPrefab=player, ConnectionApproval=true, EnableSceneManagement=true, TickRate=30 };
   manager.NetworkConfig.Prefabs.Add(new NetworkPrefab{Prefab=player});
   var session=network.AddComponent<NetworkSession>();session.manager=manager;session.transport=transport;
   var state=new GameObject("Night Shift");state.AddComponent<NetworkObject>();var shift=state.AddComponent<ShiftManager>();shift.definition=definition;
   var points=new List<Transform>();
   for(int i=0;i<4;i++){var p=new GameObject("Spawn "+(i+1));p.transform.position=new Vector3(-12+(i%2)*2,1.05f,-7+(i/2)*2);points.Add(p.transform);}
   shift.spawnPoints=points.ToArray();
   Box("Museum floor",new Vector3(0,-.2f,0),new Vector3(30,.4f,20),floor);
   Box("Museum ceiling",new Vector3(0,3.7f,0),new Vector3(30,.3f,20),wall).layer=9;
   Box("North wall",new Vector3(0,1.8f,10),new Vector3(30,3.6f,.25f),wall);
   Box("South wall",new Vector3(0,1.8f,-10),new Vector3(30,3.6f,.25f),wall);
   Box("West wall",new Vector3(-15,1.8f,0),new Vector3(.25f,3.6f,20),wall);
   Box("East wall",new Vector3(15,1.8f,0),new Vector3(.25f,3.6f,20),wall);
   foreach(float x in new[]{-5f,5f})foreach(float z in new[]{-5f,5f})Partition(new Vector3(x,0,z),90);
   foreach(float x in new[]{-10f,0f,10f})Partition(new Vector3(x,0,0),0);
   Room("STAFF ROOM",new Vector3(-10,0,-5));Room("SERVICE CORRIDOR",new Vector3(0,0,-5));Room("SECURITY ROOM",new Vector3(10,0,-5));
   Room("STORAGE",new Vector3(-10,0,5));Room("MAIN HALL",new Vector3(0,0,5));Room("GALLERY A",new Vector3(10,0,5));
   shift.crateZone=Zone("CRATE DELIVERY",new Vector3(10,1,4),new Vector3(5,2,3),"crate");
   shift.exitZone=Zone("END SHIFT — ALL WORKERS",new Vector3(-10,1,-6),new Vector3(7,2,5),"exit");
   Box("Statue pedestal",new Vector3(12,.4f,8),new Vector3(1.6f,.8f,1.6f),stone);
   shift.statueZone=Zone("RETURN STATUE",new Vector3(12,1.35f,8),new Vector3(2,1.2f,2),"statue");
   for(int i=0;i<3;i++)Pickup("Artifact crate "+(i+1),new Vector3(-12+i*1.8f,.55f,5),Vector3.one*.85f,"crate",wood,8);
   var statue=Pickup("Misplaced small statue",new Vector3(-8,.65f,8),new Vector3(.45f,1,.45f),"statue",stone,5);
   for(int i=0;i<3;i++) {
    Vector3 pos=i==0?new Vector3(-3,.65f,8):i==1?new Vector3(3,.65f,8):new Vector3(8,.65f,8);
    var display=Box("Display case "+(i+1),pos,new Vector3(1.4f,1.3f,1),metal);display.AddComponent<NetworkObject>();display.AddComponent<DisplayCase>();
    var exhibit=GameObject.CreatePrimitive(PrimitiveType.Sphere);exhibit.name="Exhibit";exhibit.transform.SetParent(display.transform,false);exhibit.transform.localPosition=new Vector3(0,.7f,0);exhibit.transform.localScale=Vector3.one*.4f;exhibit.GetComponent<Renderer>().sharedMaterial=stone;
    Sign("[E] INSPECT",pos+Vector3.up*1.2f,Quaternion.identity,.09f);
   }
   MakeWatcher();
   Box("Staff desk",new Vector3(-13,.5f,-9),new Vector3(3,1,1),wood);
   Box("Security desk",new Vector3(12,.5f,-8),new Vector3(4,1,1.2f),metal);
   Sign("NIGHT SHIFT\n1. Deliver crates to Gallery A\n2. Inspect three display cases\n3. Return the small statue\n4. Everyone returns here",new Vector3(-10,2,-9.7f),Quaternion.Euler(0,180,0),.085f);
   var sunlight=new GameObject("Moonlight").AddComponent<Light>();sunlight.type=LightType.Directional;sunlight.intensity=.35f;sunlight.color=new Color(.6f,.72f,1);sunlight.transform.rotation=Quaternion.Euler(55,-30,0);
   RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;RenderSettings.ambientLight=new Color(.22f,.26f,.29f);
   var cam=new GameObject("Lobby Camera",typeof(Camera),typeof(AudioListener));cam.transform.position=new Vector3(0,13,-14);cam.transform.rotation=Quaternion.Euler(40,0,0);
   cam.GetComponent<Camera>().cullingMask=~(1<<9);
   cam.GetComponent<Camera>().backgroundColor=new Color(.04f,.06f,.08f);cam.GetComponent<Camera>().clearFlags=CameraClearFlags.SolidColor;
   var canvas=new GameObject("Prototype UI",typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));canvas.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;
   var scaler=canvas.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1280,720);scaler.matchWidthOrHeight=.5f;
   var ui=canvas.AddComponent<PrototypeUI>();ui.session=session;ui.lobbyCamera=cam.GetComponent<Camera>();
   new GameObject("EventSystem",typeof(EventSystem),typeof(InputSystemUIInputModule));
   PlayerSettings.runInBackground=true;
   EditorSceneManager.SaveScene(SceneManager.GetActiveScene(),ScenePath);
   FinalizeNetworkAssets();
   var scenes=EditorBuildSettings.scenes.Where(s=>s.path!=ScenePath).ToList();scenes.Insert(0,new EditorBuildSettingsScene(ScenePath,true));EditorBuildSettings.scenes=scenes.ToArray();
   AssetDatabase.SaveAssets();AssetDatabase.Refresh();Validate();Debug.Log("DO NOT TOUCH: Prototype built. Press Play, then Host shift.");
  }
  internal static void FinalizeNetworkAssets() {
   // Scene objects have stable GlobalObjectIds only after the first save.
   // Invoke NGO validation directly: SendMessage also dispatches to inactive behaviours
   // and emits ShouldRunBehaviour assertions in Unity 6000.3 batchmode.
   var validate=typeof(NetworkObject).GetMethod("OnValidate",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
   if(validate==null)throw new System.InvalidOperationException("NGO validation API changed; review generated network IDs.");
   foreach(var net in UnityEngine.Object.FindObjectsByType<NetworkObject>(FindObjectsSortMode.None)) {
    validate.Invoke(net,null);EditorUtility.SetDirty(net);
   }
   var prefab=UnityEngine.Object.FindFirstObjectByType<NetworkManager>().NetworkConfig.PlayerPrefab;
   if(prefab){validate.Invoke(prefab.GetComponent<NetworkObject>(),null);EditorUtility.SetDirty(prefab);PrefabUtility.SavePrefabAsset(prefab);}
   EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());EditorSceneManager.SaveScene(SceneManager.GetActiveScene());AssetDatabase.SaveAssets();
  }
  static Material Mat(string name,Color color) {
   string path=Root+"/Materials/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
   if(!m){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));m.color=color;AssetDatabase.CreateAsset(m,path);}return m;
  }
  static GameObject Box(string name,Vector3 pos,Vector3 size,Material material) {
   var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.position=pos;go.transform.localScale=size;go.GetComponent<Renderer>().sharedMaterial=material;return go;
  }
  static void Sign(string text,Vector3 pos,Quaternion rotation,float scale=.13f) {
   var go=new GameObject(text);go.transform.position=pos;go.transform.rotation=rotation;var label=go.AddComponent<TextMesh>();label.text=text;label.fontSize=48;label.characterSize=scale;label.anchor=TextAnchor.MiddleCenter;label.alignment=TextAlignment.Center;label.color=new Color(.8f,.92f,.84f);
  }
  static void Room(string name,Vector3 center) {
   Sign(name,center+new Vector3(0,3,4.65f),Quaternion.identity);
   var lamp=new GameObject(name+" light").AddComponent<Light>();lamp.type=LightType.Point;lamp.range=13;lamp.intensity=3;lamp.color=new Color(.8f,.9f,.83f);lamp.transform.position=center+Vector3.up*3;lamp.shadows=LightShadows.None;
  }
  static void Partition(Vector3 center,float yaw) {
   Quaternion q=Quaternion.Euler(0,yaw,0);
   foreach(float side in new[]{-1f,1f}) {var part=Box("Partition",center+q*new Vector3(side*3.1f,1.8f,0),new Vector3(3.8f,3.6f,.22f),wall);part.transform.rotation=q;}
   var lintel=Box("Door lintel",center+Vector3.up*3.15f,new Vector3(2.4f,.9f,.22f),wall);lintel.transform.rotation=q;
   var door=new GameObject("Museum door");door.transform.position=center+q*new Vector3(-1.15f,0,0);door.transform.rotation=q;door.AddComponent<NetworkObject>();var component=door.AddComponent<MuseumDoor>();
   var pivot=new GameObject("Door pivot");pivot.transform.SetParent(door.transform,false);component.leaf=pivot.transform;
   var leaf=Box("Door leaf",Vector3.zero,new Vector3(2.3f,2.65f,.14f),wood);leaf.transform.SetParent(pivot.transform,false);leaf.transform.localPosition=new Vector3(1.15f,1.325f,0);
  }
  static ObjectiveZone Zone(string name,Vector3 center,Vector3 size,string kind) {
   var go=new GameObject(name);go.transform.position=center;var col=go.AddComponent<BoxCollider>();col.isTrigger=true;col.size=size;var zone=go.AddComponent<ObjectiveZone>();zone.requiredKind=kind;
   var marking=Box(name+" floor marking",new Vector3(center.x,.015f,center.z),new Vector3(size.x,.025f,size.z),teal);UnityEngine.Object.DestroyImmediate(marking.GetComponent<Collider>());
   Sign(name,new Vector3(center.x,.05f,center.z),Quaternion.Euler(90,0,0),.075f);return zone;
  }
  static GameObject Pickup(string name,Vector3 pos,Vector3 scale,string kind,Material material,float mass) {
   var go=Box(name,pos,scale,material);var body=go.AddComponent<Rigidbody>();body.mass=mass;body.interpolation=RigidbodyInterpolation.Interpolate;body.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;body.angularDamping=1;
   go.AddComponent<NetworkObject>();go.AddComponent<NetworkTransform>();go.AddComponent<NetworkRigidbody>();go.AddComponent<PickupObject>().kind=kind;return go;
  }
  static GameObject MakePlayer() {
   var go=new GameObject("Museum Worker");go.layer=8;go.AddComponent<NetworkObject>();go.AddComponent<NetworkTransform>();
   var body=go.AddComponent<Rigidbody>();body.mass=75;body.constraints=RigidbodyConstraints.FreezeRotation;body.interpolation=RigidbodyInterpolation.Interpolate;body.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;
   var col=go.AddComponent<CapsuleCollider>();col.height=1.8f;col.radius=.3f;
   var physics=new PhysicsMaterial("Worker friction"){staticFriction=0,dynamicFriction=0,frictionCombine=PhysicsMaterialCombine.Minimum};
   string physicsPath=Root+"/Materials/Worker friction.physicMaterial";var existing=AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(physicsPath);if(!existing){AssetDatabase.CreateAsset(physics,physicsPath);existing=physics;}else UnityEngine.Object.DestroyImmediate(physics);col.sharedMaterial=existing;
   go.AddComponent<NetworkRigidbody>();var player=go.AddComponent<PlayerController>();
   var mesh=GameObject.CreatePrimitive(PrimitiveType.Capsule);mesh.name="Worker body";UnityEngine.Object.DestroyImmediate(mesh.GetComponent<Collider>());mesh.transform.SetParent(go.transform,false);mesh.transform.localScale=new Vector3(.6f,.9f,.6f);mesh.GetComponent<Renderer>().sharedMaterial=teal;player.bodyRenderer=mesh.GetComponent<Renderer>();
   var head=new GameObject("Head");head.transform.SetParent(go.transform,false);head.transform.localPosition=Vector3.up*.78f;player.head=head.transform;
   var cam=new GameObject("Player camera",typeof(Camera),typeof(AudioListener));cam.transform.SetParent(head.transform,false);player.viewCamera=cam.GetComponent<Camera>();player.viewCamera.clearFlags=CameraClearFlags.SolidColor;player.viewCamera.backgroundColor=new Color(.025f,.035f,.05f);player.viewCamera.nearClipPlane=.05f;player.viewCamera.fieldOfView=75;cam.SetActive(false);
   player.EnsureFlashlight();
   var prefab=PrefabUtility.SaveAsPrefabAsset(go,Root+"/Prefabs/Player.prefab");UnityEngine.Object.DestroyImmediate(go);return prefab;
  }
  static void MakeWatcher() {
   var go=new GameObject("The Watcher");go.transform.position=new Vector3(0,0,6);go.AddComponent<NetworkObject>();go.AddComponent<NetworkTransform>();var watcher=go.AddComponent<Watcher>();
   var torso=GameObject.CreatePrimitive(PrimitiveType.Capsule);torso.name="Watcher torso";torso.transform.SetParent(go.transform,false);torso.transform.localPosition=new Vector3(0,1.1f,0);torso.transform.localScale=new Vector3(.65f,.9f,.5f);torso.GetComponent<Renderer>().sharedMaterial=stone;
   var head=GameObject.CreatePrimitive(PrimitiveType.Sphere);head.name="Watcher head";head.transform.SetParent(go.transform,false);head.transform.localPosition=new Vector3(0,2,0);head.transform.localScale=Vector3.one*.48f;head.GetComponent<Renderer>().sharedMaterial=stone;
   foreach(float side in new[]{-1f,1f}){var arm=Box("Watcher arm",Vector3.zero,new Vector3(.18f,1,.22f),stone);arm.transform.SetParent(go.transform,false);arm.transform.localPosition=new Vector3(side*.45f,1.05f,0);}
   var spots=new List<Transform>();foreach(var pos in new[]{new Vector3(0,0,6),new Vector3(-2,0,3),new Vector3(2,0,5),new Vector3(0,0,8)}){var spot=new GameObject("Watcher position");spot.transform.position=pos;spots.Add(spot.transform);}watcher.positions=spots.ToArray();
  }
  [MenuItem("Tools/Do Not Touch/Run Validation")]
  public static void Validate() {
   var errors=new List<string>();var manager=UnityEngine.Object.FindFirstObjectByType<NetworkManager>();var shift=UnityEngine.Object.FindFirstObjectByType<ShiftManager>();var watcher=UnityEngine.Object.FindFirstObjectByType<Watcher>();var ui=UnityEngine.Object.FindFirstObjectByType<PrototypeUI>();
   if(!manager)errors.Add("NetworkManager missing");else {
    if(!manager.NetworkConfig.PlayerPrefab)errors.Add("Player prefab missing");
    else {var p=manager.NetworkConfig.PlayerPrefab.GetComponent<PlayerController>();if(!p||!p.head||!p.viewCamera||!p.bodyRenderer||!p.flashlight)errors.Add("Player prefab references incomplete or flashlight missing");}
    if(!manager.GetComponent<UnityTransport>())errors.Add("UnityTransport missing");
   }
   if(!shift||!shift.definition)errors.Add("Shift definition missing");
   if(!shift||shift.spawnPoints==null||shift.spawnPoints.Length<4||shift.spawnPoints.Any(p=>!p))errors.Add("Four spawn points required");
   if(!shift||!shift.crateZone||!shift.statueZone)errors.Add("Objective zones missing");
   if(!shift||!shift.exitZone)errors.Add("Exit zone missing");
   if(!watcher||watcher.positions==null||watcher.positions.Length<2||watcher.positions.Any(p=>!p))errors.Add("Watcher positions missing");
   if(!UnityEngine.Object.FindFirstObjectByType<EventSystem>())errors.Add("EventSystem missing");
   if(!ui||!ui.session||!ui.lobbyCamera)errors.Add("UI references missing");
   bool night01=UnityEngine.Object.FindFirstObjectByType<NightShiftDirector>();
   if(!night01&&UnityEngine.Object.FindObjectsByType<DisplayCase>(FindObjectsSortMode.None).Length!=3)errors.Add("Expected three display cases");
   if(!night01&&UnityEngine.Object.FindObjectsByType<PickupObject>(FindObjectsSortMode.None).Count(p=>p.kind=="crate")!=3)errors.Add("Expected three crates");
   foreach(var root in SceneManager.GetActiveScene().GetRootGameObjects()) foreach(var t in root.GetComponentsInChildren<Transform>(true)) {
    if(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)>0)errors.Add("Missing script: "+t.name);
    foreach(var c in t.GetComponents<Component>()) {
     if(!c)continue;var so=new SerializedObject(c);var property=so.GetIterator();
     while(property.NextVisible(true))if(property.propertyType==SerializedPropertyType.ObjectReference && property.objectReferenceValue==null && property.objectReferenceInstanceIDValue!=0)errors.Add("Missing reference: "+t.name+"."+property.propertyPath);
    }
   }
   var hashes=new HashSet<long>();
   foreach(var net in UnityEngine.Object.FindObjectsByType<NetworkObject>(FindObjectsSortMode.None)) {
    long hash=new SerializedObject(net).FindProperty("GlobalObjectIdHash").longValue;
    if(hash==0 || !hashes.Add(hash))errors.Add("Invalid or duplicate network scene ID: "+net.name);
   }
   if(manager && manager.NetworkConfig.PlayerPrefab && new SerializedObject(manager.NetworkConfig.PlayerPrefab.GetComponent<NetworkObject>()).FindProperty("GlobalObjectIdHash").longValue==0)errors.Add("Player prefab network ID is zero");
   if(errors.Count>0){foreach(var error in errors)Debug.LogError("DO NOT TOUCH: "+error);throw new InvalidOperationException("Prototype validation failed: "+errors.Count+" errors");}
   if(UnityEngine.Object.FindFirstObjectByType<HorrorDirector>()&&!night01)HorrorBuilder.ValidateSetup();
   Debug.Log("DO NOT TOUCH VALIDATION PASSED: network, player, spawns, objectives, Watcher, exit, UI and references.");
  }
  public static void BuildMacBase() {
   BuildBase();
   var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{ScenePath},locationPathName="Builds/macOS/DoNotTouch.app",target=BuildTarget.StandaloneOSX,options=BuildOptions.Development});
   if(report.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded)throw new Exception("macOS build failed");
  }
 }
}
