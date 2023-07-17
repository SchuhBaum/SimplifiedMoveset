using Mono.Cecil.Cil;
using MonoMod.Cil;
using RWCustom;
using System;
using System.Collections.Generic;
using UnityEngine;
using static BodyChunk;
using static Room;
using static Room.SlopeDirection;
using static Room.Tile;
using static SimplifiedMoveset.MainMod;
using static SimplifiedMoveset.PlayerMod;
using static SimplifiedMoveset.RoomMod;

namespace SimplifiedMoveset;

public static class BodyChunkMod {
    //
    // variables
    //

    internal static readonly Dictionary<BodyChunk, BodyChunk_Attached_Fields> _all_attached_fields = new();
    public static BodyChunk_Attached_Fields? Get_Attached_Fields(this BodyChunk body_chunk) {
        _all_attached_fields.TryGetValue(body_chunk, out BodyChunk_Attached_Fields? attached_fields);
        return attached_fields;
    }

    //
    // main
    //

    internal static void On_Config_Changed() {
        IL.BodyChunk.checkAgainstSlopesVertically -= IL_BodyChunk_CheckAgainstSlopesVertically;
        On.BodyChunk.CheckVerticalCollision -= BodyChunk_CheckVerticalCollision;
        On.BodyChunk.ctor -= BodyChunk_Ctor;
        On.BodyChunk.Update -= BodyChunk_Update;

        if (Option_BeamClimb) {
            // bonk your head less often when trying to jump off beams;
            On.BodyChunk.CheckVerticalCollision += BodyChunk_CheckVerticalCollision;
        }

        if (Option_BellySlide || Option_Crawl) {
            IL.BodyChunk.checkAgainstSlopesVertically += IL_BodyChunk_CheckAgainstSlopesVertically;
            On.BodyChunk.ctor += BodyChunk_Ctor;
            On.BodyChunk.Update += BodyChunk_Update;
        }
    }

    //
    // public
    //

