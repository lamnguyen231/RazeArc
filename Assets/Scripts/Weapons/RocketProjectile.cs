using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RocketProjectile : MonoBehaviour
{
    [Header("Explosion Force")]
    public float explosionForce = 30f;
    public float upwardsModifier = 1.5f;

    [Header("Explosion Damage")]
    public float minDamageMultiplierAtEdge = 0.5f;
    public float playerSelfDamageMultiplier = 0.6f;
    public float maxSelfDamage = 85f;

    [Header("Rocket Jump")]
    public float playerExplosionForceMultiplier = 3.2f;
    public float rocketJumpCenterBonus = 2.1f;
    public float minPlayerForceFalloff = 0.6f;

    [Header("Explosion VFX")]
    public GameObject explosionEffectPrefab;
    public bool useRuntimeExplosionEffect = true;
    public Color explosionCoreColor = new Color(1f, 0.75f, 0.15f, 1f);
    public Color explosionSmokeColor = new Color(0.25f, 0.25f, 0.25f, 0.8f);
    public float explosionVisualScale = 1.2f;
    public float explosionDuration = 1.8f;
    public float fireBurstDuration = 0.9f;
    public float fireBurstMinLifetime = 0.45f;
    public float fireBurstMaxLifetime = 1.1f;
    public float explosionLightIntensity = 5.5f;
    public float explosionLightFadeDelay = 0.08f;
    public float explosionLightFadeDuration = 0.85f;

    [Header("Explosion Audio")]
    public AudioClip explosionClip;
    [Range(0f, 1f)]
    public float explosionVolume = 1f;
    [Range(0f, 1f)]
    public float explosionSpatialBlend = 1f;
    public float explosionMinDistance = 3f;
    public float explosionMaxDistance = 45f;

    [Header("Smoke Trail")]
    public bool useSmokeTrail = true;
    public Color smokeColor = new Color(0.4f, 0.4f, 0.4f, 0.7f);
    public float smokeLifetime = 2.4f;
    public float smokeStartSize = 0.18f;
    public float smokeEndSize = 0.7f;
    public float smokeEmissionRate = 45f;
    public float smokeLingerDuration = 3.5f;


    public float speed;
    public float damage;
    public float explosionRadius = 7.2f;

    public LayerMask damageMask;

    Vector3 direction;
    Rigidbody rb;
    ParticleSystem smokeTrail;
    bool hasExploded;

    static Material sharedSmokeMaterial;
    static Material sharedExplosionMaterial;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();

        if (useSmokeTrail)
        {
            CreateSmokeTrail();
        }
    }

    void Start()
    {
        Destroy(gameObject, 20f); // Destroy after 20 seconds if it doesn't hit anything
    }

    public void Initialize(Vector3 dir, float rocketSpeed, float damage)
    {
        direction = dir.normalized;
        speed = rocketSpeed;
        this.damage = damage;

        rb.velocity = direction * speed;
    }

    void OnCollisionEnter(Collision collision)
    {
        if (hasExploded)
        {
            return;
        }

        Explode();
    }

    void Explode()
    {
        hasExploded = true;
        Vector3 explosionPosition = transform.position;

        Collider[] hits = Physics.OverlapSphere(explosionPosition, explosionRadius);
        HashSet<IDamageable> damagedTargets = new HashSet<IDamageable>();
        HashSet<PlayerMovement> pushedPlayers = new HashSet<PlayerMovement>();

        foreach (Collider hit in hits)
        {
            if (hit == null)
            {
                continue;
            }

            Vector3 closestPoint = GetSafeClosestPoint(hit, explosionPosition);
            float distance = Vector3.Distance(explosionPosition, closestPoint);
            float normalizedDistance = Mathf.Clamp01(distance / Mathf.Max(0.01f, explosionRadius));
            float falloffMultiplier = Mathf.Lerp(
                1f,
                Mathf.Clamp01(minDamageMultiplierAtEdge),
                normalizedDistance
            );

            // Damage
            IDamageable damageable =
                hit.GetComponent<IDamageable>()
                ?? hit.GetComponentInParent<IDamageable>();

            if (damageable != null && damagedTargets.Add(damageable))
            {
                float finalDamage = damage * falloffMultiplier;
                if (damageable is PlayerInventory)
                {
                    finalDamage *= Mathf.Max(0f, playerSelfDamageMultiplier);
                    finalDamage = Mathf.Min(finalDamage, Mathf.Max(0f, maxSelfDamage));
                }

                damageable.TakeDamage(finalDamage);
            }

            // Player pushback
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

                // Close explosions should provide strong lift for intentional rocket jumps.
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

        DetachAndFadeSmokeTrail();

        Destroy(gameObject);
    }

    Vector3 GetSafeClosestPoint(Collider hit, Vector3 origin)
    {
        if (hit == null)
        {
            return origin;
        }

        // Non-convex mesh colliders throw on ClosestPoint; bounds fallback is stable.
        MeshCollider meshCollider = hit as MeshCollider;
        if (meshCollider != null && !meshCollider.convex)
        {
            return meshCollider.bounds.ClosestPoint(origin);
        }

        try
        {
            return hit.ClosestPoint(origin);
        }
        catch (UnityException)
        {
            return hit.bounds.ClosestPoint(origin);
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }

    void PlayExplosionSound(Vector3 explosionPosition)
    {
        if (explosionClip == null)
        {
            return;
        }

        GameObject audioObject = new GameObject("RocketExplosionAudio");
        audioObject.transform.position = explosionPosition;

        AudioSource source = audioObject.AddComponent<AudioSource>();
        source.clip = explosionClip;
        source.volume = Mathf.Clamp01(explosionVolume);
        source.spatialBlend = Mathf.Clamp01(explosionSpatialBlend);
        source.minDistance = Mathf.Max(0.01f, explosionMinDistance);
        source.maxDistance = Mathf.Max(source.minDistance, explosionMaxDistance);
        source.rolloffMode = AudioRolloffMode.Linear;
        source.playOnAwake = false;
        source.Play();

        Destroy(audioObject, explosionClip.length + 0.1f);
    }

    void CreateSmokeTrail()
    {
        Transform existing = transform.Find("SmokeTrail");
        if (existing != null)
        {
            smokeTrail = existing.GetComponent<ParticleSystem>();
            if (smokeTrail != null)
            {
                return;
            }
        }

        GameObject smokeObject = new GameObject("SmokeTrail");
        smokeObject.transform.SetParent(transform, false);
        smokeObject.transform.localPosition = Vector3.zero;

        smokeTrail = smokeObject.AddComponent<ParticleSystem>();
        ParticleSystemRenderer psRenderer = smokeObject.GetComponent<ParticleSystemRenderer>();
        Color baseSmokeColor = GetGreyscaleSmokeColor(smokeColor);

        var main = smokeTrail.main;
        main.loop = true;
        main.playOnAwake = true;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startLifetime = smokeLifetime;
        main.startSize = smokeStartSize;
        main.startSpeed = 0f;
        main.maxParticles = 400;
        main.startColor = baseSmokeColor;

        var emission = smokeTrail.emission;
        emission.enabled = true;
        emission.rateOverTime = smokeEmissionRate;

        var shape = smokeTrail.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.03f;

        var sizeOverLifetime = smokeTrail.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(
            1f,
            new AnimationCurve(
                new Keyframe(0f, 1f),
                new Keyframe(1f, smokeEndSize / Mathf.Max(0.01f, smokeStartSize))
            )
        );

        var colorOverLifetime = smokeTrail.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        Color darkSmoke = Color.Lerp(Color.black, baseSmokeColor, 0.75f);
        Color lightSmoke = Color.Lerp(baseSmokeColor, Color.white, 0.3f);
        gradient.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(darkSmoke, 0f),
                new GradientColorKey(lightSmoke, 1f)
            },
            new GradientAlphaKey[]
            {
                new GradientAlphaKey(baseSmokeColor.a, 0f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        colorOverLifetime.color = gradient;

        var noise = smokeTrail.noise;
        noise.enabled = true;
        noise.strength = 0.18f;
        noise.frequency = 0.5f;
        noise.scrollSpeed = 0.2f;

        psRenderer.renderMode = ParticleSystemRenderMode.Billboard;
        psRenderer.sortMode = ParticleSystemSortMode.Distance;
        TryAssignSmokeMaterial(psRenderer);

        smokeTrail.Play();
    }

    void DetachAndFadeSmokeTrail()
    {
        if (smokeTrail == null)
        {
            return;
        }

        Transform smokeTransform = smokeTrail.transform;
        smokeTransform.SetParent(null, true);

        var emission = smokeTrail.emission;
        emission.enabled = false;

        smokeTrail.Stop(true, ParticleSystemStopBehavior.StopEmitting);

        float particleLifetime = Mathf.Max(smokeLifetime, 1.5f);
        float lingerDuration = Mathf.Max(smokeLingerDuration, 2.5f);
        float cleanupDelay = particleLifetime + lingerDuration;
        Destroy(smokeTransform.gameObject, cleanupDelay);
    }

    void TryAssignSmokeMaterial(ParticleSystemRenderer psRenderer)
    {
        if (psRenderer == null)
        {
            return;
        }

        if (sharedSmokeMaterial == null)
        {
            Shader shader =
                Shader.Find("Universal Render Pipeline/Particles/Unlit")
                ?? Shader.Find("Particles/Standard Unlit")
                ?? Shader.Find("Legacy Shaders/Particles/Alpha Blended");

            if (shader != null)
            {
                sharedSmokeMaterial = new Material(shader);
                sharedSmokeMaterial.name = "RocketSmokeRuntimeMaterial";
                sharedSmokeMaterial.hideFlags = HideFlags.HideAndDontSave;
            }
        }

        if (sharedSmokeMaterial != null)
        {
            psRenderer.sharedMaterial = sharedSmokeMaterial;
        }
    }

    void SpawnExplosionEffect(Vector3 position)
    {
        if (explosionEffectPrefab != null)
        {
            GameObject prefabVfx = Instantiate(explosionEffectPrefab, position, Quaternion.identity);
            Destroy(prefabVfx, Mathf.Max(2f, explosionDuration + 0.5f));
            return;
        }

        if (!useRuntimeExplosionEffect)
        {
            return;
        }

        GameObject root = new GameObject("RocketExplosionVFX");
        root.transform.position = position;

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
        fireEmission.SetBursts(
            new ParticleSystem.Burst[]
            {
                new ParticleSystem.Burst(0f, 65)
            }
        );

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
        smokeEmission.SetBursts(
            new ParticleSystem.Burst[]
            {
                new ParticleSystem.Burst(0f, 30)
            }
        );

        var smokeShape = smoke.shape;
        smokeShape.enabled = true;
        smokeShape.shapeType = ParticleSystemShapeType.Sphere;
        smokeShape.radius = 0.18f * explosionVisualScale;

        var smokeSizeOverLifetime = smoke.sizeOverLifetime;
        smokeSizeOverLifetime.enabled = true;
        smokeSizeOverLifetime.size = new ParticleSystem.MinMaxCurve(
            1f,
            new AnimationCurve(
                new Keyframe(0f, 0.5f),
                new Keyframe(1f, 1.4f)
            )
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
        if (psRenderer == null)
        {
            return;
        }

        if (sharedExplosionMaterial == null)
        {
            Shader shader =
                Shader.Find("Universal Render Pipeline/Particles/Unlit")
                ?? Shader.Find("Particles/Standard Unlit")
                ?? Shader.Find("Legacy Shaders/Particles/Alpha Blended");

            if (shader != null)
            {
                sharedExplosionMaterial = new Material(shader);
                sharedExplosionMaterial.name = "RocketExplosionRuntimeMaterial";
                sharedExplosionMaterial.hideFlags = HideFlags.HideAndDontSave;
            }
        }

        if (sharedExplosionMaterial != null)
        {
            psRenderer.sharedMaterial = sharedExplosionMaterial;
        }
    }

    IEnumerator FadeExplosionLight(Light lightToFade, float delay, float fadeDuration)
    {
        if (lightToFade == null)
        {
            yield break;
        }

        if (delay > 0f)
        {
            yield return new WaitForSeconds(delay);
        }

        float startIntensity = lightToFade.intensity;
        float duration = Mathf.Max(0.01f, fadeDuration);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            if (lightToFade == null)
            {
                yield break;
            }

            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            // SmoothStep provides a more natural, gradual light decay than a hard linear drop.
            float easedT = Mathf.SmoothStep(0f, 1f, t);
            lightToFade.intensity = Mathf.Lerp(startIntensity, 0f, easedT);
            yield return null;
        }

        if (lightToFade != null)
        {
            lightToFade.intensity = 0f;
            Destroy(lightToFade);
        }
    }

    Color GetGreyscaleSmokeColor(Color source)
    {
        float luma = source.r * 0.299f + source.g * 0.587f + source.b * 0.114f;
        float smokeGrey = Mathf.Clamp01(luma * 0.95f + 0.05f);
        return new Color(smokeGrey, smokeGrey, smokeGrey, source.a);
    }
}