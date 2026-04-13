using UnityEngine;

public class PlayerBullet : MonoBehaviour
{
    [Tooltip("How much damage this bullet deals to enemies.")]
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
        // Check if the collided object is an enemy
        if (other.gameObject.CompareTag("Enemy"))
        {
            Enemy enemy = other.gameObject.GetComponentInParent<Enemy>();
            if (enemy != null)
            {
                enemy.Hit(damage);
            }
            else
            {
                Debug.LogWarning("PlayerBullet hit an object tagged 'Enemy', but no Enemy script was found on it or its parents!");
            }
        }

        // Destroy the bullet on any collision
        Destroy(gameObject);
    }
}
