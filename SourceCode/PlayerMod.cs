using Mono.Cecil.Cil;
using MonoMod.Cil;
using MoreSlugcats;
using RWCustom;
using System;
using System.Collections.Generic;
using UnityEngine;
using static Player;
using static Room;
using static SimplifiedMoveset.MainMod;

namespace SimplifiedMoveset;

public static class PlayerMod {
    //
    // parameters
    //

    public static readonly float lean_factor = 1f;

    //
    // variables
    //

    internal static readonly Dictionary<Player, Player_Attached_Fields> _all_attached_fields = new();
    public static Player_Attached_Fields? Get_Attached_Fields(this Player player) {
        _all_attached_fields.TryGetValue(player, out Player_Attached_Fields? attached_fields);
        return attached_fields;
    }

    //
    // main
    //

    internal static void On_Config_Changed() {
        // 
        // prevent hooks from getting added more than once;
        // I can remove them as often as I want without error
        // but I can't add them as often as I want;
        // otherwise they get called multiple times;
        // 

        IL.Player.ClassMechanicsGourmand -= IL_Player_ClassMechanicsGourmand;
        IL.Player.Collide -= IL_Player_Collide;
        IL.Player.Jump -= IL_Player_Jump;
        IL.Player.GrabUpdate -= IL_Player_GrabUpdate;

        IL.Player.GrabVerticalPole -= IL_Player_GrabVerticalPole;
        IL.Player.MovementUpdate -= IL_Player_MovementUpdate;
        IL.Player.SlugSlamConditions -= IL_Player_SlugSlamConditions;
        IL.Player.TerrainImpact -= IL_Player_TerrainImpact;

        IL.Player.TongueUpdate -= IL_Player_TongueUpdate;
        IL.Player.Update -= IL_Player_Update;
        IL.Player.UpdateAnimation -= IL_Player_UpdateAnimation;

        IL.Player.UpdateBodyMode -= IL_Player_UpdateBodyMode;
        IL.Player.WallJump -= IL_Player_WallJump;

        On.Player.checkInput -= Player_CheckInput;
        On.Player.ctor -= Player_Ctor;
        On.Player.Grabability -= Player_Grabability;
        On.Player.GraphicsModuleUpdated -= Player_GraphicsModuleUpdated;

        On.Player.Jump -= Player_Jump;
        On.Player.MovementUpdate -= Player_MovementUpdate;
        On.Player.SaintTongueCheck -= Player_SaintTongueCheck;
        On.Player.TerrainImpact -= Player_TerrainImpact;

        On.Player.ThrowObject -= Player_ThrowObject;
        On.Player.Update -= Player_Update;
        On.Player.UpdateAnimation -= Player_UpdateAnimation;
        On.Player.UpdateBodyMode -= Player_UpdateBodyMode;

        On.Player.UpdateMSC -= Player_UpdateMSC;
        On.Player.WallJump -= Player_WallJump;
        On.Player.Tongue.AutoAim -= Tongue_AutoAim;
        On.Player.Tongue.Shoot -= Tongue_Shoot;

        //
        // add hooks
        //

        IL.Player.UpdateAnimation += IL_Player_UpdateAnimation;

        On.Player.ctor += Player_Ctor; // change stats for swimming
        On.Player.Jump += Player_Jump;
        On.Player.UpdateAnimation += Player_UpdateAnimation;

        if (Option_BeamClimb) {
            IL.Player.GrabVerticalPole += IL_Player_GrabVerticalPole;
            IL.Player.Update += IL_Player_Update;
            On.Player.MovementUpdate += Player_MovementUpdate;
        }

        if (Option_BeamClimb || Option_TubeWorm) {
            On.Player.Update += Player_Update;
        }

        if (Option_BeamClimb || Option_WallJump) {
            // removes lifting your booty when being in a corner with your upper bodyChunk / head
            // usually this happens in one tile horizontal holes
            // but this can also happen when climbing beams and bumping your head into a corner
            // in this situation canceling beam climbing can be spammed
            //
            // grabbing beams by holding down is now implemented here instead of UpdateAnimation()
            IL.Player.MovementUpdate += IL_Player_MovementUpdate;
        }

        if (Option_BellySlide || Option_Crawl || Option_Roll_1 || Option_Roll_2) {
            IL.Player.TerrainImpact += IL_Player_TerrainImpact;
        }

        if (Option_BellySlide || Option_Crawl || Option_Roll_1 || Option_TubeWorm) {
            IL.Player.Jump += IL_Player_Jump;
        }

        if (Option_BellySlide || Option_Gourmand || Option_SpearThrow) {
            On.Player.ThrowObject += Player_ThrowObject;
        }

        if (Option_Crawl || Option_TubeWorm || Option_WallClimb || Option_WallJump) {
            IL.Player.UpdateBodyMode += IL_Player_UpdateBodyMode;
        }

        if (Option_Grab) {
            // only grab dead large creatures when crouching
            On.Player.Grabability += Player_Grabability;
        }

        if (Option_Gourmand) {
            // only exhaust when throwing spears; allow slam using rocket jumps;
            IL.Player.ClassMechanicsGourmand += IL_Player_ClassMechanicsGourmand;
            IL.Player.Collide += IL_Player_Collide;
            IL.Player.SlugSlamConditions += IL_Player_SlugSlamConditions;
        }

        if (Option_SlideTurn) {
            On.Player.UpdateBodyMode += Player_UpdateBodyMode;
        }

        if (Option_StandUp) {
            On.Player.TerrainImpact += Player_TerrainImpact;
        }

        if (Option_Swim) {
            IL.Player.GrabUpdate += IL_Player_GrabUpdate; // can eat stuff underwater
            On.Player.UpdateMSC += Player_UpdateMSC; // don't let MSC reset buoyancy
        }

        if (Option_TubeWorm) {
            IL.Player.TongueUpdate += IL_Player_TongueUpdate;
            On.Player.SaintTongueCheck += Player_SaintTongueCheck;
            On.Player.Tongue.AutoAim += Tongue_AutoAim;
            On.Player.Tongue.Shoot += Tongue_Shoot;
        }

        if (Option_WallClimb || Option_WallJump) {
            IL.Player.WallJump += IL_Player_WallJump;

            // fix cicade lifting up while wall climbing;
            On.Player.GraphicsModuleUpdated += Player_GraphicsModuleUpdated;
        }

        if (Option_WallJump) {
            On.Player.checkInput += Player_CheckInput; // input "buffer" for wall jumping
            On.Player.WallJump += Player_WallJump;
        }
    }

    //
    // public
    //

    // useful as a setup for some animations while on slopes
    public static void AlignPosYOnSlopes(Player? player) {
        if (player == null) return;

        if (player.bodyChunks[0].pos.y < player.bodyChunks[1].pos.y) {
            player.bodyChunks[0].pos.y = player.bodyChunks[1].pos.y;
        }
        player.bodyChunks[0].vel.y += player.dynamicRunSpeed[0];
    }

    public static bool CanWallJumpOrMidAirWallJump(this Player player) => player.canWallJump != 0 || player.animation == AnimationIndex.LedgeGrab || player.bodyMode == BodyModeIndex.WallClimb;

    // the name of the function is a bit ambiguous since one of the animations
    // is called ClimbOnBeam..
    public static bool IsClimbingOnBeam(this Player player) {
        int player_animation = (int)player.animation;
        return (player_animation >= 6 && player_animation <= 12) || player.bodyMode == BodyModeIndex.ClimbingOnBeam;
    }

    public static bool IsJumpPressed(this Player player) => player.input[0].jmp && !player.input[1].jmp;

    public static bool IsTileSolidOrSlope(this Player player, int chunk_index, int relative_x, int relative_y) {
        if (player.room is not Room room) return false;
        if (player.IsTileSolid(chunk_index, relative_x, relative_y)) return true;
        return room.GetTile(room.GetTilePosition(player.bodyChunks[chunk_index].pos) + new IntVector2(relative_x, relative_y)).Terrain == Tile.TerrainType.Slope;
    }

    public static bool IsTongueRetracting(this Player player) {
        if (player.tubeWorm == null) {
            return player.tongue != null && player.tongue.mode == Tongue.Mode.Retracting;
        }

        if (player.tubeWorm.tongues[0].mode == TubeWorm.Tongue.Mode.Retracting) return true;
        if (player.tubeWorm.tongues[0].Attached && player.Get_Attached_Fields() is Player_Attached_Fields attached_fields) {
            // the update for TubeWorm.Tongue is late in some cases;
            // for example: UpdateAnimation() -> TubeWorm.Update() -> Jump();
            // make sure that it is really retracting in the cases where this is used;
            attached_fields.tubeworm_tongue_needs_to_retract = true;
            return true;
        }
        return player.tongue != null && player.tongue.mode == Tongue.Mode.Retracting;
    }

    public static Vector2? Tongue_AutoAim_Beams(Tongue tongue, Vector2 original_dir, bool prioritize_angle_over_distance, int preferred_horizontal_direction) {
        if (tongue.player?.room is not Room room) return null;

        float min_distance = 30f;
        float max_distance = 230f;
        float ideal_distance = tongue.baseIdealRopeLength;

        float deg = Custom.VecToDeg(original_dir);
        float min_cost = float.MaxValue;

        Vector2? best_attach_pos = null;
        Vector2? best_direction = null;

        for (float deg_modifier = 0.0f; deg_modifier < 30f; deg_modifier += 5f) {
            for (float sign = -1f; sign <= 1f; sign += 2f) {
                Vector2? attach_pos = null;
                Vector2 direction = Custom.DegToVec(deg + sign * deg_modifier);

                float local_min_cost = float.MaxValue;
                float cost;
                foreach (IntVector2 int_attach_pos in SharedPhysics.RayTracedTilesArray(tongue.baseChunk.pos + direction * min_distance, tongue.baseChunk.pos + direction * max_distance)) {
                    Tile tile = room.GetTile(int_attach_pos);
                    Vector2 middle_of_tile = room.MiddleOfTile(int_attach_pos);
                    cost = Mathf.Abs(ideal_distance - Vector2.Distance(tongue.baseChunk.pos + tongue.baseChunk.vel * 3f, middle_of_tile));

                    if ((tile.horizontalBeam || tile.verticalBeam) && cost < local_min_cost) {
                        attach_pos = middle_of_tile;
                        local_min_cost = cost;
                    }
                }

                if (!attach_pos.HasValue) continue;

                cost = deg_modifier * 1.5f;
                if (!prioritize_angle_over_distance) {
                    cost += Mathf.Abs(ideal_distance - Vector2.Distance(tongue.baseChunk.pos + tongue.baseChunk.vel * 3f, attach_pos.Value));
                    if (preferred_horizontal_direction != 0) {
                        cost += Mathf.Abs(preferred_horizontal_direction * 90f - (deg + sign * deg_modifier)) * 0.9f;
                    }
                }

                if (cost < min_cost) {
                    // a bit simplified compared to what tubeworm does;
                    best_attach_pos = attach_pos;
                    best_direction = direction;
                    min_cost = cost;
                }
            }
        }

        if (best_attach_pos.HasValue) {
            tongue.AttachToTerrain(best_attach_pos.Value);
            return best_direction;
        }
        return null;
    }

    // direction: up = 1, down = -1
    public static void PrepareGetUpOnBeamAnimation(Player? player, int direction, Player_Attached_Fields attached_fields) {
        // trying to be more robust
        // I had cases where mods would break (but not vanilla) when trying to adjust room loading => annoying to work around
        if (player?.room is not Room room) return;

        int chunk_index = direction == 1 ? 0 : 1;
        player.bodyChunks[1 - chunk_index].pos.x = player.bodyChunks[chunk_index].pos.x;
        room.PlaySound(SoundID.Slugcat_Get_Up_On_Horizontal_Beam, player.mainBodyChunk, false, 1f, 1f);

        Tile tile = room.GetTile(player.bodyChunks[chunk_index].pos + new Vector2(player.flipDirection * 20f, 0.0f));
        if (tile.Terrain == Tile.TerrainType.Solid || !tile.horizontalBeam) {
            player.flipDirection = -player.flipDirection;
        }

        player.animation = AnimationIndex.GetUpOnBeam;
        player.upOnHorizontalBeamPos = new Vector2(player.bodyChunks[chunk_index].pos.x, room.MiddleOfTile(player.bodyChunks[chunk_index].pos).y + direction * 20f);
        attached_fields.get_up_on_beam_direction = direction;
    }

    public static void RocketJump(Player? player, float adrenaline_modifier, float scale = 1f, SoundID? sound_id = null) {
        if (player == null) return;

        BodyChunk body_chunk_0 = player.bodyChunks[0];
        BodyChunk body_chunk_1 = player.bodyChunks[1];
        sound_id ??= SoundID.Slugcat_Rocket_Jump;

        body_chunk_1.vel *= 0.0f;
        body_chunk_1.pos += new Vector2(5f * player.rollDirection, 5f);
        body_chunk_0.pos = body_chunk_1.pos + new Vector2(5f * player.rollDirection, 5f);
        player.animation = AnimationIndex.RocketJump;

        Vector2 velocity = Custom.DegToVec(player.rollDirection * (90f - Mathf.Lerp(30f, 55f, scale))) * Mathf.Lerp(9.5f, 13.1f, scale) * adrenaline_modifier * (player.isSlugpup ? 0.65f : 1f);
        body_chunk_0.vel = velocity;
        body_chunk_1.vel = velocity;

        body_chunk_0.vel.x *= player.isRivulet ? 1.5f : 1f;
        body_chunk_1.vel.x *= player.isRivulet ? 1.5f : 1f;

        if (sound_id == SoundID.None) return;
        if (player.room == null) return;
        player.room.PlaySound(sound_id, player.mainBodyChunk, false, 1f, 1f);
    }

