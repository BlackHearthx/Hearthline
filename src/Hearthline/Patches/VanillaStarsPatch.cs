using HarmonyLib;
using UnityEngine;

namespace BlackHearthx.Hearthline.Patches
{
	internal static class VanillaStars
	{
		internal static string AdultPrefab(Character character)
		{
			Growup grow = character.GetComponent<Growup>();
			return YardTables.StripClone(grow != null && grow.m_grownPrefab != null
				? grow.m_grownPrefab.name : character.gameObject.name);
		}

		internal static int ParentLevel(Character character)
		{
			return Plugin.EnableMod.Value
				? Bloodline.BreedingLevel(AdultPrefab(character), character.GetLevel()) : character.GetLevel();
		}

		internal static bool ManagedAnimal(Character character)
		{
			string name = AdultPrefab(character);
			return name == "Hen" || name == "Chicken" || name == "Lox"
				|| character.GetComponent<Procreation>() != null || character.GetComponent<Growup>() != null;
		}
	}

	[HarmonyPatch(typeof(Character), nameof(Character.SetLevel))]
	internal static class VanillaAnimalLevelPatch
	{
		[HarmonyPrefix]
		private static void Prefix(Character __instance, ref int level)
		{
			if (Plugin.EnableMod.Value && VanillaStars.ManagedAnimal(__instance))
				level = Bloodline.BreedingLevel(VanillaStars.AdultPrefab(__instance), level);
		}
	}

	// Awake loads levels directly from ZDO. Normalize before LevelEffects.Start applies visuals.
	[HarmonyPatch(typeof(Character), "Awake")]
	internal static class VanillaAnimalLoadPatch
	{
		[HarmonyPostfix]
		private static void Postfix(Character __instance, ref int ___m_level)
		{
			if (!Plugin.EnableMod.Value || !VanillaStars.ManagedAnimal(__instance)) return;
			ZNetView view = __instance.GetComponent<ZNetView>();
			if (view == null || !view.IsValid()) return;
			int level = VanillaStars.ParentLevel(__instance);
			if (__instance.GetLevel() == level) return;
			if (view.IsOwner()) __instance.SetLevel(level);
			else ___m_level = level;
		}
	}

	[HarmonyPatch(typeof(ItemDrop), nameof(ItemDrop.SetQuality))]
	internal static class VanillaEggQualityPatch
	{
		[HarmonyPrefix]
		private static void Prefix(ItemDrop __instance, ref int quality)
		{
			if (Plugin.EnableMod.Value && YardTables.StripClone(__instance.gameObject.name) == "ChickenEgg")
				quality = 1;
		}
	}
}
