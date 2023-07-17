using RWCustom;
using UnityEngine;

using static SimplifiedMoveset.MainMod;

namespace SimplifiedMoveset;

internal static class WeaponMod {
    //
    // main
    //

    internal static void On_Config_Changed() {
        On.Weapon.Shoot -= Weapon_Shoot;
        On.Weapon.Thrown -= Weapon_Thrown;

        if (Option_SpearThrow) {
            On.Weapon.Shoot += Weapon_Shoot;
            On.Weapon.Thrown += Weapon_Thrown;
        }
    }

    //
    // private
    //

    private static void Weapon_Shoot(On.Weapon.orig_Shoot orig, Weapon weapon, Creature shot_by, Vector2 thrown_pos, Vector2 throw_dir, float force, bool eu) { // Option_SpearThrow
        orig(weapon, shot_by, thrown_pos, throw_dir, force, eu);
        weapon.changeDirCounter = 0;
    }

    private static void Weapon_Thrown(On.Weapon.orig_Thrown orig, Weapon weapon, Creature thrown_by, Vector2 thrown_pos, Vector2? first_frame_trace_from_pos, IntVector2 throw_dir, float frc, bool eu) { // Option_SpearThrow
        orig(weapon, thrown_by, thrown_pos, first_frame_trace_from_pos, throw_dir, frc, eu);
        weapon.changeDirCounter = 0;
    }
}
