using NUnit.Framework;
namespace DoNotTouch.Tests {
 public class HorrorRulesTests {
  [Test] public void StaffAndEmergencyLightsRemainOnDuringBlackout() {
   Assert.AreEqual(1,HorrorDirector.LightLevel(0,false,true,4,4,1,5,1));
   Assert.AreEqual(1,HorrorDirector.LightLevel(1,true,true,4,4,1,5,1));
   Assert.Less(HorrorDirector.LightLevel(4,false,true,4,4,1,5,1),.02f);
  }
  [Test] public void SectionOutageAffectsOnlyItsRoomAndRestores() {
   Assert.AreEqual(1,HorrorDirector.LightLevel(5,false,true,3,4,1,5,1));
   Assert.Less(HorrorDirector.LightLevel(4,false,true,3,4,1,5,1),.02f);
   Assert.AreEqual(1,HorrorDirector.LightLevel(4,false,true,3,4,5,5,1));
   Assert.AreEqual(0,HorrorDirector.LightLevel(4,false,false,3,4,6,5,1));
  }
  [Test] public void FlickerUsesDeterministicNetworkTimeline() {
   for(int i=0;i<100;i++)Assert.AreEqual(HorrorDirector.LightLevel(4,false,true,2,4,i*.07,7,23),HorrorDirector.LightLevel(4,false,true,2,4,i*.07,7,23));
  }
  [TestCase(0,0)][TestCase(195,0)][TestCase(300,1)][TestCase(500,2)][TestCase(800,3)]
  public void TensionRisesGradually(float elapsed,int stage)=>Assert.AreEqual(stage,HorrorDirector.Stage(elapsed,900));
 }
}
