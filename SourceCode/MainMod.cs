using BepInEx;
using MonoMod.Cil;
using UnityEngine;

namespace SimplifiedMoveset
{
    [BepInPlugin("SchuhBaum.SimplifiedMoveset", "SimplifiedMoveset", "0.70")]
    public class MainMod : BaseUnityPlugin
    {
        public string updateURL = "http://beestuff.pythonanywhere.com/audb/api/mods/8/2";
        public int version = 16;
        public string keyE = "AQAB";
        public string keyN = "0Sb8AUUh0jkFOuNDGJti4jL0iTB4Oug0pM8opATxJH8hfAt6FW3//Q4wb4VfTHZVP3+zHMX6pxcqjdvN0wt/0SWyccfoFhx2LupmT3asV4UDPBdQNmDeA/XMfwmwYb23yxp0apq3kVJNJ3v1SExvo+EPQP4/74JueNBiYshKysRK1InJfkrO1pe1WxtcE7uIrRBVwIgegSVAJDm4PRCODWEp533RxA4FZjq8Hc4UP0Pa0LxlYlSI+jJ+hUrdoA6wd+c/R+lRqN2bjY9OE/OktAxqgthEkSXTtmZwFkCjds0RCqZTnzxfJLN7IheyZ69ptzcB6Zl7kFTEofv4uDjCYNic52/C8uarj+hl4O0yU4xpzdxhG9Tq9SAeNu7h6Dt4Impbr3dAonyVwOhA/HNIz8TUjXldRs0THcZumJ/ZvCHO3qSh7xKS/D7CWuwuY5jWzYZpyy14WOK55vnEFS0GmTwjR+zZtSUy2Y7m8hklllqHZNqRYejoORxTK4UkL4GFOk/uLZKVtOfDODwERWz3ns/eOlReeUaCG1Tole7GhvoZkSMyby/81k3Fh16Z55JD+j1HzUCaoKmT10OOmLF7muV7RV2ZWG0uzvN2oUfr5HSN3TveNw7JQPd5DvZ56whr5ExLMS7Gs6fFBesmkgAwcPTkU5pFpIjgbyk07lDI81k=";


        public readonly string author = "SchuhBaum";

        public static bool Option_BeamClimb = true;
        public static bool Option_BellySlide = true;
        public static bool Option_Crawl = true;

        public static bool Option_CrouchJump = true;
        public static bool Option_LedgeGrab = false;
        public static bool Option_RocketJump = true;

        public static bool Option_Roll = true;
        public static bool Option_SlideTurn = false;
        public static bool Option_SpearThrow = true;

        public static bool Option_Swim = false;
        public static bool Option_TubeWorm = true;
        public static bool Option_WallClimb = false;

        public static bool Option_WallJump = true;

        //
        // ConfigMachine
        //

        public static MainMod? instance;
        public static OptionalUI.OptionInterface LoadOI()
        {
            return new MainModOptions();
        }

        //
        // main
        //

        public MainMod() => instance = this;

        public void OnEnable()
        {
            On.RainWorld.Start += RainWorld_Start; // initialize hooks
        }

        // ---------------- //
        // public functions //
        // ---------------- //

        public static void LogAllInstructions(ILContext? context, int indexStringLength = 9, int opCodeStringLength = 14)
        {
            if (context == null)
            {
                return;
            }

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
                if (cursor.Next.MatchRet())
                {
                    break;
                }

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
            Debug.Log("-----------------------------------------------------------------");
        }

        // ----------------- //
        // private functions //
        // ----------------- //

        private void RainWorld_Start(On.RainWorld.orig_Start orig, RainWorld rainWorld)
        {
            Debug.Log("SimplifiedMoveset: Version " + Info.Metadata.Version);
            BodyChunkMod.OnEnable();
            BodyChunkConnectionMod.OnEnable();
            PlayerMod.OnEnable();

            RainWorldGameMod.OnEnable();
            SlugcatHandMod.OnEnable();
            TubeWormMod.OnEnable();
            orig(rainWorld);
        }
    }
}