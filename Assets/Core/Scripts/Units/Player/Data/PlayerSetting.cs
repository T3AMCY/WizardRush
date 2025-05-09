using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(
    fileName = "PlayerSetting",
    menuName = "ScriptableObjects/PlayerSetting",
    order = 1
)]
public class PlayerSetting : ScriptableObject
{
    public double maxHp = 100f;
    public double damage = 5f;
    public double moveSpeed = 5f;
    public double attackSpeed = 1f;
    public double attackRange = 2f;
    public double hpRegenRate = 1f;
    public double attackRadius = 1f;
    public double criticalChance = 10f;
    public double criticalDamage = 10f;
    public double lifeSteal = 10f;

    public Stat GetDefualtStat()
    {
        var statTypes = Enum.GetValues(typeof(StatType)).Cast<StatType>().ToArray();
        var statValues = new double[statTypes.Length];
        for (int i = 0; i < statTypes.Length; i++)
        {
            statValues[i] = statTypes[i] switch
            {
                StatType.MaxHp => maxHp,
                StatType.Damage => damage,
                StatType.MoveSpeed => moveSpeed,
                StatType.AttackSpeed => attackSpeed,
                StatType.AttackRange => attackRange,
                StatType.HpRegenRate => hpRegenRate,
                StatType.AttackRadius => attackRadius,
                StatType.CriticalChance => criticalChance,
                StatType.CriticalDamage => criticalDamage,
                StatType.LifeSteal => lifeSteal,
                _ => 0f,
            };
        }
        return new Stat(statTypes, statValues);
    }
}
