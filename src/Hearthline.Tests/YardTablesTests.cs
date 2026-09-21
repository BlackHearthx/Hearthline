using BlackHearthx.Hearthline;
using Xunit;

namespace Hearthline.Tests;

public class YardTablesTests
{
	[Fact]
	public void ParseChancesReadsPrefabPercents()
	{
		var map = YardTables.ParseChances("Boar:8,Wolf:5");
		Assert.Equal(8f, YardTables.ChanceForPrefab("Boar", map, 5f));
		Assert.Equal(5f, YardTables.ChanceForPrefab("Wolf", map, 99f));
		Assert.Equal(5f, YardTables.ChanceForPrefab("Lox", map, 5f));
	}

	[Fact]
	public void ParseFavoritesSplitsCreaturesAndItems()
	{
		var map = YardTables.ParseFavorites("Boar:Carrot,Raspberry;Wolf:RawMeat");
		Assert.True(YardTables.IsListedFavorite("Boar", "Carrot", map));
		Assert.True(YardTables.IsListedFavorite("Boar", "Raspberry", map));
		Assert.False(YardTables.IsListedFavorite("Boar", "Barley", map));
		Assert.True(YardTables.IsListedFavorite("Wolf", "RawMeat", map));
	}

	[Fact]
	public void FavoriteMealUsesHigherChance()
	{
		Assert.Equal(5f, YardTables.RollChance(5f, favoriteMeal: false, favoriteChance: 25f));
		Assert.Equal(25f, YardTables.RollChance(5f, favoriteMeal: true, favoriteChance: 25f));
	}

	[Fact]
	public void GrowthPercentClampsBeforeGrown()
	{
		Assert.Equal(50, YardTables.GrowthPercent(1500, 3000f));
		Assert.Equal(99, YardTables.GrowthPercent(4000, 3000f));
		Assert.Equal(0, YardTables.GrowthPercent(0, 3000f));
	}

	[Fact]
	public void StripCloneMatchesProcreationPlusNameTrim()
	{
		Assert.Equal("Boar", YardTables.StripClone("Boar(Clone)"));
	}

	[Theory]
	[InlineData("Boar_piggy(Clone)", "$enemy_boarpiggy", "Boar piggy")]
	[InlineData("Wolf_cub", "enemy_wolfcub", "Wolf cub")]
	[InlineData("Lox_Calf", "", "Lox calf")]
	public void PrettyCreatureNameHidesEnemyKeys(string objectName, string mName, string expected)
	{
		Assert.Equal(expected, YardTables.PrettyCreatureName(objectName, mName));
	}
}
