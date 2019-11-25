using UnityEngine;
using System.Collections.Generic;

public class MapManager : MonoBehaviour {
    struct Line {
        public Vector3 pos1;
        public Vector3 pos2;
    }
    struct Bounds {
        public Line line1;
        public Line line2;

        public float getArea() {
            float baseLength = Vector3.Distance(line1.pos2, line2.pos2);

            Vector3 baseDirec = (line2.pos2 - line1.pos2).normalized;
            baseDirec.y = baseDirec.x;
            baseDirec.x = baseDirec.z;
            baseDirec.z = baseDirec.y;
            baseDirec.y = 0;
            Vector3 tempDirec = line2.pos2 - line2.pos1;
            baseDirec.x *= tempDirec.x;
            baseDirec.z *= tempDirec.z;
            float height = baseDirec.magnitude;

            float area = (baseLength * height) / 2.0f;
            return area;
        }
    }

    public GameObject baseTile;
    // Length : Width (14:9)
    public float length = 84;
    public float width = 54;

    List<List<Tile>> mapTiles = new List<List<Tile>>();
    List<List<Tile.TileId>> mapIds = new List<List<Tile.TileId>>();

    // Land Mass Size
    int landMassParam = 2;
    // Controls amount of desert or tundra/arctic
    int tempParam= 1;
    // Controls amount of desert or jungle/swamp
    int climateParam = 1;
    // Controls amount of mountains or hills/lakes
    int ageParam = 2;

    static int sortByStartScore(Tile a, Tile b) {
        return a.startScore.CompareTo(b.startScore);
    }

    public void generateMap() {
        determineMap();
        tileCreation();
        determineAdjs();
    }

