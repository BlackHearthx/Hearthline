using System;
using System.Collections.Generic;

namespace BlackHearthx.Hearthline
{
	/// <summary>
	/// Yard QoL helpers: vanilla overcrowding (m_maxCreatures / m_totalCheckRange)
	/// and soft mate-draw eligibility. Pure logic stays Unity-free for tests.
	/// </summary>
	public static class YardBreeding
	{
		public static bool IsOverCap(int sameSpeciesInRange, int maxCreatures)
		{
			return maxCreatures > 0 && sameSpeciesInRange >= maxCreatures;
		}

		public static bool CountsTowardCap(string selfPrefab, string offspringPrefab, string otherPrefab)
		{
			if (string.IsNullOrEmpty(otherPrefab))
			{
				return false;
			}

			if (string.Equals(otherPrefab, selfPrefab, System.StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}

			if (!string.IsNullOrEmpty(offspringPrefab)
			    && string.Equals(otherPrefab, offspringPrefab, System.StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}

			return WildHerd.IsParentSpeciesOf(selfPrefab, otherPrefab)
			       || WildHerd.IsParentSpeciesOf(otherPrefab, selfPrefab);
		}

		public static bool MayMateDraw(
			bool enabled,
			bool isTamed,
			bool isYoung,
			bool isPregnant,
			bool isHungry,
			bool isAlerted,
			bool overCap,
			bool hasPartnerInDrawRange,
			bool partnerAlreadyClose)
		{
			if (!enabled || !isTamed || isYoung || isPregnant || isHungry || isAlerted || overCap)
			{
				return false;
			}

			if (!hasPartnerInDrawRange || partnerAlreadyClose)
			{
				return false;
			}

			return true;
		}

		public enum MateMove
		{
			None,
			Walk,
			Stay
		}

		/// <summary>
		/// Walk toward the partner until inside stopAt, then stay beside it so the
		/// next Procreate tick still finds it. holdSlack keeps a settled pair from jittering.
		/// </summary>
		public static MateMove MateDrawMove(bool eligible, bool hasPartner, float distance, float stopAt, bool wasStaying, float holdSlack = 0.75f)
		{
			if (!eligible || !hasPartner)
			{
				return MateMove.None;
			}

			float limit = wasStaying ? stopAt + holdSlack : stopAt;
			return distance <= limit ? MateMove.Stay : MateMove.Walk;
		}

		/// <summary>
		/// Vanilla counts a partner by center distance (m_partnerCheckRange). Big bodies
		/// like the lox cannot overlap, so stop just outside contact instead of shoving.
		/// </summary>
		public static float MateStopDistance(float partnerCheckRange, float selfRadius, float otherRadius)
		{
			float range = partnerCheckRange > 0f ? partnerCheckRange : 3f;
			float contact = Math.Max(0f, selfRadius) + Math.Max(0f, otherRadius) + 0.25f;
			float comfy = Math.Max(0.75f, range * 0.7f);
			return Math.Max(comfy, contact);
		}

		public static string OverCapHoverLine(bool overCap, string language = GameText.English)
		{
			return overCap ? GameText.Line("pen_full", language) : string.Empty;
		}

		/// <summary>
		/// Pregnancy countdown from ZDO start ticks + Procreation.m_pregnancyDuration (ZNet clock).
		/// No percent — that is what lying "Show Pregnancy" UIs invent.
		/// </summary>
		public static string ExpectingHoverLine(long pregnantTicks, float pregnancyDurationSeconds, DateTime now, string language = GameText.English)
		{
			if (pregnantTicks <= 0L)
			{
				return string.Empty;
			}

			if (pregnancyDurationSeconds <= 0f)
			{
				return GameText.Line("expecting", language);
			}

			double left = pregnancyDurationSeconds - (now - new DateTime(pregnantTicks)).TotalSeconds;
			if (left <= 0.5)
			{
				return GameText.Line("expecting_soon", language);
			}

			return string.Format(GameText.Line("expecting_due", language), FormatDuration(left));
		}

		public static string FormatDuration(double seconds)
		{
			if (seconds < 0)
			{
				seconds = 0;
			}

			int whole = Math.Max(0, (int)Math.Floor(seconds));
			int d = whole / 86400;
			int h = whole % 86400 / 3600;
			int m = whole % 3600 / 60;
			int s = whole % 60;

			if (d > 0)
			{
				return string.Format("{0}d {1}h", d, h);
			}

			if (h > 0)
			{
				return string.Format("{0}h {1:D2}m", h, m);
			}

			if (m > 0)
			{
				return string.Format("{0}m {1:D2}s", m, s);
			}

			return string.Format("{0}s", s);
		}
	}
}
