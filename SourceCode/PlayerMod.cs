using System;
using System.Collections.Generic;
using Mono.Cecil.Cil;
using MonoMod.Cil;
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

        public sealed class AttachedFields // need reference => reference type
        {
            public bool initializeHands = false;
            public bool switchingBeam = false;

            public int dontUseTubeWormCounter = 0;
            public int getUpOnBeamAbortCounter = 0;
            public int getUpOnBeamDirection = 0;
            public int grabBeamCooldownCounter = 0;

            public int grabBeamCounter = 0;
            public int jumpPressedCounter = 0;
            public int soundCooldown = 0;

            public AttachedFields()
            {
            }
        }
        internal static readonly Dictionary<Player, AttachedFields> attachedFields = new();
        public static AttachedFields GetAttachedFields(this Player player) => attachedFields[player];

        //
        // OnDisable
        //

        internal static void OnDisable_Option_BeamClimb()
        {
            IL.Player.MovementUpdate -= IL_Player_MovementUpdate;
            IL.Player.UpdateAnimation -= IL_Player_UpdateAnimation;
        }

        internal static void OnDisable_Option_BellySlide()
        {
            On.Player.ThrowObject -= Player_ThrowObject_Option_BellySlide;
        }

        internal static void OnDisable_Option_Crawl()
        {
            On.Player.TerrainImpact -= Player_TerrainImpact;
        }

        internal static void OnDisable_Option_Grab()
        {
            On.Player.Grabability -= Player_Grabability;
        }

        internal static void OnDisable_Option_SpearThrow()
        {
            On.Player.ThrowObject -= Player_ThrowObject_Option_SpearThrow;
        }

        internal static void OnDisable_Option_Swim()
        {
            On.Player.ctor -= Player_ctor_2;
            IL.Player.GrabUpdate -= IL_Player_GrabUpdate;
        }

        internal static void OnDisable_Option_WallJump()
        {
            On.Player.checkInput -= Player_CheckInput;
        }

        //
        // OnEnable
        //

        internal static void OnEnable()
        {
            On.Player.ctor += Player_ctor_1;
            On.Player.Jump += Player_Jump;
            On.Player.UpdateAnimation += Player_UpdateAnimation;
            On.Player.UpdateBodyMode += Player_UpdateBodyMode;
            On.Player.WallJump += Player_WallJump;
        }

        internal static void OnEnable_Option_BeamClimb()
        {
            // removes lifting your booty when being in a corner with your upper bodyChunk / head
            // usually this happens in one tile horizontal holes
            // but this can also happen when climbing beams and bumping your head into a corner
            // in this situation canceling beam climbing can be spammed
            IL.Player.MovementUpdate += IL_Player_MovementUpdate;

            // removes the ability to jump during ClimbUpToBeamTip
            IL.Player.UpdateAnimation += IL_Player_UpdateAnimation;
        }

        internal static void OnEnable_Option_BellySlide()
        {
            On.Player.ThrowObject += Player_ThrowObject_Option_BellySlide; // remove throw timing
        }

        internal static void OnEnable_Option_Crawl()
        {
            On.Player.TerrainImpact += Player_TerrainImpact; // initiate rolls from crawl turns
        }

        internal static void OnEnable_Option_Grab()
        {
            On.Player.Grabability += Player_Grabability; // only grab dead large creatures when crouching
        }

        internal static void OnEnable_Option_SpearThrow()
        {
            On.Player.ThrowObject += Player_ThrowObject_Option_SpearThrow; // momentum adjustment
        }

        internal static void OnEnable_Option_Swim()
        {
            On.Player.ctor += Player_ctor_2; // change stats for swimming
            IL.Player.GrabUpdate += IL_Player_GrabUpdate; // can eat stuff underwater
        }

        internal static void OnEnable_Option_WallJump()
        {
            On.Player.checkInput += Player_CheckInput; // input "buffer" for wall jumping
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

        public static bool IsTileSolidOrSlope(Player? player, int chunkIndex, int relativeX, int relativeY)
        {
            if (player?.room == null)
            {
                return false;
            }

            if (player.IsTileSolid(chunkIndex, relativeX, relativeY))
            {
                return true;
            }
            return player.room.GetTile(player.room.GetTilePosition(player.bodyChunks[chunkIndex].pos) + new IntVector2(relativeX, relativeY)).Terrain == Room.Tile.TerrainType.Slope;
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

        public static void RocketJump(Player? player, float adrenalineModifier, float scale = 1f, SoundID soundID = SoundID.Slugcat_Rocket_Jump)
        {
            if (player == null)
            {
                return;
            }

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
                if (player.input[0].y != 0 && room.GetTile(player.bodyChunks[0].pos).verticalBeam) // grab vertical beam when possible // && room.GetTile(player.bodyChunks[0].pos + new Vector2(0.0f, 20f * player.input[0].y)).verticalBeam
                {
                    if (!attachedFields.switchingBeam)
                    {
                        attachedFields.switchingBeam = true;
                        player.flipDirection = player.bodyChunks[0].pos.x >= room.MiddleOfTile(player.bodyChunks[0].pos).x ? 1 : -1;
                        player.animation = Player.AnimationIndex.ClimbOnBeam;
                        return true;
                    }
                }
                else if (attachedFields.switchingBeam)
                {
                    attachedFields.switchingBeam = false;
                }
            }
            return false;
        }

        public static void UpdateAnimationCounter(Player? player)
        {
            if (player == null)
            {
                return;
            }

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

            if (player.bodyChunks[0].ContactPoint.y < 0)
            {
                ++player.upperBodyFramesOnGround;
                player.upperBodyFramesOffGround = 0;
            }
            else
            {
                player.upperBodyFramesOnGround = 0;
                ++player.upperBodyFramesOffGround;
            }

            if (player.bodyChunks[1].ContactPoint.y < 0)
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

        // first IL-Hook yaaaay
        private static void IL_Player_GrabUpdate(ILContext context)
        {
            ILCursor cursor = new(context);
            if (cursor.TryGotoNext(MoveType.Before,
                                     instruction => instruction.MatchLdarg(0), // argument0: player
                                     instruction => instruction.MatchCall<Creature>("get_mainBodyChunk"),
                                     instruction => instruction.MatchCallvirt<BodyChunk>("get_submersion")))
            {
                // remove: player.mainBodyChunk.submersion < 0.5f // but leave goto label intakt
                cursor.GotoPrev();
                cursor.RemoveRange(6);

                // not sure why this is what you do // I just changed the code and looked at the generated IL instructions
                // NOTE: does not work // the changed code gets optimized and has a variable less
                // make as few changes as possible // less optimized but works

                // invert input[0].thrw // this is skipped in the original function
                // now this is saved locally instead of player.mainBodyChunk.submersion < 0.5f 
                cursor.Emit(OpCodes.Ldc_I4_0);
                cursor.Emit(OpCodes.Ceq);
            }
            else
            {
                Debug.LogException(new Exception("SimplifiedMoveset: IL_Player_GrabUpdate failed."));
            }
        }

        private static void IL_Player_MovementUpdate(ILContext context)
        {
            ILCursor cursor = new(context);
            if (cursor.TryGotoNext(
                instruction => instruction.MatchLdarg(0),
                instruction => instruction.MatchCallvirt("Player", "get_input"),
                instruction => instruction.MatchLdcI4(0),
                instruction => instruction.MatchLdelema("Player/InputPackage"),
                instruction => instruction.MatchLdfld("Player/InputPackage", "y"),
                instruction => instruction.MatchLdcI4(1)))
            {
                int index = cursor.Index;
                if (cursor.TryGotoNext(instruction => instruction.MatchBneUn(out _))) // out label is not accessible => discard
                {
                    ILLabel label = (ILLabel)cursor.Next.Operand;
                    cursor.Goto(index, MoveType.AfterLabel); // incoming labels will point to the emmited instruction // cursor.MoveAfterLabels() is called
                    cursor.Emit(OpCodes.Br, label);
                }
            }
            else
            {
                Debug.LogException(new Exception("SimplifiedMoveset: IL_Player_MovementUpdate failed."));
            }
            // MainMod.LogAllInstructions(context);
        }

        private static void IL_Player_UpdateAnimation(ILContext context)
        {
            ILCursor cursor = new(context);

            // case Player.AnimationIndex.StandOnBeam:
            cursor.TryGotoNext(
                instruction => instruction.MatchLdarg(0),
                instruction => instruction.MatchLdcI4(6),
                instruction => instruction.MatchStfld("Player", "bodyMode"),
                instruction => instruction.MatchLdarg(0),
                instruction => instruction.MatchLdcI4(1),
                instruction => instruction.MatchStfld("Player", "standing"),
                instruction => instruction.MatchLdarg(0),
                instruction => instruction.MatchLdcI4(5),
                instruction => instruction.MatchStfld("Player", "canJump"));

            //case Player.AnimationIndex.GetUpToBeamTip:
            if (cursor.TryGotoNext(MoveType.After,
                instruction => instruction.MatchLdarg(0),
                instruction => instruction.MatchLdcI4(6),
                instruction => instruction.MatchStfld("Player", "bodyMode"),
                instruction => instruction.MatchLdarg(0),
                instruction => instruction.MatchLdcI4(1),
                instruction => instruction.MatchStfld("Player", "standing"),
                instruction => instruction.MatchLdarg(0),
                instruction => instruction.MatchLdcI4(5),
                instruction => instruction.MatchStfld("Player", "canJump")))
            {
                // it seems that GotoPrev() and GotoNext() are messed up
                // Debug.Log(cursor.Index); // 2381

                // player.canJump = 0
                // cursor.GotoPrev(); // 2379 // why???
                // cursor.Next.OpCode = OpCodes.Ldc_I4_0; // why does this work?? // should GotoPrev() and Next not cancel out??

                // player.canJump = 0
                cursor.Goto(cursor.Index - 1); // 2380
                cursor.Prev.OpCode = OpCodes.Ldc_I4_0;
            }
            else
            {
                Debug.LogException(new Exception("SimplifiedMoveset: IL_Player_UpdateAnimation failed."));
            }
            // MainMod.LogAllInstructions(context);
        }

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

        private static void Player_ctor_1(On.Player.orig_ctor orig, Player player, AbstractCreature abstractCreature, World world)
        {
            orig(player, abstractCreature, world);
            PlayerMod.attachedFields.Add(player, new PlayerMod.AttachedFields());
        }

        private static void Player_ctor_2(On.Player.orig_ctor orig, Player player, AbstractCreature abstractCreature, World world)
        {
            orig(player, abstractCreature, world);
            player.slugcatStats.lungsFac = 0.0f;
            player.buoyancy = 0.9f;
        }

        private static int Player_Grabability(On.Player.orig_Grabability orig, Player player, PhysicalObject physicalObject)
        {
            if (player.standing && physicalObject is Creature creature && !creature.Template.smallCreature && creature.dead)
            {
                return (int)Player.ObjectGrabability.CantGrab;
            }
            return orig(player, physicalObject);
        }

        // there are cases where this function does not call orig()
        private static void Player_Jump(On.Player.orig_Jump orig, Player player)
        {
            if (!MainMod.Option_Roll_2 && player.animation == Player.AnimationIndex.Roll)
            {
                player.animation = Player.AnimationIndex.None;
                player.standing = true;
                player.rollCounter = 0;
                player.rollDirection = 0;

                orig(player);
                return;
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

                    bool superLaunchJump = player.superLaunchJump >= 20;
                    orig(player);

                    // crouch jump // stand up during jump
                    if (MainMod.Option_CrouchJump && player.animation != Player.AnimationIndex.BellySlide && player.animation != Player.AnimationIndex.AntlerClimb && player.animation != Player.AnimationIndex.ZeroGSwim && player.animation != Player.AnimationIndex.ZeroGPoleGrab && player.animation != Player.AnimationIndex.LedgeGrab && player.animation != Player.AnimationIndex.ClimbOnBeam && (player.animation != Player.AnimationIndex.DownOnFours || player.bodyChunks[1].ContactPoint.y >= 0 || player.input[0].downDiagonal != player.flipDirection) && !player.standing && !superLaunchJump)
                    {
                        player.standing = true;
                    }
                }
            }
            else
            {
                orig(player);
            }
        }

        private static void Player_TerrainImpact(On.Player.orig_TerrainImpact orig, Player player, int chunk, IntVector2 direction, float speed, bool firstContact)
        {
            orig(player, chunk, direction, speed, firstContact);

            // roll initiate logic // has less requirements than roll initiate logic from orig() // allow roll from crawl turn
            if (MainMod.Option_Crawl && player.input[0].downDiagonal != 0 && direction.y < 0 && player.animation != Player.AnimationIndex.Roll && ((speed > 12.0 || player.animation == Player.AnimationIndex.Flip) && player.allowRoll > 0 || player.animation == Player.AnimationIndex.CrawlTurn))
            {
                player.room.PlaySound(SoundID.Slugcat_Roll_Init, player.mainBodyChunk.pos, 1f, 1f);
                player.animation = Player.AnimationIndex.Roll;
                player.rollDirection = player.input[0].downDiagonal;
                player.rollCounter = 0;

                player.bodyChunks[0].vel.x = Mathf.Lerp(player.bodyChunks[0].vel.x, 9f * player.input[0].x, 0.7f);
                player.bodyChunks[1].vel.x = Mathf.Lerp(player.bodyChunks[1].vel.x, 9f * player.input[0].x, 0.7f);
                player.standing = false;
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
            AttachedFields attachedFields = player.GetAttachedFields();
            Room? room = player.room;

            if (attachedFields.getUpOnBeamAbortCounter > 0) // beam climb
            {
                --attachedFields.getUpOnBeamAbortCounter;
            }

            if (attachedFields.grabBeamCounter > 0)
            {
                --attachedFields.grabBeamCounter;
            }

            if (attachedFields.grabBeamCooldownCounter > 0)
            {
                --attachedFields.grabBeamCooldownCounter;
            }

            if (attachedFields.soundCooldown > 0)
            {
                --attachedFields.soundCooldown;
            }

            if (attachedFields.jumpPressedCounter > 0) // ledge grab
            {
                --attachedFields.jumpPressedCounter;
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
            if (MainMod.Option_BellySlide && player.animation == Player.AnimationIndex.BellySlide && room != null)
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
                if (player.input[0].y < 0 && player.input[0].downDiagonal == 0 && player.input[0].x == 0 && player.rollCounter > 10 && room.GetTilePosition(player.bodyChunks[0].pos).y == room.GetTilePosition(player.bodyChunks[1].pos).y)
                {
                    IntVector2 tilePosition = room.GetTilePosition(player.mainBodyChunk.pos);
                    if (!room.GetTile(tilePosition + new IntVector2(0, -1)).Solid && room.GetTile(tilePosition + new IntVector2(-1, -1)).Solid && room.GetTile(tilePosition + new IntVector2(1, -1)).Solid)
                    {
                        player.bodyChunks[0].pos = room.MiddleOfTile(player.bodyChunks[0].pos) + new Vector2(0.0f, -20f);
                        player.bodyChunks[1].pos = Vector2.Lerp(player.bodyChunks[1].pos, player.bodyChunks[0].pos + new Vector2(0.0f, player.bodyChunkConnections[0].distance), 0.5f);
                        player.bodyChunks[0].vel = new Vector2(0.0f, -11f);
                        player.bodyChunks[1].vel = new Vector2(0.0f, -11f);

                        player.animation = Player.AnimationIndex.None;
                        player.GoThroughFloors = true;
                        player.rollDirection = 0;
                        return;
                    }
                }
                player.whiplashJump = player.input[0].x == -player.rollDirection;

                if (player.rollCounter < 6)
                {
                    player.bodyChunks[1].vel.x -= 9.1f * player.rollDirection;
                    player.bodyChunks[1].vel.y += 2f; // default: 2.7f
                }
                else if (IsTileSolidOrSlope(player, chunkIndex: 1, 0, -1) || IsTileSolidOrSlope(player, chunkIndex: 1, 0, -2))
                {
                    player.bodyChunks[1].vel.y -= 3f; // stick better to slopes // default: -0.5f
                }

                if (IsTileSolidOrSlope(player, chunkIndex: 0, 0, -1) || IsTileSolidOrSlope(player, chunkIndex: 0, 0, -2))
                {
                    player.bodyChunks[0].vel.y -= 3f; // default: -2.3f
                }

                player.bodyChunks[0].vel.x += (!player.longBellySlide ? 16.7f : 14f) * player.rollDirection * Mathf.Sin((float)(player.rollCounter / (!player.longBellySlide ? 20.0 : 39.0) * Math.PI));
                foreach (BodyChunk bodyChunk in player.bodyChunks)
                {
                    if (bodyChunk.ContactPoint.y == 0)
                    {
                        bodyChunk.vel.x *= player.surfaceFriction;
                    }
                }

                // finish // abort when mid-air // don't cancel belly slides on slopes
                if (player.rollCounter > (!player.longBellySlide ? 20 : 39) || player.canJump == 0 && !IsTileSolidOrSlope(player, chunkIndex: 0, 0, -1) && !IsTileSolidOrSlope(player, chunkIndex: 1, 0, -1))
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
            if (MainMod.Option_Swim && player.animation == Player.AnimationIndex.DeepSwim)
            {
                UpdateAnimationCounter(player);
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

                player.dynamicRunSpeed[0] = 0.0f;
                player.dynamicRunSpeed[1] = 0.0f;

                if (player.grasps[0] != null && player.grasps[0].grabbed is JetFish jetFish && jetFish.Consious)
                {
                    player.waterFriction = 1f;
                    return;
                }

                player.canJump = 0;
                player.standing = false;
                player.GoThroughFloors = true;

                float num1 = (float)((Mathf.Abs(Vector2.Dot(player.bodyChunks[0].vel.normalized, (player.bodyChunks[0].pos - player.bodyChunks[1].pos).normalized)) + (double)Mathf.Abs(Vector2.Dot(player.bodyChunks[1].vel.normalized, (player.bodyChunks[0].pos - player.bodyChunks[1].pos).normalized))) / 2.0);
                player.swimCycle += 0.01f;
                if (player.input[0].ZeroGGamePadIntVec.x != 0 || player.input[0].ZeroGGamePadIntVec.y != 0)
                {
                    float num2 = (float)(0.2 + Mathf.InverseLerp(0.0f, 12f, Vector2.Angle(player.bodyChunks[0].lastPos - player.bodyChunks[1].lastPos, player.bodyChunks[0].pos - player.bodyChunks[1].pos)) * 0.8);
                    if (player.slowMovementStun > 0)
                        num2 *= 0.5f;

                    float to = num2 * Mathf.Lerp(1f, 1.2f, player.Adrenaline);
                    player.swimForce = (double)to <= player.swimForce ? Mathf.Lerp(player.swimForce, to, 0.05f) : Mathf.Lerp(player.swimForce, to, 0.7f);
                    player.swimCycle += Mathf.Lerp(player.swimForce, 1f, 0.5f) / 10f;

                    if (player.airInLungs < 0.5 && (double)player.airInLungs > 1 / 6)
                        player.swimCycle += 0.05f;
                    if (player.bodyChunks[0].ContactPoint.x != 0 || player.bodyChunks[0].ContactPoint.y != 0)
                        player.swimForce *= 0.5f;

                    if (player.swimCycle > 4.0)
                        player.swimCycle = 0.0f;
                    else if (player.swimCycle > 3.0)
                        player.bodyChunks[0].vel += Custom.DirVec(player.bodyChunks[1].pos, player.bodyChunks[0].pos) * 0.7f * Mathf.Lerp(player.swimForce, 1f, 0.5f) * player.bodyChunks[0].submersion;

                    Vector2 vector2 = player.SwimDir(true);
                    if (player.airInLungs < 0.3)
                        vector2 = Vector3.Slerp(vector2, new Vector2(0.0f, 1f), Mathf.InverseLerp(0.3f, 0.0f, player.airInLungs));

                    player.bodyChunks[0].vel += vector2 * 0.5f * player.swimForce * Mathf.Lerp(num1, 1f, 0.5f) * player.bodyChunks[0].submersion;
                    player.bodyChunks[1].vel -= vector2 * 0.1f * player.bodyChunks[0].submersion;
                    player.bodyChunks[0].vel += Custom.DirVec(player.bodyChunks[1].pos, player.bodyChunks[0].pos) * 0.4f * player.swimForce * num1 * player.bodyChunks[0].submersion;

                    if (player.bodyChunks[0].vel.magnitude < 6.0)
                    {
                        player.bodyChunks[0].vel += vector2 * 0.2f * Mathf.InverseLerp(6f, 1.5f, player.bodyChunks[0].vel.magnitude);
                        player.bodyChunks[1].vel -= vector2 * 0.1f * Mathf.InverseLerp(6f, 1.5f, player.bodyChunks[0].vel.magnitude);
                    }
                }

                player.waterFriction = Mathf.Lerp(0.92f, 0.96f, num1);
                if (player.bodyMode == Player.BodyModeIndex.Swimming)
                    return;
                player.animation = Player.AnimationIndex.None;
                return;
            }

            // ledge grab 
            if (player.animation == Player.AnimationIndex.LedgeGrab)
            {
                // enabled always // TODO: consider changing ledge grab option to this only
                if (player.canWallJump == 0 || Math.Sign(player.canWallJump) == -Math.Sign(player.flipDirection))
                {
                    player.canWallJump = player.flipDirection * -15; // you can do a (mid-air) wall jump off a ledge grab
                }

                if (MainMod.Option_LedgeGrab && room != null)
                {
                    if (player.input[0].jmp && attachedFields.jumpPressedCounter < 20)
                    {
                        attachedFields.jumpPressedCounter = 20;
                    }

                    // holds the ledge grab animation until jump is pressed
                    if (attachedFields.jumpPressedCounter == 0 && player.IsTileSolid(0, player.flipDirection, 0) && !player.IsTileSolid(0, player.flipDirection, 1) && room.GetTile(player.abstractCreature.pos.Tile + new IntVector2(player.flipDirection * 2, 0)).Terrain != Room.Tile.TerrainType.ShortcutEntrance && room.GetTile(player.abstractCreature.pos.Tile + new IntVector2(player.flipDirection * 2, 1)).Terrain != Room.Tile.TerrainType.ShortcutEntrance) // dont stay in ledge grab when at a shortcut ledge
                    {
                        player.ledgeGrabCounter = 0;
                        player.bodyChunks[0].pos -= new Vector2(0.0f, 4f);
                        player.bodyChunks[1].vel.x -= player.flipDirection;

                        if (player.input[0].x == player.flipDirection || player.input[0].y == 1)
                        {
                            player.bodyChunks[1].vel -= new Vector2(-0.5f * player.flipDirection, -0.5f);
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
            if (MainMod.Option_BeamClimb && room != null)
            {
                // velocity gained in x direction each frame (if no slowmovementstun, and bodyMode is default)
                float velXGain = 2.4f * Mathf.Lerp(1f, 1.2f, player.Adrenaline) * player.surfaceFriction * room.gravity;
                if (player.slowMovementStun > 0)
                {
                    velXGain *= 0.4f + 0.6f * Mathf.InverseLerp(10f, 0.0f, player.slowMovementStun);
                }

                // grab beams by holding down // extends some cases when holding up -- forget which ones :/ // don't grab beams while falling inside corridors
                if (attachedFields.grabBeamCooldownCounter == 0 && (player.input[0].y != 0 || attachedFields.grabBeamCounter > 0) && player.animation == Player.AnimationIndex.None && player.bodyMode == Player.BodyModeIndex.Default && (!player.IsTileSolid(bChunk: 0, -1, 0) || !player.IsTileSolid(bChunk: 0, 1, 0)) && (!player.IsTileSolid(bChunk: 1, -1, 0) || !player.IsTileSolid(bChunk: 1, 1, 0)))
                {
                    if (room.GetTile(player.bodyChunks[0].pos).verticalBeam)
                    {
                        if (attachedFields.soundCooldown == 0)
                        {
                            attachedFields.soundCooldown = 40;
                            room.PlaySound(SoundID.Slugcat_Grab_Beam, player.mainBodyChunk, false, 1f, 1f);
                        }

                        float middleOfTileX = room.MiddleOfTile(player.bodyChunks[0].pos).x;
                        player.flipDirection = Mathf.Abs(player.bodyChunks[0].vel.x) <= 5f ? (player.bodyChunks[0].pos.x >= middleOfTileX ? 1 : -1) : (player.bodyChunks[0].vel.x >= 0.0f ? 1 : -1);

                        player.bodyChunks[0].vel = new Vector2(0.0f, 0.0f);
                        player.bodyChunks[0].pos.x = middleOfTileX;
                        player.bodyChunks[1].vel.y = 0.0f;
                        player.animation = Player.AnimationIndex.ClimbOnBeam;
                    }
                    else
                    {
                        int x = room.GetTilePosition(player.bodyChunks[0].pos).x;
                        for (int y = room.GetTilePosition(player.bodyChunks[0].lastPos).y; y >= room.GetTilePosition(player.bodyChunks[0].pos).y; --y)
                        {
                            if (room.GetTile(x, y).horizontalBeam)
                            {
                                attachedFields.grabBeamCooldownCounter = 8;
                                if (attachedFields.soundCooldown == 0)
                                {
                                    attachedFields.soundCooldown = 40;
                                    room.PlaySound(SoundID.Slugcat_Grab_Beam, player.mainBodyChunk, false, 1f, 1f);
                                }

                                player.bodyChunks[0].pos.y = room.MiddleOfTile(new IntVector2(x, y)).y;
                                player.bodyChunks[1].vel.y = 0.0f;
                                player.animation = Player.AnimationIndex.HangFromBeam;
                                break;
                            }
                        }
                    }
                }

                // ClimbOnBeam // grab horizontal beam when holding button
                if (player.animation == Player.AnimationIndex.ClimbOnBeam && player.input[0].x != 0 && player.input[0].x == player.flipDirection && player.input[0].x == player.lastFlipDirection)
                {
                    if (room.GetTile(player.bodyChunks[0].pos).horizontalBeam && !player.IsTileSolid(bChunk: 0, 0, -1) && !player.IsTileSolid(bChunk: 0, player.input[0].x, 0)) // && room.GetTile(player.bodyChunks[0].pos + new Vector2(20f * player.input[0].x, 0.0f)).horizontalBeam // !player.IsTileSolid(bChunk: 0, player.input[0].x, 0) is for consistency with vanilla behavior (which is not that consistent itself -- oh well)
                    {
                        if (!attachedFields.switchingBeam)
                        {
                            attachedFields.switchingBeam = true;
                            player.animation = Player.AnimationIndex.HangFromBeam;
                        }
                    }
                    else if (room.GetTile(player.bodyChunks[1].pos).horizontalBeam && !player.IsTileSolid(bChunk: 1, player.input[0].x, 0)) //  && room.GetTile(player.bodyChunks[1].pos + new Vector2(20f * player.input[0].x, 0.0f)).horizontalBeam
                    {
                        if (!attachedFields.switchingBeam)
                        {
                            attachedFields.switchingBeam = true;
                            player.animation = Player.AnimationIndex.StandOnBeam;
                        }
                    }
                    else if (attachedFields.switchingBeam)
                    {
                        attachedFields.switchingBeam = false;
                    }
                }

                // HangUnderVerticalBeam
                if (player.animation == Player.AnimationIndex.HangUnderVerticalBeam)
                {
                    UpdateAnimationCounter(player);
                    player.bodyMode = Player.BodyModeIndex.ClimbingOnBeam; // gets updated and is default afterwards
                    player.standing = false;

                    // drop when pressing jump
                    if (player.input[0].jmp && !player.input[1].jmp || player.bodyChunks[1].vel.magnitude > 10.0 || player.bodyChunks[0].vel.magnitude > 10.0 || !room.GetTile(player.bodyChunks[0].pos + new Vector2(0.0f, 20f)).verticalBeam)
                    {
                        attachedFields.dontUseTubeWormCounter = 2;
                        player.animation = Player.AnimationIndex.None;
                        player.standing = true;
                    }
                    else
                    {
                        player.bodyChunks[0].pos.x = Mathf.Lerp(player.bodyChunks[0].pos.x, room.MiddleOfTile(player.bodyChunks[0].pos).x, 0.5f);
                        player.bodyChunks[0].pos.y = Mathf.Max(player.bodyChunks[0].pos.y, room.MiddleOfTile(player.bodyChunks[0].pos).y + 5f + player.bodyChunks[0].vel.y);

                        player.bodyChunks[0].vel.x *= 0.5f; // dont kill all momentum
                        player.bodyChunks[0].vel.x -= player.input[0].x * (velXGain + leanFactor);
                        player.bodyChunks[0].vel.y *= 0.5f;
                        player.bodyChunks[1].vel.x -= player.input[0].x * (velXGain - leanFactor);

                        if (player.input[0].y > 0)
                        {
                            player.bodyChunks[0].vel.y += 2.5f;
                        }

                        if (room.GetTile(player.bodyChunks[0].pos).verticalBeam)
                        {
                            player.animation = Player.AnimationIndex.ClimbOnBeam;
                        }
                    }
                    return;
                }

                // HangFromBeam
                if (player.animation == Player.AnimationIndex.HangFromBeam)
                {
                    UpdateAnimationCounter(player);
                    player.bodyMode = Player.BodyModeIndex.ClimbingOnBeam;
                    player.standing = true;

                    player.bodyChunks[0].vel.x *= 0.2f;
                    player.bodyChunks[0].vel.y = 0.0f;
                    player.bodyChunks[0].pos.y = room.MiddleOfTile(player.bodyChunks[0].pos).y;

                    if (player.input[0].x != 0 && player.bodyChunks[0].ContactPoint.x != player.input[0].x)
                    {
                        Room.Tile tile = room.GetTile(player.bodyChunks[0].pos + new Vector2(12f * player.input[0].x, 0.0f));
                        if (tile.horizontalBeam)
                        {
                            if (player.bodyChunks[1].ContactPoint.x != player.input[0].x)
                            {
                                player.bodyChunks[0].vel.x += player.input[0].x * Mathf.Lerp(1.2f, 1.4f, player.Adrenaline) * player.slugcatStats.poleClimbSpeedFac * Custom.LerpMap(player.slowMovementStun, 0.0f, 10f, 1f, 0.5f);
                            }

                            player.bodyChunks[1].vel.x += player.flipDirection * (0.5f + 0.5f * Mathf.Sin((float)(player.animationFrame / 20.0 * Math.PI * 2.0))) * -0.5f;
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
                            player.bodyChunks[0].pos.x = room.MiddleOfTile(player.bodyChunks[0].pos).x;
                            player.bodyChunks[0].vel.x -= leanFactor * player.input[0].x;
                            player.bodyChunks[1].vel.x += leanFactor * player.input[0].x;
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

                    if (SwitchHorizontalToVerticalBeam(player, attachedFields)) // grab vertical beam if possible
                    {
                        return;
                    }

                    if (player.input[0].jmp && !player.input[1].jmp)
                    {
                        if (player.tubeWorm?.tongues[0].Attached == true) // retract tubeWorm first // consistent behavior with when standing on beam and pressing jump
                        {
                            return;
                        }

                        if (player.input[0].y == 1) // only drop when pressing jump without holding up
                        {
                            PrepareGetUpOnBeamAnimation(player, 1, attachedFields);
                            return;
                        }

                        attachedFields.dontUseTubeWormCounter = 2; // don't drop and shoot tubeWorm at the same time
                        attachedFields.grabBeamCooldownCounter = 8;
                        player.animation = Player.AnimationIndex.None;
                        return;
                    }
                    else if (player.input[0].y == 1 && player.input[1].y == 0)
                    {
                        PrepareGetUpOnBeamAnimation(player, 1, attachedFields);
                        return;
                    }

                    if (!room.GetTile(player.bodyChunks[0].pos).horizontalBeam)
                    {
                        player.animation = Player.AnimationIndex.None;
                    }
                    return;
                }

                // GetUpOnBeam and GetDownOnBeam
                if (player.animation == Player.AnimationIndex.GetUpOnBeam)
                {
                    int direction = attachedFields.getUpOnBeamDirection; // -1 (down) or 1 (up)
                    int bodyChunkIndex = direction == 1 ? 1 : 0;

                    player.canJump = 0; // otherwise: bugged when pressing jump during this animation => drops slugcat when StandOnBeam animation is reached
                    player.bodyMode = Player.BodyModeIndex.ClimbingOnBeam;
                    player.bodyChunks[0].vel.x = 0.0f;
                    player.bodyChunks[0].vel.y = 0.0f;

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

                    player.bodyChunks[0].vel += Custom.DirVec(player.bodyChunks[0].pos, player.upOnHorizontalBeamPos) * 1.8f;
                    player.bodyChunks[1].vel += Custom.DirVec(player.bodyChunks[1].pos, player.upOnHorizontalBeamPos + new Vector2(0.0f, -20f)) * 1.8f;

                    // ----- //
                    // exits //
                    // ----- //

                    if (room.GetTile(player.bodyChunks[bodyChunkIndex].pos).horizontalBeam && Math.Abs(player.bodyChunks[bodyChunkIndex].pos.y - player.upOnHorizontalBeamPos.y) < 25.0)
                    {
                        player.noGrabCounter = 15;
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
                    else if (player.bodyChunks[0].ContactPoint.y == direction || player.bodyChunks[1].ContactPoint.y == direction)
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

                    if ((room.GetTile(player.bodyChunks[0].pos).horizontalBeam || room.GetTile(player.bodyChunks[1].pos).horizontalBeam) && Custom.DistLess(player.bodyChunks[1 - bodyChunkIndex].pos, player.upOnHorizontalBeamPos, 30f)) // default: 25f
                    {
                        return;
                    }

                    player.animation = Player.AnimationIndex.None;
                    return;
                }

                // StandOnBeam
                if (player.animation == Player.AnimationIndex.StandOnBeam)
                {
                    bool isWallClimbing = player.bodyMode == Player.BodyModeIndex.WallClimb;

                    UpdateAnimationCounter(player);
                    player.bodyMode = Player.BodyModeIndex.ClimbingOnBeam;
                    player.standing = true;
                    player.canJump = 5;
                    player.bodyChunks[1].vel.x *= 0.5f;

                    if (player.input[0].x != 0 && player.bodyChunks[1].ContactPoint.x != player.input[0].x)
                    {
                        Room.Tile tile = room.GetTile(player.bodyChunks[1].pos + new Vector2(12f * player.input[0].x, 0.0f));
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
                                    player.bodyChunks[0].vel.x += player.input[0].x * velXGain; // player.dynamicRunSpeed[0];
                                    player.bodyChunks[1].vel.x += player.input[0].x * velXGain;
                                }
                                else
                                {
                                    player.bodyChunks[1].pos.x = room.MiddleOfTile(player.bodyChunks[1].pos).x;
                                    player.bodyChunks[0].vel.x -= player.input[0].x * (velXGain - leanFactor);
                                    player.bodyChunks[1].vel.x -= player.input[0].x * (velXGain + leanFactor);
                                }
                            }
                            else // stop at the end of horizontal beam
                            {
                                player.bodyChunks[1].pos.x = room.MiddleOfTile(player.bodyChunks[1].pos).x;
                                player.bodyChunks[0].vel.x -= player.input[0].x * velXGain;
                                player.bodyChunks[1].vel.x -= player.input[0].x * velXGain;
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
                    if (SwitchHorizontalToVerticalBeam(player, attachedFields))
                    {
                        return;
                    }

                    if (isWallClimbing)
                    {
                        player.animation = Player.AnimationIndex.None;
                        return;
                    }

                    if (player.bodyChunks[0].ContactPoint.y < 1 || !player.IsTileSolid(bChunk: 1, 0, 1))
                    {
                        player.bodyChunks[1].vel.y = 0.0f;
                        player.bodyChunks[1].pos.y = room.MiddleOfTile(player.bodyChunks[1].pos).y + 5f;
                        player.bodyChunks[0].vel.y += 2f;

                        player.dynamicRunSpeed[0] = 2.1f * player.slugcatStats.runspeedFac;
                        player.dynamicRunSpeed[1] = 2.1f * player.slugcatStats.runspeedFac;
                    }
                    else
                    {
                        // stop moving forward when bumping your "head" into something
                        player.bodyChunks[0].vel.x -= player.input[0].x * velXGain;
                        player.bodyChunks[1].vel.x -= player.input[0].x * velXGain;
                    }

                    // move down to HangFromBeam
                    if (player.input[0].y == -1 && (player.input[1].y == 0 || player.input[0].jmp && !player.input[1].jmp))
                    {
                        PrepareGetUpOnBeamAnimation(player, -1, attachedFields);
                        return;
                    }

                    // grab nearby horizontal beams
                    if (player.input[0].y == 1 && player.input[1].y == 0 && room.GetTile(room.GetTilePosition(player.bodyChunks[0].pos) + new IntVector2(0, 1)).horizontalBeam)
                    {
                        player.bodyChunks[0].pos.y += 8f;
                        player.bodyChunks[1].pos.y += 8f;
                        player.animation = Player.AnimationIndex.HangFromBeam;
                    }
                    return;
                }

                // GetUpToBeamTip // don't let go of beam while climbing to the top // don't prevent player from entering corridors
                if (player.animation == Player.AnimationIndex.GetUpToBeamTip) // player.bodyChunks[0].contactPoint.x == 0 && player.bodyChunks[1].contactPoint.x == 0
                {
                    foreach (BodyChunk bodyChunk in player.bodyChunks)
                    {
                        Room.Tile tile = room.GetTile(bodyChunk.pos);
                        if (!tile.verticalBeam && room.GetTile(tile.X, tile.Y - 1).verticalBeam)
                        {
                            float middleOfTileX = room.MiddleOfTile(tile.X, tile.Y).x;
                            BodyChunk bodyChunk0 = player.bodyChunks[0];
                            BodyChunk bodyChunk1 = player.bodyChunks[1];

                            // give a bit of protection against wind and horizontal momentum
                            bodyChunk0.pos.x += Mathf.Clamp(middleOfTileX - bodyChunk0.pos.x, -2f * velXGain, 2f * velXGain);
                            bodyChunk1.pos.x += Mathf.Clamp(middleOfTileX - bodyChunk1.pos.x, -2f * velXGain, 2f * velXGain);

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

                // BeamTip // don't drop off beam tip by leaning too much
                if (player.animation == Player.AnimationIndex.BeamTip)
                {
                    UpdateAnimationCounter(player);
                    player.bodyMode = Player.BodyModeIndex.ClimbingOnBeam;
                    player.standing = true;
                    player.canJump = 5;

                    player.bodyChunks[1].pos = (player.bodyChunks[1].pos + room.MiddleOfTile(player.bodyChunks[1].pos)) / 2f;
                    player.bodyChunks[1].vel *= 0.5f;

                    if (player.input[0].jmp && !player.input[1].jmp)
                    {
                        player.bodyChunks[0].vel.x += player.input[0].x * velXGain;
                        player.bodyChunks[1].vel.x += player.input[0].x * velXGain;
                    }
                    else if (player.input[0].x == 0 && player.input[0].y == -1)
                    {
                        // wind can make lining yourself up more difficult // on the other hand, wind makes catching the beam below also harder => leave it as is
                        player.bodyChunks[0].pos.x += Mathf.Clamp(player.bodyChunks[1].pos.x - player.bodyChunks[0].pos.x, -velXGain, velXGain);
                    }
                    else
                    {
                        player.bodyChunks[0].vel.x -= player.input[0].x * (velXGain - leanFactor);
                        player.bodyChunks[1].vel.x -= player.input[0].x * (velXGain + leanFactor);
                    }

                    player.bodyChunks[0].vel.y += 1.5f;
                    player.bodyChunks[0].vel.y += player.input[0].y * 0.1f;

                    // what does this do?
                    if (player.input[0].y > 0 && player.input[1].y == 0)
                    {
                        --player.bodyChunks[1].vel.y;
                        player.canJump = 0;
                        player.animation = Player.AnimationIndex.None;
                    }

                    if (player.input[0].y == -1 && (player.bodyChunks[0].pos.x == player.bodyChunks[1].pos.x || player.input[0].jmp && !player.input[1].jmp)) // IsPosXAligned(player)
                    {
                        attachedFields.grabBeamCounter = 15;
                        attachedFields.dontUseTubeWormCounter = 2;
                        player.canJump = 0;
                        player.animation = Player.AnimationIndex.None;
                    }
                    else if (player.bodyChunks[0].pos.y < player.bodyChunks[1].pos.y - 5f || !room.GetTile(player.bodyChunks[1].pos + new Vector2(0.0f, -20f)).verticalBeam)
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
                if (player.animation == Player.AnimationIndex.CrawlTurn && player.input[0].x > 0 == player.bodyChunks[0].pos.x >= (double)player.bodyChunks[1].pos.x && player.bodyChunks[0].contactPoint.y == -1)
                {
                    player.animation = Player.AnimationIndex.None;
                }

                // finish ledge crawl when on slopes
                if (player.animation == Player.AnimationIndex.LedgeCrawl && player.bodyChunks[0].onSlope != 0 && player.bodyChunks[1].onSlope != 0)
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
                    else if ((player.bodyChunks[1].onSlope == 0 || player.input[0].x != -player.bodyChunks[1].onSlope) && (player.lowerBodyFramesOnGround >= 3 || player.bodyChunks[1].ContactPoint.y < 0 && room.GetTile(room.GetTilePosition(player.bodyChunks[1].pos) + new IntVector2(0, -1)).Terrain != Room.Tile.TerrainType.Air && room.GetTile(room.GetTilePosition(player.bodyChunks[0].pos) + new IntVector2(0, -1)).Terrain != Room.Tile.TerrainType.Air))
                    {
                        AlignPosYOnSlopes(player);
                        room.PlaySound(SoundID.Slugcat_Stand_Up, player.mainBodyChunk);
                        player.animation = Player.AnimationIndex.StandUp;

                        if (player.input[0].x == 0)
                        {
                            if (player.bodyChunks[1].ContactPoint.y == -1 && player.IsTileSolid(1, 0, -1) && !player.IsTileSolid(1, 0, 1))
                            {
                                player.feetStuckPos = new Vector2?(room.MiddleOfTile(room.GetTilePosition(player.bodyChunks[1].pos)) + new Vector2(0.0f, player.bodyChunks[1].rad - 10f));
                            }
                            else if (player.bodyChunks[0].ContactPoint.y == -1 && player.IsTileSolid(0, 0, -1) && !player.IsTileSolid(0, 0, 1))
                            {
                                player.feetStuckPos = new Vector2?(player.bodyChunks[0].pos + new Vector2(0.0f, -1f));
                            }
                        }
                        return;
                    }

                    if (player.bodyChunks[0].ContactPoint.y > -1 && player.input[0].x != 0 && player.bodyChunks[1].pos.y < player.bodyChunks[0].pos.y - 3.0 && player.bodyChunks[1].ContactPoint.x == player.input[0].x)
                    {
                        ++player.bodyChunks[1].pos.y;
                    }

                    if (player.input[0].y < 0)
                    {
                        player.GoThroughFloors = true;
                        for (int chunkIndex = 0; chunkIndex < 2; ++chunkIndex)
                        {
                            if (!IsTileSolidOrSlope(player, chunkIndex, 0, -1) && (IsTileSolidOrSlope(player, chunkIndex, -1, -1) || IsTileSolidOrSlope(player, chunkIndex, 1, -1))) // push into shortcuts and holes but don't stand still on slopes
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

                    if (player.input[0].x != 0)
                    {
                        // bodyMode would change when player.input[0].x != player.bodyChunks[0].ContactPoint.x // skip this check for now
                        player.canWallJump = player.input[0].x * -15;

                        // when upside down, flip instead of climbing
                        if (player.bodyChunks[0].pos.y < player.bodyChunks[1].pos.y)
                        {
                            player.bodyChunks[0].vel.y = Custom.LerpAndTick(player.bodyChunks[0].vel.y, 2f * player.gravity, 0.8f, 1f);
                            player.bodyChunks[1].vel.y = Custom.LerpAndTick(player.bodyChunks[1].vel.y, 0.0f, 0.8f, 1f);
                            player.bodyChunks[1].vel.x = -player.input[0].x * 5f;
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
                                if (player.input[0].y == 1 && !player.IsTileSolid(bChunk: 1, player.input[0].x, 0) && (player.bodyChunks[1].pos.x < player.bodyChunks[0].pos.x) == (player.input[0].x < 0)) // climb up even when lower body part is hanging in the air
                                {
                                    player.bodyChunks[0].pos.y += Math.Abs(player.bodyChunks[0].pos.x - player.bodyChunks[1].pos.x);
                                    player.bodyChunks[1].pos.x = player.bodyChunks[0].pos.x;
                                    player.bodyChunks[1].vel.x = -player.input[0].x * velXGain;
                                }

                                player.bodyChunks[0].vel.y += player.gravity;
                                player.bodyChunks[1].vel.y += player.gravity;

                                // downward momentum when ContactPoint.x != 0 is limited to -player.gravity bc of Player.Update()
                                player.bodyChunks[0].vel.y = Mathf.Lerp(player.bodyChunks[0].vel.y, player.input[0].y * 2.5f, 0.3f);
                                player.bodyChunks[1].vel.y = Mathf.Lerp(player.bodyChunks[1].vel.y, player.input[0].y * 2.5f, 0.3f);
                                ++player.animationFrame;
                            }
                            else if (player.lowerBodyFramesOffGround > 8 && player.input[0].y != -1) // stay in place // don't slide down // when only Option_WallClimb is enabled then this happens even when holding up // don't slide/climb when doing a normal jump off the ground
                            {
                                player.bodyChunks[0].vel.y = Custom.LerpAndTick(player.bodyChunks[0].vel.y, player.gravity, 0.3f, 1f);
                                player.bodyChunks[1].vel.y = Custom.LerpAndTick(player.bodyChunks[1].vel.y, player.gravity, 0.3f, 1f);

                                if (!player.IsTileSolid(bChunk: 1, player.input[0].x, 0) && player.input[0].x > 0 == player.bodyChunks[1].pos.x > player.bodyChunks[0].pos.x)
                                {
                                    player.bodyChunks[1].vel.x = -player.input[0].x * velXGain;
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

                    player.bodyChunks[1].vel.y += player.bodyChunks[1].submersion * room.gravity;
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
                if (player.input[0].x != 0 && player.bodyChunks[1].ContactPoint.x == player.input[0].x && player.IsTileSolid(0, player.input[0].x, -1) && !player.IsTileSolid(0, player.input[0].x, 0))
                {
                    player.simulateHoldJumpButton = 0;
                    return;
                }

                // jump to be able to climb on smaller obstacles
                if (player.input[0].x != 0 && (player.bodyChunks[0].ContactPoint.x == player.input[0].x && player.IsTileSolid(0, player.input[0].x, 0)) && !player.IsTileSolid(0, player.input[0].x, 1))
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
    }
}