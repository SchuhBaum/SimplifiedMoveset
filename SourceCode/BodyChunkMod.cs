using System;
using System.Collections.Generic;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RWCustom;
using UnityEngine;

using static Room;
using static SimplifiedMoveset.MainMod;

namespace SimplifiedMoveset;

public static class BodyChunkMod
{
    //
    // variables
    //

    internal static readonly Dictionary<BodyChunk, BodyChunk_Attached_Fields> all_attached_fields = new();
    public static BodyChunk_Attached_Fields? Get_Attached_Fields(this BodyChunk body_chunk)
    {
        all_attached_fields.TryGetValue(body_chunk, out BodyChunk_Attached_Fields? attached_fields);
        return attached_fields;
    }

    private static bool is_enabled = false;

    //
    //
    //

    internal static void OnToggle()
    {
        is_enabled = !is_enabled;
        if (Option_BellySlide || Option_Crawl)
        {
            if (is_enabled)
            {
                IL.BodyChunk.checkAgainstSlopesVertically += IL_BodyChunk_CheckAgainstSlopesVertically;

                On.BodyChunk.ctor += BodyChunk_ctor;
                On.BodyChunk.Update += BodyChunk_Update;
            }
            else
            {
                IL.BodyChunk.checkAgainstSlopesVertically -= IL_BodyChunk_CheckAgainstSlopesVertically;

                On.BodyChunk.ctor -= BodyChunk_ctor;
                On.BodyChunk.Update -= BodyChunk_Update;
            }
        }
    }


    //
    // public
    //

    public static void CheckAgainstSlopesVertically(BodyChunk body_chunk, BodyChunk_Attached_Fields attached_fields) // Option_BellySlide // Option_Crawl
    {
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
        if (slope_direction == SlopeDirection.Broken && attached_fields.lastOnSlopeTilePos is IntVector2 lastOnSlopeTilePos && attached_fields.lastOnSlope * (body_chunk.vel.x - attached_fields.body_chunk_connection_velocity.x) > 0.0f && body_chunk.vel.y - attached_fields.body_chunk_connection_velocity.y < -player.gravity)
        {
            tile_position.y = lastOnSlopeTilePos.y + attached_fields.lastOnSlope * (lastOnSlopeTilePos.x - tile_position.x); // project tilePosition.y down to the slope surface line // check later at this position a slope tile exists and do some other checks
            Tile? nonAirTileBelow = RoomMod.GetNonAirTileBelow(room, tile_position);

            if (nonAirTileBelow == null || nonAirTileBelow.Y < tile_position.y) // enough air tiles available // can project down to the slope surface line
            {
                tile_position = lastOnSlopeTilePos;
            }
            else if (nonAirTileBelow.Terrain == Tile.TerrainType.Slope) // slope // project down to this slope surface line instead // can be a differenct one and closer in distance
            {
                tile_position.y = nonAirTileBelow.Y;
            }
            else // solid // floor // place the bodyChunk above solid or floor tiles
            {
                tile_position.y = nonAirTileBelow.Y + 1; // collision checks for solids in y direction are already done // let them collide next frame
            }

            middle_of_tile = room.MiddleOfTile(tile_position);
            slope_direction = room.IdentifySlope(tile_position);
            body_chunk.pos.y = middle_of_tile.y;
        }

        if (slope_direction == SlopeDirection.Broken)
        {
            // look horizontal first to anticipate colliding with slopes ahead
            for (int modifierX = -1; modifierX <= 1; modifierX += 2)
            {
                SlopeDirection slopeDirection_ = room.IdentifySlope(tile_position.x + modifierX, tile_position.y);
                if (slopeDirection_ != SlopeDirection.Broken && modifierX * (body_chunk.pos.x - middle_of_tile.x) >= 10.0 - body_chunk.slopeRad) // body_chunk is "peeking out" of the tile at tilePosition (right side when modifierX == 1)
                {
                    tile_position.x += modifierX;
                    middle_of_tile = room.MiddleOfTile(tile_position);
                    slope_direction = slopeDirection_;
                    break;
                }
            }
        }

        if (slope_direction == SlopeDirection.Broken)
        {
            for (int modifierY = -1; modifierY <= 1; modifierY += 2)
            {
                SlopeDirection slope_direction_ = room.IdentifySlope(tile_position.x, tile_position.y + modifierY);
                if (slope_direction_ != SlopeDirection.Broken && modifierY * (body_chunk.pos.y - middle_of_tile.y) > 10.0 - body_chunk.slopeRad) // > to smooth out transition from slope to solid
                {
                    tile_position.y += modifierY;
                    middle_of_tile = room.MiddleOfTile(tile_position);
                    slope_direction = slope_direction_;
                    break;
                }
            }
        }

        attached_fields.lastOnSlopeTilePos = null;

        // no slope detected;
        if (slope_direction == SlopeDirection.Broken) return;

        //
        // initialize variables
        //

        int on_slope = 0;
        float position_y;
        int slope_vertical_position;

        if (slope_direction == SlopeDirection.UpLeft) // oO
        {
            // project down to the slope surface line
            position_y = middle_of_tile.y + body_chunk.pos.x - middle_of_tile.x;
            on_slope = -1;
            slope_vertical_position = -1;
        }
        else if (slope_direction == SlopeDirection.UpRight) // Oo
        {
            // project down to the slope surface line 
            // pos.x stays constant 
            // pos.y moves down when bodyChunk.pos.x > middleOfTile.x otherwise up to slope surface
            position_y = middle_of_tile.y + middle_of_tile.x - body_chunk.pos.x;
            on_slope = 1;
            slope_vertical_position = -1;
        }
        else if (slope_direction == SlopeDirection.DownLeft) // �O
        {
            position_y = middle_of_tile.y + middle_of_tile.x - body_chunk.pos.x;
            slope_vertical_position = 1;
        }
        else  // O�
        {
            position_y = middle_of_tile.y + body_chunk.pos.x - middle_of_tile.x;
            slope_vertical_position = 1;
        }

        //
        // collision detection
        //

        if (slope_vertical_position == -1 && body_chunk.pos.y <= position_y + body_chunk.slopeRad + body_chunk.slopeRad)
        {
            body_chunk.pos.y = position_y + body_chunk.slopeRad + body_chunk.slopeRad;
            if (body_chunk.vel.y < -player.impactTreshhold)
            {
                player.TerrainImpact(body_chunk.index, new IntVector2(0, -1), -body_chunk.vel.y, body_chunk.lastContactPoint.y > -1);
            }

            body_chunk.vel.x *= 0.7f * Mathf.Clamp(body_chunk.owner.surfaceFriction * 2f, 0.0f, 1f); // keep distance almost identical // 0.7 approx 1/sqrt(2)
            body_chunk.vel.y = Mathf.Abs(body_chunk.vel.y) * player.bounce;

            if (body_chunk.vel.y < player.gravity || body_chunk.vel.y < 1.0 + 9.0 * (1.0 - player.bounce))
            {
                body_chunk.vel.y = 0.0f;
            }

            body_chunk.contactPoint.y = -1;
            body_chunk.onSlope = on_slope;
            attached_fields.lastOnSlopeTilePos = tile_position;
            return;
        }

        if (slope_vertical_position != 1) return;
        if (body_chunk.pos.y < position_y - body_chunk.slopeRad - body_chunk.slopeRad) return;

        body_chunk.pos.y = position_y - body_chunk.slopeRad - body_chunk.slopeRad;
        if (body_chunk.vel.y > player.impactTreshhold)
        {
            player.TerrainImpact(body_chunk.index, new IntVector2(0, 1), body_chunk.vel.y, body_chunk.lastContactPoint.y < 1);
        }

        body_chunk.vel.x *= 0.7f * Mathf.Clamp(player.surfaceFriction * 2f, 0.0f, 1f);
        body_chunk.vel.y = -Mathf.Abs(body_chunk.vel.y) * player.bounce;

        if (body_chunk.vel.y > -1.0 - 9.0 * (1.0 - player.bounce))
        {
            body_chunk.vel.y = 0.0f;
        }
        body_chunk.contactPoint.y = 1;
    }

