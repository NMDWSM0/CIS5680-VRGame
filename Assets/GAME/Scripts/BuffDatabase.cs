using System.Collections.Generic;
using UnityEngine;

public enum BuffType
{
    Heal,
    MaxHealth,
    DamageBoost,
    CritChance,
    FireRate,
    HealthSteal,
    AmmoEfficiency,
    Penetration,
}

[System.Serializable]
public struct Buff
{
    public BuffType type;
    public string name;
    public string description;
    public float amount;

    public Buff(BuffType type, string name, string descriptionTemplate, float amount)
    {
        this.type = type;
        this.name = name;
        this.amount = amount;
        
        string formattedAmount = (amount > 0 && amount < 1f) ? (amount * 100).ToString("0") + "%" : amount.ToString();
        this.description = descriptionTemplate.Replace("{0}", formattedAmount);
    }
}

public static class BuffDatabase
{
    public static readonly Buff[] AvailableBuffs = new Buff[]
    {
        new Buff(BuffType.Heal, "Health Restore", "Restores health by {0}.", 10f),
        new Buff(BuffType.MaxHealth, "Max Health Up", "Increases your maximum health by {0}.", 5f),
        new Buff(BuffType.DamageBoost, "Damage Boost", "Increases your attack damage by {0}.", 0.05f),
        new Buff(BuffType.CritChance, "Critical Hit Chance", "Increases the chance of landing a critical hit by {0}.", 0.05f),
        new Buff(BuffType.FireRate, "Rapid Fire", "Increases the rate of fire of your weapon by {0}. (Maximum 80%)", 0.1f),
        new Buff(BuffType.HealthSteal, "Health Steal", "Increases the healing you receive from dealing damage by {0}.", 0.05f),
        new Buff(BuffType.AmmoEfficiency, "Ammo Efficiency", "Increases ammo gained from absorbing damage by {0}.", 0.2f),
        new Buff(BuffType.Penetration, "Penetration", "Bullets can penetrate through enemies, but deal {0} damage. (Choose again will increase penetration damage by that amount)", 0.1f),
    };
}
