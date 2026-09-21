using BlackHearthx.Hearthline;
using Xunit;

namespace Hearthline.Tests;

public class WildHerdTests
{
	[Fact]
	public void TamedAdultsAlwaysMayStart()
	{
		Assert.True(WildHerd.MayStartProcreate(parentTamed: true, wildEnabled: false, hasVanillaOffspring: false));
	}

	[Fact]
	public void WildAdultNeedsOptionAndOffspringPrefab()
	{
		Assert.False(WildHerd.MayStartProcreate(false, wildEnabled: false, hasVanillaOffspring: true));
		Assert.False(WildHerd.MayStartProcreate(false, wildEnabled: true, hasVanillaOffspring: false));
		Assert.True(WildHerd.MayStartProcreate(false, wildEnabled: true, hasVanillaOffspring: true));
	}

	[Fact]
	public void PartnerScanKeepsVanillaReady()
	{
		Assert.True(WildHerd.MayCountAsPartner(true, isTamed: true, wildEnabled: true, hasVanillaOffspring: true, isPregnant: false, isHungry: false, ignoreHunger: true));
	}

	[Fact]
	public void WildPartnerRequiresNotPregnant()
	{
		Assert.False(WildHerd.MayCountAsPartner(false, isTamed: false, wildEnabled: true, hasVanillaOffspring: true, isPregnant: true, isHungry: false, ignoreHunger: true));
		Assert.True(WildHerd.MayCountAsPartner(false, isTamed: false, wildEnabled: true, hasVanillaOffspring: true, isPregnant: false, isHungry: true, ignoreHunger: true));
		Assert.False(WildHerd.MayCountAsPartner(false, isTamed: false, wildEnabled: true, hasVanillaOffspring: true, isPregnant: false, isHungry: true, ignoreHunger: false));
	}

	[Fact]
	public void HungerBypassOnlyDuringWildProcreate()
	{
		Assert.False(WildHerd.ShouldIgnoreHunger(ignoreHunger: true, isTamed: true, inProcreate: true, hasVanillaOffspring: true));
		Assert.False(WildHerd.ShouldIgnoreHunger(ignoreHunger: true, isTamed: false, inProcreate: false, hasVanillaOffspring: true));
		Assert.True(WildHerd.ShouldIgnoreHunger(ignoreHunger: true, isTamed: false, inProcreate: true, hasVanillaOffspring: true));
	}

	[Fact]
	public void WildPregnancyRespectsChance()
	{
		Assert.False(WildHerd.MayWildPregnancy(rollPercent: 40f, chancePercent: 30f));
		Assert.True(WildHerd.MayWildPregnancy(rollPercent: 30f, chancePercent: 30f));
		Assert.False(WildHerd.MayWildPregnancy(rollPercent: 1f, chancePercent: 0f));
		Assert.True(WildHerd.MayWildPregnancy(rollPercent: 99f, chancePercent: 100f));
	}

	[Fact]
	public void WildFamilySpawnUsesSameRollSemantics()
	{
		Assert.True(WildHerd.MaySpawnWildFamily(25f, chancePercent: 25f));
		Assert.False(WildHerd.MaySpawnWildFamily(26f, chancePercent: 25f));
		Assert.False(WildHerd.MaySpawnWildFamily(1f, chancePercent: 0f));
	}

	[Fact]
	public void LiveOffspringIsGrowupNotEgg()
	{
		Assert.True(WildHerd.IsLiveOffspringPrefab(hasGrowup: true, hasItemDrop: false));
		Assert.False(WildHerd.IsLiveOffspringPrefab(hasGrowup: false, hasItemDrop: true));
		Assert.False(WildHerd.IsLiveOffspringPrefab(hasGrowup: true, hasItemDrop: true));
	}

	[Fact]
	public void IsYoungFromGrowupOrNonBreeder()
	{
		Assert.True(WildHerd.IsYoungAnimal(hasGrowup: true, hasTameable: true, hasProcreation: true));
		Assert.True(WildHerd.IsYoungAnimal(hasGrowup: false, hasTameable: true, hasProcreation: false));
		Assert.False(WildHerd.IsYoungAnimal(hasGrowup: false, hasTameable: true, hasProcreation: true));
	}

	[Fact]
	public void ParentSpeciesMatchesCubPrefabs()
	{
		Assert.True(WildHerd.IsParentSpeciesOf("Boar", "Boar_piggy"));
		Assert.True(WildHerd.IsParentSpeciesOf("Wolf", "Wolf_cub"));
		Assert.True(WildHerd.IsParentSpeciesOf("Lox", "Lox_Calf"));
		Assert.False(WildHerd.IsParentSpeciesOf("Boar", "Wolf_cub"));
		Assert.False(WildHerd.IsParentSpeciesOf("Boar", "Boar"));
	}

	[Fact]
	public void StealRequiresWildYoung()
	{
		Assert.False(WildHerd.MayStealYoung(isTamed: true, isYoung: true, hasTameable: true, stealEnabled: true));
		Assert.False(WildHerd.MayStealYoung(isTamed: false, isYoung: false, hasTameable: true, stealEnabled: true));
		Assert.False(WildHerd.MayStealYoung(isTamed: false, isYoung: true, hasTameable: true, stealEnabled: false));
		Assert.True(WildHerd.MayStealYoung(isTamed: false, isYoung: true, hasTameable: true, stealEnabled: true));
		// Vanilla Boar_piggy / Wolf_cub / Lox_Calf: Growup + AnimalAI, no Tameable on prefab.
		Assert.True(WildHerd.MayStealYoung(isTamed: false, isYoung: true, hasTameable: false, stealEnabled: true));
	}

	[Fact]
	public void CarryRequiresTamedYoung()
	{
		Assert.False(WildHerd.MayCarryYoung(isTamed: false, isYoung: true, carryEnabled: true));
		Assert.False(WildHerd.MayCarryYoung(isTamed: true, isYoung: false, carryEnabled: true));
		Assert.True(WildHerd.MayCarryYoung(isTamed: true, isYoung: true, carryEnabled: true));
	}

	[Theory]
	[InlineData("Boar_piggy", true)]
	[InlineData("Boar_piggy(Clone)", true)]
	[InlineData("Wolf_cub", true)]
	[InlineData("Lox_Calf", true)]
	[InlineData("Boar", false)]
	[InlineData("Wolf", false)]
	public void YoungPrefabNamesMatchFallbacks(string prefab, bool expected)
	{
		Assert.Equal(expected, WildHerd.IsYoungPrefabName(prefab));
	}
}
