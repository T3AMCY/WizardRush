using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : Singleton<GameManager>
{
    private ItemSystem itemSystem;
    private SkillSystem skillSystem;
    private PlayerSystem playerSystem;
    private MonsterSystem monsterSystem;
    private List<Monster> monsters = new();

    public ItemSystem ItemSystem => itemSystem;
    public SkillSystem SkillSystem => skillSystem;
    public PlayerSystem PlayerSystem => playerSystem;
    public MonsterSystem MonsterSystem => monsterSystem;
    public List<Monster> Monsters => monsters;
    private readonly WaitForSeconds LOADING_TIME = new WaitForSeconds(0.3f);

    private void Start()
    {
        UIManager.Instance.Initialize();
        LoadingManager.Instance.Initialize();

        List<Func<IEnumerator>> operations = new()
        {
            SkillDataManager.Instance.Initialize,
            ItemDataManager.Instance.Initialize,
            PlayerDataManager.Instance.Initialize,
            PoolManager.Instance.Initialize,
            LoadSystems,
        };

        LoadingManager.Instance.LoadScene(
            SceneType.Main_Title,
            operations
        );
    }

    public IEnumerator LoadSystems()
    {
        float progress = 0f;
        int steps = 8;
        yield return progress;
        yield return LOADING_TIME;

        LoadingManager.Instance.SetLoadingText("Initializing Skill System...");
        skillSystem = new GameObject("SkillSystem").AddComponent<SkillSystem>();
        skillSystem.transform.SetParent(transform);
        skillSystem.Initialize();
        progress += 1f / steps;
        yield return progress;
        yield return LOADING_TIME;

        LoadingManager.Instance.SetLoadingText("Initializing Item System...");
        itemSystem = new GameObject("ItemSystem").AddComponent<ItemSystem>();
        itemSystem.transform.SetParent(transform);
        itemSystem.Initialize();
        progress += 1f / steps;
        yield return progress;
        yield return LOADING_TIME;

        playerSystem = new GameObject("PlayerSystem").AddComponent<PlayerSystem>();
        playerSystem.transform.SetParent(transform);
        playerSystem.Initialize();
        progress += 1f / steps;
        yield return progress;
        yield return LOADING_TIME;

        LoadingManager.Instance.SetLoadingText("Initializing Monster System...");
        monsterSystem = new GameObject("MonsterSystem").AddComponent<MonsterSystem>();
        monsterSystem.transform.SetParent(transform);
        monsterSystem.Initialize();
        progress += 1f / steps;
        yield return progress;
        yield return LOADING_TIME;
        progress = 1f;
        yield return progress;
    }

    public void SaveGameData()
    {
        if (PlayerSystem != null)
        {
            if (PlayerSystem.Player != null)
            {
                PlayerSystem.SavePlayerData();
            }
        }
    }

    public void ClearGameData()
    {
        PlayerDataManager.Instance.ClearAllRuntimeData();
    }

    public bool HasSaveData()
    {
        return PlayerDataManager.Instance.HasSaveData();
    }

    private void OnDisable()
    {
        SaveGameData();
    }

}
