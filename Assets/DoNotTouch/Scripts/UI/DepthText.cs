using UnityEngine;
namespace DoNotTouch {
 // The built-in TextMesh GUI shader ignores depth. Keep its dynamic font atlas,
 // but use our depth-tested world material so labels no longer reveal other rooms.
 [RequireComponent(typeof(TextMesh))]
 public class DepthText : MonoBehaviour {
  public Material template;
  Material instance; TextMesh textMesh;
  void OnEnable(){textMesh=GetComponent<TextMesh>();instance=new Material(template);textMesh.font.RequestCharactersInTexture(textMesh.text,textMesh.fontSize,FontStyle.Normal);Refresh(textMesh.font);GetComponent<Renderer>().sharedMaterial=instance;Font.textureRebuilt+=Refresh;}
  void Refresh(Font font){if(font && textMesh && font==textMesh.font && instance)instance.mainTexture=font.material.mainTexture;}
  void OnDisable(){Font.textureRebuilt-=Refresh;if(instance)Destroy(instance);}
 }
}
