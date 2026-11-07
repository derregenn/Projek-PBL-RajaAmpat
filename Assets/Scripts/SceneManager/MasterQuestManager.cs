using UnityEngine;
using UnityEngine.Events;

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
    public static MasterQuestManager Instance { get; private set; }

    [Header("Pengaturan Pulau")]
    [SerializeField] private IslandType currentIsland = IslandType.Misool;

    [Header("Event Perubahan Stage (Opsional)")]
    public UnityEvent<int> onStageChanged;

    private float lastBuluSeribuTime = -1f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private string StageKey => $"{currentIsland}_QuestStage";
    private string CollectibleKey => $"{currentIsland}_CollectCount";
    private string ExitPointKey => $"{currentIsland}_LastExitPoint";

    public IslandType CurrentIsland => currentIsland;

    public void SetActiveIsland(IslandType newIsland)
    {
        currentIsland = newIsland;
        Debug.Log($"<color=yellow>[MasterQuestManager] Pulau Aktif Berpindah Ke: {newIsland}</color>");
    }

    // --- MANAJEMEN QUEST STAGE ---

    public int CurrentStage
    {
        get => PlayerPrefs.GetInt(StageKey, 0);
        private set
        {
            PlayerPrefs.SetInt(StageKey, value);
            PlayerPrefs.Save();
        }
    }

    public int GetStageOfIsland(IslandType island)
    {
        return PlayerPrefs.GetInt($"{island}_QuestStage", 0);
    }

    public void AdvanceStageTo(int newStage)
    {
        // PERBAIKAN: Hapus pembatasan if (newStage <= CurrentStage) 
        // agar stage bisa di-set ke angka berapa pun (termasuk reset ke 0)
        CurrentStage = newStage;
        Debug.Log($"<color=green>[{currentIsland}] Quest Stage Naik/Berubah Ke: {newStage}</color>");

        onStageChanged?.Invoke(newStage);
        RefreshCurrentLandState();
    }

    // --- MANAJEMEN COLLECTIBLES / HAMA / ITEM ---

    public int CollectibleCount => PlayerPrefs.GetInt(CollectibleKey, 0);

    public void AddCollectible(int targetAmount, int stageOnComplete)
    {

        // PERBAIKAN: Tambah item terlebih dahulu tanpa diblokir guard clause
        int count = CollectibleCount + 1;
        PlayerPrefs.SetInt(CollectibleKey, count);
        PlayerPrefs.Save();
        Debug.Log($"<color=cyan>[{currentIsland}] Progress Item/Hama: {count}/{targetAmount}</color>");

        // Jika jumlah target tercapai, baru naikkan stage
        if (count >= targetAmount)
        {
            AdvanceStageTo(stageOnComplete);
        }
    }

    // --- FUNGSI HELPER UNTUK UNITY EVENT INSPECTOR ---

    /// <summary>
    /// Dipanggil dari Inspector UnityEvent atau kode C# untuk pengambilan Bulu Seribu (Target 3, Stage 5)
    /// </summary>
    public void AddBuluSeribu()
    {
        // Mencegah penambahan ganda jika dipanggil bersamaan dari kode C# dan UnityEvent Inspector
        if (Mathf.Approximately(Time.time, lastBuluSeribuTime))
        {
            return;
        }
        lastBuluSeribuTime = Time.time;

        // MasterQuestManager bertahan antar-scene, jadi pastikan event ini
        // mencatat progress pada pulau yang memiliki quest Bulu Seribu.
        SetActiveIsland(IslandType.Misool);
        AddCollectible(3, 5);
    }

    public void ResetCollectibleCount()
    {
        PlayerPrefs.SetInt(CollectibleKey, 0);
        PlayerPrefs.Save();
    }

    // --- MANAJEMEN SPAWN / EXIT POINT ---

    public void SetLastExitPoint(string exitPointName)
    {
        PlayerPrefs.SetString(ExitPointKey, exitPointName);
        PlayerPrefs.Save();
    }

    public string ConsumeLastExitPoint()
    {
        string exit = PlayerPrefs.GetString(ExitPointKey, "DEFAULT");
        PlayerPrefs.SetString(ExitPointKey, "DEFAULT");
        PlayerPrefs.Save();
        return exit;
    }

    private void RefreshCurrentLandState()
    {
        switch (currentIsland)
        {
            case IslandType.Misool:
                MisoolLandSceneManager.Instance?.RefreshLandState();
                break;
        }
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
