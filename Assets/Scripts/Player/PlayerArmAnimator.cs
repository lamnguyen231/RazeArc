using UnityEngine;

public class PlayerArmAnimator : MonoBehaviour
{
    [Header("References")]
    public Transform rightArm;
    public Transform leftArm;
    public WeaponSwitcher weaponSwitcher;
    public PlayerMovement playerMovement;

    [Header("Layering")]
    public bool applyViewModelLayer = true;
    public string viewModelLayerName = "ViewModel";

    [Header("General Motion")]
    public float motionSmoothing = 16f;
    public Vector3 rightArmPositionOffset;
    public Vector3 rightArmRotationOffset;
    public Vector3 leftArmPositionOffset;
    public Vector3 leftArmRotationOffset;

    [Header("Movement Bob")]
    public bool useMovementBob = true;
    public float bobFrequency = 8.5f;
    public float bobSpeedInfluence = 1f;
    public Vector3 rightBobPositionAmplitude = new Vector3(0.012f, 0.014f, 0.01f);
    public Vector3 rightBobRotationAmplitude = new Vector3(1.8f, 1.2f, 1.4f);
    public Vector3 leftBobPositionAmplitude = new Vector3(0.009f, 0.011f, 0.008f);
    public Vector3 leftBobRotationAmplitude = new Vector3(1.2f, 0.8f, 1f);

    [Header("Fire Kick")]
    public Vector3 rightFireKickPosition = new Vector3(0f, 0f, -0.038f);
    public Vector3 rightFireKickEuler = new Vector3(-7.5f, 3.2f, 1.8f);
    public Vector3 leftFireKickPosition = new Vector3(0f, 0f, -0.02f);
    public Vector3 leftFireKickEuler = new Vector3(-3f, -1.2f, -1f);
    public float kickRecoverySpeed = 17f;

    [Header("Reload Pose")]
    public Vector3 rightReloadPosition = new Vector3(0.012f, -0.032f, -0.03f);
    public Vector3 rightReloadEuler = new Vector3(18f, 10f, 10f);
    public Vector3 leftReloadPosition = new Vector3(-0.012f, -0.02f, -0.025f);
    public Vector3 leftReloadEuler = new Vector3(12f, -8f, -9f);

    WeaponBase activeWeapon;
    float bobTimer;
    Vector3 rightKickPositionCurrent;
    Vector3 rightKickEulerCurrent;
    Vector3 leftKickPositionCurrent;
    Vector3 leftKickEulerCurrent;

    Vector3 rightBaseLocalPosition;
    Quaternion rightBaseLocalRotation;
    Vector3 leftBaseLocalPosition;
    Quaternion leftBaseLocalRotation;
    bool cachedBasePose;
    bool warnedMissingLayer;

    void Awake()
    {
        ResolveReferences();
        CacheBasePose();
        ApplyArmLayerIfNeeded();
    }

    void OnEnable()
    {
        ResolveReferences();
        SubscribeToSwitcher();

        if (weaponSwitcher != null)
        {
            SetActiveWeapon(weaponSwitcher.CurrentWeapon);
        }
        else
        {
            SetActiveWeapon(null);
        }
    }

    void OnDisable()
    {
        UnsubscribeFromWeaponEvents(activeWeapon);

        if (weaponSwitcher != null)
        {
            weaponSwitcher.WeaponSelected -= HandleWeaponSelected;
        }
    }

    void Update()
    {
        if (!cachedBasePose)
        {
            CacheBasePose();
        }

        if (rightArm == null)
        {
            return;
        }

        float dt = Time.deltaTime;
        float movementWeight = EvaluateMovementWeight();
        UpdateKickRecovery(dt);

        if (useMovementBob)
        {
            float bobSpeed = bobFrequency * Mathf.Lerp(0.65f, 1.35f, movementWeight * bobSpeedInfluence);
            bobTimer += dt * Mathf.Max(0.1f, bobSpeed);
        }
        else
        {
            bobTimer = 0f;
        }

        bool reloadActive = activeWeapon != null && activeWeapon.IsReloading;
        bool equipActive = activeWeapon != null && activeWeapon.IsEquipping;

        float weaponMotionMultiplier = 1f;
        if (reloadActive)
        {
            weaponMotionMultiplier *= 0.2f;
        }
        if (equipActive)
        {
            weaponMotionMultiplier *= 0.5f;
        }

        ApplyRightArmPose(movementWeight, weaponMotionMultiplier, reloadActive);
        ApplyLeftArmPose(movementWeight, weaponMotionMultiplier, reloadActive);
    }

    void ResolveReferences()
    {
        if (weaponSwitcher == null)
        {
            weaponSwitcher = FindObjectOfType<WeaponSwitcher>();
        }

        if (playerMovement == null)
        {
            playerMovement = FindObjectOfType<PlayerMovement>();
        }
    }

