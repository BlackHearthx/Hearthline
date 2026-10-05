using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx.Bootstrap;
using HarmonyLib;
using UnityEngine;

namespace BlackHearthx.Hearthline
{
    // Uses vanilla components and assets; no dependency on TameableHares.
    internal static class HareLivestock
    {
        internal const string KitName = "Hearthline_HareKit";
        internal const string LegacyKitName = "nick008_HareKit";
        private static GameObject root;

        internal static void Configure(ZNetScene scene)
        {
            GameObject hare = scene.m_prefabs.Find(p => p != null && p.name == "Hare");
            GameObject boar = scene.m_prefabs.Find(p => p != null && p.name == "Boar");
            if (hare == null || boar == null) return;
            MonsterAI boarAI = boar.GetComponent<MonsterAI>();
            Tameable boarTame = boar.GetComponent<Tameable>();
            if (boarAI == null || boarTame == null)
            {
                Plugin.Log.LogWarning("Hare setup skipped: vanilla Boar AI or taming template is missing.");
                return;
            }
            // Register kits even when the feature is disabled so existing saves remain loadable.
            if (root == null)
            {
                root = new GameObject("Hearthline hare prefabs");
                root.transform.SetParent(Plugin.Instance.transform, false);
                root.SetActive(false);
            }
            RegisterKit(scene, hare, KitName);
            RegisterKit(scene, hare, LegacyKitName);
            if (Chainloader.PluginInfos.ContainsKey("nick008.tameablehares"))
            {
                Plugin.Log.LogInfo("TameableHares detected: it owns hare prefabs; Hearthline supplies breeding and yard features.");
                return;
            }
            if (!Plugin.EnableMod.Value || !Plugin.EnableHares.Value) return;
            if (hare.GetComponent<Procreation>() != null && hare.GetComponent<HearthlineHareAI>() == null)
            {
                Plugin.Log.LogWarning("Another mod already configures Hare breeding; leaving its adult prefab intact.");
                return;
            }
            AnimalAI animal = hare.GetComponent<AnimalAI>();
            Character character = hare.GetComponent<Character>();
            if (animal == null || character == null) return;
            Humanoid humanoid = hare.AddComponent<Humanoid>();
            CopyFields(character, humanoid, typeof(Character));
            if (humanoid.m_eye == null && hare.transform.childCount > 0) humanoid.m_eye = hare.transform.GetChild(0);
            humanoid.m_group = "Hare";
            UnityEngine.Object.DestroyImmediate(character);
            HearthlineHareAI ai = hare.AddComponent<HearthlineHareAI>();
            CopyFields(boarAI, ai, typeof(MonsterAI));
            CopyFields(animal, ai, typeof(BaseAI));
            UnityEngine.Object.DestroyImmediate(animal);
            ai.m_enableHuntPlayer = false;
            ai.m_consumeRange = 1.4f;
            ai.m_consumeSearchRange = 15f;
            ai.m_consumeSearchInterval = 10f;
            ai.m_consumeItems = new List<ItemDrop>();
            Tameable tame = hare.AddComponent<Tameable>();
            tame.m_sootheEffect = boarTame.m_sootheEffect;
            tame.m_tamedEffect = boarTame.m_tamedEffect;
            tame.m_petEffect = Sounds(scene, "sfx_hare_idle");
            tame.m_commandable = true;
            tame.m_tamingTime = Plugin.HareTamingSeconds.Value;
            tame.m_fedDuration = Plugin.HareFedSeconds.Value;
            Procreation breed = hare.AddComponent<Procreation>();
            breed.m_offspring = scene.m_prefabs.Find(p => p.name == KitName);
            breed.m_maxCreatures = Plugin.HarePenLimit.Value;
            breed.m_pregnancyDuration = Plugin.HarePregnancySeconds.Value;
            breed.m_pregnancyChance = 1f - Plugin.HarePregnancyChance.Value / 100f;
            breed.m_updateInterval = 20f;
            breed.m_partnerCheckRange = 4f;
            breed.m_totalCheckRange = 10f;
            breed.m_minOffspringLevel = 1;
            breed.m_loveEffects = Sounds(scene, "sfx_hare_idle");
            breed.m_birthEffects = Sounds(scene, "sfx_hare_alerted");
            PopulateFoods(ai);
            Plugin.Log.LogInfo("Hearthline configured tameable and breedable hares.");
        }

