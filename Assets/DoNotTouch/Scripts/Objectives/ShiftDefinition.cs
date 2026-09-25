using UnityEngine;
namespace DoNotTouch {
 [CreateAssetMenu(menuName="Do Not Touch/Shift Definition")]
 public class ShiftDefinition : ScriptableObject {
  public float durationSeconds=900;
  public int crateTarget=3, inspectionTarget=3, statueTarget=1;
  public string crateTask="Move artifact crates to Gallery A";
  public string inspectTask="Inspect display cases";
  public string statueTask="Return the statue";
 }
}