    public static bool SwitchHorizontalToVerticalBeam(Player? player, Player_Attached_Fields attached_fields) {
        if (player?.room is not Room room) return false;

        BodyChunk body_chunk_0 = player.bodyChunks[0];
        bool is_vertical_beam_long_enough = player.input[0].y != 0 && room.GetTile(body_chunk_0.pos + new Vector2(0.0f, 20f * player.input[0].y)).verticalBeam;

        if (room.GetTile(body_chunk_0.pos).verticalBeam && (is_vertical_beam_long_enough || player.input[0].x != 0 && (player.animation == AnimationIndex.HangFromBeam && !room.GetTile(body_chunk_0.pos + new Vector2(20f * player.input[0].x, 0.0f)).horizontalBeam || player.animation == AnimationIndex.StandOnBeam && !room.GetTile(player.bodyChunks[1].pos + new Vector2(20f * player.input[0].x, 0.0f)).horizontalBeam))) {
            // prioritize horizontal over vertical beams when they are one-tile only => only commit in that case when isVerticalBeamLongEnough
            if (!attached_fields.is_switching_beams && (player.input[0].y == 0 || is_vertical_beam_long_enough)) {
                attached_fields.is_switching_beams = true;
                player.flipDirection = body_chunk_0.pos.x >= room.MiddleOfTile(body_chunk_0.pos).x ? 1 : -1;
                player.animation = AnimationIndex.ClimbOnBeam;
                return true;
            }
            return false;
        }

        attached_fields.is_switching_beams = false;
        return false;
    }

    public static bool SwitchVerticalToHorizontalBeam(Player? player, Player_Attached_Fields attached_fields) {
        if (player?.room is Room room) {
            BodyChunk body_chunk_0 = player.bodyChunks[0];
            BodyChunk body_chunk_1 = player.bodyChunks[1];
            Tile tile0 = room.GetTile(body_chunk_0.pos);

            // HangFromBeam
            // prioritize HangFromBeam when at the end of a vertical beam
            // not very clean since isSwitchingBeams is not used
            // BUT switching to vertical beams can be nice to jump further => need a case to fall back; even after having just switched
            if (tile0.horizontalBeam && player.input[0].y != 0 && !room.GetTile(tile0.X, tile0.Y + player.input[0].y).verticalBeam) {
                attached_fields.is_switching_beams = true;
                player.animation = AnimationIndex.HangFromBeam;
                return true;
            }
            // HangFromBeam
            else if (tile0.horizontalBeam && player.input[0].x != 0 && room.GetTile(tile0.X + player.input[0].x, tile0.Y).horizontalBeam) {
                if (!attached_fields.is_switching_beams) {
                    attached_fields.is_switching_beams = true;
                    player.animation = AnimationIndex.HangFromBeam;
                    return true;
                }
            }
            // StandOnBeam
            else if (room.GetTile(body_chunk_1.pos).horizontalBeam && player.input[0].x != 0 && room.GetTile(body_chunk_1.pos + new Vector2(20f * player.input[0].x, 0.0f)).horizontalBeam) { //  || player.input[0].y == -1 && !room.GetTile(body_chunk_1.pos - new Vector2(0.0f, 20f)).verticalBeam
                if (!attached_fields.is_switching_beams) {
                    attached_fields.is_switching_beams = true;
                    player.animation = AnimationIndex.StandOnBeam;
                    return true;
                }
            } else {
                attached_fields.is_switching_beams = false;
            }
        }
        return false;
    }

    //
    //
    //

    public static void UpdateAnimation_BeamTip(Player player, Player_Attached_Fields attached_fields) {
        if (player.room is not Room room) return;

        BodyChunk body_chunk_0 = player.bodyChunks[0];
        BodyChunk body_chunk_1 = player.bodyChunks[1];

        float vel_x_gain = 2.4f * Mathf.Lerp(1f, 1.2f, player.Adrenaline) * player.surfaceFriction * player.EffectiveRoomGravity;
        if (player.slowMovementStun > 0) {
            vel_x_gain *= 0.4f + 0.6f * Mathf.InverseLerp(10f, 0.0f, player.slowMovementStun);
        }

        player.bodyMode = BodyModeIndex.ClimbingOnBeam;
        player.standing = true;
        player.canJump = 5;

        body_chunk_1.pos = (body_chunk_1.pos + room.MiddleOfTile(body_chunk_1.pos)) / 2f;
        body_chunk_1.vel *= 0.5f;

        if (player.IsJumpPressed()) {
            body_chunk_0.vel.x += player.input[0].x * vel_x_gain;
            body_chunk_1.vel.x += player.input[0].x * vel_x_gain;
        } else if (player.input[0].x == 0 && player.input[0].y == -1) {
            // wind can make lining yourself up more difficult // on the other hand, wind makes catching the beam below also harder => leave it as is
            body_chunk_0.pos.x += Mathf.Clamp(body_chunk_1.pos.x - body_chunk_0.pos.x, -vel_x_gain, vel_x_gain);
        } else {
            body_chunk_0.vel.x -= player.input[0].x * (vel_x_gain - lean_factor);
            body_chunk_1.vel.x -= player.input[0].x * (vel_x_gain + lean_factor);
        }

        body_chunk_0.vel.y += 1.5f;
        body_chunk_0.vel.y += player.input[0].y * 0.1f;

        //
        // exits
        //

        if (player.IsJumpPressed() && player.IsTongueRetracting()) return;

        // what does this do?
        if (player.input[0].y > 0 && player.input[1].y == 0) {
            --body_chunk_1.vel.y;
            player.canJump = 0;
            player.animation = AnimationIndex.None;
        }

        if (player.input[0].y == -1 && (body_chunk_0.pos.x == body_chunk_1.pos.x || player.IsJumpPressed())) { // IsPosXAligned(player)
            attached_fields.grab_beam_counter = 15;
            attached_fields.dont_use_tubeworm_counter = 2;
            player.canJump = 0;
            player.animation = AnimationIndex.None;
        } else if (body_chunk_0.pos.y < body_chunk_1.pos.y - 5f || !room.GetTile(body_chunk_1.pos + new Vector2(0.0f, -20f)).verticalBeam) {
            player.animation = AnimationIndex.None;
        }
        return;
    }

    public static void UpdateAnimation_BellySlide(Player player) {
        if (player.room is not Room room) return;

        BodyChunk body_chunk_0 = player.bodyChunks[0];
        BodyChunk body_chunk_1 = player.bodyChunks[1];

        if (player.slideCounter > 0) { // no backflips after belly slide
            player.slideCounter = 0;
        }

        player.allowRoll = 0; // might get set otherwise when sliding fast on slopes or over a small gap // prevent direct transition from belly slide to roll
        player.bodyMode = BodyModeIndex.Default;
        player.standing = false;

        // stop belly slide to get into holes in the ground
        if (player.input[0].y < 0 && player.input[0].downDiagonal == 0 && player.input[0].x == 0 && player.rollCounter > 10 && room.GetTilePosition(body_chunk_0.pos).y == room.GetTilePosition(body_chunk_1.pos).y) {
            IntVector2 tile_position = room.GetTilePosition(player.mainBodyChunk.pos);
            if (!room.GetTile(tile_position + new IntVector2(0, -1)).Solid && room.GetTile(tile_position + new IntVector2(-1, -1)).Solid && room.GetTile(tile_position + new IntVector2(1, -1)).Solid) {
                body_chunk_0.pos = room.MiddleOfTile(body_chunk_0.pos) + new Vector2(0.0f, -20f);
                body_chunk_1.pos = Vector2.Lerp(body_chunk_1.pos, body_chunk_0.pos + new Vector2(0.0f, player.bodyChunkConnections[0].distance), 0.5f);
                body_chunk_0.vel = new Vector2(0.0f, -11f);
                body_chunk_1.vel = new Vector2(0.0f, -11f);

                player.animation = AnimationIndex.None;
                player.GoThroughFloors = true;
                player.rollDirection = 0;
                return;
            }
        }

        // when being close to wall the belly slide might get canceled early;
        // this happens in MovementUpdate(): if (this.goIntoCorridorClimb > 2 && !this.corridorDrop)..
        // as a workaround set goIntoCorridorClimb to zero;
        player.goIntoCorridorClimb = 0;
        player.whiplashJump = player.input[0].x == -player.rollDirection;

        if (player.rollCounter < 6 && !player.isRivulet) {
            body_chunk_1.vel.x -= 9.1f * player.rollDirection;
            body_chunk_1.vel.y += 2f; // default: 2.7f
        } else if (player.IsTileSolidOrSlope(chunk_index: 1, 0, -1) || player.IsTileSolidOrSlope(chunk_index: 1, 0, -2)) {
            body_chunk_1.vel.y -= 3f; // stick better to slopes // default: -0.5f
        }

        if (player.IsTileSolidOrSlope(chunk_index: 0, 0, -1) || player.IsTileSolidOrSlope(chunk_index: 0, 0, -2)) {
            body_chunk_0.vel.y -= 3f; // default: -2.3f
        }

        // this is somewhat odd; I reduced the speed for simplicity in the normal case;
        // using vanilla speed + duration gives an increased distance for some reason;
        // and in most cases but not in all cases (Rivulet); reduced speed + vanilla
        // duration seems to give vanilla distance in most cases;
        float belly_slide_speed = 14f;
        int normal_duation_in_frames = 15; // vanilla: 15
        int long_duration_in_frames = 39; // vanilla: 39

        if (player.isRivulet) {
            belly_slide_speed = 20f;
            normal_duation_in_frames = 17;
        } else if (player.isGourmand) {
            // vanilla: player.gourmandExhausted ? 10f : 40f;
            if (player.gourmandExhausted) {
                belly_slide_speed = 4f;
            }

            normal_duation_in_frames = 30;
            long_duration_in_frames = 78;
        } else if (player.isSlugpup) {
            belly_slide_speed = 7f;
        }

        body_chunk_0.vel.x += belly_slide_speed * player.rollDirection * Mathf.Sin((float)(player.rollCounter / (double)(player.longBellySlide ? long_duration_in_frames : normal_duation_in_frames) * Math.PI));
        foreach (BodyChunk body_chunk in player.bodyChunks) {
            if (body_chunk.contactPoint.y == 0) {
                body_chunk.vel.x *= player.surfaceFriction;
            }
        }

        // finish // abort when mid-air // don't cancel belly slides on slopes
        if (player.rollCounter <= (player.longBellySlide ? long_duration_in_frames : normal_duation_in_frames) && (player.canJump > 0 || player.IsTileSolidOrSlope(chunk_index: 0, 0, -1) || player.IsTileSolidOrSlope(chunk_index: 1, 0, -1))) return;

        player.rollDirection = 0;
        player.animation = AnimationIndex.None;
        player.standing = true;
        player.longBellySlide = false;
    }

    public static void UpdateAnimation_ClimbOnBeam(Player player, Player_Attached_Fields attached_fields) {
        BodyChunk body_chunk_0 = player.bodyChunks[0];
        BodyChunk body_chunk_1 = player.bodyChunks[1];

        player.bodyMode = BodyModeIndex.ClimbingOnBeam;
        player.standing = true;
        player.canJump = 1;

        foreach (BodyChunk body_chunk in player.bodyChunks) {
            if (body_chunk.contactPoint.x != 0) {
                player.flipDirection = -body_chunk.contactPoint.x;
                break;
            }
        }

        bool should_switch_sides = player.IsTileSolid(0, 0, 1) || player.input[0].y <= 0 || (body_chunk_0.contactPoint.y >= 0 && !player.IsTileSolid(0, player.flipDirection, 1));
        if (should_switch_sides && player.IsTileSolid(0, player.flipDirection, 0)) {
            player.flipDirection = -player.flipDirection;
        }

        if (should_switch_sides) {
            body_chunk_0.pos.x = (body_chunk_0.pos.x + player.room.MiddleOfTile(body_chunk_0.pos).x + player.flipDirection * 5f) / 2f;
            body_chunk_1.pos.x = (body_chunk_1.pos.x * 7f + player.room.MiddleOfTile(body_chunk_0.pos).x + player.flipDirection * 5f) / 8f;
        } else {
            body_chunk_0.pos.x = (body_chunk_0.pos.x + player.room.MiddleOfTile(body_chunk_0.pos).x) / 2f;
            body_chunk_1.pos.x = (body_chunk_1.pos.x * 7f + player.room.MiddleOfTile(body_chunk_0.pos).x) / 8f;
        }

        body_chunk_0.vel.x = 0f;
        body_chunk_0.vel.y = 0.5f * body_chunk_0.vel.y + 1f + player.gravity;
        body_chunk_1.vel.y -= 1f - player.gravity;

        if (player.input[0].y > 0) {
            player.animationFrame++;
            if (player.animationFrame > 20) {
                player.animationFrame = 0;
                player.room.PlaySound(SoundID.Slugcat_Climb_Up_Vertical_Beam, player.mainBodyChunk, false, 1f, 1f);
                player.AerobicIncrease(0.1f);
            }
            body_chunk_0.vel.y += Mathf.Lerp(1f, 1.4f, player.Adrenaline) * player.slugcatStats.poleClimbSpeedFac * Custom.LerpMap(player.slowMovementStun, 0f, 10f, 1f, 0.2f);
        } else if (player.input[0].y < 0) {
            body_chunk_0.vel.y -= 2.2f * (0.2f + 0.8f * player.EffectiveRoomGravity);
        }

        if (player.slideUpPole > 0) {
            player.slideUpPole--;
            if (player.slideUpPole > 8) {
                player.animationFrame = 12;
            }
            if (player.slideUpPole == 0) {
                player.slowMovementStun = Math.Max(player.slowMovementStun, 16);
            }
            if (player.slideUpPole > 14) {
                body_chunk_0.pos.y += 2f;
                body_chunk_1.pos.y += 2f;
            }

            body_chunk_0.vel.y += Custom.LerpMap(player.slideUpPole, 17f, 0f, 3f, -1.2f, 0.45f);
            body_chunk_1.vel.y += Custom.LerpMap(player.slideUpPole, 17f, 0f, 1.5f, -1.4f, 0.45f);
        }
        player.GoThroughFloors = player.input[0].x == 0 && player.input[0].downDiagonal == 0;

        // grab other parallel vertical beam
        if (player.input[0].x == player.flipDirection && player.input[1].x == 0 && player.flipDirection == player.lastFlipDirection && player.room.GetTile(player.room.GetTilePosition(body_chunk_0.pos) + new IntVector2(player.flipDirection, 0)).verticalBeam) {
            body_chunk_0.pos.x = player.room.MiddleOfTile(player.room.GetTilePosition(body_chunk_0.pos) + new IntVector2(player.flipDirection, 0)).x - player.flipDirection * 5f;
            player.flipDirection = -player.flipDirection;
            player.jumpStun = 11 * player.flipDirection;
        }

        //
        // exits
        //

        // stand on ground
        if (body_chunk_1.contactPoint.y < 0 && player.input[0].y < 0) {
            player.room.PlaySound(SoundID.Slugcat_Regain_Footing, player.mainBodyChunk, false, 1f, 1f);
            player.animation = AnimationIndex.StandUp;
            player.animationFrame = 0;
            return;
        }

        // switch to horizontal beams
        if (SwitchVerticalToHorizontalBeam(player, attached_fields)) {
            player.animationFrame = 0;
            return;
        }

        // lose grip
        if (player.room.GetTile(body_chunk_0.pos).verticalBeam) return;
        player.animationFrame = 0;

        if (player.room.GetTile(player.room.GetTilePosition(body_chunk_0.pos) + new IntVector2(0, -1)).verticalBeam) {
            player.room.PlaySound(SoundID.Slugcat_Get_Up_On_Top_Of_Vertical_Beam_Tip, player.mainBodyChunk, false, 1f, 1f);
            player.animation = AnimationIndex.GetUpToBeamTip;

            // otherwise it might cancel the GetUpToBeamTip animation before it gets reached;
            player.wantToJump = 0;
            return;
        }

        if (player.room.GetTile(player.room.GetTilePosition(body_chunk_0.pos) + new IntVector2(0, 1)).verticalBeam) {
            player.animation = AnimationIndex.HangUnderVerticalBeam;
            return;
        }
        player.animation = AnimationIndex.None;
    }

