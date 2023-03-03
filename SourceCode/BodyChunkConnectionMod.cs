using UnityEngine;
using static SimplifiedMoveset.BodyChunkMod;
using static SimplifiedMoveset.MainMod;

namespace SimplifiedMoveset;

public static class BodyChunkConnectionMod
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
        if (Option_BellySlide || Option_Crawl)
        {
            if (is_enabled)
            {
                // save bodyChunkConnectionVel;
                // used in bodyChunks to be aware of pulling and pushing effects;
                On.PhysicalObject.BodyChunkConnection.Update += BodyChunkConnection_Update;
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
        if (!bodyChunkConnection.active || bodyChunkConnection.chunk1.Get_Attached_Fields() is not BodyChunk_Attached_Fields attached_fields_1 || bodyChunkConnection.chunk2.Get_Attached_Fields() is not BodyChunk_Attached_Fields attached_fields_2)
        {
            orig(bodyChunkConnection);
            return;
        }

        Vector2 chunk_1_velocity = bodyChunkConnection.chunk1.vel;
        Vector2 chunk_2_velocity = bodyChunkConnection.chunk2.vel;
        orig(bodyChunkConnection);

        attached_fields_1.body_chunk_connection_velocity = bodyChunkConnection.chunk1.vel - chunk_1_velocity; // this needs to be adapted if there are multiple bodyChunks connecting to one // enough for players with only one connection
        attached_fields_2.body_chunk_connection_velocity = bodyChunkConnection.chunk2.vel - chunk_2_velocity;
    }
}