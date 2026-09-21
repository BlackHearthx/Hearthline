using System.Collections.Generic;
using UnityEngine;

namespace BlackHearthx.Hearthline
{
	/// <summary>
	/// Soft nudge: fed, calm, tamed adults walk toward a same-species partner
	/// so Bond ticks happen sooner. Yields when already in partner range or pen is full.
	/// Only moves ZDO owners. Inspired by farm QoL (mate draw), not a portal system.
	/// </summary>
	internal static class MateDraw
	{
		private static readonly List<Character> Buffer = new List<Character>();
		private static float _nextScan;

		internal static void Tick()
		{
			if (!Plugin.EnableMod.Value || !Plugin.EnableMateDraw.Value)
			{
				return;
			}

			if (Time.unscaledTime < _nextScan)
			{
				return;
			}

			_nextScan = Time.unscaledTime + Plugin.MateDrawInterval.Value;

			Player local = Player.m_localPlayer;
			if (local == null || ((Character)local).IsDead())
			{
				return;
			}

			float playerRange = Plugin.MateDrawPlayerRange.Value;
			Buffer.Clear();
			Character.GetCharactersInRange(local.transform.position, playerRange, Buffer);

			float dt = Plugin.MateDrawInterval.Value;
			foreach (Character character in Buffer)
			{
				TryNudge(character, dt);
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

		private static void TryNudge(Character character, float dt)
		{
			if (character == null || character.IsPlayer() || !character.IsTamed())
			{
				return;
			}

			if (CubCarry.IsYoung(character) || character == CubCarry.Carried)
			{
				return;
			}

			Procreation procreation = ((Component)character).GetComponent<Procreation>();
			if (procreation == null || procreation.m_offspring == null)
			{
				return;
			}

			ZNetView nview = ((Component)character).GetComponent<ZNetView>();
			if (nview == null || !nview.IsValid() || !nview.IsOwner())
			{
				return;
			}

			long pregnant = nview.GetZDO().GetLong(ZDOVars.s_pregnant, 0L);
			Tameable tameable = ((Component)character).GetComponent<Tameable>();
			BaseAI ai = ((Component)character).GetComponent<BaseAI>();
			bool hungry = tameable != null && tameable.IsHungry();
			bool alerted = ai != null && ai.IsAlerted();
			bool overCap = IsOverCap(character, procreation);

			float partnerRange = procreation.m_partnerCheckRange > 0f ? procreation.m_partnerCheckRange : 3f;
			float drawRange = Mathf.Max(Plugin.MateDrawRange.Value, partnerRange + 1f);
			float stopAt = Mathf.Max(0.75f, partnerRange * 0.85f);

			Character partner = FindPartner(character, procreation, drawRange);
			bool hasPartner = partner != null;
			bool alreadyClose = hasPartner
				&& Vector3.Distance(character.transform.position, partner.transform.position) <= stopAt;

			if (!YardBreeding.MayMateDraw(
				    enabled: true,
				    isTamed: true,
				    isYoung: false,
				    isPregnant: pregnant != 0L,
				    isHungry: hungry,
				    isAlerted: alerted,
				    overCap: overCap,
				    hasPartnerInDrawRange: hasPartner,
				    partnerAlreadyClose: alreadyClose))
			{
				return;
			}

			Vector3 to = partner.transform.position - character.transform.position;
			to.y = 0f;
			float dist = to.magnitude;
			if (dist < 0.05f)
			{
				return;
			}

			float step = Mathf.Min(Plugin.MateDrawSpeed.Value * dt, dist - stopAt);
			if (step <= 0f)
			{
				return;
			}

			Vector3 next = character.transform.position + to.normalized * step;
			character.transform.position = next;

			if (Plugin.DebugLogging.Value)
			{
				Plugin.Log.LogInfo(
					$"[Hearthline] mate draw {YardTables.StripClone(character.gameObject.name)} -> {YardTables.StripClone(partner.gameObject.name)} step={step:0.##}");
			}
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
				if (other == null || other == self || other.IsPlayer() || !other.IsTamed())
				{
					continue;
				}

				if (CubCarry.IsYoung(other))
				{
					continue;
				}

				Procreation otherProc = ((Component)other).GetComponent<Procreation>();
				if (otherProc == null || otherProc.m_offspring == null)
				{
					continue;
				}

				string otherPrefab = YardTables.StripClone(other.gameObject.name);
				if (!string.Equals(otherPrefab, selfPrefab, System.StringComparison.OrdinalIgnoreCase)
				    && !string.Equals(Utils.GetPrefabName(otherProc.m_offspring), offspring, System.StringComparison.OrdinalIgnoreCase))
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