        private static void RegisterKit(ZNetScene scene, GameObject hare, string name)
        {
            if (scene.m_prefabs.Exists(p => p != null && p.name == name)) return;
            GameObject kit = UnityEngine.Object.Instantiate(hare, root.transform);
            kit.name = name;
            kit.transform.localScale *= 0.55f;
            foreach (Component component in new Component[] { kit.GetComponent<CharacterDrop>(), kit.GetComponent<Tameable>(), kit.GetComponent<Procreation>() })
                if (component != null) UnityEngine.Object.DestroyImmediate(component);
            // Kits cloned from another mod's adult must not eat or run adult breeding AI.
            MonsterAI adultAI = kit.GetComponent<MonsterAI>();
            if (adultAI != null)
            {
                AnimalAI youngAI = kit.AddComponent<AnimalAI>();
                CopyFields(adultAI, youngAI, typeof(BaseAI));
                UnityEngine.Object.DestroyImmediate(adultAI);
            }
            Growup grow = kit.GetComponent<Growup>() ?? kit.AddComponent<Growup>();
            grow.m_grownPrefab = hare;
            grow.m_growTime = Plugin.HareGrowthSeconds.Value;
            grow.m_inheritTame = true;
            scene.m_prefabs.Add(kit);
        }

        private static void CopyFields(Component source, Component target, Type type)
        {
            foreach (FieldInfo field in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
                if (!field.IsInitOnly && !typeof(Delegate).IsAssignableFrom(field.FieldType)) field.SetValue(target, field.GetValue(source));
        }

        private static EffectList Sounds(ZNetScene scene, string name)
        {
            GameObject prefab = scene.m_prefabs.Find(p => p != null && p.name == name);
            return new EffectList { m_effectPrefabs = prefab == null ? Array.Empty<EffectList.EffectData>() : new[] { new EffectList.EffectData { m_prefab = prefab, m_enabled = true } } };
        }

        internal static void PopulateFoods(HearthlineHareAI ai)
        {
            if (ObjectDB.instance == null) return;
            // Unity clones public lists by reference; keep each animal's meal list independent.
            var foods = new List<ItemDrop>();
            foreach (string food in Plugin.HareFoods.Value.Split(','))
            {
                string id = food.Trim();
                if (id.Length == 0) continue;
                GameObject prefab = ObjectDB.instance.GetItemPrefab(id);
                ItemDrop drop = prefab != null ? prefab.GetComponent<ItemDrop>() : null;
                if (drop != null && !foods.Contains(drop)) foods.Add(drop);
            }
            ai.m_consumeItems = foods;
        }

        internal static void ApplySettings(GameObject hare)
        {
            Tameable tame = hare.GetComponent<Tameable>();
            Procreation breed = hare.GetComponent<Procreation>();
            if (tame != null)
            {
                tame.m_tamingTime = Plugin.HareTamingSeconds.Value;
                tame.m_fedDuration = Plugin.HareFedSeconds.Value;
            }
            if (breed != null)
            {
                breed.m_maxCreatures = Plugin.HarePenLimit.Value;
                breed.m_pregnancyDuration = Plugin.HarePregnancySeconds.Value;
                breed.m_pregnancyChance = 1f - Plugin.HarePregnancyChance.Value / 100f;
            }
        }
    }

    public sealed class HearthlineHareAI : MonsterAI
    {
        private static readonly Func<BaseAI, float, bool> TickBase = AccessTools.MethodDelegate<Func<BaseAI, float, bool>>(AccessTools.Method(typeof(BaseAI), "UpdateAI"), virtualCall: false);
        private static readonly Func<MonsterAI, Humanoid, float, bool> Eat = AccessTools.MethodDelegate<Func<MonsterAI, Humanoid, float, bool>>(AccessTools.Method(typeof(MonsterAI), "UpdateConsumeItem"));
        private float nextFoodRefresh;
        private float nextThreatCheck;
        private float calmAt;
        private Character threat;

