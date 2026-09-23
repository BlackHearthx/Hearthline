namespace BlackHearthx.Hearthline
{
	/// <summary>
	/// Vanilla Tameable.TamingUpdate only runs while the animal is loaded.
	/// This replays the missed fed time when the player comes back, one meal per fedDuration.
	/// </summary>
	internal static class AwayTame
	{
		internal readonly struct Result
		{
			internal Result(float timeLeft, int foodsEaten, double secondsSinceFeedAtEnd, bool finished)
			{
				TimeLeft = timeLeft;
				FoodsEaten = foodsEaten;
				SecondsSinceFeedAtEnd = secondsSinceFeedAtEnd;
				Finished = finished;
			}

			internal float TimeLeft { get; }
			internal int FoodsEaten { get; }
			internal double SecondsSinceFeedAtEnd { get; }
			internal bool Finished { get; }
		}

		internal static Result Simulate(
			double gapSeconds,
			float fedDuration,
			double secondsSinceFeedAtStart,
			float timeLeft,
			int foodOnGround)
		{
			if (timeLeft <= 0f)
			{
				return new Result(0f, 0, secondsSinceFeedAtStart, true);
			}

			if (gapSeconds <= 0d || fedDuration <= 0f)
			{
				return new Result(timeLeft, 0, secondsSinceFeedAtStart, false);
			}

			if (foodOnGround < 0)
			{
				foodOnGround = 0;
			}

			double cursor = 0d;
			double sinceFeed = secondsSinceFeedAtStart < 0d ? 0d : secondsSinceFeedAtStart;
			int eaten = 0;
			int guard = 0;
			while (cursor < gapSeconds - 0.0001d && timeLeft > 0f && guard++ < 100000)
			{
				if (sinceFeed >= fedDuration)
				{
					if (eaten >= foodOnGround)
					{
						break;
					}

					eaten++;
					sinceFeed = 0d;
					continue;
				}

				double step = gapSeconds - cursor;
				double fedLeft = fedDuration - sinceFeed;
				if (fedLeft < step)
				{
					step = fedLeft;
				}

				if (step <= 0d)
				{
					sinceFeed = fedDuration;
					continue;
				}

				timeLeft -= (float)step;
				if (timeLeft < 0f)
				{
					timeLeft = 0f;
				}

				cursor += step;
				sinceFeed += step;
			}

			return new Result(timeLeft, eaten, sinceFeed, timeLeft <= 0f);
		}
	}
}
