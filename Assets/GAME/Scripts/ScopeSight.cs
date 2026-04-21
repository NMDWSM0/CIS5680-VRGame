using UnityEngine;
using UnityEngine.InputSystem;

public class ScopeSight : MonoBehaviour
{
    [Header("Zoom Settings")]
    [Tooltip("The camera used for rendering the scope view. Should be this object's camera.")]
    public Camera scopeCamera;

    [Tooltip("The biggest Field of View.")]
    public float maxFOV = 20f;

    [Tooltip("The smallest Field of View.")]
    public float minFOV = 3f;

    [Tooltip("The target Field of View.")]
    public float targetFOV = 6f;

    [Tooltip("How fast the scope transitions between zooms.")]
    public float zoomSpeed = 10f;

    [Header("Scope Object")]
    [Tooltip("The scope object to show/hide.")]
    public GameObject scopeObject;

    [Header("Input")]
    [Tooltip("An input action bound to a Vector2 (the joystick) to control zoom.")]
    public InputActionReference zoomAction;

    [Tooltip("Optional: An input action to show/hide the scope object (e.g. thumbstick click or a secondary button).")]
    public InputActionReference showAction;
    
    private void Start()
    {
        if (scopeCamera == null)
            scopeCamera = GetComponent<Camera>();

        if (scopeCamera != null)
            scopeCamera.fieldOfView = targetFOV;
    }

    private void OnEnable()
    {
        if (showAction != null && showAction.action != null)
        {
            showAction.action.performed += ToggleShow;
            showAction.action.Enable();
        }
        
        if (zoomAction != null && zoomAction.action != null)
        {
            zoomAction.action.Enable();
        }
    }

    private void OnDisable()
    {
        if (showAction != null && showAction.action != null)
        {
            showAction.action.performed -= ToggleShow;
        }
    }

    private void ToggleShow(InputAction.CallbackContext context)
    {
        if (scopeObject != null)
        {
            scopeObject.SetActive(!scopeObject.activeSelf);
        }
    }

    private void Update()
    {
        if (scopeCamera != null)
        {
            // Read joystick input if available
            if (zoomAction != null && zoomAction.action != null)
            {
                // We use ReadValue<Vector2> assuming a joystick input
                Vector2 stickInput = Vector2.zero;
                
                if (zoomAction.action.activeControl?.valueType == typeof(Vector2))
                {
                    stickInput = zoomAction.action.ReadValue<Vector2>();
                }
                else if (zoomAction.action.activeControl?.valueType == typeof(float))
                {
                    // Fallback in case they bound an axis instead of a Vector2
                    stickInput.y = zoomAction.action.ReadValue<float>();
                }
                // Smoothly slide the target FOV based on joystick Y axis
                // Pushing forward (positive Y) decreases FOV (zooms in)
                targetFOV -= stickInput.y * zoomSpeed * 5f * Time.deltaTime;
                targetFOV = Mathf.Clamp(targetFOV, minFOV, maxFOV);

            }

            // Smoothly interpolate the field of view
            scopeCamera.fieldOfView = Mathf.Lerp(scopeCamera.fieldOfView, targetFOV, Time.deltaTime * zoomSpeed);
        }
    }
}
