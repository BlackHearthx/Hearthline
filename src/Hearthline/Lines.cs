namespace BlackHearthx.Hearthline
{
	/// <summary>
	/// Picks the line for the language the player chose in Valheim.
	/// </summary>
	internal static class Lines
	{
		internal static string Language()
		{
			Localization loc = Localization.instance;
			if (loc == null)
			{
				return GameText.English;
			}

			string language = loc.GetSelectedLanguage();
			return string.IsNullOrEmpty(language) ? GameText.English : language;
		}

		internal static string T(string key)
		{
			return GameText.Line(key, Language());
		}

		internal static string F(string key, params object[] args)
		{
			return string.Format(T(key), args);
		}

		/// <summary>Vanilla word such as $hud_tame, with an English fallback.</summary>
		internal static string Vanilla(string token, string fallback)
		{
			Localization loc = Localization.instance;
			if (loc == null || string.IsNullOrEmpty(token))
			{
				return fallback;
			}

			string value = loc.Localize(token);
			if (string.IsNullOrEmpty(value) || value.IndexOf('[') >= 0)
			{
				return fallback;
			}

			return value;
		}
	}
}
