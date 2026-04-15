using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.EventSystems; // Required for RaycastResult

public class RightGun : MonoBehaviour
{
    [Header("Input Setup")]
    [Tooltip("Reference to the input action (e.g. XRI RightHand/Activate).")]
    public InputActionReference triggerAction;

    [Tooltip("Reference to the XR Ray Interactor on this controller to check for UI/Interactables.")]
    public XRRayInteractor rayInteractor;

    [Header("Laser Setup")]
    [Tooltip("Optional: A prefab for the laser, which must have the LaserBeam script on it. If left null, one will be auto-generated.")]
    public GameObject laserPrefab;

    [Tooltip("How much damage each laser shot deals to enemies.")]
    public float laserDamage = 25f;

    [Tooltip("If true, the laser pierces through enemies and hits all of them in its path.")]
    public bool penetrate = false;

    [Tooltip("Optional: The point where the laser spawns. If left null, it will use this script's transform.")]
    public Transform firePoint;

    [Header("Player Setup")]
    [Tooltip("Reference to the player's status script to manage ammunition. Automatically attempts to find it if left null.")]
    public PlayerStatus playerStatus;

    [Header("Reticle Setup")]
    [Tooltip("Optional: A visual object (like a red dot) to show where the gun is aiming. If null, a tiny red sphere is automatically generated.")]
    public GameObject reticleVisual;

    private void Start()
    {
        // Try to automatically find the PlayerStatus if not manually assigned
        if (playerStatus == null)
        {
            playerStatus = GetComponentInParent<PlayerStatus>();
        }

        // Auto-generate a basic red dot if the user didn't assign a custom one
        if (reticleVisual == null)
        {
            reticleVisual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            
            // Remove collider so it doesn't block physics or laser shots
            Collider col = reticleVisual.GetComponent<Collider>();
            if (col != null) Destroy(col);

            // Make it very small
            reticleVisual.transform.localScale = new Vector3(0.04f, 0.04f, 0.04f);
            
            // Make it glow red
            Renderer rend = reticleVisual.GetComponent<Renderer>();
            if (rend != null)
            {
                rend.material.color = Color.red;
                rend.material.EnableKeyword("_EMISSION");
                rend.material.SetColor("_EmissionColor", Color.red * 2f);
            }
            
            // Start hidden
            reticleVisual.SetActive(false);
        }
    }

    private void OnEnable()
    {
        if (triggerAction != null && triggerAction.action != null)
        {
            triggerAction.action.performed += OnTriggerPerformed;
            triggerAction.action.Enable();
        }
    }

    private void OnDisable()
    {
        if (triggerAction != null && triggerAction.action != null)
        {
            triggerAction.action.performed -= OnTriggerPerformed;
        }
    }

    private void OnTriggerPerformed(InputAction.CallbackContext context)
    {
        if (rayInteractor != null)
        {
            // 1. Check if we are hovering over any UI Canvas Element
            if (rayInteractor.TryGetCurrentUIRaycastResult(out RaycastResult uiResult))
            {
                return; // Abort if pointing at UI
            }

            // 2. Check if we are hovering over or holding any 3D interactable object
            if (rayInteractor.interactablesHovered.Count > 0 || rayInteractor.hasSelection)
            {
                return; // Abort if holding or pointing at interactable
            }
        }
        else
        {
            Debug.LogWarning("RightGun: RayInteractor is not assigned! UI checking bypassed.");
        }

        // 3. Check and consume ammunition
        if (playerStatus != null)
        {
            if (!playerStatus.TryConsumeAmmo())
            {
                // We are out of ammo! The PlayerStatus script handles the Debug.Log.
                return;
            }
        }
        else
        {
            Debug.LogWarning("RightGun: No PlayerStatus found! Free firing allowed.");
        }

        // 4. Shoot the laser
        ShootLaser();
    }

    private void ShootLaser()
    {
        Transform spawnPoint = firePoint != null ? firePoint : transform;

        GameObject laserObj;

        // Instantiate or create the laser
        if (laserPrefab != null)
        {
            laserObj = Instantiate(laserPrefab, spawnPoint.position, spawnPoint.rotation);
        }
        else
        {
            laserObj = new GameObject("Generated Laser Beam");
            laserObj.transform.position = spawnPoint.position;
            laserObj.transform.rotation = spawnPoint.rotation;
        }

        // Ensure it has the LaserBeam script to manage stretching/anchoring
        LaserBeam laserScript = laserObj.GetComponent<LaserBeam>();
        if (laserScript == null)
        {
            laserScript = laserObj.AddComponent<LaserBeam>();
        }

        // Calculate dynamic damage applying multipliers and crit
        float finalDamage = CalculateFinalDamage();

        // Initialize it so it anchors to the gun and starts extending
        laserScript.Initialize(spawnPoint, finalDamage, penetrate, playerStatus);
    }

    /// <summary>
    /// Computes damage using PlayerStatus modifiers like damageMultiplier and critRate.
    /// </summary>
    private float CalculateFinalDamage()
    {
        float damage = laserDamage;
        
        if (playerStatus != null)
        {
            // Apply straight multiplier
            damage *= playerStatus.damageMultiplier;
            
            // Apply critical hit (critRate is 0 to 1)
            if (Random.value < playerStatus.critRate)
            {
                damage *= 2.0f; // Standard 2x crit multiplier
                Debug.Log($"Critical Hit! Damage modified to {damage}");
            }
        }
        
        return damage;
    }

    private void Update()
    {
        // Manage the laser sight / red dot position every frame
        if (rayInteractor != null && reticleVisual != null)
        {
            // If the ray interactor is pointing at a UI element, hide the world-dot
            if (rayInteractor.TryGetCurrentUIRaycastResult(out RaycastResult uiResult))
            {
                if (reticleVisual.activeSelf) reticleVisual.SetActive(false);
                return;
            }

            // Check where the ray interactor is currently hitting in the 3D world
            if (rayInteractor.TryGetCurrent3DRaycastHit(out RaycastHit hit))
            {
                if (!reticleVisual.activeSelf) reticleVisual.SetActive(true);
                
                // Move the dot perfectly to the hit point
                reticleVisual.transform.position = hit.point;
                // Optional: orient the dot so it lays flat against the surface it hit
                reticleVisual.transform.rotation = Quaternion.LookRotation(hit.normal);
            }
            else
            {
                // We are aiming into the empty sky
                if (reticleVisual.activeSelf) reticleVisual.SetActive(false);
            }
        }
    }
}
