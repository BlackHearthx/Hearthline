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

		public static string PlayerMessage(Outcome outcome, bool isYoung = true)
		{
			switch (outcome)
			{
				case Outcome.OkStolenCarry:
					return "You took the young";
				case Outcome.OkCarry:
					return isYoung ? "Carrying young" : "Carrying";
				case Outcome.RefuseClaimDisabled:
					return "Cannot steal this";
				case Outcome.RefuseTameFailed:
					return "Steal failed — not owner yet";
				case Outcome.RefuseWildAdult:
					return "Only tamed adults";
				case Outcome.RefuseStealCooldown:
					return "Steal cooldown";
				case Outcome.RefuseStealStamina:
					return "Too tired to steal";
				default:
					return string.Empty;
			}
		}
	}
}
