# Hearthline 1.1.1 audit

Vanilla eligibility was checked directly in the installed game's serialized spawn assets (core SoftRef bundle c4210710 with script dependencies), rather than inferred from LevelEffects. Max levels: Boar, Wolf, Asksvin, Hare, Moose, Deer and Neck allow level 3; Hen, Chicken and Lox allow only level 1. Level 1 means zero stars.

The central policy denies unrecognized species and limits eligible species to two stars even with CLLC. It applies to ordinary Procreate inheritance and upgrades, away births, wild-family offspring, SetLevel/growth and ChickenEgg.SetQuality. Character.Awake normalizes existing breeding animals before visual effects start; only the owner persists the level, while replicas normalize their local level. Disabling Hearthline bypasses these patches. Existing custom breeding species outside the allowlist also normalize to level 1.

Validation: 87 automated tests passed; 29 Unity Mono fixture checks passed against installed Valheim assemblies, including installation of the actual Procreate transpiler and new Harmony patches, unsupported species, adult resolution through Growup, egg quality, disabled behavior and prior hare compatibility checks. Release build completed with zero errors (75 existing warnings on a non-incremental build). Five README image URLs passed HTTP/image validation.

This is code and isolated runtime validation, not a gameplay/multiplayer session. Restart the Mod tests profile to load the DLL and observe normalization in-world. Deployment was verified by matching SHA256 hashes between the build and the real Thunderstore Mod tests profile; the previous DLL was backed up outside the profile.
