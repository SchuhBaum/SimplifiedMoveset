using UnityEngine;

namespace SimplifiedMoveset
{
    public static class RainWorldGameMod
    {
        internal static void OnEnable()
        {
            On.RainWorldGame.ctor += RainWorldGame_ctor;
            On.RainWorldGame.ShutDownProcess += RainWorldGame_ShutDownProcess;
        }

        // ----------------- //
        // private functions //
        // ----------------- //

        private static void RainWorldGame_ctor(On.RainWorldGame.orig_ctor orig, RainWorldGame game, ProcessManager manager)
        {
            Debug.Log("SimplifiedMoveset: Initialize.");
            MainModOptions.instance.MainModOptions_OnConfigChanged();
            Debug.Log("SimplifiedMoveset: Add option specific hooks.");

            PlayerMod.OnEnable_();
            if (MainMod.Option_TubeWorm)
            {
                TubeWormMod.OnEnable();
            }

            BodyChunkMod.allAttachedFields.Clear();
            PlayerMod.attachedFields.Clear();
            orig(game, manager);
        }

        private static void RainWorldGame_ShutDownProcess(On.RainWorldGame.orig_ShutDownProcess orig, RainWorldGame game)
        {
            Debug.Log("SimplifiedMoveset: Cleanup. Remove option specific hooks.");
            orig(game);
            BodyChunkMod.allAttachedFields.Clear();
            PlayerMod.attachedFields.Clear();

            PlayerMod.OnDisable_();
            if (MainMod.Option_TubeWorm)
            {
                TubeWormMod.OnDisable();
            }
        }
    }
}