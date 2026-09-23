using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace BlackHearthx.Hearthline.Patches
{
	/// <summary>
	/// When an untamed adult loads again after the player left, apply missed tame time
	/// if that animal's food is still on the ground within vanilla consume search range.
	/// </summary>
	[HarmonyPatch(typeof(Tameable), "TamingUpdate")]
	internal static class AwayTamePatch
	{
		private const double MinGapSeconds = 6d;

		private static readonly MethodInfo TameMethod = AccessTools.Method(typeof(Tameable), "Tame");

		[HarmonyPostfix]
		private static void Postfix(Tameable __instance)
		{
			if (!Plugin.EnableMod.Value || !Plugin.TameWhileAway.Value || __instance == null || __instance.IsTamed())
			{
				return;
			}

			if (ZNet.instance == null)
			{
				return;
			}

			ZNetView nview = ((Component)__instance).GetComponent<ZNetView>();
			if (nview == null || !nview.IsValid() || !nview.IsOwner())
			{
				return;
			}

			MonsterAI ai = ((Component)__instance).GetComponent<MonsterAI>();
			Character character = ((Component)__instance).GetComponent<Character>();
			if (ai == null || character == null || character.IsPlayer())
			{
				return;
			}

			ZDO zdo = nview.GetZDO();
			DateTime now = ZNet.instance.GetTime();
			long seenTicks = zdo.GetLong(HearthlineZdo.TameSeen, 0L);
			zdo.Set(HearthlineZdo.TameSeen, now.Ticks);
			if (seenTicks <= 0L)
			{
				return;
			}

			double gap = (now - new DateTime(seenTicks)).TotalSeconds;
			if (gap < MinGapSeconds)
			{
				return;
			}

			long fedTicks = zdo.GetLong(ZDOVars.s_tameLastFeeding, 0L);
			double sinceNow = fedTicks <= 0L
				? double.MaxValue
				: (now - new DateTime(fedTicks)).TotalSeconds;
			double sinceStart = sinceNow - gap;
			if (sinceStart < 0d)
			{
				sinceStart = 0d;
			}

			float timeLeft = zdo.GetFloat(ZDOVars.s_tameTimeLeft, __instance.m_tamingTime);
			List<ItemDrop> foods = CollectFood(ai, __instance.transform.position);
			int available = CountStacks(foods);
			AwayTame.Result result = AwayTame.Simulate(
				gap,
				__instance.m_fedDuration,
				sinceStart,
				timeLeft,
				available);

			if (result.FoodsEaten == 0 && result.TimeLeft >= timeLeft - 0.01f)
			{
				return;
			}

			int removed = Consume(foods, result.FoodsEaten, __instance, nview);
			if (removed < result.FoodsEaten)
			{
				result = AwayTame.Simulate(gap, __instance.m_fedDuration, sinceStart, timeLeft, removed);
			}

			if (result.FoodsEaten > 0 || result.TimeLeft < timeLeft)
			{
				DateTime fedAt = now.AddSeconds(-result.SecondsSinceFeedAtEnd);
				zdo.Set(ZDOVars.s_tameLastFeeding, fedAt.Ticks);
				zdo.Set(ZDOVars.s_tameTimeLeft, result.TimeLeft);
			}

			if (Plugin.DebugLogging.Value)
			{
				Plugin.Log.LogInfo(
					$"[Hearthline] away tame {YardTables.StripClone(__instance.gameObject.name)} gap={gap:0}s meals={result.FoodsEaten} left={result.TimeLeft:0}s done={result.Finished}");
			}

			if (result.Finished && TameMethod != null)
			{
				TameMethod.Invoke(__instance, null);
			}
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
			if (ai.m_consumeItems == null || ai.m_consumeItems.Count == 0)
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

		private static int Consume(List<ItemDrop> foods, int meals, Tameable tameable, ZNetView nview)
		{
			if (meals <= 0)
			{
				return 0;
			}

			int removed = 0;
			bool lastFavorite = false;
			string creature = YardTables.StripClone(tameable.gameObject.name);
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
