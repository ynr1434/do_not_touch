using Unity.Netcode;
using UnityEngine;
namespace DoNotTouch {
 public enum AnomalyPresentation { ChangedPainting, OpenCase, ExtraExhibit }
 public class NightAnomalyProp : NetworkBehaviour {
  public AnomalyPresentation presentation;
  public Renderer targetRenderer;
  public Transform movingPart;
  public NetworkVariable<bool> Revealed=new();
  MaterialPropertyBlock block;
  void Update(){
   if(targetRenderer)targetRenderer.enabled=presentation!=AnomalyPresentation.ExtraExhibit||Revealed.Value;
   if(movingPart&&presentation==AnomalyPresentation.OpenCase)movingPart.localRotation=Quaternion.RotateTowards(movingPart.localRotation,Quaternion.Euler(Revealed.Value?-55:0,0,0),Time.deltaTime*90);
   if(targetRenderer&&presentation==AnomalyPresentation.ChangedPainting){block??=new MaterialPropertyBlock();block.SetColor("_BaseColor",Revealed.Value?new Color(.20f,.09f,.08f):new Color(.28f,.27f,.24f));targetRenderer.SetPropertyBlock(block);}
  }
 }
}
