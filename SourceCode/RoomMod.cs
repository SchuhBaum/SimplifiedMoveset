using RWCustom;

namespace SimplifiedMoveset
{
    public static class RoomMod
    {
        // ---------------- //
        // public functions //
        // ---------------- //

        public static Room.Tile? GetNonAirTileBelow(Room? room, IntVector2 tilePosition)
        {
            if (room == null)
            {
                return null;
            }

            for (int tilePositionY = tilePosition.y; tilePositionY >= 0; --tilePositionY)
            {
                Room.Tile tile = room.GetTile(tilePosition.x, tilePositionY);
                if (tile.Terrain != Room.Tile.TerrainType.Air)
                {
                    return tile;
                }
            }
            return null;
        }
    }
}