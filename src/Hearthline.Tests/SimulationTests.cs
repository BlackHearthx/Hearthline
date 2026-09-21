using BlackHearthx.Hearthline;
using Xunit;

namespace Hearthline.Tests;

/// <summary>
/// In-process simulation of decision trees (no Valheim / Unity runtime).
/// Prefab facts from Jötunn character-list: Boar_piggy / Wolf_cub / Lox_Calf = Growup+AnimalAI, no Tameable.
/// </summary>
public class SimulationTests
{
	[Fact]
	public void Sim_BoarPiggy_Wild_StealSucceedsWithoutTameable()
	{
		Assert.True(WildHerd.IsYoungAnimal(hasGrowup: true, hasTameable: false, hasProcreation: false));
		Assert.True(WildHerd.IsYoungPrefabName("Boar_piggy(Clone)"));

		StealCarryLogic.Outcome o = StealCarryLogic.Evaluate(
			alreadyCarrying: false,
			carryEnabled: true,
			claimEnabled: true,
			isYoung: true,
			isTamedAdult: false,
			isTamed: false,
			tameSucceeded: true);

		Assert.Equal(StealCarryLogic.Outcome.OkStolenCarry, o);
		Assert.Equal("You took the young", StealCarryLogic.PlayerMessage(o));
	}

	[Theory]
	[InlineData("Boar_piggy")]
	[InlineData("Wolf_cub")]
	[InlineData("Lox_Calf")]
	public void Sim_VanillaCubNamesAreYoung(string prefab)
	{
		Assert.True(WildHerd.IsYoungPrefabName(prefab));
	}

	[Fact]
	public void Sim_WildAdult_Refused()
	{
		StealCarryLogic.Outcome o = StealCarryLogic.Evaluate(
			alreadyCarrying: false,
			carryEnabled: true,
			claimEnabled: true,
			isYoung: false,
			isTamedAdult: false,
			isTamed: false,
			tameSucceeded: false);

		Assert.Equal(StealCarryLogic.Outcome.RefuseWildAdult, o);
		Assert.Equal("Only tamed adults", StealCarryLogic.PlayerMessage(o));
	}

	[Fact]
	public void Sim_TamedAdult_Carry()
	{
		Assert.True(WildHerd.MayCarryTamedAdult(isTamed: true, isYoung: false, hasTameable: true, carryEnabled: true));
		Assert.False(WildHerd.MayCarryTamedAdult(isTamed: false, isYoung: false, hasTameable: true, carryEnabled: true));

		StealCarryLogic.Outcome o = StealCarryLogic.Evaluate(
			alreadyCarrying: false,
			carryEnabled: true,
			claimEnabled: true,
			isYoung: false,
			isTamedAdult: true,
			isTamed: true,
			tameSucceeded: true);

		Assert.Equal(StealCarryLogic.Outcome.OkCarry, o);
		Assert.Equal("Carrying", StealCarryLogic.PlayerMessage(o, isYoung: false));
	}

	[Fact]
	public void Sim_TamedCub_CarryOnly()
	{
		StealCarryLogic.Outcome o = StealCarryLogic.Evaluate(
			alreadyCarrying: false,
			carryEnabled: true,
			claimEnabled: true,
			isYoung: true,
			isTamedAdult: false,
			isTamed: true,
			tameSucceeded: true);

		Assert.Equal(StealCarryLogic.Outcome.OkCarry, o);
		Assert.Equal("Carrying young", StealCarryLogic.PlayerMessage(o, isYoung: true));
	}

	[Fact]
	public void Sim_ClaimDisabled_CannotStealMessage()
	{
		StealCarryLogic.Outcome o = StealCarryLogic.Evaluate(
			alreadyCarrying: false,
			carryEnabled: true,
			claimEnabled: false,
			isYoung: true,
			isTamedAdult: false,
			isTamed: false,
			tameSucceeded: false);

		Assert.Equal(StealCarryLogic.Outcome.RefuseClaimDisabled, o);
		Assert.Equal("Cannot steal this", StealCarryLogic.PlayerMessage(o));
	}

	[Fact]
	public void Sim_TameFails_OwnerMessage()
	{
		StealCarryLogic.Outcome o = StealCarryLogic.Evaluate(
			alreadyCarrying: false,
			carryEnabled: true,
			claimEnabled: true,
			isYoung: true,
			isTamedAdult: false,
			isTamed: false,
			tameSucceeded: false);

		Assert.Equal(StealCarryLogic.Outcome.RefuseTameFailed, o);
		Assert.Equal("Steal failed — not owner yet", StealCarryLogic.PlayerMessage(o));
	}

	[Fact]
	public void Sim_StealCooldown_Refused()
	{
		StealCarryLogic.Outcome o = StealCarryLogic.Evaluate(
			alreadyCarrying: false,
			carryEnabled: true,
			claimEnabled: true,
			isYoung: true,
			isTamedAdult: false,
			isTamed: false,
			tameSucceeded: true,
			stealOnCooldown: true,
			hasStealStamina: true);

		Assert.Equal(StealCarryLogic.Outcome.RefuseStealCooldown, o);
		Assert.Equal("Steal cooldown", StealCarryLogic.PlayerMessage(o));
	}

