using HarmonyLib;
using UnityEngine;

namespace BlackHearthx.Hearthline.Patches
{
	/// <summary>
	/// Z stays on the horse. The saddle keeps only its own lines (ride, remove saddle).
	/// Young animals still pull the prompt onto the cub.
	/// </summary>
	[HarmonyPatch(typeof(Player), "FindHoverObject")]
	internal static class PlayerFindHoverYoungPatch
	{
		[HarmonyPostfix]
		private static void Postfix(Player __instance, ref GameObject hover, ref Character hoverCreature)
		{
			if (!Plugin.EnableMod.Value || (!Plugin.EnableCubCarry.Value && !Plugin.ClaimWildYoung.Value))
			{
				return;
			}

			if (CubCarry.IsCarrying)
			{
				return;
			}

			Sadle saddle = CubCarry.ClosestAimedSaddle(__instance, __instance.m_maxInteractDistance);
			if (saddle != null)
			{
				hover = ((Component)saddle).gameObject;
				return;
			}

			Character target = CubCarry.FindCarryTargetUnderCrosshair(__instance, __instance.m_maxInteractDistance);
			if (target == null || !CubCarry.IsYoung(target))
			{
				return;
			}

			hover = target.gameObject;
			hoverCreature = target;
		}
	}

	[HarmonyPatch(typeof(Tameable), "GetHoverText")]
	internal static class TameableStealHoverPatch
	{
		[HarmonyPostfix]
		private static void Postfix(Tameable __instance, ref string __result)
		{
			if (!Plugin.EnableMod.Value || string.IsNullOrEmpty(__result))
			{
				return;
			}

			string hint = CubCarry.CarryHotkeyHint;
			if (CubCarry.IsCarrying)
			{
				__result += $"\n{hint} Put down";
				return;
			}

			Character character = ((Component)__instance).GetComponent<Character>();
			if (character == null || !Plugin.EnableCubCarry.Value)
			{
				return;
			}

			if (CubCarry.IsYoung(character))
			{
				if (!character.IsTamed() && Plugin.ClaimWildYoung.Value)
				{
					__result += $"\n{hint} Steal & carry";
				}
				else if (character.IsTamed())
				{
					__result += $"\n{hint} Carry";
				}

				return;
			}

			if (CubCarry.IsTamedAdult(character)
			    && __result.IndexOf("Carry", System.StringComparison.Ordinal) < 0)
			{
				__result += $"\n{hint} Carry";
			}
		}
	}

	[HarmonyPatch(typeof(Character), nameof(Character.GetHoverText))]
	internal static class CharacterYoungHoverPatch
	{
		[HarmonyPostfix]
		private static void Postfix(Character __instance, ref string __result)
		{
			if (!Plugin.EnableMod.Value || __instance == null || !CubCarry.IsYoung(__instance))
			{
				return;
			}

			if (string.IsNullOrEmpty(__result))
			{
				string name = YardTables.PrettyCreatureName(
					__instance.gameObject.name,
					__instance.m_name);
				string wild = __instance.IsTamed() ? "tame" : "wild";
				__result = $"{name} ( {wild} )";
			}

			string hint = CubCarry.CarryHotkeyHint;
			if (CubCarry.IsCarrying)
			{
				if (__result.IndexOf("Put down", System.StringComparison.Ordinal) < 0)
				{
					__result += $"\n{hint} Put down";
				}

				return;
			}

			if (!__instance.IsTamed() && Plugin.ClaimWildYoung.Value
			    && __result.IndexOf("Steal", System.StringComparison.Ordinal) < 0)
			{
				__result += $"\n{hint} Steal & carry";
			}
			else if (__instance.IsTamed() && Plugin.EnableCubCarry.Value
			         && __result.IndexOf("Carry", System.StringComparison.Ordinal) < 0)
			{
				__result += $"\n{hint} Carry";
			}
		}
	}

	/// <summary>
	/// Drop carried animal when the local player takes real damage (anti combat-cheese).
	/// Hooks Character.RPC_Damage — same entry as networked hits.
	/// </summary>
	[HarmonyPatch(typeof(Character), "RPC_Damage")]
	internal static class CarrierDamageDropPatch
	{
		[HarmonyPostfix]
		private static void Postfix(Character __instance, HitData hit)
		{
			if (!Plugin.EnableMod.Value || !Plugin.DropCarryOnDamage.Value || !CubCarry.IsCarrying)
			{
				return;
			}

			Player local = Player.m_localPlayer;
			if (local == null || __instance != (Character)local || hit == null)
			{
				return;
			}

			if (hit.GetTotalDamage() <= 0f)
			{
				return;
			}

			CubCarry.OnCarrierDamaged(local);
		}
	}

	/// <summary>
	/// Arms full of livestock — no swings or bow draws until put down.
	/// Confirmed: Humanoid.StartAttack is the shared entry for primary and secondary attacks.
	/// </summary>
	[HarmonyPatch(typeof(Humanoid), "StartAttack")]
	internal static class CarryBlocksAttackPatch
	{
		[HarmonyPrefix]
		private static bool Prefix(Humanoid __instance)
		{
			if (!Plugin.EnableMod.Value || !Plugin.BlockAttackWhileCarrying.Value || !CubCarry.IsCarrying)
			{
				return true;
			}

			Player local = Player.m_localPlayer;
			if (local == null || __instance != (Humanoid)local)
			{
				return true;
			}

			return false;
		}
	}
}
