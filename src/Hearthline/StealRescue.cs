using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace BlackHearthx.Hearthline
{
	/// <summary>
	/// When a wild cub is stolen, nearby wild adults of the same breeding line aggro the thief.
	/// Uses Alert + MonsterAI target (OnDamaged-style) and AggravatedReason.Theif —
	/// no SetHuntPlayer, so chase duration stays closer to vanilla aggro drop-off.
	/// </summary>
	internal static class StealRescue
	{
		private static readonly List<Character> RangeBuffer = new List<Character>();

		/// <returns>How many same-species wild adults were agitated.</returns>
		internal static int AlertGuardians(Character stolenYoung, Player thief)
		{
			if (!Plugin.EnableMod.Value || !Plugin.StealAlertsHerd.Value || stolenYoung == null || thief == null)
			{
				return 0;
			}

			float radius = Plugin.StealAlertRange.Value;
			if (radius <= 0f)
			{
				return 0;
			}

			Vector3 point = stolenYoung.transform.position;
			string youngPrefab = YardTables.StripClone(stolenYoung.gameObject.name);

			BaseAI.AggravateAllInArea(point, radius, BaseAI.AggravatedReason.Theif);

			RangeBuffer.Clear();
			Character.GetCharactersInRange(point, radius, RangeBuffer);

			int alerted = 0;
			foreach (Character nearby in RangeBuffer)
			{
				if (nearby == null || nearby == stolenYoung || nearby.IsPlayer() || nearby.IsTamed())
				{
					continue;
				}

				if (CubCarry.IsYoung(nearby))
				{
					continue;
				}

				if (!IsGuardianOf(nearby, youngPrefab))
				{
					continue;
				}

				if (AgitateAdult(nearby, (Character)thief))
				{
					alerted++;
				}
			}

			if (Plugin.DebugLogging.Value)
			{
				Plugin.Log.LogInfo($"[Hearthline] steal rescue young={youngPrefab} alerted={alerted} radius={radius:0.#}");
			}

			return alerted;
		}

		/// <summary>
		/// Adult is a guardian if its Procreation.m_offspring matches the cub, or prefab name is the cub's parent stem.
		/// </summary>
		internal static bool IsGuardianOf(Character adult, string youngPrefab)
		{
			if (adult == null || string.IsNullOrEmpty(youngPrefab))
			{
				return false;
			}

			Procreation procreation = ((Component)adult).GetComponent<Procreation>();
			if (procreation != null && procreation.m_offspring != null)
			{
				string offspring = Utils.GetPrefabName(procreation.m_offspring);
				if (string.Equals(offspring, youngPrefab, System.StringComparison.OrdinalIgnoreCase))
				{
					return true;
				}
			}

			string adultPrefab = YardTables.StripClone(adult.gameObject.name);
			return WildHerd.IsParentSpeciesOf(adultPrefab, youngPrefab);
		}

		private static bool AgitateAdult(Character adult, Character thief)
		{
			BaseAI ai = ((Component)adult).GetComponent<BaseAI>();
			if (ai == null)
			{
				return false;
			}

			ai.Alert();

			if (ai.IsAggravatable())
			{
				ai.SetAggravated(true, BaseAI.AggravatedReason.Theif);
			}

			MonsterAI monsterAi = ai as MonsterAI;
			if (monsterAi != null)
			{
				// Mirror MonsterAI.OnDamaged: force target even if one was already set.
				AccessTools.Field(typeof(MonsterAI), "m_targetCreature")?.SetValue(monsterAi, thief);
				AccessTools.Field(typeof(MonsterAI), "m_lastKnownTargetPos")?.SetValue(monsterAi, thief.transform.position);
				AccessTools.Field(typeof(MonsterAI), "m_beenAtLastPos")?.SetValue(monsterAi, false);
				AccessTools.Field(typeof(MonsterAI), "m_targetStatic")?.SetValue(monsterAi, null);
				AccessTools.Field(typeof(MonsterAI), "m_timeSinceSensedTargetCreature")?.SetValue(monsterAi, 0f);
			}

			return true;
		}
	}
}
