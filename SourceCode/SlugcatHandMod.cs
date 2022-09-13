using System.Drawing.Drawing2D;
using UnityEngine;

namespace SimplifiedMoveset
{
    public static class SlugcatHandMod
    {
        internal static void OnEnable()
        {
            On.SlugcatHand.EngageInMovement += SlugcatHand_EngageInMovement;
        }

        // ----------------- //
        // private functions //
        // ----------------- //

        private static bool SlugcatHand_EngageInMovement(On.SlugcatHand.orig_EngageInMovement orig, SlugcatHand slugcatHand)
        {
            if (MainMod.Option_WallClimb && slugcatHand.owner.owner is Player player)
            {
                PlayerMod.AttachedFields attachedFields = player.GetAttachedFields();
                if (player.bodyMode == Player.BodyModeIndex.WallClimb && player.input[0].y != 0 && player.animation == Player.AnimationIndex.None)
                {
                    if (attachedFields.initializeHands)
                    {
                        if (slugcatHand.limbNumber == 1)
                        {
                            attachedFields.initializeHands = false;
                            player.animationFrame = 0; // not pretty
                        }
                        return orig(slugcatHand);
                    }

                    if ((player.animationFrame == 1 && slugcatHand.limbNumber == 0) || (player.animationFrame == 11 && slugcatHand.limbNumber == 1))
                    {
                        slugcatHand.mode = Limb.Mode.HuntAbsolutePosition;
                        Vector2 position = slugcatHand.connection.pos + new Vector2(player.flipDirection * 10f, 0.0f);
                        slugcatHand.FindGrip(player.room, position, position, 100f, position + new Vector2(0.0f, player.input[0].y < 0 ? -10f : 30f), -player.flipDirection, 2, false);
                    }
                    return false;
                }

                if (!attachedFields.initializeHands)
                {
                    attachedFields.initializeHands = true;
                }
            }
            return orig(slugcatHand);
        }
    }
}