    public static void UpdateAnimation_GetUpOnBeam(Player player, Player_Attached_Fields attached_fields) {
        if (player.room is not Room room) return;

        BodyChunk body_chunk_0 = player.bodyChunks[0];
        BodyChunk body_chunk_1 = player.bodyChunks[1];

        // GetUpOnBeam and GetDownOnBeam
        int direction = attached_fields.get_up_on_beam_direction; // -1 (down) or 1 (up)
        int body_chunk_index = direction == 1 ? 1 : 0;

        // otherwise this is bugged when pressing jump during this animation
        // => drops slugcat when StandOnBeam animation is reached;
        player.canJump = 0;

        player.bodyMode = BodyModeIndex.ClimbingOnBeam;
        body_chunk_0.vel.x = 0.0f;
        body_chunk_0.vel.y = 0.0f;

        if (direction == 1) {
            player.forceFeetToHorizontalBeamTile = 20;
        }

        // adjust x direction? // skip // this makes the same animation as with straightUpOnHorizontalBeam = false
        // if (room.GetTile(player.upOnHorizontalBeamPos).Solid)
        // {
        //     for (int index = 1; index >= -1; index -= 2)
        //     {
        //         if (!room.GetTile(player.upOnHorizontalBeamPos + new Vector2(player.flipDirection * index * 20f, 0.0f)).Solid)
        //         {
        //             player.upOnHorizontalBeamPos.x += player.flipDirection * index * 20f;
        //             break;
        //         }
        //     }
        // }

        body_chunk_0.vel += Custom.DirVec(body_chunk_0.pos, player.upOnHorizontalBeamPos) * 1.8f;
        body_chunk_1.vel += Custom.DirVec(body_chunk_1.pos, player.upOnHorizontalBeamPos + new Vector2(0.0f, -20f)) * 1.8f;

        // ----- //
        // exits //
        // ----- //

        if (room.GetTile(player.bodyChunks[body_chunk_index].pos).horizontalBeam && Math.Abs(player.bodyChunks[body_chunk_index].pos.y - player.upOnHorizontalBeamPos.y) < 25.0) {
            // this might be helpful when horizontal beams are stacked vertically;
            // however, this can lead to a bug where you are not able to grab beams after jumping off;
            // => reduce this counter as a workaround;
            player.noGrabCounter = 5; // vanilla: 15

            player.animation = direction == 1 ? AnimationIndex.StandOnBeam : AnimationIndex.HangFromBeam;
            player.bodyChunks[body_chunk_index].pos.y = room.MiddleOfTile(player.bodyChunks[body_chunk_index].pos).y + direction * 5f;
            player.bodyChunks[body_chunk_index].vel.y = 0.0f;
            return;
        }

        // revert when bumping into something or pressing the opposite direction
        if (player.input[0].y == -direction) {
            player.upOnHorizontalBeamPos -= direction * new Vector2(0.0f, 20f);
            attached_fields.get_up_on_beam_direction = -direction;
            return;
        } else if (body_chunk_0.contactPoint.y == direction || body_chunk_1.contactPoint.y == direction) {
            if (attached_fields.get_up_on_beam_abort_counter > 0) { // revert to the original position should always work // abort if stuck in a loop just in case
                attached_fields.grab_beam_counter = 15;
                player.animation = AnimationIndex.None;
                return;
            } else {
                attached_fields.get_up_on_beam_abort_counter = 2;
            }

            player.upOnHorizontalBeamPos -= direction * new Vector2(0.0f, 20f);
            attached_fields.get_up_on_beam_direction = -direction;
            return;
        }

        if ((room.GetTile(body_chunk_0.pos).horizontalBeam || room.GetTile(body_chunk_1.pos).horizontalBeam) && Custom.DistLess(player.bodyChunks[1 - body_chunk_index].pos, player.upOnHorizontalBeamPos, 30f)) return; // default: 25f
        player.animation = AnimationIndex.None;
    }

    public static void UpdateAnimation_HangFromBeam(Player player, Player_Attached_Fields attached_fields) {
        if (player.room is not Room room) return;

        BodyChunk body_chunk_0 = player.bodyChunks[0];
        BodyChunk body_chunk_1 = player.bodyChunks[1];

        player.bodyMode = BodyModeIndex.ClimbingOnBeam;
        player.standing = true;

        // prevent additional momentum from wall jumps
        player.canWallJump = 0;

        body_chunk_0.vel.x *= 0.2f;
        body_chunk_0.vel.y = 0.0f;
        body_chunk_0.pos.y = room.MiddleOfTile(body_chunk_0.pos).y;

        if (player.input[0].x != 0 && body_chunk_0.contactPoint.x != player.input[0].x) {
            Tile tile = room.GetTile(body_chunk_0.pos + new Vector2(12f * player.input[0].x, 0.0f));
            if (tile.horizontalBeam) {
                if (body_chunk_1.contactPoint.x != player.input[0].x) {
                    body_chunk_0.vel.x += player.input[0].x * Mathf.Lerp(1.2f, 1.4f, player.Adrenaline) * player.slugcatStats.poleClimbSpeedFac * Custom.LerpMap(player.slowMovementStun, 0.0f, 10f, 1f, 0.5f);
                }

                body_chunk_1.vel.x += player.flipDirection * (0.5f + 0.5f * Mathf.Sin((float)(player.animationFrame / 20.0 * Mathf.PI * 2.0))) * -0.5f;
                ++player.animationFrame;

                if (player.animationFrame > 20) {
                    player.animationFrame = 1;
                    room.PlaySound(SoundID.Slugcat_Climb_Along_Horizontal_Beam, player.mainBodyChunk, false, 1f, 1f);
                    player.AerobicIncrease(0.05f);
                }
            } else if (!tile.Solid && player.input[1].y != 1) { // stop at end of horizontal beam // leaning
                body_chunk_0.pos.x = room.MiddleOfTile(body_chunk_0.pos).x;
                body_chunk_0.vel.x -= lean_factor * player.input[0].x;
                body_chunk_1.vel.x += lean_factor * player.input[0].x;
            }
        } else if (player.animationFrame < 10) {
            ++player.animationFrame;
        } else if (player.animationFrame > 10) {
            --player.animationFrame;
        }

        // ----- //
        // exits //
        // ----- //

        // stand on ground
        // don't exit animation when leaving corridor with beam horizontally
        if (body_chunk_1.contactPoint.y < 0 && player.input[0].y < 0 && Mathf.Abs(body_chunk_0.pos.x - body_chunk_1.pos.x) < 5f) {
            player.room.PlaySound(SoundID.Slugcat_Regain_Footing, player.mainBodyChunk, false, 1f, 1f);
            player.animation = AnimationIndex.StandUp;
            player.animationFrame = 0;
            return;
        }

        if (SwitchHorizontalToVerticalBeam(player, attached_fields)) {
            player.animationFrame = 0;
            return;// grab vertical beam if possible
        }

        if (player.IsJumpPressed()) {
            // retract tubeworm first // consistent behavior with when standing on beam and pressing jump
            if (IsTongueRetracting(player)) return;

            if (player.input[0].y == 1) { // only drop when pressing jump without holding up
                PrepareGetUpOnBeamAnimation(player, 1, attached_fields);
                player.animationFrame = 0;
                return;
            } else if (player.input[0].y == -1 && player.IsTileSolid(1, 0, -1)) {
                // this case would lead to jumping + regrabbing beam otherwise
                // not clean..
                player.input[1].jmp = true;
                // player.canJump = 0;
            }

            attached_fields.dont_use_tubeworm_counter = 2; // don't drop and shoot tubeworm at the same time
            attached_fields.grab_beam_cooldown_position = body_chunk_0.pos;
            player.animation = AnimationIndex.None;
            player.animationFrame = 0;
            return;
        } else if (player.input[0].y == 1 && player.input[1].y == 0) {
            PrepareGetUpOnBeamAnimation(player, 1, attached_fields);
            player.animationFrame = 0;
            return;
        }

        if (room.GetTile(body_chunk_0.pos).horizontalBeam) return;
        player.animation = AnimationIndex.None;
    }

    public static void UpdateAnimation_HangUnderVerticalBeam(Player player, Player_Attached_Fields attached_fields) {
        if (player.room is not Room room) return;

        BodyChunk body_chunk_0 = player.bodyChunks[0];
        BodyChunk body_chunk_1 = player.bodyChunks[1];

        float vel_x_gain = 2.4f * Mathf.Lerp(1f, 1.2f, player.Adrenaline) * player.surfaceFriction * player.EffectiveRoomGravity;
        if (player.slowMovementStun > 0) {
            vel_x_gain *= 0.4f + 0.6f * Mathf.InverseLerp(10f, 0.0f, player.slowMovementStun);
        }

        player.bodyMode = BodyModeIndex.ClimbingOnBeam; // gets updated and is default afterwards
        player.standing = false;

        //
        // exits
        //

        if (player.IsJumpPressed() && player.IsTongueRetracting()) return;

        // drop when pressing jump
        if (player.IsJumpPressed() || body_chunk_1.vel.magnitude > 10.0 || body_chunk_0.vel.magnitude > 10.0 || !room.GetTile(body_chunk_0.pos + new Vector2(0.0f, 20f)).verticalBeam) {
            attached_fields.dont_use_tubeworm_counter = 2;
            player.animation = AnimationIndex.None;
            player.standing = true;
            return;
        }

        body_chunk_0.pos.x = Mathf.Lerp(body_chunk_0.pos.x, room.MiddleOfTile(body_chunk_0.pos).x, 0.5f);
        body_chunk_0.pos.y = Mathf.Max(body_chunk_0.pos.y, room.MiddleOfTile(body_chunk_0.pos).y + 5f + body_chunk_0.vel.y);

        body_chunk_0.vel.x *= 0.5f; // dont kill all momentum
        body_chunk_0.vel.x -= player.input[0].x * (vel_x_gain + lean_factor);
        body_chunk_0.vel.y *= 0.5f;
        body_chunk_1.vel.x -= player.input[0].x * (vel_x_gain - lean_factor);

        if (player.input[0].y > 0) {
            body_chunk_0.vel.y += 2.5f;
        }

        if (!room.GetTile(body_chunk_0.pos).verticalBeam) return;
        player.animation = AnimationIndex.ClimbOnBeam;
    }

    public static void UpdateAnimation_StandOnBeam(Player player, Player_Attached_Fields attached_fields) {
        if (player.room is not Room room) return;

        // bool isWallClimbing = player.bodyMode == BodyModeIndex.WallClimb && body_chunk_1.contactPoint.x != 0;
        BodyChunk body_chunk_0 = player.bodyChunks[0];
        BodyChunk body_chunk_1 = player.bodyChunks[1];

        // velocity gained in x direction each frame (if no slowmovementstun, and bodyMode is default)
        float vel_x_gain = 2.4f * Mathf.Lerp(1f, 1.2f, player.Adrenaline) * player.surfaceFriction * player.EffectiveRoomGravity;
        if (player.slowMovementStun > 0) {
            vel_x_gain *= 0.4f + 0.6f * Mathf.InverseLerp(10f, 0.0f, player.slowMovementStun);
        }

        player.bodyMode = BodyModeIndex.ClimbingOnBeam;
        player.standing = true;
        player.canJump = 5;
        body_chunk_1.vel.x *= 0.5f;

        // prevent additional momentum from wall jumps
        player.canWallJump = 0;

        if (player.input[0].x != 0 && body_chunk_1.contactPoint.x != player.input[0].x) {
            Tile tile = room.GetTile(body_chunk_1.pos + new Vector2(12f * player.input[0].x, 0.0f));
            if (tile.horizontalBeam) {
                // run normally (like when on ground) with reduced(?) player.dynamicRunSpeed
                ++player.animationFrame;
                if (player.animationFrame > 6) {
                    player.animationFrame = 0;
                    room.PlaySound(SoundID.Slugcat_Walk_On_Horizontal_Beam, player.mainBodyChunk, false, 1f, 1f);
                }
            } else if (!tile.Solid) {
                if (player.input[1].y != -1) { // leaning
                    if (player.IsJumpPressed()) { // jump from leaning
                        body_chunk_0.vel.x += player.input[0].x * vel_x_gain; // player.dynamicRunSpeed[0];
                        body_chunk_1.vel.x += player.input[0].x * vel_x_gain;
                    } else {
                        body_chunk_1.pos.x = room.MiddleOfTile(body_chunk_1.pos).x;
                        body_chunk_0.vel.x -= player.input[0].x * (vel_x_gain - lean_factor);
                        body_chunk_1.vel.x -= player.input[0].x * (vel_x_gain + lean_factor);
                    }
                } else { // stop at the end of horizontal beam
                    body_chunk_1.pos.x = room.MiddleOfTile(body_chunk_1.pos).x;
                    body_chunk_0.vel.x -= player.input[0].x * vel_x_gain;
                    body_chunk_1.vel.x -= player.input[0].x * vel_x_gain;
                }
            }
        } else if (player.animationFrame > 0) {
            player.animationFrame = 0;
        }

        // ----- //
        // exits //
        // ----- //

        // grab vertical beam if possible
        if (SwitchHorizontalToVerticalBeam(player, attached_fields)) {
            player.animationFrame = 0;
            return;
        }

        // if (isWallClimbing)
        // {
        //     // player.animation = AnimationIndex.None;
        //     // return;
        // }

        if (body_chunk_0.contactPoint.y < 1 || !player.IsTileSolid(bChunk: 1, 0, 1)) {
            body_chunk_1.vel.y = 0.0f;
            body_chunk_1.pos.y = room.MiddleOfTile(body_chunk_1.pos).y + 5f;
            body_chunk_0.vel.y += 2f;

            player.dynamicRunSpeed[0] = 2.1f * player.slugcatStats.runspeedFac;
            player.dynamicRunSpeed[1] = 2.1f * player.slugcatStats.runspeedFac;
        } else {
            // stop moving forward when bumping your "head" into something
            body_chunk_0.vel.x -= player.input[0].x * vel_x_gain;
            body_chunk_1.vel.x -= player.input[0].x * vel_x_gain;
        }

        // move down to HangFromBeam
        if (player.input[0].y == -1 && (player.input[1].y == 0 || player.IsJumpPressed())) {
            PrepareGetUpOnBeamAnimation(player, -1, attached_fields);
            player.animationFrame = 0;
            return;
        }

        // grab nearby horizontal beams
        if (player.input[0].y < 1) return;
        if (player.input[1].y != 0) return;
        if (!room.GetTile(room.GetTilePosition(body_chunk_0.pos) + new IntVector2(0, 1)).horizontalBeam) return;

        body_chunk_0.pos.y += 8f;
        body_chunk_1.pos.y += 8f;
        player.animation = AnimationIndex.HangFromBeam;
        player.animationFrame = 0;
    }

