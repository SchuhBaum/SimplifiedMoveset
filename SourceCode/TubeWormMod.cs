using System;
using RWCustom;
using UnityEngine;

namespace SimplifiedMoveset
{
    public static class TubeWormMod
    {
        //
        // variables
        //

        private static bool isEnabled;

        //
        //
        //

        internal static void OnToggle()
        {
            if (MainMod.Option_TubeWorm)
            {
                if (!isEnabled)
                {
                    On.TubeWorm.Tongue.ProperAutoAim += Tongue_ProperAutoAim; // auto aim and grapple beams on contact 
                    On.TubeWorm.Tongue.Shoot += Tongue_Shoot; // adjust angle based on inputs in some cases

                    On.TubeWorm.JumpButton += TubeWorm_JumpButton; // prioritize jump over using tube worm
                    On.TubeWorm.Update += TubeWorm_Update; // force retract tongue in some cases
                }
                else
                {
                    On.TubeWorm.Tongue.ProperAutoAim -= Tongue_ProperAutoAim;
                    On.TubeWorm.Tongue.Shoot -= Tongue_Shoot;

                    On.TubeWorm.JumpButton -= TubeWorm_JumpButton;
                    On.TubeWorm.Update -= TubeWorm_Update;
                }
            }
            isEnabled = !isEnabled;
        }

        //
        // public
        //

        public static Vector2? Tongue_AutoAim_Beams(TubeWorm.Tongue tongue, Vector2 originalDir, bool prioritizeAngleOverDistance, int preferredHorizontalDirection)
        {
            if (tongue.room is not Room room) return originalDir;

            // if you hit with maxDistance (> maxRopeLength) then the bodyChunk connection might need to overcompensate
            // this gives you a lot of speed
            float maxDistance = 230f;

            float minDistance = 30f;
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

        private static Vector2 Tongue_ProperAutoAim(On.TubeWorm.Tongue.orig_ProperAutoAim orig, TubeWorm.Tongue tongue, Vector2 direction) // MainMod.Option_TubeWorm
        {
            if (tongue.worm.grabbedBy.Count == 0) return orig(tongue, direction);
            if (tongue.worm.grabbedBy[0].grabber is not Player player) return orig(tongue, direction);

            Vector2 output = orig(tongue, direction); // updates playerCheatAttachPos
            if (tongue.worm.playerCheatAttachPos.HasValue) return output;

            // priotize up versus left/right in preferredHorizontalDirection
            Vector2? newOutput = Tongue_AutoAim_Beams(tongue, direction, prioritizeAngleOverDistance: player.input[0].x == 0 && player.input[0].y > 0, preferredHorizontalDirection: direction.y >= 0.9f ? 0 : player.input[0].x);
            if (newOutput.HasValue) return newOutput.Value;
            return output;
        }

        private static void Tongue_Shoot(On.TubeWorm.Tongue.orig_Shoot orig, TubeWorm.Tongue tongue, Vector2 direction)// MainMod.Option_TubeWorm
        {
            if (tongue.worm.grabbedBy.Count == 0 || tongue.worm.grabbedBy[0].grabber is not Player player)
            {
                orig(tongue, direction);
                return;
            }

            // adept tongue direction to player inputs in some additional cases
            if (player.input[0].x != 0)
            {
                // used in the case where y > 0 as well
                direction += new Vector2(player.input[0].x * 0.35f, 0.0f);
                direction.Normalize();
            }
            else
            {
                direction = new Vector2(0.0f, 1f);
            }
            orig(tongue, direction);
        }

        //
        //
        //

        private static bool TubeWorm_JumpButton(On.TubeWorm.orig_JumpButton orig, TubeWorm tubeWorm, Player player) // MainMod.Option_TubeWorm
        {
            if (player.IsClimbingOnBeam() || player.CanMidAirWallJump() || player.bodyMode == Player.BodyModeIndex.CorridorClimb) return player.IsJumpPressed();

            // prevents falling off beams and using tongue at the same time
            if (player.GetAttachedFields().isTongueDisabled) return player.IsJumpPressed();
            return orig(tubeWorm, player);
        }
        private static void TubeWorm_Update(On.TubeWorm.orig_Update orig, TubeWorm tubeWorm, bool eu) // MainMod.Option_TubeWorm
        {
            Player? player = null;
            foreach (Creature.Grasp? grasp in tubeWorm.grabbedBy)
            {
                if (grasp?.grabber is Player player_)
                {
                    player = player_;
                    break;
                }
            }

            if (player == null)
            {
                orig(tubeWorm, eu);
                return;
            }

            if (player.GetAttachedFields().tongueNeedsToRetract || tubeWorm.tongues[0].Attached && player.IsJumpPressed() && (player.IsClimbingOnBeam() || player.CanMidAirWallJump() || player.bodyMode == Player.BodyModeIndex.CorridorClimb))
            {
                tubeWorm.tongues[0].Release();
                player.GetAttachedFields().tongueNeedsToRetract = false;
                return;
            }
            orig(tubeWorm, eu);
        }
    }
}