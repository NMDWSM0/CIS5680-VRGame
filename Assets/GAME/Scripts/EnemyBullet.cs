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

    private void OnTriggerEnter(Collider other)
    {
        // Check if the collided object is the player
        if (other.gameObject.CompareTag("Player"))
        {
            // The PlayerStatus script is likely on the root XR Origin, but the collider hit is on the Main Camera.
            // GetComponentInParent will search the camera and traverse up safely the hierarchy to find it!
            PlayerStatus player = other.gameObject.GetComponentInParent<PlayerStatus>();
            if (player != null)
            {
                player.TakeDamage(damage);
            }
            else
            {
                Debug.LogWarning("EnemyBullet hit an object tagged 'Player', but no PlayerStatus script was found on it or its parents!");
            }
        }
        else
        {
            // If it's not the player, check if it's the Shield
            Shield shield = other.gameObject.GetComponentInParent<Shield>();
            if (shield != null)
            {
                shield.ProcessHit(this);
            }
        }

        // Destroy the bullet on any collision (whether it hit a wall, shield, or player)
        Destroy(gameObject);
    }
}
