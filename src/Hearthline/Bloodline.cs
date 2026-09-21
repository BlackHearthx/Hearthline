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

		public static int MaxAllowedLevel(int maxStars, bool extendedLevelsAvailable)
		{
			int configuredLevel = Math.Max(0, maxStars) + 1;
			int gameMax = extendedLevelsAvailable ? configuredLevel : VanillaMaxLevel;
			return Math.Min(configuredLevel, gameMax);
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
