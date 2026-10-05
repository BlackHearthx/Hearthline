using System;

namespace BlackHearthx.Hearthline
{
	/// <summary>
	/// Valheim levels are 1-based: 0 stars = level 1, 2 stars = level 3.
	/// Confirmed by vanilla Procreation birth:
	/// Character.SetLevel(Mathf.Max(m_minOffspringLevel, parent.GetLevel()))
	/// and eggs: ItemDrop.SetQuality with the same value.
	/// </summary>
	public static class Bloodline
	{
		public const int VanillaMaxLevel = 3;

		// Verified against serialized vanilla spawn entries, not unused LevelEffects setups.
		public static bool SupportsVanillaStars(string prefab)
		{
			prefab = (prefab ?? string.Empty).Replace("(Clone)", string.Empty).Trim();
			switch (prefab)
			{
				case "Boar": case "Wolf": case "Asksvin": case "Hare":
				case "Moose": case "Deer": case "Neck": return true;
				default: return false;
			}
		}

		public static int BreedingLevel(string adultPrefab, int level)
		{
			return SupportsVanillaStars(adultPrefab) ? Math.Max(1, Math.Min(level, VanillaMaxLevel)) : 1;
		}

		public static int MaxAllowedLevel(int maxStars, bool extendedLevelsAvailable)
		{
			int configuredLevel = Math.Max(0, maxStars) + 1;
			return Math.Min(configuredLevel, VanillaMaxLevel);
		}

		/// <summary>
		/// Temporary floor for Procreation.m_minOffspringLevel during one Procreate() call.
		/// Vanilla then does Max(min, parentLevel), so raising min to parentLevel+1 yields +1 star.
		/// </summary>
		public static int RaisedMinOffspringLevel(int currentMin, int parentLevel, int maxLevel, bool rolledUpgrade)
		{
			if (!rolledUpgrade)
			{
				return currentMin;
			}

			int target = Math.Min(parentLevel + 1, maxLevel);
			return Math.Max(currentMin, target);
		}

		public static int StarsFromLevel(int level)
		{
			return Math.Max(0, level - 1);
		}
	}
}
