using UnityEngine;

public class Shield : MonoBehaviour
{
    [Header("Reflection Settings")]
    [Tooltip("Prefab for reflected player bullets. If null, a cyan sphere will be generated.")]
    public GameObject playerBulletPrefab;
    
    [Tooltip("Speed of the reflected bullet.")]
    public float reflectSpeed = 20f;

    [Header("VFX Settings")]
    [Tooltip("The power-up particle (e.g. det_01_ad) to show when ammo is full.")]
    public GameObject powerParticle;

    private void Update()
    {
        if (powerParticle == null) return;

        PlayerStatus ps = GetComponentInParent<PlayerStatus>();
        bool full = ps != null && ps.ammo >= ps.maxAmmo;
        if (powerParticle.activeSelf != full)
            powerParticle.SetActive(full);
    }

    /// <summary>
    /// Processes a hit from an EnemyBullet, reflecting it if ammo is full.
    /// </summary>
    public void ProcessHit(EnemyBullet bullet)
    {
        PlayerStatus playerStatus = GetComponentInParent<PlayerStatus>();
        if (playerStatus != null)
        {
            if (playerStatus.ammo >= playerStatus.maxAmmo)
            {
                ReflectBullet(bullet);
            }
            else
            {
                Absorb(bullet.damage);
            }
        }
        else
        {
            // Fallback if no PlayerStatus is found
            Absorb(bullet.damage);
        }

        // Trigger vibration
        ControllerVibration.VibrateLeft(0.6f, 0.05f);
    }

    /// <summary>
    /// Legacy absorb method if needed, but ProcessHit is preferred.
    /// </summary>
    public void Absorb(float incomingDamage)
    {
        Debug.Log($"Shield successfully absorbed {incomingDamage} damage!");
        
        // Find PlayerStatus on the root XR origin
        PlayerStatus playerStatus = GetComponentInParent<PlayerStatus>();
        if (playerStatus != null)
        {
            playerStatus.AddAmmoFromDamage(incomingDamage);
        }
        else
        {
            Debug.LogWarning("Shield absorbed damage but couldn't find PlayerStatus to give ammo to!");
        }
    }

    private void ReflectBullet(EnemyBullet bullet)
    {
        Debug.Log("Ammo full! Shield is reflecting the bullet!");

        // Determine incoming direction from the bullet's velocity or forward vector
        Vector3 incomingDir = bullet.transform.forward;
        Rigidbody bulletRb = bullet.GetComponent<Rigidbody>();
        if (bulletRb != null && bulletRb.velocity.sqrMagnitude > 0.1f)
        {
            incomingDir = bulletRb.velocity.normalized;
        }

        // Reflect using the shield's up as the normal
        Vector3 reflectDir = Vector3.Reflect(incomingDir, transform.up);

        GameObject reflectedBullet;

        if (playerBulletPrefab != null)
        {
            reflectedBullet = Instantiate(playerBulletPrefab, bullet.transform.position, Quaternion.LookRotation(reflectDir));
        }
        else
        {
            // Auto-generate basic PlayerBullet identical to how EnemyShooter works
            reflectedBullet = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            reflectedBullet.transform.position = bullet.transform.position;
            reflectedBullet.transform.localScale = new Vector3(0.15f, 0.15f, 0.15f);

            Renderer rend = reflectedBullet.GetComponent<Renderer>();
            if (rend != null)
            {
                rend.material.color = Color.cyan;
                rend.material.EnableKeyword("_EMISSION");
                rend.material.SetColor("_EmissionColor", Color.cyan * 2f);
            }
            
            Collider col = reflectedBullet.GetComponent<Collider>();
            if (col != null)
            {
                col.isTrigger = true;
            }
        }

        // Attach logic
        PlayerBullet pbScript = reflectedBullet.GetComponent<PlayerBullet>();
        if (pbScript == null) 
        {
            pbScript = reflectedBullet.AddComponent<PlayerBullet>();
        }
        pbScript.damage = bullet.damage; // Same damage as incoming bullet

        Rigidbody rb = reflectedBullet.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = reflectedBullet.AddComponent<Rigidbody>();
            rb.useGravity = false;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous; 
        }

        // Shoot the reflected bullet
        rb.velocity = reflectDir * reflectSpeed;
    }
}