    void determineMap() {
        mapIds.Clear();
        for (int x = 0; x < length; x++) {
            mapIds.Add(new List<Tile.TileId>());
            for (int y = 0; y < width; y++) {
                mapIds[x].Add(new Tile.TileId());
            }
        }

        float yMax = 0;
        float yMin = Mathf.Infinity;
        float ySum = 0;
        float totalLandMass = 0;
        // Check whether land mass is large enough
        while (totalLandMass < ((landMassParam + 2) * 320)) {
            // Generate Land Mass
            List<Vector2> landmass = new List<Vector2>();
            Vector2 randPos = new Vector2(Random.Range(4, length - 5), Random.Range(8, width - 9));
            if (Random.Range(0.0f, 1.0f) >= 0.5f) {
                randPos += new Vector2(length / 2, width / 2);
                randPos /= 2;
            }
            int path = Random.Range(25, 75);
            landmass.Add(randPos);
            while (path > 0 && pointInBounds(randPos)) {
                List<Vector2> adjs = getAdjacents(randPos);
                randPos = adjs[Random.Range(0, adjs.Count - 1)];
                if (landmass.Contains(randPos) == false) {
                    path--;
                    landmass.Add(randPos);
                }
                // Adding additional mass
                if (pointInBounds(randPos + new Vector2(1, 1))) {
                    if (landmass.Contains(randPos + new Vector2(1, 1)) == false) {
                        landmass.Add(randPos + new Vector2(1, 1));
                    }
                }
            }

            // Merge Chunk to main geography
            foreach (Vector2 pos in landmass) {
                if (mapIds[(int)pos.x][(int)pos.y].landHeight == 0) {
                    ySum += pos.y;
                }
                if (pos.y < yMin) {
                    yMin = pos.y;
                }
                if (pos.y > yMax) {
                    yMax = pos.y;
                }
                mapIds[(int)pos.x][(int)pos.y].landHeight++;
            }

            totalLandMass += landmass.Count;
        }

        // Shift land mass
        float yDiff = width/2.0f - (ySum / totalLandMass);
        float diff = ((yMin - width / 2.0f) + (width - yMax)) / 2.0f;

        for (int i = 0; i < Mathf.Abs(diff) - yDiff; i++) {
            for (int y = 1; y < width; y++) {
                for (int x = 0; x < length; x++) {
                    mapIds[x][y - 1].landHeight = mapIds[x][y].landHeight;
                }
            }
            for (int x = 0; x < length; x++) {
                mapIds[x][(int)width - 1].landHeight = 0;
            }
        }

        // Assigning height type of tiles
        for (int x = 0; x < length; x++) {
            for (int y = 0; y < width; y++) {
                switch ((int)mapIds[x][y].landHeight) {
                    case 0:
                        // Ocean
                        mapIds[x][y].landType = Tile.LandType.Ocean;
                        break;
                    case 1:
                        // Flat land
                        mapIds[x][y].landType = Tile.LandType.Flatland;
                        break;
                    case 2:
                        // Mountain
                        mapIds[x][y].landType = Tile.LandType.Mountain;
                        break;
                    default:
                        // Hill
                        mapIds[x][y].landType = Tile.LandType.Hill;
                        break;
                }
            }
        }

        // Temperature Adjustments
        for (int x = 0; x < length; x++) {
            for (int y = 0; y < width; y++) {
                int value = Mathf.Abs(y - 32 + Random.Range(0, 7)) + (1 - tempParam);
                value = (value / 6) + 1;
                mapIds[x][y].latitude = value / 2;
                switch (mapIds[x][y].latitude) {
                    case 0:
                        mapIds[x][y].tempType = Tile.TempType.Desert;
                        break;
                    case 1:
                        mapIds[x][y].tempType = Tile.TempType.Plains;
                        break;
                    case 2:
                        mapIds[x][y].tempType = Tile.TempType.Tundra;
                        break;
                    case 3:
                        mapIds[x][y].tempType = Tile.TempType.Arctic;
                        break;
                }
            }
        }

        // Climate Adjustments
        for (int y = 0; y < width; y++) {
            int wetness = 0;
            int latitude = (int)Mathf.Abs(width / 2 - y);
            for (int x = 0; x < length; x++) {
                if (mapIds[x][y].landType == Tile.LandType.Ocean) {
                    int yield = Mathf.Abs(latitude - 12) + climateParam * 4;
                    if (yield > wetness) {
                        wetness++;
                    }
                }
                else if (wetness > 0) {
                    int rainfall = Random.Range(0, 7 - climateParam * 2);
                    wetness -= rainfall;
                    // Land Type
                    if (mapIds[x][y].landType == Tile.LandType.Mountain) {
                        wetness -= 3;
                    }
                    else if (mapIds[x][y].landType == Tile.LandType.Hill) {
                        mapIds[x][y].landType = Tile.LandType.ForestHill;
                    }
                    // Biome Type
                    if (mapIds[x][y].tempType == Tile.TempType.Plains) {
                        mapIds[x][y].tempType = Tile.TempType.Grassland;
                    }
                    else if (mapIds[x][y].tempType == Tile.TempType.Tundra) {
                        mapIds[x][y].tempType = Tile.TempType.Arctic;
                    }
                    else if (mapIds[x][y].tempType == Tile.TempType.Desert) {
                        mapIds[x][y].tempType = Tile.TempType.Plains;
                    }
                }
            }
            wetness = 0;
            for (int x = (int)length - 1; x >= 0; x--) {
                if (mapIds[x][y].landType == Tile.LandType.Ocean) {
                    int yield = latitude/2+climateParam;
                    if (yield > wetness) {
                        wetness++;
                    }
                }
                else if (wetness > 0) {
                    int rainfall = Random.Range(0, 7 - climateParam * 2);
                    wetness -= rainfall;
                    // Land Type
                    if (mapIds[x][y].landType == Tile.LandType.Swamp) {
                        mapIds[x][y].landType = Tile.LandType.Forest;
                    }
                    else if (mapIds[x][y].tempType == Tile.TempType.Grassland) {
                        if (latitude < 10) {
                            mapIds[x][y].landType = Tile.LandType.Jungle;
                        }
                        else {
                            mapIds[x][y].landType = Tile.LandType.Swamp;
                        }
                        wetness -= 2;
                    }
                    else if (mapIds[x][y].landType == Tile.LandType.Hill) {
                        mapIds[x][y].landType = Tile.LandType.ForestHill;
                    }
                    else if (mapIds[x][y].landType == Tile.LandType.Mountain) {
                        mapIds[x][y].landType = Tile.LandType.Forest;
                    }
                    // Biome Type
                    if (mapIds[x][y].tempType == Tile.TempType.Plains) {
                        mapIds[x][y].tempType = Tile.TempType.Grassland;
                    }
                    else if (mapIds[x][y].tempType == Tile.TempType.Desert) {
                        mapIds[x][y].tempType = Tile.TempType.Plains;
                    }
                }
            }
        }

        // Age/Erosion adjustments
        int loopCount = 800 * (1 + ageParam);
        Vector2 tile = new Vector2(Random.Range(0, length), Random.Range(0, width));
        for (int i = 0; i < loopCount; i++) {
            if (i % 2 == 0) {
                // Even
                tile = new Vector2(Random.Range(0, length), Random.Range(0, width));
            }
            else {
                // Odd
                List<Vector2> adjs = getAdjacents(tile);
                tile = adjs[Random.Range(0, adjs.Count)];
            }
            int x = (int)tile.x;
            int y = (int)tile.y;
            // Land Type
            if (mapIds[x][y].landType == Tile.LandType.Forest) {
                mapIds[x][y].landType = Tile.LandType.Jungle;
            }
            else if (mapIds[x][y].landType == Tile.LandType.Swamp) {
                mapIds[x][y].landType = Tile.LandType.Flatland;
                mapIds[x][y].tempType = Tile.TempType.Grassland;
            }
            else if (mapIds[x][y].landType == Tile.LandType.Jungle) {
                mapIds[x][y].landType = Tile.LandType.Swamp;
            }
            else if (mapIds[x][y].landType == Tile.LandType.Mountain) {
                mapIds[x][y].landType = Tile.LandType.Hill;
            }
            else if (mapIds[x][y].landType == Tile.LandType.Flatland) {
                List<Vector2> adjs = getAdjacents(tile);
                int count = 0;
                for (int j = 0; j < adjs.Count; j++) {
                    if (mapIds[(int)adjs[j].x][(int)adjs[j].y].landType == Tile.LandType.Ocean) {
                        count++;
                    }
                }
                if (count > 3) {
                    mapIds[x][y].landType = Tile.LandType.Ocean;
                }
            }
            else if (mapIds[x][y].landType == Tile.LandType.Ocean) {
                List<Vector2> adjs = getAdjacents(tile);
                int count = 0;
                for (int j = 0; j < adjs.Count; j++) {
                    if (mapIds[(int)adjs[j].x][(int)adjs[j].y].landType != Tile.LandType.Ocean) {
                        count++;
                    }
                }
                if (count > 3) {
                    mapIds[x][y].landType = Tile.LandType.Flatland;
                }
            }
            // Biome Type
            if (mapIds[x][y].tempType == Tile.TempType.Plains && mapIds[x][y].landType != Tile.LandType.Ocean) {
                mapIds[x][y].landType = Tile.LandType.Flatland;
            }
            else if (mapIds[x][y].tempType == Tile.TempType.Tundra && mapIds[x][y].landType != Tile.LandType.Ocean) {
                mapIds[x][y].landType = Tile.LandType.Flatland;
            }
            else if (mapIds[x][y].tempType == Tile.TempType.Grassland && mapIds[x][y].landType != Tile.LandType.Ocean) {
                mapIds[x][y].landType = Tile.LandType.Forest;
            }
            else if (mapIds[x][y].tempType == Tile.TempType.Desert && mapIds[x][y].landType != Tile.LandType.Ocean) {
                mapIds[x][y].tempType = Tile.TempType.Plains;
            }
            else if (mapIds[x][y].tempType == Tile.TempType.Arctic && mapIds[x][y].landType != Tile.LandType.Ocean) {
                mapIds[x][y].landType = Tile.LandType.Mountain;
            }
        }

        /*
        // Pick a random hill
        // Give the hill a river
        // Pick an adjacent tile
        // If the hill wasn't near an ocean and the next tile isn't an ocean, mountain or already have a river
        //     Continue the river
        // Else If (the hill is near an ocean or the next tile is already a river) and the river's length is less than 5
        //     Convert nearby forest into jungle
        //     Create a new river
        // Else
        //     Cancel that river
        */
        List<Vector2> hills = new List<Vector2>();
        for (int x = 0; x < length; x++) {
            for (int y = 0; y < width; y++) {
                if (mapIds[x][y].landType == Tile.LandType.Hill || mapIds[x][y].landType == Tile.LandType.ForestHill || mapIds[x][y].landType == Tile.LandType.Mountain) {
                    hills.Add(new Vector2(x,y));
                }
            }
        }

        float riverCount = 0;
        while (riverCount <= (climateParam * 3 * landMassParam * 2) + 6 && hills.Count > 0) {
            bool continueRiver = true;

            List<Vector2> currentRiver = new List<Vector2>();
            Vector2 origin = hills[Random.Range(0, hills.Count)];
            hills.Remove(origin);
            while (continueRiver == true) {
                currentRiver.Add(origin);
                List<Vector2> adjs = getAdjacents(origin);
                float rand = Random.Range(0.0f, 1.0f);
                float value = 1.8395f * Mathf.Exp(1) - (Mathf.Pow(rand - 0.5f, 2.0f) / (2.0f * Mathf.Pow(0.158f, 2)));
                int index = (int)(value / 5.0f * (adjs.Count-1));
                Vector2 next = adjs[index];

                bool nearOcean = false;
                foreach (Vector2 adj in getAdjacents(origin)) {
                    if (mapIds[(int)adj.x][(int)adj.y].landType == Tile.LandType.Ocean) {
                        nearOcean = true;
                    }
                }

                if (nearOcean == false && (mapIds[(int)next.x][(int)next.y].landType != Tile.LandType.Ocean || mapIds[(int)next.x][(int)next.y].landType != Tile.LandType.Mountain || mapIds[(int)next.x][(int)next.y].river == true || currentRiver.Contains(next) == false)) {
                    origin = next;
                }
                else if ((nearOcean == true || mapIds[(int)next.x][(int)next.y].river == true) && currentRiver.Count <= 5) {
                    foreach (Vector2 pos in currentRiver) {
                        mapIds[(int)pos.x][(int)pos.y].river = true;
                    }
                    foreach (Vector2 forest in getPosInRange(origin, 3)) {
                        if (mapIds[(int)forest.x][(int)forest.y].landType == Tile.LandType.Forest) {
                            mapIds[(int)forest.x][(int)forest.y].landType = Tile.LandType.Jungle;
                        }
                    }
                    currentRiver.Clear();
                    riverCount++;
                    continueRiver = false;
                }
                else {
                    currentRiver.Clear();
                    continueRiver = false;
                }
            }
        }

        // North and South pole generation
        for (int x = 0; x < length; x++) {
            mapIds[x][0].landType = Tile.LandType.Flatland;
            mapIds[x][0].tempType = Tile.TempType.Arctic;
            mapIds[x][(int)width-1].landType = Tile.LandType.Flatland;
            mapIds[x][(int)width-1].tempType = Tile.TempType.Arctic;
        }
        for (int i = 0; i < width*1.33f; i++) {
            if (Random.Range(0.0f, 1.0f) >= 0.5f) {
                if (Random.Range(0.0f, 1.0f) >= 0.5f) {
                    int x = (int)Random.Range(0, length);
                    mapIds[x][(int)width - 3].landType = Tile.LandType.Flatland;
                    mapIds[x][(int)width - 3].tempType = Tile.TempType.Arctic;
                }
                else {
                    int x = (int)Random.Range(0, length);
                    mapIds[x][(int)width - 2].landType = Tile.LandType.Flatland;
                    mapIds[x][(int)width - 2].tempType = Tile.TempType.Arctic;
                }
            }
            else {
                if (Random.Range(0.0f, 1.0f) >= 0.5f) {
                    int x = (int)Random.Range(0, length);
                    mapIds[x][2].landType = Tile.LandType.Flatland;
                    mapIds[x][2].tempType = Tile.TempType.Arctic;
                }
                else {
                    int x = (int)Random.Range(0, length);
                    mapIds[x][1].landType = Tile.LandType.Flatland;
                    mapIds[x][1].tempType = Tile.TempType.Arctic;
                }
            }
        }

        // Height fixing and determining
        for (int x = 0; x < mapIds.Count; x++) {
            for (int y = 0; y < mapIds[x].Count; y++) {
                Tile.TileId bob = mapIds[x][y];
                if (bob.landType == Tile.LandType.Ocean || bob.landType == Tile.LandType.Swamp) {
                    bob.landHeight = 0;
                }
                else if (bob.landType == Tile.LandType.Flatland || bob.landType == Tile.LandType.Forest || bob.landType == Tile.LandType.Jungle) {
                    bob.landHeight = 0.5f;
                }
                else if (bob.landType == Tile.LandType.Hill || bob.landType == Tile.LandType.ForestHill) {
                    bob.landHeight = 1.0f;
                }
                else if (bob.landType == Tile.LandType.Mountain) {
                    bob.landHeight = 2.5f;
                }
            }
        }
    }

