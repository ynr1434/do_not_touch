using NUnit.Framework;
using UnityEngine;
namespace DoNotTouch.Tests {
 public class Night01RulesTests {
  [TestCase(1,4)][TestCase(2,5)][TestCase(3,6)][TestCase(4,6)] public void ShipmentScalesWithPlayers(int players,int expected)=>Assert.AreEqual(expected,NightRules.ShipmentTarget(players));
  [Test] public void PhaseProgressionRequiresEveryNormalTask(){Assert.IsFalse(NightRules.CanAdvance(NightPhase.NormalShift,4,4,2,5,false,false,false,false,false,false,false,false,false,false));Assert.IsTrue(NightRules.CanAdvance(NightPhase.NormalShift,4,4,3,5,false,false,false,false,false,false,false,false,false,false));}
  [TestCase(NightPhase.FirstAnomalies,WatcherState.Awake)][TestCase(NightPhase.NormalShift,WatcherState.Dormant)][TestCase(NightPhase.WatcherActive,WatcherState.Stalking)][TestCase(NightPhase.RestrictedArea,WatcherState.Stalking)][TestCase(NightPhase.FinalTask,WatcherState.Hunting)] public void WatcherStateTracksScenario(NightPhase phase,WatcherState state)=>Assert.AreEqual(state,NightRules.StateForPhase(phase));
  [Test] public void SoloWatcherIsSlowerAndHeavyCarryRemainsPossible(){Assert.Greater(NightRules.WatcherCooldown(1,WatcherState.Hunting),NightRules.WatcherCooldown(4,WatcherState.Hunting));Assert.Greater(NightRules.HeavyCarrySpeed(1),0);Assert.Greater(NightRules.HeavyCarrySpeed(4),NightRules.HeavyCarrySpeed(1));}
  [Test] public void FuseMustPrecedeBreaker(){Assert.IsFalse(NightRules.CanRestorePower(false,false));Assert.IsFalse(NightRules.CanRestorePower(true,true));Assert.IsTrue(NightRules.CanRestorePower(true,false));}
  [Test] public void RegistryDistinguishesKnownAndUnknown(){var registry=ScriptableObject.CreateInstance<ExhibitRegistry>();registry.entries.Add(new ExhibitRegistry.Entry{id="0241",label="Study",registered=true});StringAssert.Contains("ЗАРЕГИСТРИРОВАН",registry.Read("0241"));StringAssert.Contains("отсутствует",registry.Read("9999"));Object.DestroyImmediate(registry);}
 }
}
