using HarmonyLib;
using UnityEngine;

namespace BlackHearthx.Hearthline.Patches
{
	/// <summary>
	/// Debug-only: compress bond ticks + pregnancy so yard smoke tests finish in seconds.
	/// Does not change production defaults (config off).
	/// </summary>
	[HarmonyPatch(typeof(Procreation), "Awake")]
	internal static class FastBreedAwakePatch
	{
		[HarmonyPostfix]
		private static void Postfix(Procreation __instance)
		{
			FastBreed.ApplyIfEnabled(__instance, reschedule: true);
		}
	}

	[HarmonyPatch(typeof(Procreation), "Procreate")]
	[HarmonyPriority(Priority.First)]
	internal static class FastBreedProcreatePatch
	{
		[HarmonyPrefix]
		private static void Prefix(Procreation __instance)
		{
			// Animals already alive when the toggle flips still get turbo fields.
			FastBreed.ApplyIfEnabled(__instance, reschedule: false);
		}
	}
}

namespace BlackHearthx.Hearthline
{
	internal static class FastBreed
	{
		internal const int TurboLovePoints = 1;
		internal const float TurboPregnancyChance = 0f;
		internal const float TurboPregnancyDuration = 5f;
		internal const float TurboUpdateInterval = 2f;
		internal const float TurboPartnerRange = 8f;

		private static readonly System.Collections.Generic.HashSet<int> Rescheduled =
			new System.Collections.Generic.HashSet<int>();

		internal static void ApplyIfEnabled(Procreation procreation, bool reschedule)
		{
			if (!Plugin.EnableMod.Value || !Plugin.DebugFastBreeding.Value || procreation == null)
			{
				return;
			}

			procreation.m_requiredLovePoints = TurboLovePoints;
			procreation.m_pregnancyChance = TurboPregnancyChance;
			procreation.m_pregnancyDuration = TurboPregnancyDuration;
			procreation.m_updateInterval = TurboUpdateInterval;
			if (procreation.m_partnerCheckRange < TurboPartnerRange)
			{
				procreation.m_partnerCheckRange = TurboPartnerRange;
			}

			int id = ((UnityEngine.Object)procreation).GetInstanceID();
			bool needSchedule = reschedule || !Rescheduled.Contains(id);
			if (!needSchedule)
			{
				return;
			}

			procreation.CancelInvoke("Procreate");
			procreation.InvokeRepeating("Procreate", 0.5f, TurboUpdateInterval);
			Rescheduled.Add(id);
		}
	}
}
