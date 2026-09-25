using HarmonyLib;
using UnityEngine;

namespace BlackHearthx.Hearthline.Patches
{
	/// <summary>
	/// Vanilla Tameable.OnConsumedItem(ItemDrop) runs when MonsterAI eats a world item
	/// (old Tameable dump + Beyondthepen Tameable). HZ FoodContainer can invoke m_onConsumedItem(null).
	/// Favorite IDs are vanilla consume-list prefabs (wiki / Jotunn piece list), not new foods.
	/// </summary>
	[HarmonyPatch(typeof(Tameable), "OnConsumedItem")]
	internal static class TameableConsumedPatch
	{
		[HarmonyPostfix]
		private static void Postfix(Tameable __instance, ItemDrop item)
		{
			if (!Plugin.EnableMod.Value || item == null)
			{
				return;
			}

			ZNetView nview = ((Component)__instance).GetComponent<ZNetView>();
			if (nview == null || !nview.IsValid() || !nview.IsOwner())
			{
				return;
			}

			string creature = YardTables.StripClone(((Component)__instance).gameObject.name);
			string food = YardTables.StripClone(((Component)item).gameObject.name);
			bool favorite = Plugin.IsFavoriteFood(creature, food, item);
			nview.GetZDO().Set(HearthlineZdo.FavoriteMeal, favorite ? 1 : 0);

			if (Plugin.DebugLogging.Value)
			{
				Plugin.Log.LogInfo($"[Hearthline] {creature} ate {food} favorite={favorite}");
			}
		}
	}

	[HarmonyPatch(typeof(Character), nameof(Character.GetHoverText))]
	[HarmonyPriority(Priority.Low)]
	internal static class CharacterHoverPatch
	{
		[HarmonyPostfix]
		private static void Postfix(Character __instance, ref string __result)
		{
			if (!Plugin.EnableMod.Value || !Plugin.ShowYardHover.Value || __instance == null)
			{
				return;
			}

			Growup growup = ((Component)__instance).GetComponent<Growup>();
			BaseAI ai = ((Component)__instance).GetComponent<BaseAI>();
			if (growup != null && ai != null)
			{
				// Cubs without Tameable have empty vanilla hover; still show growth.
				if (string.IsNullOrEmpty(__result))
				{
					__result = YardTables.PrettyCreatureName(
						__instance.gameObject.name,
						__instance.m_name,
						Lines.Language());
				}

				int grown = YardTables.GrowthPercent(ai.GetTimeSinceSpawned().TotalSeconds, growup.m_growTime);
				string growing = Lines.F("growing", grown);
				if (__result.IndexOf(growing, System.StringComparison.Ordinal) < 0)
				{
					__result += "\n" + growing;
				}

				return;
			}

			if (string.IsNullOrEmpty(__result))
			{
				return;
			}

			Procreation procreation = ((Component)__instance).GetComponent<Procreation>();
			if (procreation == null)
			{
				return;
			}

			ZNetView nview = ((Component)__instance).GetComponent<ZNetView>();
			if (nview == null || !nview.IsValid())
			{
				return;
			}

			string language = Lines.Language();
			int love = nview.GetZDO().GetInt(ZDOVars.s_lovePoints, 0);
			int need = procreation.m_requiredLovePoints;
			__result += "\n" + Lines.F("bond", love, need);

			long pregnant = nview.GetZDO().GetLong(ZDOVars.s_pregnant, 0L);
			if (pregnant != 0L)
			{
				string expecting = ZNet.instance != null
					? YardBreeding.ExpectingHoverLine(
						pregnant,
						procreation.m_pregnancyDuration,
						ZNet.instance.GetTime(),
						language)
					: Lines.T("expecting");
				if (!string.IsNullOrEmpty(expecting))
				{
					__result += "\n" + expecting;
				}
			}

			if (MateDraw.IsOverCap(__instance, procreation))
			{
				string capLine = YardBreeding.OverCapHoverLine(true, language);
				if (!string.IsNullOrEmpty(capLine)
				    && __result.IndexOf(capLine, System.StringComparison.Ordinal) < 0)
				{
					__result += "\n" + capLine;
				}
			}

			if (nview.GetZDO().GetInt(HearthlineZdo.FavoriteMeal, 0) == 1)
			{
				Tameable tameable = ((Component)__instance).GetComponent<Tameable>();
				if (tameable != null && !tameable.IsHungry())
				{
					__result += "\n" + Lines.T("favorite");
				}
			}
		}
	}
}
