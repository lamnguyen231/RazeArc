using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class WeaponBase : MonoBehaviour
{
    enum ReloadTriggerType
    {
        None,
        Manual,
        AutoEmpty,
    }

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

    [Header("Reload Animation")]
    public bool useReloadAnimation = true;
    public Vector3 reloadTiltEuler = new Vector3(28f, 0f, 0f);
    public Vector3 reloadPositionOffset = new Vector3(0f, -0.05f, -0.08f);
    [Range(0.05f, 0.45f)]
    public float reloadLowerFraction = 0.22f;
    [Range(0.05f, 0.45f)]
    public float reloadRaiseFraction = 0.1f;

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

    [Header("Equip Animation")]
    public bool useEquipAnimation = true;
    public Vector3 equipStartOffset = new Vector3(0f, -0.35f, 0f);
    public float equipRiseDuration = 0.22f;

    [Header("References")]
    public Transform firePoint;
    public CameraRecoil cameraRecoil;

    [Header("Gun Muzzle Compensation")]
    public Vector3 muzzleLocalOffset = new Vector3(0f, 0f, 0.2f);

    protected float lastShotTime = -999f;
    protected int currentAmmo;
    protected bool isReloading = false;

    protected float reloadElapsed = 0f;
    protected bool reloadPausedWhileHolstered = false;
    ReloadTriggerType reloadTriggerType = ReloadTriggerType.None;
    protected PlayerInventory playerInventory;
    bool ammoPoolInitialized;
    bool hasCachedPose;
    bool isEquipping;
    Coroutine equipCoroutine;

    protected virtual void Awake()
    {
        CacheOriginalPose();
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

        CacheOriginalPose();
    }

    protected virtual void OnEnable()
    {
        CacheOriginalPose();

        if (reloadPausedWhileHolstered && reloadTriggerType == ReloadTriggerType.AutoEmpty)
        {
            isReloading = true;
            reloadPausedWhileHolstered = false;
            Debug.Log("Resuming auto reload...");
        }

        if (useEquipAnimation && motionType == WeaponMotionType.Gun)
        {
            if (equipCoroutine != null)
            {
                StopCoroutine(equipCoroutine);
            }

            equipCoroutine = StartCoroutine(PlayEquipAnimation());
        }
        else
        {
            isEquipping = false;
            transform.localPosition = originalLocalPosition;
            transform.localRotation = originalLocalRotation;
            if (recoilPivot != null)
            {
                recoilPivot.localRotation = originalRecoilPivotLocalRotation;
            }
        }
    }

    protected virtual void OnDisable()
    {
        if (equipCoroutine != null)
        {
            StopCoroutine(equipCoroutine);
            equipCoroutine = null;
        }

        isEquipping = false;

        if (isReloading)
        {
            if (reloadTriggerType == ReloadTriggerType.AutoEmpty)
            {
                isReloading = false;
                reloadPausedWhileHolstered = true;
                ResetWeaponPoseAfterReloadStop();
            }
            else
            {
                CancelReload();
            }
        }
    }

    void Update()
    {
        if (usesAmmo && !isReloading && !reloadPausedWhileHolstered && currentAmmo <= 0)
        {
            TryStartReload(ReloadTriggerType.AutoEmpty);
        }

        UpdateReloadState();
        HandleInput();

        if (motionType == WeaponMotionType.Gun && !isEquipping)
        {
            if (!isReloading)
            {
                transform.localPosition = Vector3.Lerp(
                    transform.localPosition,
                    originalLocalPosition,
                    kickbackRecoverySpeed * Time.deltaTime
                );

                transform.localRotation = Quaternion.Slerp(
                    transform.localRotation,
                    originalLocalRotation,
                    kickbackRecoverySpeed * Time.deltaTime
                );
            }

            if (!isReloading)
            {
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
    }

    protected virtual void HandleInput()
    {
        if (isEquipping)
        {
            return;
        }

        if (usesAmmo && Input.GetKeyDown(KeyCode.R))
        {
            TryStartReload(ReloadTriggerType.Manual);
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
            if (reloadTriggerType == ReloadTriggerType.Manual && currentAmmo > 0)
            {
                CancelReload();
            }
            else
            {
                return;
            }
        }

        if (usesAmmo && currentAmmo <= 0)
        {
            TryStartReload(ReloadTriggerType.AutoEmpty);
            Debug.Log("Click! (Dry Fire)");
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

            if (usesAmmo && currentAmmo <= 0)
            {
                TryStartReload(ReloadTriggerType.AutoEmpty);
            }
        }
    }

    bool TryStartReload(ReloadTriggerType triggerType)
    {
        if (!usesAmmo)
        {
            return false;
        }

        if (isReloading)
        {
            return false;
        }

        if (currentAmmo >= magazineSize)
        {
            return false;
        }

        if (GetReserveAmmoCount() <= 0)
        {
            return false;
        }

        reloadElapsed = 0f;
        isReloading = true;
        reloadPausedWhileHolstered = false;
        reloadTriggerType = triggerType;

        if (motionType == WeaponMotionType.Gun && useReloadAnimation)
        {
            currentRecoilAngle = 0f;
            if (recoilPivot != null)
            {
                recoilPivot.localRotation = originalRecoilPivotLocalRotation;
            }
        }

        Debug.Log(triggerType == ReloadTriggerType.AutoEmpty ? "Auto reloading..." : "Reloading...");
        return true;
    }

    void UpdateReloadState()
    {
        if (!isReloading)
        {
            return;
        }

        float reloadDuration = Mathf.Max(0.01f, reloadTime);
        reloadElapsed += Time.deltaTime;

        if (motionType == WeaponMotionType.Gun && useReloadAnimation)
        {
            ApplyReloadAnimationPose(Mathf.Clamp01(reloadElapsed / reloadDuration));
        }

        if (reloadElapsed < reloadDuration)
        {
            return;
        }

        int ammoNeeded = magazineSize - currentAmmo;
        int ammoToReload = ConsumeReserveAmmo(ammoNeeded);
        currentAmmo += ammoToReload;

        ResetReloadState();
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
        if (!isReloading && !reloadPausedWhileHolstered)
        {
            return;
        }

        ResetReloadState();

        Debug.Log("Reload Cancelled");
    }

    void ResetReloadState()
    {
        isReloading = false;
        reloadPausedWhileHolstered = false;
        reloadElapsed = 0f;
        reloadTriggerType = ReloadTriggerType.None;
        ResetWeaponPoseAfterReloadStop();
    }

    void ResetWeaponPoseAfterReloadStop()
    {
        if (motionType == WeaponMotionType.Gun)
        {
            transform.localPosition = originalLocalPosition;
            transform.localRotation = originalLocalRotation;
        }
    }

    void ApplyReloadAnimationPose(float normalizedProgress)
    {
        float progress = Mathf.Clamp01(normalizedProgress);
        float lowerDuration = Mathf.Max(0.01f, reloadTime * reloadLowerFraction);
        float raiseDuration = Mathf.Max(0.01f, reloadTime * reloadRaiseFraction);
        float holdDuration = Mathf.Max(0f, reloadTime - lowerDuration - raiseDuration);
        float lowerEndTime = lowerDuration;
        float holdEndTime = lowerDuration + holdDuration;
        float elapsedTime = progress * Mathf.Max(0.01f, reloadTime);

        Vector3 reloadPosition = originalLocalPosition + reloadPositionOffset;
        Quaternion reloadRotation =
            originalLocalRotation * Quaternion.Euler(reloadTiltEuler);

        if (elapsedTime <= lowerEndTime)
        {
            float lowerT = Mathf.Clamp01(elapsedTime / lowerDuration);
            transform.localPosition = Vector3.Lerp(originalLocalPosition, reloadPosition, lowerT);
            transform.localRotation = Quaternion.Slerp(originalLocalRotation, reloadRotation, lowerT);
            return;
        }

        if (elapsedTime <= holdEndTime)
        {
            transform.localPosition = reloadPosition;
            transform.localRotation = reloadRotation;
            return;
        }

        float raiseTime = elapsedTime - holdEndTime;
        float raiseT = Mathf.Clamp01(raiseTime / raiseDuration);
        float snapProgress = 1f - Mathf.Pow(1f - raiseT, 3f);
        transform.localPosition = Vector3.Lerp(reloadPosition, originalLocalPosition, snapProgress);
        transform.localRotation = Quaternion.Slerp(reloadRotation, originalLocalRotation, snapProgress);
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

    void CacheOriginalPose()
    {
        if (recoilPivot == null)
        {
            recoilPivot = transform;
        }

        if (hasCachedPose)
        {
            return;
        }

        originalLocalPosition = transform.localPosition;
        originalLocalRotation = transform.localRotation;
        originalRecoilPivotLocalRotation = recoilPivot.localRotation;
        hasCachedPose = true;
    }

    IEnumerator PlayEquipAnimation()
    {
        isEquipping = true;

        float duration = Mathf.Max(0.01f, equipRiseDuration);
        Vector3 startPosition = originalLocalPosition + equipStartOffset;

        transform.localPosition = startPosition;
        transform.localRotation = originalLocalRotation;
        if (recoilPivot != null)
        {
            recoilPivot.localRotation = originalRecoilPivotLocalRotation;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float easedT = t * t * (3f - 2f * t);
            transform.localPosition = Vector3.LerpUnclamped(
                startPosition,
                originalLocalPosition,
                easedT
            );
            yield return null;
        }

        transform.localPosition = originalLocalPosition;
        isEquipping = false;
        equipCoroutine = null;
    }

    protected abstract void Fire();
}
