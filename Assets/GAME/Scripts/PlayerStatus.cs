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
    public float ammoEfficiency = 0.1f;

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
        if (ammo > 0)
        {
            ammo--;
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

    private void Die()
    {
        Debug.Log("Player has died! Triggering game over...");
        // Handle game over logic, respawning, or scene reloading here
    }
}