    void tileCreation() {
        mapTiles.Clear();
        for (int x = 0; x < length; x++) {
            mapTiles.Add(new List<Tile>());
            for (int y = 0; y < width; y++) {
                Vector3 pos = new Vector3(x * 1.5f, mapIds[x][y].landHeight*0.25f, y * 1.734f);
                if (x % 2 == 1) {
                    // Odd
                    pos.z += 0.867f;
                }
                Tile tile = ((GameObject)Instantiate(baseTile, pos, transform.rotation, transform)).GetComponent<Tile>();
                tile.landType = mapIds[x][y].landType;
                tile.tempType = mapIds[x][y].tempType;
                tile.river = mapIds[x][y].river;
                if (tile.landType == Tile.LandType.Ocean) {
                    tile.init(x, y, new Color(0, 0.47f, 0.75f));
                }
                else if (tile.landType == Tile.LandType.Flatland) {
                    if (tile.tempType == Tile.TempType.Desert) {
                        tile.init(x, y, new Color(0.87f, 0.78f, 0.25f));
                    }
                    else if (tile.tempType == Tile.TempType.Tundra) {
                        tile.init(x, y, new Color(0.86f, 0.86f, 0.86f));
                    }
                    else if (tile.tempType == Tile.TempType.Arctic) {
                        tile.init(x, y, new Color(0.73f, 0.95f, 0.94f));
                    }
                    else { 
                        tile.init(x, y, new Color(0.49f, 0.99f, 0));
                    }
                }
                else if (tile.landType == Tile.LandType.Forest) {
                    tile.init(x, y, new Color(0.08f, 0.2f, 0.02f));
                }
                else if (tile.landType == Tile.LandType.Hill) {
                    tile.init(x, y, new Color(0.55f, 0.27f, 0.07f));
                }
                else if (tile.landType == Tile.LandType.ForestHill) {
                    tile.init(x, y, new Color(0.4f, 0.39f, 0));
                }
                else if (tile.landType == Tile.LandType.Jungle) {
                    tile.init(x, y, new Color(0, 0.06f, 0.02f));
                }
                else if (tile.landType == Tile.LandType.Swamp) {
                    tile.init(x, y, new Color(0.25f, 0.41f, 0.15f));
                }
                else if (tile.landType == Tile.LandType.Mountain) {
                    tile.init(x, y, new Color(0.53f, 0.49f, 0.44f));
                }

                if (tile.river == true) {
                    //tile.GetComponent<Renderer>().material.color = new Color(0,0,1);
                }

                mapTiles[x].Add(tile);
            }
        }
    }

