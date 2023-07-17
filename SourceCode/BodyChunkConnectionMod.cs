using UnityEngine;
using static SimplifiedMoveset.BodyChunkMod;
using static SimplifiedMoveset.MainMod;

namespace SimplifiedMoveset;

internal static class BodyChunkConnectionMod {
    //
    // main
    //

    internal static void On_Config_Changed() {
        On.PhysicalObject.BodyChunkConnection.Update -= BodyChunkConnection_Update;
        if (Option_BellySlide || Option_Crawl) {
            // save body_chunk_connection_velocity;
            // used in bodyChunks to be aware of pulling and pushing effects;
            On.PhysicalObject.BodyChunkConnection.Update += BodyChunkConnection_Update;
        }
    }

    //
    // private
    //

    private static void BodyChunkConnection_Update(On.PhysicalObject.BodyChunkConnection.orig_Update orig, PhysicalObject.BodyChunkConnection body_chunk_connection) { // Option_BellySlide // Option_Crawl
        if (!body_chunk_connection.active) {
            orig(body_chunk_connection);
            return;
        }

        if (body_chunk_connection.chunk1.Get_Attached_Fields() is not BodyChunk_Attached_Fields chunk1_attached_fields) {
            orig(body_chunk_connection);
            return;
        }

        if (body_chunk_connection.chunk2.Get_Attached_Fields() is not BodyChunk_Attached_Fields chunk2_attached_fields) {
            orig(body_chunk_connection);
            return;
        }

        Vector2 chunk_1_velocity = body_chunk_connection.chunk1.vel;
        Vector2 chunk_2_velocity = body_chunk_connection.chunk2.vel;
        orig(body_chunk_connection);

        // this needs to be adapted if there are multiple body chunks connecting to one; 
        // enough for players since they have only two body chunks with one connection;
        chunk1_attached_fields.body_chunk_connection_velocity = body_chunk_connection.chunk1.vel - chunk_1_velocity;
        chunk2_attached_fields.body_chunk_connection_velocity = body_chunk_connection.chunk2.vel - chunk_2_velocity;
    }
}
