using UnityEngine;
using System.Collections;

public class EnemyShooter : MonoBehaviour
{
    [Header("Shooting Properties")]
    [Tooltip("How often the enemy fires a bullet (in seconds).")]
    public float fireRate = 2f;

    [Tooltip("The speed of the bullet.")]
    public float bulletSpeed = 15f;

    [Tooltip("How much damage the bullet deals to the player.")]
    public float bulletDamage = 10f;

    [Header("Setup")]
    [Tooltip("Optional: Custom prefab for the bullet. If null, a primitive sphere is generated automatically.")]
    public GameObject customBulletPrefab;

    [Tooltip("Where the bullet spawns. If null, spawns at this enemy's center.")]
    public Transform firePoint;

    private Transform playerHead;
    private bool isShooting = false;

    private void Start()
    {
        // Automatically try to find the player's Main Camera
        if (Camera.main != null)
        {
            playerHead = Camera.main.transform;
            StartCoroutine(ShootingRoutine());
        }
        else
        {
            Debug.LogWarning("EnemyShooter: Could not find Camera.main! Shooting is offline.");
        }
    }

    private IEnumerator ShootingRoutine()
    {
        isShooting = true;
        
        // Wait a slight random offset so multiple enemies don't fire on the exact same frame like a robot army
        yield return new WaitForSeconds(Random.Range(0f, fireRate));

        while (true)
        {
            if (playerHead != null)
            {
                ShootAtPlayer();
            }
            yield return new WaitForSeconds(fireRate);
        }
    }

    private void ShootAtPlayer()
    {
        Transform spawnPoint = firePoint != null ? firePoint : transform;

        // Calculate direction to the player's head securely
        Vector3 directionToPlayer = (playerHead.position - spawnPoint.position).normalized;

        GameObject bullet;

        if (customBulletPrefab != null)
        {
            bullet = Instantiate(customBulletPrefab, spawnPoint.position, Quaternion.LookRotation(directionToPlayer));
        }
        else
        {
            // Auto-generate a basic physical bullet
            bullet = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            bullet.transform.position = spawnPoint.position;
            bullet.transform.localScale = new Vector3(0.15f, 0.15f, 0.15f);

            Renderer rend = bullet.GetComponent<Renderer>();
            if (rend != null)
            {
                rend.material.color = Color.magenta;
                rend.material.EnableKeyword("_EMISSION");
                rend.material.SetColor("_EmissionColor", Color.magenta * 2f);
            }
        }

        // Apply bullet collision logic and damage stats
        EnemyBullet bulletScript = bullet.GetComponent<EnemyBullet>();
        if (bulletScript == null) 
        {
            bulletScript = bullet.AddComponent<EnemyBullet>();
        }
        bulletScript.damage = bulletDamage;

        // Apply rigorous physical movement requirements
        Rigidbody rb = bullet.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = bullet.AddComponent<Rigidbody>();
            rb.useGravity = false; // Bullets fly straight without dropping like stones
            
            // Continuous collision prevents fast bullets from aggressively clipping clean through thin walls before unity detects them!
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous; 
        }

        // Fire out of the barrel instantly!
        rb.velocity = directionToPlayer * bulletSpeed;
    }
}