    public static void CheckAgainstSlopesVertically(BodyChunk body_chunk, BodyChunk_Attached_Fields attached_fields) { // Option_BellySlide // Option_Crawl
        if (body_chunk.owner?.room is not Room room) return;

        // only Player get BodyChunk_Attached_Fields in ctor;
        PhysicalObject player = body_chunk.owner;

        IntVector2 tile_position = room.GetTilePosition(body_chunk.pos);
        Vector2 middle_of_tile = room.MiddleOfTile(tile_position);
        SlopeDirection slope_direction = room.IdentifySlope(tile_position);

        //
        // detect slopes
        //

        // smooth moving down slopes
        if (slope_direction == Broken && attached_fields.last_on_slope_tile_position is IntVector2 last_on_slope_tile_position && attached_fields.last_on_slope * (body_chunk.vel.x - attached_fields.body_chunk_connection_velocity.x) > 0.0f && body_chunk.vel.y - attached_fields.body_chunk_connection_velocity.y < -player.gravity) {
            // project tilePosition.y down to the slope surface line; check later at
            // this position a slope tile exists and do some other checks;
            tile_position.y = last_on_slope_tile_position.y + attached_fields.last_on_slope * (last_on_slope_tile_position.x - tile_position.x);
            Tile? non_air_tile_below = Get_Non_Air_Tile_Below(room, tile_position);

            if (non_air_tile_below == null || non_air_tile_below.Y < tile_position.y) {
                // enough air tiles available; can project down to the slope surface 
                // line;
                tile_position = last_on_slope_tile_position;
            } else if (non_air_tile_below.Terrain == TerrainType.Slope) {
                // project down to this slope surface line instead; can be a different 
                // one and closer in distance;
                tile_position.y = non_air_tile_below.Y;
            } else {
                // solid floor; place the bodyChunk above solid or floor tiles;
                //
                // collision checks for solids in y direction are already done; let them
                //  collide next frame;
                tile_position.y = non_air_tile_below.Y + 1;
            }

            middle_of_tile = room.MiddleOfTile(tile_position);
            slope_direction = room.IdentifySlope(tile_position);
            body_chunk.pos.y = middle_of_tile.y;
        }

        if (slope_direction == Broken) {
            // look horizontal first to anticipate colliding with slopes ahead;
            for (int modifier_x = -1; modifier_x <= 1; modifier_x += 2) {
                SlopeDirection slope_direction_ = room.IdentifySlope(tile_position.x + modifier_x, tile_position.y);
                if (slope_direction_ != Broken && modifier_x * (body_chunk.pos.x - middle_of_tile.x) >= 10.0 - body_chunk.slopeRad) {
                    // body_chunk is "peeking out" of the tile at tilePosition (right side when
                    // modifierX == 1);
                    tile_position.x += modifier_x;
                    middle_of_tile = room.MiddleOfTile(tile_position);
                    slope_direction = slope_direction_;
                    break;
                }
            }
        }

        if (slope_direction == Broken) {
            for (int modifier_y = -1; modifier_y <= 1; modifier_y += 2) {
                SlopeDirection slope_direction_ = room.IdentifySlope(tile_position.x, tile_position.y + modifier_y);
                if (slope_direction_ != Broken && modifier_y * (body_chunk.pos.y - middle_of_tile.y) > 10.0 - body_chunk.slopeRad) // > to smooth out transition from slope to solid
                {
                    tile_position.y += modifier_y;
                    middle_of_tile = room.MiddleOfTile(tile_position);
                    slope_direction = slope_direction_;
                    break;
                }
            }
        }

        attached_fields.last_on_slope_tile_position = null;

        // no slope detected;
        if (slope_direction == Broken) return;

        //
        // initialize variables
        //

        int on_slope = 0;
        float position_y;
        int slope_vertical_position;

        if (slope_direction == UpLeft) { // oO
            // project down to the slope surface line
            position_y = middle_of_tile.y + body_chunk.pos.x - middle_of_tile.x;
            on_slope = -1;
            slope_vertical_position = -1;
        } else if (slope_direction == UpRight) { // Oo
            // project down to the slope surface line 
            // pos.x stays constant 
            // pos.y moves down when bodyChunk.pos.x > middleOfTile.x otherwise up to slope surface
            position_y = middle_of_tile.y + middle_of_tile.x - body_chunk.pos.x;
            on_slope = 1;
            slope_vertical_position = -1;
        } else if (slope_direction == DownLeft) { // �O
            position_y = middle_of_tile.y + middle_of_tile.x - body_chunk.pos.x;
            slope_vertical_position = 1;
        } else { // O�
            position_y = middle_of_tile.y + body_chunk.pos.x - middle_of_tile.x;
            slope_vertical_position = 1;
        }

        //
        // collision detection
        //

        if (slope_vertical_position == -1 && body_chunk.pos.y <= position_y + body_chunk.slopeRad + body_chunk.slopeRad) {
            body_chunk.pos.y = position_y + body_chunk.slopeRad + body_chunk.slopeRad;
            if (body_chunk.vel.y < -player.impactTreshhold) {
                player.TerrainImpact(body_chunk.index, new IntVector2(0, -1), -body_chunk.vel.y, body_chunk.lastContactPoint.y > -1);
            }

            body_chunk.vel.x *= 0.7f * Mathf.Clamp(body_chunk.owner.surfaceFriction * 2f, 0.0f, 1f); // keep distance almost identical // 0.7 approx 1/sqrt(2)
            body_chunk.vel.y = Mathf.Abs(body_chunk.vel.y) * player.bounce;

            if (body_chunk.vel.y < player.gravity || body_chunk.vel.y < 1.0 + 9.0 * (1.0 - player.bounce)) {
                body_chunk.vel.y = 0.0f;
            }

            body_chunk.contactPoint.y = -1;
            body_chunk.onSlope = on_slope;
            attached_fields.last_on_slope_tile_position = tile_position;
            return;
        }

        if (slope_vertical_position != 1) return;
        if (body_chunk.pos.y < position_y - body_chunk.slopeRad - body_chunk.slopeRad) return;

        body_chunk.pos.y = position_y - body_chunk.slopeRad - body_chunk.slopeRad;
        if (body_chunk.vel.y > player.impactTreshhold) {
            player.TerrainImpact(body_chunk.index, new IntVector2(0, 1), body_chunk.vel.y, body_chunk.lastContactPoint.y < 1);
        }

        body_chunk.vel.x *= 0.7f * Mathf.Clamp(player.surfaceFriction * 2f, 0.0f, 1f);
        body_chunk.vel.y = -Mathf.Abs(body_chunk.vel.y) * player.bounce;

        if (body_chunk.vel.y > -1.0 - 9.0 * (1.0 - player.bounce)) {
            body_chunk.vel.y = 0.0f;
        }
        body_chunk.contactPoint.y = 1;
    }

    //
    // private
    //