    // Calculates a score for each tile which works out which would be the best place to have a city
    public List<Tile> getSpawnTiles(int numPlayers) {
        List<Tile> locations = new List<Tile>();
        List<Tile> map = new List<Tile>();
        for (int x = 0; x < length; x++) {
            map.AddRange(mapTiles[x]);
        }

        float max = Mathf.NegativeInfinity;
        for (int x = 0; x < length; x++) {
            for (int y = 0; y < width; y++) {

                bool ocean = false;
                int flatCount = 0;
                List<Tile> range = getTilesInRange(mapTiles[x][y].tilePos, 2);
                for (int adj = 0; adj < range.Count; adj++) {
                    Tile tile = range[adj];
                    if (adj <= 6 && tile.river == true) {
                        mapTiles[x][y].startScore += 25;
                    }
                    // Scoring for land types
                    switch (tile.landType) {
                        case Tile.LandType.Ocean:
                            if (adj <= 6 || tile.river == true) {
                                ocean = true;
                            }
                            mapTiles[x][y].startScore += 0;
                            break;
                        case Tile.LandType.Swamp:
                            mapTiles[x][y].startScore += 1;
                            break;
                        case Tile.LandType.Jungle:
                            mapTiles[x][y].startScore += 2;
                            break;
                        case Tile.LandType.Mountain:
                            mapTiles[x][y].startScore += 4;
                            break;
                        case Tile.LandType.Flatland:
                            mapTiles[x][y].startScore += 5;
                            flatCount++;
                            break;
                        case Tile.LandType.Hill:
                            mapTiles[x][y].startScore += 6;
                            break;
                        case Tile.LandType.Forest:
                            mapTiles[x][y].startScore += 6;
                            break;
                        case Tile.LandType.ForestHill:
                            mapTiles[x][y].startScore += 7;
                            break;
                    }
                    // Scoring for temperatures
                    switch (tile.tempType) {
                        case Tile.TempType.Arctic:
                            mapTiles[x][y].startScore += 0;
                            break;
                        case Tile.TempType.Tundra:
                            mapTiles[x][y].startScore += 1;
                            break;
                        case Tile.TempType.Desert:
                            mapTiles[x][y].startScore += 2;
                            break;
                        case Tile.TempType.Plains:
                            mapTiles[x][y].startScore += 5;
                            break;
                        case Tile.TempType.Grassland:
                            mapTiles[x][y].startScore += 6;
                            break;
                    }
                }
                // More score if next to ocean
                if (ocean == true) {
                    mapTiles[x][y].startScore += 100;
                }
                // More score if near lots of flat land
                if (flatCount >= 4) {
                    mapTiles[x][y].startScore += 100;
                }
                // Invalid if tile is ocean, mountain or arctic
                if (mapTiles[x][y].landType == Tile.LandType.Ocean || mapTiles[x][y].landType == Tile.LandType.Mountain || mapTiles[x][y].tempType == Tile.TempType.Arctic) {
                    mapTiles[x][y].startScore *= 0;
                }
                if (mapTiles[x][y].startScore > max) {
                    max = mapTiles[x][y].startScore;
                }
            }

        }

        for (int p = 0; p < numPlayers; p++) {
            map.Sort(sortByStartScore);
            locations.Add(map[map.Count - 1]);
            map[map.Count - 1].GetComponent<Renderer>().material.color = new Color(1, 0, 0);

            // If tile is close to another start point make it invalid
            for (int l = 0; l < locations.Count; l++) {
                foreach (Tile tile in getTilesInRange(locations[l].tilePos, 10)) {
                    tile.startScore *= 0;
                }
            }
        }

        return locations;
    }

