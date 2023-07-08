using UnityEngine;

using static SimplifiedMoveset.MainMod;

namespace SimplifiedMoveset;

internal static class RainWorldGameMod {
    internal static void OnEnable() {
        On.RainWorldGame.ctor += RainWorldGame_ctor;
        On.RainWorldGame.ShutDownProcess += RainWorldGame_ShutDownProcess;
    }

    //
    // private
    //

    private static void RainWorldGame_ctor(On.RainWorldGame.orig_ctor orig, RainWorldGame game, ProcessManager manager) {
        Debug.Log(mod_id + ": Initialize variables.");
        BodyChunkMod.all_attached_fields.Clear();
        PlayerMod.all_attached_fields.Clear();
        orig(game, manager);
    }

    private static void RainWorldGame_ShutDownProcess(On.RainWorldGame.orig_ShutDownProcess orig, RainWorldGame game) {
        Debug.Log(mod_id + ": Cleanup.");
        orig(game);
        BodyChunkMod.all_attached_fields.Clear();
        PlayerMod.all_attached_fields.Clear();
    }
}
