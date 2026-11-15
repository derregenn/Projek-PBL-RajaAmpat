using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public enum IslandType
{
    Airstrip,
    Waisai,
    Piaynemo,
    Misool,
    Arborek,
    Sawinggrai,
    Friwen,
    Gag
}

public class MasterQuestManager : MonoBehaviour
{
    private static MasterQuestManager _instance;
    public static MasterQuestManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<MasterQuestManager>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("MasterQuestManager");
                    _instance = go.AddComponent<MasterQuestManager>();
                    DontDestroyOnLoad(go);
                }
            }
            return _instance;
        }
    }

    [Header("Pengaturan Pulau")]
    [SerializeField] private IslandType currentIsland = IslandType.Misool;

    [Header("Event Perubahan Stage (Opsional)")]
    public UnityEvent<int> onStageChanged;

    private int lastBuluSeribuFrame = -1;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(_instance.gameObject);
        }

        _instance = this;
        transform.SetParent(null);
        DontDestroyOnLoad(gameObject);

        DetectIslandFromActiveScene(SceneManager.GetActiveScene().name);
    }

    private void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
        }
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        DetectIslandFromActiveScene(scene.name);
        RefreshCurrentLandState();
    }

    public void DetectIslandFromActiveScene(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName)) return;
        string lower = sceneName.ToLower();

        if (lower.Contains("misool")) SetActiveIsland(IslandType.Misool);
        else if (lower.Contains("arborek")) SetActiveIsland(IslandType.Arborek);
        else if (lower.Contains("sawinggrai")) SetActiveIsland(IslandType.Sawinggrai);
        else if (lower.Contains("friwen")) SetActiveIsland(IslandType.Friwen);
        else if (lower.Contains("gag")) SetActiveIsland(IslandType.Gag);
        else if (lower.Contains("pianemo") || lower.Contains("piaynemo")) SetActiveIsland(IslandType.Piaynemo);
        else if (lower.Contains("waisai")) SetActiveIsland(IslandType.Waisai);
        else if (lower.Contains("airstrip")) SetActiveIsland(IslandType.Airstrip);
    }

    private string StageKey => $"{currentIsland}_QuestStage";
    private string CollectibleKey => $"{currentIsland}_CollectCount";
    private string ExitPointKey => $"{currentIsland}_LastExitPoint";

    public IslandType CurrentIsland => currentIsland;

    public void SetActiveIsland(IslandType newIsland)
    {
        currentIsland = newIsland;
        Debug.Log($"<color=yellow>[MasterQuestManager] Pulau Aktif: {newIsland}</color>");
    }

    // --- MANAJEMEN QUEST STAGE ---

    public int CurrentStage
    {
        get => PlayerPrefs.GetInt(StageKey, 0);
        private set
        {
            PlayerPrefs.SetInt(StageKey, value);

            // Selaraskan dengan key legacy Misool jika pulau aktif adalah Misool
            if (currentIsland == IslandType.Misool)
            {
                PlayerPrefs.SetInt("Misool_QuestStage", value);
            }

            PlayerPrefs.Save();
        }
    }

    public int GetStageOfIsland(IslandType island)
    {
        return PlayerPrefs.GetInt($"{island}_QuestStage", 0);
    }

    public void AdvanceStageTo(int newStage)
    {
        CurrentStage = newStage;
        Debug.Log($"<color=green>[MasterQuestManager: {currentIsland}] Quest Stage Diperbarui Ke: {newStage}</color>");

        onStageChanged?.Invoke(newStage);
        RefreshCurrentLandState();
    }

    // --- MANAJEMEN COLLECTIBLES / HAMA / ITEM ---

    public int CollectibleCount => PlayerPrefs.GetInt(CollectibleKey, 0);

    public void AddCollectible(int targetAmount, int stageOnComplete)
    {
        int count = CollectibleCount + 1;
        PlayerPrefs.SetInt(CollectibleKey, count);

        if (currentIsland == IslandType.Misool)
        {
            PlayerPrefs.SetInt("Misool_CotsCount", count);
        }

        PlayerPrefs.Save();
        Debug.Log($"<color=cyan>[MasterQuestManager: {currentIsland}] Item/Hama Terkumpul: {count}/{targetAmount}</color>");

        if (count >= targetAmount)
        {
            AdvanceStageTo(stageOnComplete);
        }
    }

    // --- FUNGSI HELPER UNTUK BULU SERIBU ---

    public void AddBuluSeribu()
    {
        // Hindari eksekusi dobel pada frame yang sama
        if (Time.frameCount == lastBuluSeribuFrame) return;
        lastBuluSeribuFrame = Time.frameCount;

        SetActiveIsland(IslandType.Misool);
        AddCollectible(3, 5);
    }

    public void ResetCollectibleCount()
    {
        PlayerPrefs.SetInt(CollectibleKey, 0);
        if (currentIsland == IslandType.Misool)
        {
            PlayerPrefs.SetInt("Misool_CotsCount", 0);
        }
        PlayerPrefs.Save();
    }

    // --- MANAJEMEN SPAWN / EXIT POINT ---

    public void SetLastExitPoint(string exitPointName)
    {
        PlayerPrefs.SetString(ExitPointKey, exitPointName);
        if (currentIsland == IslandType.Misool)
        {
            PlayerPrefs.SetString("Misool_LastExitPoint", exitPointName);
        }
        PlayerPrefs.Save();
    }

    public string ConsumeLastExitPoint()
    {
        string exit = PlayerPrefs.GetString(ExitPointKey, "DEFAULT");
        PlayerPrefs.SetString(ExitPointKey, "DEFAULT");
        if (currentIsland == IslandType.Misool)
        {
            PlayerPrefs.SetString("Misool_LastExitPoint", "DEFAULT");
        }
        PlayerPrefs.Save();
        return exit;
    }

    public void RefreshCurrentLandState()
    {
        switch (currentIsland)
        {
            case IslandType.Misool:
                MisoolLandSceneManager.Instance?.RefreshLandState();
                var seaManager = FindFirstObjectByType<MisoolSeaManager>();
                seaManager?.RefreshSeaState();
                break;
            default:
                IslandCompletedStateRestorer.RestoreActiveSceneState();
                break;
        }
    }

    [ContextMenu("Reset All Progress (All Islands)")]
    public void ResetAllProgress()
    {
        foreach (IslandType island in Enum.GetValues(typeof(IslandType)))
        {
            PlayerPrefs.DeleteKey($"{island}_QuestStage");
            PlayerPrefs.DeleteKey($"{island}_CollectCount");
            PlayerPrefs.DeleteKey($"{island}_LastExitPoint");
        }
        PlayerPrefs.DeleteKey("Misool_QuestStage");
        PlayerPrefs.DeleteKey("Misool_CotsCount");
        PlayerPrefs.DeleteKey("Misool_LastExitPoint");
        PlayerPrefs.Save();

        Debug.LogWarning("[MasterQuestManager] Seluruh progres pulau telah direset.");
        RefreshCurrentLandState();
    }

    [ContextMenu("Reset Current Island Quest Data")]
    public void ResetCurrentIslandData()
    {
        PlayerPrefs.DeleteKey(StageKey);
        PlayerPrefs.DeleteKey(CollectibleKey);
        PlayerPrefs.DeleteKey(ExitPointKey);
        PlayerPrefs.Save();
        Debug.LogWarning($"[MasterQuestManager] Data Quest untuk pulau {currentIsland} BERHASIL DIRESET!");
        RefreshCurrentLandState();
    }
}
