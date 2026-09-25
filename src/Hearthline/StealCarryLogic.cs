namespace BlackHearthx.Hearthline
{
	/// <summary>
	/// Pure decision tree for steal/carry — same gates as CubCarry.TryPickup, testable without Unity.
	/// Young: wild steal+carry, or tamed carry.
	/// Adults: carry only when already tamed (never steal/tame-via-E).
	/// Steal path also gates cooldown + stamina.
	/// </summary>
	public static class StealCarryLogic
	{
		public enum Outcome
		{
			OkStolenCarry,
			OkCarry,
			RefuseCarryDisabled,
			RefuseNotEligible,
			RefuseClaimDisabled,
			RefuseTameFailed,
			RefuseAlreadyCarrying,
			RefuseWildAdult,
			RefuseStealCooldown,
			RefuseStealStamina
		}

		public static Outcome Evaluate(
			bool alreadyCarrying,
			bool carryEnabled,
			bool claimEnabled,
			bool isYoung,
			bool isTamedAdult,
			bool isTamed,
			bool tameSucceeded,
			bool stealOnCooldown = false,
			bool hasStealStamina = true)
		{
			if (alreadyCarrying)
			{
				return Outcome.RefuseAlreadyCarrying;
			}

			if (!carryEnabled)
			{
				return Outcome.RefuseCarryDisabled;
			}

			bool eligible = isYoung || isTamedAdult;
			if (!eligible)
			{
				return isTamed ? Outcome.RefuseNotEligible : Outcome.RefuseWildAdult;
			}

			if (!isTamed)
			{
				// Only young may be claimed; wild adults never.
				if (!isYoung)
				{
					return Outcome.RefuseWildAdult;
				}

				if (!claimEnabled)
				{
					return Outcome.RefuseClaimDisabled;
				}

				if (stealOnCooldown)
				{
					return Outcome.RefuseStealCooldown;
				}

				if (!hasStealStamina)
				{
					return Outcome.RefuseStealStamina;
				}

				if (!tameSucceeded)
				{
					return Outcome.RefuseTameFailed;
				}

				return Outcome.OkStolenCarry;
			}

			return Outcome.OkCarry;
		}

		public static string PlayerMessage(Outcome outcome, bool isYoung = true, string language = GameText.English)
		{
			switch (outcome)
			{
				case Outcome.OkStolenCarry:
					return GameText.Line("took_young", language);
				case Outcome.OkCarry:
					return GameText.Line(isYoung ? "carrying_young" : "carrying", language);
				case Outcome.RefuseClaimDisabled:
					return GameText.Line("cannot_steal", language);
				case Outcome.RefuseTameFailed:
					return GameText.Line("steal_failed", language);
				case Outcome.RefuseWildAdult:
					return GameText.Line("only_tamed", language);
				case Outcome.RefuseStealCooldown:
					return GameText.Line("steal_cooldown", language);
				case Outcome.RefuseStealStamina:
					return GameText.Line("too_tired", language);
				default:
					return string.Empty;
			}
		}
	}
}
