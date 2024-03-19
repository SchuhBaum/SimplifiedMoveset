using Mono.Cecil.Cil;
using MonoMod.Cil;
using RWCustom;
using System;
using System.Collections.Generic;
using UnityEngine;
using static Player;
using static Room;
using static SimplifiedMoveset.MainMod;
using static SimplifiedMoveset.PlayerMod;

namespace SimplifiedMoveset;

public static class TubeWormMod {
    //
    // main
    //

    internal static void On_Config_Changed() {
        IL.TubeWorm.Update -= IL_TubeWorm_Update;
        On.TubeWorm.JumpButton -= TubeWorm_JumpButton;
        On.TubeWorm.Tongue.ProperAutoAim -= Tongue_ProperAutoAim;
        On.TubeWorm.Tongue.Shoot -= Tongue_Shoot;

        if (Option_TubeWorm) {
            IL.TubeWorm.Update += IL_TubeWorm_Update; // force retract tongue in some cases
            On.TubeWorm.JumpButton += TubeWorm_JumpButton; // prioritize jump over using tube worm
            On.TubeWorm.Tongue.ProperAutoAim += Tongue_ProperAutoAim; // auto aim and grapple beams on contact 
            On.TubeWorm.Tongue.Shoot += Tongue_Shoot; // adjust angle based on inputs in some cases
        }
    }

    //
    // public
    //

    public static Vector2? Tongue_AutoAim_Beams(TubeWorm.Tongue tongue, Vector2 original_dir, bool prioritize_angle_over_distance, int preferred_horizontal_direction) {
        if (tongue.room is not Room room) return original_dir;

        // if you hit with maxDistance (> maxRopeLength) then the bodyChunk connection might need to overcompensate
        // this gives you a lot of speed
        float max_distance = 230f;

        float min_distance = 30f;
        float ideal_distance = tongue.idealRopeLength;

        float deg = Custom.VecToDeg(original_dir);
        float min_cost = float.MaxValue;

        Vector2? best_attach_position = null;
        Vector2? best_direction = null;

        for (float deg_modifier = 0.0f; deg_modifier < 35f; deg_modifier += 2.5f) {
            for (float sign = -1f; sign <= 1f; sign += 2f) {
                Vector2? attach_position = null;
                Vector2 direction = Custom.DegToVec(deg + sign * deg_modifier);

                float local_min_cost = float.MaxValue;
                float cost;

                List<IntVector2> tiles_position_list = new();
                SharedPhysics.RayTracedTilesArray(tongue.baseChunk.pos + direction * min_distance, tongue.baseChunk.pos + direction * max_distance, tiles_position_list);

                foreach (IntVector2 tile_position in tiles_position_list) { // don't try to grapple too early, i.e. when MiddleOfTile might be already behind
                    Tile tile = room.GetTile(tile_position);
                    Vector2 middle_of_tile = room.MiddleOfTile(tile_position);
                    cost = Mathf.Abs(ideal_distance - Vector2.Distance(tongue.baseChunk.pos + tongue.baseChunk.vel * 3f, middle_of_tile));

                    if ((tile.horizontalBeam || tile.verticalBeam) && cost < local_min_cost) {
                        attach_position = middle_of_tile;
                        local_min_cost = cost;
                    }
                }

                if (!attach_position.HasValue) continue;

                // a bit simpler than vanilla
                cost = deg_modifier * 1.5f;
                if (!prioritize_angle_over_distance) {
                    cost += Mathf.Abs(ideal_distance - Vector2.Distance(tongue.baseChunk.pos + tongue.baseChunk.vel * 3f, attach_position.Value));
                    if (preferred_horizontal_direction != 0) {
                        cost += Mathf.Abs(preferred_horizontal_direction * 90f - (deg + sign * deg_modifier)) * 0.9f;
                    }
                }

                if (cost < min_cost) {
                    best_attach_position = attach_position;
                    best_direction = direction;
                    min_cost = cost;
                }
            }
        }

        if (best_attach_position.HasValue) {
            tongue.worm.playerCheatAttachPos = best_attach_position.Value + Custom.DirVec(best_attach_position.Value, tongue.baseChunk.pos) * 2f;
            return best_direction;
        }
        return null;
    }

    //
    // private
    //

    private static Vector2 Tongue_ProperAutoAim(On.TubeWorm.Tongue.orig_ProperAutoAim orig, TubeWorm.Tongue tongue, Vector2 direction) { // Option_TubeWorm
        if (tongue.worm.grabbedBy.Count == 0) return orig(tongue, direction);
        if (tongue.worm.grabbedBy[0].grabber is not Player player) return orig(tongue, direction);

        Vector2 output = orig(tongue, direction); // updates playerCheatAttachPos
        if (tongue.worm.playerCheatAttachPos.HasValue) return output;

        // priotize up versus left/right in preferredHorizontalDirection
        Vector2? new_output = Tongue_AutoAim_Beams(tongue, direction, prioritize_angle_over_distance: player.input[0].x == 0 && player.input[0].y > 0, preferred_horizontal_direction: direction.y >= 0.9f ? 0 : player.input[0].x);
        if (new_output.HasValue) return new_output.Value;
        return output;
    }

    private static void Tongue_Shoot(On.TubeWorm.Tongue.orig_Shoot orig, TubeWorm.Tongue tongue, Vector2 direction) { // Option_TubeWorm
        if (tongue.worm.grabbedBy.Count == 0 || tongue.worm.grabbedBy[0].grabber is not Player player) {
            orig(tongue, direction);
            return;
        }

        // adept tongue direction to player inputs in some additional cases
        if (player.input[0].x != 0) {
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

    private static void IL_TubeWorm_Update(ILContext context) { // Option_TubeWorm
        // LogAllInstructions(context);
        ILCursor cursor = new(context);

        cursor.Emit(OpCodes.Ldarg_0);
        cursor.EmitDelegate<Func<TubeWorm, bool>>(tubeworm => {
            Player? player = null;
            foreach (Creature.Grasp? grasp in tubeworm.grabbedBy) {
                if (grasp?.grabber is Player player_) {
                    player = player_;
                    break;
                }
            }

            // "call" orig() when returning true;
            if (player == null || player.Get_Attached_Fields() is not Player_Attached_Fields attached_fields) return true;

            if (attached_fields.tubeworm_tongue_needs_to_retract || tubeworm.tongues[0].Attached && player.IsJumpPressed() && (player.IsClimbingOnBeam() || player.CanWallJumpOrMidAirWallJump() || player.bodyMode == BodyModeIndex.CorridorClimb)) {
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

    private static bool TubeWorm_JumpButton(On.TubeWorm.orig_JumpButton orig, TubeWorm tube_worm, Player player) { // Option_TubeWorm
        bool vanilla_result = orig(tube_worm, player);
        if (player.IsClimbingOnBeam() || player.CanWallJumpOrMidAirWallJump() || player.bodyMode == BodyModeIndex.CorridorClimb) {
            tube_worm.useBool = false;
            return player.IsJumpPressed();
        }

        if (player.shortcutDelay > 10) {
            tube_worm.useBool = false;
            return player.IsJumpPressed();
        }
        if (player.Get_Attached_Fields() is not Player_Attached_Fields attached_fields) return vanilla_result;

        // prevents falling off beams and using tongue at the same time
        if (attached_fields.dont_use_tubeworm_counter > 0) {
            tube_worm.useBool = false;
            return player.IsJumpPressed();
        }
        return vanilla_result;
    }
}
