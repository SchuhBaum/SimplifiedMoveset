using UnityEngine;

using static Player;
using static SimplifiedMoveset.MainMod;
using static SimplifiedMoveset.PlayerMod;

namespace SimplifiedMoveset;

internal static class SlugcatHandMod {
    //
    // main
    //

    internal static void On_Config_Changed() {
        On.SlugcatHand.EngageInMovement -= SlugcatHand_EngageInMovement;
        if (Option_WallClimb) {
            On.SlugcatHand.EngageInMovement += SlugcatHand_EngageInMovement;
        }
    }

    //
    // private
    //

    private static bool SlugcatHand_EngageInMovement(On.SlugcatHand.orig_EngageInMovement orig, SlugcatHand slugcat_hand) // Option_WallClimb
    {
        if (slugcat_hand.owner is not PlayerGraphics player_graphics) return orig(slugcat_hand);
        if (player_graphics.owner is not Player player) return orig(slugcat_hand);
        if (player.Get_Attached_Fields() is not Player_Attached_Fields attached_fields) return orig(slugcat_hand);

        if (player.bodyMode != BodyModeIndex.WallClimb || player.input[0].y == 0 || player.animation != AnimationIndex.None) {
            attached_fields.initialize_hands = true;
            return orig(slugcat_hand);
        }

        if (attached_fields.initialize_hands) {
            if (slugcat_hand.limbNumber == 1) {
                attached_fields.initialize_hands = false;
                player.animationFrame = 0; // not pretty
            }
            return orig(slugcat_hand);
        }

        // make sure to call orig() for compatibility;
        // the wall climb section in orig() changes absoluteHuntPos;
        Vector2 current_absolute_hunt_position = slugcat_hand.absoluteHuntPos;
        orig(slugcat_hand);
        slugcat_hand.absoluteHuntPos = current_absolute_hunt_position;

        if (!(player.animationFrame == 1 && slugcat_hand.limbNumber == 0 || player.animationFrame == 11 && slugcat_hand.limbNumber == 1)) return false;
        slugcat_hand.mode = Limb.Mode.HuntAbsolutePosition;
        Vector2 attached_position = slugcat_hand.connection.pos + new Vector2(player.flipDirection * 10f, 0.0f);

        // player.input[0].y is not zero;
        if (player.input[0].y > 0) {
            slugcat_hand.FindGrip(player.room, attached_position, attached_position, 100f, attached_position + new Vector2(0.0f, 30f), -player.flipDirection, 2, false);
            player_graphics.LookAtPoint(slugcat_hand.absoluteHuntPos, 0f);
            player_graphics.objectLooker.timeLookingAtThis = 6;
            return false;
        }

        slugcat_hand.FindGrip(player.room, attached_position, attached_position, 100f, attached_position + new Vector2(0.0f, -10f), -player.flipDirection, 2, false);
        player_graphics.LookAtPoint(slugcat_hand.absoluteHuntPos + new Vector2(0f, -20f), 0f);
        player_graphics.objectLooker.timeLookingAtThis = 6;
        return false;
    }
}
