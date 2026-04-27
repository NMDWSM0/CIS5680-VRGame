using UnityEngine;
using System.Collections;

public class EnemyShooter : MonoBehaviour
{
    [Header("Shooting Properties")]
    [Tooltip("Number of bullets fired in one burst sequence.")]
    public int bulletsPerBurst = 3;

    [Tooltip("Time between individual bullets in a burst (发射时间).")]
    public float fireInterval = 0.1f;

    [Tooltip("Time between bursts (间歇时间).")]
    public float restInterval = 2f;

    [Tooltip("Random range added to restInterval (随机范围).")]
    public float restIntervalRandom = 0.0f;

    [Tooltip("The speed of the bullet.")]
    public float bulletSpeed = 15f;

    [Tooltip("How much damage the bullet deals to the player.")]
    public float bulletDamage = 10f;

    [Tooltip("Maximum angular velocity (degrees/sec magnitude) allowed to shoot. If spinning faster, firing is blocked.")]
    public float maxAngularVelocity = 0.1f;

    [Header("Setup")]
    [Tooltip("Optional: Custom prefab for the bullet. If null, a primitive sphere is generated automatically.")]
    public GameObject customBulletPrefab;

    [Tooltip("Where the bullets spawn. If empty, spawns at this enemy's center.")]
    public Transform[] firePoints;

    [Tooltip("Optional: Prefab for the muzzle flash effect.")]
    public GameObject muzzleFlashPrefab;

    [Tooltip("Audio to play when firing.")]
    public AudioSource audioSource;
    public Enemy enemy;
    private Transform playerHead;
    private bool isShooting = false;
    private Rigidbody rb;

    private void Start()
    {
        if(enemy){
            
            rb = enemy.GetComponent<Rigidbody>();
        }

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

    private void Update()
    {
        if (playerHead != null)
        {
            // Calculate direction to player's head
            Vector3 direction = (playerHead.position - transform.position).normalized;
            
            if (direction != Vector3.zero)
            {
                // Quaternion.LookRotation with Vector3.up as the second parameter 
                // ensures the Z-axis points at the player and the X-axis stays horizontal.
                transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
            }
        }
    }

    private IEnumerator ShootingRoutine()
    {
        isShooting = true;
        
        // Wait a slight random offset so multiple enemies don't fire on the exact same frame
        yield return new WaitForSeconds(0.5f * restInterval + Random.Range(0f, restInterval));

        while (true)
        {
            if (playerHead != null)
            {
                for (int i = 0; i < bulletsPerBurst; i++)
                {
                    ShootAtPlayer();

                    // Wait between shots in the burst, except after the last one
                    if (i < bulletsPerBurst - 1)
                    {
                        yield return new WaitForSeconds(fireInterval);
                    }
                }
            }
            // Wait for the rest interval before starting the next burst
            yield return new WaitForSeconds(restInterval + Random.Range(0f, restIntervalRandom));
        }
    }

    private void ShootAtPlayer()
    {
        // Debug.Log("Angular velocity: " + rb.angularVelocity.magnitude);
        // 0. Check if we are spinning too fast to aim/fire
        if (rb != null && rb.angularVelocity.magnitude > maxAngularVelocity)
        {
            return;
        }

        // 1. Filter out fire points that have been detached (no longer children of this enemy)
        Transform[] allPoints = firePoints != null && firePoints.Length > 0 ? firePoints : new Transform[] { transform };
        
        System.Collections.Generic.List<Transform> activePoints = new System.Collections.Generic.List<Transform>();
        foreach (Transform p in allPoints)
        {
            // If the point is null or no longer a child of this object/root, skip it
            if (p != null && (p == transform || p.IsChildOf(this.transform)))
            {
                activePoints.Add(p);
            }
        }

        // If no weapons left, don't fire or play sound
        if (activePoints.Count == 0) return;

        // 2. Play shooting sound (using PlayClipAtPoint to ensure it plays at world position)
        if (audioSource != null && audioSource.clip != null)
        {
            AudioSource.PlayClipAtPoint(audioSource.clip, transform.position, audioSource.volume);
        }

        // 3. Fire from all active points
        foreach (Transform spawnPoint in activePoints)
        {
            // Spawn muzzle flash if provided
            if (muzzleFlashPrefab != null)
            {
                // Instantiate at the muzzle position with its rotation
                GameObject flash = Instantiate(muzzleFlashPrefab, spawnPoint.position, spawnPoint.rotation);
                
                // Parent it to the spawn point so it moves with the gun if it's tracking fast
                flash.transform.SetParent(spawnPoint);
            }

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
                rb.useGravity = false; 
                rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            }

            // Fire out of the barrel instantly!
            rb.velocity = directionToPlayer * bulletSpeed;
        }
    }

    /// <summary>
    /// Called via SendMessage from the Enemy script when it dies.
    /// </summary>
    public void OnEnemyDead()
    {
        // Stop the ShootingRoutine immediately
        StopAllCoroutines();
        isShooting = false;
    }
}
