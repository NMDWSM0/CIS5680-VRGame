using UnityEngine;

public class LaserBeam : MonoBehaviour
{
    [Header("Laser Properties")]
    [Tooltip("How fast the laser stretches forward (units per second)")]
    public float extendSpeed = 50f;

    [Tooltip("Time in seconds before the laser disappears")]
    public float lifeTime = 3.0f;

    [Tooltip("Maximum length the laser will grow to")]
    public float maxDistance = 100f;

    [Header("Visuals")]
    [Tooltip("The visual cylinder child that stretches. If left empty, the script will generate a red primitive cylinder automatically.")]
    public Transform visualCylinder;

    private Transform anchor;
    private float currentLength = 0f;
    private float targetDistance;
    private bool isInitialized = false;
    private float laserDamage = 0f;
    private bool penetrate = false;
    private PlayerStatus playerStatus = null;
    private float penetrationDamageMulti = 0f;

    /// <summary>
    /// Sets up the laser beam, tracking the anchor and auto-generating visuals if needed.
    /// </summary>
    public void Initialize(Transform anchorPoint, float damage, bool penetrateLaser = false, PlayerStatus status = null, float penDamageMulti = 0f)
    {
        this.anchor = anchorPoint;
        this.targetDistance = maxDistance;
        this.laserDamage = damage;
        this.penetrate = penetrateLaser;
        this.playerStatus = status;
        this.penetrationDamageMulti = penDamageMulti;
        
        SetupVisuals();
        ProcessRaycastHits();

        // Destroy this entire GameObject after 'lifeTime' seconds
        Destroy(gameObject, lifeTime);
        isInitialized = true;
    }

    private void SetupVisuals()
    {
        // Auto-generate the visual cylinder if not provided
        if (visualCylinder == null)
        {
            GameObject cyl = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            
            // Remove the collider so it doesn't bump into physics objects unintentionally
            Collider col = cyl.GetComponent<Collider>();
            if (col != null) Destroy(col);
            
            cyl.transform.SetParent(this.transform);
            
            // By default, Unity cylinders are Y-up. We rotate it 90 degrees on X so its Y-axis aims forward (Z).
            cyl.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            
            // Make the base visually thin
            cyl.transform.localScale = new Vector3(0.01f, 0f, 0.01f);
            
            Renderer rend = cyl.GetComponent<Renderer>();
            if (rend != null)
            {
                Shader customGlow = Shader.Find("Custom/LaserGlow");
                if (customGlow != null)
                {
                    rend.material = new Material(customGlow);
                }
                else
                {
                    // Fallback to standard red if shader is somehow not found
                    rend.material.color = Color.red;
                    rend.material.EnableKeyword("_EMISSION");
                    rend.material.SetColor("_EmissionColor", Color.red * 2f);
                }
            }
            visualCylinder = cyl.transform;
        }

        // Initialize length
        if (visualCylinder != null)
        {
            visualCylinder.localScale = new Vector3(visualCylinder.localScale.x, 0f, visualCylinder.localScale.z);
            visualCylinder.localPosition = Vector3.zero;
        }
    }

    private void ProcessRaycastHits()
    {
        // Fire a raycast that hits everything in its path, so we can selectively ignore bullets
        RaycastHit[] hits = Physics.RaycastAll(anchor.position, anchor.forward, maxDistance);
        
        if (penetrate)
        {
            ProcessPenetratingShot(hits);
        }
        else
        {
            ProcessStandardShot(hits);
        }
    }

    private void ProcessPenetratingShot(RaycastHit[] hits)
    {
        // If penetrating, the beam slices through everything up to its max distance.
        targetDistance = maxDistance;

        // Sort hits by distance to know which enemy is hit first
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        bool hitFirstEnemy = false;

        foreach (RaycastHit h in hits)
        {
            // Ignore specific layers
            string layerName = LayerMask.LayerToName(h.collider.gameObject.layer);
            if (layerName == "UI" || layerName == "Player" || layerName == "EnemyBullet" || layerName == "PlayerBullet" || layerName == "Shield")
            {
                continue;
            }

            // Hit all enemies/guns in the beam
            if (h.collider.CompareTag("Enemy") || h.collider.CompareTag("Gun"))
            {
                Enemy enemyScript = h.collider.GetComponentInParent<Enemy>();
                if (enemyScript != null)
                {
                    // Deal full damage to the closest enemy, penetrationDamage percentage to others
                    float dmg = hitFirstEnemy ? (laserDamage * penetrationDamageMulti) : laserDamage;
                    float realDamage = enemyScript.Hit(dmg, h.collider.gameObject);
                    hitFirstEnemy = true;

                    if (playerStatus != null)
                    {
                        playerStatus.OnDamageDealt(realDamage);
                    }
                }
            }
        }
    }

    private void ProcessStandardShot(RaycastHit[] hits)
    {
        // Standard Shot logic (stops at first valid object)
        RaycastHit nearestValidHit = new RaycastHit();
        bool foundValidHit = false;
        float minDistance = float.MaxValue;

        foreach (RaycastHit h in hits)
        {
            // Ignore specific layers
            string layerName = LayerMask.LayerToName(h.collider.gameObject.layer);
            if (layerName == "UI" || layerName == "Player" || layerName == "EnemyBullet" || layerName == "PlayerBullet" || layerName == "Shield")
            {
                continue;
            }

            // Track the closest valid object we hit
            if (h.distance < minDistance)
            {
                minDistance = h.distance;
                nearestValidHit = h;
                foundValidHit = true;
            }
        }

        if (foundValidHit)
        {
            // Set the visual laser to stop exactly there
            targetDistance = nearestValidHit.distance;

            // Check if we hit an enemy body or gun
            if (nearestValidHit.collider.CompareTag("Enemy") || nearestValidHit.collider.CompareTag("Gun"))
            {
                Enemy enemyScript = nearestValidHit.collider.GetComponentInParent<Enemy>();
                if (enemyScript != null)
                {
                    float realDamage = enemyScript.Hit(laserDamage, nearestValidHit.collider.gameObject);
                    if (playerStatus != null)
                    {
                        playerStatus.OnDamageDealt(realDamage);
                    }
                }
            }
        }
    }

    void Update()
    {
        if (!isInitialized) return;

        // Stretch the laser over time up to the target distance
        if (visualCylinder != null && currentLength < targetDistance)
        {
            currentLength += extendSpeed * Time.deltaTime;
            
            // Clamp it so it doesn't overshoot the wall/enemy
            if (currentLength > targetDistance) currentLength = targetDistance;

            // Scale the Y axis of the cylinder (Unity cylinders: scale.y = 1 means 2 meters long)
            visualCylinder.localScale = new Vector3(visualCylinder.localScale.x, currentLength / 2f, visualCylinder.localScale.z);
            
            // Offset the local Z axis so that the base stays perfectly locked at the anchor point.
            visualCylinder.localPosition = new Vector3(0, 0, currentLength / 2f);
        }
    }
}
