using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;

namespace BlackHearthx.Hearthline.Patches
{
	/// <summary>
	/// Current Valheim Procreation.Procreate (ilspycmd on live assembly_valheim):
	///   if (!m_nview.IsValid() || !m_nview.IsOwner() || !m_tameable.IsTamed()) return;
	/// Older builds used m_character.IsTamed() — both patterns are matched.
	/// Birth still does SetTamed(m_tameable.IsTamed()) so wild parent => wild cub (left alone).
	/// </summary>
	[HarmonyPatch(typeof(Procreation), "Procreate")]
	internal static class ProcreationPatch
	{
		internal static bool InProcreate;

		[HarmonyPrefix]
		private static void Prefix(Procreation __instance, out int __state)
		{
			InProcreate = true;
			__state = __instance.m_minOffspringLevel;

			if (!Plugin.EnableMod.Value)
			{
				return;
			}

			Character parent = ((Component)__instance).GetComponent<Character>();
			if (parent == null)
			{
				return;
			}

			int parentLevel = parent.GetLevel();
			int maxLevel = Bloodline.MaxAllowedLevel(Plugin.MaxStarLevel.Value, Plugin.IsCllcPresent());
			bool rolled = Plugin.RollUpgrade(Plugin.UpgradeChanceFor(__instance));
			int nextMin = Bloodline.RaisedMinOffspringLevel(__state, parentLevel, maxLevel, rolled);
			__instance.m_minOffspringLevel = nextMin;

			// Only log on an actual birth tick — Procreate also runs for love points.
			if (Plugin.DebugLogging.Value && IsBirthTick(__instance))
			{
				string name = ((Component)__instance).gameObject.name.Replace("(Clone)", string.Empty);
				Plugin.Log.LogInfo(
					$"[Hearthline] birth {name} parentLv={parentLevel} ({Bloodline.StarsFromLevel(parentLevel)}*) " +
					$"tamed={parent.IsTamed()} roll={(rolled ? "upgrade" : "keep")} min {__state} -> {nextMin} capLv={maxLevel}");
			}
		}

		private static bool IsBirthTick(Procreation procreation)
		{
			ZNetView nview = ((Component)procreation).GetComponent<ZNetView>();
			if (nview == null || !nview.IsValid() || ZNet.instance == null)
			{
				return false;
			}

			long ticks = nview.GetZDO().GetLong(ZDOVars.s_pregnant, 0L);
			if (ticks <= 0L || procreation.m_pregnancyDuration <= 0f)
			{
				return false;
			}

			double elapsed = (ZNet.instance.GetTime() - new System.DateTime(ticks)).TotalSeconds;
			return elapsed > procreation.m_pregnancyDuration;
		}

		[HarmonyPostfix]
		private static void Postfix(Procreation __instance, int __state)
		{
			__instance.m_minOffspringLevel = __state;
			InProcreate = false;
		}

		[HarmonyTranspiler]
		private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			MethodInfo characterIsTamed = AccessTools.Method(typeof(Character), "IsTamed");
			MethodInfo tameableIsTamed = AccessTools.Method(typeof(Tameable), "IsTamed");
			FieldInfo characterField = AccessTools.Field(typeof(Procreation), "m_character");
			FieldInfo tameableField = AccessTools.Field(typeof(Procreation), "m_tameable");
			MethodInfo replacement = AccessTools.Method(typeof(ProcreationPatch), nameof(MayStartProcreate));
			List<CodeInstruction> codes = new List<CodeInstruction>(instructions);
			int replaced = 0;

			for (int i = 0; i < codes.Count; i++)
			{
				// Current game: ldarg.0 / ldfld m_tameable / callvirt Tameable.IsTamed
				if (replaced == 0
				    && i + 2 < codes.Count
				    && codes[i].opcode == OpCodes.Ldarg_0
				    && tameableField != null
				    && codes[i + 1].opcode == OpCodes.Ldfld
				    && Equals(codes[i + 1].operand, tameableField)
				    && CodeInstructionExtensions.Calls(codes[i + 2], tameableIsTamed))
				{
					CodeInstruction load = new CodeInstruction(OpCodes.Ldarg_0);
					load.labels.AddRange(codes[i].labels);
					yield return load;
					yield return new CodeInstruction(OpCodes.Call, replacement);
					i += 2;
					replaced++;
					continue;
				}

				// Legacy: ldarg.0 / ldfld m_character / callvirt Character.IsTamed
				if (replaced == 0
				    && i + 2 < codes.Count
				    && codes[i].opcode == OpCodes.Ldarg_0
				    && characterField != null
				    && codes[i + 1].opcode == OpCodes.Ldfld
				    && Equals(codes[i + 1].operand, characterField)
				    && CodeInstructionExtensions.Calls(codes[i + 2], characterIsTamed))
				{
					CodeInstruction load = new CodeInstruction(OpCodes.Ldarg_0);
					load.labels.AddRange(codes[i].labels);
					yield return load;
					yield return new CodeInstruction(OpCodes.Call, replacement);
					i += 2;
					replaced++;
					continue;
				}

				yield return codes[i];
			}