    void SubscribeToSwitcher()
    {
        if (weaponSwitcher == null)
        {
            return;
        }

        weaponSwitcher.WeaponSelected -= HandleWeaponSelected;
        weaponSwitcher.WeaponSelected += HandleWeaponSelected;
    }

    void HandleWeaponSelected(WeaponBase weapon, int index)
    {
        SetActiveWeapon(weapon);
    }

    void SetActiveWeapon(WeaponBase weapon)
    {
        if (activeWeapon == weapon)
        {
            UpdateArmVisibility(activeWeapon);
            return;
        }

        UnsubscribeFromWeaponEvents(activeWeapon);
        activeWeapon = weapon;
        SubscribeToWeaponEvents(activeWeapon);

        rightKickPositionCurrent = Vector3.zero;
        rightKickEulerCurrent = Vector3.zero;
        leftKickPositionCurrent = Vector3.zero;
        leftKickEulerCurrent = Vector3.zero;

        UpdateArmVisibility(activeWeapon);
    }

    void SubscribeToWeaponEvents(WeaponBase weapon)
    {
        if (weapon == null)
        {
            return;
        }

        weapon.Fired += HandleWeaponFired;
    }

    void UnsubscribeFromWeaponEvents(WeaponBase weapon)
    {
        if (weapon == null)
        {
            return;
        }

        weapon.Fired -= HandleWeaponFired;
    }

    void HandleWeaponFired(WeaponBase weapon)
    {
        rightKickPositionCurrent += rightFireKickPosition;
        rightKickEulerCurrent += rightFireKickEuler;

        if (ShouldShowLeftArm(weapon))
        {
            leftKickPositionCurrent += leftFireKickPosition;
            leftKickEulerCurrent += leftFireKickEuler;
        }
    }

    void CacheBasePose()
    {
        if (rightArm == null)
        {
            return;
        }

        rightBaseLocalPosition = rightArm.localPosition;
        rightBaseLocalRotation = rightArm.localRotation;

        if (leftArm != null)
        {
            leftBaseLocalPosition = leftArm.localPosition;
            leftBaseLocalRotation = leftArm.localRotation;
        }

        cachedBasePose = true;
    }

    float EvaluateMovementWeight()
    {
        if (playerMovement != null && playerMovement.bodyController != null)
        {
            Vector3 velocity = playerMovement.bodyController.velocity;
            float horizontalSpeed = new Vector2(velocity.x, velocity.z).magnitude;
            float speedDenominator = Mathf.Max(0.01f, playerMovement.moveSpeed);
            return Mathf.Clamp01(horizontalSpeed / speedDenominator);
        }

        float x = Input.GetAxisRaw("Horizontal");
        float z = Input.GetAxisRaw("Vertical");
        return Mathf.Clamp01(new Vector2(x, z).magnitude);
    }

    void UpdateKickRecovery(float dt)
    {
        float recovery = Mathf.Max(0.1f, kickRecoverySpeed) * dt;

        rightKickPositionCurrent = Vector3.Lerp(rightKickPositionCurrent, Vector3.zero, recovery);
        rightKickEulerCurrent = Vector3.Lerp(rightKickEulerCurrent, Vector3.zero, recovery);
        leftKickPositionCurrent = Vector3.Lerp(leftKickPositionCurrent, Vector3.zero, recovery);
        leftKickEulerCurrent = Vector3.Lerp(leftKickEulerCurrent, Vector3.zero, recovery);
    }

    void ApplyRightArmPose(float movementWeight, float weaponMotionMultiplier, bool reloadActive)
    {
        float sinA = Mathf.Sin(bobTimer);
        float sinB = Mathf.Sin(bobTimer * 2f);
        float cosA = Mathf.Cos(bobTimer);

        Vector3 bobPosition = new Vector3(
            sinA * rightBobPositionAmplitude.x,
            (sinB * 0.5f + 0.5f) * rightBobPositionAmplitude.y,
            cosA * rightBobPositionAmplitude.z
        ) * movementWeight * weaponMotionMultiplier;

        Vector3 bobEuler = new Vector3(
            sinB * rightBobRotationAmplitude.x,
            cosA * rightBobRotationAmplitude.y,
            sinA * rightBobRotationAmplitude.z
        ) * movementWeight * weaponMotionMultiplier;

        Vector3 reloadPosition = Vector3.zero;
        Vector3 reloadEuler = Vector3.zero;
        if (reloadActive)
        {
            float reloadBlend = EvaluateReloadBlend(activeWeapon);
            reloadPosition = rightReloadPosition * reloadBlend;
            reloadEuler = rightReloadEuler * reloadBlend;
        }

        Vector3 targetPosition =
            rightBaseLocalPosition
            + rightArmPositionOffset
            + bobPosition
            + rightKickPositionCurrent
            + reloadPosition;

        Quaternion targetRotation =
            rightBaseLocalRotation
            * Quaternion.Euler(
                rightArmRotationOffset
                + bobEuler
                + rightKickEulerCurrent
                + reloadEuler
            );

        float smooth = Mathf.Max(1f, motionSmoothing) * Time.deltaTime;
        rightArm.localPosition = Vector3.Lerp(rightArm.localPosition, targetPosition, smooth);
        rightArm.localRotation = Quaternion.Slerp(rightArm.localRotation, targetRotation, smooth);
    }

