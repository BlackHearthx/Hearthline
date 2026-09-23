using BlackHearthx.Hearthline;
using Xunit;

namespace Hearthline.Tests;

public class AwayBreedTests
{
	[Fact]
	public void WildAdultsAreExcluded()
	{
		Assert.False(AwayBreed.AppliesTo(tamed: false));
		Assert.True(AwayBreed.AppliesTo(tamed: true));
	}

	[Fact]
	public void ANormalTickIsNotAnAbsence()
	{
		Assert.False(AwayBreed.GapIsAbsence(10d, 10f));
		Assert.False(AwayBreed.GapIsAbsence(25d, 10f));
		Assert.True(AwayBreed.GapIsAbsence(26d, 10f));
	}

	[Fact]
	public void HungryWithNoFoodDoesNotGainLove()
	{
		AwayBreed.Result result = AwayBreed.Simulate(
			gapSeconds: 100,
			updateInterval: 10f,
			pregnancyChance: 0f,
			pregnancyDuration: 10f,
			requiredLovePoints: 4,
			maxCreatures: 4,
			creaturesNow: 2,
			fedDuration: 30f,
			secondsSinceFeedAtStart: 999d,
			loveNow: 2,
			pregnancyElapsedNow: -1d,
			hasPartner: true,
			foodOnGround: 0,
			roll01: () => 1f);

		Assert.Equal(2, result.LovePoints);
		Assert.Equal(0, result.Births);
		Assert.Equal(0, result.FoodsEaten);
		Assert.True(result.PregnancyElapsed < 0d);
	}

	[Fact]
	public void PregnancyFinishesWhileHungryAndWithoutAPartner()
	{
		AwayBreed.Result result = AwayBreed.Simulate(
			gapSeconds: 40,
			updateInterval: 10f,
			pregnancyChance: 0.5f,
			pregnancyDuration: 30f,
			requiredLovePoints: 4,
			maxCreatures: 4,
			creaturesNow: 2,
			fedDuration: 30f,
			secondsSinceFeedAtStart: 999d,
			loveNow: 0,
			pregnancyElapsedNow: 10d,
			hasPartner: false,
			foodOnGround: 0,
			roll01: () => 1f);

		Assert.Equal(1, result.Births);
		Assert.True(result.PregnancyElapsed < 0d);
		Assert.Equal(0, result.FoodsEaten);
	}

	[Fact]
	public void FullPenDoesNotStartANewPregnancy()
	{
		AwayBreed.Result result = AwayBreed.Simulate(
			gapSeconds: 500,
			updateInterval: 10f,
			pregnancyChance: 0f,
			pregnancyDuration: 10f,
			requiredLovePoints: 1,
			maxCreatures: 4,
			creaturesNow: 4,
			fedDuration: 1000f,
			secondsSinceFeedAtStart: 0d,
			loveNow: 0,
			pregnancyElapsedNow: -1d,
			hasPartner: true,
			foodOnGround: 20,
			roll01: () => 1f);

		Assert.Equal(0, result.Births);
		Assert.Equal(0, result.LovePoints);
		Assert.Equal(0, result.FoodsEaten);
	}

	[Fact]
	public void OverduePregnancyStillBirthsWhenThePenIsFull()
	{
		AwayBreed.Result result = AwayBreed.Simulate(
			gapSeconds: 50,
			updateInterval: 10f,
			pregnancyChance: 0f,
			pregnancyDuration: 10f,
			requiredLovePoints: 1,
			maxCreatures: 4,
			creaturesNow: 4,
			fedDuration: 30f,
			secondsSinceFeedAtStart: 0d,
			loveNow: 3,
			pregnancyElapsedNow: 100d,
			hasPartner: false,
			foodOnGround: 0,
			roll01: () => 1f);

		Assert.Equal(1, result.Births);
		Assert.Equal(0, result.LovePoints);
		Assert.True(result.PregnancyElapsed < 0d);
	}

