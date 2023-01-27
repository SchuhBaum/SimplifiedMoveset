using System;
using RWCustom;
using UnityEngine;

namespace SimplifiedMoveset
{
    public static class TubeWormMod
    {
        internal static void OnEnable()
        {
            On.TubeWorm.JumpButton += TubeWorm_JumpButton; // prioritize jump over using tube worm
            On.TubeWorm.Update += TubeWorm_Update; // prioritize jump over using tube worm

            On.TubeWorm.Tongue.ProperAutoAim += Tongue_ProperAutoAim; // auto aim and grapple beams on contact // adjust angle based on inputs in some cases
        }

        // ----------------- //
        // private functions //
        // ----------------- //

        private static bool TubeWorm_JumpButton(On.TubeWorm.orig_JumpButton orig, TubeWorm tubeWorm, Player player)
        {
            if ((MainMod.Option_WallJump && player.tubeWorm != null && player.canWallJump != 0 && player.input[0].x != -Math.Sign(player.canWallJump)) || player.GetAttachedFields().dontUseTubeWormCounter > 0)
            {
                return player.input[0].jmp && !player.input[1].jmp;
            }
            return orig(tubeWorm, player);
        }

        private static void TubeWorm_Update(On.TubeWorm.orig_Update orig, TubeWorm tubeWorm, bool eu)
        {
            if (tubeWorm.grabbedBy.Count == 1 && tubeWorm.grabbedBy[0].grabber is Player player && player.GetAttachedFields().dontUseTubeWormCounter > 0)
            {
                --player.GetAttachedFields().dontUseTubeWormCounter;
            }
            orig(tubeWorm, eu);
        }

        private static Vector2 Tongue_ProperAutoAim(On.TubeWorm.Tongue.orig_ProperAutoAim orig, TubeWorm.Tongue tongue, Vector2 originalDir)
        {
            if (!MainMod.Option_TubeWorm)
            {
                return orig(tongue, originalDir);
            }

            Vector2 newDir = originalDir;
            int inputX = 0;
            bool inputUp = false;

            // adept tongue direction to player inputs in some additional cases
            if (tongue.worm.grabbedBy.Count > 0 && tongue.worm.grabbedBy[0].grabber is Player player)
            {
                if (player.input[0].x != 0)
                {
                    // used in the case where y > 0 as well
                    newDir += new Vector2(player.input[0].x * 0.35f, 0.0f);
                }
                else
                {
                    newDir = new Vector2(0.0f, 1f);
                }

                inputUp = player.input[0].y > 0;
                if (newDir.y < 0.9f)
                {
                    inputX = player.input[0].x;
                }
            }

            Vector2 output = orig(tongue, newDir);
            if (tongue.worm.playerCheatAttachPos.HasValue || tongue.room == null)
            {
                return output;
            }

            // do the same thing as in the original function but with horizontal and vertical beams
            float maxDistance = 230f;
            float idealDistance = tongue.idealRopeLength;
            float deg = Custom.VecToDeg(newDir);
            bool beamFound = false;

            if (!inputUp)
            {
                IntVector2 tilePosition = tongue.room.GetTilePosition(tongue.baseChunk.pos + tongue.baseChunk.vel * 3f);
                for (int index = 0; index < 10; ++index)
                {
                    --tilePosition.y;
                    Room.Tile tile = tongue.room.GetTile(tilePosition);

                    if (tile.horizontalBeam || tile.verticalBeam)
                    {
                        idealDistance = Mathf.Max(40f, Vector2.Distance(tongue.baseChunk.pos + tongue.baseChunk.vel * 3f, tongue.room.MiddleOfTile(tilePosition)) - 40f);
                        beamFound = true;
                        break;
                    }
                }
            }

            float minCost = float.MaxValue;
            for (float degModifier = 0.0f; degModifier < 35f; degModifier += 2.5f)
            {
                for (float sign = -1f; sign <= 1f; sign += 2f)
                {
                    Vector2? attachPos = null;
                    Vector2 direction = Custom.DegToVec(deg + sign * degModifier);

                    foreach (IntVector2 intAttachPos in SharedPhysics.RayTracedTilesArray(tongue.baseChunk.pos + direction * 30f, tongue.baseChunk.pos + direction * maxDistance)) // don't try to grapple too early, i.e. when MiddleOfTile might be already behind
                    {
                        Room.Tile tile = tongue.room.GetTile(intAttachPos);
                        if (tile.horizontalBeam || tile.verticalBeam)
                        {
                            attachPos = tongue.room.MiddleOfTile(intAttachPos);
                            break;
                        }
                    }

                    if (attachPos.HasValue)
                    {
                        float cost = degModifier * 1.5f;

                        // why do I check for inputUp?
                        // the effect is that only the degree difference matters for the cost;
                        // the closer you the better;
                        // otherwise the distance gets more important;
                        // shouldn't that be the default?
                        //
                        // I guess vanilla does this as well;
                        // the goal is to extend and not to replace here;
                        if (!inputUp)
                        {
                            cost += Mathf.Abs(idealDistance - Vector2.Distance(tongue.baseChunk.pos + tongue.baseChunk.vel * 3f, attachPos.Value));
                            if (inputX != 0)
                            {
                                cost += Mathf.Abs(inputX * 90f - (deg + sign * degModifier)) * 0.9f;
                            }

                            if (beamFound)
                            {
                                for (int index = -1; index < 2; ++index)
                                {
                                    if (!tongue.room.VisualContact(attachPos.Value, attachPos.Value - new Vector2(40f * index, Vector2.Distance(tongue.baseChunk.pos, attachPos.Value) + 20f)))
                                    {
                                        cost += 1000f;
                                        break;
                                    }
                                }
                            }
                        }

                        if (cost < minCost)
                        {
                            minCost = cost;
                            output = direction;
                            tongue.worm.playerCheatAttachPos = new Vector2?(attachPos.Value + Custom.DirVec(attachPos.Value, tongue.baseChunk.pos) * 2f);
                        }
                    }
                }
            }
            return output;
        }
    }
}