    public static void UpdateBodyMode_Crawl(Player player) {
        if (player.room is not Room room) return;

        BodyChunk body_chunk_0 = player.bodyChunks[0];
        BodyChunk body_chunk_1 = player.bodyChunks[1];
        player.dynamicRunSpeed[0] = 2.5f;

        // I want to prevent a crawl turn on ledges;
        // not sure if the check for solid tiles are enough;
        // seems like it works;
        if (player.input[0].x != 0 && player.input[0].x > 0 == body_chunk_0.pos.x < body_chunk_1.pos.x && player.crawlTurnDelay > 5 && !player.IsTileSolid(0, 0, 1) && !player.IsTileSolid(1, 0, 1) && player.IsTileSolidOrSlope(1, 0, -1)) { //  && player.IsTileSolidOrSlope(1, player.input[0].x, -1)
            AlignPosYOnSlopes(player);
            player.dynamicRunSpeed[0] *= 0.5f; // default: 0.75f
            player.crawlTurnDelay = 0;
            player.animation = AnimationIndex.CrawlTurn;
        }
        player.dynamicRunSpeed[1] = player.dynamicRunSpeed[0];

        if (!player.standing) {
            foreach (BodyChunk body_chunk in player.bodyChunks) {
                // this makes you less floaty when crawl turning;
                // in vanilla this is only applied when on slopes;
                // this messes up the Hunter start script
                // => change the super launch jump in order to cancel this out;
                if (body_chunk.contactPoint.y != -1) continue;
                // if (body_chunk.onSlope == 0) continue;
                body_chunk.vel.y -= 1.5f;
            }
        }
        // more requirements than vanilla // prevent collision and sound spam
        else if ((body_chunk_1.onSlope == 0 || player.input[0].x != -body_chunk_1.onSlope) && (player.lowerBodyFramesOnGround >= 3 || body_chunk_1.contactPoint.y < 0 && room.GetTile(room.GetTilePosition(body_chunk_1.pos) + new IntVector2(0, -1)).Terrain != Tile.TerrainType.Air && room.GetTile(room.GetTilePosition(body_chunk_0.pos) + new IntVector2(0, -1)).Terrain != Tile.TerrainType.Air)) {
            AlignPosYOnSlopes(player);
            room.PlaySound(SoundID.Slugcat_Stand_Up, player.mainBodyChunk);
            player.animation = AnimationIndex.StandUp;

            if (player.input[0].x == 0) {
                if (body_chunk_1.contactPoint.y == -1 && player.IsTileSolid(1, 0, -1) && !player.IsTileSolid(1, 0, 1)) {
                    player.feetStuckPos = new Vector2?(room.MiddleOfTile(room.GetTilePosition(body_chunk_1.pos)) + new Vector2(0.0f, body_chunk_1.rad - 10f));
                } else if (body_chunk_0.contactPoint.y == -1 && player.IsTileSolid(0, 0, -1) && !player.IsTileSolid(0, 0, 1)) {
                    player.feetStuckPos = new Vector2?(body_chunk_0.pos + new Vector2(0.0f, -1f));
                }
            }

            // otherwise the animationFrame might not get updated correctly
            // and PlayerGraphics might look for sprites that don't exist;
            // I think this happens more the other way around;
            // other animations have higher animationFrames and when they don't reset
            // the element LegsACrawlingX is not found with X > 5;
            player.animationFrame = 0;
            return;
        }

        if (body_chunk_0.contactPoint.y > -1 && player.input[0].x != 0 && body_chunk_1.pos.y < body_chunk_0.pos.y - 3.0 && body_chunk_1.contactPoint.x == player.input[0].x) {
            ++body_chunk_1.pos.y;
        }

        if (player.input[0].y < 0) {
            player.GoThroughFloors = true;
            for (int chunk_index = 0; chunk_index < 2; ++chunk_index) {
                if (!player.IsTileSolidOrSlope(chunk_index, 0, -1) && (player.IsTileSolidOrSlope(chunk_index, -1, -1) || player.IsTileSolidOrSlope(chunk_index, 1, -1))) { // push into shortcuts and holes but don't stand still on slopes
                    BodyChunk body_chunk = player.bodyChunks[chunk_index];
                    body_chunk.vel.x = 0.8f * body_chunk.vel.x + 0.4f * (room.MiddleOfTile(body_chunk.pos).x - body_chunk.pos.x);
                    --body_chunk.vel.y;
                    break;
                }
            }
        }

        if (player.input[0].x != 0 && Mathf.Abs(body_chunk_1.pos.x - body_chunk_1.lastPos.x) > 0.5) {
            ++player.animationFrame;
        } else {
            player.animationFrame = 0;
        }

        if (player.animationFrame <= 10) return;
        player.animationFrame = 0;
        room.PlaySound(SoundID.Slugcat_Crawling_Step, player.mainBodyChunk);
    }

    public static void UpdateBodyMode_WallClimb(Player player) {
        BodyChunk body_chunk_0 = player.bodyChunks[0];
        BodyChunk body_chunk_1 = player.bodyChunks[1];

        player.canJump = 1;
        player.standing = true;

        // don't climb on one-tile "walls" instead of crawling (for example)
        if (body_chunk_1.contactPoint.x == 0 && body_chunk_1.contactPoint.y == -1) {
            player.animation = AnimationIndex.StandUp;
            player.animationFrame = 0;
            return;
        }

        if (player.input[0].x != 0) {
            // bodyMode would change when player.input[0].x != body_chunk_0.contactPoint.x // skip this check for now
            player.canWallJump = player.IsClimbingOnBeam() ? 0 : player.input[0].x * -15;

            // when upside down, flip instead of climbing
            if (body_chunk_0.pos.y < body_chunk_1.pos.y) {
                body_chunk_0.vel.y = Custom.LerpAndTick(body_chunk_0.vel.y, 2f * player.gravity, 0.8f, 1f);
                body_chunk_1.vel.y = Custom.LerpAndTick(body_chunk_1.vel.y, 0.0f, 0.8f, 1f);
                body_chunk_1.vel.x = -player.input[0].x * 5f;
            } else {
                float vel_x_gain = 2.4f * Mathf.Lerp(1f, 1.2f, player.Adrenaline) * player.surfaceFriction;
                if (player.slowMovementStun > 0) {
                    vel_x_gain *= 0.4f + 0.6f * Mathf.InverseLerp(10f, 0.0f, player.slowMovementStun);
                }

                if (Option_WallClimb && player.input[0].y != 0) {
                    if (player.input[0].y == 1 && !player.IsTileSolid(bChunk: 1, player.input[0].x, 0) && (body_chunk_1.pos.x < body_chunk_0.pos.x) == (player.input[0].x < 0)) { // climb up even when lower body part is hanging in the air
                        body_chunk_0.pos.y += Mathf.Abs(body_chunk_0.pos.x - body_chunk_1.pos.x);
                        body_chunk_1.pos.x = body_chunk_0.pos.x;
                        body_chunk_1.vel.x = -player.input[0].x * vel_x_gain;
                    }

                    body_chunk_0.vel.y += player.gravity;
                    body_chunk_1.vel.y += player.gravity;

                    // downward momentum when ContactPoint.x != 0 is limited to -player.gravity bc of Update()
                    body_chunk_0.vel.y = Mathf.Lerp(body_chunk_0.vel.y, player.input[0].y * 2.5f, 0.3f);
                    body_chunk_1.vel.y = Mathf.Lerp(body_chunk_1.vel.y, player.input[0].y * 2.5f, 0.3f);
                    ++player.animationFrame;
                } else if (player.lowerBodyFramesOffGround > 8 && player.input[0].y != -1) { // stay in place // don't slide down // when only Option_WallClimb is enabled then this happens even when holding up // don't slide/climb when doing a normal jump off the ground
                    if (player.grasps[0]?.grabbed is Cicada cicada) {
                        body_chunk_0.vel.y = Custom.LerpAndTick(body_chunk_0.vel.y, player.gravity - cicada.LiftPlayerPower * 0.5f, 0.3f, 1f);
                    } else {
                        body_chunk_0.vel.y = Custom.LerpAndTick(body_chunk_0.vel.y, player.gravity, 0.3f, 1f);
                    }
                    body_chunk_1.vel.y = Custom.LerpAndTick(body_chunk_1.vel.y, player.gravity, 0.3f, 1f);

                    if (!player.IsTileSolid(bChunk: 1, player.input[0].x, 0) && player.input[0].x > 0 == body_chunk_1.pos.x > body_chunk_0.pos.x) {
                        body_chunk_1.vel.x = -player.input[0].x * vel_x_gain;
                    }
                }
            }
        }

        if (player.slideLoop != null && player.slideLoop.volume > 0.0f) {
            player.slideLoop.volume = 0.0f;
        }
        body_chunk_1.vel.y += body_chunk_1.submersion * player.EffectiveRoomGravity;

        if (player.animationFrame <= 20) return;
        player.room?.PlaySound(SoundID.Slugcat_Crawling_Step, player.mainBodyChunk);
        player.animationFrame = 0;
    }

    public static bool WallJump(Player player, Player_Attached_Fields attached_fields, int direction) {
        // "call" orig() by returning true;
        if (player.room is not Room room) return true;

        // I think this was to prevent glitching hands when jumping off walls;
        // hand animation is only used for wall climb;
        if (Option_WallClimb) {
            attached_fields.initialize_hands = true;
        }

        if (!Option_WallJump) return true;

        BodyChunk body_chunk_0 = player.bodyChunks[0];
        BodyChunk body_chunk_1 = player.bodyChunks[1];

        // climb on smaller obstacles instead
        if (player.input[0].x != 0 && body_chunk_1.contactPoint.x == player.input[0].x && player.IsTileSolid(0, player.input[0].x, -1) && !player.IsTileSolid(0, player.input[0].x, 0)) {
            player.simulateHoldJumpButton = 0;
            return false;
        }

        // jump to be able to climb on smaller obstacles
        if (player.input[0].x != 0 && body_chunk_0.contactPoint.x == player.input[0].x && player.IsTileSolid(0, player.input[0].x, 0) && !player.IsTileSolid(0, player.input[0].x, 1)) {
            float adrenaline_modifier = Mathf.Lerp(1f, 1.15f, player.Adrenaline);
            if (player.exhausted) {
                adrenaline_modifier *= 1f - 0.5f * player.aerobicLevel;
            }

            body_chunk_0.pos.y += 10f * Mathf.Min(1f, adrenaline_modifier);
            body_chunk_1.pos.y += 10f * Mathf.Min(1f, adrenaline_modifier);
            body_chunk_0.vel.y = 4f * adrenaline_modifier;
            body_chunk_1.vel.y = 3.5f * adrenaline_modifier;

            player.simulateHoldJumpButton = 0;
            return false;
        }

        IntVector2 body_chunk_tile_position = room.GetTilePosition(body_chunk_1.pos);
        Tile body_chunk_tile = room.GetTile(body_chunk_tile_position);
        Tile ground_tile = room.GetTile(body_chunk_tile_position - new IntVector2(0, 1));

        // normal jump off the ground // not exactly the same as in jump // but the same as in vanilla code // only changed conditions
        if (ground_tile.Solid || ground_tile.Terrain == Tile.TerrainType.Slope || ground_tile.Terrain == Tile.TerrainType.Floor || body_chunk_tile.WaterSurface || ground_tile.WaterSurface) { // ||  body_chunk_1.submersion > 0.1 // bodyChunkTile.horizontalBeam || groundTile.horizontalBeam ||
            if (player.PainJumps && player.grasps[0]?.grabbed is not Yeek) {
                player.gourmandExhausted = true;
                player.aerobicLevel = 1f;
            }

            float adrenaline_modifier = Mathf.Lerp(1f, 1.15f, player.Adrenaline);
            if (player.exhausted) {
                adrenaline_modifier *= 1f - 0.5f * player.aerobicLevel;
            }

            body_chunk_0.pos.y += 10f * Mathf.Min(1f, adrenaline_modifier);
            body_chunk_1.pos.y += 10f * Mathf.Min(1f, adrenaline_modifier);
            body_chunk_0.vel.y = (player.isRivulet ? 9f : 8f) * adrenaline_modifier;
            body_chunk_1.vel.y = (player.isRivulet ? 8f : 7f) * adrenaline_modifier;

            room.PlaySound(SoundID.Slugcat_Normal_Jump, player.mainBodyChunk, false, 1f, 1f);
            player.jumpBoost = 0.0f;
            player.simulateHoldJumpButton = 0;
            return false;
        }

        // don't jump off the wall while climbing;
        // x input direction == wall jump direction;
        return player.input[0].x == 0 || direction > 0 == player.input[0].x > 0;
    }

