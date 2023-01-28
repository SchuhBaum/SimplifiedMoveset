using System;
using RWCustom;
using UnityEngine;

namespace SimplifiedMoveset
{
    public static class TubeWormMod
    {
        internal static void OnDisable()
        {
            On.TubeWorm.JumpButton -= TubeWorm_JumpButton;
            On.TubeWorm.Tongue.ProperAutoAim -= Tongue_ProperAutoAim;
        }

        internal static void OnEnable()
        {
            On.TubeWorm.JumpButton += TubeWorm_JumpButton; // prioritize jump over using tube worm
            On.TubeWorm.Tongue.ProperAutoAim += Tongue_ProperAutoAim; // auto aim and grapple beams on contact // adjust angle based on inputs in some cases
        }

        //
        // public
        //

        public static Vector2? Tongue_AutoAim_Beams(TubeWorm.Tongue tongue, Vector2 originalDir, bool prioritizeAngleOverDistance, int preferredHorizontalDirection)
        {
            if (tongue.room is not Room room) return originalDir;

            float minDistance = 30f;
            float maxDistance = 230f;
            float idealDistance = tongue.idealRopeLength;

            float deg = Custom.VecToDeg(originalDir);
            float minCost = float.MaxValue;

            Vector2? bestAttachPos = null;
            Vector2? bestDirection = null;

            for (float degModifier = 0.0f; degModifier < 35f; degModifier += 2.5f)
            {
                for (float sign = -1f; sign <= 1f; sign += 2f)
                {
                    Vector2? attachPos = null;
                    Vector2 direction = Custom.DegToVec(deg + sign * degModifier);

                    float localMinCost = float.MaxValue;
                    float cost;

                    foreach (IntVector2 intAttachPos in SharedPhysics.RayTracedTilesArray(tongue.baseChunk.pos + direction * minDistance, tongue.baseChunk.pos + direction * maxDistance)) // don't try to grapple too early, i.e. when MiddleOfTile might be already behind
                    {
                        Room.Tile tile = room.GetTile(intAttachPos);
                        Vector2 middleOfTile = room.MiddleOfTile(intAttachPos);
                        cost = Mathf.Abs(idealDistance - Vector2.Distance(tongue.baseChunk.pos + tongue.baseChunk.vel * 3f, middleOfTile));

                        if ((tile.horizontalBeam || tile.verticalBeam) && cost < localMinCost)
                        {
                            attachPos = middleOfTile;
                            localMinCost = cost;
                        }
                    }

                    if (!attachPos.HasValue) continue;

                    // a bit simpler than vanilla
                    cost = degModifier * 1.5f;
                    if (!prioritizeAngleOverDistance)
                    {
                        cost += Mathf.Abs(idealDistance - Vector2.Distance(tongue.baseChunk.pos + tongue.baseChunk.vel * 3f, attachPos.Value));
                        if (preferredHorizontalDirection != 0)
                        {
                            cost += Mathf.Abs(preferredHorizontalDirection * 90f - (deg + sign * degModifier)) * 0.9f;
                        }
                    }

                    if (cost < minCost)
                    {
                        bestAttachPos = attachPos;
                        bestDirection = direction;
                        minCost = cost;
                    }
                }
            }

            if (bestAttachPos.HasValue)
            {
                tongue.worm.playerCheatAttachPos = bestAttachPos.Value + Custom.DirVec(bestAttachPos.Value, tongue.baseChunk.pos) * 2f;
                return bestDirection;
            }
            return null;
        }

        //
        // private
        //

        private static bool TubeWorm_JumpButton(On.TubeWorm.orig_JumpButton orig, TubeWorm tubeWorm, Player player) // MainMod.Option_TubeWorm
        {
            if ((player.tubeWorm != null && player.canWallJump != 0 && player.input[0].x != -Math.Sign(player.canWallJump)) || player.GetAttachedFields().dontUseTubeWormCounter > 0)
            {
                return player.input[0].jmp && !player.input[1].jmp;
            }
            return orig(tubeWorm, player);
        }

        private static Vector2 Tongue_ProperAutoAim(On.TubeWorm.Tongue.orig_ProperAutoAim orig, TubeWorm.Tongue tongue, Vector2 originalDir) // MainMod.Option_TubeWorm
        {
            if (tongue.room == null) return orig(tongue, originalDir);
            if (tongue.worm.grabbedBy.Count == 0) return orig(tongue, originalDir);
            if (tongue.worm.grabbedBy[0].grabber is not Player player) return orig(tongue, originalDir);

            // adept tongue direction to player inputs in some additional cases
            Vector2 newDir = originalDir;

            if (player.input[0].x != 0)
            {
                // used in the case where y > 0 as well
                newDir += new Vector2(player.input[0].x * 0.35f, 0.0f);
            }
            else
            {
                newDir = new Vector2(0.0f, 1f);
            }

            Vector2 output = orig(tongue, newDir); // updates playerCheatAttachPos
            if (tongue.worm.playerCheatAttachPos.HasValue) return output;

            // priotize up versus left/right in preferredHorizontalDirection
            Vector2? newOutput = Tongue_AutoAim_Beams(tongue, newDir, prioritizeAngleOverDistance: player.input[0].y > 0, preferredHorizontalDirection: newDir.y >= 0.9f ? 0 : player.input[0].x);
            if (newOutput.HasValue) return newOutput.Value;
            return output;
        }
    }
}