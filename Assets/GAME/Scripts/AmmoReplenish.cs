using UnityEngine;

public class AmmoReplenish : MonoBehaviour
{
    [Tooltip("How much ammo this projectile replenishes when it hits the player.")]
    public float replenishAmount = 5f;

    [Tooltip("How long before the projectile cleans itself up if it misses entirely.")]
    public float lifeTime = 5f;

    [Tooltip("How fast the projectile moves towards the player.")]
    public float speed = 10f;

    [Tooltip("How fast the projectile accelerates.")]
    public float acceleration = 20f;

    public Transform playerTarget;
    private Rigidbody rb;
    private bool hasCollided = false;
    private PlayerStatus hitPlayerRef = null;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
            rb.useGravity = false;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        }

        // Automatically find the player's shield as the target
        if (playerTarget == null)
        {
            GameObject shieldObj = GameObject.Find("Shield");
            if (shieldObj != null)
            {
                playerTarget = shieldObj.transform;
            }
            else
            {
                // Fallback to Main Camera if Shield is not found
                playerTarget = Camera.main?.transform;
            }
        }

        // Automatically destroy if it misses or takes too long
        Destroy(gameObject, lifeTime);
    }

    private void FixedUpdate()
    {
        if (hasCollided || playerTarget == null) return;

        // Calculate direction to the player
        Vector3 direction = (playerTarget.position - transform.position).normalized;
        
        // Gradually increase speed towards the target
        float currentSpeed = rb.velocity.magnitude;
        currentSpeed = Mathf.MoveTowards(currentSpeed, speed, acceleration * Time.fixedDeltaTime);
        
        // Update velocity to fly towards the player
        rb.velocity = direction * currentSpeed;
        
        // Also look at the player for better visual alignment
        if (direction != Vector3.zero)
        {
            rb.MoveRotation(Quaternion.LookRotation(direction));
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasCollided) return;

        // Process collision logic similar to EnemyBullet
        var playerParent = other.transform?.parent?.parent;

        // Check if it hit the shield
        Shield shield = other.gameObject.GetComponentInParent<Shield>();
        if (shield != null)
        {
            hitPlayerRef = shield.GetComponentInParent<PlayerStatus>();
            if (hitPlayerRef != null)
            {
                hasCollided = true;
            }
        }
        // Otherwise check if the collided object is the player
        else if (playerParent && playerParent.gameObject.CompareTag("Player"))
        {
            PlayerStatus player = playerParent.gameObject.GetComponentInParent<PlayerStatus>();
            if (player != null)
            {
                hitPlayerRef = player;
                hasCollided = true;
            }
        }
        else
        {
            // If it hits the environment (walls, floor), it should still be able to fly towards the player 
            // unless we want it to be destroyed on any impact. 
            // Let's make it only destroy on player/shield hit for better usability, 
            // or destroy on anything but the enemy that spawned it.
            
            // Checking if it hit an enemy to avoid self-collision at spawn
            if (other.gameObject.GetComponentInParent<Enemy>() == null)
            {
                // Commenting out the line below so it can bounce off walls or pass through them to reach the player
                // hasCollided = true; 
            }
        }
    }

    private void LateUpdate()
    {
        if (hasCollided)
        {
            // If we successfully identified the player, replenish their ammo
            if (hitPlayerRef != null)
            {
                hitPlayerRef.AddAmmo(replenishAmount);
                
                // Trigger a small vibration on both controllers to notify the player
                ControllerVibration.VibrateLeft(0.3f, 0.1f);
                ControllerVibration.VibrateRight(0.3f, 0.1f);
            }

            // Destroy the projectile on any collision
            Destroy(gameObject);
        }
    }
}
