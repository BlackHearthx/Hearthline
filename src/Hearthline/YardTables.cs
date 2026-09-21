using System;
using System.Collections.Generic;

namespace BlackHearthx.Hearthline
{
	public static class YardTables
	{
		public static string StripClone(string name)
		{
			if (string.IsNullOrEmpty(name))
			{
				return string.Empty;
			}

			return name.Replace("(Clone)", string.Empty).Trim();
		}

		/// <summary>
		/// Readable label for cubs without Tameable. Prefers known prefab display names so
		/// spawn/debug labels like enemy_boarpiggy / $enemy_boarpiggy do not show raw.
		/// </summary>
		public static string PrettyCreatureName(string prefabOrObjectName, string mName)
		{
			string prefab = StripClone(prefabOrObjectName);
			if (PrefabDisplayNames.TryGetValue(prefab, out string pretty))
			{
				return pretty;
			}

			string key = mName ?? string.Empty;
			if (key.StartsWith("$", StringComparison.Ordinal))
			{
				key = key.Substring(1);
			}

			if (key.StartsWith("enemy_", StringComparison.OrdinalIgnoreCase) || string.IsNullOrEmpty(key))
			{
				return HumanizePrefab(prefab);
			}

			return key;
		}

		private static readonly Dictionary<string, string> PrefabDisplayNames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
		{
			{ "Boar_piggy", "Boar piggy" },
			{ "Wolf_cub", "Wolf cub" },
			{ "Lox_Calf", "Lox calf" },
			{ "Moose_calf", "Moose calf" },
			{ "Asksvin_hatchling", "Asksvin hatchling" },
			{ "Chicken", "Chicken" },
			{ "Boar", "Boar" },
			{ "Wolf", "Wolf" },
			{ "Lox", "Lox" },
			{ "Hen", "Hen" },
		};

		private static string HumanizePrefab(string prefab)
		{
			if (string.IsNullOrEmpty(prefab))
			{
				return "Young";
			}

			return prefab.Replace('_', ' ');
		}

		public static Dictionary<string, float> ParseChances(string raw)
		{
			Dictionary<string, float> map = new Dictionary<string, float>(StringComparer.Ordinal);
			if (string.IsNullOrWhiteSpace(raw))
			{
				return map;
			}

			foreach (string part in raw.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries))
			{
				string[] pair = part.Split(':');
				if (pair.Length != 2)
				{
					continue;
				}

				string prefab = pair[0].Trim();
				if (prefab.Length == 0 || !float.TryParse(pair[1].Trim(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float chance))
				{
					continue;
				}

				map[prefab] = Math.Max(0f, Math.Min(100f, chance));
			}

			return map;
		}

		public static Dictionary<string, HashSet<string>> ParseFavorites(string raw)
		{
			Dictionary<string, HashSet<string>> map = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
			if (string.IsNullOrWhiteSpace(raw))
			{
				return map;
			}

			foreach (string creatureBlock in raw.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
			{
				int colon = creatureBlock.IndexOf(':');
				if (colon <= 0)
				{
					continue;
				}

				string prefab = creatureBlock.Substring(0, colon).Trim();
				string items = creatureBlock.Substring(colon + 1);
				if (prefab.Length == 0)
				{
					continue;
				}

				HashSet<string> set = new HashSet<string>(StringComparer.Ordinal);
				foreach (string item in items.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
				{
					string id = item.Trim();
					if (id.Length > 0)
					{
						set.Add(id);
					}
				}

				if (set.Count > 0)
				{
					map[prefab] = set;
				}
			}

			return map;
		}

		public static float ChanceForPrefab(string prefab, Dictionary<string, float> overrides, float fallback)
		{
			if (overrides != null && overrides.TryGetValue(prefab, out float chance))
			{
				return chance;
			}

			return fallback;
		}

		public static bool IsListedFavorite(string creaturePrefab, string itemPrefab, Dictionary<string, HashSet<string>> favorites)
		{
			if (favorites == null || string.IsNullOrEmpty(creaturePrefab) || string.IsNullOrEmpty(itemPrefab))
			{
				return false;
			}

			return favorites.TryGetValue(creaturePrefab, out HashSet<string>? items) && items != null && items.Contains(itemPrefab);
		}

		public static float RollChance(float baseChance, bool favoriteMeal, float favoriteChance)
		{
			return favoriteMeal ? favoriteChance : baseChance;
		}

		public static int GrowthPercent(double secondsAlive, float growTime)
		{
			if (growTime <= 0f)
			{
				return 0;
			}

			int percent = (int)Math.Round(100.0 * secondsAlive / growTime);
			if (percent < 0)
			{
				return 0;
			}

			if (percent > 99)
			{
				return 99;
			}

			return percent;
		}
	}
}
