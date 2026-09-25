using NUnit.Framework;
using UnityEngine;
namespace DoNotTouch.Tests {
 public class ShiftRulesTests {
  [TestCase(-1,3,0)][TestCase(2,3,2)][TestCase(8,3,3)]
  public void ObjectiveProgressIsBounded(int n,int target,int expected)=>Assert.AreEqual(expected,ShiftRules.Progress(n,target));
  [TestCase(0,"00:00")][TestCase(225,"01:30")][TestCase(900,"06:00")][TestCase(1000,"06:00")]
  public void ShiftClockConversion(float elapsed,string expected)=>Assert.AreEqual(expected,ShiftRules.Clock(elapsed,900));
  [Test] public void VisibilityRequiresAngleAndDistance() {
   Assert.IsTrue(ShiftRules.InView(Vector3.zero,Vector3.forward,new Vector3(0,0,5),90,10));
   Assert.IsFalse(ShiftRules.InView(Vector3.zero,Vector3.forward,new Vector3(0,0,-5),90,10));
   Assert.IsFalse(ShiftRules.InView(Vector3.zero,Vector3.forward,new Vector3(0,0,11),90,10));
   Assert.IsFalse(ShiftRules.InView(Vector3.zero,Vector3.forward,new Vector3(8,0,1),90,10));
  }
  [Test] public void WatcherNeverMovesWhenObserved() {
   Assert.IsFalse(Watcher.MayMove(true,true,100,0));
   Assert.IsFalse(Watcher.MayMove(false,false,100,0));
   Assert.IsFalse(Watcher.MayMove(true,false,2,10));
   Assert.IsTrue(Watcher.MayMove(true,false,100,0));
  }
 }
}
