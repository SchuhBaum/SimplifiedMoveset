using RWCustom;

using static Room;
namespace SimplifiedMoveset;

public static class RoomMod
{
    //
    // public
    //

    public static Tile? GetNonAirTileBelow(Room? room, IntVector2 tilePosition)
    {
        if (room == null) return null;

        for (int tilePositionY = tilePosition.y; tilePositionY >= 0; --tilePositionY)
        {
            Tile tile = room.GetTile(tilePosition.x, tilePositionY);
            if (tile.Terrain != Tile.TerrainType.Air) return tile;
        }
        return null;
    }
}