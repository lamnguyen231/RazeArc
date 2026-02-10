using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraRecoil : MonoBehaviour
{
    [Header("References")]
    public Transform cameraTransform;
    public PlayerLook playerLook;

    [Header("Recoil Settings")]
    public float returnSpeed = 10f;
    public float snappiness = 20f;

    Vector3 currentRecoil;
    Vector3 targetRecoil;

    void Update()
    {
        // Smoothly return recoil
        targetRecoil = Vector3.Lerp(
            targetRecoil,
            Vector3.zero,
            returnSpeed * Time.deltaTime
        );

        currentRecoil = Vector3.Lerp(
            currentRecoil,
            targetRecoil,
            snappiness * Time.deltaTime
        );

        ApplyRotation();
    }

    public void AddRecoil(Vector3 recoil)
    {
        targetRecoil += recoil;
    }

    void ApplyRotation()
    {
        float finalPitch = playerLook.Pitch + currentRecoil.y;
        float finalYaw = currentRecoil.x;

        cameraTransform.localRotation = Quaternion.Euler(
            finalPitch,
            finalYaw,
            0f
        );
    }
}
