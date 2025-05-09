using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;

public class ItemDataManager : Singleton<ItemDataManager>
{
    #region Constants
    private const string ITEM_DB_PATH = "Items/Database";
    private const string DROP_TABLES_PATH = "Items/DropTables";
    private const string EFFECT_RANGES_PATH = "Items/EffectRanges";
    private const string ICONS_PATH = "Items/Icons";
    #endregion

    #region Fields
    private Dictionary<Guid, ItemData> itemDatabase = new();
    private Dictionary<MonsterType, DropTableData> dropTables = new();
    private ItemEffectRangeDatabase effectRangeDatabase = new();

    public List<ItemData> ItemDatabase;
    public List<DropTableData> DropTables;
    #endregion

    #region Data Loading
    public IEnumerator Initialize()
    {
        float progress = 0f;
        int steps = 0;

        yield return progress;
        yield return new WaitForSeconds(0.5f);
        LoadingManager.Instance.SetLoadingText("Loading Item Data...");

        var itemJSON = Resources.Load<TextAsset>($"{ITEM_DB_PATH}/ItemDatabase");
        ItemList itemData = null;

        var dropTableJSON = Resources.Load<TextAsset>($"{DROP_TABLES_PATH}/DropTables");
        DropTableList dropTableData = null;

        var effectRangeJSON = Resources.Load<TextAsset>(
            $"{EFFECT_RANGES_PATH}/EffectRangeDatabase"
        );

        if (itemJSON != null)
        {
            itemData = JsonConvert.DeserializeObject<ItemList>(itemJSON.text);
            steps += itemData.items.Count;
        }
        else
        {
            Logger.LogError(
                typeof(ItemDataManager),
                $" ItemDatabase.json not found at path: Resources/{ITEM_DB_PATH}/ItemDatabase"
            );
        }

        if (dropTableJSON != null)
        {
            dropTableData = JsonConvert.DeserializeObject<DropTableList>(dropTableJSON.text);
            steps += dropTableData.dropTables.Count;
        }
        else
        {
            Logger.LogError(
                typeof(ItemDataManager),
                $" DropTables.json not found at path: Resources/{DROP_TABLES_PATH}/DropTables"
            );
        }

        if (effectRangeJSON != null)
        {
            effectRangeDatabase = JsonConvert.DeserializeObject<ItemEffectRangeDatabase>(
                effectRangeJSON.text
            );
            steps += effectRangeDatabase.effectRanges.Count;
        }
        else
        {
            Logger.LogError(
                typeof(ItemDataManager),
                $" EffectRangeDatabase.json not found at path: Resources/{EFFECT_RANGES_PATH}/EffectRangeDatabase"
            );
        }

        if (itemData?.items != null)
        {
            itemDatabase = itemData.items.ToDictionary(item => item.ID);
            foreach (var item in itemDatabase)
            {
                progress += 1f / steps;
                yield return progress;
                yield return new WaitForSeconds(0.1f);
            }
        }
        else
        {
            Logger.LogError(
                typeof(ItemDataManager),
                " Failed to deserialize item data or items list is null"
            );
        }

        yield return new WaitForSeconds(0.5f);
        LoadingManager.Instance.SetLoadingText("Loading Drop Tables...");

        if (dropTableJSON != null)
        {
            var wrapper = JsonConvert.DeserializeObject<DropTableList>(dropTableJSON.text);
            dropTables = wrapper.dropTables.ToDictionary(dt => dt.enemyType);
            foreach (var dropTable in dropTables)
            {
                progress += 1f / steps;
                yield return progress;
                yield return new WaitForSeconds(0.1f);
                LoadingManager.Instance.SetLoadingText(
                    $"Loading Drop Tables... {progress * 100f:F0}%"
                );
            }
        }
        else
        {
            Logger.LogError(typeof(ItemDataManager), "No drop tables found.");
        }

        foreach (var item in itemDatabase)
        {
            item.Value.Icon = Resources.Load<Sprite>($"{ICONS_PATH}/{item.Value.ID}_Icon");
        }

        if (itemDatabase.Count > 0)
        {
            ItemDatabase = itemDatabase.Values.ToList();
        }
        if (dropTables.Count > 0)
        {
            DropTables = dropTables.Values.ToList();
        }
    }

    public List<ItemData> GetAllData()
    {
        return new List<ItemData>(itemDatabase.Values);
    }

    #endregion

    #region Data Access

    public ItemData GetData(Guid itemId)
    {
        if (itemDatabase.TryGetValue(itemId, out var itemData))
        {
            ItemData item = itemData.Clone();
            item.Icon = itemData.Icon;
            return item;
        }
        Logger.LogWarning(typeof(ItemDataManager), $"Item not found: {itemId}");
        return null;
    }

    public ItemEffectRange GetEffectRange(Guid effectRangeId)
    {
        return effectRangeDatabase.GetEffectRange(effectRangeId);
    }

    public bool HasData(Guid itemId)
    {
        return itemDatabase.ContainsKey(itemId);
    }

    public Dictionary<Guid, ItemData> GetDatabase()
    {
        return new Dictionary<Guid, ItemData>(itemDatabase);
    }

    public Dictionary<MonsterType, DropTableData> GetDropTables()
    {
        return new Dictionary<MonsterType, DropTableData>(dropTables);
    }

    #endregion
}
