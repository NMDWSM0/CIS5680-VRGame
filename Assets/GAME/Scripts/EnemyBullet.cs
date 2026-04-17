using UnityEngine;

public class EnemyBullet : MonoBehaviour
{
    [Tooltip("How much damage this bullet deals to the player.")]
    public float damage = 10f;

    [Tooltip("How long before the bullet cleans itself up if it misses entirely.")]
    public float lifeTime = 5f;

    private void Start()
    {
        // Automatically destroy if it flies off into space
        Destroy(gameObject, lifeTime);
    }

    private bool hasCollided = false;
    private Shield hitShieldRef = null;
    private PlayerStatus hitPlayerRef = null;

    private void OnTriggerEnter(Collider other)
    {
        hasCollided = true;

        var playerParent = other.transform?.parent?.parent;

        // Check if it's the Shield
        Shield shield = other.gameObject.GetComponentInParent<Shield>();
        if (shield != null)
        {
            hitShieldRef = shield;
        }
        // Otherwise check if the collided object is the player
        else if (playerParent && playerParent.gameObject.CompareTag("Player"))
        {
            // GetComponentInParent will search the camera and traverse up safely to find it!
            PlayerStatus player = playerParent.gameObject.GetComponentInParent<PlayerStatus>();
            if (player != null)
            {
                hitPlayerRef = player;
            }
            else
            {
                Debug.LogWarning("EnemyBullet hit an object tagged 'Player', but no PlayerStatus script was found on it or its parents!");
            }
        }
    }

    private void LateUpdate()
    {
        if (hasCollided)
        {
            // Process the hit with priority given to the shield
            if (hitShieldRef != null)
            {
                hitShieldRef.ProcessHit(this);
            }
            else if (hitPlayerRef != null)
            {
                hitPlayerRef.TakeDamage(damage);
            }

            // Destroy the bullet on any collision (whether it hit a wall, shield, or player)
            Destroy(gameObject);
        }
    }
}
