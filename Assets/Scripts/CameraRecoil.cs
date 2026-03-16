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

    [Header("FOV Kick")]
    public Camera recoilCamera;
    public float fovKickReturnSpeed = 8f;
    public float fovKickSnappiness = 14f;
    public float maxFovKick = 8f;

    [Header("Screen Shake")]
    public float shakeSpeed = 18f;
    Vector3 currentShake;
    Vector3 targetShake;
    float shakeTimer;
    float shakeDuration;

    Vector3 currentRecoil;
    Vector3 targetRecoil;
    float baseFov;
    float currentFovKick;
    float targetFovKick;

    void Awake()
    {
        if (recoilCamera == null && cameraTransform != null)
        {
            recoilCamera = cameraTransform.GetComponent<Camera>();
        }

        if (recoilCamera != null)
        {
            baseFov = recoilCamera.fieldOfView;
        }
    }

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

        targetFovKick = Mathf.Lerp(
            targetFovKick,
            0f,
            fovKickReturnSpeed * Time.deltaTime
        );
        currentFovKick = Mathf.Lerp(
            currentFovKick,
            targetFovKick,
            fovKickSnappiness * Time.deltaTime
        );

        // Update screen shake
        if (shakeDuration > 0f)
        {
            shakeTimer -= Time.deltaTime;
            if (shakeTimer <= 0f)
            {
                shakeTimer = 0.016f;
                targetShake = new Vector3(
                    Random.Range(-1f, 1f),
                    Random.Range(-1f, 1f),
                    0f
                ) * (shakeDuration / Mathf.Max(0.01f, shakeDuration));
            }
            shakeDuration = Mathf.Max(0f, shakeDuration - Time.deltaTime);

            if (shakeDuration <= 0f)
                targetShake = Vector3.zero;
        }

        currentShake = Vector3.Lerp(
            currentShake,
            targetShake,
            shakeSpeed * Time.deltaTime
        );

        if (recoilCamera != null)
        {
            recoilCamera.fieldOfView = baseFov + currentFovKick;
        }

        ApplyRotation();
    }

    public void AddRecoil(Vector3 recoil)
    {
        targetRecoil += recoil;
    }

    public void AddFovKick(float kickAmount)
    {
        float safeKick = Mathf.Max(0f, kickAmount);
        targetFovKick = Mathf.Clamp(targetFovKick + safeKick, 0f, Mathf.Max(0f, maxFovKick));
    }

    public void AddScreenShake(float amount, float duration = 0.1f)
    {
        shakeDuration = Mathf.Max(shakeDuration, duration);
        targetShake = new Vector3(
            Random.Range(-1f, 1f),
            Random.Range(-1f, 1f),
            0f
        ) * amount;
    }

    void ApplyRotation()
    {
        float finalPitch = playerLook.Pitch + currentRecoil.y + currentShake.y;
        float finalYaw = currentRecoil.x + currentShake.x;

        cameraTransform.localRotation = Quaternion.Euler(
            finalPitch,
            finalYaw,
            0f
        );
    }
}
