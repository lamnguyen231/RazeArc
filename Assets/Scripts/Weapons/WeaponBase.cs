using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class WeaponBase : MonoBehaviour
{
    [Header("Weapon Behavior")]
    public bool usesAmmo = true;

    [Header("Weapon Stats")]
    public float damage = 25;
    public float fireRate = 0.5f;
    public float range = 100f;
    public bool isAutomatic = false;

    [Header("Ammo Settings")]
    public AmmoType ammoType = AmmoType.Pistol;
    public int magazineSize = 12;
    public int reserveAmmo = 36;
    public float reloadTime = 1.5f;

    [Header("Recoil Settings")]
    public Vector3 recoilMin = new Vector3(-0.5f, -15f, 0f);
    public Vector3 recoilMax = new Vector3(0.5f, -20f, 0f);

    [Header("Motion Type")]
    public WeaponMotionType motionType = WeaponMotionType.Gun;

    [Header("Gun Kickback")]
    public float kickbackAmount = 0.15f;
    public float kickbackRecoverySpeed = 8f;
    public Transform recoilPivot;
    public float recoilKickAngle = 3.5f;
    public float maxRecoilAngle = 12f;
    public float recoilAngleRecoverySpeed = 16f;

    [Header("Tracer Settings")]
    public bool useTracer = true;
    public int tracerEveryNthShot = 1;
    public float tracerDuration = 0.08f;
    public float tracerWidth = 0.07f;
    public float tracerSpeed = 900f;
    public Color tracerStartColor = new Color(1f, 0.95f, 0.7f, 1f);
    public Color tracerEndColor = new Color(1f, 0.6f, 0.2f, 0.35f);
    [Range(0f, 1f)]
    public float tracerAimRayBlend = 0.65f;
    public float tracerMaxAimRayOffset = 0.2f;

    [Header("Muzzle Flash Settings")]
    public bool useMuzzleFlash = true;
    public int muzzleFlashParticleCount = 12;
    public float muzzleFlashDuration = 0.05f;
    public float muzzleFlashSize = 0.18f;
    public float muzzleFlashSpeed = 8f;
    public Color muzzleFlashColor = new Color(1f, 0.78f, 0.45f, 1f);
    public float muzzleFlashLightIntensity = 7f;
    public float muzzleFlashLightRange = 4f;
    public float muzzleFlashLightDuration = 0.09f;
    Vector3 originalLocalPosition;
    Quaternion originalRecoilPivotLocalRotation;
    float currentRecoilAngle;
    int tracerShotCounter;
    Material runtimeMuzzleFlashMaterial;

    [Header("Melee Swing")]
    public float swingAngle = 60f;
    public float swingSpeed = 12f;
    public Vector3 meleeWindupEuler = new Vector3(0f, 0f, 25f);
    public Vector3 meleeSwingEuler = new Vector3(0f, 0f, -85f);
    public float meleeWindupTime = 0.05f;
    public float meleeAttackTime = 0.06f;
    public float meleeRecoverTime = 0.14f;
    public float meleeLungeDistance = 0.18f;
    Quaternion originalLocalRotation;
    bool isSwinging = false;

    [Header("References")]
    public Transform firePoint;
    public CameraRecoil cameraRecoil;

    [Header("Gun Muzzle Compensation")]
    public Vector3 muzzleLocalOffset = new Vector3(0f, 0f, 0.2f);

    protected float lastShotTime = -999f;
    protected int currentAmmo;
    protected bool isReloading = false;

    protected bool reloadCancelledThisFrame = false;
    protected Coroutine reloadCoroutine;
    protected PlayerInventory playerInventory;
    bool ammoPoolInitialized;

    protected virtual void Awake()
    {
    }

    protected virtual void OnDestroy()
    {
        if (runtimeMuzzleFlashMaterial != null)
        {
            Destroy(runtimeMuzzleFlashMaterial);
            runtimeMuzzleFlashMaterial = null;
        }
    }

    protected virtual void Start()
    {
        playerInventory = GetComponentInParent<PlayerInventory>();
        if (playerInventory == null)
        {
            // Weapons can live under a different branch than PlayerBody in this project.
            playerInventory = FindObjectOfType<PlayerInventory>();
        }

        if (usesAmmo)
        {
            currentAmmo = magazineSize;
            InitializeAmmoPool();
        }

        if (recoilPivot == null)
        {
            recoilPivot = transform;
        }

        originalLocalPosition = transform.localPosition;
        originalLocalRotation = transform.localRotation;
        originalRecoilPivotLocalRotation = recoilPivot.localRotation;
    }

    void Update()
    {
        HandleInput();

        if (motionType == WeaponMotionType.Gun)
        {
            transform.localPosition = Vector3.Lerp(
                transform.localPosition,
                originalLocalPosition,
                kickbackRecoverySpeed * Time.deltaTime
            );

            currentRecoilAngle = Mathf.MoveTowards(
                currentRecoilAngle,
                0f,
                recoilAngleRecoverySpeed * Time.deltaTime
            );

            Quaternion targetRecoilRotation =
                originalRecoilPivotLocalRotation *
                Quaternion.Euler(-currentRecoilAngle, 0f, 0f);

            recoilPivot.localRotation = Quaternion.Slerp(
                recoilPivot.localRotation,
                targetRecoilRotation,
                recoilAngleRecoverySpeed * Time.deltaTime
            );
        }
    }

    protected virtual void HandleInput()
    {
        if (usesAmmo && Input.GetKeyDown(KeyCode.R) && !isReloading)
        {
            reloadCoroutine = StartCoroutine(Reload());
            return;
        }

        if (isAutomatic)
        {
            if (Input.GetMouseButton(0))
            {
                TryFire();
            }
        }
        else
        {
            if (Input.GetMouseButtonDown(0))
            {
                TryFire();
            }
        }
    }

    protected void TryFire()
    {
        if (isReloading)
        {
            CancelReload();
        }

        if (usesAmmo && currentAmmo <= 0)
        {
            if (GetReserveAmmoCount() > 0 && !reloadCancelledThisFrame)
            {
                reloadCoroutine = StartCoroutine(Reload());
            }
            else
            {
                Debug.Log("Click! (Dry Fire)");
            }

            reloadCancelledThisFrame = false;
            return;
        }

        if (Time.time >= lastShotTime + fireRate)
        {
            lastShotTime = Time.time;
            if (usesAmmo)
            {
                currentAmmo--;
            }

            // Weapon attack animation
            if (motionType == WeaponMotionType.Gun)
            {
                if (cameraRecoil != null)
                {
                    Vector3 recoil = new Vector3(
                        Random.Range(recoilMin.x, recoilMax.x),
                        Random.Range(recoilMin.y, recoilMax.y),
                        0f
                    );

                    cameraRecoil.AddRecoil(recoil);
                }

                transform.localPosition -= Vector3.forward * kickbackAmount;
                currentRecoilAngle = Mathf.Clamp(
                    currentRecoilAngle + recoilKickAngle,
                    0f,
                    maxRecoilAngle
                );

                SpawnMuzzleFlash();
            }
            else if (motionType == WeaponMotionType.Melee)
            {
                if (!isSwinging)
                {
                    StartCoroutine(PerformSwing());
                }
            }


            Fire();
        }
    }

    protected IEnumerator Reload()
    {
        if (!usesAmmo)
        {
            yield break; // This weapon doesn't use ammo
        }

        if (currentAmmo == magazineSize)
        {
            yield break; // Magazine is already full
        }

        if (GetReserveAmmoCount() <= 0)
        {
            yield break; // No reserve ammo to reload
        }

        isReloading = true;
        Debug.Log("Reloading...");

        yield return new WaitForSeconds(reloadTime);

        int ammoNeeded = magazineSize - currentAmmo;
        int ammoToReload = ConsumeReserveAmmo(ammoNeeded);

        currentAmmo += ammoToReload;

        isReloading = false;
        Debug.Log("Reloaded. Ammo: " + currentAmmo + "/" + GetReserveAmmoCount());
    }

    protected int GetReserveAmmoCount()
    {
        if (!usesAmmo)
        {
            return 0;
        }

        if (playerInventory != null)
        {
            return playerInventory.GetReserveAmmo(ammoType);
        }

        return Mathf.Max(0, reserveAmmo);
    }

    int ConsumeReserveAmmo(int amount)
    {
        int requested = Mathf.Max(0, amount);
        if (requested <= 0)
        {
            return 0;
        }

        if (playerInventory != null)
        {
            return playerInventory.ConsumeReserveAmmo(ammoType, requested);
        }

        int consumed = Mathf.Min(requested, Mathf.Max(0, reserveAmmo));
        reserveAmmo -= consumed;
        return consumed;
    }

    void InitializeAmmoPool()
    {
        if (ammoPoolInitialized)
        {
            return;
        }

        if (playerInventory != null)
        {
            playerInventory.EnsureAmmoPool(ammoType, reserveAmmo);
        }

        ammoPoolInitialized = true;
    }

    protected void CancelReload()
    {
        if (reloadCoroutine != null)
        {
            StopCoroutine(reloadCoroutine);
            reloadCoroutine = null;
        }

        isReloading = false;
        reloadCancelledThisFrame = true;

        Debug.Log("Reload Cancelled");
    }

    protected void SpawnTracer(Vector3 startPoint, Vector3 endPoint)
    {
        if (!useTracer)
        {
            return;
        }

        tracerShotCounter++;
        int tracerInterval = Mathf.Max(1, tracerEveryNthShot);
        if (tracerShotCounter % tracerInterval != 0)
        {
            return;
        }

        StartCoroutine(PlayTracer(startPoint, endPoint));
    }

    protected Vector3 GetGunMuzzlePosition()
    {
        Transform source = firePoint != null ? firePoint : transform;
        return source.TransformPoint(muzzleLocalOffset);
    }

    protected Vector3 GetTracerStartPosition(Ray aimRay)
    {
        Vector3 muzzlePosition = GetGunMuzzlePosition();
        float depthOnAimRay = Mathf.Max(
            0f,
            Vector3.Dot(muzzlePosition - aimRay.origin, aimRay.direction)
        );
        Vector3 projectedPoint =
            aimRay.origin + (aimRay.direction * depthOnAimRay);

        float maxOffset = Mathf.Max(0f, tracerMaxAimRayOffset);
        Vector3 alignmentOffset = projectedPoint - muzzlePosition;
        if (maxOffset > 0f && alignmentOffset.magnitude > maxOffset)
        {
            projectedPoint =
                muzzlePosition + alignmentOffset.normalized * maxOffset;
        }

        return Vector3.Lerp(
            muzzlePosition,
            projectedPoint,
            Mathf.Clamp01(tracerAimRayBlend)
        );
    }

    void SpawnMuzzleFlash()
    {
        if (!useMuzzleFlash)
        {
            return;
        }

        Transform source = firePoint != null ? firePoint : transform;
        Vector3 muzzlePosition = GetGunMuzzlePosition();

        GameObject flashObject = new GameObject("MuzzleFlash");
        flashObject.transform.position = muzzlePosition;
        flashObject.transform.rotation = Quaternion.LookRotation(source.forward, source.up);

        ParticleSystem flashParticles = flashObject.AddComponent<ParticleSystem>();
        flashParticles.Stop(
            true,
            ParticleSystemStopBehavior.StopEmittingAndClear
        );
        var main = flashParticles.main;
        main.playOnAwake = false;
        main.duration = muzzleFlashDuration;
        main.startLifetime = new ParticleSystem.MinMaxCurve(
            muzzleFlashDuration * 0.5f,
            muzzleFlashDuration
        );
        main.startSpeed = new ParticleSystem.MinMaxCurve(
            muzzleFlashSpeed * 0.7f,
            muzzleFlashSpeed
        );
        main.startSize = new ParticleSystem.MinMaxCurve(
            muzzleFlashSize * 0.65f,
            muzzleFlashSize
        );
        main.startColor = muzzleFlashColor;
        main.maxParticles = Mathf.Max(1, muzzleFlashParticleCount + 4);
        main.loop = false;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;

        var emission = flashParticles.emission;
        emission.enabled = false;

        var shape = flashParticles.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 18f;
        shape.radius = 0.01f;
        shape.length = 0.06f;

        ParticleSystemRenderer particleRenderer =
            flashObject.GetComponent<ParticleSystemRenderer>();
        particleRenderer.renderMode = ParticleSystemRenderMode.Billboard;
        particleRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        particleRenderer.receiveShadows = false;

        Material muzzleFlashMaterial = GetOrCreateMuzzleFlashMaterial();
        if (muzzleFlashMaterial != null)
        {
            particleRenderer.material = muzzleFlashMaterial;
            if (muzzleFlashMaterial.HasProperty("_BaseColor"))
            {
                muzzleFlashMaterial.SetColor("_BaseColor", muzzleFlashColor);
            }
            if (muzzleFlashMaterial.HasProperty("_Color"))
            {
                muzzleFlashMaterial.SetColor("_Color", muzzleFlashColor);
            }
        }

        if (muzzleFlashLightIntensity > 0f)
        {
            Light flashLight = flashObject.AddComponent<Light>();
            flashLight.type = LightType.Point;
            flashLight.color = muzzleFlashColor;
            flashLight.intensity = muzzleFlashLightIntensity;
            flashLight.range = muzzleFlashLightRange;
            flashLight.shadows = LightShadows.None;
            flashLight.renderMode = LightRenderMode.ForcePixel;
            flashLight.cullingMask = ~0;
            flashLight.enabled = true;

            StartCoroutine(FadeMuzzleFlashLight(flashLight, muzzleFlashLightDuration));
        }

        flashParticles.Emit(Mathf.Max(1, muzzleFlashParticleCount));
        flashParticles.Play();

        float lifetime = Mathf.Max(muzzleFlashDuration, muzzleFlashLightDuration);
        Destroy(flashObject, lifetime + 0.08f);
    }

    IEnumerator FadeMuzzleFlashLight(Light flashLight, float duration)
    {
        if (flashLight == null)
        {
            yield break;
        }

        float startIntensity = flashLight.intensity;
        float elapsed = 0f;
        float fadeDuration = Mathf.Max(0.01f, duration);

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / fadeDuration;
            flashLight.intensity = Mathf.Lerp(startIntensity, 0f, progress);
            yield return null;
        }

        flashLight.intensity = 0f;
    }

    Material GetOrCreateMuzzleFlashMaterial()
    {
        if (runtimeMuzzleFlashMaterial != null)
        {
            return runtimeMuzzleFlashMaterial;
        }

        string[] shaderCandidates =
        {
            "Universal Render Pipeline/Particles/Unlit",
            "Particles/Standard Unlit",
            "Legacy Shaders/Particles/Additive",
            "Sprites/Default",
        };

        Shader chosenShader = null;
        for (int i = 0; i < shaderCandidates.Length; i++)
        {
            Shader candidate = Shader.Find(shaderCandidates[i]);
            if (candidate != null)
            {
                chosenShader = candidate;
                break;
            }
        }

        if (chosenShader == null)
        {
            return null;
        }

        runtimeMuzzleFlashMaterial = new Material(chosenShader);
        runtimeMuzzleFlashMaterial.name = "RuntimeMuzzleFlashMaterial";
        runtimeMuzzleFlashMaterial.enableInstancing = true;

        if (runtimeMuzzleFlashMaterial.HasProperty("_Surface"))
        {
            runtimeMuzzleFlashMaterial.SetFloat("_Surface", 1f);
        }
        if (runtimeMuzzleFlashMaterial.HasProperty("_Blend"))
        {
            runtimeMuzzleFlashMaterial.SetFloat("_Blend", 0f);
        }

        return runtimeMuzzleFlashMaterial;
    }

    IEnumerator PlayTracer(Vector3 startPoint, Vector3 endPoint)
    {
        GameObject tracerObject = new GameObject("BulletTracer");
        tracerObject.transform.position = startPoint;

        TrailRenderer trail = tracerObject.AddComponent<TrailRenderer>();
        trail.time = tracerDuration;
        trail.startWidth = tracerWidth;
        trail.endWidth = 0f;
        trail.minVertexDistance = 0.01f;
        trail.numCapVertices = 2;
        trail.numCornerVertices = 2;
        trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        trail.receiveShadows = false;
        trail.alignment = LineAlignment.View;
        trail.emitting = true;

        Gradient tracerGradient = new Gradient();
        tracerGradient.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(tracerStartColor, 0f),
                new GradientColorKey(tracerStartColor, 0.35f),
                new GradientColorKey(tracerEndColor, 1f),
            },
            new GradientAlphaKey[]
            {
                new GradientAlphaKey(tracerStartColor.a, 0f),
                new GradientAlphaKey(tracerStartColor.a, 0.35f),
                new GradientAlphaKey(tracerEndColor.a, 1f),
            }
        );
        trail.colorGradient = tracerGradient;

        Shader tracerShader = Shader.Find("Unlit/Color");
        if (tracerShader == null)
        {
            tracerShader = Shader.Find("Sprites/Default");
        }
        if (tracerShader == null)
        {
            tracerShader = Shader.Find("Legacy Shaders/Particles/Additive");
        }
        if (tracerShader != null)
        {
            Material tracerMaterial = new Material(tracerShader);
            tracerMaterial.color = tracerStartColor;
            trail.material = tracerMaterial;
        }

        yield return null;

        float elapsed = 0f;
        float speed = Mathf.Max(1f, tracerSpeed);
        float travelDuration = Mathf.Max(0.005f, Vector3.Distance(startPoint, endPoint) / speed);

        while (elapsed < travelDuration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / travelDuration;
            tracerObject.transform.position = Vector3.Lerp(startPoint, endPoint, progress);
            yield return null;
        }

        tracerObject.transform.position = endPoint;
        Destroy(tracerObject, tracerDuration);
    }

    IEnumerator PerformSwing()
    {
        isSwinging = true;

        Quaternion startRot = originalLocalRotation;

        Quaternion windupRot =
            originalLocalRotation *
            Quaternion.Euler(meleeWindupEuler);
        Quaternion swingRot =
            originalLocalRotation *
            Quaternion.Euler(meleeSwingEuler);

        float t = 0;

        // WINDUP (raise knife)
        while (t < meleeWindupTime)
        {
            t += Time.deltaTime;
            float progress = t / meleeWindupTime;

            transform.localPosition = originalLocalPosition;
            transform.localRotation =
                Quaternion.Slerp(startRot, windupRot, progress);

            yield return null;
        }

        t = 0;

        // ATTACK (downward slash)
        while (t < meleeAttackTime)
        {
            t += Time.deltaTime;
            float progress = t / meleeAttackTime;

            transform.localPosition =
                originalLocalPosition + Vector3.forward * meleeLungeDistance;

            transform.localRotation =
                Quaternion.Slerp(windupRot, swingRot, progress);

            yield return null;
        }

        t = 0;

        // RECOVERY (slower)
        while (t < meleeRecoverTime)
        {
            t += Time.deltaTime;
            float progress = t / meleeRecoverTime;

            transform.localPosition = originalLocalPosition;

            transform.localRotation =
                Quaternion.Slerp(swingRot, startRot, progress);

            yield return null;
        }

        transform.localRotation = startRot;
        isSwinging = false;
    }

    protected abstract void Fire();
}
