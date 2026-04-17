using UnityEngine;
using System.Collections;

/// <summary>
/// A suicide attack behavior for enemies. 
/// When the player is in range, it plays an alarm, then charges at the player with accelerating speed.
/// </summary>
public class SuicideDrone : MonoBehaviour, IEnemy
{
    public enum DroneState { Idle, Warning, Charging, Exploded }
    
    [Header("Detection & Warning")]
    [Tooltip("Range at which the drone notices the player and starts the alarm.")]
    public float detectionRange = 12f;
    
    [Tooltip("How long the alarm plays before the drone charges.")]
    public float alarmDuration = 2.5f;
    
    [Tooltip("The sound to play during the warning phase.")]
    public AudioClip alarmSound;
    
    [Range(0, 1)] public float alarmVolume = 1f;

    [Header("Movement Settings")]
    [Tooltip("Starting speed when charge begins.")]
    public float startSpeed = 2f;
    
    [Tooltip("Maximum speed the drone can reach.")]
    public float maxSpeed = 18f;
    
    [Tooltip("How fast the drone accelerates (m/s^2).")]
    public float acceleration = 6f;
    
    [Tooltip("How fast the drone turns to face the player.")]
    public float rotationSpeed = 8f;

    [Header("Combat")]
    [Tooltip("Damage dealt to the player on impact.")]
    public float impactDamage = 40f;

    [Tooltip("The layer or tag identifying the player. Usually 'Player'.")]
    public string playerTag = "Player";

    [Header("Audio (Explosion)")]
    [Tooltip("Sound to play specifically when the drone hits its target or is shot down.")]
    public AudioClip explosionSound;

    [Tooltip("Volume of the explosion sound.")]
    [Range(0, 1)] public float explosionVolume = 1f;

    private DroneState currentState = DroneState.Idle;
    private Transform player;
    private float currentSpeed;
    private Enemy baseEnemyComponent;
    private AudioSource audioSource;

    private void Start()
    {
        // Find player camera
        if (Camera.main != null)
        {
            player = Camera.main.transform;
        }

        baseEnemyComponent = GetComponent<Enemy>();
        currentSpeed = startSpeed;

        // // --- 1. Fix Collision Issues ---
        // // Every collision requires at least one Rigidbody. We add/configure it here.
        // Rigidbody rb = GetComponent<Rigidbody>();
        // if (rb == null) rb = gameObject.AddComponent<Rigidbody>();
        
        // // We set to Kinematic because we move via transform, but it allows trigger/collision msgs.
        // rb.isKinematic = true; 
        // // ContinuousSpeculative is best for kinematic objects that might move fast.
        // rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

        // // Ensure there is a collider
        // if (GetComponent<Collider>() == null)
        // {
        //     Debug.LogWarning($"SuicideDrone: No collider found on {gameObject.name}. Adding a SphereCollider automatically.");
        //     gameObject.AddComponent<SphereCollider>();
        // }

        // --- 2. Initialize AudioSource for Alarm ---
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        
        audioSource.spatialBlend = 1.0f; // Ensure it's 3D sound
        audioSource.playOnAwake = false;
    }

    private void Update()
    {
        if (player == null || currentState == DroneState.Exploded) return;

        float distance = Vector3.Distance(transform.position, player.position);

        switch (currentState)
        {
            case DroneState.Idle:
                if (distance <= detectionRange)
                {
                    StartCoroutine(WarningRoutine());
                }
                break;

            case DroneState.Charging:
                ChargeAtPlayer();
                break;
        }
    }

    /// <summary>
    /// Implementation of IEnemy. Allows the drone to react when shot.
    /// </summary>
    public float Hit(float damage, GameObject hitPart = null)
    {
        if (currentState == DroneState.Exploded) return 0;

        Debug.Log($"<color=red>SuicideDrone:</color> I was hit! Current state: {currentState}");

        // React to being shot: if idle, skip the wait and prepare for attack
        if (currentState == DroneState.Idle)
        {
            StopAllCoroutines();
            StartCoroutine(WarningRoutine());
        }

        // Forward damage to the main Enemy script to handle health/shattering
        if (baseEnemyComponent != null)
        {
            float realDamage = baseEnemyComponent.Hit(damage, hitPart);
            
            // If the enemy health reaches zero from this shot, disable colliders immediately
            if (baseEnemyComponent.health <= 0)
            {
                DisableColliders();
            }
            return realDamage;
        }

        return 0;
    }

