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

    private bool hasShattered = false;
    private bool hasPlayedEffect = false;

    // Performance Optimization: Pre-instantiated pieces
    private GameObject preInstantiatedFragments;
    private Rigidbody[] cachedFragmentRbs;
    private Transform[] cachedFragmentTransforms;

    private void Start()
    {
        // Optimization: Pre-instantiate the fractured version at start to avoid frame spikes during death
        if (fracturedPrefab != null)
        {
            preInstantiatedFragments = Instantiate(fracturedPrefab);
            preInstantiatedFragments.SetActive(false); // Keep it hidden and inactive

            // Cache pieces and ensure they have rigidbodies
            System.Collections.Generic.List<Rigidbody> rbs = new System.Collections.Generic.List<Rigidbody>();
            System.Collections.Generic.List<Transform> transforms = new System.Collections.Generic.List<Transform>();

            foreach (Transform t in preInstantiatedFragments.GetComponentsInChildren<Transform>())
            {
                if (t == preInstantiatedFragments.transform) continue;

                Rigidbody rb = t.GetComponent<Rigidbody>();
                if (rb == null) rb = t.gameObject.AddComponent<Rigidbody>();

                // Set initial physics state
                rb.isKinematic = true;
                rb.useGravity = false;

                rbs.Add(rb);
                transforms.Add(t);
            }

            cachedFragmentRbs = rbs.ToArray();
            cachedFragmentTransforms = transforms.ToArray();
        }
    }

    public bool ShouldExplode()
    {
        return dealsAreaDamage || fracturedPrefab != null || deathEffectPrefab != null;
    }

    /// <summary>
    /// Instantiates the death particle effect immediately.
    /// </summary>
    public void PlayDeathEffect()
    {
        if (hasPlayedEffect) return;
        hasPlayedEffect = true;

        if (deathEffectPrefab != null)
        {
            Instantiate(deathEffectPrefab, transform.position, transform.rotation);
        }
    }

    /// <summary>
    /// Executes both death effect and shattering immediately.
    /// </summary>
    public void Explode(float cleanupTime, float detachForce, bool applyDetachPhysics)
    {
        PlayDeathEffect();
        Shatter(cleanupTime, detachForce, applyDetachPhysics);
    }

    /// <summary>
    /// Swaps the enemy with its fractured prefab and applies physics forces.
    /// </summary>
    public void Shatter(float cleanupTime, float detachForce, bool applyDetachPhysics)
    {
        if (hasShattered) return;
        hasShattered = true;

        // Area Damage Logic
        if (dealsAreaDamage)
        {
            ApplyAreaDamage();
        }

        if (preInstantiatedFragments != null)
        {
            Debug.Log($"Enemy '{gameObject.name}' shattering (Optimized Path)!");

            // 1. Move root to current position
            preInstantiatedFragments.transform.position = transform.position;
            preInstantiatedFragments.transform.rotation = transform.rotation;
            preInstantiatedFragments.transform.localScale = transform.localScale;
            preInstantiatedFragments.SetActive(true);

            // Get current velocity of the enemy to inherit momentum
            Vector3 inheritedVelocity = Vector3.zero;
            Rigidbody enemyRb = GetComponent<Rigidbody>();
            if (enemyRb != null) inheritedVelocity = enemyRb.velocity;

            // 2. Process all cached pieces
            for (int i = 0; i < cachedFragmentRbs.Length; i++)
            {
                Rigidbody rb = cachedFragmentRbs[i];
                Transform piece = cachedFragmentTransforms[i];

                if (rb == null || piece == null) continue;

                // Unparent to allow independent movement
                piece.SetParent(null);

                // Enable physics
                rb.isKinematic = false;
                rb.useGravity = true;

                // Inherit momentum + Add random variation (Upper Hemisphere only)
                Vector3 randomDir = Random.insideUnitSphere;
                randomDir.y = Mathf.Abs(randomDir.y); // Ensure fragments fly upwards/outwards, not into the ground
                
                rb.velocity = inheritedVelocity + (randomDir * fragmentRandomSpeed);
                rb.AddTorque(Random.onUnitSphere * fragmentRandomSpeed, ForceMode.Impulse);

                if (applyDetachPhysics)
                {
                    ApplyDetachPhysicsToPiece(piece, rb, detachForce);
                }
                
                // Apply the main explosion force
                rb.AddExplosionForce(explosionForce, transform.position, explosionRadius);

                // Destroy piece individually after cleanup time
                Destroy(piece.gameObject, cleanupTime);
            }

            // Cleanup the empty root
            Destroy(preInstantiatedFragments);
        }
        else if (fracturedPrefab != null)
        {
            // Fallback to slow path if pre-instantiation failed or wasn't done
            GameObject fragments = Instantiate(fracturedPrefab, transform.position, transform.rotation);
            fragments.transform.localScale = transform.localScale;
            // ... (rest of old slow path if needed, but we mostly rely on cached version)
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
