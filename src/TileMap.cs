using System.Collections.Generic;

namespace BigBoyEngine;

public struct Tile {
    public int Index;
}

public struct TileSet {

}

public class TileMap : Node {
    private readonly Dictionary<(int, int), Tile> _tiles = new();
    public readonly int GridWidth, GridHeight;

    public TileMap(int gridWidth, int gridHeight) {
        GridWidth = gridWidth;
        GridHeight = gridHeight;
    }

    public override void Draw() {
        foreach (var tile in _tiles.Values) {

        }
        base.Draw();
    }
}