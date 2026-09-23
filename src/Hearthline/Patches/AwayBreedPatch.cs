using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace BlackHearthx.Hearthline.Patches
{
	/// <summary>
	/// Tamed adults catch up love, pregnancy, and birth after the pen was unloaded.
	/// Wild adults are ignored here. Their Procreate still runs only while that area is loaded.
	/// </summary>
	[HarmonyPatch(typeof(Procreation), "Procreate")]
	[HarmonyPriority(Priority.VeryHigh)]
	internal static class AwayBreedPatch
	{
		[HarmonyPrefix]
		private static bool Prefix(Procreation __instance)
		{
			if (!Plugin.EnableMod.Value || !Plugin.BreedWhileAway.Value || __instance == null || __instance.m_offspring == null)
			{
				return true;
			}

			Character character = ((Component)__instance).GetComponent<Character>();
			Tameable tameable = ((Component)__instance).GetComponent<Tameable>();
			if (character == null || tameable == null || !AwayBreed.AppliesTo(character.IsTamed()))
			{
				return true;
			}

			if (ZNet.instance == null || ZNetScene.instance == null)
			{
				return true;
			}

			ZNetView nview = ((Component)__instance).GetComponent<ZNetView>();
			if (nview == null || !nview.IsValid() || !nview.IsOwner())
			{
				return true;
			}

			ZDO zdo = nview.GetZDO();
			DateTime now = ZNet.instance.GetTime();
			long seenTicks = zdo.GetLong(HearthlineZdo.BreedSeen, 0L);
			zdo.Set(HearthlineZdo.BreedSeen, now.Ticks);
			if (seenTicks <= 0L)
			{
				return true;
			}

			double gap = (now - new DateTime(seenTicks)).TotalSeconds;
			if (!AwayBreed.GapIsAbsence(gap, __instance.m_updateInterval))
			{
				return true;
			}

			GameObject myPrefab = ZNetScene.instance.GetPrefab(Utils.GetPrefabName(character.gameObject));
			GameObject offspringPrefab = ZNetScene.instance.GetPrefab(Utils.GetPrefabName(__instance.m_offspring));
			if (myPrefab == null || offspringPrefab == null)
			{
				return true;
			}

			Vector3 position = ((Component)__instance).transform.position;
			int adults = SpawnSystem.GetNrOfInstances(myPrefab, position, __instance.m_totalCheckRange);
			if (adults < 1)
			{
				return true;
			}

			int young = SpawnSystem.GetNrOfInstances(offspringPrefab, position, __instance.m_totalCheckRange);
			int creatures = adults + young;
			bool hasPartner = HasPartner(__instance, myPrefab, position);

			long fedTicks = zdo.GetLong(ZDOVars.s_tameLastFeeding, 0L);
			double sinceNow = fedTicks <= 0L
				? double.MaxValue
				: (now - new DateTime(fedTicks)).TotalSeconds;
			double sinceStart = sinceNow - gap;
			if (sinceStart < 0d)
			{
				sinceStart = 0d;
			}

			long pregTicks = zdo.GetLong(ZDOVars.s_pregnant, 0L);
			double pregElapsed = -1d;
			if (pregTicks > 0L)
			{
				pregElapsed = (now - new DateTime(pregTicks)).TotalSeconds - gap;
				if (pregElapsed < 0d)
				{
					pregElapsed = 0d;
				}
			}

			int loveNow = zdo.GetInt(ZDOVars.s_lovePoints, 0);
			MonsterAI ai = ((Component)__instance).GetComponent<MonsterAI>();
			List<ItemDrop> foods = CollectFood(ai, position);
			int available = CountStacks(foods);

			AwayBreed.Result result = AwayBreed.Simulate(
				gap,
				__instance.m_updateInterval,
				__instance.m_pregnancyChance,
				__instance.m_pregnancyDuration,
				__instance.m_requiredLovePoints,
				__instance.m_maxCreatures,
				creatures,
				tameable.m_fedDuration,
				sinceStart,
				loveNow,
				pregElapsed,
				hasPartner,
				available,
				() => UnityEngine.Random.value);

			int removed = Consume(foods, result.FoodsEaten, character, nview);
			if (removed < result.FoodsEaten)
			{
				result = AwayBreed.Simulate(
					gap,
					__instance.m_updateInterval,
					__instance.m_pregnancyChance,
					__instance.m_pregnancyDuration,
					__instance.m_requiredLovePoints,
					__instance.m_maxCreatures,
					creatures,
					tameable.m_fedDuration,
					sinceStart,
					loveNow,
					pregElapsed,
					hasPartner,
					removed,
					() => UnityEngine.Random.value);
			}

			bool started = pregElapsed < 0d && result.PregnancyElapsed >= 0d;
			bool ended = pregElapsed >= 0d && result.PregnancyElapsed < 0d;
			bool loveChanged = result.LovePoints != loveNow;
			if (result.Births <= 0 && !started && !ended && !loveChanged && result.FoodsEaten <= 0)
			{
				return true;
			}

			if (result.FoodsEaten > 0)
			{
				DateTime fedAt = now.AddSeconds(-Math.Max(0d, result.SecondsSinceFeedAtEnd));
				zdo.Set(ZDOVars.s_tameLastFeeding, fedAt.Ticks);
			}

			int spawned = SpawnBirths(__instance, character, offspringPrefab, result.Births);
			if (result.Births > 0 && spawned == 0)
			{
				// Do not clear a pregnancy we failed to deliver. Vanilla still runs this tick.
				return true;
			}

			if (result.PregnancyElapsed < 0d)
			{
				zdo.Set(ZDOVars.s_pregnant, 0L);
			}
			else
			{
				zdo.Set(ZDOVars.s_pregnant, now.AddSeconds(-result.PregnancyElapsed).Ticks);
			}

			zdo.Set(ZDOVars.s_lovePoints, result.LovePoints);

			if (Plugin.DebugLogging.Value)
			{
				Plugin.Log.LogInfo(
					$"[Hearthline] away breed {YardTables.StripClone(character.gameObject.name)} gap={gap:0}s births={spawned} love={result.LovePoints} meals={result.FoodsEaten}");
			}

			return false;
		}

		private static bool HasPartner(Procreation procreation, GameObject myPrefab, Vector3 position)
		{
			if (procreation.m_noPartnerOffspring != null)
			{
				return true;
			}

			GameObject partnerPrefab = myPrefab;
			if (procreation.m_seperatePartner != null)
			{
				partnerPrefab = ZNetScene.instance.GetPrefab(Utils.GetPrefabName(procreation.m_seperatePartner));
			}

			if (partnerPrefab == null)
			{
				return false;
			}

			// Hunger and pregnancy are ignored here on purpose: the sim feeds them from food still on the ground.
			// Vanilla's ready-check would see them hungry on return and skip the whole absence.
			int nearby = SpawnSystem.GetNrOfInstances(partnerPrefab, position, procreation.m_partnerCheckRange);
			if (procreation.m_seperatePartner != null)
			{
				return nearby >= 1;
			}

			return nearby >= 2;
		}

		private static int SpawnBirths(Procreation procreation, Character parent, GameObject offspringPrefab, int births)
		{
			if (births <= 0 || offspringPrefab == null)
			{
				return 0;
			}

			int originalMin = procreation.m_minOffspringLevel;
			int parentLevel = parent.GetLevel();
			int maxLevel = Bloodline.MaxAllowedLevel(Plugin.MaxStarLevel.Value, Plugin.IsCllcPresent());
			Transform transform = ((Component)procreation).transform;
			int spawned = 0;
			for (int i = 0; i < births; i++)
			{
				bool rolled = Plugin.RollUpgrade(Plugin.UpgradeChanceFor(procreation));
				int min = Bloodline.RaisedMinOffspringLevel(originalMin, parentLevel, maxLevel, rolled);
				int level = Mathf.Max(min, parentLevel);
				GameObject prefab = ResolveOffspring(procreation, offspringPrefab, transform.position);
				if (prefab == null)
				{
					break;
				}

				float angle = (Mathf.PI * 2f * i) / births;
				Vector3 dir = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
				float dist = procreation.m_spawnOffset > 0f ? procreation.m_spawnOffset : 2f;
				Vector3 pos = transform.position + dir * dist;
				Quaternion rot = Quaternion.LookRotation(-transform.forward, Vector3.up);
				GameObject baby = UnityEngine.Object.Instantiate(prefab, pos, rot);
				if (baby == null)
				{
					break;
				}

				Character babyCharacter = baby.GetComponent<Character>();
				if (babyCharacter != null)
				{
					babyCharacter.SetTamed(true);
					babyCharacter.SetLevel(level);
				}
				else
				{
					baby.GetComponent<ItemDrop>()?.SetQuality(level);
				}

				procreation.m_birthEffects?.Create(baby.transform.position, Quaternion.identity);
				spawned++;
			}

			return spawned;
		}

		private static GameObject ResolveOffspring(Procreation procreation, GameObject offspringPrefab, Vector3 position)
		{
			if (procreation.m_noPartnerOffspring == null || ZNetScene.instance == null)
			{
				return offspringPrefab;
			}

			GameObject partnerPrefab = procreation.m_seperatePartner != null
				? ZNetScene.instance.GetPrefab(Utils.GetPrefabName(procreation.m_seperatePartner))
				: ZNetScene.instance.GetPrefab(Utils.GetPrefabName(((Component)procreation).gameObject));
			int nearby = partnerPrefab != null
				? SpawnSystem.GetNrOfInstances(partnerPrefab, position, procreation.m_partnerCheckRange, false, true)
				: 0;
			bool missing = procreation.m_seperatePartner != null ? nearby < 1 : nearby < 2;
			if (!missing)
			{
				return offspringPrefab;
			}

			GameObject alone = ZNetScene.instance.GetPrefab(Utils.GetPrefabName(procreation.m_noPartnerOffspring));
			return alone != null ? alone : offspringPrefab;
		}

		private static int CountStacks(List<ItemDrop> foods)
		{
			int count = 0;
			foreach (ItemDrop drop in foods)
			{
				if (drop?.m_itemData != null && drop.m_itemData.m_stack > 0)
				{
					count += drop.m_itemData.m_stack;
				}
			}

			return count;
		}

		private static List<ItemDrop> CollectFood(MonsterAI ai, Vector3 position)
		{
			List<ItemDrop> found = new List<ItemDrop>();
			if (ai == null || ai.m_consumeItems == null || ai.m_consumeItems.Count == 0)
			{
				return found;
			}

			float range = ai.m_consumeSearchRange > 0f ? ai.m_consumeSearchRange : 5f;
			int mask = LayerMask.GetMask("item");
			Collider[] hits = Physics.OverlapSphere(position, range, mask);
			foreach (Collider hit in hits)
			{
				if (hit == null || hit.attachedRigidbody == null)
				{
					continue;
				}

				ItemDrop drop = hit.attachedRigidbody.GetComponent<ItemDrop>();
				ZNetView view = drop != null ? drop.GetComponent<ZNetView>() : null;
				if (drop == null || view == null || !view.IsValid() || !CanConsume(ai, drop.m_itemData))
				{
					continue;
				}

				found.Add(drop);
			}

			found.Sort((a, b) =>
			{
				float da = Vector3.Distance(a.transform.position, position);
				float db = Vector3.Distance(b.transform.position, position);
				return da.CompareTo(db);
			});
			return found;
		}

		private static bool CanConsume(MonsterAI ai, ItemDrop.ItemData item)
		{
			if (item?.m_shared == null)
			{
				return false;
			}

			foreach (ItemDrop prefab in ai.m_consumeItems)
			{
				if (prefab?.m_itemData?.m_shared != null
				    && prefab.m_itemData.m_shared.m_name == item.m_shared.m_name)
				{
					return true;
				}
			}

			return false;
		}

		private static int Consume(List<ItemDrop> foods, int meals, Character character, ZNetView nview)
		{
			if (meals <= 0)
			{
				return 0;
			}

			int removed = 0;
			bool lastFavorite = false;
			string creature = YardTables.StripClone(character.gameObject.name);
			foreach (ItemDrop drop in foods)
			{
				while (removed < meals && drop != null && drop.m_itemData != null && drop.m_itemData.m_stack > 0)
				{
					string foodName = YardTables.StripClone(drop.gameObject.name);
					lastFavorite = Plugin.IsFavoriteFood(creature, foodName, drop);
					if (!drop.RemoveOne())
					{
						return removed;
					}

					removed++;
				}
			}

			if (removed > 0 && nview != null && nview.IsValid())
			{
				nview.GetZDO().Set(HearthlineZdo.FavoriteMeal, lastFavorite ? 1 : 0);
			}

			return removed;
		}
	}
}
