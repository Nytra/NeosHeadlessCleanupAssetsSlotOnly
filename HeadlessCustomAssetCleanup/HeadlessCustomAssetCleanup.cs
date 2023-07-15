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
		public override string Version => "1.1.0";
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

		private static void CustomCleanupAssets(World w)
		{
			int count = 1;
			int iterations = 0;
			//Debug("Entering loop!");
			while (count > 0 && iterations < 25)
			{
				//Debug("Loop iteration started!");
				List<IAssetProvider> componentsInChildren = w.AssetsSlot.GetComponentsInChildren((IAssetProvider p) => p.AssetReferenceCount == 0);
				count = componentsInChildren.Count;

				componentsInChildren.ForEach(delegate (IAssetProvider p)
				{
					p.Destroy();
				});

				count += WorldOptimizer.CleanupEmptySlots(w.AssetsSlot);
				iterations++;
			}
			//Debug("Finished loop!");
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
					lastAssetCleanupField.SetValue(__instance, DateTime.UtcNow);

					Debug("Running custom asset cleanup!");

					CustomCleanupAssets(__instance.World);

					__instance.World.RunInUpdates(7, delegate { CustomCleanupAssets(__instance.World); });

					//Debug("Finished custom asset cleanup!");

					// ORIGINAL CODE BELOW

					//_lastAssetCleanup = DateTime.UtcNow;
					//if (base.InputInterface.HeadDevice == HeadOutputDevice.Headless)
					//{
					//    MaterialOptimizer.DeduplicateMaterials(base.World);
					//    WorldOptimizer.DeduplicateStaticProviders(base.World);
					//}
					//WorldOptimizer.CleanupAssets(base.World, ignoreNonpersistentUsers: false, WorldOptimizer.CleanupMode.Destroy);
				}
				return false;
			}
		}
	}
}