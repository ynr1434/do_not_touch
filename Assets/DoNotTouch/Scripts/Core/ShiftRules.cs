using UnityEngine;
namespace DoNotTouch {
 public static class ShiftRules {
  public static int Progress(int count, int target) => Mathf.Clamp(count, 0, Mathf.Max(0,target));
  public static string Clock(float elapsed, float duration) {
   int minutes = Mathf.FloorToInt(Mathf.Clamp01(elapsed / Mathf.Max(1,duration))*360);
   return $"{minutes/60:00}:{minutes%60:00}";
  }
  public static bool InView(Vector3 eye, Vector3 forward, Vector3 target, float angle, float distance) {
   var delta=target-eye;
   return delta.sqrMagnitude <= distance*distance && Vector3.Angle(forward,delta)<=angle*.5f;
  }
 }
}
