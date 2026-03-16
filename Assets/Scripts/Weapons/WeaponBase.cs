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
    public float kickReturnDuration = 0.12f;
    public float kickReturnExponent = 2.2f;
    public Transform recoilPivot;
    public float recoilKickAngle = 3.5f;
    public float maxRecoilAngle = 12f;
    public float recoilAngleRecoverySpeed = 16f;

    [Header("Camera Kick")]
    public float fovKickAmount = 0.35f;

    [Header("Movement Bob")]
    public bool useMovementBob = true;
    public float bobFrequency = 9f;
    public float bobSmoothing = 14f;
    public Vector3 bobPositionAmplitude = new Vector3(0.012f, 0.016f, 0.01f);
    public Vector3 bobRotationAmplitude = new Vector3(1.4f, 0.7f, 1.6f);
    [Range(0f, 1f)]
    public float aimBobMultiplier = 0.35f;
    [Range(0f, 1f)]
    public float firingBobMultiplier = 0.2f;
    [Range(0f, 1f)]
    public float reloadBobMultiplier = 0.08f;
    public float firingBobSuppressDuration = 0.08f;

    [Header("Reload Animation")]
    public bool useReloadAnimation = true;
    public Vector3 reloadTiltEuler = new Vector3(28f, 0f, 0f);
    public Vector3 reloadPositionOffset = new Vector3(0f, -0.05f, -0.08f);
    [Range(0.05f, 0.45f)]
    public float reloadLowerFraction = 0.22f;
    [Range(0.05f, 0.45f)]
    public float reloadRaiseFraction = 0.1f;

    [Header("Reload Movement Bob")]
    public bool useReloadMovementBob = true;
    public float reloadBobFrequency = 8f;
    public Vector3 reloadBobPositionAmplitude = new Vector3(0.006f, 0.009f, 0.004f);
    public Vector3 reloadBobRotationAmplitude = new Vector3(0.9f, 0.45f, 0.9f);

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

    [Header("Impact Decals")]
    public bool useImpactDecals = true;
    public int maxImpactDecals = 120;
    public float impactDecalMinSize = 0.07f;
    public float impactDecalMaxSize = 0.12f;
    public float impactDecalSurfaceOffset = 0.012f;
    public float impactDecalLifetime = 18f;
    public Color worldImpactDecalColor = new Color(0.45f, 0.45f, 0.45f, 0.9f);
    public Color enemyImpactDecalColor = new Color(0.75f, 0.08f, 0.08f, 0.95f);

    Vector3 originalLocalPosition;
    Quaternion originalRecoilPivotLocalRotation;
    float currentRecoilAngle;
    int tracerShotCounter;
    Material runtimeMuzzleFlashMaterial;
    static Material sharedImpactDecalMaterial;
    readonly List<PooledImpactDecal> impactDecalPool = new List<PooledImpactDecal>();
    int nextImpactDecalIndex;
    MaterialPropertyBlock impactDecalPropertyBlock;

    class PooledImpactDecal
    {
        public GameObject gameObject;
        public Transform transform;
        public Renderer renderer;
        public float despawnTime;
        public bool inUse;
    }

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
    protected PlayerMovement playerMovement;
    bool ammoPoolInitialized;
    bool hasCachedPose;
    bool isEquipping;
    Coroutine equipCoroutine;
    float kickOffsetImpulse;
    float kickOffsetPeak;
    float kickOffsetTimer;
    float movementBobTimer;
    float reloadMovementBobTimer;
    float firingBobTimer;
    Vector3 bobPositionCurrent;
    Vector3 bobEulerCurrent;

    protected virtual void Awake()
    {
        if (impactDecalPropertyBlock == null)
        {
            impactDecalPropertyBlock = new MaterialPropertyBlock();
        }

        CacheOriginalPose();
    }

    protected virtual void OnDestroy()
    {
        if (runtimeMuzzleFlashMaterial != null)
        {
            Destroy(runtimeMuzzleFlashMaterial);
            runtimeMuzzleFlashMaterial = null;
        }

        for (int i = 0; i < impactDecalPool.Count; i++)
        {
            if (impactDecalPool[i].gameObject != null)
            {
                Destroy(impactDecalPool[i].gameObject);
            }
        }

        impactDecalPool.Clear();
    }

    protected virtual void Start()
    {
        playerInventory = GetComponentInParent<PlayerInventory>();
        if (playerInventory == null)
        {
            // Weapons can live under a different branch than PlayerBody in this project.
            playerInventory = FindObjectOfType<PlayerInventory>();
        }

        playerMovement = GetComponentInParent<PlayerMovement>();
        if (playerMovement == null)
        {
            playerMovement = FindObjectOfType<PlayerMovement>();
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
        kickOffsetImpulse = 0f;
        kickOffsetPeak = 0f;
        kickOffsetTimer = 0f;
        movementBobTimer = 0f;
        reloadMovementBobTimer = 0f;
        firingBobTimer = 0f;
        bobPositionCurrent = Vector3.zero;
        bobEulerCurrent = Vector3.zero;

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

        UpdateImpactDecalPool();
        UpdateReloadState();
        HandleInput();

        if (motionType == WeaponMotionType.Gun && !isEquipping)
        {
            if (!isReloading)
            {
                UpdateFireKickAndBob();

                Quaternion bobRotation =
                    originalLocalRotation * Quaternion.Euler(bobEulerCurrent);

                transform.localPosition = Vector3.Lerp(
                    transform.localPosition,
                    originalLocalPosition + bobPositionCurrent - (Vector3.forward * kickOffsetImpulse),
                    kickbackRecoverySpeed * Time.deltaTime
                );

                transform.localRotation = Quaternion.Slerp(
                    transform.localRotation,
                    bobRotation,
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
                    cameraRecoil.AddFovKick(fovKickAmount);
                }

                TriggerFireKick();
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
        ClearFireKickState();

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

    void UpdateFireKickAndBob()
    {
        UpdateFireKick();
        UpdateMovementBob();
    }

    void ClearFireKickState()
    {
        kickOffsetImpulse = 0f;
        kickOffsetPeak = 0f;
        kickOffsetTimer = 0f;
        firingBobTimer = 0f;
    }

    void TriggerFireKick()
    {
        float maxKick = Mathf.Max(0.001f, kickbackAmount * 2.25f);
        kickOffsetPeak = Mathf.Clamp(kickOffsetImpulse + kickbackAmount, 0f, maxKick);
        kickOffsetImpulse = kickOffsetPeak;
        kickOffsetTimer = 0f;
        firingBobTimer = Mathf.Max(firingBobTimer, firingBobSuppressDuration);
    }

    void UpdateFireKick()
    {
        if (firingBobTimer > 0f)
        {
            firingBobTimer = Mathf.Max(0f, firingBobTimer - Time.deltaTime);
        }

        if (kickOffsetImpulse <= 0f)
        {
            return;
        }

        float duration = Mathf.Max(0.01f, kickReturnDuration);
        float exponent = Mathf.Max(0.1f, kickReturnExponent);

        kickOffsetTimer += Time.deltaTime;
        float t = Mathf.Clamp01(kickOffsetTimer / duration);
        float curveValue = Mathf.Pow(1f - t, exponent);
        kickOffsetImpulse = Mathf.Max(0f, kickOffsetPeak * curveValue);

        if (t >= 1f || kickOffsetImpulse <= 0.0001f)
        {
            kickOffsetImpulse = 0f;
            kickOffsetPeak = 0f;
        }
    }

    void UpdateMovementBob()
    {
        if (!useMovementBob)
        {
            bobPositionCurrent = Vector3.zero;
            bobEulerCurrent = Vector3.zero;
            return;
        }

        float horizontalSpeed = 0f;
        bool isGrounded = true;

        if (playerMovement != null && playerMovement.bodyController != null)
        {
            Vector3 controllerVelocity = playerMovement.bodyController.velocity;
            horizontalSpeed = new Vector2(controllerVelocity.x, controllerVelocity.z).magnitude;
            isGrounded = playerMovement.bodyController.isGrounded;
        }
        else
        {
            float x = Input.GetAxisRaw("Horizontal");
            float z = Input.GetAxisRaw("Vertical");
            horizontalSpeed = new Vector2(x, z).magnitude * 6f;
        }

        float speedDenominator = playerMovement != null
            ? Mathf.Max(0.01f, playerMovement.moveSpeed)
            : 6f;

        float speed01 = Mathf.Clamp01(horizontalSpeed / speedDenominator);
        float movementWeight = (isGrounded && speed01 > 0.01f) ? speed01 : 0f;

        float bobMultiplier = 1f;
        if (Input.GetMouseButton(1))
        {
            bobMultiplier *= Mathf.Clamp01(aimBobMultiplier);
        }
        if (isReloading)
        {
            bobMultiplier *= Mathf.Clamp01(reloadBobMultiplier);
        }
        if (firingBobTimer > 0f)
        {
            bobMultiplier *= Mathf.Clamp01(firingBobMultiplier);
        }

        Vector3 bobPositionTarget = Vector3.zero;
        Vector3 bobEulerTarget = Vector3.zero;

        if (movementWeight > 0f)
        {
            movementBobTimer += Time.deltaTime * bobFrequency * Mathf.Lerp(0.75f, 1.35f, movementWeight);

            float sinA = Mathf.Sin(movementBobTimer);
            float sinB = Mathf.Sin(movementBobTimer * 2f);
            float cosA = Mathf.Cos(movementBobTimer);

            bobPositionTarget = new Vector3(
                sinA * bobPositionAmplitude.x,
                (sinB * 0.5f + 0.5f) * bobPositionAmplitude.y,
                cosA * bobPositionAmplitude.z
            ) * (movementWeight * bobMultiplier);

            bobEulerTarget = new Vector3(
                sinB * bobRotationAmplitude.x,
                cosA * bobRotationAmplitude.y,
                sinA * bobRotationAmplitude.z
            ) * (movementWeight * bobMultiplier);
        }
        else
        {
            movementBobTimer = 0f;
        }

        float smoothing = Mathf.Max(1f, bobSmoothing);
        bobPositionCurrent = Vector3.Lerp(
            bobPositionCurrent,
            bobPositionTarget,
            smoothing * Time.deltaTime
        );
        bobEulerCurrent = Vector3.Lerp(
            bobEulerCurrent,
            bobEulerTarget,
            smoothing * Time.deltaTime
        );
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

        Vector3 basePosition;
        Quaternion baseRotation;

        if (elapsedTime <= lowerEndTime)
        {
            float lowerT = Mathf.Clamp01(elapsedTime / lowerDuration);
            basePosition = Vector3.Lerp(originalLocalPosition, reloadPosition, lowerT);
            baseRotation = Quaternion.Slerp(originalLocalRotation, reloadRotation, lowerT);
        }
        else if (elapsedTime <= holdEndTime)
        {
            basePosition = reloadPosition;
            baseRotation = reloadRotation;
        }
        else
        {
            float raiseTime = elapsedTime - holdEndTime;
            float raiseT = Mathf.Clamp01(raiseTime / raiseDuration);
            float snapProgress = 1f - Mathf.Pow(1f - raiseT, 3f);
            basePosition = Vector3.Lerp(reloadPosition, originalLocalPosition, snapProgress);
            baseRotation = Quaternion.Slerp(reloadRotation, originalLocalRotation, snapProgress);
        }

        Vector3 bobPosition;
        Vector3 bobEuler;
        EvaluateReloadMovementBob(out bobPosition, out bobEuler);

        transform.localPosition = basePosition + bobPosition;
        transform.localRotation = baseRotation * Quaternion.Euler(bobEuler);
    }

    void EvaluateReloadMovementBob(out Vector3 bobPosition, out Vector3 bobEuler)
    {
        bobPosition = Vector3.zero;
        bobEuler = Vector3.zero;

        if (!useReloadMovementBob)
        {
            reloadMovementBobTimer = 0f;
            return;
        }

        float horizontalSpeed = 0f;
        bool isGrounded = true;

        if (playerMovement != null && playerMovement.bodyController != null)
        {
            Vector3 controllerVelocity = playerMovement.bodyController.velocity;
            horizontalSpeed = new Vector2(controllerVelocity.x, controllerVelocity.z).magnitude;
            isGrounded = playerMovement.bodyController.isGrounded;
        }
        else
        {
            float x = Input.GetAxisRaw("Horizontal");
            float z = Input.GetAxisRaw("Vertical");
            horizontalSpeed = new Vector2(x, z).magnitude * 6f;
        }

        float speedDenominator = playerMovement != null
            ? Mathf.Max(0.01f, playerMovement.moveSpeed)
            : 6f;

        float movementWeight = Mathf.Clamp01(horizontalSpeed / speedDenominator);
        if (!isGrounded || movementWeight <= 0.01f)
        {
            reloadMovementBobTimer = 0f;
            return;
        }

        reloadMovementBobTimer += Time.deltaTime
            * Mathf.Max(0.1f, reloadBobFrequency)
            * Mathf.Lerp(0.7f, 1.3f, movementWeight);

        float sinA = Mathf.Sin(reloadMovementBobTimer);
        float sinB = Mathf.Sin(reloadMovementBobTimer * 2f);
        float cosA = Mathf.Cos(reloadMovementBobTimer);

        bobPosition = new Vector3(
            sinA * reloadBobPositionAmplitude.x,
            (sinB * 0.5f + 0.5f) * reloadBobPositionAmplitude.y,
            cosA * reloadBobPositionAmplitude.z
        ) * movementWeight;

        bobEuler = new Vector3(
            sinB * reloadBobRotationAmplitude.x,
            cosA * reloadBobRotationAmplitude.y,
            sinA * reloadBobRotationAmplitude.z
        ) * movementWeight;
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

    protected void SpawnImpactDecal(RaycastHit hit)
    {
        SpawnImpactDecal(hit, IsEnemyHit(hit));
    }

    protected void SpawnImpactDecal(RaycastHit hit, bool isEnemyHit)
    {
        if (!useImpactDecals || hit.collider == null)
        {
            return;
        }

        int decalIndex = AcquireImpactDecalIndex();
        if (decalIndex < 0 || decalIndex >= impactDecalPool.Count)
        {
            return;
        }

        PooledImpactDecal pooledDecal = impactDecalPool[decalIndex];
        if (pooledDecal.gameObject == null)
        {
            return;
        }

        pooledDecal.inUse = true;
        pooledDecal.gameObject.name = isEnemyHit ? "ImpactDecalEnemy" : "ImpactDecalWorld";
        pooledDecal.transform.SetParent(hit.collider.transform, true);
        pooledDecal.transform.position = hit.point + (hit.normal * impactDecalSurfaceOffset);
        pooledDecal.transform.rotation = Quaternion.LookRotation(-hit.normal);
        pooledDecal.transform.Rotate(Vector3.forward, Random.Range(0f, 360f), Space.Self);

        float decalSize = Random.Range(impactDecalMinSize, impactDecalMaxSize);
        pooledDecal.transform.localScale = new Vector3(decalSize, decalSize, decalSize);

        if (pooledDecal.renderer != null)
        {
            if (impactDecalPropertyBlock == null)
            {
                impactDecalPropertyBlock = new MaterialPropertyBlock();
            }

            Color decalColor = isEnemyHit ? enemyImpactDecalColor : worldImpactDecalColor;
            impactDecalPropertyBlock.Clear();
            impactDecalPropertyBlock.SetColor("_BaseColor", decalColor);
            impactDecalPropertyBlock.SetColor("_Color", decalColor);
            pooledDecal.renderer.SetPropertyBlock(impactDecalPropertyBlock);
        }

        pooledDecal.gameObject.SetActive(true);
        pooledDecal.despawnTime = Time.time + Mathf.Max(0.2f, impactDecalLifetime);
        impactDecalPool[decalIndex] = pooledDecal;
    }

    void UpdateImpactDecalPool()
    {
        if (impactDecalPool.Count <= 0)
        {
            return;
        }

        for (int i = 0; i < impactDecalPool.Count; i++)
        {
            PooledImpactDecal pooledDecal = impactDecalPool[i];
            if (!pooledDecal.inUse)
            {
                continue;
            }

            if (
                pooledDecal.gameObject == null
                || Time.time >= pooledDecal.despawnTime
            )
            {
                ReleaseImpactDecal(i);
            }
        }
    }

    int AcquireImpactDecalIndex()
    {
        int poolSize = impactDecalPool.Count;

        for (int i = 0; i < poolSize; i++)
        {
            int index = (nextImpactDecalIndex + i) % poolSize;
            PooledImpactDecal pooledDecal = impactDecalPool[index];

            if (pooledDecal.gameObject == null)
            {
                impactDecalPool[index] = CreatePooledImpactDecal();
                nextImpactDecalIndex = (index + 1) % Mathf.Max(1, impactDecalPool.Count);
                return index;
            }

            if (!pooledDecal.inUse)
            {
                nextImpactDecalIndex = (index + 1) % Mathf.Max(1, impactDecalPool.Count);
                return index;
            }
        }

        int maxPoolSize = Mathf.Max(1, maxImpactDecals);
        if (impactDecalPool.Count < maxPoolSize)
        {
            impactDecalPool.Add(CreatePooledImpactDecal());
            nextImpactDecalIndex = impactDecalPool.Count % Mathf.Max(1, impactDecalPool.Count);
            return impactDecalPool.Count - 1;
        }

        int oldestIndex = 0;
        float oldestDespawnTime = float.MaxValue;
        for (int i = 0; i < impactDecalPool.Count; i++)
        {
            if (impactDecalPool[i].despawnTime < oldestDespawnTime)
            {
                oldestDespawnTime = impactDecalPool[i].despawnTime;
                oldestIndex = i;
            }
        }

        ReleaseImpactDecal(oldestIndex);
        nextImpactDecalIndex = (oldestIndex + 1) % Mathf.Max(1, impactDecalPool.Count);
        return oldestIndex;
    }

    PooledImpactDecal CreatePooledImpactDecal()
    {
        GameObject decalObject = GameObject.CreatePrimitive(PrimitiveType.Quad);
        decalObject.name = "ImpactDecal";

        Collider decalCollider = decalObject.GetComponent<Collider>();
        if (decalCollider != null)
        {
            Destroy(decalCollider);
        }

        Renderer decalRenderer = decalObject.GetComponent<Renderer>();
        if (decalRenderer != null)
        {
            Material decalMaterial = GetOrCreateImpactDecalMaterial();
            if (decalMaterial != null)
            {
                decalRenderer.sharedMaterial = decalMaterial;
            }

            decalRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            decalRenderer.receiveShadows = false;
            decalRenderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
            decalRenderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
        }

        decalObject.SetActive(false);

        return new PooledImpactDecal
        {
            gameObject = decalObject,
            transform = decalObject.transform,
            renderer = decalRenderer,
            despawnTime = 0f,
            inUse = false,
        };
    }

    void ReleaseImpactDecal(int index)
    {
        if (index < 0 || index >= impactDecalPool.Count)
        {
            return;
        }

        PooledImpactDecal pooledDecal = impactDecalPool[index];
        if (pooledDecal.gameObject == null)
        {
            pooledDecal.inUse = false;
            pooledDecal.despawnTime = 0f;
            impactDecalPool[index] = pooledDecal;
            return;
        }

        pooledDecal.transform.SetParent(null, true);
        pooledDecal.gameObject.SetActive(false);
        pooledDecal.inUse = false;
        pooledDecal.despawnTime = 0f;
        impactDecalPool[index] = pooledDecal;
    }

    bool IsEnemyHit(RaycastHit hit)
    {
        if (hit.collider == null)
        {
            return false;
        }

        if (hit.collider.CompareTag("Enemy"))
        {
            return true;
        }

        IDamageable damageable =
            hit.collider.GetComponent<IDamageable>()
            ?? hit.collider.GetComponentInParent<IDamageable>();

        if (damageable == null)
        {
            return false;
        }

        return !(damageable is PlayerInventory);
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

    Material GetOrCreateImpactDecalMaterial()
    {
        if (sharedImpactDecalMaterial != null)
        {
            return sharedImpactDecalMaterial;
        }

        string[] shaderCandidates =
        {
            "Universal Render Pipeline/Unlit",
            "Unlit/Color",
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

        sharedImpactDecalMaterial = new Material(chosenShader);
        sharedImpactDecalMaterial.name = "RuntimeImpactDecalMaterial";
        sharedImpactDecalMaterial.enableInstancing = true;
        sharedImpactDecalMaterial.hideFlags = HideFlags.HideAndDontSave;

        if (sharedImpactDecalMaterial.HasProperty("_Surface"))
        {
            sharedImpactDecalMaterial.SetFloat("_Surface", 1f);
        }
        if (sharedImpactDecalMaterial.HasProperty("_Cull"))
        {
            sharedImpactDecalMaterial.SetFloat("_Cull", 0f);
        }
        if (sharedImpactDecalMaterial.HasProperty("_BaseColor"))
        {
            sharedImpactDecalMaterial.SetColor("_BaseColor", worldImpactDecalColor);
        }
        if (sharedImpactDecalMaterial.HasProperty("_Color"))
        {
            sharedImpactDecalMaterial.SetColor("_Color", worldImpactDecalColor);
        }

        return sharedImpactDecalMaterial;
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
