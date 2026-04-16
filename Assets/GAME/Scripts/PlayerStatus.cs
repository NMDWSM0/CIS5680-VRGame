using UnityEngine;

public class PlayerStatus : MonoBehaviour
{
    [Tooltip("The player's starting health.")]
    public float health = 100f;

    [Tooltip("The player's maximum health.")]
    public float maxHealth = 100f;

    [Tooltip("Current ammunition.")]
    public int ammo = 0;

    [Tooltip("Maximum ammunition capacity.")]
    public int maxAmmo = 100;

    [Tooltip("Efficiency of converting absorbed damage to ammo.")]
    public float ammoEfficiency = 1.0f;

    [Header("Combat Stats")]
    [Tooltip("Damage multiplier (starts at 1.0 = 100%).")]
    public float damageMultiplier = 1.0f;

    [Tooltip("Critical hit chance (starts at 0.0 = 0%).")]
    public float critRate = 0f;

    [Tooltip("Percentage of damage dealt gained as health (starts at 0.0 = 0%).")]
    public float lifeSteal = 0f;

    [Header("Game Over Settings")]
    [Tooltip("The Game Over UI prefab to spawn.")]
    public GameObject gameoverUIPrefab;

    [Tooltip("Distance in front of the player to spawn the UI.")]
    public float spawnDistance = 2.0f;

    public bool isGameOver = false;

    /// <summary>
    /// Called when the player's shield absorbs incoming damage.
    /// Converts that absorbed damage into ammunition!
    /// </summary>
    public void AddAmmoFromDamage(float damageAbsorbed)
    {
        // Add 1 ammo for every 1 damage absorbed (can be tweaked)
        int ammoGained = Mathf.RoundToInt(damageAbsorbed * ammoEfficiency);
        ammo = Mathf.Clamp(ammo + ammoGained, 0, maxAmmo);
        
        Debug.Log($"Absorbed {damageAbsorbed} damage and converted to {ammoGained} ammo! Total Ammo: {ammo}/{maxAmmo}");
    }

    /// <summary>
    /// Attempts to consume 1 ammo to fire the gun. Returns true if successful.
    /// </summary>
    public bool TryConsumeAmmo()
    {
        if (ammo >= 10)
        {
            ammo -= 10;
            return true;
        }
        Debug.Log("Out of ammo! Block bullets with the shield to recharge.");
        return false;
    }

    /// <summary>
    /// Called when the player takes damage from an enemy bullet or hazard.
    /// </summary>
    public void TakeDamage(float damage)
    {
        health -= damage;
        Debug.Log($"Player took {damage} damage! Remaining health: {health}");

        // Example logic for updating a UI on your screen, playing a damage grunt sound, etc., can go here!

        if (health <= 0)
        {
            Die();
        }
    }

    /// <summary>
    /// Heals the player by a specified amount.
    /// </summary>
    public void Heal(float amount)
    {
        health = Mathf.Clamp(health + amount, 0, maxHealth);
        Debug.Log($"Player healed {amount}! Current health: {health}");
    }

    /// <summary>
    /// Called when the player deals damage to an enemy to trigger health gain.
    /// </summary>
    public void OnDamageDealt(float damageDealt)
    {
        if (lifeSteal > 0)
        {
            float healAmount = damageDealt * lifeSteal;
            Heal(healAmount);
        }
    }

    private void Die()
    {
        Debug.Log("Player has died! Triggering game over...");

        // Handle game over logic, respawning, or scene reloading here
        if (!isGameOver && gameoverUIPrefab != null)
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

            var currentGameOverUI = Instantiate(gameoverUIPrefab, spawnPos, rotation);
            currentGameOverUI.GetComponent<GameoverUI>().Initialize("Game Over");
        }
        isGameOver = true;
    }

    public void Win()
    {
        Debug.Log("Player has won! Triggering game over...");

        // Handle game over logic, respawning, or scene reloading here
        if (!isGameOver && gameoverUIPrefab != null)
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

            var currentGameOverUI = Instantiate(gameoverUIPrefab, spawnPos, rotation);
            currentGameOverUI.GetComponent<GameoverUI>().Initialize("You Win!");
        }
        isGameOver = true;
    }
}
