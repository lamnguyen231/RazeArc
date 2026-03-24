using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class SuicideEnemy : MonoBehaviour
{
    [Header("Trigger")]
    public float triggerRadius = 2.5f;
    public LayerMask playerMask;

    [Header("Explosion Force")]
    public float explosionForce = 22f;
    public float upwardsModifier = 1.2f;

    [Header("Explosion Damage")]
    public float damage = 80f;
    public float explosionRadius = 5f;
    public float minDamageMultiplierAtEdge = 0.4f;
    public float playerSelfDamageMultiplier = 0.6f;
    public float maxSelfDamage = 85f;

    [Header("Rocket Jump (Player Pushback)")]
    public float playerExplosionForceMultiplier = 2.8f;
    public float rocketJumpCenterBonus = 1.8f;
    public float minPlayerForceFalloff = 0.55f;

    [Header("Explosion VFX")]
    public GameObject explosionEffectPrefab;
    public bool useRuntimeExplosionEffect = true;
    public Color explosionCoreColor = new Color(1f, 0.6f, 0.1f, 1f);
    public Color explosionSmokeColor = new Color(0.2f, 0.2f, 0.2f, 0.85f);
    public float explosionVisualScale = 1.4f;
    public float explosionDuration = 1.8f;
    public float fireBurstDuration = 0.9f;
    public float fireBurstMinLifetime = 0.45f;
    public float fireBurstMaxLifetime = 1.1f;
    public float explosionLightIntensity = 6f;
    public float explosionLightFadeDelay = 0.08f;
    public float explosionLightFadeDuration = 0.85f;

    [Header("Explosion Audio")]
    public AudioClip explosionClip;
    [Range(0f, 1f)] public float explosionVolume = 1f;
    [Range(0f, 1f)] public float explosionSpatialBlend = 1f;
    public float explosionMinDistance = 3f;
    public float explosionMaxDistance = 45f;

    public LayerMask damageMask;

    bool hasExploded;
    private Animator anim;

    static Material sharedExplosionMaterial;

    // ??? Called externally or from Update proximity check ???????????????????

    private void Start()
    {
        anim = GetComponent<Animator>();
    }
    void Update()
    {
        if (hasExploded) return;

        // Proximity-based trigger (alternative / backup to collider trigger)
        Collider[] nearby = Physics.OverlapSphere(transform.position, triggerRadius, playerMask);
        if (nearby.Length > 0)
        {
            SuicideExplode();
            anim.Play("Defeated");
        }
    }

    // Use a sphere trigger collider on this GameObject as the primary trigger.
    void OnTriggerEnter(Collider other)
    {
        if (hasExploded) return;

        if (((1 << other.gameObject.layer) & playerMask) != 0)
        {
            SuicideExplode();
        }
    }

    // ??? Core explosion logic ????????????????????????????????????????????????

    public void SuicideExplode()
    {
        if (hasExploded) return;
        hasExploded = true;        

        Vector3 explosionPosition = transform.position;

        Collider[] hits = Physics.OverlapSphere(explosionPosition, explosionRadius, damageMask);
        HashSet<IDamageable> damagedTargets = new HashSet<IDamageable>();
        HashSet<PlayerMovement> pushedPlayers = new HashSet<PlayerMovement>();


        foreach (Collider hit in hits)
        {
            if (hit == null) continue;

            Vector3 closestPoint = GetSafeClosestPoint(hit, explosionPosition);
            float distance = Vector3.Distance(explosionPosition, closestPoint);
            float normalizedDistance = Mathf.Clamp01(distance / Mathf.Max(0.01f, explosionRadius));
            float falloffMultiplier = Mathf.Lerp(
                1f,
                Mathf.Clamp01(minDamageMultiplierAtEdge),
                normalizedDistance
            );

            // ?? Damage ??????????????????????????????????????????????????????
            IDamageable damageable =
                hit.GetComponent<IDamageable>()
                ?? hit.GetComponentInParent<IDamageable>();

            if (damageable != null && damagedTargets.Add(damageable))
            {
                float finalDamage = damage * falloffMultiplier;

                // Friendly-fire self-damage cap (matches Rocket behaviour)
                if (damageable is PlayerInventory)
                {
                    finalDamage *= Mathf.Max(0f, playerSelfDamageMultiplier);
                    finalDamage = Mathf.Min(finalDamage, Mathf.Max(0f, maxSelfDamage));
                }

                damageable.TakeDamage(finalDamage);
            }

            // ?? Player pushback ?????????????????????????????????????????????
            PlayerMovement player =
                hit.GetComponent<PlayerMovement>()
                ?? hit.GetComponentInParent<PlayerMovement>();

            if (player != null && pushedPlayers.Add(player))
            {
                float distanceForceMultiplier = Mathf.Lerp(
                    1f,
                    Mathf.Clamp01(minPlayerForceFalloff),
                    normalizedDistance
                );

                float centerBonus = Mathf.Lerp(
                    Mathf.Max(1f, rocketJumpCenterBonus),
                    1f,
                    normalizedDistance
                );

                player.AddExplosionForce(
                    explosionPosition,
                    explosionForce
                        * Mathf.Max(1f, playerExplosionForceMultiplier)
                        * distanceForceMultiplier
                        * centerBonus,
                    explosionRadius
                );
            }
        }

        PlayExplosionSound(explosionPosition);
        SpawnExplosionEffect(explosionPosition);

        // Destroy with a tiny delay so VFX/audio spawning finishes first
        Destroy(gameObject, 0.05f);
    }

    // ??? Helpers (direct ports from RocketProjectile) ????????????????????????

    Vector3 GetSafeClosestPoint(Collider hit, Vector3 origin)
    {
        if (hit == null) return origin;

        MeshCollider meshCollider = hit as MeshCollider;
        if (meshCollider != null && !meshCollider.convex)
            return meshCollider.bounds.ClosestPoint(origin);

        try { return hit.ClosestPoint(origin); }
        catch (UnityException) { return hit.bounds.ClosestPoint(origin); }
    }

    void PlayExplosionSound(Vector3 pos)
    {
        if (explosionClip == null) return;

        GameObject audioObj = new GameObject("EnemyExplosionAudio");
        audioObj.transform.position = pos;

        AudioSource source = audioObj.AddComponent<AudioSource>();
        source.clip = explosionClip;
        source.volume = Mathf.Clamp01(explosionVolume);
        source.spatialBlend = Mathf.Clamp01(explosionSpatialBlend);
        source.minDistance = Mathf.Max(0.01f, explosionMinDistance);
        source.maxDistance = Mathf.Max(source.minDistance, explosionMaxDistance);
        source.rolloffMode = AudioRolloffMode.Linear;
        source.playOnAwake = false;
        source.Play();

        Destroy(audioObj, explosionClip.length + 0.1f);
    }

    void SpawnExplosionEffect(Vector3 position)
    {
        if (explosionEffectPrefab != null)
        {
            GameObject prefabVfx = Instantiate(explosionEffectPrefab, position, Quaternion.identity);
            Destroy(prefabVfx, Mathf.Max(2f, explosionDuration + 0.5f));
            return;
        }

        if (!useRuntimeExplosionEffect) return;

        GameObject root = new GameObject("EnemyExplosionVFX");
        root.transform.position = position;

        // ?? Fire burst ??????????????????????????????????????????????????????
        GameObject fireObject = new GameObject("FireBurst");
        fireObject.transform.SetParent(root.transform, false);
        ParticleSystem fire = fireObject.AddComponent<ParticleSystem>();
        ParticleSystemRenderer fireRenderer = fireObject.GetComponent<ParticleSystemRenderer>();
        fire.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var fireMain = fire.main;
        fireMain.loop = false;
        fireMain.playOnAwake = false;
        fireMain.duration = fireBurstDuration;
        fireMain.simulationSpace = ParticleSystemSimulationSpace.World;
        fireMain.startLifetime = new ParticleSystem.MinMaxCurve(
            Mathf.Max(0.05f, fireBurstMinLifetime),
            Mathf.Max(fireBurstMinLifetime + 0.05f, fireBurstMaxLifetime)
        );
        fireMain.startSpeed = new ParticleSystem.MinMaxCurve(2.5f, 5.5f);
        fireMain.startSize = new ParticleSystem.MinMaxCurve(
            0.35f * explosionVisualScale,
            0.85f * explosionVisualScale
        );
        fireMain.maxParticles = 140;
        fireMain.startColor = explosionCoreColor;

        var fireEmission = fire.emission;
        fireEmission.enabled = true;
        fireEmission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 65) });

        var fireShape = fire.shape;
        fireShape.enabled = true;
        fireShape.shapeType = ParticleSystemShapeType.Sphere;
        fireShape.radius = 0.12f * explosionVisualScale;

        var fireColorOverLifetime = fire.colorOverLifetime;
        fireColorOverLifetime.enabled = true;
        Gradient fireGradient = new Gradient();
        fireGradient.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(Color.Lerp(Color.yellow, explosionCoreColor, 0.55f), 0f),
                new GradientColorKey(new Color(1f, 0.45f, 0.05f, 1f), 0.7f),
                new GradientColorKey(new Color(0.1f, 0.1f, 0.1f, 1f), 1f)
            },
            new GradientAlphaKey[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(0.9f, 0.2f),
                new GradientAlphaKey(0.55f, 0.6f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        fireColorOverLifetime.color = fireGradient;

        var fireSizeOverLifetime = fire.sizeOverLifetime;
        fireSizeOverLifetime.enabled = true;
        fireSizeOverLifetime.size = new ParticleSystem.MinMaxCurve(
            1f,
            new AnimationCurve(
                new Keyframe(0f, 0.35f),
                new Keyframe(0.25f, 1f),
                new Keyframe(1f, 0.12f)
            )
        );

        TryAssignExplosionMaterial(fireRenderer);

        // ?? Smoke burst ?????????????????????????????????????????????????????
        GameObject smokeObject = new GameObject("ExplosionSmoke");
        smokeObject.transform.SetParent(root.transform, false);
        ParticleSystem smoke = smokeObject.AddComponent<ParticleSystem>();
        ParticleSystemRenderer smokeRenderer = smokeObject.GetComponent<ParticleSystemRenderer>();
        smoke.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var smokeMain = smoke.main;
        smokeMain.loop = false;
        smokeMain.playOnAwake = false;
        smokeMain.duration = 1.2f;
        smokeMain.simulationSpace = ParticleSystemSimulationSpace.World;
        smokeMain.startLifetime = new ParticleSystem.MinMaxCurve(1.5f, 2.4f);
        smokeMain.startSpeed = new ParticleSystem.MinMaxCurve(1f, 2.3f);
        smokeMain.startSize = new ParticleSystem.MinMaxCurve(
            0.45f * explosionVisualScale,
            1.45f * explosionVisualScale
        );
        smokeMain.maxParticles = 80;
        smokeMain.startColor = explosionSmokeColor;

        var smokeEmission = smoke.emission;
        smokeEmission.enabled = true;
        smokeEmission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 30) });

        var smokeShape = smoke.shape;
        smokeShape.enabled = true;
        smokeShape.shapeType = ParticleSystemShapeType.Sphere;
        smokeShape.radius = 0.18f * explosionVisualScale;

        var smokeSizeOverLifetime = smoke.sizeOverLifetime;
        smokeSizeOverLifetime.enabled = true;
        smokeSizeOverLifetime.size = new ParticleSystem.MinMaxCurve(
            1f,
            new AnimationCurve(new Keyframe(0f, 0.5f), new Keyframe(1f, 1.4f))
        );

        var smokeColorOverLifetime = smoke.colorOverLifetime;
        smokeColorOverLifetime.enabled = true;
        Gradient smokeGradient = new Gradient();
        smokeGradient.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(explosionSmokeColor, 0f),
                new GradientColorKey(Color.Lerp(explosionSmokeColor, Color.white, 0.25f), 1f)
            },
            new GradientAlphaKey[]
            {
                new GradientAlphaKey(explosionSmokeColor.a, 0f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        smokeColorOverLifetime.color = smokeGradient;

        var smokeNoise = smoke.noise;
        smokeNoise.enabled = true;
        smokeNoise.strength = 0.35f;
        smokeNoise.frequency = 0.35f;

        TryAssignExplosionMaterial(smokeRenderer);

        // ?? Flash light ?????????????????????????????????????????????????????
        Light flash = root.AddComponent<Light>();
        flash.type = LightType.Point;
        flash.color = explosionCoreColor;
        flash.intensity = explosionLightIntensity;
        flash.range = Mathf.Max(3f, explosionRadius * 1.2f);
        StartCoroutine(FadeExplosionLight(flash, explosionLightFadeDelay, explosionLightFadeDuration));

        fire.Play();
        smoke.Play();

        float lightLifetime = explosionLightFadeDelay + explosionLightFadeDuration + 0.2f;
        Destroy(root, Mathf.Max(explosionDuration, 2.6f, lightLifetime));
    }

    void TryAssignExplosionMaterial(ParticleSystemRenderer psRenderer)
    {
        if (psRenderer == null) return;

        if (sharedExplosionMaterial == null)
        {
            Shader shader =
                Shader.Find("Universal Render Pipeline/Particles/Unlit")
                ?? Shader.Find("Particles/Standard Unlit")
                ?? Shader.Find("Legacy Shaders/Particles/Alpha Blended");

            if (shader != null)
            {
                sharedExplosionMaterial = new Material(shader);
                sharedExplosionMaterial.name = "EnemyExplosionRuntimeMaterial";
                sharedExplosionMaterial.hideFlags = HideFlags.HideAndDontSave;
            }
        }

        if (sharedExplosionMaterial != null)
            psRenderer.sharedMaterial = sharedExplosionMaterial;
    }

    IEnumerator FadeExplosionLight(Light lightToFade, float delay, float fadeDuration)
    {
        if (lightToFade == null) yield break;

        if (delay > 0f) yield return new WaitForSeconds(delay);

        float startIntensity = lightToFade.intensity;
        float duration = Mathf.Max(0.01f, fadeDuration);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            if (lightToFade == null) yield break;

            elapsed += Time.deltaTime;
            float easedT = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
            lightToFade.intensity = Mathf.Lerp(startIntensity, 0f, easedT);
            yield return null;
        }

        if (lightToFade != null)
        {
            lightToFade.intensity = 0f;
            Destroy(lightToFade);
        }
    }

    void OnDrawGizmos()
    {
        // Trigger radius (yellow)
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, triggerRadius);

        // Blast radius (red)
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}