	[Fact]
	public void Sim_StealStamina_Refused()
	{
		StealCarryLogic.Outcome o = StealCarryLogic.Evaluate(
			alreadyCarrying: false,
			carryEnabled: true,
			claimEnabled: true,
			isYoung: true,
			isTamedAdult: false,
			isTamed: false,
			tameSucceeded: true,
			stealOnCooldown: false,
			hasStealStamina: false);

		Assert.Equal(StealCarryLogic.Outcome.RefuseStealStamina, o);
		Assert.Equal("Too tired to steal", StealCarryLogic.PlayerMessage(o));
	}

	[Fact]
	public void Sim_TamedCarry_IgnoresStealGates()
	{
		StealCarryLogic.Outcome o = StealCarryLogic.Evaluate(
			alreadyCarrying: false,
			carryEnabled: true,
			claimEnabled: true,
			isYoung: true,
			isTamedAdult: false,
			isTamed: true,
			tameSucceeded: true,
			stealOnCooldown: true,
			hasStealStamina: false);

		Assert.Equal(StealCarryLogic.Outcome.OkCarry, o);
	}

	[Fact]
	public void Sim_MayCarryTarget_YoungOrTamedAdult()
	{
		Assert.True(WildHerd.MayCarryTarget(isTamed: true, isYoung: true, hasTameable: false, carryEnabled: true));
		Assert.True(WildHerd.MayCarryTarget(isTamed: true, isYoung: false, hasTameable: true, carryEnabled: true));
		Assert.False(WildHerd.MayCarryTarget(isTamed: false, isYoung: false, hasTameable: true, carryEnabled: true));
	}

	[Fact]
	public void Sim_WildPregnancy_ThrottleAt30Percent()
	{
		int allowed = 0;
		for (int roll = 0; roll <= 100; roll++)
		{
			if (WildHerd.MayWildPregnancy(roll, chancePercent: 30f))
			{
				allowed++;
			}
		}

		Assert.Equal(31, allowed);
	}

	[Fact]
	public void Sim_StarUpgrade_Parent0Star_Becomes1StarWhenRolled()
	{
		int min = Bloodline.RaisedMinOffspringLevel(1, parentLevel: 1, maxLevel: 3, rolledUpgrade: true);
		int birthLevel = System.Math.Max(min, 1);
		Assert.Equal(2, birthLevel);
		Assert.Equal(1, Bloodline.StarsFromLevel(birthLevel));
	}

	[Fact]
	public void Sim_FavoriteMeal_UsesHigherChanceTable()
	{
		Assert.Equal(25f, YardTables.RollChance(5f, favoriteMeal: true, favoriteChance: 25f));
		Assert.Equal(5f, YardTables.RollChance(5f, favoriteMeal: false, favoriteChance: 25f));
	}

	[Fact]
	public void Sim_PenFull_WhenAtMaxCreatures()
	{
		Assert.True(YardBreeding.IsOverCap(5, maxCreatures: 5));
		Assert.False(YardBreeding.IsOverCap(4, maxCreatures: 5));
		Assert.Equal("Pen full — no breeding", YardBreeding.OverCapHoverLine(true));
		Assert.Equal(string.Empty, YardBreeding.OverCapHoverLine(false));

		DateTime now = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
		Assert.Equal(string.Empty, YardBreeding.ExpectingHoverLine(0L, 60f, now));
		Assert.Equal(
			"Expecting — due in 45s",
			YardBreeding.ExpectingHoverLine(now.AddSeconds(-15).Ticks, 60f, now));
		Assert.Equal(
			"Expecting — due in 1m 05s",
			YardBreeding.ExpectingHoverLine(now.AddSeconds(-55).Ticks, 120f, now));
		Assert.Equal(
			"Expecting — any moment",
			YardBreeding.ExpectingHoverLine(now.AddSeconds(-60).Ticks, 60f, now));
		Assert.Equal("Expecting", YardBreeding.ExpectingHoverLine(now.Ticks, 0f, now));
		Assert.Equal("48s", YardBreeding.FormatDuration(48.9));
		Assert.Equal("2m 05s", YardBreeding.FormatDuration(125));
	}

	[Fact]
	public void Sim_MateDraw_RequiresFedCalmPartnerFartherAway()
	{
		Assert.True(YardBreeding.MayMateDraw(
			enabled: true, isTamed: true, isYoung: false, isPregnant: false, isHungry: false,
			isAlerted: false, overCap: false, hasPartnerInDrawRange: true, partnerAlreadyClose: false));
		Assert.False(YardBreeding.MayMateDraw(
			enabled: true, isTamed: true, isYoung: false, isPregnant: false, isHungry: false,
			isAlerted: false, overCap: false, hasPartnerInDrawRange: true, partnerAlreadyClose: true));
		Assert.False(YardBreeding.MayMateDraw(
			enabled: true, isTamed: true, isYoung: false, isPregnant: false, isHungry: true,
			isAlerted: false, overCap: false, hasPartnerInDrawRange: true, partnerAlreadyClose: false));
	}

	[Fact]
	public void Sim_CapCounts_AdultAndPiggy()
	{
		Assert.True(YardBreeding.CountsTowardCap("Boar", "Boar_piggy", "Boar"));
		Assert.True(YardBreeding.CountsTowardCap("Boar", "Boar_piggy", "Boar_piggy"));
		Assert.False(YardBreeding.CountsTowardCap("Boar", "Boar_piggy", "Wolf"));
	}
}
