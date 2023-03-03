using RWCustom;
using UnityEngine;

using static SimplifiedMoveset.MainMod;

namespace SimplifiedMoveset;

public static class WeaponMod
{
    //
    // variables
    //

    private static bool is_enabled = false;

    //
    //
    //

    internal static void OnToggle()
    {
        is_enabled = !is_enabled;
        if (Option_SpearThrow)
        {
            if (is_enabled)
            {
                On.Weapon.Shoot += Weapon_Shoot;
                On.Weapon.Thrown += Weapon_Thrown;
            }
            else
            {
                On.Weapon.Shoot -= Weapon_Shoot;
                On.Weapon.Thrown -= Weapon_Thrown;
            }
        }
    }

    //
    // private
    //

    private static void Weapon_Shoot(On.Weapon.orig_Shoot orig, Weapon weapon, Creature shotBy, Vector2 thrownPos, Vector2 throwDir, float force, bool eu) // Option_SpearThrow
    {
        orig(weapon, shotBy, thrownPos, throwDir, force, eu);
        weapon.changeDirCounter = 0;
    }

    private static void Weapon_Thrown(On.Weapon.orig_Thrown orig, Weapon weapon, Creature thrownBy, Vector2 thrownPos, Vector2? firstFrameTraceFromPos, IntVector2 throwDir, float frc, bool eu) // Option_SpearThrow
    {
        orig(weapon, thrownBy, thrownPos, firstFrameTraceFromPos, throwDir, frc, eu);
        weapon.changeDirCounter = 0;
    }
}