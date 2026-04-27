using UnityEngine;
using System.Collections;

public class BossShooter : MonoBehaviour
{
    [Header("Phase Durations")]
    public float idleDuration = 5f;
    public float chargeDuration = 5f;

    [Header("Attack Properties")]
    public float bulletSpeed = 20f;
    public float bulletDamage = 25f;
    public int bulletsPerAttack = 1;

    [Header("Prefabs & Setup")]
    public GameObject chargeEffectPrefab;
    public GameObject customBulletPrefab;
    public Transform[] firePoints;
    
    [Tooltip("Optional: Sound to play when charging.")]
    public AudioClip chargeSound;
    [Tooltip("Optional: Sound to play when firing.")]
    public AudioClip fireSound;
    public AudioSource audioSource;

    [Header("Rotation Settings")]
    public float idleRotationSpeed = 2f;

    public Transform IdleRot1;
    public Transform IdleRot2;

    [Header("Visual Effects")]
    public Renderer bossRenderer;
    public float maxEmissionIntensity = 3f;
    public float zRotationAcceleration = 600f;

    private Material dynamicMaterial;
    private Color originalEmissionColor;

    private Transform playerHead;
    private Enemy enemy;
    private Rigidbody rb;
    private bool isDead = false;

    private void Start()
    {
        enemy = GetComponent<Enemy>();
        if (enemy == null) enemy = GetComponentInParent<Enemy>();
        
        rb = GetComponent<Rigidbody>();
        if (rb == null && enemy != null) rb = enemy.GetComponent<Rigidbody>();

        if (Camera.main != null)
        {
            playerHead = Camera.main.transform;
            
            // Initialize dynamic material
            if (bossRenderer != null)
            {
                dynamicMaterial = bossRenderer.material;
                if (dynamicMaterial.HasProperty("_EmissionColor"))
                {
                    originalEmissionColor = dynamicMaterial.GetColor("_EmissionColor");
                }
            }

            StartCoroutine(AttackCycle());
        }
        else
        {
            Debug.LogWarning("BossShooter: Main Camera not found. Attack cycle not started.");
        }
    }

    private IEnumerator AttackCycle()
    {
        while (!isDead)
        {
            // --- Phase 1: Idle (Random Rotation) ---
            float idleTimer = 0f;
            Quaternion targetRot1 = IdleRot1.rotation;
            Quaternion targetRot2 = IdleRot2.rotation;
            
            while (idleTimer < idleDuration)
            {
                // Every second or so, pick a new random target rotation
                if (idleTimer % 1.0f < Time.deltaTime)
                {
                    targetRot1 = Random.rotation;
                    targetRot2 = Random.rotation;
                }

                IdleRot1.rotation = Quaternion.Slerp(IdleRot1.rotation, targetRot1, Time.deltaTime * idleRotationSpeed);
                IdleRot2.rotation = Quaternion.Slerp(IdleRot2.rotation, targetRot2, Time.deltaTime * idleRotationSpeed);
                idleTimer += Time.deltaTime;
                yield return null;
            }

            // --- Phase 2: Charging (Particle Effect) ---
            GameObject chargeEffect = null;
            if (chargeEffectPrefab != null)
            {
                // Instantiate at boss center or first fire point
                Vector3 spawnPos = firePoints != null && firePoints.Length > 0 ? firePoints[0].position : transform.position;
                chargeEffect = Instantiate(chargeEffectPrefab, spawnPos, transform.rotation);
                chargeEffect.transform.SetParent(transform);
            }

            if (audioSource != null && chargeSound != null)
            {
                audioSource.PlayOneShot(chargeSound);
            }

            // Gradually rotate towards player and spin during charging
            float chargeTimer = 0f;
            float currentZSpin = 0f;

            while (chargeTimer < chargeDuration)
            {
                float t = chargeTimer / chargeDuration;

                // 1. Update Emission
                if (dynamicMaterial != null && dynamicMaterial.HasProperty("_EmissionColor"))
                {
                    float intensity = Mathf.Lerp(1f, maxEmissionIntensity, t);
                    dynamicMaterial.SetColor("_EmissionColor", originalEmissionColor * intensity);
                    dynamicMaterial.EnableKeyword("_EMISSION");
                }

                // 2. Update Rotation (Look at Player + Z Spin)
                if (playerHead != null)
                {
                    Vector3 dir = (playerHead.position - transform.position).normalized;
                    if (dir != Vector3.zero)
                    {
                        Quaternion lookRot = Quaternion.LookRotation(dir);
                        
                        // Accelerate Z spin
                        currentZSpin += zRotationAcceleration * Time.deltaTime * t;
                        Quaternion zRoll = Quaternion.Euler(0, 0, currentZSpin);
                        
                        IdleRot1.rotation = Quaternion.Slerp(IdleRot1.rotation, lookRot, Time.deltaTime * idleRotationSpeed);
                        IdleRot2.rotation = Quaternion.Slerp(IdleRot2.rotation, lookRot, Time.deltaTime * idleRotationSpeed);
                        IdleRot2.rotation *= zRoll;
                    }
                }

                chargeTimer += Time.deltaTime;
                yield return null;
            }

            // Reset emission after firing
            if (dynamicMaterial != null && dynamicMaterial.HasProperty("_EmissionColor"))
            {
                dynamicMaterial.SetColor("_EmissionColor", originalEmissionColor);
            }

            if (chargeEffect != null) Destroy(chargeEffect);

            // --- Phase 3: Firing ---
            Fire();

            // Short pause after firing before restarting cycle
            yield return new WaitForSeconds(1f);
        }
    }

    private void Fire()
    {
        if (isDead || playerHead == null) return;

        if (audioSource != null && fireSound != null)
        {
            audioSource.PlayOneShot(fireSound);
        }

        Transform[] points = (firePoints != null && firePoints.Length > 0) ? firePoints : new Transform[] { transform };

        foreach (Transform spawnPoint in points)
        {
            if (spawnPoint == null) continue;

            Vector3 directionToPlayer = (playerHead.position - spawnPoint.position).normalized;
            GameObject bullet;

            if (customBulletPrefab != null)
            {
                bullet = Instantiate(customBulletPrefab, spawnPoint.position, Quaternion.LookRotation(directionToPlayer));
            }
            else
            {
                // Fallback to primitive sphere
                bullet = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                bullet.transform.position = spawnPoint.position;
                bullet.transform.localScale = Vector3.one * 0.3f;
                
                Renderer rend = bullet.GetComponent<Renderer>();
                if (rend != null)
                {
                    rend.material.color = Color.red;
                    rend.material.EnableKeyword("_EMISSION");
                    rend.material.SetColor("_EmissionColor", Color.red * 3f);
                }
            }

            // Setup bullet script (assuming EnemyBullet exists as in EnemyShooter)
            EnemyBullet bulletScript = bullet.GetComponent<EnemyBullet>();
            if (bulletScript == null) bulletScript = bullet.AddComponent<EnemyBullet>();
            bulletScript.damage = bulletDamage;

            Rigidbody bulletRb = bullet.GetComponent<Rigidbody>();
            if (bulletRb == null)
            {
                bulletRb = bullet.AddComponent<Rigidbody>();
                bulletRb.useGravity = false;
                bulletRb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            }

            bulletRb.velocity = directionToPlayer * bulletSpeed;
        }
    }

    public void OnEnemyDead()
    {
        isDead = true;
        StopAllCoroutines();
    }
}
