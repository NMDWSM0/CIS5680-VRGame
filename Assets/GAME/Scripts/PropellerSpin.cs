using UnityEngine;

/// <summary>
/// A simple script to rotate an object (like a propeller or fan) at a constant speed.
/// Uses local transform rotation for maximum performance and simplicity.
/// </summary>
public class PropellerSpin : MonoBehaviour
{
    [Tooltip("The speed of rotation in degrees per second.")]
    public float spinSpeed = 1000f;

    [Tooltip("The axis around which the object will spin (local space).")]
    public Vector3 spinAxis = Vector3.forward;

    private void Update()
    {
        // Rotate the object around the specified axis relative to its own local space
        transform.Rotate(spinAxis, spinSpeed * Time.deltaTime, Space.Self);
    }
}
