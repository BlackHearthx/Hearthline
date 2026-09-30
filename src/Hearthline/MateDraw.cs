using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using HarmonyLib;
using UnityEngine;

namespace BlackHearthx.Hearthline
{
	/// <summary>
	/// Fed, calm, tamed adults walk to a same-species partner with their own AI
	/// (pathing + animation) and stay beside it, so Procreate ticks keep finding
	/// the partner instead of the idle wander pulling the pair apart.
	/// Runs in place of BaseAI.IdleMovement only; followers, fleeing, eating and
	/// combat never reach it.
	/// </summary>
	internal static class MateDraw
	{
		private sealed class Plan
		{
			public float NextThink;
			public Character Partner;
			public float StopAt;
			public bool Eligible;
			public bool Staying;
			public float StayStart;
			public float BestDistance = float.MaxValue;
			public float LastProgress;
			public float RestUntil;
		}

		private static readonly ConditionalWeakTable<BaseAI, Plan> Plans = new ConditionalWeakTable<BaseAI, Plan>();

		private static readonly Func<BaseAI, float, Vector3, float, bool, bool> MoveTo =
			AccessTools.MethodDelegate<Func<BaseAI, float, Vector3, float, bool, bool>>(
				AccessTools.Method(typeof(BaseAI), "MoveTo", new[] { typeof(float), typeof(Vector3), typeof(float), typeof(bool) }));

		/// <summary>True when mate draw moved (or held) the animal this frame.</summary>
		internal static bool Steer(BaseAI ai, float dt)
		{
			if (!Plugin.EnableMod.Value || !Plugin.EnableMateDraw.Value || ai == null || MoveTo == null)
			{
				return false;
			}

			Character self = ((Component)ai).GetComponent<Character>();
			if (self == null || !self.IsTamed())
			{
				return false;
			}

			Plan plan = Plans.GetOrCreateValue(ai);
			float now = Time.time;
			if (now < plan.RestUntil)
			{
				return false;
			}

			if (now >= plan.NextThink)
			{
				plan.NextThink = now + Plugin.MateDrawInterval.Value;
				Think(ai, self, plan);
			}

			if (!plan.Eligible || plan.Partner == null || plan.Partner.IsDead())
			{
				Reset(plan);
				return false;
			}

			Vector3 partnerPos = plan.Partner.transform.position;
			float distance = Utils.DistanceXZ(self.transform.position, partnerPos);
			YardBreeding.MateMove move = YardBreeding.MateDrawMove(true, true, distance, plan.StopAt, plan.Staying);
			if (move == YardBreeding.MateMove.Stay)
			{
				if (!plan.Staying)
				{
					plan.Staying = true;
					plan.StayStart = now;
				}
			}
			else
			{
				if (plan.Staying || plan.BestDistance == float.MaxValue)
				{
					plan.Staying = false;
					plan.BestDistance = distance;
					plan.LastProgress = now;
				}
				else if (distance < plan.BestDistance - 0.5f)
				{
					plan.BestDistance = distance;
					plan.LastProgress = now;
				}
			}

			if (YardBreeding.MateDrawShouldRest(plan.Staying, now - plan.StayStart, now - plan.LastProgress))
			{
				if (Plugin.DebugLogging.Value)
				{
					Plugin.Log.LogInfo(
						$"[Hearthline] mate draw rest {YardTables.StripClone(self.gameObject.name)} ({(plan.Staying ? "stood long enough" : "cannot reach partner")})");
				}

				Reset(plan);
				plan.RestUntil = now + YardBreeding.MateRestSeconds;
				return false;
			}

			if (plan.Staying)
			{
				ai.StopMoving();
				return true;
			}

			MoveTo(ai, dt, partnerPos, plan.StopAt, false);
			return true;
		}

		private static void Reset(Plan plan)
		{
			plan.Staying = false;
			plan.BestDistance = float.MaxValue;
		}

