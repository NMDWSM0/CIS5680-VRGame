using UnityEngine;
using System.Collections;

/// <summary>
/// A script that moves an enemy in an intermittent, randomized orbit around the player.
/// </summary>
public class EnemyPatrol : MonoBehaviour
{
    [Header("Orbit Timing")]
    [Tooltip("Min and Max time the enemy spends moving (x = min, y = max).")]
    public Vector2 moveDurationRange = new Vector2(2f, 4f);

    [Tooltip("Min and Max time the enemy spends stopped (x = min, y = max).")]
    public Vector2 stopDurationRange = new Vector2(1f, 3f);

    [Header("Randomization Ranges")]
    [Tooltip("Distance range from the player.")]
    public Vector2 distanceRange = new Vector2(3f, 7f);

    [Tooltip("Height range above the player.")]
    public Vector2 heightRange = new Vector2(2f, 5f);

    [Tooltip("Orbit speed range (degrees per second). Direction is randomized.")]
    public Vector2 speedRange = new Vector2(20f, 60f);

    [Header("Movement Settings")]
    [Tooltip("How smoothly the enemy follows its target parameters.")]
    public float smoothing = 3f;

    [Tooltip("Should the enemy always face the player?")]
    public bool alwaysFacePlayer = true;

    [Tooltip("How fast the enemy rotates to face the player.")]
    public float rotationSpeed = 5f;

    [Tooltip("Explicitly assign the player transform. If null, automatically finds Camera.main.")]
    public Transform playerTransform;

    // Internal state
    private float currentAngle = 0f;
    private float targetOrbitDistance;
    private float targetHeightOffset;
    private float currentOrbitSpeed;
    private bool isMoving = false;

    // Smoothed values
    private float smoothDistance;
    private float smoothHeight;

    private void Start()
    {
        if (playerTransform == null && Camera.main != null)
            playerTransform = Camera.main.transform;

        if (playerTransform != null)
        {
            // Initial positioning
            Vector3 diff = transform.position - playerTransform.position;
            diff.y = 0;
            if (diff != Vector3.zero)
                currentAngle = Mathf.Atan2(diff.z, diff.x) * Mathf.Rad2Deg;

            // Set initial targets
            targetOrbitDistance = Vector3.ProjectOnPlane(diff, Vector3.up).magnitude;
            targetHeightOffset = transform.position.y - playerTransform.position.y;
            smoothDistance = targetOrbitDistance;
            smoothHeight = targetHeightOffset;
        }

        StartCoroutine(MovementCycle());
    }

    private IEnumerator MovementCycle()
    {
        while (true)
        {
            // --- STOP PHASE ---
            isMoving = false;
            yield return new WaitForSeconds(Random.Range(stopDurationRange.x, stopDurationRange.y));

            // --- MOVE PHASE ---
            // Randomize direction and speed
            float baseSpeed = Random.Range(speedRange.x, speedRange.y);
            currentOrbitSpeed = (Random.value > 0.5f) ? baseSpeed : -baseSpeed;
            
            // Randomize target distance and height for this segment
            targetOrbitDistance = Random.Range(distanceRange.x, distanceRange.y);
            targetHeightOffset = Random.Range(heightRange.x, heightRange.y);
            
            isMoving = true;
            yield return new WaitForSeconds(Random.Range(moveDurationRange.x, moveDurationRange.y));
        }
    }

    private void Update()
    {
        if (playerTransform == null) return;

        // 1. Progress current angle if moving
        if (isMoving)
        {
            currentAngle += currentOrbitSpeed * Time.deltaTime;
        }

        // 2. Smoothly approach target parameters
        smoothDistance = Mathf.Lerp(smoothDistance, targetOrbitDistance, smoothing * Time.deltaTime);
        smoothHeight = Mathf.Lerp(smoothHeight, targetHeightOffset, smoothing * Time.deltaTime);

        // 3. Calculate position
        float radians = currentAngle * Mathf.Deg2Rad;
        Vector3 orbitOffset = new Vector3(Mathf.Cos(radians), 0, Mathf.Sin(radians)) * smoothDistance;
        Vector3 targetPosition = playerTransform.position + orbitOffset;
        targetPosition.y = playerTransform.position.y + smoothHeight;

        transform.position = targetPosition;

        // 4. Handle rotation
        if (alwaysFacePlayer)
        {
            LookAtTarget(playerTransform.position);
        }
    }

    private void LookAtTarget(Vector3 targetPosition)
    {
        Vector3 direction = (targetPosition - transform.position).normalized;

        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime
            );
        }
    }
}
