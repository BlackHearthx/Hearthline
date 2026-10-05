using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEditor;

public static class HareAudit
{
    static readonly string Workspace = Environment.GetEnvironmentVariable("HEARTHLINE_AUDIT_WORKSPACE") ?? @"D:\Valheim Mods";
    static readonly string Install = Environment.GetEnvironmentVariable("VALHEIM_INSTALL") ?? @"C:\Program Files (x86)\Steam\steamapps\common\Valheim";
    static int checks;
    static Assembly mod, game, bepin;
    static readonly BindingFlags Any = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;
    static Type Game(string name) => game.GetType(name, true);
    static Type Mod(string name) => mod.GetType("BlackHearthx.Hearthline." + name, true);
    static Component Add(GameObject obj, string name) => obj.AddComponent(Game(name));
    static void Assert(bool ok, string text) { if (!ok) throw new Exception(text); checks++; Debug.Log("AUDIT PASS: " + text); }
    public static void Run()
    {
        try
        {
            AppDomain.CurrentDomain.AssemblyResolve += (sender, args) => {
                string file = new AssemblyName(args.Name).Name + ".dll";
                foreach (string folder in new[] { Install + @"\BepInEx\core", Install + @"\valheim_Data\Managed", Workspace + @"\Hearthline\bin\Hearthline" })
                { string path = Path.Combine(folder, file); if (File.Exists(path)) return Assembly.LoadFrom(path); }
                return null;
            };
            game = Assembly.LoadFrom(Install + @"\valheim_Data\Managed\assembly_valheim.dll");
            bepin = Assembly.LoadFrom(Install + @"\BepInEx\core\BepInEx.dll");
            mod = Assembly.LoadFrom(Workspace + @"\Hearthline\bin\Hearthline\BlackHearthx.Hearthline.dll");
            RuntimeHelpers.RunClassConstructor(Mod("HearthlineHareAI").TypeHandle);
            Assert(true, "AI delegates resolve against installed Valheim in Unity Mono");
            bepin.GetType("BepInEx.Paths").GetMethod("SetExecutablePath", Any).Invoke(null, new object[] { Workspace + @"\_checks\HearthlineAuditUnity\valheim.exe", Workspace + @"\_checks\HearthlineAuditUnity\BepInEx", Install + @"\valheim_Data\Managed", null });
            var pluginRoot = new GameObject("Audit plugin"); pluginRoot.SetActive(false);
            Type threading = bepin.GetType("BepInEx.ThreadingHelper");
            threading.GetField("<Instance>k__BackingField", Any).SetValue(null, pluginRoot.AddComponent(threading));
            Component plugin = pluginRoot.AddComponent(Mod("Plugin"));
            Mod("Plugin").GetField("<Instance>k__BackingField", Any).SetValue(null, plugin);
            var log = Activator.CreateInstance(bepin.GetType("BepInEx.Logging.ManualLogSource"), "HareAudit");
            Mod("Plugin").GetField("<Log>k__BackingField", Any).SetValue(null, log);
            object config = Activator.CreateInstance(bepin.GetType("BepInEx.Configuration.ConfigFile"), new object[] { Workspace + @"\_checks\HearthlineAuditUnity\audit.cfg", false, null });
            MethodInfo bind = config.GetType().GetMethods().Single(m => m.Name == "Bind" && m.IsGenericMethod && m.GetParameters().Length == 4 && m.GetParameters()[3].ParameterType == typeof(string));
            Action<string, object> set = (name, value) => { object entry = bind.MakeGenericMethod(value.GetType()).Invoke(config, new[] { (object)"Audit", name, value, "" }); entry.GetType().GetProperty("Value").SetValue(entry, value); Mod("Plugin").GetField(name, Any).SetValue(null, entry); };
            set("EnableMod", true); set("EnableHares", true); set("HareTamingSeconds", 1500f); set("HareFedSeconds", 300f); set("HareGrowthSeconds", 2000f); set("HarePregnancySeconds", 90f); set("HarePregnancyChance", 66f); set("HarePenLimit", 7); set("HareFoods", "Carrot,Turnip,MushroomJotunPuffs,Apple");
            var sceneObject = new GameObject("Audit scene"); sceneObject.SetActive(false); Component scene = Add(sceneObject, "ZNetScene");
            var hare = new GameObject("Hare"); hare.SetActive(false); var eye = new GameObject("Eye"); eye.transform.SetParent(hare.transform);
            Component original = Add(hare, "Character"); Game("Character").GetField("m_eye").SetValue(original, eye.transform); Game("Character").GetField("m_health").SetValue(original, 10f); Add(hare, "AnimalAI"); Add(hare, "CharacterDrop");
            var boar = new GameObject("Boar"); boar.SetActive(false); Add(boar, "Humanoid"); Add(boar, "MonsterAI"); Add(boar, "Tameable");
            object prefabs = Game("ZNetScene").GetField("m_prefabs").GetValue(scene);
            prefabs.GetType().GetMethod("Add").Invoke(prefabs, new object[] { hare }); prefabs.GetType().GetMethod("Add").Invoke(prefabs, new object[] { boar });
            Mod("HareLivestock").GetMethod("Configure", Any).Invoke(null, new object[] { scene });
            Assert(hare.GetComponents(Game("Character")).Length == 1 && hare.GetComponent(Game("Humanoid")) != null, "Adult has exactly one Character, converted to Humanoid");
            Assert(hare.GetComponents(Game("BaseAI")).Length == 1 && hare.GetComponent(Mod("HearthlineHareAI")) != null, "Adult has exactly one AI, the hare AI");
            Component adult = hare.GetComponent(Game("Character")); Assert((float)Game("Character").GetField("m_health").GetValue(adult) == 10f, "Adult preserves original health"); Assert((Transform)Game("Character").GetField("m_eye").GetValue(adult) == eye.transform, "Adult eye remains attached to original model");
            Component breeding = hare.GetComponent(Game("Procreation")); GameObject kit = (GameObject)Game("Procreation").GetField("m_offspring").GetValue(breeding);
            Assert(kit.name == "Hearthline_HareKit", "Adult offspring points to registered kit"); Assert(kit.GetComponent(Game("Growup")) != null && kit.GetComponent(Game("AnimalAI")) != null, "Kit has growth and prey AI"); Assert(kit.GetComponent(Game("Tameable")) == null && kit.GetComponent(Game("Procreation")) == null && kit.GetComponent(Game("CharacterDrop")) == null, "Kit cannot tame, breed or drop adult loot");
            Assert((GameObject)Game("Growup").GetField("m_grownPrefab").GetValue(kit.GetComponent(Game("Growup"))) == hare, "Kit grows into the converted adult");
            Assert(Mathf.Approximately((float)Game("Procreation").GetField("m_pregnancyChance").GetValue(breeding), .34f), "66 percent attempt chance correctly inverts vanilla threshold");
            int before = (int)prefabs.GetType().GetProperty("Count").GetValue(prefabs); Mod("HareLivestock").GetMethod("Configure", Any).Invoke(null, new object[] { scene }); Assert((int)prefabs.GetType().GetProperty("Count").GetValue(prefabs) == before, "Repeated setup does not duplicate kit registrations");
            Assert(boar.GetComponent(Game("Humanoid")) != null && boar.GetComponent(Game("MonsterAI")) != null && boar.GetComponent(Game("AnimalAI")) == null, "Boar template remains intact");
            var dbObject = new GameObject("Audit items"); dbObject.SetActive(false); Component db = Add(dbObject, "ObjectDB"); Game("ObjectDB").GetField("m_instance", Any).SetValue(null, db);
            var items = (System.Collections.IList)Game("ObjectDB").GetField("m_items").GetValue(db);
            foreach (string food in new[] { "Carrot", "Turnip" }) { var item = new GameObject(food); item.SetActive(false); Component drop = Add(item, "ItemDrop"); object data = Game("ItemDrop").GetField("m_itemData").GetValue(drop); data.GetType().GetField("m_shared").SetValue(data, Activator.CreateInstance(Game("ItemDrop+ItemData+SharedData"))); items.Add(item); }
            Game("ObjectDB").GetMethod("UpdateRegisters", Any).Invoke(db, null);
            set("HareFoods", "Carrot,Turnip,Apple,,Carrot"); Component hareAI = hare.GetComponent(Mod("HearthlineHareAI")); Mod("HareLivestock").GetMethod("PopulateFoods", Any).Invoke(null, new object[] { hareAI });
            var firstFoods = (System.Collections.IList)Game("MonsterAI").GetField("m_consumeItems").GetValue(hareAI); Assert(firstFoods.Count == 2, "Food lookup ignores blanks, duplicates and absent optional Apple");
            Mod("HareLivestock").GetMethod("PopulateFoods", Any).Invoke(null, new object[] { hareAI }); Assert(!ReferenceEquals(firstFoods, Game("MonsterAI").GetField("m_consumeItems").GetValue(hareAI)), "Food refresh isolates cloned animal lists");
            set("HareGrowthSeconds", 1234f); Mod("HareKitGrowthPatch").GetMethod("Prefix", Any).Invoke(null, new object[] { kit.GetComponent(Game("Growup")) }); Assert((float)Game("Growup").GetField("m_growTime").GetValue(kit.GetComponent(Game("Growup"))) == 1234f, "Kit growth observes synchronized setting updates");
            var allPrefabs = ((System.Collections.IEnumerable)prefabs).Cast<GameObject>().ToList(); GameObject legacy = allPrefabs.Single(p => p.name == "nick008_HareKit");
            Assert(legacy.GetComponent(Game("Growup")) != null, "Legacy kit ID remains registered and grows");
            Game("ZNetScene").GetField("s_instance", Any).SetValue(null, scene); var named = (System.Collections.IDictionary)Game("ZNetScene").GetField("m_namedPrefabs", Any).GetValue(scene);
            MethodInfo hash = Assembly.LoadFrom(Install + @"\valheim_Data\Managed\assembly_utils.dll").GetType("StringExtensionMethods").GetMethod("GetStableHashCode", new[] { typeof(string) }); foreach (GameObject p in allPrefabs) named[hash.Invoke(null, new object[] { p.name })] = p;
            var ais = (System.Collections.IList)Game("BaseAI").GetProperty("BaseAIInstances", Any).GetValue(null);
            foreach (GameObject p in new[] { kit, legacy, legacy }) { GameObject baby = UnityEngine.Object.Instantiate(p, pluginRoot.transform); baby.name = p.name + "(Clone)"; ais.Add(baby.GetComponent(Game("BaseAI"))); }
            Assembly harmonyAssembly = Assembly.LoadFrom(Install + @"\BepInEx\core\0Harmony.dll"); Type harmonyType = harmonyAssembly.GetType("HarmonyLib.Harmony"); object harmony = Activator.CreateInstance(harmonyType, "hearthline.audit.counts"); harmonyType.GetMethod("PatchAll", new[] { typeof(Type) }).Invoke(harmony, new object[] { Mod("HareKitCountPatch") });
            MethodInfo count = Game("SpawnSystem").GetMethod("GetNrOfInstances", new[] { typeof(GameObject), typeof(Vector3), typeof(float), typeof(bool), typeof(bool) }); Assert((int)count.Invoke(null, new object[] { kit, Vector3.zero, 10f, false, false }) == 3, "Patched pen count includes both legacy kits without recursive double-counting"); Assert((int)count.Invoke(null, new object[] { legacy, Vector3.zero, 10f, false, false }) == 3, "Legacy pen queries include new kits symmetrically");
            Func<GameObject, Component> newScene = adultPrefab => { var obj = new GameObject("Another audit scene"); obj.SetActive(false); Component sc = Add(obj, "ZNetScene"); var list = (System.Collections.IList)Game("ZNetScene").GetField("m_prefabs").GetValue(sc); list.Add(adultPrefab); list.Add(boar); return sc; };
            var moddedHare = UnityEngine.Object.Instantiate(hare, pluginRoot.transform); moddedHare.name = "Hare"; UnityEngine.Object.DestroyImmediate(moddedHare.GetComponent(Mod("HearthlineHareAI"))); Add(moddedHare, "MonsterAI"); Component thirdPartyScene = newScene(moddedHare);
            var infos = (System.Collections.IDictionary)bepin.GetType("BepInEx.Bootstrap.Chainloader").GetProperty("PluginInfos").GetValue(null); infos["nick008.tameablehares"] = null;
            Mod("HareLivestock").GetMethod("Configure", Any).Invoke(null, new object[] { thirdPartyScene }); Assert(moddedHare.GetComponent(Mod("HearthlineHareAI")) == null && moddedHare.GetComponent(Game("MonsterAI")) != null, "TameableHares owns adult AI when installed");
            var coexistPrefabs = ((System.Collections.IEnumerable)Game("ZNetScene").GetField("m_prefabs").GetValue(thirdPartyScene)).Cast<GameObject>().ToList(); GameObject coexistKit = coexistPrefabs.Single(p => p.name == "Hearthline_HareKit"); Assert(coexistKit.GetComponent(Game("MonsterAI")) == null && coexistKit.GetComponent(Game("AnimalAI")) != null, "Compatibility kit strips adult feeding AI"); infos.Remove("nick008.tameablehares");
            var rawHare = new GameObject("Hare"); rawHare.SetActive(false); Add(rawHare, "Character"); Add(rawHare, "AnimalAI"); Component disabledScene = newScene(rawHare); set("EnableHares", false); Mod("HareLivestock").GetMethod("Configure", Any).Invoke(null, new object[] { disabledScene }); Assert(rawHare.GetComponent(Game("Humanoid")) == null && rawHare.GetComponent(Game("Procreation")) == null, "Disabled hare feature preserves native adult components"); Assert(((System.Collections.IList)Game("ZNetScene").GetField("m_prefabs").GetValue(disabledScene)).Count == 4, "Disabling feature retains kit IDs for existing saves");
            set("DebugLogging", false); set("MaxStarLevel", 2);
            foreach (string patch in new[] { "ProcreationPatch", "VanillaAnimalLevelPatch", "VanillaAnimalLoadPatch", "VanillaEggQualityPatch" })
                harmonyType.GetMethod("PatchAll", new[] { typeof(Type) }).Invoke(harmony, new object[] { Mod("Patches." + patch) });
            Assert(true, "Star restriction patches and native Procreate transpiler install in Unity Mono");
            foreach (string name in new[] { "Hen", "Lox", "Chicken" }) {
                var animal = new GameObject(name + "(Clone)"); animal.SetActive(false); Component ch = Add(animal, "Character");
                object[] args = { ch, 3 }; Mod("Patches.VanillaAnimalLevelPatch").GetMethod("Prefix", Any).Invoke(null, args);
                Assert((int)args[1] == 1, name + " rejects inherited starred level");
            }
            var chick = new GameObject("CustomChick(Clone)"); chick.SetActive(false); Component chickChar = Add(chick, "Character"); Component growChick = Add(chick, "Growup");
            var henPrefab = new GameObject("Hen"); henPrefab.SetActive(false); Game("Growup").GetField("m_grownPrefab").SetValue(growChick, henPrefab);
            object[] chickArgs = { chickChar, 3 }; Mod("Patches.VanillaAnimalLevelPatch").GetMethod("Prefix", Any).Invoke(null, chickArgs);
            Assert((int)chickArgs[1] == 1, "Growup resolves adult species before clamping");
            var egg = new GameObject("ChickenEgg(Clone)"); egg.SetActive(false); Component dropEgg = Add(egg, "ItemDrop");
            object[] eggArgs = { dropEgg, 3 }; Mod("Patches.VanillaEggQualityPatch").GetMethod("Prefix", Any).Invoke(null, eggArgs);
            Assert((int)eggArgs[1] == 1, "Old and new chicken eggs reject starred quality");
            set("EnableMod", false); object[] disabledArgs = { chickChar, 3 }; Mod("Patches.VanillaAnimalLevelPatch").GetMethod("Prefix", Any).Invoke(null, disabledArgs);
            Assert((int)disabledArgs[1] == 3, "Disabled Hearthline leaves requested levels alone");
            File.WriteAllText(Workspace + @"\_checks\HearthlineAuditUnity\result.txt", "PASS " + checks);
            EditorApplication.Exit(0);
        }
        catch (Exception ex) { Debug.LogException(ex); File.WriteAllText(Workspace + @"\_checks\HearthlineAuditUnity\result.txt", "FAIL " + ex); EditorApplication.Exit(1); }
    }
}
