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

		public static string OverCapHoverLine(bool overCap)
		{
			return overCap ? "Pen full — no breeding" : string.Empty;
		}

		/// <summary>
		/// Pregnancy countdown from ZDO start ticks + Procreation.m_pregnancyDuration (ZNet clock).
		/// No percent — that is what lying "Show Pregnancy" UIs invent.
		/// </summary>
		public static string ExpectingHoverLine(long pregnantTicks, float pregnancyDurationSeconds, DateTime now)
		{
			if (pregnantTicks <= 0L)
			{
				return string.Empty;
			}

			if (pregnancyDurationSeconds <= 0f)
			{
				return "Expecting";
			}

			double left = pregnancyDurationSeconds - (now - new DateTime(pregnantTicks)).TotalSeconds;
			if (left <= 0.5)
			{
				return "Expecting — any moment";
			}

			return "Expecting — due in " + FormatDuration(left);
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
