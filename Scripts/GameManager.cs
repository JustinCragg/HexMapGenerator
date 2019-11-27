using UnityEngine;
using System.Collections.Generic;

public class GameManager : MonoBehaviour {
    MapManager mapManager;

    int maxPlayers = 8;

	void Start () {
        mapManager = GameObject.FindGameObjectWithTag("MapManager").GetComponent<MapManager>();
        mapManager.generateMap();
	}

    void Update() {
        if (Input.GetKeyUp(KeyCode.Return)) {
            mapManager.generateMap();
        }
    }
}