    //
    // private
    //

    private static void IL_Player_ClassMechanicsGourmand(ILContext context) {
        // LogAllInstructions(context);
        ILCursor cursor = new(context);

        if (cursor.TryGotoNext(instruction => instruction.MatchLdcI4(1)) &&
            cursor.TryGotoNext(instruction => instruction.MatchStfld<Player>("gourmandExhausted"))) {
            if (can_log_il_hooks) {
                Debug.Log(mod_id + ": IL_Player_ClassMechanicsGourmand: Index " + cursor.Index);
            }

            // don't exhaust from aerobicLevel;
            cursor.Goto(cursor.Index - 12);
            cursor.RemoveRange(13);
        } else {
            if (can_log_il_hooks) {
                Debug.Log(mod_id + ": IL_Player_ClassMechanicsGourmand could not be applied.");
            }
            return;
        }
        // LogAllInstructions(context);
    }

    private static void IL_Player_Collide(ILContext context) {
        // LogAllInstructions(context);
        ILCursor cursor = new(context);

        if (cursor.TryGotoNext(instruction => instruction.MatchLdsfld<AnimationIndex>("Roll")) &&
            cursor.TryGotoNext(instruction => instruction.MatchLdsfld<AnimationIndex>("None"))) {
            if (can_log_il_hooks) {
                Debug.Log(mod_id + ": IL_Player_Collide: Index " + cursor.Index);
            }

            // reduce damage and stun duration because rolls are easier to start with
            // Option_Crawl; same damage and stun values as for slides and rocket jumps;
            // maybe setting damage to zero is better since these moves are not so
            // involved to do;
            cursor.Goto(cursor.Index - 3);
            cursor.Prev.Operand = 0f;
            cursor.Next.Operand = 50f;

            // interrupt Gourmand's roll attack by standing up; otherwise a crawl turn
            // might start another roll instantly => more damage + stun than intended;
            cursor.Goto(cursor.Index + 2);
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.EmitDelegate<Action<Player>>(player => player.standing = true);
        } else {
            if (can_log_il_hooks) {
                Debug.Log(mod_id + ": IL_Player_Collide could not be applied.");
            }
            return;
        }

        if (cursor.TryGotoNext(instruction => instruction.MatchLdsfld<SoundID>("Big_Needle_Worm_Impale_Terrain"))) {
            if (can_log_il_hooks) {
                Debug.Log(mod_id + ": IL_Player_Collide: Index " + cursor.Index);
            }

            // I find the Big_Needle_Worm_Impale_Terrain sound somewhat annoying; 
            // replace with normal terrain impact sound;
            cursor.RemoveRange(1);
            cursor.Emit<SoundID>(OpCodes.Ldsfld, "Slugcat_Terrain_Impact_Medium");
            cursor.Goto(cursor.Index + 3);
            cursor.Next.Operand = 1f; // volume;
        } else {
            if (can_log_il_hooks) {
                Debug.Log(mod_id + ": IL_Player_Collide could not be applied.");
            }
            return;
        }

        object? damage_variable_id = null;
        if (cursor.TryGotoNext(instruction => instruction.MatchLdsfld<AnimationIndex>("RocketJump"))) {
            if (can_log_il_hooks) {
                Debug.Log(mod_id + ": IL_Player_Collide: Index " + cursor.Index);
            }

            // interrupt Gourmand's new rocket jump attack; otherwise it is spammed
            // and can kill larger creature by stun locking;
            cursor.Goto(cursor.Index + 3);
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.Emit(OpCodes.Ldarg_1);

            cursor.EmitDelegate<Action<Player, PhysicalObject>>((player, other_object) => {
                if (player.animation != AnimationIndex.RocketJump) return;
                player.animation = AnimationIndex.None;
                if (!Option_StandUp) return;
                player.standing = true;
            });

            // set damage to zero because these moves can be spammed;
            cursor.Next.Operand = 0f;
            damage_variable_id = cursor.Next.Next.Operand;
        } else {
            if (can_log_il_hooks) {
                Debug.Log(mod_id + ": IL_Player_Collide could not be applied.");
            }
            return;
        }

        if (damage_variable_id != null && cursor.TryGotoNext(instruction => instruction.MatchLdsfld<SoundID>("Big_Needle_Worm_Impale_Terrain"))) {
            if (can_log_il_hooks) {
                Debug.Log(mod_id + ": IL_Player_Collide: Index " + cursor.Index);
            }

            // I find the Big_Needle_Worm_Impale_Terrain sound somewhat annoying; 
            // replace with normal terrain impact sound;
            cursor.RemoveRange(1);
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.Emit(OpCodes.Ldloc, damage_variable_id);

            cursor.EmitDelegate<Func<Player, float, SoundID>>((player, damage) => {
                if (damage <= 0f) return SoundID.Slugcat_Terrain_Impact_Medium;
                return SoundID.Big_Needle_Worm_Impale_Terrain; // vanilla case;
            });

            cursor.Goto(cursor.Index + 3);
            cursor.Next.Operand = 1f; // volume;
        } else {
            if (can_log_il_hooks) {
                Debug.Log(mod_id + ": IL_Player_Collide could not be applied.");
            }
            return;
        }
        // LogAllInstructions(context);
    }

    private static void IL_Player_GrabUpdate(ILContext context) { // Option_Swim
        // LogAllInstructions(context);
        ILCursor cursor = new(context);

        // allow to eat underwater
        // change player.mainBodyChunk.submersion < 0.5f to < 2f => always true

        if (cursor.TryGotoNext(MoveType.After, instruction => instruction.MatchCallvirt<BodyChunk>("get_submersion"))) { // 87
            if (can_log_il_hooks) {
                Debug.Log(mod_id + ": IL_Player_GrabUpdate: Index " + cursor.Index);
            }

            cursor.Next.OpCode = OpCodes.Ldc_R4;
            cursor.Next.Operand = 2f;
        } else {
            if (can_log_il_hooks) {
                Debug.Log(mod_id + ": IL_Player_GrabUpdate could not be applied.");
            }
            return;
        }
        // LogAllInstructions(context);
    }

    private static void IL_Player_GrabVerticalPole(ILContext context) { // Option_BeamClimb
        // this makes grabbing vertical poles less sensitive;
        // in vanilla when standing still you reach vertical
        // poles further;
        // this changes this such that you never reach further;

        // LogAllInstructions(context);
        ILCursor cursor = new(context);

        if (cursor.TryGotoNext(
            instruction => instruction.MatchCeq(),
            instruction => instruction.MatchBr(out _)
        )) {
            if (can_log_il_hooks) {
                Debug.Log(mod_id + ": IL_Player_GrabVerticalPole: Index " + cursor.Index);
            }
            cursor.Goto(cursor.Index + 1);
            cursor.Emit(OpCodes.Pop);
            cursor.Emit(OpCodes.Ldc_I4_0);
        } else {
            if (can_log_il_hooks) {
                Debug.Log(mod_id + ": IL_Player_GrabVerticalPole could not be applied.");
            }
            return;
        }
        // LogAllInstructions(context);
    }

    private static void IL_Player_Jump(ILContext context) { // Option_BellySlide // Option_Crawl // Option_Roll_1 // Option_TubeWorm
        // LogAllInstructions(context);
        ILCursor cursor = new(context);

        if (Option_BellySlide) {
            // don't do a rocket jump out of shortcuts;

            cursor.Emit(OpCodes.Ldarg_0);
            cursor.EmitDelegate<Func<Player, bool>>(player => player.shortcutDelay > 14 && player.animation == AnimationIndex.BellySlide);

            ILLabel label = cursor.DefineLabel();
            cursor.Emit(OpCodes.Brfalse, label);
            cursor = cursor.Emit(OpCodes.Ret);
            cursor.MarkLabel(label);
        }

        if (Option_TubeWorm) {
            // prioritize retracting over jumping off beams;

            cursor.Emit(OpCodes.Ldarg_0);
            cursor.EmitDelegate<Func<Player, bool>>(player => (player.IsClimbingOnBeam() || player.bodyMode == BodyModeIndex.CorridorClimb) && player.IsTongueRetracting());

            ILLabel label = cursor.DefineLabel();
            cursor.Emit(OpCodes.Brfalse, label);
            cursor = cursor.Emit(OpCodes.Ret);
            cursor.MarkLabel(label);
        }

        if (cursor.TryGotoNext(instruction => instruction.MatchLdsfld<AnimationIndex>("Roll"))) {
            if (can_log_il_hooks) {
                Debug.Log(mod_id + ": IL_Player_Jump: Index " + cursor.Index); // 517
            }

            if (Option_Roll_1) {
                // roll jumps 
                // two types of jumps 
                // timing scaling removed

                // re-using Ldarg_0;
                // probably not needed;
                // sometimes there are labels pointing to the first statement;
                // preserves labels;

                cursor.Goto(cursor.Index + 4);
                cursor.Emit(OpCodes.Ldloc_0); // adrenaline_modifier

                cursor.EmitDelegate<Action<Player, float>>((player, adrenaline_modifier) => {
                    // should not be needed anymore
                    // the roll initiation logic has been modded
                    player.rocketJumpFromBellySlide = true;

                    RocketJump(player, adrenaline_modifier);
                    player.rollDirection = 0;
                });

                cursor.Emit(OpCodes.Ret);
                cursor.Emit(OpCodes.Ldarg_0); // player
            }
        } else {
            if (can_log_il_hooks) {
                Debug.Log(mod_id + ": IL_Player_Jump could not be applied.");
            }
            return;
        }

        if (cursor.TryGotoNext(MoveType.After,
              instruction => instruction.MatchLdcI4(0),
              instruction => instruction.MatchStfld<Player>("superLaunchJump")
            )) {
            if (can_log_il_hooks) {
                Debug.Log(mod_id + ": IL_Player_Jump: Index " + cursor.Index); // 1843
            }

            if (Option_Crawl) {
                // compensate for the change made in UpdateBodyMode_Crawl();
                // otherwise the Hunter cutscene fails;

                cursor.Emit(OpCodes.Ldarg_0); // player
                cursor.EmitDelegate<Action<Player>>(player => {
                    if (player.bodyMode != BodyModeIndex.Crawl) return;
                    if (player.standing) return;

                    foreach (BodyChunk body_chunk in player.bodyChunks) {
                        if (body_chunk.contactPoint.y != -1) continue;
                        if (body_chunk.onSlope != 0) continue;
                        body_chunk.vel.y += 1.5f;
                    }
                });
            }
        } else {
            if (can_log_il_hooks) {
                Debug.Log(mod_id + ": IL_Player_Jump could not be applied.");
            }
            return;
        }
        // LogAllInstructions(context);
    }

    private static void IL_Player_MovementUpdate(ILContext context) { // Option_BeamClimb // Option_WallJump
        // LogAllInstructions(context);
        ILCursor cursor = new(context);

        //
        // prevents spamming when in a corner while beam climbing
        // skip whole if statement
        //

        cursor.Goto(504);
        if (cursor.TryGotoNext(instruction => instruction.MatchLdfld<InputPackage>("y"))) { // 604
            if (can_log_il_hooks) {
                Debug.Log(mod_id + ": IL_Player_MovementUpdate: Index " + cursor.Index);
            }

            if (Option_BeamClimb) {
                cursor.Goto(cursor.Index + 2); // 606
                object label = cursor.Next.Operand;
                cursor.Goto(cursor.Index - 6); // 600

                cursor.Next.OpCode = OpCodes.Br;
                cursor.Next.Operand = label;

                // it works but why do I need two?
                cursor.GotoNext();
                cursor.GotoNext();
                cursor.Emit(OpCodes.Ldarg_0); // 601
            }
        } else {
            if (can_log_il_hooks) {
                Debug.Log(mod_id + ": IL_Player_MovementUpdate could not be applied.");
            }
            return;
        }

        //
        // grab beams by holding down
        //

        cursor.Goto(1829);
        if (cursor.TryGotoNext(MoveType.After,
            instruction => instruction.MatchCall<PhysicalObject>("get_Submersion"),
            instruction => instruction.MatchLdcR4(0.9f)
            )) {
            if (can_log_il_hooks) {
                Debug.Log(mod_id + ": IL_Player_MovementUpdate: Index " + cursor.Index); // 1929
            }

            if (Option_BeamClimb) {
                //
                // // this.wantToGrab = 1 when EmitDelegate() returns true
                //
                cursor.Next.OpCode = OpCodes.Brfalse;
                cursor.Goto(cursor.Index - 14); // 1915
                cursor = cursor.RemoveRange(14);

                cursor.EmitDelegate<Func<Player, bool>>(player => {
                    if (player.timeSinceInCorridorMode is > 0 and < 20) {
                        player.timeSinceInCorridorMode = 20;
                    }

                    if (player.input[0].y > 0 && (!ModManager.MSC || !player.monkAscension) && !(player.Submersion > 0.9f)) return true; // vanilla case
                    if (ModManager.MSC && player.monkAscension) return false; // Saint's mode

                    if (player.Get_Attached_Fields() is not Player_Attached_Fields attached_fields) return false;
                    if (attached_fields.grab_beam_counter > 0) return true; // automatically re-grab
                    if (player.animation != AnimationIndex.None || player.bodyMode != BodyModeIndex.Default) return false;

                    return attached_fields.grab_beam_cooldown_position == null && player.input[0].y < 0 && !player.input[0].jmp && !player.IsTileSolidOrSlope(0, 0, -1) && !player.IsTileSolidOrSlope(1, 0, -1);
                });
            }
        } else {
            if (can_log_il_hooks) {
                Debug.Log(mod_id + ": IL_Player_MovementUpdate could not be applied.");
            }
            return;
        }

        //
        // allow mid-air wall jumps even when pressing the other direction
        //

        if (cursor.TryGotoNext(instruction => instruction.MatchLdfld<Player>("canWallJump"))) {
            if (can_log_il_hooks) {
                Debug.Log(mod_id + ": IL_Player_MovementUpdate: Index " + cursor.Index); // 3319
            }

            if (Option_WallJump) {
                cursor.Goto(cursor.Index + 7);
                cursor.RemoveRange(8); // 3326-3333
                cursor.Next.OpCode = OpCodes.Brfalse;
                cursor.EmitDelegate<Func<Player, bool>>(player => player.canWallJump != 0);

                cursor.Goto(cursor.Index + 2);
                cursor.RemoveRange(4); // 3336-3339

                cursor.EmitDelegate<Action<Player>>(player => {
                    if (player.input[0].x == 0) {
                        player.WallJump(Math.Sign(player.canWallJump));
                        return;
                    }
                    player.WallJump(player.input[0].x);
                });
            }
        } else {
            if (can_log_il_hooks) {
                Debug.Log(mod_id + ": IL_Player_MovementUpdate could not be applied.");
            }
            return;
        }
        // LogAllInstructions(context);
    }

