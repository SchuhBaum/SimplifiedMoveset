using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MoreSlugcats;
using RWCustom;
using UnityEngine;

namespace SimplifiedMoveset
{
    public static class PlayerMod
    {
        //
        // parameters
        //

        public static readonly float leanFactor = 1f;

        //
        // variables
        //

        internal static readonly Dictionary<Player, AttachedFields> attachedFields = new();
        public static AttachedFields GetAttachedFields(this Player player) => attachedFields[player];

        //
        //
        //

        internal static void OnDisable_()
        {
            IL.Player.UpdateAnimation -= IL_Player_UpdateAnimation;
            if (MainMod.Option_BeamClimb)
            {
                IL.Player.MovementUpdate -= IL_Player_MovementUpdate;
            }

            if (MainMod.Option_BellySlide)
            {
                On.Player.ThrowObject -= Player_ThrowObject_Option_BellySlide;
            }

            if (MainMod.Option_Grab)
            {
                On.Player.Grabability -= Player_Grabability;
            }

            if (MainMod.Option_BellySlide || MainMod.Option_Crawl || MainMod.Option_Roll_1 || MainMod.Option_Roll_2)
            {
                IL.Player.TerrainImpact -= IL_Player_TerrainImpact;
            }

            if (MainMod.Option_SpearThrow)
            {
                On.Player.ThrowObject -= Player_ThrowObject_Option_SpearThrow;
            }

            if (MainMod.Option_Swim)
            {
                IL.Player.GrabUpdate -= IL_Player_GrabUpdate;
                On.Player.UpdateMSC -= Player_UpdateMSC;
            }

            if (MainMod.Option_WallJump)
            {
                On.Player.checkInput -= Player_CheckInput;
            }

            if (MainMod.Option_WallJump || MainMod.Option_WallClimb)
            {
                On.Player.GraphicsModuleUpdated -= Player_GraphicsModuleUpdated;
            }
        }

        //
        //
        //

        internal static void OnEnable()
        {
            On.Player.ctor += Player_ctor; // change stats for swimming
            On.Player.Jump += Player_Jump;
            On.Player.UpdateAnimation += Player_UpdateAnimation;

            On.Player.UpdateBodyMode += Player_UpdateBodyMode;
            On.Player.WallJump += Player_WallJump;
        }

        internal static void OnEnable_()
        {
            IL.Player.UpdateAnimation += IL_Player_UpdateAnimation;
            if (MainMod.Option_BeamClimb)
            {
                // removes lifting your booty when being in a corner with your upper bodyChunk / head
                // usually this happens in one tile horizontal holes
                // but this can also happen when climbing beams and bumping your head into a corner
                // in this situation canceling beam climbing can be spammed
                //
                // grabbing beams by holding down is now implemented here instead of UpdateAnimation()
                IL.Player.MovementUpdate += IL_Player_MovementUpdate;
            }

            if (MainMod.Option_BellySlide)
            {
                On.Player.ThrowObject += Player_ThrowObject_Option_BellySlide; // remove throw timing
            }

            if (MainMod.Option_Grab)
            {
                On.Player.Grabability += Player_Grabability; // only grab dead large creatures when crouching
            }

            if (MainMod.Option_BellySlide || MainMod.Option_Crawl || MainMod.Option_Roll_1 || MainMod.Option_Roll_2)
            {
                IL.Player.TerrainImpact += IL_Player_TerrainImpact;
            }

            if (MainMod.Option_SpearThrow)
            {
                On.Player.ThrowObject += Player_ThrowObject_Option_SpearThrow; // momentum adjustment
            }

            if (MainMod.Option_Swim)
            {
                IL.Player.GrabUpdate += IL_Player_GrabUpdate; // can eat stuff underwater
                On.Player.UpdateMSC += Player_UpdateMSC; // don't let MSC reset buoyancy
            }

            if (MainMod.Option_WallJump)
            {
                On.Player.checkInput += Player_CheckInput; // input "buffer" for wall jumping
            }

            if (MainMod.Option_WallJump || MainMod.Option_WallClimb)
            {
                On.Player.GraphicsModuleUpdated += Player_GraphicsModuleUpdated; // fix cicade lifting up while wall climbing
            }
        }

        // ---------------- //
        // public functions //
        // ---------------- //

        // useful as a setup for some animations while on slopes
        public static void AlignPosYOnSlopes(Player? player)
        {
            if (player == null)
            {
                return;
            }

            if (player.bodyChunks[0].pos.y < player.bodyChunks[1].pos.y)
            {
                player.bodyChunks[0].pos.y = player.bodyChunks[1].pos.y;
            }
            player.bodyChunks[0].vel.y += player.dynamicRunSpeed[0];
        }

        // the name of the function is a bit ambiguous since one of the animations
        // is called ClimbOnBeam..
        // public static bool IsClimbingOnBeam(this Player player)
        // {
        //     int player_animation = (int)player.animation;
        //     return player_animation >= 6 && player_animation <= 12;
        // }

        public static bool IsTileSolidOrSlope(this Player player, int chunkIndex, int relativeX, int relativeY)
        {
            if (player.room is not Room room) return false;
            if (player.IsTileSolid(chunkIndex, relativeX, relativeY)) return true;
            return room.GetTile(room.GetTilePosition(player.bodyChunks[chunkIndex].pos) + new IntVector2(relativeX, relativeY)).Terrain == Room.Tile.TerrainType.Slope;
        }

        // direction: up = 1, down = -1
        public static void PrepareGetUpOnBeamAnimation(Player? player, int direction, AttachedFields attachedFields)
        {
            // trying to be more robust
            // I had cases where mods would break (but not vanilla) when trying to adjust room loading => annoying to work around
            if (player?.room is Room room)
            {
                int chunkIndex = direction == 1 ? 0 : 1;
                player.bodyChunks[1 - chunkIndex].pos.x = player.bodyChunks[chunkIndex].pos.x;
                room.PlaySound(SoundID.Slugcat_Get_Up_On_Horizontal_Beam, player.mainBodyChunk, false, 1f, 1f);

                Room.Tile tile = room.GetTile(player.bodyChunks[chunkIndex].pos + new Vector2(player.flipDirection * 20f, 0.0f));
                if (tile.Terrain == Room.Tile.TerrainType.Solid || !tile.horizontalBeam)
                {
                    player.flipDirection = -player.flipDirection;
                }

                player.animation = Player.AnimationIndex.GetUpOnBeam;
                player.upOnHorizontalBeamPos = new Vector2(player.bodyChunks[chunkIndex].pos.x, room.MiddleOfTile(player.bodyChunks[chunkIndex].pos).y + direction * 20f);
                attachedFields.getUpOnBeamDirection = direction;
            }
        }

        public static void RocketJump(Player? player, float adrenalineModifier, float scale = 1f, SoundID? soundID = null)
        {
            if (player == null) return;

            soundID ??= SoundID.Slugcat_Rocket_Jump;

            player.bodyChunks[1].vel *= 0.0f;
            player.bodyChunks[1].pos += new Vector2(5f * player.rollDirection, 5f);
            player.bodyChunks[0].pos = player.bodyChunks[1].pos + new Vector2(5f * player.rollDirection, 5f);
            player.animation = Player.AnimationIndex.RocketJump;

            Vector2 vel = Custom.DegToVec(player.rollDirection * (90f - Mathf.Lerp(30f, 55f, scale))) * Mathf.Lerp(9.5f, 13.1f, scale) * adrenalineModifier;
            player.bodyChunks[0].vel = vel;
            player.bodyChunks[1].vel = vel;

            if (soundID != SoundID.None)
            {
                player.room?.PlaySound(soundID, player.mainBodyChunk, false, 1f, 1f);
            }
        }

        public static bool SwitchHorizontalToVerticalBeam(Player? player, AttachedFields attachedFields)
        {
            if (player?.room is Room room)
            {
                BodyChunk bodyChunk0 = player.bodyChunks[0];
                bool isVerticalBeamLongEnough = player.input[0].y != 0 && room.GetTile(bodyChunk0.pos + new Vector2(0.0f, 20f * player.input[0].y)).verticalBeam;

                if (room.GetTile(bodyChunk0.pos).verticalBeam && (isVerticalBeamLongEnough || player.input[0].x != 0 && (player.animation == Player.AnimationIndex.HangFromBeam && !room.GetTile(bodyChunk0.pos + new Vector2(20f * player.input[0].x, 0.0f)).horizontalBeam || player.animation == Player.AnimationIndex.StandOnBeam && !room.GetTile(player.bodyChunks[1].pos + new Vector2(20f * player.input[0].x, 0.0f)).horizontalBeam)))
                {
                    // prioritize horizontal over vertical beams when they are one-tile only => only commit in that case when isVerticalBeamLongEnough
                    if (!attachedFields.isSwitchingBeams && (player.input[0].y == 0 || isVerticalBeamLongEnough))
                    {
                        attachedFields.isSwitchingBeams = true;
                        player.flipDirection = bodyChunk0.pos.x >= room.MiddleOfTile(bodyChunk0.pos).x ? 1 : -1;
                        player.animation = Player.AnimationIndex.ClimbOnBeam;
                        return true;
                    }
                }
                else
                {
                    attachedFields.isSwitchingBeams = false;
                }
            }
            return false;
        }

        public static bool SwitchVerticalToHorizontalBeam(Player? player, AttachedFields attachedFields)
        {
            if (player?.room is Room room)
            {
                BodyChunk bodyChunk0 = player.bodyChunks[0];
                BodyChunk bodyChunk1 = player.bodyChunks[1];
                Room.Tile tile0 = room.GetTile(bodyChunk0.pos);

                // HangFromBeam
                // prioritize HangFromBeam when at the end of a vertical beam
                // not very clean since isSwitchingBeams is not used
                // BUT switching to vertical beams can be nice to jump further => need a case to fall back; even after having just switched
                if (tile0.horizontalBeam && player.input[0].y != 0 && !room.GetTile(tile0.X, tile0.Y + player.input[0].y).verticalBeam)
                {
                    attachedFields.isSwitchingBeams = true;
                    player.animation = Player.AnimationIndex.HangFromBeam;
                    return true;
                }
                // HangFromBeam
                else if (tile0.horizontalBeam && player.input[0].x != 0 && room.GetTile(tile0.X + player.input[0].x, tile0.Y).horizontalBeam)
                {
                    if (!attachedFields.isSwitchingBeams)
                    {
                        attachedFields.isSwitchingBeams = true;
                        player.animation = Player.AnimationIndex.HangFromBeam;
                        return true;
                    }
                }
                // StandOnBeam
                else if (room.GetTile(bodyChunk1.pos).horizontalBeam && player.input[0].x != 0 && room.GetTile(bodyChunk1.pos + new Vector2(20f * player.input[0].x, 0.0f)).horizontalBeam) //  || player.input[0].y == -1 && !room.GetTile(bodyChunk1.pos - new Vector2(0.0f, 20f)).verticalBeam
                {
                    if (!attachedFields.isSwitchingBeams)
                    {
                        attachedFields.isSwitchingBeams = true;
                        player.animation = Player.AnimationIndex.StandOnBeam;
                        return true;
                    }
                }
                else
                {
                    attachedFields.isSwitchingBeams = false;
                }
            }
            return false;
        }

