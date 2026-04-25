using UnityEngine;
using TMPro;

public class Enemy : MonoBehaviour, IEnemy
{
    [Header("Stats")]
    [Tooltip("Starting health of the enemy.")]
    public float health = 100f;

    [Header("Death Physics")]
    [Tooltip("Initial velocity given to shot-off weapons or fragments (only applies if detachOnDeath is true).")]
    public float weaponDetachForce = 15f;

    [Tooltip("If true, this object will detach from its parent and fly off when it dies.")]
    public bool detachOnDeath = false;

    [Tooltip("How long fragments or dead bodies stay in the scene.")]
    public float bodyCleanupTime = 5.0f;

    [Header("UI Setup")]
    [Tooltip("Optional: Assign a 3D TextMeshPro object here. If left null, UI will be auto-generated.")]
    public TextMeshPro healthText;

    [Tooltip("The position of the generated health UI relative to the enemy.")]
    public Vector3 healthUiPosition = new Vector3(0, 1.6f, 0);

    [Header("Hit Effects")]
    [Tooltip("How fast the enemy moves back when hit.")]
    public float knockbackSpeed = 8f;

    [Tooltip("How far the enemy moves back when hit.")]
    public float knockbackDistance = 0.6f;

    [Header("UI Setup")]
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
    private bool isDead = false;

    // Knockback state
    private Vector3 knockbackDir;
    private float knockbackDistanceRemaining;

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

                // Unity strips the default primitive shader in builds, causing magenta/purple materials. 
                // To safely bypass this via script, we steal a compiled material from the enemy itself.
                Material safeMat = null;
                Renderer safeRend = GetComponentInChildren<Renderer>();
                if (safeRend != null && safeRend.sharedMaterial != null)
                {
                    safeMat = new Material(safeRend.sharedMaterial);
                    // Remove any textures so it becomes a solid color material
                    if (safeMat.HasProperty("_MainTex")) safeMat.SetTexture("_MainTex", null);
                    if (safeMat.HasProperty("_BaseMap")) safeMat.SetTexture("_BaseMap", null);
                }

                // Background Bar (Black)
                GameObject bgBar = GameObject.CreatePrimitive(PrimitiveType.Quad);
                Destroy(bgBar.GetComponent<Collider>());
                bgBar.transform.SetParent(uiRoot);
                bgBar.transform.localPosition = Vector3.zero;
                bgBar.transform.localScale = new Vector3(1.0f, 0.15f, 1f);
                
                Renderer bgRend = bgBar.GetComponent<Renderer>();
                if (safeMat != null) bgRend.material = new Material(safeMat);
                bgRend.material.color = Color.black;

                // Foreground Bar (Green)
                GameObject fgBar = GameObject.CreatePrimitive(PrimitiveType.Quad);
                Destroy(fgBar.GetComponent<Collider>());
                fgBar.transform.SetParent(uiRoot);
                
                // Moved slightly "forward" (-0.01 on local Z) so it renders cleanly in front of the black background
                fgBar.transform.localPosition = new Vector3(0, 0, -0.01f);
                fgBar.transform.localScale = new Vector3(1.0f, 0.15f, 1f);
                
                Renderer fgRend = fgBar.GetComponent<Renderer>();
                if (safeMat != null) fgRend.material = new Material(safeMat);

                fgRend.material.color = Color.green;
                fgRend.material.EnableKeyword("_EMISSION");
                if (fgRend.material.HasProperty("_EmissionColor"))
                {
                    fgRend.material.SetColor("_EmissionColor", Color.green * 0.5f);
                }

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
        transform.position += knockbackDir;

        knockbackDir *= 0.3f;
        // Apply knockback displacement after movement scripts (Update) have finished
        // if (true||knockbackDistanceRemaining > 0)
        // {
        //     float step = knockbackSpeed * Time.deltaTime;

        //     // Clamp step to avoid overshooting the target distance
        //     if (step > knockbackDistanceRemaining)
        //         step = knockbackDistanceRemaining;

        //     transform.position += knockbackDir;
        //     knockbackDistanceRemaining -= step;
        // }
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

        // Apply knockback if still alive
        if (health > 0 && Camera.main != null)
        {
            knockbackDir = transform.position.normalized / Vector3.Magnitude(transform.position)*20.0f;
            knockbackDistanceRemaining = knockbackDistance;
        }

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
        if (isDead) return;
        isDead = true;

        // Broadcast that the enemy is dead so other components (like EnemyShooter) can react
        SendMessage("OnEnemyDead", SendMessageOptions.DontRequireReceiver);

        // 1. Play death sound
        if (deathSound != null)
        {
            // We use PlayClipAtPoint because the enemy object is about to be destroyed.
            // This creates a temporary audio object in world space.
            AudioSource.PlayClipAtPoint(deathSound, transform.position, deathSoundVolume);
        }

        // 3. Hide health UI
        if (uiRoot != null) uiRoot.gameObject.SetActive(false);

        // 3. Handle Shatter or Physics Fall
        EnemyExplodeBehavior explodeBehavior = GetComponent<EnemyExplodeBehavior>();
        if (explodeBehavior != null && explodeBehavior.ShouldExplode())
        {
            explodeBehavior.Explode(bodyCleanupTime, weaponDetachForce, detachOnDeath);
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
