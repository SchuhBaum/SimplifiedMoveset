using System;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RWCustom;
using UnityEngine;

using static Player;
using static Room;
using static SimplifiedMoveset.MainMod;
using static SimplifiedMoveset.PlayerMod;

namespace SimplifiedMoveset;

public static class TubeWormMod
{
    //
    // main
    //

    internal static void On_Config_Changed()
    {
        On.TubeWorm.Tongue.ProperAutoAim -= Tongue_ProperAutoAim;
        On.TubeWorm.Tongue.Shoot -= Tongue_Shoot;

        IL.TubeWorm.Update -= IL_TubeWorm_Update;
        On.TubeWorm.JumpButton -= TubeWorm_JumpButton;

        if (Option_TubeWorm)
        {
            On.TubeWorm.Tongue.ProperAutoAim += Tongue_ProperAutoAim; // auto aim and grapple beams on contact 
            On.TubeWorm.Tongue.Shoot += Tongue_Shoot; // adjust angle based on inputs in some cases

            IL.TubeWorm.Update += IL_TubeWorm_Update; // force retract tongue in some cases
            On.TubeWorm.JumpButton += TubeWorm_JumpButton; // prioritize jump over using tube worm
        }
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
                    Tile tile = room.GetTile(intAttachPos);
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

    private static Vector2 Tongue_ProperAutoAim(On.TubeWorm.Tongue.orig_ProperAutoAim orig, TubeWorm.Tongue tongue, Vector2 direction) // Option_TubeWorm
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

    private static void Tongue_Shoot(On.TubeWorm.Tongue.orig_Shoot orig, TubeWorm.Tongue tongue, Vector2 direction)// Option_TubeWorm
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
            orig(tongue, direction);
            return;
        }

        direction = new Vector2(0.0f, 1f);
        orig(tongue, direction);
    }

    //
    //
    //

    private static void IL_TubeWorm_Update(ILContext context) // Option_TubeWorm
    {
        // LogAllInstructions(context);
        ILCursor cursor = new(context);

        cursor.Emit(OpCodes.Ldarg_0);
        cursor.EmitDelegate<Func<TubeWorm, bool>>(tubeworm =>
        {
            Player? player = null;
            foreach (Creature.Grasp? grasp in tubeworm.grabbedBy)
            {
                if (grasp?.grabber is Player player_)
                {
                    player = player_;
                    break;
                }
            }

            // "call" orig() when returning true;
            if (player == null || player.Get_Attached_Fields() is not Player_Attached_Fields attached_fields) return true;

            if (attached_fields.tubeworm_tongue_needs_to_retract || tubeworm.tongues[0].Attached && player.IsJumpPressed() && (player.IsClimbingOnBeam() || player.CanWallJumpOrMidAirWallJump() || player.bodyMode == BodyModeIndex.CorridorClimb))
            {
                tubeworm.tongues[0].Release();
                attached_fields.tubeworm_tongue_needs_to_retract = false;
                return false;
            }
            return true;
        });

        ILLabel label = cursor.DefineLabel();
        cursor.Emit(OpCodes.Brtrue, label);
        cursor.Emit(OpCodes.Ret);
        cursor.MarkLabel(label);

        // LogAllInstructions(context);
    }

    private static bool TubeWorm_JumpButton(On.TubeWorm.orig_JumpButton orig, TubeWorm tubeworm, Player player) // Option_TubeWorm
    {
        bool vanilla_result = orig(tubeworm, player);

        if (player.IsClimbingOnBeam() || player.CanWallJumpOrMidAirWallJump() || player.bodyMode == BodyModeIndex.CorridorClimb) return player.IsJumpPressed();
        if (player.shortcutDelay > 10) return player.IsJumpPressed();
        if (player.Get_Attached_Fields() is not Player_Attached_Fields attached_fields) return vanilla_result;

        // prevents falling off beams and using tongue at the same time
        if (attached_fields.dont_use_tubeworm_counter > 0) return player.IsJumpPressed();
        return vanilla_result;
    }
}