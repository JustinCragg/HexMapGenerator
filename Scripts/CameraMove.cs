using UnityEngine;
using System.Collections;

public class CameraMove : MonoBehaviour {
    public MapManager mapManager;

    float horizontalSpeed = 37.5f;
    float verticalSpeed = 25;

    float momentum = 0;
	
	void Update () {
        Vector3 delta = Vector3.zero;
        if (Input.GetMouseButton(0)) {
            float mouseX = Input.GetAxis("Mouse X") * horizontalSpeed * Time.deltaTime;
            float mouseY = Input.GetAxis("Mouse Y") * verticalSpeed * Time.deltaTime;

            delta = new Vector3(-mouseX, 0, -mouseY);
        }

        momentum *= 0.3f;
        momentum -= Input.mouseScrollDelta.y * 3.0f;
        delta.y += momentum;

        Vector3 pos = transform.position + delta;
        pos.x = Mathf.Clamp(pos.x, 0, mapManager.length * 1.5f);
        pos.y = Mathf.Clamp(pos.y, 5.0f, 30.0f);
        pos.z = Mathf.Clamp(pos.z, -10.0f, mapManager.width * 1.5f - 10.0f);

        transform.position = pos;
    }
}
