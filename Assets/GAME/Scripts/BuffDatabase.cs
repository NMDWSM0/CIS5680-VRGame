using System.Collections.Generic;
using UnityEngine;

public enum BuffType
{
    // common buffs
    Heal,
    MaxHealth,
    DamageBoost,
    CritChance,
    HealthSteal,
    AmmoEfficiency,
    AutoAmmoRecharge,
    ShieldSize,
    // gold buffs
    Penetration,
    Multishot,
    HomingLasers,
}

public enum BuffRarity
{
    Green,
    Blue,
    Purple,
    Gold
}

[System.Serializable]
public struct Buff
{
    public BuffType type;
    public string name;
    public string description;
    public string descriptionTemplate;
    public bool isPercentage;
    public float amount;
    public bool isGoldBuff;
    public BuffRarity rarity;

    public Buff(BuffType type, string name, string descriptionTemplate, float amount, bool isPercentage = false, bool isGoldBuff = false)
    {
        this.type = type;
        this.name = name;
        this.descriptionTemplate = descriptionTemplate;
        this.isPercentage = isPercentage;
        this.amount = amount;
        this.isGoldBuff = isGoldBuff;
        this.rarity = isGoldBuff ? BuffRarity.Gold : BuffRarity.Green;
        
        string formattedAmount = isPercentage ? (amount * 100).ToString("0") + "%" : amount.ToString();
        this.description = descriptionTemplate.Replace("{0}", formattedAmount);
    }

    public void ScaleRarity(BuffRarity newRarity)
    {
        this.rarity = newRarity;
        float multiplier = 1.0f;
        string rarityName = "Normal";

        if (newRarity == BuffRarity.Blue) { multiplier = 1.5f; rarityName = "Rare"; }
        else if (newRarity == BuffRarity.Purple) { multiplier = 2.0f; rarityName = "Epic"; }
        else if (newRarity == BuffRarity.Gold) { rarityName = "Legendary"; }
        
        this.amount *= multiplier;
        this.name = $"[{rarityName}] " + this.name;
        
        string formattedAmount = this.isPercentage ? (this.amount * 100).ToString("0") + "%" : this.amount.ToString("0.0#");
        this.description = this.descriptionTemplate.Replace("{0}", formattedAmount);
    }
}

public static class BuffDatabase
{
    public static readonly Buff[] AvailableBuffs = new Buff[]
    {
        // common buffs
        new Buff(BuffType.Heal, "Health Restore", "Restores health by {0}.", 20f),
        new Buff(BuffType.MaxHealth, "Max Health Up", "Increases your maximum health by {0}.", 10f),
        new Buff(BuffType.DamageBoost, "Damage Boost", "Increases your attack damage by {0}.", 0.08f, true),
        new Buff(BuffType.CritChance, "Critical Hit Chance", "Increases the chance of landing a critical hit by {0}.", 0.05f, true),
        new Buff(BuffType.AmmoEfficiency, "Ammo Efficiency", "Increases ammo gained from absorbing damage by {0}.", 0.12f, true),
        new Buff(BuffType.AutoAmmoRecharge, "Auto Ammo Recharge", "Increases ammo auto recharge rate by {0} per second.", 0.03f),
        new Buff(BuffType.HealthSteal, "Health Steal", "Increases the healing you receive from dealing damage by {0}.", 0.01f, true),
        new Buff(BuffType.ShieldSize, "Bigger Shield", "Increases your shield's covering area by {0}.", 0.08f, true),

        // gold buffs
        new Buff(BuffType.Penetration, "Penetration", "Bullets can penetrate through enemies, but deal {0} damage. (Choose again will increase penetration damage by that amount)", 0.2f, true, true),
        new Buff(BuffType.Multishot, "Multishot", "Shoot 2 more lasers each time, but cost more ammo. (Max 7 lasers)", 2f, false, true),
        new Buff(BuffType.HomingLasers, "Homing Lasers", "Lasers can track enemies in some angle range.", 0f, false, true),
    };
}
