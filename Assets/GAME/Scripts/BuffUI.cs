using UnityEngine;
using TMPro;

public class BuffUI : MonoBehaviour
{
    [Tooltip("The UI text element for the buff's name.")]
    public TMP_Text Name;

    [Tooltip("The UI text element for the buff's description.")]
    public TMP_Text Desc;

    private EnemyManager enemyManager;
    public Buff buff;

    /// <summary>
    /// Call this from the object that generates this UI to set its text.
    /// </summary>
    public void Initialize(Buff newBuff, EnemyManager manager)
    {
        this.buff = newBuff;
        if (Name != null) Name.text = buff.name;
        if (Desc != null) Desc.text = buff.description;
        this.enemyManager = manager;
    }

    public void OnSelect()
    {
        Debug.Log($"BuffUI OnSelect called for {buff.name}.");
        
        PlayerStatus playerStatus = FindObjectOfType<PlayerStatus>();
        RightGun rightGun = FindObjectOfType<RightGun>();
        
        if (playerStatus != null)
        {
            switch (buff.type)
            {
                case BuffType.Heal:
                    playerStatus.Heal(buff.amount);
                    break;
                case BuffType.MaxHealth:
                    playerStatus.maxHealth += buff.amount;
                    playerStatus.Heal(buff.amount);
                    break;
                case BuffType.DamageBoost:
                    playerStatus.damageMultiplier += buff.amount;
                    break;
                case BuffType.CritChance:
                    playerStatus.critRate += buff.amount;
                    break;
                case BuffType.HealthSteal:
                    playerStatus.lifeSteal += buff.amount;
                    break;
                case BuffType.AmmoEfficiency:
                    playerStatus.ammoEfficiency += buff.amount;
                    break;
                case BuffType.FireRate:
                    if (rightGun != null)
                    {
                        rightGun.fireRate = Mathf.Max(rightGun.minFireRate, rightGun.fireRate - buff.amount);
                        Debug.Log($"Fire rate buff triggered! New delay: {rightGun.fireRate}s");
                    }
                    break;
                case BuffType.Penetration:
                    if (rightGun != null)
                    {
                        rightGun.penetrate = true;
                        // Based on user description, choosing again increases penetration damage by amount
                        rightGun.penetrationDamage += buff.amount;
                    }
                    break;
            }
        }

        if (enemyManager != null)
        {
            enemyManager.OnBuffSelected(this);
        }
    }
}
