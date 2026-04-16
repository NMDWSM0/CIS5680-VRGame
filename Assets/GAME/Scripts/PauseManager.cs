using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;
using UnityEngine.XR.Interaction.Toolkit;

public class PauseManager : MonoBehaviour
{
    [Header("Input Setup")]
    [Tooltip("Reference to the input action (e.g., LeftHand/Menu button).")]
    public InputActionReference pauseButtonAction;

    [Header("Render Feature")]
    [Tooltip("Reference to the Universal Renderer Data.")]
    public UniversalRendererData rendererData;

    [Tooltip("The layer name of the hands and UI.")]
    public string targetLayerName = "HandAndPauseUI";

    [Header("Ray Interactors")]
    [Tooltip("The XR Ray Interactors to restrict when the game is paused.")]
    public XRRayInteractor[] rayInteractors;
    
    private int[] originalRaycastMasks;

    [Header("UI Settings")]
    [Tooltip("The Pause UI Prefab to spawn.")]
    public GameObject pauseUIPrefab;
    
    [Tooltip("Distance in front of the player to spawn the UI.")]
    public float spawnDistance = 2.0f;

    [Tooltip("The layer that darkens the background.")]
    public GameObject darkenLayer;

    private GameObject currentPauseUI;

    private static PauseManager instance;

    private string[] targetFeatureNames = new string[] 
    { 
        "ClearDepthForHandUI",
        "DrawHandsAndUIOpaque",
        "DrawHandsAndUITransparent"
    };

    private void Start()
    {
        instance = this;
        ToggleVRRenderFeatures(false);
        ToggleDarkenLayer(false);
    }

    private void OnEnable()
    {
        if (pauseButtonAction != null && pauseButtonAction.action != null)
        {
            pauseButtonAction.action.performed += OnTriggerPerformed;
            pauseButtonAction.action.Enable();
        }
    }

    private void OnDisable()
    {
        if (pauseButtonAction != null && pauseButtonAction.action != null)
        {
            pauseButtonAction.action.performed -= OnTriggerPerformed;
        }
    }

    private void OnTriggerPerformed(InputAction.CallbackContext context)
    {
        // Don't pause or spawn if already paused
        if (Time.timeScale < 0.1f || currentPauseUI != null) return;

        // Setting time scale to 0 completely freezes all physics, updates (using Time.deltaTime), and animations.
        TogglePause(true);
        Debug.Log("Game Paused");

        if (pauseUIPrefab != null)
        {
            // Find player camera
            Transform playerCamera = Camera.main != null ? Camera.main.transform : transform;
            
            // Calculate spawn position in front of the camera
            // Flatten the forward vector so the UI doesn't spawn tilted up/down into the floor/ceiling
            Vector3 flatForward = playerCamera.forward;
            flatForward.y = 0;
            if (flatForward.sqrMagnitude < 0.01f) flatForward = playerCamera.up; // Edge case if looking straight down
            flatForward.Normalize();

            Vector3 spawnPos = playerCamera.position + flatForward * spawnDistance - Vector3.up * 0.3f;
            
            // Make the UI face the camera but stay perfectly upright
            Quaternion rotation = Quaternion.LookRotation(flatForward);

            Instantiate(pauseUIPrefab, spawnPos, rotation);
        }
    }

    public static void TogglePause(bool isActive)
    {
        if (isActive)
        {
            Time.timeScale = 0f;
            instance.ToggleVRRenderFeatures(true);
            instance.ToggleDarkenLayer(true);
            instance.ToggleRaycastMasks(true);
        }
        else
        {
            Time.timeScale = 1f;
            instance.ToggleVRRenderFeatures(false);
            instance.ToggleDarkenLayer(false);
            instance.ToggleRaycastMasks(false);
        }
    }

    private void ToggleDarkenLayer(bool isActive)
    {
        if (darkenLayer != null)
        {
            darkenLayer.SetActive(isActive);
        }
    }

    private void ToggleVRRenderFeatures(bool isActive)
    {
        if (rendererData == null) return;

        foreach (var feature in rendererData.rendererFeatures)
        {
            if (System.Array.Exists(targetFeatureNames, name => name == feature.name))
            {
                feature.SetActive(isActive);
            }
        }

        int layerIndex = LayerMask.NameToLayer(targetLayerName);
        if (layerIndex == -1)
        {
            Debug.LogError($"Layer {targetLayerName} not found, please check spelling!");
            return;
        }

        int layerBitMask = 1 << layerIndex;

        if (isActive)
        {
            rendererData.opaqueLayerMask &= ~layerBitMask;
            rendererData.transparentLayerMask &= ~layerBitMask;
        }
        else
        {
            rendererData.opaqueLayerMask |= layerBitMask;
            rendererData.transparentLayerMask |= layerBitMask;
        }
    }

    private void ToggleRaycastMasks(bool isActive)
    {
        if (rayInteractors == null || rayInteractors.Length == 0) return;

        int layerIndex = LayerMask.NameToLayer(targetLayerName);
        if (layerIndex == -1) return;

        int targetMask = 1 << layerIndex;

        if (isActive)
        {
            if (originalRaycastMasks == null || originalRaycastMasks.Length != rayInteractors.Length)
            {
                originalRaycastMasks = new int[rayInteractors.Length];
            }

            for (int i = 0; i < rayInteractors.Length; i++)
            {
                if (rayInteractors[i] != null)
                {
                    // Save the current mask before overriding
                    originalRaycastMasks[i] = rayInteractors[i].raycastMask;
                    rayInteractors[i].raycastMask = targetMask;
                }
            }
        }
        else
        {
            if (originalRaycastMasks != null && originalRaycastMasks.Length == rayInteractors.Length)
            {
                for (int i = 0; i < rayInteractors.Length; i++)
                {
                    if (rayInteractors[i] != null)
                    {
                        // Restore the previous mask
                        rayInteractors[i].raycastMask = originalRaycastMasks[i];
                    }
                }
            }
        }
    }
}