		private static void Think(BaseAI ai, Character self, Plan plan)
		{
			plan.Eligible = false;
			plan.Partner = null;

			if (CubCarry.IsYoung(self) || self == CubCarry.Carried)
			{
				return;
			}

			Player local = Player.m_localPlayer;
			if (local == null
			    || Utils.DistanceXZ(local.transform.position, self.transform.position) > Plugin.MateDrawPlayerRange.Value)
			{
				return;
			}

			Procreation procreation = ((Component)self).GetComponent<Procreation>();
			if (procreation == null || procreation.m_offspring == null)
			{
				return;
			}

			ZNetView nview = ((Component)self).GetComponent<ZNetView>();
			if (nview == null || !nview.IsValid() || !nview.IsOwner())
			{
				return;
			}

			Tameable tameable = ((Component)self).GetComponent<Tameable>();
			bool pregnant = nview.GetZDO().GetLong(ZDOVars.s_pregnant, 0L) != 0L;
			bool hungry = tameable != null && tameable.IsHungry();
			bool overCap = IsOverCap(self, procreation);

			float partnerRange = procreation.m_partnerCheckRange > 0f ? procreation.m_partnerCheckRange : 3f;
			float drawRange = Mathf.Max(Plugin.MateDrawRange.Value, partnerRange + 1f);
			Character partner = FindPartner(self, procreation, drawRange);

			bool eligible = YardBreeding.MayMateDraw(
				enabled: true,
				isTamed: true,
				isYoung: false,
				isPregnant: pregnant,
				isHungry: hungry,
				isAlerted: ai.IsAlerted(),
				overCap: overCap,
				hasPartnerInDrawRange: partner != null,
				partnerAlreadyClose: false);
			if (!eligible)
			{
				return;
			}

			plan.Eligible = true;
			plan.Partner = partner;
			plan.StopAt = YardBreeding.MateStopDistance(partnerRange, self.GetRadius(), partner.GetRadius());

			if (Plugin.DebugLogging.Value && !plan.Staying)
			{
				Plugin.Log.LogInfo(
					$"[Hearthline] mate draw {YardTables.StripClone(self.gameObject.name)} -> {YardTables.StripClone(partner.gameObject.name)} " +
					$"dist={Utils.DistanceXZ(self.transform.position, partner.transform.position):0.#} stopAt={plan.StopAt:0.#} partnerRange={partnerRange:0.#}");
			}
		}

		internal static int CountSameSpecies(Character self, Procreation procreation, float radius)
		{
			if (self == null || procreation == null || radius <= 0f)
			{
				return 0;
			}

			string selfPrefab = YardTables.StripClone(self.gameObject.name);
			string offspring = procreation.m_offspring != null
				? Utils.GetPrefabName(procreation.m_offspring)
				: string.Empty;

			List<Character> nearby = new List<Character>();
			Character.GetCharactersInRange(self.transform.position, radius, nearby);

			int count = 0;
			foreach (Character other in nearby)
			{
				if (other == null || other.IsPlayer())
				{
					continue;
				}

				string otherPrefab = YardTables.StripClone(other.gameObject.name);
				if (YardBreeding.CountsTowardCap(selfPrefab, offspring, otherPrefab))
				{
					count++;
				}
			}

			return count;
		}

		internal static bool IsOverCap(Character character, Procreation procreation)
		{
			if (character == null || procreation == null)
			{
				return false;
			}

			float radius = procreation.m_totalCheckRange > 0f ? procreation.m_totalCheckRange : 10f;
			int count = CountSameSpecies(character, procreation, radius);
			return YardBreeding.IsOverCap(count, procreation.m_maxCreatures);
		}

		private static Character FindPartner(Character self, Procreation procreation, float range)
		{
			string selfPrefab = YardTables.StripClone(self.gameObject.name);
			string offspring = Utils.GetPrefabName(procreation.m_offspring);

			List<Character> nearby = new List<Character>();
			Character.GetCharactersInRange(self.transform.position, range, nearby);

			Character best = null;
			float bestDist = float.MaxValue;
			foreach (Character other in nearby)
			{
				if (other == null || other == self || other.IsPlayer() || !other.IsTamed() || other.IsDead())
				{
					continue;
				}

				if (CubCarry.IsYoung(other) || other == CubCarry.Carried)
				{
					continue;
				}

				Procreation otherProc = ((Component)other).GetComponent<Procreation>();
				if (otherProc == null || otherProc.m_offspring == null)
				{
					continue;
				}

				string otherPrefab = YardTables.StripClone(other.gameObject.name);
				if (!string.Equals(otherPrefab, selfPrefab, StringComparison.OrdinalIgnoreCase)
				    && !string.Equals(Utils.GetPrefabName(otherProc.m_offspring), offspring, StringComparison.OrdinalIgnoreCase))
				{
					continue;
				}

				ZNetView otherView = ((Component)other).GetComponent<ZNetView>();
				if (otherView != null && otherView.IsValid()
				    && otherView.GetZDO().GetLong(ZDOVars.s_pregnant, 0L) != 0L)
				{
					continue;
				}

				Tameable otherTame = ((Component)other).GetComponent<Tameable>();
				if (otherTame != null && otherTame.IsHungry())
				{
					continue;
				}

				float d = Vector3.Distance(self.transform.position, other.transform.position);
				if (d < bestDist)
				{
					bestDist = d;
					best = other;
				}
			}

			return best;
		}
	}
}
