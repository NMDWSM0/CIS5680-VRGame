using UnityEngine;

public class Shield : MonoBehaviour
{
    /// <summary>
    /// Called entirely by the EnemyBullet script upon collision.
    /// </summary>
    public void Absorb(float incomingDamage)
    {
        Debug.Log($"Shield successfully absorbed {incomingDamage} damage!");
        
        // Find PlayerStatus on the root XR origin
        PlayerStatus playerStatus = GetComponentInParent<PlayerStatus>();
        if (playerStatus != null)
        {
            playerStatus.AddAmmoFromDamage(incomingDamage);
        }
        else
        {
            Debug.LogWarning("Shield absorbed damage but couldn't find PlayerStatus to give ammo to!");
        }
    }
}
