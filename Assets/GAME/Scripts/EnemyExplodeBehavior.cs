using UnityEngine;

/// <summary>
/// Handles the explosion/shatter behavior for an enemy when it dies.
/// </summary>
public class EnemyExplodeBehavior : MonoBehaviour
{
    [Header("Area Damage (Bomb)")]
    [Tooltip("If true, this enemy will deal damage to nearby enemies upon death.")]
    public bool dealsAreaDamage = false;

    [Tooltip("Amount of damage dealt to nearby enemies.")]
    public float areaDamageAmount = 50f;

    [Tooltip("Radius of the area damage.")]
    public float areaDamageRadius = 5f;

    [Header("Shatter Settings")]
    [Tooltip("Particle effect to play when the enemy dies.")]
    public GameObject deathEffectPrefab;

    [Tooltip("The fragmented version of the enemy. If assigned, this will be instantiated on death.")]
    public GameObject fracturedPrefab;

    [Tooltip("Strength of the shatter explosion.")]
    public float explosionForce = 300f;

    [Tooltip("Radius of the shatter explosion.")]
    public float explosionRadius = 2f;

    [Tooltip("Initial random velocity for each fragment when shattering.")]
    public float fragmentRandomSpeed = 5.0f;

    private bool hasExploded = false;

    public bool ShouldExplode()
    {
        return dealsAreaDamage || fracturedPrefab != null || deathEffectPrefab != null;
    }

    /// <summary>
    /// Executes the shatter logic.
    /// </summary>
    /// <param name="cleanupTime">How long fragments stay in the scene before being destroyed.</param>
    /// <param name="detachForce">Initial velocity given to fragments if they should fly off.</param>
    /// <param name="applyDetachPhysics">Whether to apply the extra detach momentum.</param>
    public void Explode(float cleanupTime, float detachForce, bool applyDetachPhysics)
    {
        if (hasExploded) return;
        hasExploded = true;

        // Area Damage Logic
        if (dealsAreaDamage)
        {
            ApplyAreaDamage();
        }

        // 1. Play death particle effect if assigned
        if (deathEffectPrefab != null)
        {
            Instantiate(deathEffectPrefab, transform.position, transform.rotation);
        }

        if (fracturedPrefab != null)
        {
            Debug.Log($"Enemy '{gameObject.name}' shattering! Swapping to fragments...");

            // Instantiate the fractured prefab
            GameObject fragments = Instantiate(fracturedPrefab, transform.position, transform.rotation);
            
            // Match the scale if the enemy was scaled
            fragments.transform.localScale = transform.localScale;

            // Get all children pieces
            Transform[] pieces = fragments.GetComponentsInChildren<Transform>();

            foreach (Transform piece in pieces)
            {
                if (piece == fragments.transform || piece == null) continue; // Skip the root object

                Rigidbody rb = piece.GetComponent<Rigidbody>();
                if (rb == null)
                {
                    rb = piece.gameObject.AddComponent<Rigidbody>();
                }
                
                rb.useGravity = true;

                // 1. Detach from the fragments root to make it independent
                piece.SetParent(null);

                // 2. Generate a random velocity and torque
                rb.velocity = Random.insideUnitSphere * fragmentRandomSpeed;
                rb.AddTorque(Random.insideUnitSphere * fragmentRandomSpeed, ForceMode.Impulse);

                if (applyDetachPhysics)
                {
                    ApplyDetachPhysicsToPiece(piece, rb, detachForce);
                }
                
                // Apply the main explosion force
                rb.AddExplosionForce(explosionForce, transform.position, explosionRadius);

                // 3. Destroy piece individually after cleanup time
                Destroy(piece.gameObject, cleanupTime);
            }

            // The root fragments object is now empty, destroy it
            Destroy(fragments);
        }

        // Destroy the main enemy object immediately
        Destroy(gameObject);
    }

    private void ApplyAreaDamage()
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, areaDamageRadius);
        foreach (Collider col in colliders)
        {
            // Ignore self
            if (col.gameObject == this.gameObject || col.transform.IsChildOf(this.transform))
                continue;

            // Try to find an IEnemy component in the parent hierarchy
            IEnemy enemy = col.GetComponentInParent<IEnemy>();
            if (enemy != null)
            {
                // Ensure we only hit each enemy once, even if they have multiple colliders
                // By just hitting it, we don't track multiple hits per enemy yet, 
                // but for simple cases it's fine. If an enemy has multiple colliders, 
                // it might take multiple damage instances. 
                // Let's refine it to only damage unique enemies.
                enemy.Hit(areaDamageAmount);
            }
        }
    }

    private void ApplyDetachPhysicsToPiece(Transform piece, Rigidbody rb, float force)
    {
        if (rb == null) return;
        
        rb.isKinematic = false;
        rb.useGravity = true;

        // Calculate direction away from parent center if possible
        Vector3 ejectDir = piece.forward + Vector3.up * 0.5f;
        
        if (transform.parent != null)
        {
            ejectDir = (piece.position - transform.parent.position).normalized + Vector3.up * 0.5f;
        }

        rb.AddForce(ejectDir.normalized * force, ForceMode.Impulse);
        rb.AddTorque(Random.insideUnitSphere * 5f, ForceMode.Impulse);
    }
}
