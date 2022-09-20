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
            Debug.Log("SimplifiedMoveset: Initialize. Add option specific hooks.");

            if (MainMod.Option_BeamClimb)
            {
                PlayerMod.OnEnable_Option_BeamClimb();
            }

            if (MainMod.Option_BellySlide)
            {
                PlayerMod.OnEnable_Option_BellySlide();
            }

            if (MainMod.Option_Crawl)
            {
                PlayerMod.OnEnable_Option_Crawl();
            }

            if (MainMod.Option_SpearThrow)
            {
                PlayerMod.OnEnable_Option_SpearThrow();
            }

            if (MainMod.Option_Swim)
            {
                PlayerMod.OnEnable_Option_Swim();
            }

            if (MainMod.Option_WallJump)
            {
                PlayerMod.OnEnable_Option_WallJump();
            }

            BodyChunkMod.bodyChunkConnectionVel.Clear();
            BodyChunkMod.lastOnSlope.Clear();
            BodyChunkMod.lastOnSlopeTilePos.Clear();
            PlayerMod.attachedFields.Clear();

            orig(game, manager);
        }

        private static void RainWorldGame_ShutDownProcess(On.RainWorldGame.orig_ShutDownProcess orig, RainWorldGame game)
        {
            Debug.Log("SimplifiedMoveset: Cleanup. Remove option specific hooks.");
            orig(game);

            BodyChunkMod.bodyChunkConnectionVel.Clear();
            BodyChunkMod.lastOnSlope.Clear();
            BodyChunkMod.lastOnSlopeTilePos.Clear();
            PlayerMod.attachedFields.Clear();

            if (MainMod.Option_BeamClimb)
            {
                PlayerMod.OnDisable_Option_BeamClimb();
            }

            if (MainMod.Option_BellySlide)
            {
                PlayerMod.OnDisable_Option_BellySlide();
            }

            if (MainMod.Option_Crawl)
            {
                PlayerMod.OnDisable_Option_Crawl();
            }

            if (MainMod.Option_SpearThrow)
            {
                PlayerMod.OnDisable_Option_SpearThrow();
            }

            if (MainMod.Option_Swim)
            {
                PlayerMod.OnDisable_Option_Swim();
            }

            if (MainMod.Option_WallJump)
            {
                PlayerMod.OnDisable_Option_WallJump();
            }
        }
    }
}