    void determineAdjs() {
        for (int x = 0; x < length; x++) {
            for (int y = 0; y < width; y++) {
                int odd = 0;
                if (x % 2 == 1) {
                    odd = 1;
                }
                if (pointInBounds(x - 1, y + odd)) {
                    mapTiles[x][y].addAdj(mapTiles[x - 1][y + odd]);
                }
                else if (pointInBounds((int)(x - 1 + length), y + odd)) {
                    mapTiles[x][y].addAdj(mapTiles[(int)(x - 1 + length)][y + odd]);
                }
                if (pointInBounds(x - 1, y - 1 + odd)) {
                    mapTiles[x][y].addAdj(mapTiles[x - 1][y - 1 + odd]);
                }
                else if (pointInBounds((int)(x - 1 + length), y - 1 + odd)) {
                    mapTiles[x][y].addAdj(mapTiles[(int)(x - 1 + length)][y - 1 + odd]);
                }
                if (pointInBounds(x, y + 1)) {
                    mapTiles[x][y].addAdj(mapTiles[x][y + 1]);
                }
                if (pointInBounds(x, y - 1)) {
                    mapTiles[x][y].addAdj(mapTiles[x][y - 1]);
                }
                if (pointInBounds(x + 1, y + odd)) {
                    mapTiles[x][y].addAdj(mapTiles[x + 1][y + odd]);
                }
                else if (pointInBounds((int)(x + 1 - length), y + odd)) {
                    mapTiles[x][y].addAdj(mapTiles[(int)(x + 1 - length)][y + odd]);
                }
                if (pointInBounds(x + 1, y - 1 + odd)) {
                    mapTiles[x][y].addAdj(mapTiles[x + 1][y - 1 + odd]);
                }
                else if (pointInBounds((int)(x + 1 - length), y - 1 + odd)) {
                    mapTiles[x][y].addAdj(mapTiles[(int)(x + 1 - length)][y - 1 + odd]);
                }
            }
        }
    }