        public static void UpdateAnimationCounter(Player? player)
        {
            if (player == null) return;

            if (player.longBellySlide && player.animation != Player.AnimationIndex.BellySlide)
            {
                player.longBellySlide = false;
            }

            if (player.stopRollingCounter > 0 && player.animation != Player.AnimationIndex.Roll)
            {
                player.stopRollingCounter = 0;
            }

            if (player.slideUpPole > 0 && player.animation != Player.AnimationIndex.ClimbOnBeam)
            {
                player.slideUpPole = 0;
            }
        }

        public static void UpdateBodyModeCounter(Player? player)
        {
            if (player == null)
            {
                return;
            }

            player.diveForce = Mathf.Max(0.0f, player.diveForce - 0.05f);
            player.waterRetardationImmunity = Mathf.InverseLerp(0.0f, 0.3f, player.diveForce) * 0.85f;

            if (player.dropGrabTile.HasValue && player.bodyMode != Player.BodyModeIndex.Default && player.bodyMode != Player.BodyModeIndex.CorridorClimb)
            {
                player.dropGrabTile = new IntVector2?();
            }

            if (player.bodyChunks[0].contactPoint.y < 0)
            {
                ++player.upperBodyFramesOnGround;
                player.upperBodyFramesOffGround = 0;
            }
            else
            {
                player.upperBodyFramesOnGround = 0;
                ++player.upperBodyFramesOffGround;
            }

            if (player.bodyChunks[1].contactPoint.y < 0)
            {
                ++player.lowerBodyFramesOnGround;
                player.lowerBodyFramesOffGround = 0;
            }
            else
            {
                player.lowerBodyFramesOnGround = 0;
                ++player.lowerBodyFramesOffGround;
            }
        }

        // ----------------- //
        // private functions //
        // ----------------- //

        private static void IL_Player_GrabUpdate(ILContext context)
        {
            ILCursor cursor = new(context);

            // allow to eat underwater
            // change player.mainBodyChunk.submersion < 0.5f to < 2f => always true

            if (cursor.TryGotoNext(MoveType.After, instruction => instruction.MatchCallvirt<BodyChunk>("get_submersion"))) // 87
            {
                Debug.Log("SimplifiedMoveset: IL_Player_GrabUpdate: Index " + cursor.Index);
                cursor.Next.OpCode = OpCodes.Ldc_R4;
                cursor.Next.Operand = 2f;
            }
            else
            {
                Debug.LogException(new Exception("SimplifiedMoveset: IL_Player_GrabUpdate failed."));
            }
            // MainMod.LogAllInstructions(context);
        }

