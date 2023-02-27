using UnityEngine;

using static SimplifiedMoveset.MainMod;

namespace SimplifiedMoveset;

public static class BodyChunkConnectionMod
{
    //
    // variables
    //

    private static bool isEnabled = false;

    //
    //
    //

    internal static void OnToggle()
    {
        isEnabled = !isEnabled;
        if (Option_BellySlide || Option_Crawl)
        {
            if (isEnabled)
            {
                On.PhysicalObject.BodyChunkConnection.Update += BodyChunkConnection_Update; // save bodyChunkConnectionVel // used in bodyChunks to be aware of pulling and pushing effects
            }
            else
            {
                On.PhysicalObject.BodyChunkConnection.Update -= BodyChunkConnection_Update;
            }
        }
    }

    //
    // private
    //

    private static void BodyChunkConnection_Update(On.PhysicalObject.BodyChunkConnection.orig_Update orig, PhysicalObject.BodyChunkConnection bodyChunkConnection) // Option_BellySlide // Option_Crawl
    {
        if (!bodyChunkConnection.active || bodyChunkConnection.chunk1.owner is not Player)
        {
            orig(bodyChunkConnection);
            return;
        }

        Vector2 chunk1Vel = bodyChunkConnection.chunk1.vel;
        Vector2 chunk2Vel = bodyChunkConnection.chunk2.vel;
        orig(bodyChunkConnection);

        bodyChunkConnection.chunk1.GetAttachedFields().bodyChunkConnectionVel = bodyChunkConnection.chunk1.vel - chunk1Vel; // this needs to be adapted if there are multiple bodyChunks connecting to one // enough for players with only one connection
        bodyChunkConnection.chunk2.GetAttachedFields().bodyChunkConnectionVel = bodyChunkConnection.chunk2.vel - chunk2Vel;
    }
}