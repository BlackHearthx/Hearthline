using BlackHearthx.Hearthline;
using Xunit;

namespace Hearthline.Tests;

public class GameTextTests
{
	[Fact]
	public void EnglishLinesStayExact()
	{
		Assert.Equal("You took the young", GameText.Line("took_young"));
		Assert.Equal("Pen full — no breeding", GameText.Line("pen_full"));
		Assert.Equal("Expecting — due in {0}", GameText.Line("expecting_due"));
		Assert.Equal("Boar piggy", GameText.Line("boar_piggy"));
	}

	[Fact]
	public void EveryLanguageKeepsPlaceholders()
	{
		foreach (string language in GameText.Languages)
		{
			string growing = GameText.Line("growing", language);
			string bond = GameText.Line("bond", language);
			string due = GameText.Line("expecting_due", language);
			Assert.Contains("{0}", growing);
			Assert.Contains("{0}", bond);
			Assert.Contains("{1}", bond);
			Assert.Contains("{0}", due);
			Assert.False(string.IsNullOrWhiteSpace(GameText.Line("carry", language)));
			Assert.False(string.IsNullOrWhiteSpace(GameText.Line("put_down", language)));
		}
	}

	[Fact]
	public void PortugueseBrazilianReadsLikeSpeech()
	{
		Assert.Equal("Soltar", GameText.Line("put_down", "Portuguese_Brazilian"));
		Assert.Equal("Carregar", GameText.Line("carry", "Portuguese_Brazilian"));
		Assert.Equal("Peguei o filhote", GameText.Line("took_young", "Portuguese_Brazilian"));
		Assert.Equal("Curral cheio", GameText.Line("pen_full", "Portuguese_Brazilian"));
	}
}
