using UnityEngine;

namespace SimplifiedMoveset
{
    public static class BodyChunkConnectionMod
    {
        internal static void OnEnable()
        {
            On.PhysicalObject.BodyChunkConnection.Update += BodyChunkConnection_Update; // save bodyChunkConnectionVel // used in bodyChunks to be aware of pulling and pushing effects
        }

        // ----------------- //
        // private functions //
        // ----------------- //

        private static void BodyChunkConnection_Update(On.PhysicalObject.BodyChunkConnection.orig_Update orig, PhysicalObject.BodyChunkConnection bodyChunkConnection)
        {
            if ((MainMod.Option_BellySlide || MainMod.Option_Crawl) && bodyChunkConnection.active && bodyChunkConnection.chunk1.owner is Player)
            {
                Vector2 chunk1Vel = bodyChunkConnection.chunk1.vel;
                Vector2 chunk2Vel = bodyChunkConnection.chunk2.vel;
                orig(bodyChunkConnection);

                BodyChunkMod.bodyChunkConnectionVel[bodyChunkConnection.chunk1] = bodyChunkConnection.chunk1.vel - chunk1Vel; // this needs to be adapted if there are multiple bodyChunks connecting to one // enough for players with only one connection
                BodyChunkMod.bodyChunkConnectionVel[bodyChunkConnection.chunk2] = bodyChunkConnection.chunk2.vel - chunk2Vel;
            }
            else
            {
                orig(bodyChunkConnection);
            }
        }
    }
}