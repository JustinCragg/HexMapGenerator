using UnityEngine;
using System.Collections.Generic;

public class Tile : MonoBehaviour {
    public class TileId {
        public LandType landType = LandType.Ocean;
        public float landHeight = 0;
        public TempType tempType = TempType.Plains;
        public int latitude = 0;

        public bool river = false;
    }

    public enum LandType { Flatland, Hill, ForestHill, Forest, Mountain, Jungle, Swamp, Ocean }
    public enum TempType { Desert, Plains, Grassland, Tundra, Arctic }

    public Vector2 tilePos;
    public List<Tile> adjacents = new List<Tile>();

    public LandType landType = LandType.Ocean;
    public TempType tempType = TempType.Grassland;

    public bool river = false;

    public int startScore = 0;

    // ***** Pathfinding *****
    // The parent tile when navigating
    public Tile parent;
    // Cost to move on to this tile
    public float moveCost = 1;
    // Total path cost
    public float fScore = Mathf.Infinity;
    // Cost to this point
    public float gScore = Mathf.Infinity;
    // Estimated remaining path
    public float hScore = Mathf.Infinity;

    public void init(int x, int y, Color colour) {
        GetComponent<Renderer>().material.color = colour;
        tilePos.x = x;
        tilePos.y = y;
    }

    public void addAdj(Tile adj) {
        adjacents.Add(adj);
    }
}
