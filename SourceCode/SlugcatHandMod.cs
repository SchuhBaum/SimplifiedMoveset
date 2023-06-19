using UnityEngine;

using static Player;
using static SimplifiedMoveset.MainMod;
using static SimplifiedMoveset.PlayerMod;

namespace SimplifiedMoveset;

internal static class SlugcatHandMod
{
    //
    // main
    //

    internal static void On_Config_Changed()
    {
        On.SlugcatHand.EngageInMovement -= SlugcatHand_EngageInMovement;
        if (Option_WallClimb)
        {
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

        if (player.bodyMode != BodyModeIndex.WallClimb || player.input[0].y == 0 || player.animation != AnimationIndex.None)
        {
            attached_fields.initialize_hands = true;
            return orig(slugcat_hand);
        }

        if (attached_fields.initialize_hands)
        {
            if (slugcat_hand.limbNumber == 1)
            {
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
        if (player.input[0].y > 0)
        {
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

    // this is closer to what crawl does;
    // I am too lazy to fine tune this at this point;
    // maybe later;
    // it seems that I still need to initialize the hands;
    // otherwise when holding up or down before reaching the wall
    // then they only show after one animation cycle;
    // private static bool SlugcatHand_EngageInMovement(On.SlugcatHand.orig_EngageInMovement orig, SlugcatHand slugcat_hand) // Option_WallClimb
    // {
    //     if (slugcat_hand.limbNumber > 1) return orig(slugcat_hand);
    //     if (slugcat_hand.mode != Mode.HuntAbsolutePosition) return orig(slugcat_hand);
    //     if (slugcat_hand.owner is not PlayerGraphics player_graphics) return orig(slugcat_hand);
    //     if (player_graphics.owner is not Player player) return orig(slugcat_hand);

    //     if (player.bodyMode != BodyModeIndex.WallClimb) return orig(slugcat_hand);
    //     if (player.animation != AnimationIndex.None) return orig(slugcat_hand);

    //     if (player.input[0].y == 0) return orig(slugcat_hand);

    //     // if (!(player.animationFrame == 1 && slugcat_hand.limbNumber == 0 || player.animationFrame == 11 && slugcat_hand.limbNumber == 1)) return false;

    //     // call orig() for compatibility;
    //     // the wall climb section in orig() changes absoluteHuntPos;
    //     Vector2 current_absolute_hunt_position = slugcat_hand.absoluteHuntPos;
    //     bool vanilla_result = orig(slugcat_hand);
    //     SlugcatHand hand_0 = player_graphics.hands[0];

    //     Debug.Log("TEMP: limbNumber " + slugcat_hand.limbNumber);
    //     Debug.Log("TEMP: mode " + slugcat_hand.mode);
    //     Debug.Log("TEMP: retract " + slugcat_hand.retract);
    //     Debug.Log("TEMP: huntSpeed " + slugcat_hand.huntSpeed);
    //     Debug.Log("TEMP: hand reached snap position " + slugcat_hand.reachedSnapPosition);
    //     Debug.Log("TEMP: hand distance " + Custom.Dist(player.mainBodyChunk.pos, slugcat_hand.absoluteHuntPos));
    //     Debug.Log("TEMP: hand pos " + slugcat_hand.pos);
    //     Debug.Log("TEMP: hand absoluteHuntPos " + slugcat_hand.absoluteHuntPos);
    //     Debug.Log("TEMP: body_chunk 0 position " + player.bodyChunks[0].pos);


    //     Debug.Log("TEMP: distance both hands " + Mathf.Abs(player_graphics.hands[0].pos.y - player_graphics.hands[1].pos.y));

    //     Debug.Log("TEMP: distance hunt positions " + Custom.Dist(hand_0.absoluteHuntPos, player_graphics.hands[1].absoluteHuntPos));
    //     // initialize first by executing the wall climb section in orig() once;
    //     // if (Custom.DistLess(hand_0.absoluteHuntPos, player_graphics.hands[1].absoluteHuntPos, 1f)) return vanilla_result;
    //     // slugcat_hand.absoluteHuntPos = current_absolute_hunt_position;


    //     // same as crawl;
    //     slugcat_hand.mode = Mode.HuntAbsolutePosition;
    //     slugcat_hand.huntSpeed = 12f;
    //     slugcat_hand.quickness = 0.7f;

    //     BodyChunk body_chunk_0 = player.bodyChunks[0];

    //     if (player.input[0].y > 0)
    //     {
    //         if ((slugcat_hand.limbNumber == 0 || (Mathf.Abs(hand_0.pos.y - body_chunk_0.pos.y) < 10f && hand_0.reachedSnapPosition)) && !Custom.DistLess(body_chunk_0.pos, current_absolute_hunt_position, 24f))
    //         {
    //             // forbiddenYDirs = 2 means it is ignored?;
    //             Vector2 attached_position = slugcat_hand.connection.pos + new Vector2(player.flipDirection * 10f, 0.0f);
    //             slugcat_hand.FindGrip(player.room, attached_position, attached_position, 100f, attached_position + new Vector2(0.0f, 28f), forbiddenXDirs: -player.flipDirection, forbiddenYDirs: -1, behindWalls: false);
    //             // slugcat_hand.FindGrip(player.room, attached_position, attached_position, 100f, attached_position + new Vector2(0.0f, 30f), forbiddenXDirs: -player.flipDirection, forbiddenYDirs: 2, behindWalls: false);
    //             return false;
    //         }
    //         return vanilla_result;
    //     }

    //     // moving up is almost good; only the first grab is a bit late;
    //     // moving down uses both hands in quick succession; not so great;
    //     if ((slugcat_hand.limbNumber == 0 || (Mathf.Abs(hand_0.pos.y - body_chunk_0.pos.y) < 10f && hand_0.reachedSnapPosition)) && !Custom.DistLess(body_chunk_0.pos, current_absolute_hunt_position, 20f))
    //     {
    //         // forbiddenYDirs = 2 means it is ignored?;
    //         Vector2 attached_position = slugcat_hand.connection.pos + new Vector2(player.flipDirection * 10f, 0.0f);
    //         slugcat_hand.FindGrip(player.room, attached_position, attached_position, 100f, attached_position + new Vector2(0.0f, -14f), -player.flipDirection, 1, false);
    //         // slugcat_hand.FindGrip(player.room, attached_position, attached_position, 100f, attached_position + new Vector2(0.0f, -30f), -player.flipDirection, 2, false);
    //         // slugcat_hand.FindGrip(owner.owner.room, connection.pos + new Vector2((float)(owner.owner as Player).flipDirection * 20f, 0f), connection.pos + new Vector2((float)(owner.owner as Player).flipDirection * 20f, 0f), 100f, new Vector2(owner.owner.bodyChunks[0].pos.x + (float)(owner.owner as Player).flipDirection * 28f, owner.owner.room.MiddleOfTile(owner.owner.bodyChunks[0].pos).y - 10f), 2, 1, behindWalls: false);
    //         return false;
    //     }
    //     return vanilla_result;
    // }
}