        private static void IL_Player_MovementUpdate(ILContext context)
        {
            ILCursor cursor = new(context);
            cursor.Goto(504);

            //
            // prevents spamming when in a corner while beam climbing
            // skip whole if statement
            //

            if (cursor.TryGotoNext(instruction => instruction.MatchLdfld<Player.InputPackage>("y"))) // 604
            {
                Debug.Log("SimplifiedMoveset: IL_Player_MovementUpdate: Index " + cursor.Index);
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
            else
            {
                Debug.LogException(new Exception("SimplifiedMoveset: IL_Player_MovementUpdate failed."));
            }

            //
            // grab beams by holding down
            //

            cursor = new(context);
            cursor.Goto(1829);

            if (cursor.TryGotoNext(MoveType.After,
                instruction => instruction.MatchCall<PhysicalObject>("get_Submersion"),
                instruction => instruction.MatchLdcR4(0.9f)))
            {
                Debug.Log("SimplifiedMoveset: IL_Player_MovementUpdate: Index " + cursor.Index); // 1929

                //
                // // this.wantToGrab = 1 when EmitDelegate() returns true
                //
                cursor.Next.OpCode = OpCodes.Brfalse;
                cursor.Goto(cursor.Index - 14); // 1915
                cursor = cursor.RemoveRange(14);

                cursor.EmitDelegate<Func<Player, bool>>(player =>
                {
                    // there is a check for timeSinceInCorridorMode >= 20 afterwards;
                    // why do you wait after climbing in a corridor?;
                    // this check is only used under certain conditions (being upside down?);
                    // this can lead to not grabbing beams and falling down;
                    // in some modded regions this means even falling to your death;
                    if (player.timeSinceInCorridorMode < 20) player.timeSinceInCorridorMode = 20;
                    if (player.input[0].y > 0 && (!ModManager.MSC || !player.monkAscension) && !(player.Submersion > 0.9f)) return true; // vanilla case

                    AttachedFields attachedFields = player.GetAttachedFields();
                    if (attachedFields.grabBeamCounter > 0) return true; // automatically re-grab
                    if (player.animation != Player.AnimationIndex.None || player.bodyMode != Player.BodyModeIndex.Default) return false;
                    // if (player.IsClimbingOnBeam()) return false; // don't mess with switching beams while beam climbing
                    return attachedFields.grabBeamCooldownPos == null && player.input[0].y < 0 && !player.input[0].jmp && !player.IsTileSolidOrSlope(0, 0, -1) && !player.IsTileSolidOrSlope(1, 0, -1);
                });
            }
            else
            {
                Debug.LogException(new Exception("SimplifiedMoveset: IL_Player_MovementUpdate failed."));
            }
            // MainMod.LogAllInstructions(context);
        }

        private static void IL_Player_TerrainImpact(ILContext context)
        {
            // add the ability to initiate rolls from crawl turns (Option_Crawl);
            // remove the ability to initiate rolls from rocket jumps (Option_Roll_2);

            ILCursor cursor = new(context);
            if (cursor.TryGotoNext(instruction => instruction.MatchCall<Player>("get_input")))
            {
                Debug.Log("SimplifiedMoveset: IL_Player_TerrainImpact: Index " + cursor.Index); // 13
                cursor.RemoveRange(42); // 55
                cursor.Emit(OpCodes.Ldarg_2);
                cursor.Emit(OpCodes.Ldarg_3);

                cursor.EmitDelegate<Func<Player, IntVector2, float, bool>>((player, direction, speed) =>
                {
                    if (player.animation == Player.AnimationIndex.RocketJump)
                    {
                        if (!MainMod.Option_Roll_2) return false;

                        if (MainMod.Option_BellySlide || MainMod.Option_Roll_1)
                        {
                            return player.input[0].downDiagonal != 0 && direction.y < 0 && player.animation != Player.AnimationIndex.Roll && player.allowRoll > 0 && player.consistentDownDiagonal > ((speed <= 24f) ? 6 : 1);
                        }
                    }

                    if (player.animation == Player.AnimationIndex.CrawlTurn && MainMod.Option_Crawl)
                    {
                        return player.input[0].downDiagonal != 0 && direction.y < 0 && player.animation != Player.AnimationIndex.Roll; // less requirements than vanilla
                    }

                    return player.input[0].downDiagonal != 0 && player.animation != Player.AnimationIndex.Roll && (speed > 12f || player.animation == Player.AnimationIndex.Flip || (player.animation == Player.AnimationIndex.RocketJump && player.rocketJumpFromBellySlide)) && direction.y < 0 && player.allowRoll > 0 && player.consistentDownDiagonal > ((speed <= 24f) ? 6 : 1); // vanilla case
                });

                cursor.GotoNext();
                cursor.Next.OpCode = OpCodes.Brfalse;
            }
            else
            {
                Debug.LogException(new Exception("SimplifiedMoveset: IL_Player_TerrainImpact failed."));
            }
            // MainMod.LogAllInstructions(context);
        }

        private static void IL_Player_UpdateAnimation(ILContext context)
        {
            ILCursor cursor = new(context);
            if (MainMod.Option_BeamClimb)
            {
                cursor.Goto(2384);

                // case Player.AnimationIndex.GetUpToBeamTip:
                // prevent jumping during animation
                if (cursor.TryGotoNext(instruction => instruction.MatchStfld<Player>("canJump"))) // 2484
                {
                    Debug.Log("SimplifiedMoveset: IL_Player_UpdateAnimation_1: Index " + cursor.Index);
                    cursor.Prev.OpCode = OpCodes.Ldc_I4_0; // player.canJump = 0
                }
                else
                {
                    Debug.LogException(new Exception("SimplifiedMoveset: IL_Player_UpdateAnimation_1 failed."));
                }
            }

            if (MainMod.Option_Swim)
            {
                cursor.Goto(3122);

                // case Player.AnimationIndex.DeepSwim:
                // prevent dashing under water by pressing jump;
                // unless remix is used and dashes are free;
                if (cursor.TryGotoNext(MoveType.After, instruction => instruction.MatchLdfld<Player.InputPackage>("jmp"))) // 3223
                {
                    Debug.Log("SimplifiedMoveset: IL_Player_UpdateAnimation_2: Index " + cursor.Index);
                    object label = cursor.Next.Operand;
                    cursor.GotoNext();
                    cursor.GotoNext();
                    cursor.EmitDelegate(() => ModManager.MMF && MMF.cfgFreeSwimBoosts.Value);
                    cursor.Emit(OpCodes.Brfalse, label);
                }
                else
                {
                    Debug.LogException(new Exception("SimplifiedMoveset: IL_Player_UpdateAnimation_2 failed."));
                }
            }

            if (MainMod.Option_Roll_1)
            {
                cursor.Goto(4732);

                // case Player.AnimationIndex.Roll:
                // always stand up when roll has finished
                // prevent chain rolling on slopes
                if (cursor.TryGotoNext(instruction => instruction.MatchStfld<Player>("standing"))) // 4832
                {
                    Debug.Log("SimplifiedMoveset: IL_Player_UpdateAnimation_3: Index " + cursor.Index);
                    cursor.Prev.Previous.OpCode = OpCodes.Pop;
                    cursor.Prev.OpCode = OpCodes.Ldc_I4_1; // player.standing = 1;
                }
                else
                {
                    Debug.LogException(new Exception("SimplifiedMoveset: IL_Player_UpdateAnimation_3 failed."));
                }
            }
            // MainMod.LogAllInstructions(context);
        }

        //
        //
        //

        private static void Player_CheckInput(On.Player.orig_checkInput orig, Player player)
        {
            orig(player);

            if (player.bodyMode == Player.BodyModeIndex.WallClimb)
            {
                // does not conflict with vanilla code // simulateHoldJumpButton is used for crouch super jumps
                //  only used once: (this.input[0].jmp || this.simulateHoldJumpButton > 0) = true anyways
                // simulateHoldJumpButton = 0 afterwards // set in WallJump()

                if (player.simulateHoldJumpButton > 0)
                {
                    player.input[0].jmp = true;
                    player.input[1].jmp = false;
                    player.GetAttachedFields().dontUseTubeWormCounter = 2;
                }
                else if (player.input[0].jmp && !player.input[1].jmp)
                {
                    player.simulateHoldJumpButton = 6;
                }
            }
        }

        private static void Player_ctor(On.Player.orig_ctor orig, Player player, AbstractCreature abstractCreature, World world)
        {
            orig(player, abstractCreature, world);
            attachedFields.Add(player, new AttachedFields());

            if (MainMod.Option_Swim)
            {
                player.slugcatStats.lungsFac = 0.0f;
                player.buoyancy = player.gravity;
            }
        }

        private static Player.ObjectGrabability Player_Grabability(On.Player.orig_Grabability orig, Player player, PhysicalObject physicalObject)
        {
            // ignore the change when you are already grabbing it;
            // otherwise this can conflict with JollyCoopFixesAndStuff's SlugcatCollision option;
            // this option also excludes collision from carried but not dragged creatures;
            foreach (Creature.Grasp? grasp in player.grasps)
            {
                if (grasp != null && grasp.grabbed == physicalObject)
                {
                    return orig(player, physicalObject);
                }
            }

            // you can stand in vertical corridors => exclude
            // you can stand when surface swimming => exclude
            // you can stand during beam climbing => exclude
            if (physicalObject is Creature creature && !creature.Template.smallCreature && creature.dead &&
                player.standing && player.bodyMode != Player.BodyModeIndex.CorridorClimb && player.bodyMode != Player.BodyModeIndex.Swimming && player.bodyMode != Player.BodyModeIndex.ZeroG && ((int)player.animation < (int)Player.AnimationIndex.HangFromBeam || (int)player.animation > (int)Player.AnimationIndex.BeamTip))
            {
                return Player.ObjectGrabability.CantGrab;
            }
            return orig(player, physicalObject);
        }

        private static void Player_GraphicsModuleUpdated(On.Player.orig_GraphicsModuleUpdated orig, Player player, bool actuallyViewed, bool eu)
        {
            // prevent cicadas from slowly lifing player while wall climbing
            if (player.bodyMode == Player.BodyModeIndex.WallClimb)
            {
                foreach (Creature.Grasp grasp in player.grasps)
                {
                    if (grasp?.grabbed is Cicada cicada && cicada.LiftPlayerPower > 0.01f)
                    {
                        Vector2 pos = player.mainBodyChunk.pos;
                        Vector2 vel = player.mainBodyChunk.vel;
                        orig(player, actuallyViewed, eu);

                        player.mainBodyChunk.pos = pos;
                        player.mainBodyChunk.vel = vel;
                        return;
                    }
                }
            }
            orig(player, actuallyViewed, eu);
        }

        // there are cases where this function does not call orig()
        private static void Player_Jump(On.Player.orig_Jump orig, Player player)
        {
            // don't instantly regrab vertical beams
            if (player.animation == Player.AnimationIndex.ClimbOnBeam)
            {
                player.GetAttachedFields().grabBeamCooldownPos = player.bodyChunks[1].pos;
            }

            if (player.bodyMode != Player.BodyModeIndex.CorridorClimb && player.bodyMode != Player.BodyModeIndex.WallClimb)
            {
                // roll jumps // two types of jumps // timing scaling removed
                if (MainMod.Option_Roll_1 && player.animation == Player.AnimationIndex.Roll)
                {
                    // before switch statement // vanilla code
                    player.feetStuckPos = new Vector2?(); // what does this do?
                    float adrenalineModifier = Mathf.Lerp(1f, 1.15f, player.Adrenaline);

                    if (player.grasps[0] != null && player.HeavyCarry(player.grasps[0].grabbed) && player.grasps[0].grabbed is not Cicada)
                    {
                        adrenalineModifier += Mathf.Min(Mathf.Max(0.0f, player.grasps[0].grabbed.TotalMass - 0.2f) * 1.5f, 1.3f);
                    }
                    player.AerobicIncrease(1f); // what does this do?

                    // switch // case: AnimationIndex.Roll
                    player.rocketJumpFromBellySlide = true;
                    RocketJump(player, adrenalineModifier);
                    player.rollDirection = 0;
                }
                else
                {
                    // early belly slide jump
                    if (MainMod.Option_BellySlide && player.animation == Player.AnimationIndex.BellySlide && player.rollCounter <= 8)
                    {
                        player.bodyChunks[1].pos.y -= 10f;
                    }

                    // don't jump in the wrong direction when beam climbing
                    if (MainMod.Option_WallJump && player.animation == Player.AnimationIndex.ClimbOnBeam && player.input[0].x != 0)
                    {
                        player.flipDirection = player.input[0].x;
                    }

                    // crouch jump // stand up during normal crouch jumps
                    // not during superLaunchJump => possible to bump your head otherwise and mess up jumps
                    // problem: you can stand up during DownOnFours animation; None + Stand + input.y == -1 => DownOnFours + Stand => don't stand up when jumping
                    // BUT: this animation is also used when aborting a crawlTurn; crawlTurn + Default + input.x == 0 => None + Default => None + Stand + no inputs => DownOnFours + Stand => stand up when jumping
                    // this solution is not perfect; you can still press down shortly before crawl turn but the timing is more tight
                    // at least when the option crawl turn is used because then you start rolling by down-diagonal
                    if (MainMod.Option_CrouchJump && player.superLaunchJump < 20 && (player.input.All(input => input.y != -1) && (player.animation == Player.AnimationIndex.DownOnFours || player.animation == Player.AnimationIndex.None && player.bodyMode == Player.BodyModeIndex.Default) || player.animation == Player.AnimationIndex.CrawlTurn || player.bodyMode == Player.BodyModeIndex.Crawl))
                    {
                        orig(player); // uses player.standing
                        player.standing = true;
                    }
                    else
                    {
                        orig(player);
                    }
                }
            }
            else
            {
                orig(player);
            }
        }

        private static void Player_ThrowObject_Option_BellySlide(On.Player.orig_ThrowObject orig, Player player, int grasp, bool eu)
        {
            // belly slide throw // removed timing
            int rollCounter = player.rollCounter;
            player.rollCounter = 10;

            orig(player, grasp, eu);

            player.rollCounter = rollCounter;
        }

        private static void Player_ThrowObject_Option_SpearThrow(On.Player.orig_ThrowObject orig, Player player, int grasp, bool eu)
        {
            // throw weapon // don't get forward momentum on ground or poles
            if (player.grasps[grasp].grabbed is Weapon && player.animation != Player.AnimationIndex.BellySlide && (player.animation != Player.AnimationIndex.Flip || player.input[0].y >= 0 || player.input[0].x != 0))
            {
                if (player.bodyMode == Player.BodyModeIndex.ClimbingOnBeam || player.bodyChunks[1].onSlope != 0)
                {
                    player.bodyChunks[0].vel.x -= player.ThrowDirection * 4f; // total: 4f
                }
                else if (player.IsTileSolid(bChunk: 1, 0, -1))
                {
                    player.bodyChunks[1].vel.x -= player.ThrowDirection * 4f; // total: -8f
                }
            }

            orig(player, grasp, eu);
        }

        // there are cases where this function does not call orig()
        private static void Player_UpdateAnimation(On.Player.orig_UpdateAnimation orig, Player player)
        {
            if (player.room is not Room room)
            {
                orig(player);
                return;
            }

            AttachedFields attachedFields = player.GetAttachedFields();
            BodyChunk bodyChunk0 = player.bodyChunks[0];
            BodyChunk bodyChunk1 = player.bodyChunks[1];

            if (attachedFields.getUpOnBeamAbortCounter > 0) // beam climb
            {
                --attachedFields.getUpOnBeamAbortCounter;
            }

            if (attachedFields.grabBeamCounter > 0)
            {
                --attachedFields.grabBeamCounter;
            }

            // check versus bodyChunk0 since you are only grabbing with your hands
            // if the distance is too low you might instantly re-grab horizontal beams when moving horizontally
            if (attachedFields.grabBeamCooldownPos is Vector2 grabBeamCooldownPos && Vector2.Distance(grabBeamCooldownPos, bodyChunk0.pos) >= 25f)
            {
                attachedFields.grabBeamCooldownPos = null;
            }

            if (attachedFields.soundCooldown > 0)
            {
                --attachedFields.soundCooldown;
            }

            if (attachedFields.jumpPressedCounter > 0) // ledge grab
            {
                --attachedFields.jumpPressedCounter;
            }

            if (player.animation == Player.AnimationIndex.None)
            {
                orig(player);
                return;
            }

            if (MainMod.Option_Crawl && player.animation == Player.AnimationIndex.CorridorTurn && player.corridorTurnCounter < 30)
            {
                player.corridorTurnCounter = 30;
            }

            if (MainMod.Option_CrouchJump && player.bodyMode == Player.BodyModeIndex.Crawl && player.superLaunchJump > 0 && player.superLaunchJump < 10)
            {
                player.superLaunchJump = 10;
            }

            // belly slide // backflip always possible // do a longer version by default
            if (MainMod.Option_BellySlide && player.animation == Player.AnimationIndex.BellySlide)
            {
                UpdateAnimationCounter(player);
                if (player.slideCounter > 0) // no backflips after belly slide
                {
                    player.slideCounter = 0;
                }

                player.allowRoll = 0; // might get set otherwise when sliding fast on slopes or over a small gap // prevent direct transition from belly slide to roll
                player.bodyMode = Player.BodyModeIndex.Default;
                player.standing = false;

                // stop belly slide to get into holes in the ground
                if (player.input[0].y < 0 && player.input[0].downDiagonal == 0 && player.input[0].x == 0 && player.rollCounter > 10 && room.GetTilePosition(bodyChunk0.pos).y == room.GetTilePosition(bodyChunk1.pos).y)
                {
                    IntVector2 tilePosition = room.GetTilePosition(player.mainBodyChunk.pos);
                    if (!room.GetTile(tilePosition + new IntVector2(0, -1)).Solid && room.GetTile(tilePosition + new IntVector2(-1, -1)).Solid && room.GetTile(tilePosition + new IntVector2(1, -1)).Solid)
                    {
                        bodyChunk0.pos = room.MiddleOfTile(bodyChunk0.pos) + new Vector2(0.0f, -20f);
                        bodyChunk1.pos = Vector2.Lerp(bodyChunk1.pos, bodyChunk0.pos + new Vector2(0.0f, player.bodyChunkConnections[0].distance), 0.5f);
                        bodyChunk0.vel = new Vector2(0.0f, -11f);
                        bodyChunk1.vel = new Vector2(0.0f, -11f);

                        player.animation = Player.AnimationIndex.None;
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

                if (player.rollCounter < 6 && !player.isRivulet)
                {
                    bodyChunk1.vel.x -= 9.1f * player.rollDirection;
                    bodyChunk1.vel.y += 2f; // default: 2.7f
                }
                else if (player.IsTileSolidOrSlope(chunkIndex: 1, 0, -1) || player.IsTileSolidOrSlope(chunkIndex: 1, 0, -2))
                {
                    bodyChunk1.vel.y -= 3f; // stick better to slopes // default: -0.5f
                }

                if (player.IsTileSolidOrSlope(chunkIndex: 0, 0, -1) || player.IsTileSolidOrSlope(chunkIndex: 0, 0, -2))
                {
                    bodyChunk0.vel.y -= 3f; // default: -2.3f
                }

                float longBellySlide = 14f;
                float normalBellySlide = 16.7f; // default: 18.1f

                if (player.isRivulet)
                {
                    longBellySlide = 20f;
                    normalBellySlide = 23.1f;
                }
                else if (player.isGourmand)
                {
                    if (player.gourmandExhausted)
                    {
                        longBellySlide = 10f;
                        normalBellySlide = 12.9f;
                    }
                    else
                    {
                        longBellySlide = 40f;
                        normalBellySlide = 41.5f;
                    }
                }
                else if (player.isSlugpup)
                {
                    longBellySlide = 7f;
                    normalBellySlide = 8.3f;
                }
                bodyChunk0.vel.x += (player.longBellySlide ? longBellySlide : normalBellySlide) * player.rollDirection * Mathf.Sin((float)(player.rollCounter / (player.longBellySlide ? 39.0 : 20.0) * Math.PI));

                foreach (BodyChunk bodyChunk in player.bodyChunks)
                {
                    if (bodyChunk.contactPoint.y == 0)
                    {
                        bodyChunk.vel.x *= player.surfaceFriction;
                    }
                }

                int longRollCounter = 39; // default: 34
                int normalRollCounter = 20; // default: 12

                if (player.isRivulet)
                {
                    longRollCounter = 23;
                    normalRollCounter = 10;
                }

                // finish // abort when mid-air // don't cancel belly slides on slopes
                if (player.rollCounter > (player.longBellySlide ? longRollCounter : normalRollCounter) || player.canJump == 0 && !player.IsTileSolidOrSlope(chunkIndex: 0, 0, -1) && !player.IsTileSolidOrSlope(chunkIndex: 1, 0, -1))
                {
                    player.rollDirection = 0;
                    player.animation = Player.AnimationIndex.None;
                    player.standing = true;
                    player.longBellySlide = false;
                    return;
                }
                return;
            }

            // deep swim // ignore jump input // increase speed 
            else if (MainMod.Option_Swim && player.animation == Player.AnimationIndex.DeepSwim)
            {
                // don't update twice;
                // UpdateAnimationCounter(player);

                if (player.slugcatStats.lungsFac != 0.0f)
                {
                    player.slugcatStats.lungsFac = 0.0f;
                }

                // only lose airInLungs when grabbed by leeches or rain timer is up
                if (player.abstractCreature.world?.rainCycle.TimeUntilRain <= 0)
                {
                    player.slugcatStats.lungsFac = 1f;
                }
                else
                {
                    foreach (Creature.Grasp creature in player.grabbedBy)
                    {
                        if (creature.grabber is Leech)
                        {
                            player.slugcatStats.lungsFac = 1f;
                            break;
                        }
                    }
                }

                // not sure why I had these;
                // maybe I just removed the jump boost since you can breath underwater;
                // player.dynamicRunSpeed[0] = 0.0f;
                // player.dynamicRunSpeed[1] = 0.0f;

                // if (player.grasps[0] != null && player.grasps[0].grabbed is JetFish jetFish && jetFish.Consious)
                // {
                //     player.waterFriction = 1f;
                //     return;
                // }

                // player.canJump = 0;
                // player.standing = false;
                // player.GoThroughFloors = true;

                // float num1 = (float)((Mathf.Abs(Vector2.Dot(bodyChunk0.vel.normalized, (bodyChunk0.pos - bodyChunk1.pos).normalized)) + (double)Mathf.Abs(Vector2.Dot(bodyChunk1.vel.normalized, (bodyChunk0.pos - bodyChunk1.pos).normalized))) / 2.0);

                // player.swimCycle += 0.01f;
                // if (player.input[0].ZeroGGamePadIntVec.x != 0 || player.input[0].ZeroGGamePadIntVec.y != 0)
                // {
                //     float num2 = (float)(0.2 + Mathf.InverseLerp(0.0f, 12f, Vector2.Angle(bodyChunk0.lastPos - bodyChunk1.lastPos, bodyChunk0.pos - bodyChunk1.pos)) * 0.8);
                //     if (player.slowMovementStun > 0)
                //         num2 *= 0.5f;

                //     float to = num2 * Mathf.Lerp(1f, 1.2f, player.Adrenaline);
                //     player.swimForce = (double)to <= player.swimForce ? Mathf.Lerp(player.swimForce, to, 0.05f) : Mathf.Lerp(player.swimForce, to, 0.7f);
                //     player.swimCycle += Mathf.Lerp(player.swimForce, 1f, 0.5f) / 10f;

                //     if (player.airInLungs < 0.5 && (double)player.airInLungs > 1 / 6)
                //         player.swimCycle += 0.05f;
                //     if (bodyChunk0.contactPoint.x != 0 || bodyChunk0.contactPoint.y != 0)
                //         player.swimForce *= 0.5f;

                //     if (player.swimCycle > 4.0)
                //         player.swimCycle = 0.0f;
                //     else if (player.swimCycle > 3.0)
                //         bodyChunk0.vel += Custom.DirVec(bodyChunk1.pos, bodyChunk0.pos) * 0.7f * Mathf.Lerp(player.swimForce, 1f, 0.5f) * bodyChunk0.submersion;

                //     Vector2 vector2 = player.SwimDir(true);
                //     if (player.airInLungs < 0.3)
                //         vector2 = Vector3.Slerp(vector2, new Vector2(0.0f, 1f), Mathf.InverseLerp(0.3f, 0.0f, player.airInLungs));

                //     bodyChunk0.vel += vector2 * 0.5f * player.swimForce * Mathf.Lerp(num1, 1f, 0.5f) * bodyChunk0.submersion;
                //     bodyChunk1.vel -= vector2 * 0.1f * bodyChunk0.submersion;
                //     bodyChunk0.vel += Custom.DirVec(bodyChunk1.pos, bodyChunk0.pos) * 0.4f * player.swimForce * num1 * bodyChunk0.submersion;

                //     if (bodyChunk0.vel.magnitude < 6.0)
                //     {
                //         bodyChunk0.vel += vector2 * 0.2f * Mathf.InverseLerp(6f, 1.5f, bodyChunk0.vel.magnitude);
                //         bodyChunk1.vel -= vector2 * 0.1f * Mathf.InverseLerp(6f, 1.5f, bodyChunk0.vel.magnitude);
                //     }
                // }

                // player.waterFriction = Mathf.Lerp(0.92f, 0.96f, num1);
                // if (player.bodyMode == Player.BodyModeIndex.Swimming)
                //     return;
                // player.animation = Player.AnimationIndex.None;
                // return;
            }

            // ledge grab 
            else if (player.animation == Player.AnimationIndex.LedgeGrab)
            {
                if (MainMod.Option_WallJump && (player.canWallJump == 0 || Math.Sign(player.canWallJump) == -Math.Sign(player.flipDirection)))
                {
                    player.canWallJump = player.flipDirection * -15; // you can do a (mid-air) wall jump off a ledge grab
                }

                if (MainMod.Option_LedgeGrab)
                {
                    if (player.input[0].jmp && attachedFields.jumpPressedCounter < 20)
                    {
                        attachedFields.jumpPressedCounter = 20;
                    }

                    // holds the ledge grab animation until jump is pressed
                    if (attachedFields.jumpPressedCounter == 0 && player.IsTileSolid(0, player.flipDirection, 0) && !player.IsTileSolid(0, player.flipDirection, 1) && room.GetTile(player.abstractCreature.pos.Tile + new IntVector2(player.flipDirection * 2, 0)).Terrain != Room.Tile.TerrainType.ShortcutEntrance && room.GetTile(player.abstractCreature.pos.Tile + new IntVector2(player.flipDirection * 2, 1)).Terrain != Room.Tile.TerrainType.ShortcutEntrance) // dont stay in ledge grab when at a shortcut ledge
                    {
                        player.ledgeGrabCounter = 0;
                        bodyChunk0.pos -= new Vector2(0.0f, 4f);
                        bodyChunk1.vel.x -= player.flipDirection;

                        if (player.input[0].x == player.flipDirection || player.input[0].y == 1)
                        {
                            bodyChunk1.vel -= new Vector2(-0.5f * player.flipDirection, -0.5f);
                        }

                        if (player.graphicsModule is PlayerGraphics playerGraphics)
                        {
                            // does not trigger otherwise when you "fall" onto a ledge grab
                            playerGraphics.hands[0].mode = Limb.Mode.HuntAbsolutePosition;
                            playerGraphics.hands[1].mode = Limb.Mode.HuntAbsolutePosition;
                        }
                    }
                }
            }

            // beam climb 
            // don't start a else if statement with an option check unless it's last check;
            // this one is the last;
            else if (MainMod.Option_BeamClimb)
            {
                // velocity gained in x direction each frame (if no slowmovementstun, and bodyMode is default)
                float velXGain = 2.4f * Mathf.Lerp(1f, 1.2f, player.Adrenaline) * player.surfaceFriction * player.EffectiveRoomGravity;
                if (player.slowMovementStun > 0)
                {
                    velXGain *= 0.4f + 0.6f * Mathf.InverseLerp(10f, 0.0f, player.slowMovementStun);
                }

                // grab beams by holding down // extends some cases when holding up -- forget which ones :/ // don't grab beams while falling inside corridors
                // if ((player.input[0].y != 0 || attachedFields.grabBeamCounter > 0) && player.animation == Player.AnimationIndex.None && player.bodyMode == Player.BodyModeIndex.Default && (!player.IsTileSolid(bChunk: 0, -1, 0) || !player.IsTileSolid(bChunk: 0, 1, 0)) && (!player.IsTileSolid(bChunk: 1, -1, 0) || !player.IsTileSolid(bChunk: 1, 1, 0)))
                // {
                //     if (room.GetTile(bodyChunk0.pos).verticalBeam && (attachedFields.grabBeamCooldownPosY == null || attachedFields.grabBeamCooldownPosY - bodyChunk0.pos.y >= 20f))
                //     {
                //         if (attachedFields.soundCooldown == 0)
                //         {
                //             attachedFields.soundCooldown = 40;
                //             room.PlaySound(SoundID.Slugcat_Grab_Beam, player.mainBodyChunk, false, 1f, 1f);
                //         }

                //         float middleOfTileX = room.MiddleOfTile(bodyChunk0.pos).x;
                //         player.flipDirection = Mathf.Abs(bodyChunk0.vel.x) <= 5f ? (bodyChunk0.pos.x >= middleOfTileX ? 1 : -1) : (bodyChunk0.vel.x >= 0.0f ? 1 : -1);

                //         bodyChunk0.pos.x = middleOfTileX;
                //         bodyChunk0.vel = new Vector2(0.0f, 0.0f);
                //         bodyChunk1.vel.y = 0.0f;
                //         player.animation = Player.AnimationIndex.ClimbOnBeam;
                //     }
                //     else
                //     {
                //         int x = room.GetTilePosition(bodyChunk0.pos).x;
                //         for (int y = room.GetTilePosition(bodyChunk0.lastPos).y; y >= room.GetTilePosition(bodyChunk0.pos).y; --y)
                //         {
                //             if (room.GetTile(x, y).horizontalBeam && (attachedFields.grabBeamCooldownPosY == null || attachedFields.grabBeamCooldownPosY - bodyChunk0.pos.y >= 20f))
                //             {
                //                 if (attachedFields.soundCooldown == 0)
                //                 {
                //                     attachedFields.soundCooldown = 40;
                //                     room.PlaySound(SoundID.Slugcat_Grab_Beam, player.mainBodyChunk, false, 1f, 1f);
                //                 }

                //                 bodyChunk0.pos.y = room.MiddleOfTile(new IntVector2(x, y)).y;
                //                 bodyChunk1.vel.y = 0.0f;
                //                 player.animation = Player.AnimationIndex.HangFromBeam;
                //                 break;
                //             }
                //         }
                //     }
                // }

                if (player.animation == Player.AnimationIndex.HangFromBeam)
                {
                    UpdateAnimationCounter(player);
                    player.bodyMode = Player.BodyModeIndex.ClimbingOnBeam;
                    player.standing = true;

                    bodyChunk0.vel.x *= 0.2f;
                    bodyChunk0.vel.y = 0.0f;
                    bodyChunk0.pos.y = room.MiddleOfTile(bodyChunk0.pos).y;

                    if (player.input[0].x != 0 && bodyChunk0.contactPoint.x != player.input[0].x)
                    {
                        Room.Tile tile = room.GetTile(bodyChunk0.pos + new Vector2(12f * player.input[0].x, 0.0f));
                        if (tile.horizontalBeam)
                        {
                            if (bodyChunk1.contactPoint.x != player.input[0].x)
                            {
                                bodyChunk0.vel.x += player.input[0].x * Mathf.Lerp(1.2f, 1.4f, player.Adrenaline) * player.slugcatStats.poleClimbSpeedFac * Custom.LerpMap(player.slowMovementStun, 0.0f, 10f, 1f, 0.5f);
                            }

                            bodyChunk1.vel.x += player.flipDirection * (0.5f + 0.5f * Mathf.Sin((float)(player.animationFrame / 20.0 * Math.PI * 2.0))) * -0.5f;
                            ++player.animationFrame;

                            if (player.animationFrame > 20)
                            {
                                player.animationFrame = 1;
                                room.PlaySound(SoundID.Slugcat_Climb_Along_Horizontal_Beam, player.mainBodyChunk, false, 1f, 1f);
                                player.AerobicIncrease(0.05f);
                            }
                        }
                        else if (!tile.Solid && player.input[1].y != 1) // stop at end of horizontal beam // leaning
                        {
                            bodyChunk0.pos.x = room.MiddleOfTile(bodyChunk0.pos).x;
                            bodyChunk0.vel.x -= leanFactor * player.input[0].x;
                            bodyChunk1.vel.x += leanFactor * player.input[0].x;
                        }
                    }
                    else if (player.animationFrame < 10)
                    {
                        ++player.animationFrame;
                    }
                    else if (player.animationFrame > 10)
                    {
                        --player.animationFrame;
                    }

                    // ----- //
                    // exits //
                    // ----- //

                    // stand on ground
                    // don't exit animation when leaving corridor with beam horizontally
                    if (bodyChunk1.contactPoint.y < 0 && player.input[0].y < 0 && Math.Abs(bodyChunk0.pos.x - bodyChunk1.pos.x) < 5f)
                    {
                        player.room.PlaySound(SoundID.Slugcat_Regain_Footing, player.mainBodyChunk, false, 1f, 1f);
                        player.animation = Player.AnimationIndex.StandUp;
                        return;
                    }

                    if (SwitchHorizontalToVerticalBeam(player, attachedFields)) return;// grab vertical beam if possible

                    if (player.input[0].jmp && !player.input[1].jmp)
                    {
                        if (player.tubeWorm?.tongues[0].Attached == true) return; // retract tubeWorm first // consistent behavior with when standing on beam and pressing jump

                        if (player.input[0].y == 1) // only drop when pressing jump without holding up
                        {
                            PrepareGetUpOnBeamAnimation(player, 1, attachedFields);
                            return;
                        }
                        else if (player.input[0].y == -1 && player.IsTileSolid(1, 0, -1))
                        {
                            // this case would lead to jumping + regrabbing beam otherwise
                            // not clean..
                            player.input[1].jmp = true;
                            // player.canJump = 0;
                        }

                        attachedFields.dontUseTubeWormCounter = 2; // don't drop and shoot tubeWorm at the same time
                        attachedFields.grabBeamCooldownPos = bodyChunk0.pos;
                        player.animation = Player.AnimationIndex.None;
                        return;
                    }
                    else if (player.input[0].y == 1 && player.input[1].y == 0)
                    {
                        PrepareGetUpOnBeamAnimation(player, 1, attachedFields);
                        return;
                    }

                    if (!room.GetTile(bodyChunk0.pos).horizontalBeam)
                    {
                        player.animation = Player.AnimationIndex.None;
                    }
                    return;
                }
                else if (player.animation == Player.AnimationIndex.GetUpOnBeam)
                {
                    // GetUpOnBeam and GetDownOnBeam
                    int direction = attachedFields.getUpOnBeamDirection; // -1 (down) or 1 (up)
                    int bodyChunkIndex = direction == 1 ? 1 : 0;

                    // otherwise this is bugged when pressing jump during this animation
                    // => drops slugcat when StandOnBeam animation is reached;
                    player.canJump = 0;

                    player.bodyMode = Player.BodyModeIndex.ClimbingOnBeam;
                    bodyChunk0.vel.x = 0.0f;
                    bodyChunk0.vel.y = 0.0f;

                    if (direction == 1)
                    {
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

                    bodyChunk0.vel += Custom.DirVec(bodyChunk0.pos, player.upOnHorizontalBeamPos) * 1.8f;
                    bodyChunk1.vel += Custom.DirVec(bodyChunk1.pos, player.upOnHorizontalBeamPos + new Vector2(0.0f, -20f)) * 1.8f;

                    // ----- //
                    // exits //
                    // ----- //

                    if (room.GetTile(player.bodyChunks[bodyChunkIndex].pos).horizontalBeam && Math.Abs(player.bodyChunks[bodyChunkIndex].pos.y - player.upOnHorizontalBeamPos.y) < 25.0)
                    {
                        // this might be helpful when horizontal beams are stacked vertically;
                        // however, this can lead to a bug where you are not able to grab beams after jumping off;
                        // => reduce this counter as a workaround;
                        player.noGrabCounter = 5; // vanilla: 15

                        player.animation = direction == 1 ? Player.AnimationIndex.StandOnBeam : Player.AnimationIndex.HangFromBeam;
                        player.bodyChunks[bodyChunkIndex].pos.y = room.MiddleOfTile(player.bodyChunks[bodyChunkIndex].pos).y + direction * 5f;
                        player.bodyChunks[bodyChunkIndex].vel.y = 0.0f;
                        return;
                    }

                    // revert when bumping into something or pressing the opposite direction
                    if (player.input[0].y == -direction)
                    {
                        player.upOnHorizontalBeamPos -= direction * new Vector2(0.0f, 20f);
                        attachedFields.getUpOnBeamDirection = -direction;
                        return;
                    }
                    else if (bodyChunk0.contactPoint.y == direction || bodyChunk1.contactPoint.y == direction)
                    {
                        if (attachedFields.getUpOnBeamAbortCounter > 0) // revert to the original position should always work // abort if stuck in a loop just in case
                        {
                            attachedFields.grabBeamCounter = 15;
                            player.animation = Player.AnimationIndex.None;
                            return;
                        }
                        else
                        {
                            attachedFields.getUpOnBeamAbortCounter = 2;
                        }

                        player.upOnHorizontalBeamPos -= direction * new Vector2(0.0f, 20f);
                        attachedFields.getUpOnBeamDirection = -direction;
                        return;
                    }

                    if ((room.GetTile(bodyChunk0.pos).horizontalBeam || room.GetTile(bodyChunk1.pos).horizontalBeam) && Custom.DistLess(player.bodyChunks[1 - bodyChunkIndex].pos, player.upOnHorizontalBeamPos, 30f)) return; // default: 25f
                    player.animation = Player.AnimationIndex.None;
                    return;
                }
                else if (player.animation == Player.AnimationIndex.StandOnBeam)
                {
                    bool isWallClimbing = player.bodyMode == Player.BodyModeIndex.WallClimb && bodyChunk1.contactPoint.x != 0;
                    UpdateAnimationCounter(player);

                    player.bodyMode = Player.BodyModeIndex.ClimbingOnBeam;
                    player.standing = true;
                    player.canJump = 5;
                    bodyChunk1.vel.x *= 0.5f;

                    if (player.input[0].x != 0 && bodyChunk1.contactPoint.x != player.input[0].x)
                    {
                        Room.Tile tile = room.GetTile(bodyChunk1.pos + new Vector2(12f * player.input[0].x, 0.0f));
                        if (tile.horizontalBeam)
                        {
                            // run normally (like when on ground) with reduced(?) player.dynamicRunSpeed
                            ++player.animationFrame;
                            if (player.animationFrame > 6)
                            {
                                player.animationFrame = 0;
                                room.PlaySound(SoundID.Slugcat_Walk_On_Horizontal_Beam, player.mainBodyChunk, false, 1f, 1f);
                            }
                        }
                        else if (!tile.Solid)
                        {
                            if (player.input[1].y != -1) // leaning
                            {
                                if (player.input[0].jmp && !player.input[1].jmp) // jump from leaning
                                {
                                    bodyChunk0.vel.x += player.input[0].x * velXGain; // player.dynamicRunSpeed[0];
                                    bodyChunk1.vel.x += player.input[0].x * velXGain;
                                }
                                else
                                {
                                    bodyChunk1.pos.x = room.MiddleOfTile(bodyChunk1.pos).x;
                                    bodyChunk0.vel.x -= player.input[0].x * (velXGain - leanFactor);
                                    bodyChunk1.vel.x -= player.input[0].x * (velXGain + leanFactor);
                                }
                            }
                            else // stop at the end of horizontal beam
                            {
                                bodyChunk1.pos.x = room.MiddleOfTile(bodyChunk1.pos).x;
                                bodyChunk0.vel.x -= player.input[0].x * velXGain;
                                bodyChunk1.vel.x -= player.input[0].x * velXGain;
                            }
                        }
                    }
                    else if (player.animationFrame > 0)
                    {
                        player.animationFrame = 0;
                    }

                    // ----- //
                    // exits //
                    // ----- //

                    // grab vertical beam if possible
                    if (SwitchHorizontalToVerticalBeam(player, attachedFields)) return;

                    if (isWallClimbing)
                    {
                        player.animation = Player.AnimationIndex.None;
                        return;
                    }

                    if (bodyChunk0.contactPoint.y < 1 || !player.IsTileSolid(bChunk: 1, 0, 1))
                    {
                        bodyChunk1.vel.y = 0.0f;
                        bodyChunk1.pos.y = room.MiddleOfTile(bodyChunk1.pos).y + 5f;
                        bodyChunk0.vel.y += 2f;

                        player.dynamicRunSpeed[0] = 2.1f * player.slugcatStats.runspeedFac;
                        player.dynamicRunSpeed[1] = 2.1f * player.slugcatStats.runspeedFac;
                    }
                    else
                    {
                        // stop moving forward when bumping your "head" into something
                        bodyChunk0.vel.x -= player.input[0].x * velXGain;
                        bodyChunk1.vel.x -= player.input[0].x * velXGain;
                    }

                    // move down to HangFromBeam
                    if (player.input[0].y == -1 && (player.input[1].y == 0 || player.input[0].jmp && !player.input[1].jmp))
                    {
                        PrepareGetUpOnBeamAnimation(player, -1, attachedFields);
                        return;
                    }

                    // grab nearby horizontal beams
                    if (player.input[0].y == 1 && player.input[1].y == 0 && room.GetTile(room.GetTilePosition(bodyChunk0.pos) + new IntVector2(0, 1)).horizontalBeam)
                    {
                        bodyChunk0.pos.y += 8f;
                        bodyChunk1.pos.y += 8f;
                        player.animation = Player.AnimationIndex.HangFromBeam;
                    }
                    return;
                }
                else if (player.animation == Player.AnimationIndex.ClimbOnBeam)
                {
                    UpdateAnimationCounter(player);
                    player.bodyMode = Player.BodyModeIndex.ClimbingOnBeam;
                    player.standing = true;
                    player.canJump = 1;

                    foreach (BodyChunk bodyChunk in player.bodyChunks)
                    {
                        if (bodyChunk.contactPoint.x != 0)
                        {
                            player.flipDirection = -bodyChunk.contactPoint.x;
                            break;
                        }
                    }

                    bool shouldSwitchSides = player.IsTileSolid(0, 0, 1) || player.input[0].y <= 0 || (bodyChunk0.contactPoint.y >= 0 && !player.IsTileSolid(0, player.flipDirection, 1));
                    if (shouldSwitchSides && player.IsTileSolid(0, player.flipDirection, 0))
                    {
                        player.flipDirection = -player.flipDirection;
                    }

                    if (shouldSwitchSides)
                    {
                        bodyChunk0.pos.x = (bodyChunk0.pos.x + player.room.MiddleOfTile(bodyChunk0.pos).x + player.flipDirection * 5f) / 2f;
                        bodyChunk1.pos.x = (bodyChunk1.pos.x * 7f + player.room.MiddleOfTile(bodyChunk0.pos).x + player.flipDirection * 5f) / 8f;
                    }
                    else
                    {
                        bodyChunk0.pos.x = (bodyChunk0.pos.x + player.room.MiddleOfTile(bodyChunk0.pos).x) / 2f;
                        bodyChunk1.pos.x = (bodyChunk1.pos.x * 7f + player.room.MiddleOfTile(bodyChunk0.pos).x) / 8f;
                    }

                    bodyChunk0.vel.x = 0f;
                    bodyChunk0.vel.y = 0.5f * bodyChunk0.vel.y + 1f + player.gravity;
                    bodyChunk1.vel.y -= 1f - player.gravity;

                    if (player.input[0].y > 0)
                    {
                        player.animationFrame++;
                        if (player.animationFrame > 20)
                        {
                            player.animationFrame = 0;
                            player.room.PlaySound(SoundID.Slugcat_Climb_Up_Vertical_Beam, player.mainBodyChunk, false, 1f, 1f);
                            player.AerobicIncrease(0.1f);
                        }
                        bodyChunk0.vel.y += Mathf.Lerp(1f, 1.4f, player.Adrenaline) * player.slugcatStats.poleClimbSpeedFac * Custom.LerpMap(player.slowMovementStun, 0f, 10f, 1f, 0.2f);
                    }
                    else if (player.input[0].y < 0)
                    {
                        bodyChunk0.vel.y -= 2.2f * (0.2f + 0.8f * player.EffectiveRoomGravity);
                    }

                    if (player.slideUpPole > 0)
                    {
                        player.slideUpPole--;
                        if (player.slideUpPole > 8)
                        {
                            player.animationFrame = 12;
                        }
                        if (player.slideUpPole == 0)
                        {
                            player.slowMovementStun = Math.Max(player.slowMovementStun, 16);
                        }
                        if (player.slideUpPole > 14)
                        {
                            bodyChunk0.pos.y += 2f;
                            bodyChunk1.pos.y += 2f;
                        }

                        bodyChunk0.vel.y += Custom.LerpMap(player.slideUpPole, 17f, 0f, 3f, -1.2f, 0.45f);
                        bodyChunk1.vel.y += Custom.LerpMap(player.slideUpPole, 17f, 0f, 1.5f, -1.4f, 0.45f);
                    }
                    player.GoThroughFloors = player.input[0].x == 0 && player.input[0].downDiagonal == 0;

                    // grab other parallel vertical beam
                    if (player.input[0].x == player.flipDirection && player.input[1].x == 0 && player.flipDirection == player.lastFlipDirection && player.room.GetTile(player.room.GetTilePosition(bodyChunk0.pos) + new IntVector2(player.flipDirection, 0)).verticalBeam)
                    {
                        bodyChunk0.pos.x = player.room.MiddleOfTile(player.room.GetTilePosition(bodyChunk0.pos) + new IntVector2(player.flipDirection, 0)).x - (float)player.flipDirection * 5f;
                        player.flipDirection = -player.flipDirection;
                        player.jumpStun = 11 * player.flipDirection;
                    }

                    //
                    // exits
                    //

                    // stand on ground
                    if (bodyChunk1.contactPoint.y < 0 && player.input[0].y < 0)
                    {
                        player.room.PlaySound(SoundID.Slugcat_Regain_Footing, player.mainBodyChunk, false, 1f, 1f);
                        player.animation = Player.AnimationIndex.StandUp;
                        return;
                    }

                    // switch to horizontal beams
                    if (SwitchVerticalToHorizontalBeam(player, attachedFields)) return;

                    // lose grip
                    if (!player.room.GetTile(bodyChunk0.pos).verticalBeam)
                    {
                        if (player.room.GetTile(player.room.GetTilePosition(bodyChunk0.pos) + new IntVector2(0, -1)).verticalBeam)
                        {
                            player.room.PlaySound(SoundID.Slugcat_Get_Up_On_Top_Of_Vertical_Beam_Tip, player.mainBodyChunk, false, 1f, 1f);
                            player.animation = Player.AnimationIndex.GetUpToBeamTip;

                            // otherwise it might cancel the GetUpToBeamTip animation before it gets reached;
                            player.wantToJump = 0;
                        }
                        else if (player.room.GetTile(player.room.GetTilePosition(bodyChunk0.pos) + new IntVector2(0, 1)).verticalBeam)
                        {
                            player.animation = Player.AnimationIndex.HangUnderVerticalBeam;
                        }
                        else
                        {
                            player.animation = Player.AnimationIndex.None;
                        }
                    }
                    return;
                }
                else if (player.animation == Player.AnimationIndex.GetUpToBeamTip)
                {
                    // otherwise you might jump early when reaching the BeamTip;
                    player.wantToJump = 0;

                    // don't let go of beam while climbing to the top // don't prevent player from entering corridors
                    // if (bodyChunk0.contactPoint.x == 0 && bodyChunk1.contactPoint.x == 0)
                    foreach (BodyChunk bodyChunk in player.bodyChunks)
                    {
                        Room.Tile tile = room.GetTile(bodyChunk.pos);
                        if (!tile.verticalBeam && room.GetTile(tile.X, tile.Y - 1).verticalBeam)
                        {
                            float middleOfTileX = room.MiddleOfTile(tile.X, tile.Y).x;
                            // give a bit of protection against wind and horizontal momentum
                            bodyChunk0.pos.x += Mathf.Clamp(middleOfTileX - bodyChunk0.pos.x, -2f * velXGain, 2f * velXGain);
                            bodyChunk1.pos.x += Mathf.Clamp(middleOfTileX - bodyChunk1.pos.x, -2f * velXGain, 2f * velXGain);

                            // you might get stuck from solid tiles above;
                            // do a auto-regrab like you can do when pressing down while being on the beam tip;
                            if (player.input[0].y < 0)
                            {
                                attachedFields.grabBeamCounter = 15;
                                attachedFields.dontUseTubeWormCounter = 2;
                                player.canJump = 0;
                                player.animation = Player.AnimationIndex.None;
                                break;
                            }

                            // ignore x input
                            if (player.input[0].x != 0 && !player.IsTileSolid(bChunk: 0, player.input[0].x, 0) && !player.IsTileSolid(bChunk: 1, player.input[0].x, 0))
                            {
                                bodyChunk0.vel.x -= player.input[0].x * velXGain;
                                bodyChunk1.vel.x -= player.input[0].x * velXGain;
                            }
                            break;
                        }
                    }
                }
                else if (player.animation == Player.AnimationIndex.HangUnderVerticalBeam)
                {
                    UpdateAnimationCounter(player);
                    player.bodyMode = Player.BodyModeIndex.ClimbingOnBeam; // gets updated and is default afterwards
                    player.standing = false;

                    // drop when pressing jump
                    if (player.input[0].jmp && !player.input[1].jmp || bodyChunk1.vel.magnitude > 10.0 || bodyChunk0.vel.magnitude > 10.0 || !room.GetTile(bodyChunk0.pos + new Vector2(0.0f, 20f)).verticalBeam)
                    {
                        attachedFields.dontUseTubeWormCounter = 2;
                        player.animation = Player.AnimationIndex.None;
                        player.standing = true;
                    }
                    else
                    {
                        bodyChunk0.pos.x = Mathf.Lerp(bodyChunk0.pos.x, room.MiddleOfTile(bodyChunk0.pos).x, 0.5f);
                        bodyChunk0.pos.y = Mathf.Max(bodyChunk0.pos.y, room.MiddleOfTile(bodyChunk0.pos).y + 5f + bodyChunk0.vel.y);

                        bodyChunk0.vel.x *= 0.5f; // dont kill all momentum
                        bodyChunk0.vel.x -= player.input[0].x * (velXGain + leanFactor);
                        bodyChunk0.vel.y *= 0.5f;
                        bodyChunk1.vel.x -= player.input[0].x * (velXGain - leanFactor);

                        if (player.input[0].y > 0)
                        {
                            bodyChunk0.vel.y += 2.5f;
                        }

                        if (room.GetTile(bodyChunk0.pos).verticalBeam)
                        {
                            player.animation = Player.AnimationIndex.ClimbOnBeam;
                        }
                    }
                    return;
                }

                // BeamTip // don't drop off beam tip by leaning too much
                if (player.animation == Player.AnimationIndex.BeamTip)
                {
                    UpdateAnimationCounter(player);
                    player.bodyMode = Player.BodyModeIndex.ClimbingOnBeam;
                    player.standing = true;
                    player.canJump = 5;

                    bodyChunk1.pos = (bodyChunk1.pos + room.MiddleOfTile(bodyChunk1.pos)) / 2f;
                    bodyChunk1.vel *= 0.5f;

                    if (player.input[0].jmp && !player.input[1].jmp)
                    {
                        bodyChunk0.vel.x += player.input[0].x * velXGain;
                        bodyChunk1.vel.x += player.input[0].x * velXGain;
                    }
                    else if (player.input[0].x == 0 && player.input[0].y == -1)
                    {
                        // wind can make lining yourself up more difficult // on the other hand, wind makes catching the beam below also harder => leave it as is
                        bodyChunk0.pos.x += Mathf.Clamp(bodyChunk1.pos.x - bodyChunk0.pos.x, -velXGain, velXGain);
                    }
                    else
                    {
                        bodyChunk0.vel.x -= player.input[0].x * (velXGain - leanFactor);
                        bodyChunk1.vel.x -= player.input[0].x * (velXGain + leanFactor);
                    }

                    bodyChunk0.vel.y += 1.5f;
                    bodyChunk0.vel.y += player.input[0].y * 0.1f;

                    // what does this do?
                    if (player.input[0].y > 0 && player.input[1].y == 0)
                    {
                        --bodyChunk1.vel.y;
                        player.canJump = 0;
                        player.animation = Player.AnimationIndex.None;
                    }

                    if (player.input[0].y == -1 && (bodyChunk0.pos.x == bodyChunk1.pos.x || player.input[0].jmp && !player.input[1].jmp)) // IsPosXAligned(player)
                    {
                        attachedFields.grabBeamCounter = 15;
                        attachedFields.dontUseTubeWormCounter = 2;
                        player.canJump = 0;
                        player.animation = Player.AnimationIndex.None;
                    }
                    else if (bodyChunk0.pos.y < bodyChunk1.pos.y - 5f || !room.GetTile(bodyChunk1.pos + new Vector2(0.0f, -20f)).verticalBeam)
                    {
                        player.animation = Player.AnimationIndex.None;
                    }
                    return;
                }
            }

            // finish
            if ((MainMod.Option_BellySlide || MainMod.Option_Roll_1) && player.animation == Player.AnimationIndex.RocketJump)
            {
                orig(player);
                if (player.animation == Player.AnimationIndex.None && (player.IsTileSolid(0, 0, -1) || player.IsTileSolid(1, 0, -1))) // stand up after rocket jump
                {
                    AlignPosYOnSlopes(player);
                    player.standing = true;
                    player.animation = Player.AnimationIndex.StandUp;
                }
                else // don't cancel rocket jumps by collision in y
                {
                    for (int chunkIndex = 0; chunkIndex <= 1; ++chunkIndex)
                    {
                        BodyChunk bodyChunk = player.bodyChunks[chunkIndex];
                        if (bodyChunk.contactPoint.y == 1)
                        {
                            bodyChunk.vel.y = 0.0f;
                            player.animation = Player.AnimationIndex.RocketJump;
                            break;
                        }
                    }
                }
            }
            else if (MainMod.Option_BellySlide && player.animation == Player.AnimationIndex.Flip && player.flipFromSlide)
            {
                orig(player);
                if (player.animation == Player.AnimationIndex.None && (player.IsTileSolid(0, 0, -1) || player.IsTileSolid(1, 0, -1))) // stand up after belly slides // don't try to stand up when sliding down walls
                {
                    AlignPosYOnSlopes(player);
                    player.standing = true;
                    player.animation = Player.AnimationIndex.StandUp;
                }
                else // don't cancel flips by collision in y
                {
                    for (int chunkIndex = 0; chunkIndex <= 1; ++chunkIndex)
                    {
                        BodyChunk bodyChunk = player.bodyChunks[chunkIndex];
                        if (bodyChunk.contactPoint.y == 1)
                        {
                            bodyChunk.vel.y = 0.0f;
                            player.animation = Player.AnimationIndex.Flip;
                            break;
                        }
                    }
                }
            }
            else if (MainMod.Option_Roll_1 && player.animation == Player.AnimationIndex.Roll && (player.IsTileSolid(0, 0, -1) || player.IsTileSolid(1, 0, -1))) // stand up after rolls
            {
                Player.AnimationIndex animationIndex = player.animation;
                orig(player);

                if (player.animation != animationIndex)
                {
                    AlignPosYOnSlopes(player);
                    player.standing = true;
                    player.animation = Player.AnimationIndex.StandUp;
                }
            }
            else
            {
                orig(player);
            }

            if (MainMod.Option_Swim && player.animation == Player.AnimationIndex.SurfaceSwim)
            {
                player.swimCycle += 0.01f;
                player.dynamicRunSpeed[0] = 3.5f;
            }

            // crawl // slopes
            if (MainMod.Option_Crawl)
            {
                // stop crawl turn when hitting the ground // might happen early on slopes
                if (player.animation == Player.AnimationIndex.CrawlTurn && player.input[0].x > 0 == player.bodyChunks[0].pos.x >= (double)bodyChunk1.pos.x && player.bodyChunks[0].contactPoint.y == -1)
                {
                    player.animation = Player.AnimationIndex.None;
                }

                // finish ledge crawl when on slopes
                if (player.animation == Player.AnimationIndex.LedgeCrawl && player.bodyChunks[0].onSlope != 0 && bodyChunk1.onSlope != 0)
                {
                    player.animation = Player.AnimationIndex.None;
                }
            }
        }

        // there are cases where this function does not call orig()
        private static void Player_UpdateBodyMode(On.Player.orig_UpdateBodyMode orig, Player player)
        {
            if (player.room is Room room)
            {
                // crawl
                if (MainMod.Option_Crawl && player.bodyMode == Player.BodyModeIndex.Crawl)
                {
                    UpdateBodyModeCounter(player);
                    player.dynamicRunSpeed[0] = 2.5f;

                    if (player.input[0].x != 0 && player.input[0].x > 0 == player.bodyChunks[0].pos.x < (double)player.bodyChunks[1].pos.x && player.crawlTurnDelay > 5 && !player.IsTileSolid(0, 0, 1) && !player.IsTileSolid(1, 0, 1))
                    {
                        AlignPosYOnSlopes(player);
                        player.dynamicRunSpeed[0] *= 0.5f; // default: 0.75f
                        player.crawlTurnDelay = 0;
                        player.animation = Player.AnimationIndex.CrawlTurn;
                    }
                    player.dynamicRunSpeed[1] = player.dynamicRunSpeed[0];

                    if (!player.standing)
                    {
                        foreach (BodyChunk bodyChunk in player.bodyChunks)
                        {
                            if (bodyChunk.contactPoint.y == -1) // bodyChunk.onSlope != 0
                            {
                                bodyChunk.vel.y -= 1.5f;
                            }
                        }
                    }
                    // more requirements than vanilla // prevent collision and sound spam
                    else if ((player.bodyChunks[1].onSlope == 0 || player.input[0].x != -player.bodyChunks[1].onSlope) && (player.lowerBodyFramesOnGround >= 3 || player.bodyChunks[1].contactPoint.y < 0 && room.GetTile(room.GetTilePosition(player.bodyChunks[1].pos) + new IntVector2(0, -1)).Terrain != Room.Tile.TerrainType.Air && room.GetTile(room.GetTilePosition(player.bodyChunks[0].pos) + new IntVector2(0, -1)).Terrain != Room.Tile.TerrainType.Air))
                    {
                        AlignPosYOnSlopes(player);
                        room.PlaySound(SoundID.Slugcat_Stand_Up, player.mainBodyChunk);
                        player.animation = Player.AnimationIndex.StandUp;

                        if (player.input[0].x == 0)
                        {
                            if (player.bodyChunks[1].contactPoint.y == -1 && player.IsTileSolid(1, 0, -1) && !player.IsTileSolid(1, 0, 1))
                            {
                                player.feetStuckPos = new Vector2?(room.MiddleOfTile(room.GetTilePosition(player.bodyChunks[1].pos)) + new Vector2(0.0f, player.bodyChunks[1].rad - 10f));
                            }
                            else if (player.bodyChunks[0].contactPoint.y == -1 && player.IsTileSolid(0, 0, -1) && !player.IsTileSolid(0, 0, 1))
                            {
                                player.feetStuckPos = new Vector2?(player.bodyChunks[0].pos + new Vector2(0.0f, -1f));
                            }
                        }
                        return;
                    }

                    if (player.bodyChunks[0].contactPoint.y > -1 && player.input[0].x != 0 && player.bodyChunks[1].pos.y < player.bodyChunks[0].pos.y - 3.0 && player.bodyChunks[1].contactPoint.x == player.input[0].x)
                    {
                        ++player.bodyChunks[1].pos.y;
                    }

                    if (player.input[0].y < 0)
                    {
                        player.GoThroughFloors = true;
                        for (int chunkIndex = 0; chunkIndex < 2; ++chunkIndex)
                        {
                            if (!player.IsTileSolidOrSlope(chunkIndex, 0, -1) && (player.IsTileSolidOrSlope(chunkIndex, -1, -1) || player.IsTileSolidOrSlope(chunkIndex, 1, -1))) // push into shortcuts and holes but don't stand still on slopes
                            {
                                BodyChunk bodyChunk = player.bodyChunks[chunkIndex];
                                bodyChunk.vel.x = 0.8f * bodyChunk.vel.x + 0.4f * (room.MiddleOfTile(bodyChunk.pos).x - bodyChunk.pos.x);
                                --bodyChunk.vel.y;
                                break;
                            }
                        }
                    }

                    if (player.input[0].x != 0 && Mathf.Abs(player.bodyChunks[1].pos.x - player.bodyChunks[1].lastPos.x) > 0.5)
                    {
                        ++player.animationFrame;
                    }
                    else
                    {
                        player.animationFrame = 0;
                    }

                    if (player.animationFrame <= 10)
                    {
                        return;
                    }

                    player.animationFrame = 0;
                    room.PlaySound(SoundID.Slugcat_Crawling_Step, player.mainBodyChunk);
                    return;
                }

                // wall climb & jump // crawl downwards when holding down // crawl upwards when holding up
                if ((MainMod.Option_WallClimb || MainMod.Option_WallJump) && player.bodyMode == Player.BodyModeIndex.WallClimb)
                {
                    UpdateBodyModeCounter(player);
                    player.canJump = 1;
                    player.standing = true;

                    // don't climb on one-tile "walls" instead of crawling (for example)
                    if (player.bodyChunks[1].contactPoint.x == 0 && player.bodyChunks[1].contactPoint.y == -1)
                    {
                        player.animation = Player.AnimationIndex.StandUp;
                        return;
                    }

                    BodyChunk bodyChunk0 = player.bodyChunks[0];
                    BodyChunk bodyChunk1 = player.bodyChunks[1];

                    if (player.input[0].x != 0)
                    {
                        // bodyMode would change when player.input[0].x != bodyChunk0.contactPoint.x // skip this check for now
                        player.canWallJump = player.input[0].x * -15;

                        // when upside down, flip instead of climbing
                        if (bodyChunk0.pos.y < bodyChunk1.pos.y)
                        {
                            bodyChunk0.vel.y = Custom.LerpAndTick(bodyChunk0.vel.y, 2f * player.gravity, 0.8f, 1f);
                            bodyChunk1.vel.y = Custom.LerpAndTick(bodyChunk1.vel.y, 0.0f, 0.8f, 1f);
                            bodyChunk1.vel.x = -player.input[0].x * 5f;
                        }
                        else
                        {
                            float velXGain = 2.4f * Mathf.Lerp(1f, 1.2f, player.Adrenaline) * player.surfaceFriction;
                            if (player.slowMovementStun > 0)
                            {
                                velXGain *= 0.4f + 0.6f * Mathf.InverseLerp(10f, 0.0f, player.slowMovementStun);
                            }

                            if (MainMod.Option_WallClimb && player.input[0].y != 0)
                            {
                                if (player.input[0].y == 1 && !player.IsTileSolid(bChunk: 1, player.input[0].x, 0) && (bodyChunk1.pos.x < bodyChunk0.pos.x) == (player.input[0].x < 0)) // climb up even when lower body part is hanging in the air
                                {
                                    bodyChunk0.pos.y += Math.Abs(bodyChunk0.pos.x - bodyChunk1.pos.x);
                                    bodyChunk1.pos.x = bodyChunk0.pos.x;
                                    bodyChunk1.vel.x = -player.input[0].x * velXGain;
                                }

                                bodyChunk0.vel.y += player.gravity;
                                bodyChunk1.vel.y += player.gravity;

                                // downward momentum when ContactPoint.x != 0 is limited to -player.gravity bc of Player.Update()
                                bodyChunk0.vel.y = Mathf.Lerp(bodyChunk0.vel.y, player.input[0].y * 2.5f, 0.3f);
                                bodyChunk1.vel.y = Mathf.Lerp(bodyChunk1.vel.y, player.input[0].y * 2.5f, 0.3f);
                                ++player.animationFrame;
                            }
                            else if (player.lowerBodyFramesOffGround > 8 && player.input[0].y != -1) // stay in place // don't slide down // when only Option_WallClimb is enabled then this happens even when holding up // don't slide/climb when doing a normal jump off the ground
                            {
                                if (player.grasps[0]?.grabbed is Cicada cicada)
                                {
                                    bodyChunk0.vel.y = Custom.LerpAndTick(bodyChunk0.vel.y, player.gravity - cicada.LiftPlayerPower * 0.5f, 0.3f, 1f);
                                }
                                else
                                {
                                    bodyChunk0.vel.y = Custom.LerpAndTick(bodyChunk0.vel.y, player.gravity, 0.3f, 1f);
                                }
                                bodyChunk1.vel.y = Custom.LerpAndTick(bodyChunk1.vel.y, player.gravity, 0.3f, 1f);

                                if (!player.IsTileSolid(bChunk: 1, player.input[0].x, 0) && player.input[0].x > 0 == bodyChunk1.pos.x > bodyChunk0.pos.x)
                                {
                                    bodyChunk1.vel.x = -player.input[0].x * velXGain;
                                }
                            }
                        }
                    }

                    if (player.slideLoop != null && player.slideLoop.volume > 0.0f)
                    {
                        player.slideLoop.volume = 0.0f;
                    }

                    if (player.animationFrame > 20)
                    {
                        room.PlaySound(SoundID.Slugcat_Crawling_Step, player.mainBodyChunk);
                        player.animationFrame = 0;
                    }

                    bodyChunk1.vel.y += bodyChunk1.submersion * player.EffectiveRoomGravity;
                    return;
                }
                orig(player);

                // backflip // earlier timing possible
                if (MainMod.Option_SlideTurn && player.initSlideCounter > 0 && player.initSlideCounter < 10)
                {
                    player.initSlideCounter = 10;
                }
            }
            else
            {
                orig(player);
            }
        }

        private static void Player_UpdateMSC(On.Player.orig_UpdateMSC orig, Player player)
        {
            orig(player);

            if (!ModManager.MSC) return;
            player.buoyancy = player.gravity;
        }

        // there are cases where this function does not call orig()
        private static void Player_WallJump(On.Player.orig_WallJump orig, Player player, int direction)
        {
            if (player.room is Room room)
            {
                // I think this was to prevent glitching hands when jumping off walls // hand animation is only used for wall climb
                if (MainMod.Option_WallClimb && !player.GetAttachedFields().initializeHands)
                {
                    player.GetAttachedFields().initializeHands = true;
                }

                if (!MainMod.Option_WallJump)
                {
                    orig(player, direction);
                    return;
                }

                // climb on smaller obstacles instead
                if (player.input[0].x != 0 && player.bodyChunks[1].contactPoint.x == player.input[0].x && player.IsTileSolid(0, player.input[0].x, -1) && !player.IsTileSolid(0, player.input[0].x, 0))
                {
                    player.simulateHoldJumpButton = 0;
                    return;
                }

                // jump to be able to climb on smaller obstacles
                if (player.input[0].x != 0 && (player.bodyChunks[0].contactPoint.x == player.input[0].x && player.IsTileSolid(0, player.input[0].x, 0)) && !player.IsTileSolid(0, player.input[0].x, 1))
                {
                    float adrenalineModifier = Mathf.Lerp(1f, 1.15f, player.Adrenaline);
                    if (player.exhausted)
                    {
                        adrenalineModifier *= (float)(1.0 - 0.5 * player.aerobicLevel);
                    }

                    player.bodyChunks[0].vel.y = 4f * adrenalineModifier;
                    player.bodyChunks[1].vel.y = 3.5f * adrenalineModifier;
                    player.bodyChunks[0].pos.y += 10f * Mathf.Min(1f, adrenalineModifier);
                    player.bodyChunks[1].pos.y += 10f * Mathf.Min(1f, adrenalineModifier);

                    player.simulateHoldJumpButton = 0;
                    return;
                }

                IntVector2 bodyChunkTilePosition = room.GetTilePosition(player.bodyChunks[1].pos);
                Room.Tile bodyChunkTile = room.GetTile(bodyChunkTilePosition);
                Room.Tile groundTile = room.GetTile(bodyChunkTilePosition - new IntVector2(0, 1));

                // normal jump off the ground // not exactly the same as in jump // but the same as in vanilla code // only changed conditions
                if (bodyChunkTile.horizontalBeam || groundTile.horizontalBeam || groundTile.Solid || groundTile.Terrain == Room.Tile.TerrainType.Slope || groundTile.Terrain == Room.Tile.TerrainType.Floor || bodyChunkTile.WaterSurface || groundTile.WaterSurface) // ||  player.bodyChunks[1].submersion > 0.1
                {
                    float adrenalineModifier = Mathf.Lerp(1f, 1.15f, player.Adrenaline);
                    if (player.exhausted)
                    {
                        adrenalineModifier *= (float)(1.0 - 0.5 * player.aerobicLevel);
                    }

                    player.bodyChunks[0].vel.y = 8f * adrenalineModifier;
                    player.bodyChunks[1].vel.y = 7f * adrenalineModifier;
                    player.bodyChunks[0].pos.y += 10f * Mathf.Min(1f, adrenalineModifier);
                    player.bodyChunks[1].pos.y += 10f * Mathf.Min(1f, adrenalineModifier);

                    room.PlaySound(SoundID.Slugcat_Normal_Jump, player.mainBodyChunk, false, 1f, 1f);
                    player.jumpBoost = 0.0f;
                    player.simulateHoldJumpButton = 0;
                }
                // don't jump off the wall while climbing // x input direction == wall jump direction
                else if (player.input[0].x == 0 || direction > 0 == player.input[0].x > 0)
                {
                    orig(player, direction);
                    player.simulateHoldJumpButton = 0;
                }
            }
        }

        //
        //
        //

        public sealed class AttachedFields // need reference => reference type
        {
            public bool initializeHands = false;
            public bool isSwitchingBeams = false;

            public int dontUseTubeWormCounter = 0;
            public int getUpOnBeamAbortCounter = 0;
            public int getUpOnBeamDirection = 0;
            public Vector2? grabBeamCooldownPos = null;

            public int grabBeamCounter = 0;
            public int jumpPressedCounter = 0;
            public int soundCooldown = 0;
        }
    }
}