using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Unity.Netcode;
namespace DoNotTouch.Editor {
 public static class HorrorBuilder {
  public const string Path="Assets/DoNotTouch/Scenes/PrototypeMuseum_Horror.unity";
  const string Root="Assets/DoNotTouch", Mats=Root+"/Materials/Horror";
  static Transform decor;
  static Material plaster,concrete,wood,oldWood,metal,stone,brass,glass,bulb,textMaterial;
  static readonly Vector3[] Centers={new(-10,0,-5),new(0,0,-5),new(10,0,-5),new(-10,0,5),new(0,0,5),new(10,0,5)};
  static readonly string[] Names={"STAFF ROOM","SERVICE CORRIDOR","SECURITY","STORAGE","MAIN HALL","GALLERY A"};
  static List<HorrorLamp> lamps;
  [MenuItem("Tools/Do Not Touch/Build Prototype")]
  public static void Build(){if(File.Exists(Path)){Open();PrototypeBuilder.Validate();return;}Generate();}
  [MenuItem("Tools/Do Not Touch/Rebuild Prototype")]
  public static void Rebuild(){
   if(!Application.isBatchMode&&!EditorUtility.DisplayDialog("Rebuild horror presentation?","Only PrototypeMuseum_Horror and its generated presentation resources will be rebuilt. The original prototype stays intact.","Rebuild","Cancel"))return;
   Generate();
  }
  [MenuItem("Tools/Do Not Touch/Open Prototype Scene")]
  public static void Open(){if(!File.Exists(Path)){Generate();return;}if(!Application.isBatchMode&&!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;EditorSceneManager.OpenScene(Path);}
  static void Generate() {
   if(!Application.isBatchMode&&!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
   if(!File.Exists(PrototypeBuilder.ScenePath))PrototypeBuilder.BuildBase();
   EditorSceneManager.OpenScene(PrototypeBuilder.ScenePath);
   EditorSceneManager.SaveScene(SceneManager.GetActiveScene(),Path);
   Directory.CreateDirectory(Mats);
   decor=new GameObject("Horror presentation — generated").transform;
   Materials();
   foreach(var light in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None))UnityEngine.Object.DestroyImmediate(light.gameObject);
   foreach(var renderer in UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None)) {
    if(renderer.GetComponent<TextMesh>())continue;
    string name=renderer.sharedMaterial?renderer.sharedMaterial.name:"";
    renderer.sharedMaterial=name=="Plaster"?plaster:name=="Floor"?concrete:name=="Crate"?oldWood:name=="Stone"?stone:name=="Safety teal"?brass:metal;
    if(renderer.name.Contains("floor marking"))renderer.enabled=false;
   }
   foreach(var old in UnityEngine.Object.FindObjectsByType<TextMesh>(FindObjectsSortMode.None))UnityEngine.Object.DestroyImmediate(old.gameObject);
   var shift=UnityEngine.Object.FindFirstObjectByType<ShiftManager>();
   var watcher=UnityEngine.Object.FindFirstObjectByType<Watcher>();
   var root=new GameObject("Museum horror director");root.AddComponent<NetworkObject>();var director=root.AddComponent<HorrorDirector>();director.watcher=watcher;
   var ambience=root.AddComponent<MuseumAmbience>();director.ambience=ambience;
   ambience.roomSources=Centers.Select((p,i)=>{var t=new GameObject(Names[i]+" audio origin").transform;t.SetParent(decor);t.position=p+new Vector3(2,2.6f,2);return t;}).ToArray();
   lamps=new List<HorrorLamp>();
   for(int i=0;i<6;i++)Room(i);
   foreach(float x in new[]{-5f,5f})foreach(float z in new[]{-5f,5f})DoorFrame(new Vector3(x,0,z),90);
   foreach(float x in new[]{-10f,0f,10f})DoorFrame(new Vector3(x,0,0),0);
   Displays();Statue(watcher,director);ZoneLines(shift.crateZone,"ARTIFACT RECEIVING");ZoneLines(shift.exitZone,"STAFF ASSEMBLY");
   ZoneLines(shift.statueZone,"COLLECTION 07");
   MakeSigns();VolumeAndPipeline();MakePlayer();
   director.lamps=lamps.ToArray();
   RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.058f,.067f,.085f);RenderSettings.ambientIntensity=.6f;
   RenderSettings.fog=true;RenderSettings.fogMode=FogMode.ExponentialSquared;RenderSettings.fogDensity=.018f;RenderSettings.fogColor=new Color(.035f,.045f,.058f);RenderSettings.skybox=null;
   var ui=UnityEngine.Object.FindFirstObjectByType<PrototypeUI>();
   ui.lobbyCamera.transform.SetPositionAndRotation(new Vector3(1.6f,1.7f,1.5f),Quaternion.Euler(3,-14,0));
   ui.lobbyCamera.cullingMask=~0;ui.lobbyCamera.GetUniversalAdditionalCameraData().renderPostProcessing=true;
   EditorSceneManager.SaveScene(SceneManager.GetActiveScene(),Path);PrototypeBuilder.FinalizeNetworkAssets();
   var scenes=EditorBuildSettings.scenes.Where(s=>s.path!=Path).ToList();scenes.Insert(0,new EditorBuildSettingsScene(Path,true));EditorBuildSettings.scenes=scenes.ToArray();
   AssetDatabase.SaveAssets();PrototypeBuilder.Validate();Debug.Log("HORROR BUILD PASSED: original museum preserved; new presentation ready.");
  }
  static Material Material(string name,Color color,float roughness=0.7f,int pattern=-1) {
   string path=Mats+"/"+name+".mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
   if(!mat){mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(mat,path);}
   mat.color=color;mat.SetFloat("_Smoothness",1-roughness);
   if(pattern>=0) {
    string texPath=Mats+"/"+name+" grain.asset";var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
    if(!texture){texture=new Texture2D(128,128,TextureFormat.RGBA32,true);texture.name=name+" grain";texture.wrapMode=TextureWrapMode.Repeat;texture.filterMode=FilterMode.Bilinear;AssetDatabase.CreateAsset(texture,texPath);}
    var random=new System.Random(83+pattern);var pixels=new Color[128*128];
    for(int y=0;y<128;y++)for(int x=0;x<128;x++) {
     float noise=(float)random.NextDouble();float v=.82f+noise*.18f;
     if(pattern==0){v*=.8f+.2f*Mathf.Sin(y*.3f+Mathf.Sin(x*.045f)*4);if(y%32<2||((x+(y/32)*43)%128)<2)v*=.35f;}
     if(pattern==1)v*=.85f+.15f*Mathf.PerlinNoise(x*.09f,y*.09f);
     if(pattern==2)v*=.9f+.1f*Mathf.Sin(x*2.6f+y*.01f);
     pixels[x+y*128]=new Color(v,v,v,1);
    }
    texture.SetPixels(pixels);texture.Apply();EditorUtility.SetDirty(texture);mat.SetTexture("_BaseMap",texture);
   }
   EditorUtility.SetDirty(mat);return mat;
  }
  static void Materials() {
   plaster=Material("Cold plaster",new Color(.34f,.37f,.39f),.95f,1);
   concrete=Material("Service concrete",new Color(.25f,.27f,.29f),.98f,1);
   wood=Material("Museum oak",new Color(.23f,.16f,.10f),.6f,0);wood.SetTextureScale("_BaseMap",new Vector2(5,5));
   oldWood=Material("Old timber",new Color(.31f,.24f,.16f),.88f,0);
   metal=Material("Patinated metal",new Color(.13f,.15f,.16f),.48f,2);metal.SetFloat("_Metallic",.55f);
   stone=Material("Weathered basalt",new Color(.31f,.33f,.34f),.95f,1);
   brass=Material("Aged brass",new Color(.34f,.29f,.20f),.6f,2);brass.SetFloat("_Metallic",.4f);
   glass=Material("Display glass",new Color(.35f,.44f,.47f,.12f),.12f);
   glass.SetFloat("_Surface",1);glass.SetFloat("_ZWrite",0);glass.SetFloat("_SrcBlend",(float)BlendMode.SrcAlpha);glass.SetFloat("_DstBlend",(float)BlendMode.OneMinusSrcAlpha);glass.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");glass.renderQueue=3000;glass.SetShaderPassEnabled("ShadowCaster",false);
   bulb=Material("Aged diffuser",new Color(.8f,.72f,.5f),.8f);bulb.EnableKeyword("_EMISSION");bulb.SetColor("_EmissionColor",new Color(1,.75f,.4f)*2);
   string path=Mats+"/World typography.mat";textMaterial=AssetDatabase.LoadAssetAtPath<Material>(path);
   if(!textMaterial){textMaterial=new Material(Shader.Find("DoNotTouch/DepthText"));AssetDatabase.CreateAsset(textMaterial,path);}
  }
  static GameObject Box(string name,Vector3 position,Vector3 size,Material material,bool collider=false) {
   var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(decor);go.transform.position=position;go.transform.localScale=size;go.GetComponent<Renderer>().sharedMaterial=material;
   if(!collider)UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());return go;
  }
  static void Sign(string label,Vector3 position,Quaternion rotation,float size=.055f) {
   var go=new GameObject(label);go.transform.SetParent(decor);go.transform.SetPositionAndRotation(position,rotation);
   var text=go.AddComponent<TextMesh>();text.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");text.text=label;text.fontSize=64;text.characterSize=size;text.anchor=TextAnchor.MiddleCenter;text.alignment=TextAlignment.Center;text.color=new Color(.48f,.45f,.38f);
   go.GetComponent<Renderer>().sharedMaterial=textMaterial;go.AddComponent<DepthText>().template=textMaterial;
  }
  static void Room(int room) {
   Vector3 c=Centers[room];bool service=room==1||room==2||room==3;
   Box(Names[room]+" floor finish",c+Vector3.up*.016f,new Vector3(9.75f,.025f,9.75f),service?concrete:wood);
   foreach(float x in new[]{-3.4f,3.4f})Box("Ceiling beam",c+new Vector3(x,3.42f,0),new Vector3(.22f,.28f,9.8f),service?metal:oldWood);
   if(room<3){foreach(float side in new[]{-1f,1f})Box("North wainscot",c+new Vector3(side*3.1f,.65f,4.83f),new Vector3(3.6f,1.3f,.05f),service?concrete:oldWood);}
   else Box("North wainscot",c+new Vector3(0,.65f,4.83f),new Vector3(9.6f,1.3f,.05f),service?concrete:oldWood);
   Box("North cornice",c+new Vector3(0,3.28f,4.82f),new Vector3(9.7f,.12f,.18f),stone);
   // Furnish the edges, leaving the established 2.4m doorways and delivery lanes open.
   if(room==4) {
    foreach(float x in new[]{-4.2f,4.2f})foreach(float z in new[]{2.5f,7.1f}) {
     Box("Museum column",new Vector3(x,1.65f,z),new Vector3(.5f,3.3f,.5f),stone,true);
     Box("Column capital",new Vector3(x,3.24f,z),new Vector3(.72f,.2f,.72f),stone);
     Box("Column foot",new Vector3(x,.12f,z),new Vector3(.72f,.24f,.72f),stone);
    }
    Lamp(room,new Vector3(1.2f,3.1f,4.5f),new Vector3(0,1.2f,6),2.8f,new Color(.67f,.74f,.83f),false);
    Bench(new Vector3(-3.5f,0,2.5f),90);Bench(new Vector3(3.5f,0,2.5f),90);
    Lamp(room,new Vector3(-2,3.25f,5),new Vector3(-3,0,8),8,new Color(.62f,.72f,.85f),true);
    Lamp(room,new Vector3(2.7f,3.3f,5.8f),new Vector3(2.7f,0,7.5f),7,new Color(.92f,.77f,.54f),true);
    Lamp(room,new Vector3(0,3.25f,2),new Vector3(0,0,3),0,Color.white,false,false,false);
   } else if(room==5) {
    Lamp(room,c+new Vector3(-2,3.3f,1),new Vector3(8,1,8),11,new Color(1,.84f,.64f),true);
    Lamp(room,c+new Vector3(2,3.3f,1),new Vector3(12,1,8),12,new Color(.92f,.82f,.66f),true);
    Lamp(room,c+new Vector3(0,3.3f,-3),c+new Vector3(0,0,-1),6,new Color(.65f,.72f,.8f),false);
    Painting(c+new Vector3(3.1f,2.15f,4.73f),0,0);Painting(c+new Vector3(-3.1f,2.15f,4.73f),0,1);
    Bench(c+new Vector3(3.6f,0,-3),0);
   } else if(room==0) {
    Lamp(room,c+new Vector3(-1,3.25f,-1),c+new Vector3(-1,0,0),12,new Color(1,.72f,.43f),true);
    Lamp(room,c+new Vector3(2.5f,3.2f,2),c+new Vector3(0,0,2),6,new Color(1,.79f,.54f),false);
    Box("Staff noticeboard",c+new Vector3(0,2,-4.7f),new Vector3(3.5f,1.5f,.12f),oldWood);
    Sign("NIGHT SHIFT / 00:00 — 06:00\nDeliver. Inspect. Return.\nReport all irregularities to management.",c+new Vector3(0,2,-4.6f),Quaternion.Euler(0,180,0),.044f);
    Lamp(room,c+new Vector3(1,2.8f,-1),c+new Vector3(0,1.4f,4),3,new Color(1,.75f,.46f),false);
    Bench(c+new Vector3(3.3f,0,-3),90);
   } else if(room==3) {
    Lamp(room,c+new Vector3(-2,3.25f,-2),c+new Vector3(-1,0,1),6.5f,new Color(.68f,.75f,.77f),true);
    Lamp(room,c+new Vector3(2,3.25f,1),c,0,Color.white,false,false,false);
    foreach(float z in new[]{1.2f,2.5f}) {
     Box("Storage shelf",new Vector3(-14,.9f,z),new Vector3(1,1.8f,.12f),metal);
     Box("Dust covered carton",new Vector3(-14,1.1f,z),new Vector3(.8f,.6f,.65f),oldWood);
    }
   } else {
    Lamp(room,c+Vector3.up*3.25f,c+new Vector3(0,0,1),room==1?4:6,new Color(.62f,.72f,.76f),true);
    Lamp(room,c+new Vector3(3,2.8f,-3),c+new Vector3(2,0,-3),1.8f,new Color(.55f,.07f,.035f),false,true);
    Box("Vent grille",c+new Vector3(3,2.8f,4.8f),new Vector3(1,.4f,.1f),metal);
    for(int i=0;i<6;i++)Box("Grille slat",c+new Vector3(2.6f+i*.16f,2.8f,4.72f),new Vector3(.035f,.35f,.05f),brass);
   }
  }
  static void Lamp(int room,Vector3 position,Vector3 target,float intensity,Color color,bool shadows,bool emergency=false,bool on=true) {
   var housing=Box("Ceiling fixture "+Names[room],position,new Vector3(.65f,.16f,.32f),metal);
   var diffuser=Box("Lamp diffuser",position-Vector3.up*.095f,new Vector3(.54f,.035f,.23f),bulb);
   var light=new GameObject("Local spotlight").AddComponent<Light>();light.transform.SetParent(housing.transform,true);light.transform.position=position-Vector3.up*.18f;light.transform.rotation=Quaternion.LookRotation(target-light.transform.position);
   light.type=LightType.Spot;light.spotAngle=room==0?100:room==5?62:85;light.innerSpotAngle=light.spotAngle*.55f;light.range=room==5?8:9;light.intensity=intensity;light.color=color;light.shadows=shadows?LightShadows.Soft:LightShadows.None;light.shadowResolution=LightShadowResolution.Medium;light.shadowBias=.025f;light.shadowNormalBias=.16f;light.shadowStrength=.9f;
   var lamp=housing.AddComponent<HorrorLamp>();lamp.source=light;lamp.bulb=diffuser.GetComponent<Renderer>();lamp.room=room;lamp.intensity=intensity;lamp.emission=color;lamp.emergency=emergency;lamp.normallyOn=on;lamp.Apply(on?1:0);lamps.Add(lamp);
  }
  static void DoorFrame(Vector3 center,float yaw) {
   var q=Quaternion.Euler(0,yaw,0);
   foreach(float side in new[]{-1f,1f}){var leg=Box("Stone door surround",center+q*new Vector3(side*1.32f,1.4f,0),new Vector3(.18f,2.8f,.38f),stone);leg.transform.rotation=q;}
   for(int i=0;i<7;i++){float angle=i*Mathf.PI/6;var p=center+q*new Vector3(Mathf.Cos(angle)*1.3f,2.75f+Mathf.Sin(angle)*.45f,0);var block=Box("Arch voussoir",p,new Vector3(.44f,.23f,.38f),stone);block.transform.rotation=q*Quaternion.Euler(0,0,(angle*Mathf.Rad2Deg)-90);}
  }
  static void Bench(Vector3 point,float yaw) {
   var q=Quaternion.Euler(0,yaw,0);var seat=Box("Museum bench",point+Vector3.up*.5f,new Vector3(1.7f,.14f,.5f),oldWood,true);seat.transform.rotation=q;
   foreach(float x in new[]{-.65f,.65f})Box("Bench foot",point+q*new Vector3(x,.24f,0),new Vector3(.12f,.48f,.4f),metal);
  }
  static void Painting(Vector3 point,float yaw,int variant) {
   var q=Quaternion.Euler(0,yaw,0);var frame=Box("Collection frame",point,new Vector3(1.5f,1.8f,.12f),oldWood);frame.transform.rotation=q;
   var canvas=Box("Faded collection portrait",point+q*new Vector3(0,0,-.07f),new Vector3(1.25f,1.55f,.02f),plaster);canvas.transform.rotation=q;
   // Abstract relief silhouette; deliberately spare and low-poly.
   var silhouette=Box("Portrait silhouette",point+q*new Vector3(0,-.2f,-.09f),new Vector3(.5f,.7f,.015f),stone);silhouette.transform.rotation=q;
   var head=Box("Portrait head",point+q*new Vector3(variant==0?-.08f:.08f,.28f,-.09f),new Vector3(.32f,.38f,.02f),metal);head.transform.rotation=q;
  }
  static void Displays() {
   foreach(var display in UnityEngine.Object.FindObjectsByType<DisplayCase>(FindObjectsSortMode.None)) {
    Vector3 center=display.transform.position;float top=center.y+.65f;
    Box("Glass display enclosure",new Vector3(center.x,top+.43f,center.z),new Vector3(1.5f,.86f,1.1f),glass);
    foreach(float x in new[]{-.76f,.76f})foreach(float z in new[]{-.56f,.56f})Box("Display frame",new Vector3(center.x+x,top+.45f,center.z+z),new Vector3(.035f,.95f,.035f),brass);
    Box("Display cap",new Vector3(center.x,top+.9f,center.z),new Vector3(1.57f,.07f,1.17f),metal);
    Sign("COLLECTION",center+new Vector3(0,.25f,-.512f),Quaternion.identity,.022f);
   }
   foreach(float x in new[]{-2.1f,2.1f}) {
    Box("Museum rail post",new Vector3(x,.45f,7.15f),new Vector3(.07f,.9f,.07f),brass);
    Box("Museum rail",new Vector3(x,.8f,7.5f),new Vector3(.05f,.05f,.7f),metal);
   }
  }
  static void Statue(Watcher watcher,HorrorDirector director) {
   foreach(var child in watcher.transform.Cast<Transform>().ToArray())UnityEngine.Object.DestroyImmediate(child.gameObject);
   var capsule=watcher.gameObject.AddComponent<CapsuleCollider>();capsule.center=Vector3.up*1.05f;capsule.height=2.1f;capsule.radius=.33f;
   var statueStone=Material("Carved limestone",new Color(.44f,.45f,.43f),.98f,1);
   void Part(string name,Vector3 p,Vector3 scale,PrimitiveType type) {
    var go=GameObject.CreatePrimitive(type);go.name=name;go.transform.SetParent(watcher.transform,false);go.transform.localPosition=p;go.transform.localScale=scale;go.GetComponent<Renderer>().sharedMaterial=statueStone;UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
   }
   Part("Carved torso",new Vector3(0,1.3f,0),new Vector3(.60f,.43f,.33f),PrimitiveType.Capsule);
   Part("Neck",new Vector3(0,1.74f,0),new Vector3(.19f,.13f,.19f),PrimitiveType.Cylinder);
   Part("Featureless head",new Vector3(0,1.95f,0),new Vector3(.35f,.39f,.30f),PrimitiveType.Sphere);
   foreach(float side in new[]{-1f,1f}) {
    Part("Carved leg",new Vector3(side*.16f,.54f,0),new Vector3(.19f,.47f,.23f),PrimitiveType.Cylinder);
    Part("Shoe",new Vector3(side*.16f,.08f,.055f),new Vector3(.24f,.16f,.38f),PrimitiveType.Cube);
    Part("Hanging arm",new Vector3(side*.37f,1.19f,0),new Vector3(.15f,.42f,.16f),PrimitiveType.Cylinder);
   }
   foreach(var old in watcher.positions)if(old)UnityEngine.Object.DestroyImmediate(old.gameObject);
   var points=new[]{new Vector3(0,0,6),new Vector3(1.8f,0,8.8f),new Vector3(-3.8f,0,9),new Vector3(3.6f,0,-8.5f),new Vector3(8,0,9.05f),new Vector3(-3.5f,0,3.5f),new Vector3(6.3f,0,4.6f),new Vector3(-1.8f,0,2.2f),new Vector3(2.8f,0,2.1f)};
   watcher.positions=points.Select((p,i)=>{var t=new GameObject("Watcher stage position "+i).transform;t.SetParent(decor);t.position=p;return t;}).ToArray();
   watcher.activationDelay=new Vector2(115,170);watcher.moveCooldown=42;
   var staging=watcher.gameObject.AddComponent<WatcherStaging>();staging.watcher=watcher;staging.director=director;
  }
  static void ZoneLines(ObjectiveZone zone,string label) {
   Vector3 c=zone.transform.position;Vector3 size=zone.GetComponent<BoxCollider>().size;c.y=.04f;
   foreach(float sign in new[]{-1f,1f}) {
    Box("Receiving zone edge",c+Vector3.right*size.x*.5f*sign,new Vector3(.035f,.01f,size.z),brass);
    Box("Receiving zone edge",c+Vector3.forward*size.z*.5f*sign,new Vector3(size.x,.01f,.035f),brass);
   }
   if(zone.requiredKind!="statue")Sign(label,c+Vector3.up*.015f,Quaternion.Euler(90,0,0),.035f);
  }
  static void MakeSigns() {
   for(int i=0;i<6;i++) {
    Vector3 c=Centers[i];Box("Room plaque",c+new Vector3(0,2.96f,4.76f),new Vector3(2.8f,.48f,.08f),metal);
    Sign(Names[i],c+new Vector3(0,2.96f,4.71f),Quaternion.identity,.055f);
   }
   Sign("STORAGE  →",new Vector3(-10,2.35f,-.2f),Quaternion.identity,.055f);
   Sign("MAIN HALL  →",new Vector3(0,2.35f,-.2f),Quaternion.identity,.055f);
   Sign("GALLERY A",new Vector3(10,2.35f,-.2f),Quaternion.identity,.055f);
  }
  static void VolumeAndPipeline() {
   string path=Root+"/ScriptableObjects/HorrorVolume.asset";var profile=AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
   if(!profile){profile=ScriptableObject.CreateInstance<VolumeProfile>();AssetDatabase.CreateAsset(profile,path);}
   T Component<T>() where T:VolumeComponent {if(profile.TryGet<T>(out var c))return c;c=profile.Add<T>(true);AssetDatabase.AddObjectToAsset(c,profile);return c;}
   var vignette=Component<Vignette>();vignette.intensity.Override(.28f);vignette.smoothness.Override(.46f);
   var color=Component<ColorAdjustments>();color.postExposure.Override(.2f);color.contrast.Override(12);color.saturation.Override(-18);
   var bloom=Component<Bloom>();bloom.intensity.Override(.18f);bloom.threshold.Override(1.1f);bloom.scatter.Override(.55f);
   var grain=Component<FilmGrain>();grain.type.Override(FilmGrainLookup.Thin1);grain.intensity.Override(.15f);grain.response.Override(.7f);
   Component<ChromaticAberration>().intensity.Override(.012f);Component<Tonemapping>().mode.Override(TonemappingMode.Neutral);
   EditorUtility.SetDirty(profile);
   var volume=new GameObject("Horror Global Volume").AddComponent<Volume>();volume.isGlobal=true;volume.priority=10;volume.sharedProfile=profile;
   string rpPath=Root+"/ScriptableObjects/HorrorPipeline.asset";
   var pipeline=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(rpPath);
   if(!pipeline){AssetDatabase.CopyAsset("Assets/Settings/PC_RPAsset.asset",rpPath);pipeline=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(rpPath);}
   var settingsAsset=new SerializedObject(pipeline);settingsAsset.FindProperty("m_AdditionalLightShadowsSupported").boolValue=true;settingsAsset.FindProperty("m_SoftShadowsSupported").boolValue=true;settingsAsset.ApplyModifiedPropertiesWithoutUndo();pipeline.shadowDistance=24;pipeline.shadowDepthBias=.2f;pipeline.shadowNormalBias=.25f;pipeline.renderScale=.9f;
   EditorUtility.SetDirty(pipeline);
   var settings=new GameObject("Horror renderer settings").AddComponent<HorrorRenderSettings>();settings.pipeline=pipeline;
  }
  static void MakePlayer() {
   var prefab=PrefabUtility.LoadPrefabContents(Root+"/Prefabs/Player.prefab");
   var player=prefab.GetComponent<PlayerController>();player.bodyRenderer.sharedMaterial=Material("Worker uniform",new Color(.20f,.23f,.21f),.95f);
   player.viewCamera.GetUniversalAdditionalCameraData().renderPostProcessing=true;player.viewCamera.allowHDR=true;
   var asset=PrefabUtility.SaveAsPrefabAsset(prefab,Root+"/Prefabs/HorrorPlayer.prefab");PrefabUtility.UnloadPrefabContents(prefab);
   var manager=UnityEngine.Object.FindFirstObjectByType<NetworkManager>();manager.NetworkConfig.PlayerPrefab=asset;manager.NetworkConfig.Prefabs.Add(new NetworkPrefab{Prefab=asset});
  }
  public static void ValidateSetup() {
   var director=UnityEngine.Object.FindFirstObjectByType<HorrorDirector>();var volume=UnityEngine.Object.FindFirstObjectByType<Volume>();
   if(!director || director.lamps==null || director.lamps.Length<10 || director.lamps.Any(l=>!l||!l.source||!l.bulb))throw new Exception("Horror light references incomplete");
   if(!director.ambience || director.ambience.roomSources.Length!=6 || director.ambience.roomSources.Any(p=>!p))throw new Exception("Horror ambience origins missing");
   if(!volume || !volume.sharedProfile || !volume.sharedProfile.Has<Bloom>() || !volume.sharedProfile.Has<ColorAdjustments>())throw new Exception("Horror volume incomplete");
   if(!UnityEngine.Object.FindFirstObjectByType<WatcherStaging>())throw new Exception("Watcher staging missing");
   if(!UnityEngine.Object.FindFirstObjectByType<HorrorRenderSettings>().pipeline)throw new Exception("Horror URP asset missing");
   UnityEngine.Physics.SyncTransforms();
   if(director.watcher.positions.Count(p=>!UnityEngine.Physics.CheckCapsule(p.position+Vector3.up*.48f,p.position+Vector3.up*1.8f,.43f,~0,QueryTriggerInteraction.Ignore))<4)throw new Exception("Not enough unobstructed Watcher staging positions");
   Debug.Log("HORROR VALIDATION PASSED: lights, ambience, volume, rendering, Watcher staging.");
  }
  public static void BuildMac() {
   Build();var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{Path},locationPathName="Builds/macOS/DoNotTouch_Horror.app",target=BuildTarget.StandaloneOSX,options=BuildOptions.Development});
   if(report.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded)throw new Exception("Horror macOS build failed");
  }
 }
}
