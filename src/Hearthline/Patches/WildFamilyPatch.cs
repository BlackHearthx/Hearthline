using HarmonyLib;

namespace BlackHearthx.Hearthline.Patches
{
	/// <summary>
	/// After Character.Awake the ZDO exists and tame/level are loaded. Schedule a one-shot
	/// wild-family roll for untamed adults with Procreation.m_offspring.
	/// </summary>
	[HarmonyPatch(typeof(Character), "Awake")]
	internal static class CharacterWildFamilyPatch
	{
		[HarmonyPostfix]
		private static void Postfix(Character __instance)
		{
			WildFamilySpawn.TrySchedule(__instance);
		}
	}
}
