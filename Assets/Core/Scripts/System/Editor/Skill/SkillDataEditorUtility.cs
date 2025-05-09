using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class SkillDataEditorUtility
{
    #region Constants
    private const string SKILL_DB_PATH = "Skills/Json";
    private const string SKILL_ICON_PATH = "Skills/Icons";
    private const string SKILL_PREFAB_PATH = "Skills/Prefabs";
    private const string SKILL_STAT_PATH = "Skills/Stats";
    #endregion

    #region Data Management
    private static Dictionary<SkillID, SkillData> skillDatabase = new();
    private static Dictionary<SkillID, Dictionary<int, SkillStatData>> statDatabase = new();
    #endregion

    public static Dictionary<SkillID, SkillData> GetSkillDatabase()
    {
        if (!skillDatabase.Any())
        {
            LoadSkillDatabase();
        }
        return new Dictionary<SkillID, SkillData>(skillDatabase);
    }

    public static Dictionary<SkillID, Dictionary<int, SkillStatData>> GetStatDatabase()
    {
        if (!statDatabase.Any())
        {
            LoadStatDatabase();
        }
        return statDatabase;
    }

    private static void LoadSkillDatabase()
    {
        try
        {
            skillDatabase.Clear();
            foreach (SkillID skillId in Enum.GetValues(typeof(SkillID)))
            {
                if (skillId == SkillID.None)
                    continue;

                var skillData = JSONIO<SkillData>.LoadData(SKILL_DB_PATH, skillId.ToString());
                if (skillData != null)
                {
                    skillData.Icon = ResourceIO<Sprite>.LoadData(
                        $"{SKILL_ICON_PATH}/{skillData.ID}/{skillData.ID}_Icon"
                    );

                    skillData.BasePrefab = ResourceIO<GameObject>.LoadData(
                        $"{SKILL_PREFAB_PATH}/{skillData.ID}/{skillData.ID}_Prefab"
                    );

                    if (skillData.Type == SkillType.Projectile)
                    {
                        skillData.ProjectilePrefab = ResourceIO<GameObject>.LoadData(
                            $"{SKILL_PREFAB_PATH}/{skillData.ID}/{skillData.ID}_Projectile"
                        );
                    }

                    var prefabs = Resources.LoadAll<GameObject>(
                        $"{SKILL_PREFAB_PATH}/{skillData.ID}/"
                    );

                    if (prefabs.Length > 0)
                    {
                        skillData.PrefabsByLevel = new GameObject[prefabs.Length - 1];
                        int cnt = 0;
                        foreach (var prefab in prefabs)
                        {
                            if (prefab.name.Contains("Level_"))
                            {
                                skillData.PrefabsByLevel[cnt] = prefab;
                                cnt++;
                            }
                        }
                    }

                    skillDatabase[skillId] = skillData;
                }
            }
        }
        catch (Exception e)
        {
            Logger.LogError(
                typeof(SkillDataEditorUtility),
                $"Error loading skill database: {e.Message}"
            );
            skillDatabase = new Dictionary<SkillID, SkillData>();
        }
    }

    private static void LoadStatDatabase()
    {
        try
        {
            statDatabase.Clear();

            LoadStatsFromCSV("ProjectileSkillStats");
            LoadStatsFromCSV("AreaSkillStats");
            LoadStatsFromCSV("PassiveSkillStats");
        }
        catch (Exception e)
        {
            Logger.LogError(
                typeof(SkillDataEditorUtility),
                $"Error loading stat database: {e.Message}"
            );
            statDatabase = new Dictionary<SkillID, Dictionary<int, SkillStatData>>();
        }
    }

    private static void LoadStatsFromCSV(string fileName)
    {
        SkillType skillType = SkillType.None;

        if (fileName.Contains("ProjectileSkillStats"))
        {
            skillType = SkillType.Projectile;
        }
        else if (fileName.Contains("AreaSkillStats"))
        {
            skillType = SkillType.Area;
        }
        else if (fileName.Contains("PassiveSkillStats"))
        {
            skillType = SkillType.Passive;
        }

        var includeFields = SkillStatFilters.GetFieldsForSkillType(skillType);

        var stats = CSVIO<SkillStatData>.LoadBulkData(SKILL_STAT_PATH, fileName, includeFields);
        foreach (var stat in stats)
        {
            if (!statDatabase.ContainsKey(stat.skillID))
            {
                statDatabase[stat.skillID] = new Dictionary<int, SkillStatData>();
            }
            statDatabase[stat.skillID][stat.level] = stat;
        }
    }

    public static void SaveSkillData(SkillData skillData)
    {
        if (skillData == null)
            return;
        if (skillData.ID == SkillID.None || skillData.Type == SkillType.None)
        {
            Logger.LogError(
                typeof(SkillDataEditorUtility),
                "Cannot save skill data with None ID or Type"
            );
            return;
        }

        try
        {
            SaveSkillResources(skillData);

            JSONIO<SkillData>.SaveData(SKILL_DB_PATH, skillData.ID.ToString(), skillData);
            skillDatabase[skillData.ID] = skillData.Clone() as SkillData;

            SaveStatData(skillData);

            SaveStatDatabase();

            LoadSkillDatabase();
        }
        catch (Exception e)
        {
            Logger.LogError(
                typeof(SkillDataEditorUtility),
                $"Error saving skill data: {e.Message}"
            );
        }
    }

    private static void SaveStatData(SkillData skillData)
    {
        if (skillData.ID == SkillID.None || skillData.Type == SkillType.None)
            return;

        if (!statDatabase.ContainsKey(skillData.ID))
        {
            statDatabase[skillData.ID] = new Dictionary<int, SkillStatData>();

            int maxLevel = 5;

            for (int level = 1; level <= maxLevel; level++)
            {
                var newStat = new SkillStatData
                {
                    skillID = skillData.ID,
                    level = level,
                    maxSkillLevel = maxLevel,
                    damage = 10f + (level - 1) * 5f,
                    elementalPower = 1f + (level - 1) * 0.2f,
                    element = skillData.Element,
                };

                switch (skillData.Type)
                {
                    case SkillType.Projectile:
                        newStat.projectileSpeed = 10f;
                        newStat.projectileScale = 1f;
                        newStat.shotInterval = 0.5f;
                        newStat.pierceCount = 1;
                        newStat.attackRange = 10f;
                        break;

                    case SkillType.Area:
                        newStat.radius = 5f;
                        newStat.duration = 3f;
                        newStat.tickRate = 1f;
                        newStat.isPersistent = false;
                        break;

                    case SkillType.Passive:
                        newStat.effectDuration = 5f;
                        newStat.cooldown = 10f;
                        newStat.triggerChance = 1f;
                        break;
                }

                statDatabase[skillData.ID][level] = newStat;
            }
        }
    }

    private static void SaveSkillResources(SkillData skillData)
    {
        if (skillData.Icon != null)
        {
            ResourceIO<Sprite>.SaveData(
                $"{SKILL_ICON_PATH}/{skillData.ID}/{skillData.ID}_Icon",
                skillData.Icon
            );
        }

        if (skillData.BasePrefab != null)
        {
            ResourceIO<GameObject>.SaveData(
                $"{SKILL_PREFAB_PATH}/{skillData.ID}/{skillData.ID}_Prefab",
                skillData.BasePrefab
            );
        }

        if (skillData.Type == SkillType.Projectile && skillData.ProjectilePrefab != null)
        {
            ResourceIO<GameObject>.SaveData(
                $"{SKILL_PREFAB_PATH}/{skillData.ID}/{skillData.ID}_Projectile",
                skillData.ProjectilePrefab
            );
        }

        if (skillData.PrefabsByLevel != null)
        {
            for (int i = 0; i < skillData.PrefabsByLevel.Length; i++)
            {
                if (skillData.PrefabsByLevel[i] != null)
                {
                    ResourceIO<GameObject>.SaveData(
                        $"{SKILL_PREFAB_PATH}/{skillData.ID}/{skillData.ID}_Level_{i + 1}",
                        skillData.PrefabsByLevel[i]
                    );
                }
            }
        }
    }

    public static void SaveStatDatabase()
    {
        var projectileStats = new List<SkillStatData>();
        var areaStats = new List<SkillStatData>();
        var passiveStats = new List<SkillStatData>();

        HashSet<(SkillID, int)> processedStats = new HashSet<(SkillID, int)>();

        foreach (var skillStatsPair in statDatabase)
        {
            SkillID skillId = skillStatsPair.Key;
            if (!skillDatabase.ContainsKey(skillId))
                continue;

            var skillData = skillDatabase[skillId];
            var skillStats = skillStatsPair.Value;

            foreach (var levelStatPair in skillStats)
            {
                int level = levelStatPair.Key;
                SkillStatData stat = levelStatPair.Value;

                if (processedStats.Contains((skillId, level)))
                    continue;

                processedStats.Add((skillId, level));

                switch (skillData.Type)
                {
                    case SkillType.Projectile:
                        projectileStats.Add(stat);
                        break;
                    case SkillType.Area:
                        areaStats.Add(stat);
                        break;
                    case SkillType.Passive:
                        passiveStats.Add(stat);
                        break;
                }
            }
        }

        CSVIO<SkillStatData>.SaveBulkData(
            SKILL_STAT_PATH,
            "ProjectileSkillStats",
            projectileStats,
            true,
            SkillStatFilters.GetFieldsForSkillType(SkillType.Projectile)
        );

        CSVIO<SkillStatData>.SaveBulkData(
            SKILL_STAT_PATH,
            "AreaSkillStats",
            areaStats,
            true,
            SkillStatFilters.GetFieldsForSkillType(SkillType.Area)
        );

        CSVIO<SkillStatData>.SaveBulkData(
            SKILL_STAT_PATH,
            "PassiveSkillStats",
            passiveStats,
            true,
            SkillStatFilters.GetFieldsForSkillType(SkillType.Passive)
        );
    }

    public static void DeleteSkillData(SkillID skillId)
    {
        try
        {
            if (skillDatabase.Remove(skillId))
            {
                JSONIO<SkillData>.DeleteData(SKILL_DB_PATH, skillId.ToString());

                DeleteSkillResources(skillId);

                statDatabase.Remove(skillId);
                SaveStatDatabase();

                AssetDatabase.Refresh();
            }
        }
        catch (Exception e)
        {
            Logger.LogError(
                typeof(SkillDataEditorUtility),
                $"Error deleting skill {skillId}: {e.Message}"
            );
        }
    }

    private static void DeleteSkillResources(SkillID skillId)
    {
        ResourceIO<Sprite>.DeleteData($"{SKILL_ICON_PATH}/{skillId}/");

        ResourceIO<GameObject>.DeleteData($"{SKILL_PREFAB_PATH}/{skillId}/");
    }

    public static void InitializeDefaultData()
    {
        try
        {
            ClearAllData();

            skillDatabase = new Dictionary<SkillID, SkillData>();
            statDatabase = new Dictionary<SkillID, Dictionary<int, SkillStatData>>();

            string resourceRoot = Path.Combine(Application.dataPath, "Resources");
            CleanDirectory(Path.Combine(resourceRoot, SKILL_DB_PATH));
            CleanDirectory(Path.Combine(resourceRoot, SKILL_ICON_PATH));
            CleanDirectory(Path.Combine(resourceRoot, SKILL_PREFAB_PATH));
            CleanDirectory(Path.Combine(resourceRoot, SKILL_STAT_PATH));

            CreateDefaultCSVFiles();

            AssetDatabase.Refresh();
        }
        catch (Exception e)
        {
            Logger.LogError(typeof(SkillDataEditorUtility), $"Error resetting data: {e.Message}");
        }
    }

    private static void ClearAllData()
    {
        try
        {
            JSONIO<SkillData>.ClearAll(SKILL_DB_PATH);
            CSVIO<SkillStatData>.ClearAll(SKILL_STAT_PATH);
            CSVIO<SkillStatData>.ClearAll(SKILL_STAT_PATH);
            ResourceIO<Sprite>.ClearCache();
            ResourceIO<GameObject>.ClearCache();

            skillDatabase.Clear();
            statDatabase.Clear();

            string resourceRoot = Path.Combine(Application.dataPath, "Resources");
            if (Directory.Exists(resourceRoot))
            {
                string[] paths = new[]
                {
                    SKILL_DB_PATH,
                    SKILL_ICON_PATH,
                    SKILL_PREFAB_PATH,
                    SKILL_STAT_PATH,
                };
                foreach (var path in paths)
                {
                    CleanDirectory(Path.Combine(resourceRoot, path));
                }
            }

            AssetDatabase.Refresh();
        }
        catch (Exception e)
        {
            Logger.LogError(typeof(SkillDataEditorUtility), $"Error clearing data: {e.Message}");
        }
    }

    private static void CleanDirectory(string path)
    {
        if (Directory.Exists(path))
        {
            try
            {
                string[] files = Directory.GetFiles(path, "*", SearchOption.AllDirectories);
                foreach (string file in files)
                {
                    if (File.Exists(file))
                    {
                        string assetPath = file.Replace('\\', '/');
                        if (assetPath.StartsWith(Application.dataPath))
                        {
                            assetPath = "Assets" + assetPath.Substring(Application.dataPath.Length);
                            AssetDatabase.DeleteAsset(assetPath);
                        }
                    }
                }

                Directory.Delete(path, true);
                Directory.CreateDirectory(path);
            }
            catch (Exception e)
            {
                Logger.LogError(
                    typeof(SkillDataEditorUtility),
                    $"Error cleaning directory {path}: {e.Message}"
                );
            }
        }
        else
        {
            Directory.CreateDirectory(path);
        }
    }

    private static void CreateDefaultCSVFiles()
    {
        var headers = new List<string>()
        {
            "skillid",
            "level",
            "damage",
            "maxskilllevel",
            "element",
            "elementalpower",
        };

        var propertyNames = typeof(SkillStatData)
            .GetProperties()
            .Where(p =>
                p.CanRead
                && p.CanWrite
                && !headers.Contains(p.Name.ToLower())
                && p.Name != "SkillID"
                && p.Name != "Level"
                && p.Name != "Damage"
                && p.Name != "MaxSkillLevel"
                && p.Name != "Element"
                && p.Name != "ElementalPower"
            )
            .OrderBy(p => p.Name)
            .Select(p => p.Name.ToLower());

        headers.AddRange(propertyNames);

        string headerLine = string.Join(",", headers);

        CSVIO<SkillStatData>.CreateDefaultFile(SKILL_STAT_PATH, "ProjectileSkillStats", headerLine);
        CSVIO<SkillStatData>.CreateDefaultFile(SKILL_STAT_PATH, "AreaSkillStats", headerLine);
        CSVIO<SkillStatData>.CreateDefaultFile(SKILL_STAT_PATH, "PassiveSkillStats", headerLine);
    }

    public static void Save()
    {
        foreach (var skill in skillDatabase.Values)
        {
            JSONIO<SkillData>.SaveData(SKILL_DB_PATH, skill.ID.ToString(), skill);
        }
    }
}
