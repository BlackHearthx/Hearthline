using BlackHearthx.Hearthline;
using Xunit;

namespace Hearthline.Tests;

public class BloodlineTests
{
	[Fact]
	public void VanillaCapIsLevel3WithoutCllc()
	{
		Assert.Equal(3, Bloodline.MaxAllowedLevel(2, extendedLevelsAvailable: false));
		Assert.Equal(3, Bloodline.MaxAllowedLevel(5, extendedLevelsAvailable: false));
	}

	[Fact]
	public void CllcCannotExtendVanillaCap()
	{
		Assert.Equal(3, Bloodline.MaxAllowedLevel(2, extendedLevelsAvailable: true));
		Assert.Equal(3, Bloodline.MaxAllowedLevel(5, extendedLevelsAvailable: true));
	}

	[Theory]
	[InlineData("Hen")]
	[InlineData("Chicken")]
	[InlineData("Lox")]
	[InlineData("CustomAnimal")]
	public void UnsupportedSpeciesCannotInheritOrUpgrade(string prefab)
	{
		Assert.False(Bloodline.SupportsVanillaStars(prefab));
		Assert.Equal(1, Bloodline.BreedingLevel(prefab, 3));
		Assert.Equal(1, Bloodline.BreedingLevel(prefab, Bloodline.RaisedMinOffspringLevel(5, 2, 3, true)));
	}

	[Theory]
	[InlineData("Boar")]
	[InlineData("Wolf")]
	[InlineData("Hare")]
	[InlineData("Moose")]
	[InlineData("Asksvin")]
	[InlineData("Deer")]
	[InlineData("Neck")]
	public void VanillaSpeciesKeepStarsWithinVanillaCap(string prefab)
	{
		Assert.True(Bloodline.SupportsVanillaStars(prefab + "(Clone)"));
		Assert.Equal(2, Bloodline.BreedingLevel(prefab, 2));
		Assert.Equal(3, Bloodline.BreedingLevel(prefab, 10));
	}

	[Fact]
	public void FailedRollLeavesMinUnchanged()
	{
		Assert.Equal(1, Bloodline.RaisedMinOffspringLevel(1, parentLevel: 2, maxLevel: 3, rolledUpgrade: false));
	}

	[Fact]
	public void SuccessfulRollRaisesMinToParentPlusOne()
	{
		// 0-star parent (level 1) -> min 2 so vanilla Max(2, 1) = 1-star cub
		Assert.Equal(2, Bloodline.RaisedMinOffspringLevel(1, parentLevel: 1, maxLevel: 3, rolledUpgrade: true));
		// 1-star parent (level 2) -> min 3
		Assert.Equal(3, Bloodline.RaisedMinOffspringLevel(1, parentLevel: 2, maxLevel: 3, rolledUpgrade: true));
	}

	[Fact]
	public void SuccessfulRollDoesNotExceedCap()
	{
		Assert.Equal(3, Bloodline.RaisedMinOffspringLevel(1, parentLevel: 3, maxLevel: 3, rolledUpgrade: true));
	}

	[Fact]
	public void StarsFromLevelMatchesVanilla()
	{
		Assert.Equal(0, Bloodline.StarsFromLevel(1));
		Assert.Equal(1, Bloodline.StarsFromLevel(2));
		Assert.Equal(2, Bloodline.StarsFromLevel(3));
	}
}
