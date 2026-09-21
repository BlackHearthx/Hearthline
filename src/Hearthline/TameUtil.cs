using UnityEngine;

namespace BlackHearthx.Hearthline
{
	/// <summary>
	/// Vanilla cubs (Boar_piggy, Wolf_cub, Lox_Calf, Hatchling, …) use AnimalAI + Growup and have
	/// no Tameable / MonsterAI. Adults use MonsterAI.MakeTame. Both paths end in Character.SetTamed.
	/// Claim ownership first: RPC_SetTamed only applies on the owner, and IsTamed() for non-owners
	/// only refreshes from ZDO about once per second.
	/// </summary>
	internal static class TameUtil
	{
		internal static bool EnsureTamed(Character character, Tameable _ = null)
		{
			if (character == null)
			{
				return false;
			}

			if (character.IsTamed())
			{
				return true;
			}

			ZNetView nview = ((Component)character).GetComponent<ZNetView>();
			if (nview == null || !nview.IsValid())
			{
				return false;
			}

			if (!nview.IsOwner())
			{
				nview.ClaimOwnership();
			}

			MonsterAI monsterAi = ((Component)character).GetComponent<MonsterAI>();
			if (monsterAi != null)
			{
				monsterAi.MakeTame();
			}
			else
			{
				character.SetTamed(true);
			}

			if (character.IsTamed())
			{
				return true;
			}

			if (!nview.IsOwner())
			{
				return false;
			}

			nview.GetZDO().Set(ZDOVars.s_tamed, true);
			character.SetTamed(true);
			return character.IsTamed() || nview.GetZDO().GetBool(ZDOVars.s_tamed);
		}
	}
}
