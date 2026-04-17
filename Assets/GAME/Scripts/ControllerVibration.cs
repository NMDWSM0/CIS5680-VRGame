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
    public static void Vibrate(bool isLeft, float amplitude, float duration)
    {
        if (instance == null)
        {
            Debug.LogWarning("ControllerVibration Instance not found! Did you add it to a global object?");
            return;
        }

        if (isLeft && instance.leftController != null)
        {
            instance.leftController.SendHapticImpulse(amplitude, duration);
        }
        else if (!isLeft && instance.rightController != null)
        {
            instance.rightController.SendHapticImpulse(amplitude, duration);
        }
        else
        {
            Debug.LogWarning($"ControllerVibration: {(isLeft ? "Left" : "Right")} controller is not assigned!");
        }
    }

    /// <summary>
    /// Triggers a haptic impulse on the left controller.
    /// </summary>
    public static void VibrateLeft(float amplitude, float duration)
    {
        Vibrate(true, amplitude, duration);
    }

    /// <summary>
    /// Triggers a haptic impulse on the right controller.
    /// </summary>
    public static void VibrateRight(float amplitude, float duration)
    {
        Vibrate(false, amplitude, duration);
    }
}