    List<Vector2> getAdjacents(Vector2 origin) {
        int x = (int)origin.x;
        int y = (int)origin.y;
        List<Vector2> adjs = new List<Vector2>();

        int odd = 0;
        if (x % 2 == 1) {
            odd = 1;
        }
        if (pointInBounds(x - 1, y + odd)) {
            adjs.Add(new Vector2(x - 1, y + odd));
        }
        else if (pointInBounds((int)(x - 1 + length), y + odd)) {
            adjs.Add(new Vector2((int)(x - 1 + length), y + odd));
        }
        if (pointInBounds(x - 1, y - 1 + odd)) {
            adjs.Add(new Vector2(x - 1, y - 1 + odd));
        }
        else if (pointInBounds((int)(x - 1 + length), y - 1 + odd)) {
            adjs.Add(new Vector2((int)(x - 1 + length), y - 1 + odd));
        }
        if (pointInBounds(x, y + 1)) {
            adjs.Add(new Vector2(x, y + 1));
        }
        if (pointInBounds(x, y - 1)) {
            adjs.Add(new Vector2(x, y - 1));
        }
        if (pointInBounds(x + 1, y + odd)) {
            adjs.Add(new Vector2(x + 1, y + odd));
        }
        else if (pointInBounds((int)(x + 1 - length), y + odd)) {
            adjs.Add(new Vector2((int)(x + 1 - length), y + odd));
        }
        if (pointInBounds(x + 1, y - 1 + odd)) {
            adjs.Add(new Vector2(x + 1, y - 1 + odd));
        }
        else if (pointInBounds((int)(x + 1 - length), y - 1 + odd)) {
            adjs.Add(new Vector2((int)(x + 1 - length), y - 1 + odd));
        }

        return adjs;
    }