    void ApplyLeftArmPose(float movementWeight, float weaponMotionMultiplier, bool reloadActive)
    {
        if (leftArm == null)
        {
            return;
        }

        bool showLeftArm = ShouldShowLeftArm(activeWeapon);
        if (!showLeftArm)
        {
            return;
        }

        float sinA = Mathf.Sin(bobTimer + 0.9f);
        float sinB = Mathf.Sin((bobTimer + 0.9f) * 2f);
        float cosA = Mathf.Cos(bobTimer + 0.9f);

        Vector3 bobPosition = new Vector3(
            sinA * leftBobPositionAmplitude.x,
            (sinB * 0.5f + 0.5f) * leftBobPositionAmplitude.y,
            cosA * leftBobPositionAmplitude.z
        ) * movementWeight * weaponMotionMultiplier;

        Vector3 bobEuler = new Vector3(
            sinB * leftBobRotationAmplitude.x,
            cosA * leftBobRotationAmplitude.y,
            sinA * leftBobRotationAmplitude.z
        ) * movementWeight * weaponMotionMultiplier;

        Vector3 reloadPosition = Vector3.zero;
        Vector3 reloadEuler = Vector3.zero;
        if (reloadActive)
        {
            float reloadBlend = EvaluateReloadBlend(activeWeapon);
            reloadPosition = leftReloadPosition * reloadBlend;
            reloadEuler = leftReloadEuler * reloadBlend;
        }

        Vector3 targetPosition =
            leftBaseLocalPosition
            + leftArmPositionOffset
            + bobPosition
            + leftKickPositionCurrent
            + reloadPosition;

        Quaternion targetRotation =
            leftBaseLocalRotation
            * Quaternion.Euler(
                leftArmRotationOffset
                + bobEuler
                + leftKickEulerCurrent
                + reloadEuler
            );

        float smooth = Mathf.Max(1f, motionSmoothing) * Time.deltaTime;
        leftArm.localPosition = Vector3.Lerp(leftArm.localPosition, targetPosition, smooth);
        leftArm.localRotation = Quaternion.Slerp(leftArm.localRotation, targetRotation, smooth);
    }

    float EvaluateReloadBlend(WeaponBase weapon)
    {
        if (weapon == null || !weapon.IsReloading)
        {
            return 0f;
        }

        float t = weapon.ReloadProgressNormalized;
        return Mathf.Sin(t * Mathf.PI);
    }

    void UpdateArmVisibility(WeaponBase weapon)
    {
        if (rightArm != null && !rightArm.gameObject.activeSelf)
        {
            rightArm.gameObject.SetActive(true);
        }

        if (leftArm == null)
        {
            return;
        }

        bool shouldShowLeftArm = ShouldShowLeftArm(weapon);
        if (leftArm.gameObject.activeSelf != shouldShowLeftArm)
        {
            leftArm.gameObject.SetActive(shouldShowLeftArm);
        }
    }

    bool ShouldShowLeftArm(WeaponBase weapon)
    {
        if (weapon == null)
        {
            return true;
        }

        if (weapon is Pistol)
        {
            return false;
        }

        if (weapon.MotionKind == WeaponMotionType.Melee)
        {
            return false;
        }

        return true;
    }

    void ApplyArmLayerIfNeeded()
    {
        if (!applyViewModelLayer)
        {
            return;
        }

        int targetLayer = LayerMask.NameToLayer(viewModelLayerName);
        if (targetLayer < 0)
        {
            if (!warnedMissingLayer)
            {
                Debug.LogWarning("PlayerArmAnimator: Viewmodel layer not found: " + viewModelLayerName);
                warnedMissingLayer = true;
            }
            return;
        }

        if (rightArm != null)
        {
            SetLayerRecursively(rightArm, targetLayer);
        }

        if (leftArm != null)
        {
            SetLayerRecursively(leftArm, targetLayer);
        }
    }

    void SetLayerRecursively(Transform root, int layer)
    {
        if (root == null)
        {
            return;
        }

        root.gameObject.layer = layer;
        for (int i = 0; i < root.childCount; i++)
        {
            SetLayerRecursively(root.GetChild(i), layer);
        }
    }
}
