using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.EventSystems; // Required for RaycastResult

public class RightGun : MonoBehaviour
{
    public enum FireMode
    {
        Laser,
        PulseBullet
    }

    [Header("Fire Mode")]
    [Tooltip("Select whether the gun shoots an instant laser or traveling pulse bullets.")]
    public FireMode currentFireMode = FireMode.Laser;

    [Header("Input Setup")]
    [Tooltip("Reference to the input action (e.g. XRI RightHand/Activate).")]
    public InputActionReference triggerAction;

    [Tooltip("If true, holding the trigger will continuously fire the weapon.")]
    public bool autoFire = false;

    private bool isTriggerHeld = false;

    [Tooltip("Reference to the XR Ray Interactor on this controller to check for UI/Interactables.")]
    public XRRayInteractor rayInteractor;

    [Header("Laser Setup")]
    [Tooltip("Optional: A prefab for the laser, which must have the LaserBeam script on it. If left null, one will be auto-generated.")]
    public GameObject laserPrefab;

    [Header("Pulse Bullet Setup")]
    [Tooltip("Optional: A prefab for the pulse bullet. If left null, a cyan pulse will be auto-generated.")]
    public GameObject pulseBulletPrefab;
    
    [Tooltip("Speed of the pulse bullet when in PulseBullet mode.")]
    public float pulseSpeed = 40f;

    [Header("Combat Settings")]
    [Tooltip("How much damage each laser/pulse shot deals to enemies.")]
    public float baseDamage = 10f;

    [Tooltip("Time between shots in seconds.")]
    public float fireRate = 0.2f;

    // advanced features
    [Tooltip("If true, the laser pierces through enemies and hits all of them in its path.")]
    public bool penetrate = false;

    [Tooltip("Damage multiplier for penetration.")]
    public float penetrationDamage = 0.0f;

    [Tooltip("Number of lasers to shoot at once.")]
    public int multishot = 1;

    [Tooltip("If true, the laser will home in on enemies.")]
    public bool homing = false;

    // visuals
    [Tooltip("Optional: The point where the laser spawns. If left null, it will use this script's transform.")]
    public Transform firePoint;

    [Header("Player Setup")]
    [Tooltip("Reference to the player's status script to manage ammunition. Automatically attempts to find it if left null.")]
    public PlayerStatus playerStatus;

    [Header("Reticle Setup")]
    [Tooltip("Optional: A visual object (like a red dot) to show where the gun is aiming. If null, a tiny red sphere is automatically generated.")]
    public GameObject reticleVisual;

    [Header("Audio Setup")]
    [Tooltip("Sound to play when shooting.")]
    public AudioClip shootSound;

    private AudioSource audioSource;

    private float lastFireTime = -9999f;

