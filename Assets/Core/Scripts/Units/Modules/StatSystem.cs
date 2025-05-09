using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public struct Stat
{
    public Dictionary<StatType, double> Stats;
    public List<StatModifier> Modifiers;

    public Stat(StatType[] StatTypes)
    {
        Stats = new Dictionary<StatType, double>();
        Modifiers = new List<StatModifier>();

        foreach (var statType in StatTypes)
        {
            if (statType == StatType.None)
                continue;

            Stats[statType] = 0;
        }
    }

    public Stat(StatType[] StatTypes, double[] StatValues)
    {
        Stats = new Dictionary<StatType, double>();
        Modifiers = new List<StatModifier>();

        for (int i = 0; i < StatTypes.Length; i++)
        {
            Stats[StatTypes[i]] = StatValues[i];
        }
    }

    public void SetStat(StatType statType, double value)
    {
        Stats[statType] = value;
    }

    public double GetStat(StatType statType)
    {
        if (Stats.TryGetValue(statType, out double value))
            return value;
        return 0;
    }

    public void CalcTotal()
    {
        foreach (var modifier in Modifiers)
        {
            Stats[modifier.Type] += modifier.Value;
        }
    }

    public static Stat operator +(Stat a, Stat b)
    {
        a.CalcTotal();
        b.CalcTotal();

        Stat result = new Stat();
        result.Stats = new Dictionary<StatType, double>(a.Stats);
        result.Modifiers = new List<StatModifier>(a.Modifiers);

        foreach (var stat in b.Stats)
        {
            if (result.Stats.ContainsKey(stat.Key))
            {
                result.Stats[stat.Key] += stat.Value;
            }
            else
            {
                result.Stats[stat.Key] = stat.Value;
            }
        }

        result.Modifiers.AddRange(b.Modifiers);
        return result;
    }
}

public class StatSystem : MonoBehaviour
{
    private Stat baseStat;
    private Stat ItemStat;
    private Stat BuffStat;
    private Stat PassiveStat;
    private Stat CurrentStat
    {
        get { return baseStat + ItemStat + BuffStat + PassiveStat; }
    }
    private Dictionary<StatType, float> currentStats = new();
    private List<StatModifier> activeModifiers = new();
    public event Action<StatType, float> OnStatChanged;

    public void Initialize(StatData statData)
    {
        if (statData.baseStats.Stats == null)
        {
            Logger.LogWarning(typeof(StatSystem), "Stat Data is null");
            return;
        }

        baseStat = statData.baseStats;
        currentStats = new Dictionary<StatType, float>();

        foreach (var stat in baseStat.Stats)
        {
            currentStats[stat.Key] = (float)stat.Value;
        }

        float maxHp = (float)baseStat.GetStat(StatType.MaxHp);
        currentStats[StatType.CurrentHp] = maxHp;
    }

    public StatData GetSaveData()
    {
        var saveData = new StatData();
        saveData.baseStats = new Stat(currentStats.Keys.ToArray());

        foreach (var stat in currentStats)
        {
            saveData.baseStats.SetStat(stat.Key, GetBaseValue(stat.Key));
        }

        return saveData;
    }

    public void AddModifier(StatModifier modifier)
    {
        activeModifiers.Add(modifier);
        RecalculateStats(modifier.Type);
    }

    public void RemoveModifier(StatModifier modifier)
    {
        activeModifiers.Remove(modifier);
        RecalculateStats(modifier.Type);
    }

    private void RecalculateStats(StatType statType)
    {
        float baseValue = GetBaseValue(statType);
        float addValue = 0;
        float mulValue = 1f;

        foreach (var modifier in activeModifiers.Where(m => m.Type == statType))
        {
            switch (modifier.IncreaseType)
            {
                case CalcType.Flat:
                    addValue += modifier.Value;
                    break;
                case CalcType.Multiply:
                    mulValue *= (1 + modifier.Value);
                    break;
            }
        }

        float oldValue = currentStats.ContainsKey(statType) ? currentStats[statType] : 0f;
        float newValue = (baseValue + addValue) * mulValue;
        if (!Mathf.Approximately(oldValue, newValue))
        {
            currentStats[statType] = newValue;
            OnStatChanged?.Invoke(statType, newValue);
        }
    }

    private void RecalculateAllStats()
    {
        foreach (StatType statType in Enum.GetValues(typeof(StatType)))
        {
            RecalculateStats(statType);
        }
    }

    public void RemoveAllModifiers()
    {
        activeModifiers.Clear();
        RecalculateAllStats();
    }

    public float GetStat(StatType type)
    {
        return currentStats.TryGetValue(type, out float value) ? value : 0f;
    }

    private float GetBaseValue(StatType type)
    {
        return (float)baseStat.GetStat(type);
    }

    public void SetCurrentHp(float value)
    {
        float maxHp = GetStat(StatType.MaxHp);
        float newHp = Mathf.Clamp(value, 0, maxHp);

        if (!Mathf.Approximately(currentStats[StatType.CurrentHp], newHp))
        {
            currentStats[StatType.CurrentHp] = newHp;
            OnStatChanged?.Invoke(StatType.CurrentHp, newHp);
        }
    }

    public void UpdateStatsForLevel(int level, StatType statType, float value) { }
}
