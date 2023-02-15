using System.Collections.Generic;
using RWCustom;
using UnityEngine;

namespace SimplifiedMoveset
{
    public static class BodyChunkMod
    {
        //
        // variables
        //

        internal static readonly Dictionary<BodyChunk, AttachedFields> attachedFieldsDictionary = new();
        public static AttachedFields GetAttachedFields(this BodyChunk bodyChunk) => attachedFieldsDictionary[bodyChunk];

        private static bool isEnabled = false;

        //
        //
        //

        internal static void OnToggle()
        {
            isEnabled = !isEnabled;
            if (MainMod.Option_BellySlide || MainMod.Option_Crawl)
            {
                if (isEnabled)
                {
                    On.BodyChunk.checkAgainstSlopesVertically += BodyChunk_checkAgainstSlopesVertically;
                    On.BodyChunk.ctor += BodyChunk_ctor;
                    On.BodyChunk.Update += BodyChunk_Update;
                }
                else
                {
                    On.BodyChunk.checkAgainstSlopesVertically -= BodyChunk_checkAgainstSlopesVertically;
                    On.BodyChunk.ctor -= BodyChunk_ctor;
                    On.BodyChunk.Update -= BodyChunk_Update;
                }
            }
        }

        //
        // private
        //

        private static void BodyChunk_checkAgainstSlopesVertically(On.BodyChunk.orig_checkAgainstSlopesVertically orig, BodyChunk bodyChunk) // MainMod.Option_BellySlide // MainMod.Option_Crawl
        {
            if (bodyChunk.owner is not Player player || player.room is not Room room)
            {
                orig(bodyChunk);
                return;
            }

            IntVector2 tilePosition = room.GetTilePosition(bodyChunk.pos);
            Vector2 middleOfTile = room.MiddleOfTile(tilePosition);
            Room.SlopeDirection slopeDirection = room.IdentifySlope(tilePosition);
            AttachedFields attachedFields = bodyChunk.GetAttachedFields();

            // smooth moving down slopes
            if (slopeDirection == Room.SlopeDirection.Broken && attachedFields.lastOnSlopeTilePos is IntVector2 lastOnSlopeTilePos && attachedFields.lastOnSlope * (bodyChunk.vel.x - attachedFields.bodyChunkConnectionVel.x) > 0.0f && bodyChunk.vel.y - attachedFields.bodyChunkConnectionVel.y < -player.gravity)
            {
                tilePosition.y = lastOnSlopeTilePos.y + attachedFields.lastOnSlope * (lastOnSlopeTilePos.x - tilePosition.x); // project tilePosition.y down to the slope surface line // check later at this position a slope tile exists and do some other checks
                Room.Tile? nonAirTileBelow = RoomMod.GetNonAirTileBelow(room, tilePosition);

                if (nonAirTileBelow == null || nonAirTileBelow.Y < tilePosition.y) // enough air tiles available // can project down to the slope surface line
                {
                    tilePosition = lastOnSlopeTilePos;
                }
                else if (nonAirTileBelow.Terrain == Room.Tile.TerrainType.Slope) // slope // project down to this slope surface line instead // can be a differenct one and closer in distance
                {
                    tilePosition.y = nonAirTileBelow.Y;
                }
                else // solid // floor // place the bodyChunk above solid or floor tiles
                {
                    tilePosition.y = nonAirTileBelow.Y + 1; // collision checks for solids in y direction are already done // let them collide next frame
                }

                middleOfTile = room.MiddleOfTile(tilePosition);
                slopeDirection = room.IdentifySlope(tilePosition);
                bodyChunk.pos.y = middleOfTile.y;
            }

            if (slopeDirection == Room.SlopeDirection.Broken)
            {
                // look horizontal first to anticipate colliding with slopes ahead
                for (int modifierX = -1; modifierX <= 1; modifierX += 2)
                {
                    Room.SlopeDirection slopeDirection_ = room.IdentifySlope(tilePosition.x + modifierX, tilePosition.y);
                    if (slopeDirection_ != Room.SlopeDirection.Broken && modifierX * (bodyChunk.pos.x - middleOfTile.x) >= 10.0 - bodyChunk.slopeRad) // bodyChunk is "peeking out" of the tile at tilePosition (right side when modifierX == 1)
                    {
                        tilePosition.x += modifierX;
                        middleOfTile = room.MiddleOfTile(tilePosition);
                        slopeDirection = slopeDirection_;
                        break;
                    }
                }
            }

            if (slopeDirection == Room.SlopeDirection.Broken)
            {
                for (int modifierY = -1; modifierY <= 1; modifierY += 2)
                {
                    Room.SlopeDirection slopeDirection_ = room.IdentifySlope(tilePosition.x, tilePosition.y + modifierY);
                    if (slopeDirection_ != Room.SlopeDirection.Broken && modifierY * (bodyChunk.pos.y - middleOfTile.y) > 10.0 - bodyChunk.slopeRad) // > to smooth out transition from slope to solid
                    {
                        tilePosition.y += modifierY;
                        middleOfTile = room.MiddleOfTile(tilePosition);
                        slopeDirection = slopeDirection_;
                        break;
                    }
                }
            }

            attachedFields.lastOnSlopeTilePos = null;
            if (slopeDirection == Room.SlopeDirection.Broken)
            {
                return;
            }

            int onSlope = 0;
            float posYFromX;
            int slopeVerticalPosition;

            if (slopeDirection == Room.SlopeDirection.UpLeft) // oO
            {
                posYFromX = (float)(middleOfTile.y + bodyChunk.pos.x - middleOfTile.x); // project down to the slope surface line
                onSlope = -1;
                slopeVerticalPosition = -1;
            }
            else if (slopeDirection == Room.SlopeDirection.UpRight) // Oo
            {
                posYFromX = (float)(middleOfTile.y + middleOfTile.x - bodyChunk.pos.x); // project down to the slope surface line // pos.x stays constant // pos.y moves down when bodyChunk.pos.x > middleOfTile.x otherwise up to slope surface
                onSlope = 1;
                slopeVerticalPosition = -1;
            }
            else if (slopeDirection == Room.SlopeDirection.DownLeft) // �O
            {
                posYFromX = (float)(middleOfTile.y + middleOfTile.x - bodyChunk.pos.x);
                slopeVerticalPosition = 1;
            }
            else  // O�
            {
                posYFromX = (float)(middleOfTile.y + bodyChunk.pos.x - middleOfTile.x);
                slopeVerticalPosition = 1;
            }

            if (slopeVerticalPosition == -1 && bodyChunk.pos.y <= posYFromX + bodyChunk.slopeRad + bodyChunk.slopeRad)
            {
                bodyChunk.pos.y = posYFromX + bodyChunk.slopeRad + bodyChunk.slopeRad;
                if (bodyChunk.vel.y < -player.impactTreshhold)
                {
                    player.TerrainImpact(bodyChunk.index, new IntVector2(0, -1), -bodyChunk.vel.y, bodyChunk.lastContactPoint.y > -1);
                }

                bodyChunk.vel.x *= 0.7f * Mathf.Clamp(bodyChunk.owner.surfaceFriction * 2f, 0.0f, 1f); // keep distance almost identical // 0.7 approx 1/sqrt(2)
                bodyChunk.vel.y = Mathf.Abs(bodyChunk.vel.y) * player.bounce;

                if (bodyChunk.vel.y < player.gravity || bodyChunk.vel.y < 1.0 + 9.0 * (1.0 - player.bounce))
                {
                    bodyChunk.vel.y = 0.0f;
                }

                bodyChunk.contactPoint.y = -1;
                bodyChunk.onSlope = onSlope;
                attachedFields.lastOnSlopeTilePos = tilePosition;
            }
            else if (slopeVerticalPosition == 1 && bodyChunk.pos.y >= posYFromX - bodyChunk.slopeRad - bodyChunk.slopeRad)
            {
                bodyChunk.pos.y = posYFromX - bodyChunk.slopeRad - bodyChunk.slopeRad;
                if (bodyChunk.vel.y > player.impactTreshhold)
                {
                    player.TerrainImpact(bodyChunk.index, new IntVector2(0, 1), bodyChunk.vel.y, bodyChunk.lastContactPoint.y < 1);
                }

                bodyChunk.vel.x *= 0.7f * Mathf.Clamp(player.surfaceFriction * 2f, 0.0f, 1f);
                bodyChunk.vel.y = -Mathf.Abs(bodyChunk.vel.y) * player.bounce;

                if (bodyChunk.vel.y > -1.0 - 9.0 * (1.0 - player.bounce))
                {
                    bodyChunk.vel.y = 0.0f;
                }
                bodyChunk.contactPoint.y = 1;
            }
        }

        private static void BodyChunk_ctor(On.BodyChunk.orig_ctor orig, BodyChunk bodyChunk, PhysicalObject owner, int index, Vector2 pos, float rad, float mass) // MainMod.Option_BellySlide // MainMod.Option_Crawl
        {
            orig(bodyChunk, owner, index, pos, rad, mass);

            if (owner is not Player) return;
            if (attachedFieldsDictionary.ContainsKey(bodyChunk)) return;
            attachedFieldsDictionary.Add(bodyChunk, new AttachedFields());
        }

        private static void BodyChunk_Update(On.BodyChunk.orig_Update orig, BodyChunk bodyChunk) // MainMod.Option_BellySlide // MainMod.Option_Crawl
        {
            if (bodyChunk.owner is not Player)
            {
                orig(bodyChunk);
                return;
            }

            bodyChunk.GetAttachedFields().lastOnSlope = bodyChunk.onSlope;
            orig(bodyChunk);
        }

        //
        //
        //

        public sealed class AttachedFields
        {
            // variables are initialized in BodyChunk_ctor() and cleared in RainWorldGameMod
            public int lastOnSlope = 0;
            public IntVector2? lastOnSlopeTilePos = null;
            public Vector2 bodyChunkConnectionVel = new();
        }
    }
}