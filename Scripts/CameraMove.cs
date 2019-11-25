using UnityEngine;
using System.Collections;

public class CameraMove : MonoBehaviour {
    public MapManager mapManager;

    float horizontalSpeed = 37.5f;
    float verticalSpeed = 25;
	
	void Update () {
        if (Input.GetMouseButton(0)) {
            float mouseX = Input.GetAxis("Mouse X") * horizontalSpeed * Time.deltaTime;
            float mouseY = Input.GetAxis("Mouse Y") * verticalSpeed * Time.deltaTime;

            Vector3 pos = transform.position + new Vector3(-mouseX, 0, -mouseY);
            pos.x = Mathf.Clamp(pos.x, 0, mapManager.length * 1.5f);
            pos.z = Mathf.Clamp(pos.z, -10.0f, mapManager.width * 1.5f - 10.0f);
            transform.position = pos;
        }
	}
}
