using UnityEngine;
using TMPro;

public class Enemy : MonoBehaviour
{
    [Header("Stats")]
    [Tooltip("Starting health of the enemy.")]
    public float health = 100f;

    [Header("Effects")]
    [Tooltip("Particle effect to play when the enemy dies.")]
    public GameObject deathEffectPrefab;

    [Tooltip("Initial velocity given to shot-off weapons (only applies if detachOnDeath is true).")]
    public float weaponDetachForce = 15f;

    [Tooltip("If true, this object will detach from its parent and fly off when it dies (useful for weapons that are separate Enemies).")]
    public bool detachOnDeath = false;

    [Header("Death Fragmentation")]
    [Tooltip("The fragmented version of the enemy. If left null, the enemy will simply fall using physics.")]
    public GameObject fracturedPrefab;

    [Tooltip("Strength of the shatter explosion.")]
    public float explosionForce = 300f;

    [Tooltip("Radius of the shatter explosion.")]
    public float explosionRadius = 2f;

    [Tooltip("How long fragments or dead bodies stay in the scene.")]
    public float bodyCleanupTime = 5.0f;

    [Tooltip("Initial random velocity for each fragment when shattering.")]
    public float fragmentRandomSpeed = 5.0f;

    [Header("UI Setup")]
    [Tooltip("Optional: Assign a 3D TextMeshPro object here. If left null, UI will be auto-generated.")]
    public TextMeshPro healthText;

    [Tooltip("The position of the generated health UI relative to the enemy.")]
    public Vector3 healthUiPosition = new Vector3(0, 1.6f, 0);

    [Tooltip("Should it generate a physical health bar?")]
    public bool generateHealthBar = true;

    [Header("Audio")]
    [Tooltip("Sound to play when the enemy is destroyed.")]
    public AudioClip deathSound;

    [Tooltip("Volume of the death sound.")]
    [Range(0, 1)] public float deathSoundVolume = 1.0f;

    private float maxHealth;
    private Transform uiRoot; // A parent object to hold both the text and the bar
    private Transform healthBarForeground; // The green part of the bar that shrinks

    private void Start()
    {
        maxHealth = health;

        // If the user hasn't manually assigned a healthText, we generate the UI hierarchy automatically
        if (healthText == null)
        {
            uiRoot = new GameObject("FloatingHealthUI").transform;
            uiRoot.SetParent(this.transform);
            uiRoot.localPosition = healthUiPosition;

            // 1. Generate Text
            GameObject textObj = new GameObject("HealthText");
            textObj.transform.SetParent(uiRoot);
            textObj.transform.localPosition = Vector3.zero; 
            
            healthText = textObj.AddComponent<TextMeshPro>();
            healthText.alignment = TextAlignmentOptions.Center;
            healthText.fontSize = 3;
            healthText.color = Color.white;
            healthText.outlineWidth = 0.2f;
            healthText.outlineColor = Color.black;

            // 2. Generate Health Bar
            if (generateHealthBar)
            {
                // Shift text up slightly to make room for the bar
                textObj.transform.localPosition = new Vector3(0, 0.2f, 0);

                // Background Bar (Black)
                GameObject bgBar = GameObject.CreatePrimitive(PrimitiveType.Quad);
                Destroy(bgBar.GetComponent<Collider>());
                bgBar.transform.SetParent(uiRoot);
                bgBar.transform.localPosition = Vector3.zero;
                bgBar.transform.localScale = new Vector3(1.0f, 0.15f, 1f);
                bgBar.GetComponent<Renderer>().material.color = Color.black;

                // Foreground Bar (Green)
                GameObject fgBar = GameObject.CreatePrimitive(PrimitiveType.Quad);
                Destroy(fgBar.GetComponent<Collider>());
                fgBar.transform.SetParent(uiRoot);
                
                // Moved slightly "forward" (-0.01 on local Z) so it renders cleanly in front of the black background
                fgBar.transform.localPosition = new Vector3(0, 0, -0.01f);
                fgBar.transform.localScale = new Vector3(1.0f, 0.15f, 1f);
                
                Renderer fgRend = fgBar.GetComponent<Renderer>();
                fgRend.material.color = Color.green;
                fgRend.material.EnableKeyword("_EMISSION");
                fgRend.material.SetColor("_EmissionColor", Color.green * 0.5f);

                healthBarForeground = fgBar.transform;
            }
        }
        else
        {
            // If they provided their own text, we just make it the root for billboarding
            uiRoot = healthText.transform; 
        }

        UpdateHealthUI();
    }

    private void Update()
    {
        // Rotate the entire UI block to face the physical player coordinates
        if (uiRoot != null && Camera.main != null)
        {
            Vector3 directionToFace = uiRoot.position - Camera.main.transform.position;
            
            if (directionToFace != Vector3.zero)
            {
                uiRoot.rotation = Quaternion.LookRotation(directionToFace);
            }
        }
    }

    /// <summary>
    /// Called when the laser or bullet hits this enemy.
    /// </summary>
    public float Hit(float damage, GameObject hitPart = null)
    {
        float prevHealth = health;

        // Future-proofing: defense calculations will modify realDamage here
        health -= damage;

        float realDamage = Mathf.Max(prevHealth - health, 0);
        Debug.Log($"Enemy '{gameObject.name}' took {realDamage} damage! Remaining health: {health}");

        UpdateHealthUI();

        if (health <= 0)
        {
            Death();
        }
        
        return realDamage;
    }

