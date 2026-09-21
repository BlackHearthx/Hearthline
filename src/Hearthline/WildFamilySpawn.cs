using System.Collections;
using UnityEngine;

namespace BlackHearthx.Hearthline
{
	/// <summary>
	/// On first ownership of a wild adult with Procreation.m_offspring, optionally spawn one
	/// wild Growup cub nearby. Marked on ZDO so zone reloads do not re-roll.
	/// Confirmed: eggs use ItemDrop offspring; we only spawn live Growup prefabs.
	/// Console / nature spawn both hit Character.Awake — ownership may arrive a few frames later.
	/// </summary>
	internal static class WildFamilySpawn
	{
		private const float SettleSeconds = 0.35f;
		private const float OwnerWaitSeconds = 3f;

		internal static void TrySchedule(Character adult)
		{
			if (!Plugin.EnableMod.Value || !Plugin.WildHerds.Value || Plugin.WildFamilySpawnChance.Value <= 0f)
			{
				return;
			}

			if (adult == null || adult.IsPlayer() || adult.IsTamed())
			{
				return;
			}

			if (((Component)adult).GetComponent<Growup>() != null)
			{
				return;
			}

			Procreation procreation = ((Component)adult).GetComponent<Procreation>();
			if (procreation == null || procreation.m_offspring == null)
			{
				return;
			}

			ZNetView nview = ((Component)adult).GetComponent<ZNetView>();
			if (nview == null)
			{
				return;
			}

			((MonoBehaviour)adult).StartCoroutine(TryAfterSettle(adult, procreation, nview));
		}

		private static IEnumerator TryAfterSettle(Character adult, Procreation procreation, ZNetView nview)
		{
			yield return null;
			yield return new WaitForSeconds(SettleSeconds);

			float waited = 0f;
			while (adult != null && nview != null && waited < OwnerWaitSeconds)
			{
				if (nview.IsValid() && nview.IsOwner())
				{
					break;
				}

				waited += 0.1f;
				yield return new WaitForSeconds(0.1f);
			}

			if (adult == null || nview == null || !nview.IsValid() || !nview.IsOwner())
			{
				if (Plugin.DebugLogging.Value)
				{
					Plugin.Log.LogInfo("[Hearthline] wild family skip — never became ZDO owner (common on late join)");
				}

				yield break;
			}

			if (adult.IsTamed())
			{
				nview.GetZDO().Set(HearthlineZdo.FamilySpawnTried, 1);
				yield break;
			}

			if (nview.GetZDO().GetInt(HearthlineZdo.FamilySpawnTried, 0) != 0)
			{
				yield break;
			}

			// Always consume the roll so reloads do not keep trying.
			nview.GetZDO().Set(HearthlineZdo.FamilySpawnTried, 1);

			float roll = Random.Range(0f, 100f);
			bool ok = WildHerd.MaySpawnWildFamily(roll, Plugin.WildFamilySpawnChance.Value);
			if (Plugin.DebugLogging.Value)
			{
				string name = YardTables.StripClone(((Component)adult).gameObject.name);
				Plugin.Log.LogInfo(
					$"[Hearthline] wild family {name} roll={roll:0.#} need<={Plugin.WildFamilySpawnChance.Value:0.#} -> {(ok ? "yes" : "no")}");
			}

			if (!ok)
			{
				yield break;
			}

			SpawnCubBeside(adult, procreation);
		}

		private static void SpawnCubBeside(Character adult, Procreation procreation)
		{
			if (ZNetScene.instance == null || procreation.m_offspring == null)
			{
				return;
			}

			string prefabName = Utils.GetPrefabName(procreation.m_offspring);
			GameObject prefab = ZNetScene.instance.GetPrefab(prefabName);
			if (prefab == null)
			{
				return;
			}

			bool hasGrowup = prefab.GetComponent<Growup>() != null;
			bool hasItemDrop = prefab.GetComponent<ItemDrop>() != null;
			if (!WildHerd.IsLiveOffspringPrefab(hasGrowup, hasItemDrop))
			{
				if (Plugin.DebugLogging.Value)
				{
					Plugin.Log.LogInfo($"[Hearthline] wild family skip non-cub offspring {prefabName}");
				}

				return;
			}

			float angle = Random.Range(0f, Mathf.PI * 2f);
			Vector3 offset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * Random.Range(1.2f, 2.2f);
			Vector3 pos = adult.transform.position + offset;
			Quaternion rot = Quaternion.LookRotation(-adult.transform.forward, Vector3.up);

			GameObject cubGo = Object.Instantiate(prefab, pos, rot);
			Character cub = cubGo.GetComponent<Character>();
			if (cub != null)
			{
				cub.SetTamed(false);
				cub.SetLevel(adult.GetLevel());
			}

			if (Plugin.DebugLogging.Value)
			{
				Plugin.Log.LogInfo(
					$"[Hearthline] wild family spawned {prefabName} near {YardTables.StripClone(adult.gameObject.name)}");
			}
		}
	}
}