    public List<Tile> getTilesInRange(Vector2 pos, int range) {
        List<Tile> tiles = new List<Tile>();
        tiles.Add(getTile((int)pos.x, (int)pos.y));
        int index = 0;
        int nextIndex = 0;

        for (int loop = 0; loop < range; loop++) {
            index = nextIndex;
            nextIndex = tiles.Count;
            for (int tile = index; tile < nextIndex; tile++) {
                List<Tile> adjs = tiles[tile].adjacents;
                foreach (Tile adj in adjs) {
                    if (tiles.Contains(adj) == false) {
                        tiles.Add(adj);
                    }
                }
            }
        }

        return tiles;
    }

    public List<Vector2> getPosInRange(Vector2 pos, int range) {
        List<Vector2> list = new List<Vector2>();
        list.Add(pos);
        int index = 0;
        int nextIndex = 0;

        for (int loop = 0; loop < range; loop++) {
            index = nextIndex;
            nextIndex = list.Count;
            for (int tile = index; tile < nextIndex; tile++) {
                List<Vector2> adjs = getAdjacents(list[tile]);
                foreach (Vector2 adj in adjs) {
                    if (list.Contains(adj) == false) {
                        list.Add(adj);
                    }
                }
            }
        }

        return list;
    }

    public Tile getTile(int x, int y) {
        return mapTiles[x][y];
    }

    public int distanceBetween(Vector2 origin, Vector2 target) {
        Vector3 a = new Vector3();
        a.x = origin.x;
        a.z = origin.y - (origin.x + ((int)origin.x & 1)) / 2.0f;
        a.y = -a.x - a.z;

        Vector3 b = new Vector3();
        a.x = target.x;
        a.z = target.y - (target.x + ((int)target.x & 1)) / 2.0f;
        a.y = -a.x - a.z;

        return (int)((Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y) + Mathf.Abs(a.z - b.z)) / 2.0f);
    }

    public bool pointInBounds(int x, int y) {
        if (x >= 0 && x < length) {
            if (y >= 0 && y < width) {
                return true;
            }
        }
        return false;
    }

    public bool pointInBounds(Vector2 point) {
        return pointInBounds((int)point.x, (int)point.y);
    }

    public Vector2 convertPosIndex(Vector3 pos) {
        int x = Mathf.RoundToInt(pos.x/1.5f);
        int y = 0;
        if (x % 2 == 1) {
            // Odd
            y = Mathf.RoundToInt((pos.z-0.867f)/0.867f / 2.0f);
        }
        else {
            y = Mathf.RoundToInt(pos.z / 0.867f / 2.0f);
        }

        return new Vector2(x, y);
    }

    public Vector3 convertIndexPos(Vector2 index) {
        float x = index.x*1.5f;
        float z = index.y*1.734f;
        if (index.x % 2 == 1) {
            // Odd
            z += 0.867f;
        }        

        return new Vector3(x, 0, z);
    }
}
