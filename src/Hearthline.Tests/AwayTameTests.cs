using BlackHearthx.Hearthline;
using Xunit;

namespace Hearthline.Tests;

public class AwayTameTests
{
	[Fact]
	public void HungryWithNoFoodDoesNotProgress()
	{
		AwayTame.Result result = AwayTame.Simulate(
			gapSeconds: 600,
			fedDuration: 30f,
			secondsSinceFeedAtStart: 99999,
			timeLeft: 1800f,
			foodOnGround: 0);

		Assert.Equal(1800f, result.TimeLeft);
		Assert.Equal(0, result.FoodsEaten);
		Assert.False(result.Finished);
	}

	[Fact]
	public void EachMealBuysOneFedWindow()
	{
		AwayTame.Result result = AwayTame.Simulate(
			gapSeconds: 600,
			fedDuration: 30f,
			secondsSinceFeedAtStart: 99999,
			timeLeft: 1800f,
			foodOnGround: 20);

		Assert.Equal(1200f, result.TimeLeft);
		Assert.Equal(20, result.FoodsEaten);
		Assert.False(result.Finished);
	}

	[Fact]
	public void LeftoverFedTimeCountsBeforeTheNextMeal()
	{
		AwayTame.Result result = AwayTame.Simulate(
			gapSeconds: 100,
			fedDuration: 30f,
			secondsSinceFeedAtStart: 20,
			timeLeft: 1800f,
			foodOnGround: 1);

		Assert.Equal(1760f, result.TimeLeft);
		Assert.Equal(1, result.FoodsEaten);
	}

	[Fact]
	public void FinishesWhenFedTimeCoversTheBar()
	{
		AwayTame.Result result = AwayTame.Simulate(
			gapSeconds: 100,
			fedDuration: 30f,
			secondsSinceFeedAtStart: 0,
			timeLeft: 25f,
			foodOnGround: 5);

		Assert.Equal(0f, result.TimeLeft);
		Assert.True(result.Finished);
		Assert.Equal(0, result.FoodsEaten);
	}
}