    private void Start()
    {
        // Setup AudioSource
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }

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
            triggerAction.action.canceled += OnTriggerCanceled;
            triggerAction.action.Enable();
        }
    }

    private void OnDisable()
    {
        if (triggerAction != null && triggerAction.action != null)
        {
            triggerAction.action.performed -= OnTriggerPerformed;
            triggerAction.action.canceled -= OnTriggerCanceled;
        }
    }

    private void OnTriggerPerformed(InputAction.CallbackContext context)
    {
        isTriggerHeld = true;
        
        // If not auto-firing, we fire exactly once when the trigger is pulled
        if (!autoFire)
        {
            TryFire();
        }
    }

    private void OnTriggerCanceled(InputAction.CallbackContext context)
    {
        isTriggerHeld = false;
    }

    private void TryFire()
    {
        if (Time.time - lastFireTime < fireRate)
        {
            return; // Still cooling down
        }

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
            if (!playerStatus.TryConsumeAmmo(1 + 0.1f * (multishot - 1)))
            {
                // We are out of ammo! The PlayerStatus script handles the Debug.Log.
                return;
            }
        }
        else
        {
            Debug.LogWarning("RightGun: No PlayerStatus found! Free firing allowed.");
        }

        // 4. Shoot the weapon
        FireWeapon();
        
        // 5. Update last fire time
        lastFireTime = Time.time;
    }

    private void FireWeapon()
    {
        Transform spawnPoint = firePoint != null ? firePoint : transform;

        // Calculate dynamic damage applying multipliers and crit
        float finalDamage = CalculateFinalDamage();

        for (int i = 0; i < multishot; i++)
        {
            float angleOffset = (i - (multishot - 1) / 2f) * (multishot <= 3 ? 15f : (multishot <= 5 ? 10f : 8.5f));
            Quaternion spawnRot = spawnPoint.rotation * Quaternion.Euler(0, angleOffset, 0);

            if (homing)
            {
                Vector3 currentForward = spawnRot * Vector3.forward;
                float closestDist = 100f; // maxDistance
                Vector3 bestTargetPos = Vector3.zero;
                bool foundTarget = false;

                Collider[] hitColliders = Physics.OverlapSphere(spawnPoint.position, 100f);
                foreach (Collider col in hitColliders)
                {
                    if (col.CompareTag("Enemy") || col.CompareTag("Gun"))
                    {
                        Vector3 targetPos = col.bounds.center;
                        Vector3 toTarget = targetPos - spawnPoint.position;
                        float dist = toTarget.magnitude;
                        float smallAngle = 15f;

                        if (dist < closestDist)
                        {
                            float angle = Vector3.Angle(currentForward, toTarget);
                            if (angle <= smallAngle)
                            {
                                smallAngle = angle;
                                bestTargetPos = targetPos;
                                foundTarget = true;
                            }
                        }
                    }
                }

                if (foundTarget)
                {
                    Vector3 toTarget = bestTargetPos - spawnPoint.position;
                    spawnRot = Quaternion.LookRotation(toTarget);
                }
            }

            if (currentFireMode == FireMode.Laser)
            {
                GameObject laserObj;

                // Instantiate or create the laser
                if (laserPrefab != null)
                {
                    laserObj = Instantiate(laserPrefab, spawnPoint.position, spawnRot);
                }
                else
                {
                    laserObj = new GameObject("Generated Laser Beam");
                    laserObj.transform.position = spawnPoint.position;
                    laserObj.transform.rotation = spawnRot;
                }

                // Ensure it has the LaserBeam script to manage stretching/anchoring
                LaserBeam laserScript = laserObj.GetComponent<LaserBeam>();
                if (laserScript == null)
                {
                    laserScript = laserObj.AddComponent<LaserBeam>();
                }

                // Initialize it using its own transform so it tracks its specific angled raycast correctly
                laserScript.Initialize(laserObj.transform, finalDamage, penetrate, playerStatus, penetrationDamage);
            }
            else if (currentFireMode == FireMode.PulseBullet)
            {
                GameObject pulseObj;

                // Instantiate or create the pulse bullet
                if (pulseBulletPrefab != null)
                {
                    pulseObj = Instantiate(pulseBulletPrefab, spawnPoint.position, spawnRot);
                }
                else
                {
                    pulseObj = new GameObject("Generated Pulse Bullet");
                    pulseObj.transform.position = spawnPoint.position;
                    pulseObj.transform.rotation = spawnRot;
                }

                // Ensure it has the PulseBullet script
                PulseBullet pulseScript = pulseObj.GetComponent<PulseBullet>();
                if (pulseScript == null)
                {
                    pulseScript = pulseObj.AddComponent<PulseBullet>();
                }

                // Initialize pulse
                pulseScript.Initialize(finalDamage, penetrate, playerStatus, penetrationDamage, pulseSpeed);
            }
        }

        // Play shoot audio
        if (shootSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(shootSound);
        }

        // Trigger vibration
        ControllerVibration.VibrateRight(0.9f, 0.03f);
    }

    /// <summary>
    /// Computes damage using PlayerStatus modifiers like damageMultiplier and critRate.
    /// </summary>
    private float CalculateFinalDamage()
    {
        float damage = baseDamage;
        
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
        // Handle Auto-Fire continuous shooting
        if (autoFire && isTriggerHeld)
        {
            TryFire();
        }

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
