using RWCustom;
using UnityEngine;

namespace SimplifiedMoveset
{
    public static class WeaponMod
    {
        internal static void OnEnable()
        {
            On.Weapon.Shoot += Weapon_Shoot;
            On.Weapon.Thrown += Weapon_Thrown;
        }

        //
        // private functions
        //

        private static void Weapon_Shoot(On.Weapon.orig_Shoot orig, Weapon weapon, Creature shotBy, Vector2 thrownPos, Vector2 throwDir, float force, bool eu)
        {
            orig(weapon, shotBy, thrownPos, throwDir, force, eu);

            if (!MainMod.Option_SpearThrow) return;
            weapon.changeDirCounter = 0;
        }

        private static void Weapon_Thrown(On.Weapon.orig_Thrown orig, Weapon weapon, Creature thrownBy, Vector2 thrownPos, Vector2? firstFrameTraceFromPos, IntVector2 throwDir, float frc, bool eu)
        {
            orig(weapon, thrownBy, thrownPos, firstFrameTraceFromPos, throwDir, frc, eu);

            if (!MainMod.Option_SpearThrow) return;
            weapon.changeDirCounter = 0;
        }
    }
}