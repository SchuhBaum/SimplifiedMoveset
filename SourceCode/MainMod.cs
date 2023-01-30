using System.Security.Permissions;
using BepInEx;
using MonoMod.Cil;
using UnityEngine;

// temporary fix // should be added automatically //TODO
#pragma warning disable CS0618
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
namespace SimplifiedMoveset
{
    [BepInPlugin("SchuhBaum.SimplifiedMoveset", "SimplifiedMoveset", "2.1.9")]
    public class MainMod : BaseUnityPlugin
    {
        //
        // meta data
        //

        public static readonly string MOD_ID = "SimplifiedMoveset";
        public static readonly string author = "SchuhBaum";
        public static readonly string version = "2.1.9";

        //
        // options
        //

        public static bool Option_BeamClimb => MainModOptions.beamClimb.Value;
        public static bool Option_BellySlide => MainModOptions.bellySlide.Value;
        public static bool Option_Crawl => MainModOptions.crawl.Value;

        public static bool Option_CrouchJump => MainModOptions.crouchJump.Value;
        public static bool Option_Grab => MainModOptions.grab.Value;
        public static bool Option_Roll_1 => MainModOptions.roll_1.Value;

        public static bool Option_Roll_2 => MainModOptions.roll_2.Value;
        public static bool Option_SlideTurn => MainModOptions.slideTurn.Value;
        public static bool Option_SpearThrow => MainModOptions.spearThrow.Value;

        public static bool Option_Swim => MainModOptions.swim.Value;
        public static bool Option_TubeWorm => MainModOptions.tubeWorm.Value;
        public static bool Option_WallClimb => MainModOptions.wallClimb.Value;

        public static bool Option_WallJump => MainModOptions.wallJump.Value;

        //
        // variables
        //

        public static bool isInitialized = false;

        //
        // main
        //

        public MainMod() { }
        public void OnEnable() => On.RainWorld.OnModsInit += RainWorld_OnModsInit; // initialize hooks


        //
        // public
        //

        public static void LogAllInstructions(ILContext? context, int indexStringLength = 9, int opCodeStringLength = 14)
        {
            if (context == null) return;

            Debug.Log("-----------------------------------------------------------------");
            Debug.Log("SimplifiedMoveset: Log all IL-instructions.");
            Debug.Log("Index:" + new string(' ', indexStringLength - 6) + "OpCode:" + new string(' ', opCodeStringLength - 7) + "Operand:");

            ILCursor cursor = new(context);
            ILCursor labelCursor = cursor.Clone();

            string cursorIndexString;
            string opCodeString;
            string operandString;

            while (true)
            {
                // this might return too early;
                // if (cursor.Next.MatchRet()) break;

                // should always break at some point;
                // only TryGotoNext() doesn't seem to be enough;
                // it still throws an exception;
                try
                {
                    if (cursor.TryGotoNext(MoveType.Before))
                    {
                        cursorIndexString = cursor.Index.ToString();
                        cursorIndexString = cursorIndexString.Length < indexStringLength ? cursorIndexString + new string(' ', indexStringLength - cursorIndexString.Length) : cursorIndexString;
                        opCodeString = cursor.Next.OpCode.ToString();

                        if (cursor.Next.Operand is ILLabel label)
                        {
                            labelCursor.GotoLabel(label);
                            operandString = "Label >>> " + labelCursor.Index;
                        }
                        else
                        {
                            operandString = cursor.Next.Operand?.ToString() ?? "";
                        }

                        if (operandString == "")
                        {
                            Debug.Log(cursorIndexString + opCodeString);
                        }
                        else
                        {
                            opCodeString = opCodeString.Length < opCodeStringLength ? opCodeString + new string(' ', opCodeStringLength - opCodeString.Length) : opCodeString;
                            Debug.Log(cursorIndexString + opCodeString + operandString);
                        }
                    }
                    else
                    {
                        break;
                    }
                }
                catch
                {
                    break;
                }
            }
            Debug.Log("-----------------------------------------------------------------");
        }

        public static void LogPlayerInformation(Player player)
        {
            Debug.Log("player.input[0].x " + player.input[0].x);
            Debug.Log("player.input[0].y " + player.input[0].y);
            Debug.Log("player.input[0].jmp " + player.input[0].jmp);
            Debug.Log("player.IsJumpPressed " + player.IsJumpPressed());
            Debug.Log("player.canJump " + player.canJump);
            Debug.Log("player.wantToJump " + player.wantToJump);
            Debug.Log("player.canWallJump " + player.canWallJump);
            Debug.Log("player.animation " + player.animation);
            Debug.Log("player.bodyMode " + player.bodyMode);
        }

        //
        // private
        //

        private void RainWorld_OnModsInit(On.RainWorld.orig_OnModsInit orig, RainWorld rainWorld)
        {
            orig(rainWorld);

            MachineConnector.SetRegisteredOI(MOD_ID, MainModOptions.instance);

            if (isInitialized) return;
            isInitialized = true;

            Debug.Log("SimplifiedMoveset: Version " + version);

            BodyChunkMod.OnEnable();
            BodyChunkConnectionMod.OnEnable();
            RainWorldGameMod.OnEnable();

            SlugcatHandMod.OnEnable();
            WeaponMod.OnEnable();
        }
    }
}