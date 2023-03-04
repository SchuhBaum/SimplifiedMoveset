using UnityEngine;

using static Player;
using static SimplifiedMoveset.MainMod;
using static SimplifiedMoveset.PlayerMod;

namespace SimplifiedMoveset;

public static class SlugcatHandMod
{
    //
    // variables
    //

    private static bool is_enabled = false;

    //
    //
    //

    internal static void OnToggle()
    {
        is_enabled = !is_enabled;
        if (Option_WallClimb)
        {
            if (is_enabled)
            {
                On.SlugcatHand.EngageInMovement += SlugcatHand_EngageInMovement;
            }
            else
            {
                On.SlugcatHand.EngageInMovement -= SlugcatHand_EngageInMovement;
            }
        }
    }

    //
    // private
    //

    private static bool SlugcatHand_EngageInMovement(On.SlugcatHand.orig_EngageInMovement orig, SlugcatHand slugcat_hand) // Option_WallClimb
    {
        // make sure to call orig() for compatibility;
        bool vanilla_result = orig(slugcat_hand);
        if (slugcat_hand.owner.owner is not Player player || player.Get_Attached_Fields() is not Player_Attached_Fields attached_fields)
        {
            return vanilla_result;
        }

        if (player.bodyMode != BodyModeIndex.WallClimb || player.input[0].y == 0 || player.animation != AnimationIndex.None)
        {
            attached_fields.initialize_hands = true;
            return vanilla_result;
        }

        if (attached_fields.initialize_hands)
        {
            if (slugcat_hand.limbNumber == 1)
            {
                attached_fields.initialize_hands = false;
                player.animationFrame = 0; // not pretty
            }
            return vanilla_result;
        }

        if (!(player.animationFrame == 1 && slugcat_hand.limbNumber == 0 || player.animationFrame == 11 && slugcat_hand.limbNumber == 1)) return false;
        slugcat_hand.mode = Limb.Mode.HuntAbsolutePosition;
        Vector2 position = slugcat_hand.connection.pos + new Vector2(player.flipDirection * 10f, 0.0f);
        slugcat_hand.FindGrip(player.room, position, position, 100f, position + new Vector2(0.0f, player.input[0].y < 0 ? -10f : 30f), -player.flipDirection, 2, false);
        return false;
    }
}