    private void ApplyDetachPhysics(Rigidbody rb)
    {
        if (rb == null) return;
        
        rb.isKinematic = false;
        rb.useGravity = true;

        // Calculate direction away from parent center if possible, otherwise just use forward/up
        Vector3 ejectDir = transform.forward + Vector3.up * 0.5f;
        
        // If we still have a parent reference during this frame (before nulling), move away from it
        if (transform.parent != null)
        {
            ejectDir = (transform.position - transform.parent.position).normalized + Vector3.up * 0.5f;
        }

        rb.AddForce(ejectDir.normalized * weaponDetachForce, ForceMode.Impulse);
        rb.AddTorque(Random.insideUnitSphere * 5f, ForceMode.Impulse);
    }

    /// <summary>
    /// Refreshes the visual text and the health bar scale
    /// </summary>
    private void UpdateHealthUI()
    {
        float clampedHealth = Mathf.Max(0, health);

        // Update Text
        if (healthText != null)
        {
            healthText.text = $"{clampedHealth:F1} / {maxHealth:F1}";
        }

        // Update Bar
        if (healthBarForeground != null)
        {
            float healthPercent = clampedHealth / maxHealth;
            
            // Scale the foreground bar down horizontally
            healthBarForeground.localScale = new Vector3(healthPercent, 0.15f, 1f);
            
            // Because standard quads scale from their center, we move the local X position 
            // slightly to the left proportionally so the bar depletes from Right-to-Left,
            // staying anchored at the left edge.
            float offset = (1f - healthPercent) / 2f;
            healthBarForeground.localPosition = new Vector3(-offset, 0, -0.01f);
            
            // Optional: change color to red when low on health
            if (healthPercent < 0.3f)
            {
                Renderer fgRend = healthBarForeground.GetComponent<Renderer>();
                fgRend.material.color = Color.red;
                fgRend.material.SetColor("_EmissionColor", Color.red * 0.5f);
            }
        }
    }

    /// <summary>
    /// Handles the enemy's logic when health reaches 0.
    /// </summary>
    private void Death()
    {
        // 1. Play death particle effect if assigned
        if (deathEffectPrefab != null)
        {
            Instantiate(deathEffectPrefab, transform.position, transform.rotation);
        }

        // 2. Play death sound
        if (deathSound != null)
        {
            // We use PlayClipAtPoint because the enemy object is about to be destroyed.
            // This creates a temporary audio object in world space.
            AudioSource.PlayClipAtPoint(deathSound, transform.position, deathSoundVolume);
        }

        // 3. Hide health UI
        if (uiRoot != null) uiRoot.gameObject.SetActive(false);

        // 3. Handle Shatter or Physics Fall
        if (fracturedPrefab != null)
        {
            Shatter();
        }
        else
        {
            FallOver();
        }

        // 4. Handle Detachment (always detach on death to stop parent movement influence)
        // If the object was shattered, it's already marked for destruction, but detaching helps if it persists for the rest of the frame.
        // If it fell over, this prevents the parent from dragging the dead body.
        if (this != null && transform.parent != null)
        {
            Debug.Log($"Enemy '{gameObject.name}' destroyed! Detaching from parent...");
            transform.SetParent(null);
        }
    }

    private void Shatter()
    {
        Debug.Log($"Enemy '{gameObject.name}' shattered! Swapping to fragments...");

        // Instantiate the fractured prefab
        GameObject fragments = Instantiate(fracturedPrefab, transform.position, transform.rotation);
        
        // Match the scale if the enemy was scaled
        fragments.transform.localScale = transform.localScale;

        // Get all children first because we will be detaching them
        Transform[] pieces = fragments.GetComponentsInChildren<Transform>();

        // Traverse all child objects to ensure they have Rigidbodies and apply physics
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

            if (detachOnDeath)
            {
                ApplyDetachPhysics(rb); // Give it the initial fly-off momentum
            }
            
            // Apply the main explosion force
            rb.AddExplosionForce(explosionForce, transform.position, explosionRadius);

            // 3. Since the piece is now detached, we must destroy it individually after the cleanup time
            Destroy(piece.gameObject, bodyCleanupTime);
        }

        // The root fragments object is now empty, destroy it
        Destroy(fragments);

        // Destroy the main enemy object immediately
        Destroy(gameObject);
    }

    private void FallOver()
    {
        Debug.Log($"Enemy '{gameObject.name}' falling over...");

        // Disable health text/UI if it still exists
        if (uiRoot != null) Destroy(uiRoot.gameObject);

        // Ensure it has a physical Rigidbody
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }
        
        if (detachOnDeath)
        {
            ApplyDetachPhysics(rb);
        }
        else
        {
            rb.isKinematic = false;
            rb.useGravity = true;
        }

        // Disable all scripts on this object so it stops moving/shooting
        MonoBehaviour[] scripts = GetComponents<MonoBehaviour>();
        foreach (var script in scripts)
        {
            if (script != this) script.enabled = false;
        }

        // Set colliders to non-trigger so it hits the floor
        foreach (Collider col in GetComponentsInChildren<Collider>())
        {
            col.isTrigger = false;
        }

        // Disable this script too so it doesn't keep updating
        this.enabled = false;

        // Clean up the dead body after a delay
        Destroy(gameObject, bodyCleanupTime);
    }
}