    private static void IL_BodyChunk_CheckAgainstSlopesVertically(ILContext context) {
        // LogAllInstructions(context);

        ILCursor cursor = new(context);
        cursor.Emit(OpCodes.Ldarg_0);

        cursor.EmitDelegate<Func<BodyChunk, bool>>(body_chunk => {
            // "call" orig() if returning true;
            if (body_chunk.Get_Attached_Fields() is not BodyChunk_Attached_Fields attached_fields) return true;
            CheckAgainstSlopesVertically(body_chunk, attached_fields);
            return false;
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

    private static void BodyChunk_CheckVerticalCollision(On.BodyChunk.orig_CheckVerticalCollision orig, BodyChunk body_chunk) {
        if (body_chunk.owner is not Player player || body_chunk != player.mainBodyChunk || body_chunk.vel.y <= 0f) {
            orig(body_chunk);
            return;
        }

        // this can interfere when shortcuts are close to the water surface; therefore
        // be more cautious when to change the collision check;
        if (player.Get_Attached_Fields() is not Player_Attached_Fields attached_fields || attached_fields.time_since_climbing_on_beam > 20) {
            orig(body_chunk);
            return;
        }

        if (player.room == null) {
            orig(body_chunk);
            return;
        }

        //
        // this is mostly vanilla copy&paste; the main difference is that you only have 
        // x instead of a loop from x_start to x_end;
        //

        body_chunk.contactPoint.y = 0;
        IntVector2 tile_position = player.room.GetTilePosition(body_chunk.lastPos);
        int number_of_repeats = 0;

        int y_start = player.room.GetTilePosition(new Vector2(0f, body_chunk.pos.y + body_chunk.TerrainRad + 0.01f)).y;
        int y_end = player.room.GetTilePosition(new Vector2(0f, body_chunk.lastPos.y + body_chunk.TerrainRad)).y;
        int x = player.room.GetTilePosition(new Vector2(body_chunk.pos.x, 0f)).x;

        for (int y = y_end; y <= y_start; y++) {
            if (player.room.GetTile(x, y).Terrain == TerrainType.Solid && player.room.GetTile(x, y - 1).Terrain != TerrainType.Solid && (tile_position.y < y || player.room.GetTile(body_chunk.lastPos).Terrain == TerrainType.Solid)) {
                body_chunk.pos.y = y * 20f - body_chunk.TerrainRad;
                if (body_chunk.vel.y > player.impactTreshhold) {
                    player.TerrainImpact(body_chunk.index, new IntVector2(0, 1), Mathf.Abs(body_chunk.vel.y), body_chunk.lastContactPoint.y < 1);
                }

                body_chunk.contactPoint.y = 1;
                body_chunk.vel.y = -Mathf.Abs(body_chunk.vel.y) * player.bounce;

                if (Mathf.Abs(body_chunk.vel.y) < 1f + 9f * (1f - player.bounce)) {
                    body_chunk.vel.y = 0f;
                }

                body_chunk.vel.x *= Mathf.Clamp(player.surfaceFriction * 2f, 0f, 1f);
                break;
            }

            number_of_repeats++;
            if (number_of_repeats > MaxRepeats) {
                Debug.Log("!!!!! " + player?.ToString() + " emergency breakout of terrain check!");
                break;
            }
        }
    }

    private static void BodyChunk_Ctor(On.BodyChunk.orig_ctor orig, BodyChunk body_chunk, PhysicalObject owner, int index, Vector2 pos, float rad, float mass) { // Option_BellySlide // Option_Crawl
        orig(body_chunk, owner, index, pos, rad, mass);
        if (owner is not Player) return;
        if (_all_attached_fields.ContainsKey(body_chunk)) return;
        _all_attached_fields.Add(body_chunk, new BodyChunk_Attached_Fields());
    }

    private static void BodyChunk_Update(On.BodyChunk.orig_Update orig, BodyChunk body_chunk) { // Option_BellySlide // Option_Crawl
        if (body_chunk.Get_Attached_Fields() is not BodyChunk_Attached_Fields attached_fields) {
            orig(body_chunk);
            return;
        }

        attached_fields.last_on_slope = body_chunk.onSlope;
        orig(body_chunk);
    }

    //
    //
    //

    public sealed class BodyChunk_Attached_Fields {
        // variables are initialized in BodyChunk_ctor() and cleared in RainWorldGameMod
        public int last_on_slope = 0;
        public IntVector2? last_on_slope_tile_position = null;
        public Vector2 body_chunk_connection_velocity = new();
    }
}