    //
    // private
    //

    private static void IL_BodyChunk_CheckAgainstSlopesVertically(ILContext context)
    {
        // LogAllInstructions(context);

        ILCursor cursor = new(context);
        cursor.Emit(OpCodes.Ldarg_0);

        cursor.EmitDelegate<Func<BodyChunk, bool>>(body_chunk =>
        {
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

    private static void BodyChunk_ctor(On.BodyChunk.orig_ctor orig, BodyChunk body_chunk, PhysicalObject owner, int index, Vector2 pos, float rad, float mass) // Option_BellySlide // Option_Crawl
    {
        orig(body_chunk, owner, index, pos, rad, mass);

        if (owner is not Player) return;
        if (all_attached_fields.ContainsKey(body_chunk)) return;
        all_attached_fields.Add(body_chunk, new BodyChunk_Attached_Fields());
    }

    private static void BodyChunk_Update(On.BodyChunk.orig_Update orig, BodyChunk body_chunk) // Option_BellySlide // Option_Crawl
    {
        if (body_chunk.Get_Attached_Fields() is not BodyChunk_Attached_Fields attached_fields)
        {
            orig(body_chunk);
            return;
        }

        attached_fields.lastOnSlope = body_chunk.onSlope;
        orig(body_chunk);
    }

    //
    //
    //

    public sealed class BodyChunk_Attached_Fields
    {
        // variables are initialized in BodyChunk_ctor() and cleared in RainWorldGameMod
        public int lastOnSlope = 0;
        public IntVector2? lastOnSlopeTilePos = null;
        public Vector2 body_chunk_connection_velocity = new();
    }
}