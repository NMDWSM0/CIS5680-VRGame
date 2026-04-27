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

    [Tooltip("If true, the enemy will fall and explode upon hitting the ground instead of exploding immediately.")]
    public bool fallAndExplode = true;

    [Header("UI Setup")]
    [Tooltip("Optional: Assign a 3D TextMeshPro object here. If left null, UI will be auto-generated.")]
    public TextMeshPro healthText;

    [Tooltip("The position of the generated health UI relative to the enemy.")]
    public Vector3 healthUiPosition = new Vector3(0, 1.6f, 0);


    [Header("UI Setup")]
    [Tooltip("Should it generate a physical health bar?")]
    public bool generateHealthBar = true;

    [Header("Audio")]
    [Tooltip("Sound to play when the enemy is destroyed.")]
    public AudioClip deathSound;

    [Tooltip("Volume of the death sound.")]
    [Range(0, 1)] public float deathSoundVolume = 1.0f;

    [Header("Drops")]
    [Tooltip("Prefab for the ammo replenish projectile.")]
    public GameObject ammoReplenishPrefab;

    [Tooltip("How many ammo replenish projectiles to spawn on death.")]
    public int ammoReplenishCount = 3;

    [Header("Hit Effects")]
    [Tooltip("Strength of the knockback impulse applied when hit.")]
    public float knockbackImpulse = 5f;

    [Tooltip("Strength of the random rotation impulse applied when hit.")]
    public float hitTorqueImpulse = 5f;

    [Header("Knockback & Movement")]
    [Tooltip("The target transform the enemy tries to stay aligned with. If null, it will create one automatically.")]
    public Transform targetTransform;

    [Tooltip("How stiff the position spring is.")]
    public float positionSpringStiffness = 10f;

    [Tooltip("How much damping is applied to the position spring.")]
    public float positionDamping = 1f;

    [Tooltip("How stiff the rotation spring is.")]
    public float rotationSpringStiffness = 10f;

    [Tooltip("How much damping is applied to the rotation spring.")]
    public float rotationDamping = 1f;

    private float maxHealth;
    private Transform uiRoot; // A parent object to hold both the text and the bar
    private Transform healthBarForeground; // The green part of the bar that shrinks
    private bool isDead = false;
    private bool isFalling = false;
    private Rigidbody rb;

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

        // Initialize Rigidbody for physics-based knockback
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
            rb.useGravity = false; // Stay in the air
            rb.drag = 1f; // Some air resistance
            rb.angularDrag = 1f;
        }
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        // Initialize target transform if not provided
        if (targetTransform == null)
        {
            GameObject targetObj = new GameObject(gameObject.name + "_MoveTarget");
            targetObj.transform.position = transform.position;
            targetObj.transform.rotation = transform.rotation;
            
            if (transform.parent != null)
                targetObj.transform.SetParent(transform.parent);
                
            targetTransform = targetObj.transform;
        }
    }

    private void FixedUpdate()
    {
        // 0. Handle movement toward targetTransform using forces (not direct position manipulation)
        if (targetTransform != null && !isDead)
        {
            // Position "Spring": Apply force to pull towards target
            Vector3 posError = targetTransform.position - transform.position;
            Vector3 currentVelocity = rb.velocity;

            // TODO calculate force to minimize posError and prevent overshooting
            // 1. 计算弹簧力 (Proportional term)：距离越远，拉力越大
            Vector3 springForce = posError * positionSpringStiffness;
            
            // 2. 计算阻尼力 (Derivative term)：速度越快，反向阻力越大（这就是防止过冲的核心）
            // 注意：如果 targetTransform 也是一个正在移动的刚体，这里最好使用相对速度 (rb.velocity - targetRb.velocity)
            Vector3 dampingForce = -currentVelocity * positionDamping;
            
            // 3. 应用合力
            // 使用 ForceMode.Acceleration 忽略质量影响，这样调节参数时更直观
            rb.AddForce(springForce + dampingForce, ForceMode.Acceleration);

            // --- [旋转控制部分] ---
            // Rotation "Spring": Apply torque to align with target rotation
            Quaternion rotError = targetTransform.rotation * Quaternion.Inverse(transform.rotation);
            rotError.ToAngleAxis(out float angle, out Vector3 axis);
            
            if (angle > 180) angle -= 360; // Get the shortest path
            
            // 确保 Axis 是有效的
            if (!float.IsNaN(axis.x) && !float.IsInfinity(axis.x))
            {
                // 1. 计算旋转弹簧扭矩 (Proportional term)
                // Unity 的角速度是基于弧度(Radians)的，因此我们将角度(Degrees)转为弧度，这样调参手感与位置完全一致
                Vector3 angularError = axis.normalized * (angle * Mathf.Deg2Rad);
                Vector3 springTorque = angularError * rotationSpringStiffness;

                // 2. 计算旋转阻尼扭矩 (Derivative term)
                // 阻力方向与当前角速度方向相反
                Vector3 dampingTorque = -rb.angularVelocity * rotationDamping;

                // 3. 施加合力矩
                // 当角度极小（如小于 0.1度）时，弹簧扭矩几乎为0，此时阻尼扭矩会主导，让物体平稳停下
                rb.AddTorque(springTorque + dampingTorque, ForceMode.Acceleration);
            }
        }
    }

    private void Update()
    {
        // 1. Rotate the entire UI block to face the physical player coordinates
        if (uiRoot != null && Camera.main != null)
        {
            Vector3 directionToFace = uiRoot.position - Camera.main.transform.position;
            
            if (directionToFace != Vector3.zero)
            {
                uiRoot.rotation = Quaternion.LookRotation(directionToFace);
            }
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        // If we are in the falling-death state, trigger the explosion on any impact
        if (isDead && isFalling)
        {
            TriggerGroundExplosion();
        }
    }

    private void TriggerGroundExplosion()
    {
        isFalling = false;

        // Trigger visual/physical explosion via EnemyExplodeBehavior if available
        EnemyExplodeBehavior explodeBehavior = GetComponent<EnemyExplodeBehavior>();
        if (explodeBehavior != null && explodeBehavior.ShouldExplode())
        {
            // Now only perform the shatter step (particles were played on death)
            explodeBehavior.Shatter(bodyCleanupTime, weaponDetachForce, detachOnDeath);
        }
        else
        {
            // Fallback destruction
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Called when the laser or bullet hits this enemy.
    /// </summary>
    public float Hit(float damage, GameObject hitPart = null, Vector3 hitPoint = default)
    {
        float prevHealth = health;

        // Future-proofing: defense calculations will modify realDamage here
        health -= damage;

        float realDamage = Mathf.Max(prevHealth - health, 0);
        Debug.Log($"Enemy '{gameObject.name}' took {realDamage} damage! Remaining health: {health}");

        // Apply knockback if still alive
        if (health > 0 && Camera.main != null && rb != null)
        {
            // Calculate a knockback vector (away from player)
            Vector3 directionFromPlayer = (transform.position - Camera.main.transform.position).normalized;
            
            // 1. Apply a direct linear impulse for consistent push-back
            rb.AddForce(directionFromPlayer * knockbackImpulse, ForceMode.Impulse);

            // 2. Apply a random torque impulse for more dynamic visual impact
            rb.AddTorque(Random.onUnitSphere * hitTorqueImpulse, ForceMode.Impulse);
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
            AudioSource.PlayClipAtPoint(deathSound, transform.position, deathSoundVolume);
        }

        // 2. Hide health UI
        if (uiRoot != null) uiRoot.gameObject.SetActive(false);

        // 3. Play death effects immediately
        EnemyExplodeBehavior explodeBehavior = GetComponent<EnemyExplodeBehavior>();
        if (explodeBehavior != null)
        {
            explodeBehavior.PlayDeathEffect();
        }

        // 4. Handle Death Movement/Physics
        targetTransform = null; // Stop trying to return to the target position
        
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
        }

        if (fallAndExplode)
        {
            Debug.Log($"Enemy '{gameObject.name}' is falling to its doom...");
            isFalling = true;

            // Apply a random initial tumble for a more dramatic fall
            if (rb != null)
            {
                rb.AddTorque(Random.onUnitSphere * hitTorqueImpulse * 2f, ForceMode.Impulse);
            }

            // Disable all other scripts (moving, shooting, etc.) so it's just a falling ragdoll
            MonoBehaviour[] scripts = GetComponents<MonoBehaviour>();
            foreach (var script in scripts)
            {
                if (script != this) script.enabled = false;
            }

            // Set all colliders to non-trigger so it hits the floor
            foreach (Collider col in GetComponentsInChildren<Collider>())
            {
                col.isTrigger = false;
            }
            
            // Clean up if it never hits the ground for some reason (e.g. falls into void)
            Destroy(gameObject, bodyCleanupTime + 5f);
        }
        else
        {
            // Standard immediate death behavior
            if (explodeBehavior != null && explodeBehavior.ShouldExplode())
            {
                explodeBehavior.Shatter(bodyCleanupTime, weaponDetachForce, detachOnDeath);
            }
            else
            {
                FallOver();
            }
        }

        // 4. Handle Detachment
        if (this != null && transform.parent != null)
        {
            transform.SetParent(null);
        }

        // 5. Spawn ammo replenish
        if (ammoReplenishPrefab != null)
        {
            for (int i = 0; i < ammoReplenishCount; i++)
            {
                // Spawn with a slight random offset so they don't overlap perfectly
                Vector3 spawnOffset = Random.insideUnitSphere * 0.5f;
                Instantiate(ammoReplenishPrefab, transform.position + spawnOffset, Quaternion.identity);
            }
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