        protected override void Awake()
        {
            base.Awake();
            HareLivestock.PopulateFoods(this);
            HareLivestock.ApplySettings(gameObject);
        }

        public override bool UpdateAI(float dt)
        {
            if (!TickBase(this, dt)) return false;
            if (Time.time >= nextFoodRefresh)
            {
                HareLivestock.PopulateFoods(this);
                HareLivestock.ApplySettings(gameObject);
                nextFoodRefresh = Time.time + 10f;
            }
            if (Time.time >= nextThreatCheck)
            {
                threat = FindEnemy();
                nextThreatCheck = Time.time + 2f;
                if (threat != null) calmAt = Time.time + 4f;
            }
            if (threat != null && !threat.IsDead())
            {
                SetAlerted(true);
                Flee(dt, threat.transform.position);
                return true;
            }
            if (Time.time >= calmAt) SetAlerted(false);
            if (m_afraidOfFire && AvoidFire(dt, null, true)) return true;
            if (!IsAlerted() && Eat(this, (Humanoid)m_character, dt)) return true;
            GameObject follow = GetFollowTarget();
            if (follow != null) Follow(follow, dt);
            else IdleMovement(dt);
            return true;
        }
    }

    [HarmonyPatch(typeof(ZNetScene), "Awake")]
    internal static class HarePrefabPatch
    {
        [HarmonyPrefix, HarmonyPriority(Priority.Last)]
        private static void Prefix(ZNetScene __instance) => HareLivestock.Configure(__instance);
    }

    [HarmonyPatch(typeof(Character), "RPC_Damage")]
    internal static class HareKitProtectionPatch
    {
        [HarmonyPrefix]
        private static bool Prefix(Character __instance, HitData hit)
        {
            string name = YardTables.StripClone(__instance.gameObject.name);
            if (name != HareLivestock.KitName && name != HareLivestock.LegacyKitName) return true;
            Humanoid attacker = hit.GetAttacker() as Humanoid;
            ItemDrop.ItemData weapon = attacker != null ? attacker.GetCurrentWeapon() : null;
            return weapon == null || (weapon.m_dropPrefab != null ? weapon.m_dropPrefab.name != "KnifeButcher" : weapon.m_shared.m_name != "$item_knife_butcher");
        }
    }

    // Count both kit IDs toward the same pen limit during migration.
    [HarmonyPatch(typeof(SpawnSystem), nameof(SpawnSystem.GetNrOfInstances), new[] { typeof(GameObject), typeof(Vector3), typeof(float), typeof(bool), typeof(bool) })]
    internal static class HareKitCountPatch
    {
        [ThreadStatic] private static bool countingAlias;
        [HarmonyPostfix]
        private static void Postfix(GameObject prefab, Vector3 center, float maxRange, bool eventCreaturesOnly, bool procreationOnly, ref int __result)
        {
            if (countingAlias || prefab == null || ZNetScene.instance == null) return;
            string alias = prefab.name == HareLivestock.KitName ? HareLivestock.LegacyKitName : prefab.name == HareLivestock.LegacyKitName ? HareLivestock.KitName : null;
            if (alias == null) return;
            GameObject other = ZNetScene.instance.GetPrefab(alias);
            if (other == null) return;
            countingAlias = true;
            try { __result += SpawnSystem.GetNrOfInstances(other, center, maxRange, eventCreaturesOnly, procreationOnly); }
            finally { countingAlias = false; }
        }
    }

    [HarmonyPatch(typeof(Growup), "GrowUpdate")]
    internal static class HareKitGrowthPatch
    {
        [HarmonyPrefix]
        private static void Prefix(Growup __instance)
        {
            string name = YardTables.StripClone(__instance.gameObject.name);
            if (name == HareLivestock.KitName || (name == HareLivestock.LegacyKitName && !Chainloader.PluginInfos.ContainsKey("nick008.tameablehares")))
                __instance.m_growTime = Plugin.HareGrowthSeconds.Value;
        }
    }
}
