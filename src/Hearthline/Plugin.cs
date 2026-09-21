using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using ServerSync;
using UnityEngine;

namespace BlackHearthx.Hearthline
{
	[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
	public class Plugin : BaseUnityPlugin
	{
		public const string PluginGuid = "blackhearthx.hearthline";
		public const string PluginName = "Hearthline";
		public const string PluginVersion = "1.0.1";

		internal const string CllcGuid = "org.bepinex.plugins.creaturelevelcontrol";

		internal static Plugin Instance { get; private set; }
		internal static ManualLogSource Log { get; private set; }

		private static readonly ConfigSync ConfigSync = new ConfigSync(PluginGuid)
		{
			DisplayName = PluginName,
			CurrentVersion = PluginVersion,
			MinimumRequiredVersion = PluginVersion
		};

		private Harmony _harmony;

		internal static ConfigEntry<bool> LockConfig;
		internal static ConfigEntry<bool> EnableMod;
		internal static ConfigEntry<float> UpgradeChancePercent;
		internal static ConfigEntry<int> MaxStarLevel;
		internal static ConfigEntry<bool> DebugLogging;
		internal static ConfigEntry<bool> DebugFastBreeding;
		internal static ConfigEntry<bool> WildHerds;
		internal static ConfigEntry<bool> WildHerdsIgnoreHunger;
		internal static ConfigEntry<float> WildHerdPregnancyChance;
		internal static ConfigEntry<float> WildFamilySpawnChance;
		internal static ConfigEntry<string> ChanceOverrides;
		internal static ConfigEntry<string> FavoriteFoods;
		internal static ConfigEntry<float> FavoriteUpgradeChance;
		internal static ConfigEntry<bool> ShowYardHover;
		internal static ConfigEntry<bool> EnableMateDraw;
		internal static ConfigEntry<float> MateDrawRange;
		internal static ConfigEntry<float> MateDrawSpeed;
		internal static ConfigEntry<float> MateDrawInterval;
		internal static ConfigEntry<float> MateDrawPlayerRange;
		internal static ConfigEntry<bool> ClaimWildYoung;
		internal static ConfigEntry<bool> EnableCubCarry;
		internal static ConfigEntry<float> CubCarryRange;
		internal static ConfigEntry<bool> StealAlertsHerd;
		internal static ConfigEntry<float> StealAlertRange;
		internal static ConfigEntry<float> StealCooldownSeconds;
		internal static ConfigEntry<float> StealStaminaCost;
		internal static ConfigEntry<bool> DropCarryOnDamage;
		internal static ConfigEntry<bool> BlockAttackWhileCarrying;

		private void Awake()
		{
			Instance = this;
			Log = Logger;

			LockConfig = Config.Bind(
				"1. General",
				"Lock Configuration",
				true,
				"If enabled, configuration is locked and can only be changed by the server.");
			ConfigSync.AddLockingConfigEntry(LockConfig);

			EnableMod = SyncedConfig("1. General", "Enable Mod", true, "Master toggle.");
			UpgradeChancePercent = SyncedConfig(
				"2. Breeding",
				"Upgrade Chance",
				5f,
				new ConfigDescription(
					"Percentage chance that a birth or egg is one star above the parent. Vanilla inheritance is otherwise unchanged.",
					new AcceptableValueRange<float>(0f, 100f)));
			MaxStarLevel = SyncedConfig(
				"2. Breeding",
				"Max Star Level",
				2,
				new ConfigDescription(
					"Highest star count reachable through breeding. Vanilla display and stats stop at 2 stars (level 3). Values above 2 need a creature-level mod such as CLLC.",
					new AcceptableValueRange<int>(0, 10)));
			DebugLogging = Config.Bind(
				"3. Debug",
				"Enable Debug Logging",
				false,
				"Verbose BepInEx log. Not synced.");
			DebugFastBreeding = Config.Bind(
				"3. Debug",
				"Debug Fast Breeding",
				false,
				"TEST ONLY: love need 1, almost always succeeds, pregnancy ~5s, partner check ~8m, Procreate every 2s. Turn off for real play.");
			WildHerds = SyncedConfig(
				"4. Wild herds",
				"Wild Herds Can Procreate",
				true,
				"If enabled, untamed adults that already have vanilla Procreation.m_offspring may run vanilla Procreate. Cub is SetTamed(parent) so wild parents yield wild babies. Still uses partner range, alert check, love points, pregnancy, and m_maxCreatures.");
			WildHerdsIgnoreHunger = SyncedConfig(
				"4. Wild herds",
				"Wild Herds Ignore Hunger",
				true,
				"Hunger is ignored only during Procreate for untamed adults with m_offspring. Taming still requires food.");
			WildHerdPregnancyChance = SyncedConfig(
				"4. Wild herds",
				"Wild Herd Pregnancy Chance",
				30f,
				new ConfigDescription(
					"Extra roll (0-100) when a wild adult would MakePregnant. Keeps herds from exploding. Tamed breeding is unchanged.",
					new AcceptableValueRange<float>(0f, 100f)));
			WildFamilySpawnChance = SyncedConfig(
				"4. Wild herds",
				"Wild Family Spawn Chance",
				25f,
				new ConfigDescription(
					"When a wild adult with vanilla offspring first appears (nature or spawn), chance (0-100) to already have one wild cub nearby. Eggs are skipped. Rolled once per creature ZDO.",
					new AcceptableValueRange<float>(0f, 100f)));
			ChanceOverrides = SyncedConfig(
				"2. Breeding",
				"Chance Per Prefab",
				"",
				"Optional overrides as Prefab:percent, comma-separated. Example: Boar:8,Wolf:5. Blank uses Upgrade Chance.");
			FavoriteFoods = SyncedConfig(
				"5. Favorite meals",
				"Favorite Foods",
				"Boar:Carrot;Wolf:RawMeat;Lox:Barley;Hen:Barley;Asksvin:Vineberry;Moose:Lingonberry",
				"CreaturePrefab:Item,Item;... Items must already be on that creature's vanilla consume list.");
			FavoriteUpgradeChance = SyncedConfig(
				"5. Favorite meals",
				"Favorite Upgrade Chance",
				25f,
				new ConfigDescription(
					"Star-upgrade chance while the last meal was a listed favorite and the creature is still fed.",
					new AcceptableValueRange<float>(0f, 100f)));
			ShowYardHover = SyncedConfig(
				"6. Hover",
				"Show Yard Hover",
				true,
				"Append bond, expecting countdown (ZNet clock), pen-full warning, cub growth, favorite-meal note. No pregnancy percent.");
			EnableMateDraw = SyncedConfig(
				"9. Mate draw",
				"Enable Mate Draw",
				true,
				"Softly nudge fed, calm, tamed adults toward a same-species partner so Bond ticks sooner. Stops when close, hungry, alert, pregnant, or pen is full.");
			MateDrawRange = SyncedConfig(
				"9. Mate draw",
				"Mate Draw Range",
				10f,
				new ConfigDescription(
					"How far a tamed adult looks for a partner to walk toward.",
					new AcceptableValueRange<float>(3f, 30f)));
			MateDrawSpeed = SyncedConfig(
				"9. Mate draw",
				"Mate Draw Speed",
				1.4f,
				new ConfigDescription(
					"Walk speed (m/s) while being drawn to a mate.",
					new AcceptableValueRange<float>(0.2f, 4f)));
			MateDrawInterval = SyncedConfig(
				"9. Mate draw",
				"Mate Draw Interval",
				0.5f,
				new ConfigDescription(
					"Seconds between mate-draw scans.",
					new AcceptableValueRange<float>(0.2f, 3f)));
			MateDrawPlayerRange = SyncedConfig(
				"9. Mate draw",
				"Mate Draw Player Range",
				40f,
				new ConfigDescription(
					"Only animals within this distance of the local player are nudged (keeps distant pens quiet).",
					new AcceptableValueRange<float>(10f, 80f)));
			ClaimWildYoung = SyncedConfig(
				"7. Steal young",
				"Claim Wild Young",
				true,
				"Press Z on a wild young animal to tame and carry it. E stays vanilla pet. Adults still tame with food.");
			EnableCubCarry = SyncedConfig(
				"8. Carry young",
				"Enable Cub Carry",
				true,
				"Z picks up / puts down young (steal if wild) and tamed adults. E stays vanilla pet. Wild adults stay untouchable.");
			CubCarryRange = SyncedConfig(
				"8. Carry young",
				"Carry Range",
				5f,
				new ConfigDescription(
					"Look distance used when putting a cub down.",
					new AcceptableValueRange<float>(1f, 12f)));
			StealAlertsHerd = SyncedConfig(
				"7. Steal young",
				"Steal Alerts Herd",
				true,
				"When you steal a wild cub, nearby wild adults of the same species aggro and hunt you (vanilla Alert + hunt + Theif aggravate).");
			StealAlertRange = SyncedConfig(
				"7. Steal young",
				"Steal Alert Range",
				20f,
				new ConfigDescription(
					"Meters around the stolen cub to wake guardian adults.",
					new AcceptableValueRange<float>(5f, 60f)));
			StealCooldownSeconds = SyncedConfig(
				"7. Steal young",
				"Steal Cooldown Seconds",
				120f,
				new ConfigDescription(
					"Seconds after a successful wild steal before another steal is allowed. Carry of already-tamed animals is unaffected. 0 = no cooldown.",
					new AcceptableValueRange<float>(0f, 600f)));
			StealStaminaCost = SyncedConfig(
				"7. Steal young",
				"Steal Stamina Cost",
				40f,
				new ConfigDescription(
					"Stamina spent on a successful wild steal. 0 = free.",
					new AcceptableValueRange<float>(0f, 100f)));
			DropCarryOnDamage = SyncedConfig(
				"8. Carry young",
				"Drop On Damage",
				true,
				"If you take damage while carrying, the animal is dropped (anti combat-cheese).");
			BlockAttackWhileCarrying = SyncedConfig(
				"8. Carry young",
				"Block Attack While Carrying",
				true,
				"Cannot swing weapons or draw a bow while carrying. Drop the animal first.");

			_harmony = new Harmony(PluginGuid);
			_harmony.PatchAll(typeof(Patches.ProcreationPatch));
			_harmony.PatchAll(typeof(Patches.FastBreedAwakePatch));
			_harmony.PatchAll(typeof(Patches.FastBreedProcreatePatch));
			_harmony.PatchAll(typeof(Patches.ReadyForProcreationPatch));
			_harmony.PatchAll(typeof(Patches.TameableIsHungryPatch));
			_harmony.PatchAll(typeof(Patches.MakePregnantPatch));
			_harmony.PatchAll(typeof(Patches.CharacterWildFamilyPatch));
			_harmony.PatchAll(typeof(Patches.TameableConsumedPatch));
			_harmony.PatchAll(typeof(Patches.CharacterHoverPatch));
			_harmony.PatchAll(typeof(Patches.PlayerFindHoverYoungPatch));
			_harmony.PatchAll(typeof(Patches.TameableStealHoverPatch));
			_harmony.PatchAll(typeof(Patches.CharacterYoungHoverPatch));
			_harmony.PatchAll(typeof(Patches.CarrierDamageDropPatch));
			_harmony.PatchAll(typeof(Patches.CarryBlocksAttackPatch));

			Log.LogInfo($"{PluginName} {PluginVersion} loaded. Z steals/carries; E stays vanilla pet.");
			if (MaxStarLevel.Value > 2 && !IsCllcPresent())
			{
				Log.LogWarning("Max Star Level is above 2 and CLLC was not detected. Offspring will cap at vanilla 2 stars.");
			}
		}

		private void Update()
		{
			CubCarry.Tick();
			MateDraw.Tick();
		}

		private void OnDestroy()
		{
			if (CubCarry.IsCarrying)
			{
				CubCarry.ReleaseAt(Vector3.zero, force: true);
			}

			_harmony?.UnpatchSelf();
		}

		internal static bool IsCllcPresent()
		{
			return Chainloader.PluginInfos != null && Chainloader.PluginInfos.ContainsKey(CllcGuid);
		}

		internal static bool RollUpgrade()
		{
			return Random.Range(0f, 100f) <= UpgradeChancePercent.Value;
		}

		internal static bool RollUpgrade(float chancePercent)
		{
			return Random.Range(0f, 100f) <= chancePercent;
		}

		internal static float UpgradeChanceFor(Procreation procreation)
		{
			string prefab = YardTables.StripClone(((Component)procreation).gameObject.name);
			float baseline = YardTables.ChanceForPrefab(prefab, YardTables.ParseChances(ChanceOverrides.Value), UpgradeChancePercent.Value);
			return YardTables.RollChance(baseline, HasFavoriteMeal(procreation), FavoriteUpgradeChance.Value);
		}

		internal static bool HasFavoriteMeal(Procreation procreation)
		{
			ZNetView nview = ((Component)procreation).GetComponent<ZNetView>();
			if (nview == null || !nview.IsValid() || nview.GetZDO().GetInt(HearthlineZdo.FavoriteMeal, 0) != 1)
			{
				return false;
			}

			Tameable tameable = ((Component)procreation).GetComponent<Tameable>();
			return tameable != null && !tameable.IsHungry();
		}

		internal static bool IsFavoriteFood(string creaturePrefab, string itemPrefab, ItemDrop drop)
		{
			System.Collections.Generic.Dictionary<string, System.Collections.Generic.HashSet<string>> map = YardTables.ParseFavorites(FavoriteFoods.Value);
			if (YardTables.IsListedFavorite(creaturePrefab, itemPrefab, map))
			{
				return true;
			}

			if (drop == null || drop.m_itemData?.m_shared == null || ZNetScene.instance == null)
			{
				return false;
			}

			if (!map.TryGetValue(creaturePrefab, out System.Collections.Generic.HashSet<string> items))
			{
				return false;
			}

			foreach (string id in items)
			{
				GameObject prefab = ZNetScene.instance.GetPrefab(id);
				ItemDrop listed = prefab != null ? prefab.GetComponent<ItemDrop>() : null;
				if (listed?.m_itemData?.m_shared != null && listed.m_itemData.m_shared.m_name == drop.m_itemData.m_shared.m_name)
				{
					return true;
				}
			}

			return false;
		}

		private ConfigEntry<T> SyncedConfig<T>(string group, string name, T value, ConfigDescription description)
		{
			ConfigEntry<T> entry = Config.Bind(group, name, value, description);
			ConfigSync.AddConfigEntry(entry).SynchronizedConfig = true;
			return entry;
		}

		private ConfigEntry<T> SyncedConfig<T>(string group, string name, T value, string description)
		{
			return SyncedConfig(group, name, value, new ConfigDescription(description));
		}
	}
}
