using FrooxEngine;
using HarmonyLib;
using NeosModLoader;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace HeadlessCustomAssetCleanup
{
    public class HeadlessCustomAssetCleanup : NeosMod
    {
        public override string Name => "HeadlessCustomAssetCleanup";
        public override string Author => "Nytra";
        public override string Version => "1.0.0";
        public override string Link => "https://github.com/Nytra/NeosHeadlessCustomAssetCleanup";

        private static FieldInfo lastAutoSaveField = AccessTools.Field(typeof(WorldConfiguration), "_lastAutoSave");
        private static FieldInfo lastAssetCleanupField = AccessTools.Field(typeof(WorldConfiguration), "_lastAssetCleanup");

        public override void OnEngineInit()
        {
            Harmony harmony = new Harmony("owo.Nytra.HeadlessCustomAssetCleanup");
            Type neosHeadless = AccessTools.TypeByName("NeosHeadless.Program");
            if (neosHeadless != null)
            {
                harmony.PatchAll();
            }
        }

        // A transpiler would be better here
        // ... but I'm lazy

        [HarmonyPatch(typeof(WorldConfiguration), "InternalRunUpdate")]
        class HeadlessCustomAssetCleanupPatch
        {
            public static bool Prefix(WorldConfiguration __instance)
            {
                if (!__instance.World.IsAuthority)
                {
                    return false;
                }
                if (__instance.AutoSaveEnabled.Value && (DateTime.UtcNow - (DateTime)lastAutoSaveField.GetValue(__instance)).TotalMinutes >= (double)__instance.AutoSaveInterval.Value)
                {
                    lastAutoSaveField.SetValue(__instance, DateTime.UtcNow);
                    Userspace.SaveWorldAuto(__instance.World, SaveType.Overwrite, exitOnSave: false);
                }
                if (__instance.AutoCleanupEnabled.Value && (DateTime.UtcNow - (DateTime)lastAssetCleanupField.GetValue(__instance)).TotalSeconds >= (double)__instance.AutoCleanupInterval.Value)
                {
                    Debug("Running custom asset cleanup!");

                    lastAssetCleanupField.SetValue(__instance, DateTime.UtcNow);

                    List<IAssetProvider> componentsInChildren = __instance.World.AssetsSlot.GetComponentsInChildren((IAssetProvider p) => p.AssetReferenceCount == 0);
                    componentsInChildren.ForEach(delegate (IAssetProvider p)
                    {
                        p.Destroy();
                    });

                    WorldOptimizer.CleanupEmptySlots(__instance.World.AssetsSlot);

                    // ORIGINAL CODE BELOW

                    //if (__instance.InputInterface.HeadDevice == HeadOutputDevice.Headless)
                    //{
                        //MaterialOptimizer.DeduplicateMaterials(__instance.World);
                        //WorldOptimizer.DeduplicateStaticProviders(__instance.World);
                    //}
                    //WorldOptimizer.CleanupAssets(__instance.World, ignoreNonpersistentUsers: false, WorldOptimizer.CleanupMode.Destroy);
                }
                return false;
            }
        }
    }
}