using UnityEngine;
namespace DoNotTouch {
 public class HorrorLamp : MonoBehaviour {
  public int room;
  public bool emergency, normallyOn=true;
  public Light source;
  public Renderer bulb;
  public float intensity=5;
  public Color emission=new(1,.75f,.4f);
  MaterialPropertyBlock block; float previous=-1;
  void OnEnable(){previous=-1;Apply(normallyOn?1:0);}
  public void Apply(float multiplier) {
   if(Mathf.Abs(previous-multiplier)<.005f)return;previous=multiplier;
   if(source)source.intensity=intensity*multiplier;
   if(bulb){block??=new MaterialPropertyBlock();block.SetColor("_EmissionColor",emission*multiplier*2);block.SetColor("_BaseColor",Color.Lerp(new Color(.09f,.09f,.08f),emission,multiplier));bulb.SetPropertyBlock(block);}
  }
 }
}