    private static void IL_Player_SlugSlamConditions(ILContext context) {
        // LogAllInstructions(context);
        ILCursor cursor = new(context);

        if (cursor.TryGotoNext(instruction => instruction.MatchLdsfld<AnimationIndex>("BellySlide")) &&
            cursor.TryGotoNext(instruction => instruction.MatchLdsfld<AnimationIndex>("BellySlide"))) {
            if (can_log_il_hooks) {
                Debug.Log(mod_id + ": IL_Player_SlugSlamConditions: Index " + cursor.Index);
            }

            // allow slug slam using rocket jumps;
            cursor.Goto(cursor.Index - 1);
            cursor.RemoveRange(3);
            cursor.EmitDelegate<Func<Player, bool>>(player => player.animation != AnimationIndex.BellySlide && player.animation != AnimationIndex.RocketJump);
        } else {
            if (can_log_il_hooks) {
                Debug.Log(mod_id + ": IL_Player_SlugSlamConditions could not be applied.");
            }
            return;
        }
        // LogAllInstructions(context);
    }

    private static void IL_Player_TerrainImpact(ILContext context) { // Option_BellySlide // Option_Crawl // Option_Roll_1 // Option_Roll_2
        // add the ability to initiate rolls from crawl turns (Option_Crawl);
        // remove the ability to initiate rolls from rocket jumps (Option_Roll_2);

        ILCursor cursor = new(context);
        if (cursor.TryGotoNext(instruction => instruction.MatchCall<Player>("get_input"))) {
            if (can_log_il_hooks) {
                Debug.Log(mod_id + ": IL_Player_TerrainImpact: Index " + cursor.Index); // 13
            }

            cursor.RemoveRange(42); // 55
            cursor.Emit(OpCodes.Ldarg_2);
            cursor.Emit(OpCodes.Ldarg_3);

            cursor.EmitDelegate<Func<Player, IntVector2, float, bool>>((player, direction, speed) => {
                if (player.animation == AnimationIndex.RocketJump) {
                    if (Option_Roll_2) {
                        return speed > 16f && player.input[0].downDiagonal != 0 && direction.y < 0 && player.animation != AnimationIndex.Roll && player.allowRoll > 0 && player.consistentDownDiagonal > ((speed <= 24f) ? 6 : 1);
                    }

                    if (Option_BellySlide || Option_Roll_1) {
                        return player.input[0].downDiagonal != 0 && direction.y < 0 && player.animation != AnimationIndex.Roll && player.allowRoll > 0 && player.consistentDownDiagonal > ((speed <= 24f) ? 6 : 1);
                    }
                }

                if (player.animation == AnimationIndex.CrawlTurn && Option_Crawl) {
                    return player.input[0].downDiagonal != 0 && direction.y < 0 && player.animation != AnimationIndex.Roll; // less requirements than vanilla
                }

                return player.input[0].downDiagonal != 0 && player.animation != AnimationIndex.Roll && (speed > 12f || player.animation == AnimationIndex.Flip || (player.animation == AnimationIndex.RocketJump && player.rocketJumpFromBellySlide)) && direction.y < 0 && player.allowRoll > 0 && player.consistentDownDiagonal > ((speed <= 24f) ? 6 : 1); // vanilla case
            });

            cursor.GotoNext();
            cursor.Next.OpCode = OpCodes.Brfalse;
        } else {
            if (can_log_il_hooks) {
                Debug.Log(mod_id + ": IL_Player_TerrainImpact could not be applied.");
            }
            return;
        }
        // LogAllInstructions(context);
    }

