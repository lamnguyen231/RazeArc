using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerLook : MonoBehaviour
{
    [Header("References")]
    public Transform cameraTransform;

    [Header("Settings")]
    public float mouseSensitivity = 150f;
    public float maxPitch = 85f;

    float pitch;

    // Update is called once per frame
    void Update()
    {
        HandleLook();
    }

    void HandleLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        pitch -= mouseY;
        pitch = Mathf.Clamp(pitch, -maxPitch, maxPitch);

        transform.Rotate(Vector3.up * mouseX);
        // PlayerLook no longer directly sets camera rotation
    }

    public float Pitch => pitch;
}
