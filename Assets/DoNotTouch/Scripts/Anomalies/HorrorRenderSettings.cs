using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
namespace DoNotTouch {
 public class HorrorRenderSettings : MonoBehaviour {
  public UniversalRenderPipelineAsset pipeline;
  RenderPipelineAsset previous;
  void OnEnable(){previous=QualitySettings.renderPipeline;if(pipeline)QualitySettings.renderPipeline=pipeline;}
  void OnDisable(){if(QualitySettings.renderPipeline==pipeline)QualitySettings.renderPipeline=previous;}
 }
}
