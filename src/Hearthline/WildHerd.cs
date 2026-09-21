namespace BlackHearthx.Hearthline
{
	/// <summary>
	/// Vanilla Procreation.Procreate returns immediately when !Character.IsTamed().
	/// Partner scans use Procreation.ReadyForProcreation, which also requires IsTamed().
	/// Offspring prefab is Procreation.m_offspring (Boar_piggy, Wolf_cub, Lox_Calf, eggs, etc.).
	/// Birth copies tame state: SetTamed(parent.IsTamed()) — wild parent => wild cub.
	/// </summary>
	public static class WildHerd
	{
		public static bool MayStartProcreate(bool parentTamed, bool wildEnabled, bool hasVanillaOffspring)
		{
			if (parentTamed)
			{
				return true;
			}

			return wildEnabled && hasVanillaOffspring;
		}

		public static bool MayCountAsPartner(
			bool vanillaReady,
			bool isTamed,
			bool wildEnabled,
			bool hasVanillaOffspring,
			bool isPregnant,
			bool isHungry,
			bool ignoreHunger)
		{
			if (vanillaReady)
			{
				return true;
			}

			if (!wildEnabled || !hasVanillaOffspring || isTamed)
			{
				return vanillaReady;
			}

			if (isPregnant)
			{
				return false;
			}

			if (isHungry && !ignoreHunger)
			{
				return false;
			}

			return true;
		}

		public static bool ShouldIgnoreHunger(bool ignoreHunger, bool isTamed, bool inProcreate, bool hasVanillaOffspring)
		{
			if (!ignoreHunger || isTamed || !inProcreate || !hasVanillaOffspring)
			{
				return false;
			}

			return true;
		}

		/// <summary>
		/// Extra throttle for wild MakePregnant only. <paramref name="rollPercent"/> is 0–100.
		/// </summary>
		public static bool MayWildPregnancy(float rollPercent, float chancePercent)
		{
			if (chancePercent <= 0f)
			{
				return false;
			}

			if (chancePercent >= 100f)
			{
				return true;
			}

			return rollPercent <= chancePercent;
		}

		/// <summary>
		/// Chance that a newly spawned wild adult already has one cub nearby (not via Procreate).
		/// Same 0–100 roll semantics as MayWildPregnancy.
		/// </summary>
		public static bool MaySpawnWildFamily(float rollPercent, float chancePercent)
		{
			return MayWildPregnancy(rollPercent, chancePercent);
		}

		/// <summary>
		/// Family spawn only for live Growup cubs — not eggs (ItemDrop offspring).
		/// </summary>
		public static bool IsLiveOffspringPrefab(bool hasGrowup, bool hasItemDrop)
		{
			return hasGrowup && !hasItemDrop;
		}

		/// <summary>
		/// Growup cubs, or tameable stock that cannot breed yet (no Procreation) — piglets/cubs.
		/// Adults that breed have Procreation on the same prefab.
		/// </summary>
		public static bool IsYoungAnimal(bool hasGrowup, bool hasTameable, bool hasProcreation)
		{
			if (hasGrowup)
			{
				return true;
			}

			return hasTameable && !hasProcreation;
		}

		private static readonly string[] YoungNameFragments =
		{
			"piggy", "cub", "calf", "hatchling", "wolfcub", "moose_calf", "kit", "chick"
		};

		public static bool IsYoungPrefabName(string rawName)
		{
			string name = YardTables.StripClone(rawName ?? string.Empty);
			foreach (string fragment in YoungNameFragments)
			{
				if (name.IndexOf(fragment, System.StringComparison.OrdinalIgnoreCase) >= 0)
				{
					return true;
				}
			}

			return false;
		}

		/// <summary>
		/// Boar + Boar_piggy, Wolf + Wolf_cub, Lox + Lox_Calf, etc.
		/// </summary>
		public static bool IsParentSpeciesOf(string adultPrefab, string youngPrefab)
		{
			if (string.IsNullOrEmpty(adultPrefab) || string.IsNullOrEmpty(youngPrefab))
			{
				return false;
			}

			if (string.Equals(adultPrefab, youngPrefab, System.StringComparison.OrdinalIgnoreCase))
			{
				return false;
			}

			return youngPrefab.StartsWith(adultPrefab, System.StringComparison.OrdinalIgnoreCase)
			       || youngPrefab.IndexOf(adultPrefab, System.StringComparison.OrdinalIgnoreCase) >= 0;
		}

		public static bool MayStealYoung(bool isTamed, bool isYoung, bool hasTameable, bool stealEnabled)
		{
			// hasTameable is informational; vanilla cubs often lack Tameable (AnimalAI + Growup only).
			_ = hasTameable;
			return stealEnabled && !isTamed && isYoung;
		}

		public static bool MayCarryYoung(bool isTamed, bool isYoung, bool carryEnabled)
		{
			return carryEnabled && isTamed && isYoung;
		}

		/// <summary>Tamed livestock adults (have Tameable, not Growup young).</summary>
		public static bool MayCarryTamedAdult(bool isTamed, bool isYoung, bool hasTameable, bool carryEnabled)
		{
			return carryEnabled && isTamed && !isYoung && hasTameable;
		}

		public static bool MayCarryTarget(bool isTamed, bool isYoung, bool hasTameable, bool carryEnabled)
		{
			return MayCarryYoung(isTamed, isYoung, carryEnabled)
			       || MayCarryTamedAdult(isTamed, isYoung, hasTameable, carryEnabled);
		}
	}
}