			if (replaced != 1)
			{
				Plugin.Log?.LogError(
					$"Hearthline expected 1 early IsTamed in Procreation.Procreate (m_tameable or m_character), found {replaced}.");
			}
			else if (Plugin.DebugLogging.Value)
			{
				Plugin.Log?.LogInfo("[Hearthline] Procreate wild-herd transpiler applied.");
			}
		}

		private static bool MayStartProcreate(Procreation instance)
		{
			Character parent = ((Component)instance).GetComponent<Character>();
			bool tamed = parent != null && parent.IsTamed();
			bool hasOffspring = instance.m_offspring != null;
			bool wildOn = Plugin.EnableMod.Value && Plugin.WildHerds.Value;
			return WildHerd.MayStartProcreate(tamed, wildOn, hasOffspring);
		}
	}

	[HarmonyPatch(typeof(Procreation), nameof(Procreation.ReadyForProcreation))]
	internal static class ReadyForProcreationPatch
	{
		[HarmonyPostfix]
		private static void Postfix(Procreation __instance, ref bool __result)
		{
			Character parent = ((Component)__instance).GetComponent<Character>();
			Tameable tameable = ((Component)__instance).GetComponent<Tameable>();
			ZNetView nview = ((Component)__instance).GetComponent<ZNetView>();
			bool pregnant = nview != null && nview.IsValid() && nview.GetZDO().GetLong(ZDOVars.s_pregnant, 0L) != 0L;
			bool hungry = tameable != null && tameable.IsHungry();
			bool tamed = parent != null && parent.IsTamed();
			bool wildOn = Plugin.EnableMod.Value && Plugin.WildHerds.Value;
			bool hasOffspring = __instance.m_offspring != null;

			__result = WildHerd.MayCountAsPartner(
				__result,
				tamed,
				wildOn,
				hasOffspring,
				pregnant,
				hungry,
				Plugin.WildHerdsIgnoreHunger.Value);
		}
	}

	[HarmonyPatch(typeof(Tameable), nameof(Tameable.IsHungry))]
	internal static class TameableIsHungryPatch
	{
		[HarmonyPostfix]
		private static void Postfix(Tameable __instance, ref bool __result)
		{
			if (!__result)
			{
				return;
			}

			Character character = ((Component)__instance).GetComponent<Character>();
			Procreation procreation = ((Component)__instance).GetComponent<Procreation>();
			bool tamed = character != null && character.IsTamed();
			bool hasOffspring = procreation != null && procreation.m_offspring != null;
			bool wildOn = Plugin.EnableMod.Value && Plugin.WildHerds.Value && Plugin.WildHerdsIgnoreHunger.Value;

			if (WildHerd.ShouldIgnoreHunger(wildOn, tamed, ProcreationPatch.InProcreate, hasOffspring))
			{
				__result = false;
			}
		}
	}

	/// <summary>
	/// Throttle wild pregnancies so herds do not explode when hunger is ignored during Procreate.
	/// Tamed MakePregnant is unchanged. Vanilla m_maxCreatures still applies before this.
	/// </summary>
	[HarmonyPatch(typeof(Procreation), "MakePregnant")]
	internal static class MakePregnantPatch
	{
		[HarmonyPrefix]
		private static bool Prefix(Procreation __instance)
		{
			if (!Plugin.EnableMod.Value || !Plugin.WildHerds.Value)
			{
				return true;
			}

			Character parent = ((Component)__instance).GetComponent<Character>();
			if (parent == null || parent.IsTamed())
			{
				return true;
			}

			float roll = Random.Range(0f, 100f);
			bool ok = WildHerd.MayWildPregnancy(roll, Plugin.WildHerdPregnancyChance.Value);
			if (Plugin.DebugLogging.Value)
			{
				string name = ((Component)__instance).gameObject.name.Replace("(Clone)", string.Empty);
				Plugin.Log.LogInfo(
					$"[Hearthline] wild pregnancy {name} roll={roll:0.#} need<={Plugin.WildHerdPregnancyChance.Value:0.#} -> {(ok ? "yes" : "no")}");
			}

			return ok;
		}
	}
}
