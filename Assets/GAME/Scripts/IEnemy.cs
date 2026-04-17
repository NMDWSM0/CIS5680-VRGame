using UnityEngine;

/// <summary>
/// Interface for any object that can be hit and take damage from the player.
/// Implementing this allows projectiles and lasers to interact with the target
/// without knowing its specific class.
/// </summary>
public interface IEnemy
{
    /// <summary>
    /// Processes a hit on this target.
    /// </summary>
    /// <param name="damage">Base damage dealt.</param>
    /// <param name="hitPart">Optional: Specific part of the object that was hit.</param>
    /// <returns>The actual damage dealt after internal calculations.</returns>
    float Hit(float damage, GameObject hitPart = null);
}