    private void DisableColliders()
    {
        foreach (var col in GetComponentsInChildren<Collider>())
        {
            col.enabled = false;
        }
    }

    private IEnumerator WarningRoutine()
    {
        currentState = DroneState.Warning;
        Debug.Log($"<color=red>SuicideDrone:</color> {gameObject.name} is locking on! WARNING started.");

        // Play alarm
        if (alarmSound != null && audioSource != null)
        {
            audioSource.clip = alarmSound;
            audioSource.volume = alarmVolume;
            audioSource.loop = true;
            audioSource.Play();
        }

        // Disable other common behaviors to prevent conflicts
        var shooter = GetComponent<EnemyShooter>();
        if (shooter != null) shooter.enabled = false;
        
        var patrol = GetComponent<EnemyPatrol>();
        if (patrol != null) patrol.enabled = false;

        yield return new WaitForSeconds(alarmDuration);

        // Stop alarm sound before charging (or keep it if you want constant beeping)
        if (audioSource != null && audioSource.clip == alarmSound)
        {
            audioSource.Stop();
        }

        currentState = DroneState.Charging;
    }

    private void ChargeAtPlayer()
    {
        // 1. Accelerate
        currentSpeed = Mathf.MoveTowards(currentSpeed, maxSpeed, acceleration * Time.deltaTime);

        // 2. Rotate towards player head
        Vector3 direction = (player.position - transform.position).normalized;
        if (direction != Vector3.zero)
        {
            Quaternion targetRot = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
        }

        // 3. Move forward in the current rotation
        transform.position += transform.forward * currentSpeed * Time.deltaTime;
    }

    // Handles impact with the player or obstacles
    private void OnTriggerEnter(Collider other)
    {
        Debug.Log($"<color=red>SuicideDrone:</color> OnTriggerEnter");
        if (currentState != DroneState.Charging) return;
        HandleImpact(other.gameObject);
    }

    private void OnCollisionEnter(Collision collision)
    {
        Debug.Log($"<color=red>SuicideDrone:</color> OnCollisionEnter");
        if (currentState != DroneState.Charging) return;
        HandleImpact(collision.gameObject);
    }

    private void HandleImpact(GameObject hitTarget)
    {
        if (currentState == DroneState.Exploded) return;
        currentState = DroneState.Exploded;
        
        Debug.Log($"<color=red>SuicideDrone:</color> {gameObject.name} IMPACT!");

        // 0. Disable all colliders immediately to prevent multiple triggers or physics issues during destruction
        DisableColliders();

        // 1. Play explosion sound
        if (explosionSound != null)
        {
            AudioSource.PlayClipAtPoint(explosionSound, transform.position, explosionVolume);
        }

        // 2. Try to deal damage to player
        PlayerStatus playerStatus = hitTarget.GetComponent<PlayerStatus>();
        if (playerStatus == null) playerStatus = hitTarget.GetComponentInParent<PlayerStatus>();
        
        // Also check by tag if component check fails (VR often has nested rigs)
        if (playerStatus == null && hitTarget.CompareTag(playerTag))
        {
            playerStatus = GameObject.FindObjectOfType<PlayerStatus>();
        }

        if (playerStatus != null)
        {
            playerStatus.TakeDamage(impactDamage);
        }

        // 3. Trigger self-destruct visuals via the Enemy script
        if (baseEnemyComponent != null)
        {
            // If the enemy has its own death sound, it will also play.
            // You can leave the Enemy deathSound empty if you only want the suicide explosion sound.
            // Hit it for more than any reasonable amount of health to force immediate death
            baseEnemyComponent.Hit(9999f);
        }
        else
        {
            // Fallback if no Enemy script exists
            Destroy(gameObject);
        }
    }
}