	[Fact]
	public void FedPairBirthsUntilThePenIsFull()
	{
		AwayBreed.Result result = AwayBreed.Simulate(
			gapSeconds: 100,
			updateInterval: 10f,
			pregnancyChance: 0.5f,
			pregnancyDuration: 10f,
			requiredLovePoints: 2,
			maxCreatures: 4,
			creaturesNow: 2,
			fedDuration: 1000f,
			secondsSinceFeedAtStart: 0d,
			loveNow: 0,
			pregnancyElapsedNow: -1d,
			hasPartner: true,
			foodOnGround: 0,
			roll01: () => 0.9f);

		Assert.Equal(2, result.Births);
		Assert.Equal(0, result.LovePoints);
		Assert.True(result.PregnancyElapsed < 0d);
		Assert.Equal(0, result.FoodsEaten);
	}

	[Fact]
	public void FailedRollDoesNotAddLove()
	{
		AwayBreed.Result result = AwayBreed.Simulate(
			gapSeconds: 100,
			updateInterval: 10f,
			pregnancyChance: 0.5f,
			pregnancyDuration: 10f,
			requiredLovePoints: 4,
			maxCreatures: 10,
			creaturesNow: 2,
			fedDuration: 1000f,
			secondsSinceFeedAtStart: 0d,
			loveNow: 0,
			pregnancyElapsedNow: -1d,
			hasPartner: true,
			foodOnGround: 0,
			roll01: () => 0.1f);

		Assert.Equal(0, result.LovePoints);
		Assert.Equal(0, result.Births);
	}

	[Fact]
	public void HardCapStopsALongAbsence()
	{
		AwayBreed.Result result = AwayBreed.Simulate(
			gapSeconds: 1000,
			updateInterval: 10f,
			pregnancyChance: 0f,
			pregnancyDuration: 10f,
			requiredLovePoints: 1,
			maxCreatures: 100,
			creaturesNow: 1,
			fedDuration: 99999f,
			secondsSinceFeedAtStart: 0d,
			loveNow: 0,
			pregnancyElapsedNow: -1d,
			hasPartner: true,
			foodOnGround: 0,
			roll01: () => 1f,
			birthCap: 2);

		Assert.Equal(2, result.Births);
	}

	[Fact]
	public void OneMealFeedsTheNextLoveTicks()
	{
		AwayBreed.Result result = AwayBreed.Simulate(
			gapSeconds: 50,
			updateInterval: 10f,
			pregnancyChance: 0f,
			pregnancyDuration: 9999f,
			requiredLovePoints: 99,
			maxCreatures: 10,
			creaturesNow: 1,
			fedDuration: 30f,
			secondsSinceFeedAtStart: 30d,
			loveNow: 0,
			pregnancyElapsedNow: -1d,
			hasPartner: true,
			foodOnGround: 1,
			roll01: () => 1f);

		Assert.Equal(1, result.FoodsEaten);
		Assert.Equal(3, result.LovePoints);
		Assert.True(result.PregnancyElapsed < 0d);
	}

	[Fact]
	public void PregnancyBecomesDueOnlyAfterTheDuration()
	{
		AwayBreed.Result notYet = AwayBreed.Simulate(
			gapSeconds: 10,
			updateInterval: 10f,
			pregnancyChance: 0.5f,
			pregnancyDuration: 10f,
			requiredLovePoints: 4,
			maxCreatures: 4,
			creaturesNow: 2,
			fedDuration: 1000f,
			secondsSinceFeedAtStart: 0d,
			loveNow: 0,
			pregnancyElapsedNow: 0d,
			hasPartner: true,
			foodOnGround: 0,
			roll01: () => 1f);

		Assert.Equal(0, notYet.Births);
		Assert.Equal(10d, notYet.PregnancyElapsed, 3);

		AwayBreed.Result due = AwayBreed.Simulate(
			gapSeconds: 20,
			updateInterval: 10f,
			pregnancyChance: 0.5f,
			pregnancyDuration: 10f,
			requiredLovePoints: 4,
			maxCreatures: 4,
			creaturesNow: 2,
			fedDuration: 1000f,
			secondsSinceFeedAtStart: 0d,
			loveNow: 0,
			pregnancyElapsedNow: 0d,
			hasPartner: true,
			foodOnGround: 0,
			roll01: () => 1f);

		Assert.Equal(1, due.Births);
	}
}
