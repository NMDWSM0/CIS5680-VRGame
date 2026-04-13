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

    [Tooltip("Optional: The point where the laser spawns. If left null, it will use this script's transform.")]
    public Transform firePoint;

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

        // 3. Shoot the laser
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

        // Initialize it so it anchors to the gun and starts extending
        laserScript.Initialize(spawnPoint);
    }
}
