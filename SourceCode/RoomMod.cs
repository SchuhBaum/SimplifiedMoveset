using RWCustom;
using static Room;

namespace SimplifiedMoveset;

public static class RoomMod {
    //
    // public
    //

    public static Tile? Get_Non_Air_Tile_Below(Room? room, IntVector2 tile_position) {
        if (room == null) return null;

        for (int tile_position_y = tile_position.y; tile_position_y >= 0; --tile_position_y) {
            Tile tile = room.GetTile(tile_position.x, tile_position_y);
            if (tile.Terrain != Tile.TerrainType.Air) return tile;
        }
        return null;
    }
}
