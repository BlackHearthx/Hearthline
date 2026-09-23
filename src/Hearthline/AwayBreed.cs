using System;

namespace BlackHearthx.Hearthline
{
	/// <summary>
	/// Replays missed Procreate ticks for one tamed adult.
	/// Wild herds must not use this. Hunger is ignored for them, so catching every unloaded herd up to the pen cap would flood the world.
	/// </summary>
	internal static class AwayBreed
	{
		/// <summary>Stops one return from spawning a broken config's worth of babies in a single frame.</summary>
		internal const int HardBirthCap = 16;

		internal static bool AppliesTo(bool tamed)
		{
			return tamed;
		}

		/// <summary>
		/// A loaded animal ticks about once per update interval. A longer gap is time the area was unloaded.
		/// </summary>
		internal static bool GapIsAbsence(double gapSeconds, float updateInterval)
		{
			if (gapSeconds <= 0d || updateInterval <= 0f)
			{
				return false;
			}

			double bar = Math.Max(20d, updateInterval * 2.5d);
			return gapSeconds > bar;
		}

		internal readonly struct Result
		{
			internal Result(int lovePoints, double pregnancyElapsed, int births, int foodsEaten, double secondsSinceFeedAtEnd)
			{
				LovePoints = lovePoints;
				PregnancyElapsed = pregnancyElapsed;
				Births = births;
				FoodsEaten = foodsEaten;
				SecondsSinceFeedAtEnd = secondsSinceFeedAtEnd;
			}

			internal int LovePoints { get; }

			/// <summary>Seconds since this pregnancy started. Negative means not pregnant.</summary>
			internal double PregnancyElapsed { get; }

			internal int Births { get; }
			internal int FoodsEaten { get; }
			internal double SecondsSinceFeedAtEnd { get; }
		}

		/// <summary>
		/// pregnancyElapsedNow below 0 means not pregnant.
		/// Vanilla aborts a love tick when the roll is less than or equal to pregnancyChance.
		/// An in-progress pregnancy still finishes if they go hungry. New love does not.
		/// </summary>
		internal static Result Simulate(
			double gapSeconds,
			float updateInterval,
			float pregnancyChance,
			float pregnancyDuration,
			int requiredLovePoints,
			int maxCreatures,
			int creaturesNow,
			float fedDuration,
			double secondsSinceFeedAtStart,
			int loveNow,
			double pregnancyElapsedNow,
			bool hasPartner,
			int foodOnGround,
			Func<float> roll01,
			int birthCap = HardBirthCap)
		{
			int love = loveNow < 0 ? 0 : loveNow;
			double preg = pregnancyElapsedNow;
			bool pregnant = preg >= 0d;
			int births = 0;
			int eaten = 0;
			double sinceFeed = secondsSinceFeedAtStart < 0d ? 0d : secondsSinceFeedAtStart;
			if (foodOnGround < 0)
			{
				foodOnGround = 0;
			}

			if (requiredLovePoints < 1)
			{
				requiredLovePoints = 1;
			}

			if (birthCap < 1)
			{
				birthCap = 1;
			}

			if (creaturesNow < 0)
			{
				creaturesNow = 0;
			}

			// Already due when the area unloaded. Vanilla would birth on the next tick, then the rest of the gap can start another cycle.
			if (pregnant && births < birthCap && preg > pregnancyDuration)
			{
				births++;
				creaturesNow++;
				pregnant = false;
				preg = -1d;
				love = 0;
			}

			if (gapSeconds <= 0d || updateInterval <= 0f)
			{
				return new Result(love, pregnant ? preg : -1d, births, eaten, sinceFeed);
			}

			int ticks = (int)(gapSeconds / updateInterval);
			if (ticks > 200000)
			{
				ticks = 200000;
			}

			double cursor = 0d;
			for (int n = 0; n < ticks; n++)
			{
				if (births >= birthCap)
				{
					break;
				}

				if (!pregnant && !CanLove(hasPartner, creaturesNow, maxCreatures))
				{
					break;
				}

				if (!pregnant && Hungry(sinceFeed, fedDuration) && eaten >= foodOnGround)
				{
					break;
				}

				cursor += updateInterval;
				bool ate = TryEat(ref sinceFeed, ref eaten, foodOnGround, fedDuration);

				if (pregnant)
				{
					sinceFeed += updateInterval;
					preg += updateInterval;
					if (preg > pregnancyDuration)
					{
						births++;
						creaturesNow++;
						pregnant = false;
						preg = -1d;
						love = 0;
					}

					continue;
				}

				if (ate || Hungry(sinceFeed, fedDuration))
				{
					continue;
				}

				sinceFeed += updateInterval;
				float roll = roll01 != null ? roll01() : 0f;
				if (roll <= pregnancyChance)
				{
					continue;
				}

				love++;
				if (love >= requiredLovePoints)
				{
					love = 0;
					pregnant = true;
					preg = 0d;
				}
			}

			double tail = gapSeconds - cursor;
			if (tail > 0d)
			{
				sinceFeed += tail;
				if (pregnant)
				{
					preg += tail;
					if (births < birthCap && preg > pregnancyDuration)
					{
						births++;
						pregnant = false;
						preg = -1d;
						love = 0;
					}
				}
			}

			return new Result(love, pregnant ? preg : -1d, births, eaten, sinceFeed);
		}

		private static bool CanLove(bool hasPartner, int creaturesNow, int maxCreatures)
		{
			return hasPartner && maxCreatures > 0 && creaturesNow < maxCreatures;
		}

		private static bool Hungry(double sinceFeed, float fedDuration)
		{
			if (fedDuration <= 0f)
			{
				return true;
			}

			return sinceFeed >= fedDuration;
		}

		private static bool TryEat(ref double sinceFeed, ref int eaten, int foodOnGround, float fedDuration)
		{
			if (fedDuration <= 0f || sinceFeed < fedDuration || eaten >= foodOnGround)
			{
				return false;
			}

			eaten++;
			sinceFeed = 0d;
			return true;
		}
	}
}
