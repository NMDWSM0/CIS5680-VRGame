using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

/// <summary>
/// A global singleton to manage controller vibration.
/// Attach this script to a global object in your scene and assign the controllers.
/// You can call its methods statically from anywhere without referencing the instance.
/// </summary>
public class ControllerVibration : MonoBehaviour
{
    private static ControllerVibration instance;

    [Header("Controllers Setup")]
    [Tooltip("Reference to the Left XR Controller.")]
    public XRBaseController leftController;

    [Tooltip("Reference to the Right XR Controller.")]
    public XRBaseController rightController;

    private void Awake()
    {
        // Singleton pattern implementation
        if (instance == null)
        {
            instance = this;
            // Optional: uncomment below if this object should persist across scenes
            // DontDestroyOnLoad(gameObject);
        }
        else if (instance != this)
        {
            Debug.LogWarning("Found multiple instances of ControllerVibration. Destroying the newest one.");
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Triggers a haptic impulse on the specified controller.
    /// </summary>
    /// <param name="isLeft">True to vibrate the left controller, false for the right.</param>
    /// <param name="amplitude">The intensity of the vibration (typically 0.0 to 1.0).</param>
    /// <param name="duration">The duration in seconds.</param>
    public static void Vibrate(bool isLeft, float amplitude, float duration, float frequency)
    {
        if (instance == null)
        {
            Debug.LogWarning("ControllerVibration Instance not found! Did you add it to a global object?");
            return;
        }

        XRBaseController controller = isLeft ? instance.leftController : instance.rightController;
        if (controller != null)
        {
            // If the controller is ActionBased and we want to use OpenXR's frequency
            if (frequency > 0f && controller is ActionBasedController actionController)
            {
                var action = actionController.hapticDeviceAction.action;
                if (action != null)
                {
                    // Safe reflection to use OpenXR's haptics without hardcoding the dependency
                    var openXRInputType = System.Type.GetType("UnityEngine.XR.OpenXR.Input.OpenXRInput, Unity.XR.OpenXR");
                    if (openXRInputType != null)
                    {
                        var method = openXRInputType.GetMethod("SendHapticImpulse", 
                            new System.Type[] { typeof(UnityEngine.InputSystem.InputAction), typeof(float), typeof(float), typeof(float), typeof(UnityEngine.InputSystem.InputDevice) });
                        if (method != null)
                        {
                            try
                            {
                                UnityEngine.InputSystem.InputDevice inputDevice = null;
                                if (action.controls.Count > 0)
                                {
                                    inputDevice = action.controls[0].device;
                                }

                                method.Invoke(null, new object[] { action, amplitude, frequency, duration, inputDevice });
                                return; // Successfully sent OpenXR haptic
                            }
                            catch (System.Exception e)
                            {
                                Debug.LogWarning("ControllerVibration: OpenXR send failed, falling back. Error: " + e.Message);
                            }
                        }
                        else
                        {
                            Debug.LogWarning("ControllerVibration: SendHapticImpulse method not found! Using standard fallback.");
                        }
                    }
                    else
                    {
                        Debug.LogWarning("ControllerVibration: OpenXRInput type not found! Using standard fallback.");
                    }
                }
                else
                {
                    Debug.LogWarning("ControllerVibration: Haptic action not found! Using standard fallback.");
                }
            }
            
            // Standard fallback without frequency control
            controller.SendHapticImpulse(amplitude, duration);
        }
        else
        {
            Debug.LogWarning($"ControllerVibration: {(isLeft ? "Left" : "Right")} controller is not assigned!");
        }
    }

    /// <summary>
    /// Triggers a haptic impulse on the left controller.
    /// </summary>
    public static void VibrateLeft(float amplitude, float duration, float frequency = 200.0f)
    {
        Vibrate(true, amplitude, duration, frequency);
    }

    /// <summary>
    /// Triggers a haptic impulse on the right controller.
    /// </summary>
    public static void VibrateRight(float amplitude, float duration, float frequency = 200.0f)
    {
        Vibrate(false, amplitude, duration, frequency);
    }
}
