using BepInEx;
using MonoMod.Cil;
using System.Security.Permissions;
using UnityEngine;
using static SimplifiedMoveset.MainModOptions;

// allows access to private members;
#pragma warning disable CS0618
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618

namespace SimplifiedMoveset;

[BepInPlugin("SimplifiedMoveset", "SimplifiedMoveset", "2.4.9")]
public class MainMod : BaseUnityPlugin {
    //
    // meta data
    //

    public static readonly string mod_id = "SimplifiedMoveset";
    public static readonly string author = "SchuhBaum";
    public static readonly string version = "2.4.9";

    //
    // options
    //

    public static bool Option_BeamClimb => beam_climb.Value;
    public static bool Option_BellySlide => belly_slide.Value;
    public static bool Option_Crawl => crawl.Value;

    public static bool Option_CrouchJump => crouch_jump.Value;
    public static bool Option_Grab => grab.Value;
    public static bool Option_Gourmand => gourmand.Value;

    public static bool Option_Roll_1 => roll_1.Value;
    public static bool Option_Roll_2 => roll_2.Value;
    public static bool Option_SlideTurn => slide_turn.Value;

    public static bool Option_SpearThrow => spear_throw.Value;
    public static bool Option_StandUp => stand_up.Value;
    public static bool Option_Swim => swim.Value;

    public static bool Option_TubeWorm => tube_worm.Value;
    public static bool Option_WallClimb => wall_climb.Value;
    public static bool Option_WallJump => wall_jump.Value;

    //
    // variables
    //

    public static bool can_log_il_hooks = false;
    private static bool _is_initialized = false;

    //
    // main
    //

    public MainMod() { }
    public void OnEnable() => On.RainWorld.OnModsInit += RainWorld_OnModsInit; // initialize hooks

    //
    // public
    //

    public static void LogAllInstructions(ILContext? context, int index_string_length = 9, int op_code_string_length = 14) {
        if (context == null) return;

        Debug.Log("-----------------------------------------------------------------");
        Debug.Log("Log all IL-instructions.");
        Debug.Log("Index:" + new string(' ', index_string_length - 6) + "OpCode:" + new string(' ', op_code_string_length - 7) + "Operand:");

        ILCursor cursor = new(context);
        ILCursor label_cursor = cursor.Clone();

        string cursor_index_string;
        string op_code_string;
        string operand_string;

        while (true) {
            // this might return too early;
            // if (cursor.Next.MatchRet()) break;

            // should always break at some point;
            // only TryGotoNext() doesn't seem to be enough;
            // it still throws an exception;
            try {
                if (cursor.TryGotoNext(MoveType.Before)) {
                    cursor_index_string = cursor.Index.ToString();
                    cursor_index_string = cursor_index_string.Length < index_string_length ? cursor_index_string + new string(' ', index_string_length - cursor_index_string.Length) : cursor_index_string;
                    op_code_string = cursor.Next.OpCode.ToString();

                    if (cursor.Next.Operand is ILLabel label) {
                        label_cursor.GotoLabel(label);
                        operand_string = "Label >>> " + label_cursor.Index;
                    } else {
                        operand_string = cursor.Next.Operand?.ToString() ?? "";
                    }

                    if (operand_string == "") {
                        Debug.Log(cursor_index_string + op_code_string);
                    } else {
                        op_code_string = op_code_string.Length < op_code_string_length ? op_code_string + new string(' ', op_code_string_length - op_code_string.Length) : op_code_string;
                        Debug.Log(cursor_index_string + op_code_string + operand_string);
                    }
                } else {
                    break;
                }
            } catch {
                break;
            }
        }
        Debug.Log("-----------------------------------------------------------------");
    }

    public static void LogPlayerInformation(Player? player) {
        if (player == null) return;

        Debug.Log("player.input[0].x " + player.input[0].x);
        Debug.Log("player.input[0].y " + player.input[0].y);
        Debug.Log("player.input[0].jmp " + player.input[0].jmp);

        Debug.Log("player.IsJumpPressed " + player.IsJumpPressed());
        Debug.Log("player.canJump " + player.canJump);
        Debug.Log("player.wantToJump " + player.wantToJump);
        Debug.Log("player.canWallJump " + player.canWallJump);

        Debug.Log("player.shortcutDelay " + player.shortcutDelay);
        Debug.Log("player.inShortcut " + player.inShortcut);

        Debug.Log("player.animation " + player.animation);
        Debug.Log("player.bodyMode " + player.bodyMode);
    }

    //
    // private
    //

    private void RainWorld_OnModsInit(On.RainWorld.orig_OnModsInit orig, RainWorld rain_world) {
        orig(rain_world);

        MachineConnector.SetRegisteredOI(mod_id, main_mod_options);

        if (_is_initialized) return;
        _is_initialized = true;

        Debug.Log(mod_id + ": version " + version);
        ProcessManagerMod.OnEnable();
        RainWorldGameMod.OnEnable();
    }
}