    private static void IL_Player_TongueUpdate(ILContext context) { // Option_TubeWorm
        // LogAllInstructions(context);
        ILCursor cursor = new(context);

        cursor.Emit(OpCodes.Ldarg_0);
        cursor.EmitDelegate<Func<Player, bool>>(player => {
            if (player.tongue == null || player.room == null) return true;

            // prioritize climbing and wall jumps;
            if (player.tongue.Attached && !player.Stunned && player.IsJumpPressed() && player.tongueAttachTime >= 2 && (player.IsClimbingOnBeam() || player.CanWallJumpOrMidAirWallJump() || player.bodyMode == BodyModeIndex.CorridorClimb)) {
                player.tongue.Release();
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

    private static void IL_Player_Update(ILContext context) {
        // LogAllInstructions(context);
        ILCursor cursor = new(context);

        if (cursor.TryGotoNext(instruction => instruction.MatchLdsfld<BodyModeIndex>("Default")) &&
            cursor.TryGotoNext(instruction => instruction.MatchLdfld<Player>("poleSkipPenalty"))) {
            //
            // allow beam hopping even when the player is holding up => makes it no risk 
            // and low reward instead of high risk and low reward;
            //

            Debug.Log(mod_id + ": IL_Player_Update: Index " + cursor.Index); // 1936
            cursor.Goto(cursor.Index - 8);

            // removes the player.input[0].y <= 0 check;
            cursor.RemoveRange(7);
        } else {
            if (can_log_il_hooks) {
                Debug.Log(mod_id + ": IL_Player_Update could not be applied.");
            }
            return;
        }
        // LogAllInstructions(context);
    }

    private static void IL_Player_UpdateAnimation(ILContext context) {
        // LogAllInstructions(context);
        ILCursor cursor = new(context);

        if (cursor.TryGotoNext(
                    instruction => instruction.MatchLdsfld<AnimationIndex>("HangFromBeam"),
                    instruction => instruction.MatchCall("ExtEnum`1<Player/AnimationIndex>", "op_Equality")
                    )) {
            if (can_log_il_hooks) {
                Debug.Log(mod_id + ": IL_Player_UpdateAnimation: Index " + cursor.Index); // 636
            }

            if (Option_BeamClimb) {
                cursor.Goto(cursor.Index + 4);
                cursor.EmitDelegate<Func<Player, bool>>(player => {
                    // "call" orig();
                    if (player.Get_Attached_Fields() is not Player_Attached_Fields attached_fields) return true;
                    UpdateAnimation_HangFromBeam(player, attached_fields);
                    return false;
                });

                ILLabel label = cursor.DefineLabel();
                cursor.Emit(OpCodes.Brtrue, label);
                cursor = cursor.Emit(OpCodes.Ret);
                cursor.MarkLabel(label);

                cursor.Emit(OpCodes.Ldarg_0);
            }
        } else {
            if (can_log_il_hooks) {
                Debug.Log(mod_id + ": IL_Player_UpdateAnimation could not be applied.");
            }
            return;
        }

        if (cursor.TryGotoNext(
                    instruction => instruction.MatchLdsfld<AnimationIndex>("GetUpOnBeam"),
                    instruction => instruction.MatchCall("ExtEnum`1<Player/AnimationIndex>", "op_Equality")
                    )) {
            if (can_log_il_hooks) {
                Debug.Log(mod_id + ": IL_Player_UpdateAnimation: Index " + cursor.Index); // 1081
            }

            if (Option_BeamClimb) {
                cursor.Goto(cursor.Index + 4);
                cursor.EmitDelegate<Func<Player, bool>>(player => {
                    if (player.Get_Attached_Fields() is not Player_Attached_Fields attached_fields) return true;
                    UpdateAnimation_GetUpOnBeam(player, attached_fields);
                    return false;
                });

                ILLabel label = cursor.DefineLabel();
                cursor.Emit(OpCodes.Brtrue, label);
                cursor = cursor.Emit(OpCodes.Ret);
                cursor.MarkLabel(label);

                cursor.Emit(OpCodes.Ldarg_0);
            }
        } else {
            if (can_log_il_hooks) {
                Debug.Log(mod_id + ": IL_Player_UpdateAnimation could not be applied.");
            }
            return;
        }

        if (cursor.TryGotoNext(
                    instruction => instruction.MatchLdsfld<AnimationIndex>("StandOnBeam"),
                    instruction => instruction.MatchCall("ExtEnum`1<Player/AnimationIndex>", "op_Equality")
                    )) {
            if (can_log_il_hooks) {
                Debug.Log(mod_id + ": IL_Player_UpdateAnimation: Index " + cursor.Index); // 1563
            }

            if (Option_BeamClimb) {
                cursor.Goto(cursor.Index + 4);
                cursor.EmitDelegate<Func<Player, bool>>(player => {
                    // "call" orig();
                    if (player.Get_Attached_Fields() is not Player_Attached_Fields attached_fields) return true;
                    UpdateAnimation_StandOnBeam(player, attached_fields);
                    return false;
                });

                ILLabel label = cursor.DefineLabel();
                cursor.Emit(OpCodes.Brtrue, label);
                cursor = cursor.Emit(OpCodes.Ret);
                cursor.MarkLabel(label);

                cursor.Emit(OpCodes.Ldarg_0);
            }
        } else {
            if (can_log_il_hooks) {
                Debug.Log(mod_id + ": IL_Player_UpdateAnimation could not be applied.");
            }
            return;
        }

        if (cursor.TryGotoNext(
                    instruction => instruction.MatchLdsfld<AnimationIndex>("ClimbOnBeam"),
                    instruction => instruction.MatchCall("ExtEnum`1<Player/AnimationIndex>", "op_Equality")
                    )) {
            if (can_log_il_hooks) {
                Debug.Log(mod_id + ": IL_Player_UpdateAnimation: Index " + cursor.Index); // 1766
            }

            if (Option_BeamClimb) {
                cursor.Goto(cursor.Index + 4);
                cursor.EmitDelegate<Func<Player, bool>>(player => {
                    // "call" orig();
                    if (player.Get_Attached_Fields() is not Player_Attached_Fields attached_fields) return true;
                    UpdateAnimation_ClimbOnBeam(player, attached_fields);
                    return false;
                });

                ILLabel label = cursor.DefineLabel();
                cursor.Emit(OpCodes.Brtrue, label);
                cursor = cursor.Emit(OpCodes.Ret);
                cursor.MarkLabel(label);

                cursor.Emit(OpCodes.Ldarg_0);
            }
        } else {
            if (can_log_il_hooks) {
                Debug.Log(mod_id + ": IL_Player_UpdateAnimation could not be applied.");
            }
            return;
        }

        cursor.TryGotoNext(
            instruction => instruction.MatchLdsfld<AnimationIndex>("GetUpToBeamTip"),
            instruction => instruction.MatchCall("ExtEnum`1<Player/AnimationIndex>", "op_Equality")
            );
        if (cursor.TryGotoNext(instruction => instruction.MatchStfld<Player>("canJump"))) {
            if (can_log_il_hooks) {
                Debug.Log(mod_id + ": IL_Player_UpdateAnimation: Index " + cursor.Index); // 2484
            }

            if (Option_BeamClimb) {
                // case AnimationIndex.GetUpToBeamTip:
                // prevent jumping during animation
                cursor.Prev.OpCode = OpCodes.Ldc_I4_0; // player.canJump = 0
            }
        } else {
            if (can_log_il_hooks) {
                Debug.Log(mod_id + ": IL_Player_UpdateAnimation could not be applied.");
            }
            return;
        }

        if (cursor.TryGotoNext(
                    instruction => instruction.MatchLdsfld<AnimationIndex>("BeamTip"),
                    instruction => instruction.MatchCall("ExtEnum`1<Player/AnimationIndex>", "op_Equality")
                    )) {
            if (can_log_il_hooks) {
                Debug.Log(mod_id + ": IL_Player_UpdateAnimation: Index " + cursor.Index); // 2662
            }

            if (Option_BeamClimb) {
                // don't drop off beam tip by leaning too much;

                cursor.Goto(cursor.Index + 4);
                cursor.EmitDelegate<Func<Player, bool>>(player => {
                    // "call" orig();
                    if (player.Get_Attached_Fields() is not Player_Attached_Fields attached_fields) return true;
                    UpdateAnimation_BeamTip(player, attached_fields);
                    return false;
                });

                ILLabel label = cursor.DefineLabel();
                cursor.Emit(OpCodes.Brtrue, label);
                cursor = cursor.Emit(OpCodes.Ret);
                cursor.MarkLabel(label);

                cursor.Emit(OpCodes.Ldarg_0);
            }
        } else {
            if (can_log_il_hooks) {
                Debug.Log(mod_id + ": IL_Player_UpdateAnimation could not be applied.");
            }
            return;
        }

        if (cursor.TryGotoNext(
                    instruction => instruction.MatchLdsfld<AnimationIndex>("HangUnderVerticalBeam"),
                    instruction => instruction.MatchCall("ExtEnum`1<Player/AnimationIndex>", "op_Equality")
                    )) {
            if (can_log_il_hooks) {
                Debug.Log(mod_id + ": IL_Player_UpdateAnimation: Index " + cursor.Index); // 2827
            }

            if (Option_BeamClimb) {
                cursor.Goto(cursor.Index + 4);
                cursor.EmitDelegate<Func<Player, bool>>(player => {
                    // "call" orig();
                    if (player.Get_Attached_Fields() is not Player_Attached_Fields attached_fields) return true;
                    UpdateAnimation_HangUnderVerticalBeam(player, attached_fields);
                    return false;
                });

                ILLabel label = cursor.DefineLabel();
                cursor.Emit(OpCodes.Brtrue, label);
                cursor = cursor.Emit(OpCodes.Ret);
                cursor.MarkLabel(label);

                cursor.Emit(OpCodes.Ldarg_0);
            }
        } else {
            if (can_log_il_hooks) {
                Debug.Log(mod_id + ": IL_Player_UpdateAnimation could not be applied.");
            }
            return;
        }

        cursor.TryGotoNext(
            instruction => instruction.MatchLdsfld<AnimationIndex>("DeepSwim"),
            instruction => instruction.MatchCall("ExtEnum`1<Player/AnimationIndex>", "op_Equality")
            );
        if (cursor.TryGotoNext(MoveType.After, instruction => instruction.MatchLdfld<InputPackage>("jmp"))) {
            if (can_log_il_hooks) {
                Debug.Log(mod_id + ": IL_Player_UpdateAnimation: Index " + cursor.Index); // 3223
            }

            if (Option_Swim) {
                // case AnimationIndex.DeepSwim:
                // prevent dashing under water by pressing jump;
                // unless remix is used and dashes are free;

                object label = cursor.Next.Operand;
                cursor.GotoNext();
                cursor.GotoNext();
                cursor.EmitDelegate(() => ModManager.MMF && MMF.cfgFreeSwimBoosts.Value);
                cursor.Emit(OpCodes.Brfalse, label);
            }
        } else {
            if (can_log_il_hooks) {
                Debug.Log(mod_id + ": IL_Player_UpdateAnimation could not be applied.");
            }
            return;
        }

        cursor.TryGotoNext(
            instruction => instruction.MatchLdsfld<AnimationIndex>("Roll"),
            instruction => instruction.MatchCall("ExtEnum`1<Player/AnimationIndex>", "op_Equality")
            );
        if (cursor.TryGotoNext(instruction => instruction.MatchStfld<Player>("standing"))) {
            if (can_log_il_hooks) {
                Debug.Log(mod_id + ": IL_Player_UpdateAnimation: Index " + cursor.Index); // 4828
            }

            if (Option_StandUp) {
                // case AnimationIndex.Roll:
                // always stand up when roll has finished
                // prevent chain rolling on slopes

                cursor.Prev.Previous.OpCode = OpCodes.Pop;
                cursor.Prev.OpCode = OpCodes.Ldc_I4_1; // player.standing = 1;
            }
        } else {
            if (can_log_il_hooks) {
                Debug.Log(mod_id + ": IL_Player_UpdateAnimation could not be applied.");
            }
            return;
        }

        if (cursor.TryGotoNext(
            instruction => instruction.MatchLdsfld<AnimationIndex>("BellySlide"),
            instruction => instruction.MatchCall("ExtEnum`1<Player/AnimationIndex>", "op_Equality")
            )) {
            if (can_log_il_hooks) {
                Debug.Log(mod_id + ": IL_Player_UpdateAnimation: Index " + cursor.Index); // 5036
            }

            if (Option_BellySlide) {
                // belly slide 
                // backflip always possible 
                // do a longer version by default

                cursor.Goto(cursor.Index + 4);
                cursor.EmitDelegate<Action<Player>>(player => UpdateAnimation_BellySlide(player));
                cursor.Emit(OpCodes.Ret);
                cursor.Emit(OpCodes.Ldarg_0); // player
            }
        } else {
            if (can_log_il_hooks) {
                Debug.Log(mod_id + ": IL_Player_UpdateAnimation could not be applied.");
            }
            return;
        }
        // LogAllInstructions(context);
    }

    private static void IL_Player_UpdateBodyMode(ILContext context) { // Option_Crawl // Option_TubeWorm // Option_WallClimb // Option_WallJump
        // LogAllInstructions(context);
        ILCursor cursor = new(context);

        // the TryGotoNext() is required even when Option_Crawl is false;
        // otherwise CorridorClimb will be found in line 28 instead of 1988;
        if (cursor.TryGotoNext(instruction => instruction.MatchLdsfld<BodyModeIndex>("Crawl"))) {
            if (can_log_il_hooks) {
                Debug.Log(mod_id + ": IL_Player_UpdateBodyMode: Index " + cursor.Index); // 674
            }

            if (Option_Crawl) {
                // this replaces the crawl section in UpdateBodyMode;
                // I put this into an IL-Hook to improve compatibility;
                // otherwise orig() never returns;

                cursor.Goto(cursor.Index + 4);
                cursor.EmitDelegate<Action<Player>>(player => UpdateBodyMode_Crawl(player));

                // skip vanilla code;
                cursor.Emit(OpCodes.Ret);

                // player was used in the function call;
                // restore vanilla code;
                cursor.Emit(OpCodes.Ldarg_0); // player
            }
        } else {
            if (can_log_il_hooks) {
                Debug.Log(mod_id + ": IL_Player_UpdateBodyMode could not be applied.");
            }
            return;
        }

        if (cursor.TryGotoNext(instruction => instruction.MatchLdsfld<BodyModeIndex>("CorridorClimb"))) {
            if (can_log_il_hooks) {
                Debug.Log(mod_id + ": IL_Player_UpdateBodyMode: Index " + cursor.Index); // 1988
            }

            if (Option_TubeWorm) {
                cursor.Goto(cursor.Index + 4);
                cursor.EmitDelegate<Func<Player, bool>>(player => player.IsJumpPressed() && player.IsTongueRetracting());

                ILLabel label = cursor.DefineLabel();
                cursor.Emit(OpCodes.Brfalse, label);

                // update cursor;
                // otherwise MarkLabel() will always label the position after all Emit() calls;
                cursor = cursor.Emit(OpCodes.Ret);
                cursor.MarkLabel(label);
                cursor.Emit(OpCodes.Ldarg_0); // player
            }
        } else {
            if (can_log_il_hooks) {
                Debug.Log(mod_id + ": IL_Player_UpdateBodyMode could not be applied.");
            }
            return;
        }

        if (cursor.TryGotoNext(instruction => instruction.MatchLdsfld<BodyModeIndex>("WallClimb"))) {
            if (can_log_il_hooks) {
                Debug.Log(mod_id + ": IL_Player_UpdateBodyMode: Index " + cursor.Index); // 4074
            }

            if (Option_WallClimb || Option_WallJump) {
                // crawl downwards when holding down;
                // crawl upwards when holding up;

                cursor.Goto(cursor.Index + 4);
                cursor.EmitDelegate<Action<Player>>(player => UpdateBodyMode_WallClimb(player));
                cursor.Emit(OpCodes.Ret);
                cursor.Emit(OpCodes.Ldarg_0); // player
            }
        } else {
            if (can_log_il_hooks) {
                Debug.Log(mod_id + ": IL_Player_UpdateBodyMode could not be applied.");
            }
            return;
        }
        // LogAllInstructions(context);
    }

    private static void IL_Player_WallJump(ILContext context) {
        // LogAllInstructions(context);

        ILCursor cursor = new(context);

        cursor.Emit(OpCodes.Ldarg_0);
        cursor.Emit(OpCodes.Ldarg_1);
        cursor.EmitDelegate<Func<Player, int, bool>>((player, direction) => {
            if (player.Get_Attached_Fields() is not Player_Attached_Fields attached_fields) return true;
            return WallJump(player, attached_fields, direction);
        });

        ILLabel label = cursor.DefineLabel();
        cursor.Emit(OpCodes.Brtrue, label);
        cursor.Emit(OpCodes.Ret);
        cursor.MarkLabel(label);

        // LogAllInstructions(context);
    }

    //
    //
    //

    private static void Player_CheckInput(On.Player.orig_checkInput orig, Player player) { // Option_WallJump
        orig(player);

        // does not conflict with vanilla code // simulateHoldJumpButton is used for crouch super jumps
        //  only used once: (this.input[0].jmp || this.simulateHoldJumpButton > 0) = true anyways
        // simulateHoldJumpButton = 0 afterwards // set in WallJump()

        if (player.bodyMode != BodyModeIndex.WallClimb) return;
        if (player.simulateHoldJumpButton > 0) {
            player.input[0].jmp = true;
            player.input[1].jmp = false;

            // otherwise, you might use the tube worm instantly when
            // the wall jump is performed;
            if (player.Get_Attached_Fields() is not Player_Attached_Fields attached_fields) return;
            attached_fields.dont_use_tubeworm_counter = 2;
            return;
        }

        if (player.IsJumpPressed()) {
            player.simulateHoldJumpButton = 6;
        }
    }

    private static void Player_Ctor(On.Player.orig_ctor orig, Player player, AbstractCreature abstract_creature, World world) {
        orig(player, abstract_creature, world);

        if (_all_attached_fields.ContainsKey(player)) return;
        _all_attached_fields.Add(player, new Player_Attached_Fields());

        // just for testing:
        // abstractCreature.world.game.wasAnArtificerDream = true;
        // Custom.rainWorld.setup.artificerDreamTest = 4;

        if (!Option_Swim) return;
        if (player.slugcatStats == null) return;

        // don't use player.gravity;
        // this crashes Artificer dreams;
        // gravity is a function that uses room;
        player.buoyancy = 0.9f;
        player.slugcatStats.lungsFac = 0.0f;
    }

    private static ObjectGrabability Player_Grabability(On.Player.orig_Grabability orig, Player player, PhysicalObject physical_object) { // Option_Grab
        // ignore the change when you are already grabbing it;
        // otherwise this can conflict with JollyCoopFixesAndStuff's SlugcatCollision option;
        // this option also excludes collision from carried but not dragged creatures;
        foreach (Creature.Grasp? grasp in player.grasps) {
            if (grasp != null && grasp.grabbed == physical_object) {
                return orig(player, physical_object);
            }
        }

        // you can stand in vertical corridors => exclude
        // you can stand when surface swimming => exclude
        // you can stand during beam climbing => exclude
        ObjectGrabability grabability = orig(player, physical_object);
        if (grabability == ObjectGrabability.Drag && player.standing && player.bodyMode != BodyModeIndex.CorridorClimb && player.bodyMode != BodyModeIndex.Swimming && player.bodyMode != BodyModeIndex.ZeroG && ((int)player.animation < (int)AnimationIndex.HangFromBeam || (int)player.animation > (int)AnimationIndex.BeamTip)) {
            return ObjectGrabability.CantGrab;
        }
        return grabability;
    }

    private static void Player_GraphicsModuleUpdated(On.Player.orig_GraphicsModuleUpdated orig, Player player, bool actually_viewed, bool eu) { // Option_WallClimb // Option_WallJump 
        // prevent cicadas from slowly lifing player while wall climbing
        if (player.bodyMode == BodyModeIndex.WallClimb) {
            foreach (Creature.Grasp grasp in player.grasps) {
                if (grasp?.grabbed is Cicada cicada && cicada.LiftPlayerPower > 0.01f) {
                    Vector2 pos = player.mainBodyChunk.pos;
                    Vector2 vel = player.mainBodyChunk.vel;
                    orig(player, actually_viewed, eu);

                    player.mainBodyChunk.pos = pos;
                    player.mainBodyChunk.vel = vel;
                    return;
                }
            }
        }
        orig(player, actually_viewed, eu);
    }

    private static void Player_Jump(On.Player.orig_Jump orig, Player player) {
        if (player.Get_Attached_Fields() is not Player_Attached_Fields attached_fields) {
            orig(player);
            return;
        }

        BodyChunk body_chunk_0 = player.bodyChunks[0];
        BodyChunk body_chunk_1 = player.bodyChunks[1];
        attached_fields.dont_use_tubeworm_counter = 2;

        // don't instantly regrab vertical beams;
        if (player.animation == AnimationIndex.ClimbOnBeam) {
            attached_fields.grab_beam_cooldown_position = body_chunk_1.pos;
        }

        // do a normal jump off beams when WallJump() is called;
        if (Option_WallJump && player.animation == AnimationIndex.StandOnBeam && player.input[0].y > -1) {
            player.lowerBodyFramesOffGround = 0;
        }

        if (player.bodyMode == BodyModeIndex.CorridorClimb || player.bodyMode == BodyModeIndex.WallClimb) {
            orig(player);
            return;
        }

        // early belly slide jump
        if (Option_BellySlide && player.animation == AnimationIndex.BellySlide && player.rollCounter <= 8) {
            body_chunk_1.pos.y -= 10f;
        }

        // don't jump in the wrong direction when beam climbing
        if (Option_WallJump && player.animation == AnimationIndex.ClimbOnBeam && player.input[0].x != 0) {
            player.flipDirection = player.input[0].x;
        }

        // don't stand up too early
        // the player might want to superLaunchJump
        if (Option_StandUp && player.superLaunchJump < 20 && (player.input[0].x != 0 || player.input[0].y > 0 || player.input[0].y > -1 && body_chunk_0.contactPoint.y == -1)) {
            orig(player); // uses player.standing
            player.standing = true;
            return;
        }
        orig(player);
    }

    private static void Player_MovementUpdate(On.Player.orig_MovementUpdate orig, Player player, bool eu) { // Option_BeamClimb
        // otherwise you can get stuck in climbing on beam and letting go;
        if (player.corridorDrop && player.animation != AnimationIndex.None) {
            player.corridorDrop = false;
        }
        orig(player, eu);
    }

    private static bool Player_SaintTongueCheck(On.Player.orig_SaintTongueCheck orig, Player player) { // Option_TubeWorm
        // it might be better to always call orig() for compatibility;
        bool vanilla_result = orig(player);

        if (player.IsClimbingOnBeam() || player.CanWallJumpOrMidAirWallJump() || player.bodyMode == BodyModeIndex.CorridorClimb) return false;
        if (player.shortcutDelay > 10) return false;
        if (player.Get_Attached_Fields() is not Player_Attached_Fields attached_fields) return vanilla_result;
        if (attached_fields.dont_use_tubeworm_counter > 0) return false;
        return vanilla_result;
    }

    private static void Player_TerrainImpact(On.Player.orig_TerrainImpact orig, Player player, int chunk, IntVector2 direction, float speed, bool first_contact) { // Option_StandUp
        orig(player, chunk, direction, speed, first_contact);

        if (!first_contact) return;

        // check speed;
        // otherwise crawl turns can fulfill this sometimes as well;
        if (player.animation == AnimationIndex.None && player.bodyMode == BodyModeIndex.Default && player.bodyChunks[1].vel.sqrMagnitude > 64f) {
            player.standing = true;
        }
    }

    private static void Player_ThrowObject(On.Player.orig_ThrowObject orig, Player player, int grasp_index, bool eu) { // Option_BellySlide // Option_Gourmand // Option_SpearThrow
        bool is_gourmand_exhausted = ModManager.MSC && player.isGourmand && player.grasps[grasp_index]?.grabbed is Spear;

        // throw weapon // don't get forward momentum on ground or poles
        if (Option_SpearThrow && player.grasps[grasp_index]?.grabbed is Weapon && player.animation != AnimationIndex.BellySlide && (player.animation != AnimationIndex.Flip || player.input[0].y >= 0 || player.input[0].x != 0)) {
            if (player.bodyMode == BodyModeIndex.ClimbingOnBeam || player.bodyChunks[1].onSlope != 0) {
                player.bodyChunks[0].vel.x -= player.ThrowDirection * 4f; // total: 4f
            } else if (player.IsTileSolid(bChunk: 1, 0, -1)) {
                player.bodyChunks[1].vel.x -= player.ThrowDirection * 4f; // total: -8f
            }
        }

        if (Option_BellySlide) {
            // remove timing for belly slide throw;
            int roll_counter = player.rollCounter;
            player.rollCounter = 10;
            orig(player, grasp_index, eu);
            player.rollCounter = roll_counter;
        } else {
            orig(player, grasp_index, eu);
        }

        if (Option_Gourmand && is_gourmand_exhausted) {
            player.gourmandExhausted = true;
        }
    }

    private static void Player_Update(On.Player.orig_Update orig, Player player, bool eu) { // Option_BeamClimb // Option_TubeWorm
        orig(player, eu);
        if (Option_BeamClimb && player.Get_Attached_Fields() is Player_Attached_Fields attached_fields) {
            if (player.bodyMode == BodyModeIndex.ClimbingOnBeam) {
                attached_fields.time_since_climbing_on_beam = 0;
            } else {
                ++attached_fields.time_since_climbing_on_beam;
            }
        }

        // depending on your input in x you might not get an mid-air wall jump;
        // make sure that you can use your tongue again next frame again;
        if (!Option_TubeWorm) return;
        if (player.canJump == 0 && player.canWallJump != 0 && player.wantToJump > 0) {
            // reset wantToJump too in order to "consume" the jump;
            // otherwise you might still do a late jump for the same jump press;
            player.wantToJump = 0;
            player.canWallJump = 0;
        }

        // player.IsTongueRetracting() needs to be last;
        // there are cases where the tongue update is late and retracting is forced when returning true;
        if (player.IsJumpPressed() && (player.IsClimbingOnBeam() || player.CanWallJumpOrMidAirWallJump() || player.bodyMode == BodyModeIndex.CorridorClimb) && player.IsTongueRetracting()) {
            // this prevents late jumps next frame;
            // wantToJump is set before inputs are updated;
            // => wantToJump = player.input[1].jmp && !player.input[2].jmp in most cases;
            player.wantToJump = 1;
        }
    }

    private static void Player_UpdateAnimation(On.Player.orig_UpdateAnimation orig, Player player) {
        if (player.room is not Room room || player.Get_Attached_Fields() is not Player_Attached_Fields attached_fields) {
            orig(player);
            return;
        }

        BodyChunk body_chunk_0 = player.bodyChunks[0];
        BodyChunk body_chunk_1 = player.bodyChunks[1];

        if (attached_fields.dont_use_tubeworm_counter > 0) {
            --attached_fields.dont_use_tubeworm_counter;
        }

        if (attached_fields.get_up_on_beam_abort_counter > 0) { // beam climb
            --attached_fields.get_up_on_beam_abort_counter;
        }

        if (attached_fields.grab_beam_counter > 0) {
            --attached_fields.grab_beam_counter;
        }

        // check versus body_chunk_0 since you are only grabbing with your hands
        // if the distance is too low you might instantly re-grab horizontal beams when moving horizontally
        if (attached_fields.grab_beam_cooldown_position is Vector2 grab_beam_cooldown_pos && Vector2.Distance(grab_beam_cooldown_pos, body_chunk_0.pos) >= 25f) {
            attached_fields.grab_beam_cooldown_position = null;
        }

        if (attached_fields.sound_cooldown > 0) {
            --attached_fields.sound_cooldown;
        }

        if (player.animation == AnimationIndex.None) {
            orig(player);
            return;
        }

        if (Option_Crawl && player.animation == AnimationIndex.CorridorTurn && player.corridorTurnCounter < 30) {
            player.corridorTurnCounter = 30;
        }

        if (Option_CrouchJump && player.bodyMode == BodyModeIndex.Crawl && player.superLaunchJump > 0 && player.superLaunchJump < 10) {
            player.superLaunchJump = 10;
        }

        // deep swim // ignore jump input // increase speed 
        else if (Option_Swim && player.animation == AnimationIndex.DeepSwim) {
            // don't update twice;
            // UpdateAnimationCounter(player);

            if (player.slugcatStats.lungsFac != 0.0f) {
                player.slugcatStats.lungsFac = 0.0f;
            }

            // only lose airInLungs when grabbed by leeches or rain timer is up
            if (player.abstractCreature.world?.rainCycle.TimeUntilRain <= 0) {
                player.slugcatStats.lungsFac = 1f;
            } else {
                foreach (Creature.Grasp creature in player.grabbedBy) {
                    if (creature.grabber is Leech) {
                        player.slugcatStats.lungsFac = 1f;
                        break;
                    }
                }
            }
        }

        // ledge grab 
        else if (Option_WallJump && player.animation == AnimationIndex.LedgeGrab && (player.canWallJump == 0 || Math.Sign(player.canWallJump) == -Math.Sign(player.flipDirection))) {
            player.canWallJump = player.flipDirection * -15; // you can do a (mid-air) wall jump off a ledge grab
        }

        // beam climb 
        else if (Option_BeamClimb && player.animation == AnimationIndex.GetUpToBeamTip) {
            // velocity gained in x direction each frame (if no slowmovementstun, and bodyMode is default)
            float vel_x_gain = 2.4f * Mathf.Lerp(1f, 1.2f, player.Adrenaline) * player.surfaceFriction * player.EffectiveRoomGravity;
            if (player.slowMovementStun > 0) {
                vel_x_gain *= 0.4f + 0.6f * Mathf.InverseLerp(10f, 0.0f, player.slowMovementStun);
            }

            // otherwise you might jump early when reaching the BeamTip;
            player.wantToJump = 0;

            // don't let go of beam while climbing to the top // don't prevent player from entering corridors
            // if (body_chunk_0.contactPoint.x == 0 && body_chunk_1.contactPoint.x == 0)
            foreach (BodyChunk body_chunk in player.bodyChunks) {
                Tile tile = room.GetTile(body_chunk.pos);
                if (!tile.verticalBeam && room.GetTile(tile.X, tile.Y - 1).verticalBeam) {
                    float middle_of_tile_x = room.MiddleOfTile(tile.X, tile.Y).x;
                    // give a bit of protection against wind and horizontal momentum
                    body_chunk_0.pos.x += Mathf.Clamp(middle_of_tile_x - body_chunk_0.pos.x, -2f * vel_x_gain, 2f * vel_x_gain);
                    body_chunk_1.pos.x += Mathf.Clamp(middle_of_tile_x - body_chunk_1.pos.x, -2f * vel_x_gain, 2f * vel_x_gain);

                    // you might get stuck from solid tiles above;
                    // do a auto-regrab like you can do when pressing down while being on the beam tip;
                    if (player.input[0].y < 0) {
                        attached_fields.grab_beam_counter = 15;
                        attached_fields.dont_use_tubeworm_counter = 2; // used if pressing down + jump // don't fall and shoot tongue at the same time
                        player.canJump = 0;
                        player.animation = AnimationIndex.None;
                        break;
                    }

                    // ignore x input
                    if (player.input[0].x != 0 && !player.IsTileSolid(bChunk: 0, player.input[0].x, 0) && !player.IsTileSolid(bChunk: 1, player.input[0].x, 0)) {
                        body_chunk_0.vel.x -= player.input[0].x * vel_x_gain;
                        body_chunk_1.vel.x -= player.input[0].x * vel_x_gain;
                    }
                    break;
                }
            }
        }

        // finish;
        if (player.animation == AnimationIndex.RocketJump) {
            // don't put orig() before if statement;
            // orig() updates player.animation;
            orig(player);
            if (player.animation == AnimationIndex.None && Option_StandUp && (player.IsTileSolid(0, 0, -1) || player.IsTileSolid(1, 0, -1))) { // stand up after rocket jump
                AlignPosYOnSlopes(player);
                player.standing = true;
                player.animation = AnimationIndex.StandUp;
            } else if (Option_BellySlide || Option_Roll_1) {
                // don't cancel rocket jumps by collision in y
                for (int chunk_index = 0; chunk_index <= 1; ++chunk_index) {
                    BodyChunk body_chunk = player.bodyChunks[chunk_index];
                    if (body_chunk.contactPoint.y == 1) {
                        body_chunk.vel.y = 0.0f;
                        player.animation = AnimationIndex.RocketJump;
                        break;
                    }
                }
            }
        } else if (player.animation == AnimationIndex.Flip && player.flipFromSlide) {
            orig(player);
            if (player.animation == AnimationIndex.None && Option_StandUp && (player.IsTileSolid(0, 0, -1) || player.IsTileSolid(1, 0, -1))) { // stand up after belly slides // don't try to stand up when sliding down walls
                AlignPosYOnSlopes(player);
                player.standing = true;
                player.animation = AnimationIndex.StandUp;
            } else if (Option_BellySlide) {
                // don't cancel flips by collision in y
                for (int chunk_index = 0; chunk_index <= 1; ++chunk_index) {
                    BodyChunk body_chunk = player.bodyChunks[chunk_index];
                    if (body_chunk.contactPoint.y == 1) {
                        body_chunk.vel.y = 0.0f;
                        player.standing = true;
                        player.animation = AnimationIndex.Flip;
                        break;
                    }
                }
            }
        } else {
            orig(player);
        }

        // rivulet gets dynamicRunSpeed of 5f => don't slow down
        if (player.animation == AnimationIndex.SurfaceSwim) {
            if (Option_StandUp) {
                // otherwise jumping too much will put you into crawl after leaving the water;
                player.standing = true;
            }

            if (Option_Swim && !player.isRivulet) {
                player.swimCycle += 0.01f;
                player.dynamicRunSpeed[0] = 3.5f;
            }
        }

        // crawl // slopes
        if (Option_Crawl) {
            // stop crawl turn when hitting the ground // might happen early on slopes
            if (player.animation == AnimationIndex.CrawlTurn && player.input[0].x > 0 == player.bodyChunks[0].pos.x >= (double)body_chunk_1.pos.x && player.bodyChunks[0].contactPoint.y == -1) {
                player.animation = AnimationIndex.None;
            }

            // finish ledge crawl when on slopes
            if (player.animation == AnimationIndex.LedgeCrawl && player.bodyChunks[0].onSlope != 0 && body_chunk_1.onSlope != 0) {
                player.animation = AnimationIndex.None;
            }
        }
    }

    private static void Player_UpdateBodyMode(On.Player.orig_UpdateBodyMode orig, Player player) { // Option_SlideTurn
        orig(player);

        // backflip
        // earlier timing possible
        if (player.initSlideCounter <= 0) return;
        if (player.initSlideCounter >= 10) return;
        player.initSlideCounter = 10;
    }

    private static void Player_UpdateMSC(On.Player.orig_UpdateMSC orig, Player player) { // Option_Swim
        orig(player);

        if (!ModManager.MSC) return;
        player.buoyancy = player.gravity;
    }

    private static void Player_WallJump(On.Player.orig_WallJump orig, Player player, int direction) {
        orig(player, direction);

        // not sure if this was required;
        player.simulateHoldJumpButton = 0;
    }

    //
    //
    //

    private static Vector2 Tongue_AutoAim(On.Player.Tongue.orig_AutoAim orig, Tongue tongue, Vector2 direction) { // Option_TubeWorm
        // here originalDir = newDir since direction is adjusted in Tongue_Shoot();
        // newDir needs to be used in TubeWormMod;
        if (tongue.player is not Player player) return orig(tongue, direction);
        if (player.room == null) return orig(tongue, direction);

        // vanilla with new direction
        // prioritize aiming for solid tiles
        Vector2 output = orig(tongue, direction);
        if (output != direction) return output;
        if (!SharedPhysics.RayTraceTilesForTerrain(player.room, tongue.baseChunk.pos, tongue.baseChunk.pos + direction * 230f)) return direction;

        Vector2? new_output = Tongue_AutoAim_Beams(tongue, direction, prioritize_angle_over_distance: player.input[0].x == 0 && player.input[0].y > 0, preferred_horizontal_direction: direction.y >= 0.9f ? 0 : player.input[0].x);
        if (new_output.HasValue) return new_output.Value;
        return direction;
    }

    private static void Tongue_Shoot(On.Player.Tongue.orig_Shoot orig, Tongue tongue, Vector2 direction) { // Option_TubeWorm
        // adept tongue direction to player inputs in some additional cases
        if (tongue.player.input[0].x != 0) {
            // used in the case where y > 0 as well
            direction += new Vector2(tongue.player.input[0].x * 0.30f, 0.0f);
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

    public sealed class Player_Attached_Fields {
        public bool initialize_hands = false;
        public bool is_switching_beams = false;
        public bool tubeworm_tongue_needs_to_retract = false;

        public int dont_use_tubeworm_counter = 0;
        public int get_up_on_beam_abort_counter = 0;
        public int get_up_on_beam_direction = 0;
        public int grab_beam_counter = 0;
        public int sound_cooldown = 0;
        public int time_since_climbing_on_beam = 0;

        public Vector2? grab_beam_cooldown_position = null;
    }
}
