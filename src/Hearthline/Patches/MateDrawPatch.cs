using HarmonyLib;

namespace BlackHearthx.Hearthline.Patches
{
	/// <summary>
	/// BaseAI.IdleMovement is the idle wander for both MonsterAI and AnimalAI.
	/// Skipping it only while mate draw is steering leaves every other AI branch vanilla.
	/// </summary>
	[HarmonyPatch(typeof(BaseAI), "IdleMovement")]
	internal static class MateDrawIdlePatch
	{
		[HarmonyPrefix]
		private static bool Prefix(BaseAI __instance, float dt)
		{
			return !MateDraw.Steer(__instance, dt);
		}
	}
}
