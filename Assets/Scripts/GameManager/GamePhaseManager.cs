using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

// Daftar pulau untuk mempermudah identifikasi
public enum IslandName
{
    Airstrip,
    Waisai,
    Piaynemo,
    Arborek,
    Misool,
    Sawinggrai,
    Friwen,
    Gag
}

/// <summary>
/// Menyimpan mapping antara QuestID dan bobot persentase kontribusinya.
/// Atur dari Inspector — tidak perlu ubah kode saat menambah quest baru.
/// </summary>
[Serializable]
public class QuestProgressEntry
{
    [Tooltip("QuestID yang ada di ScriptableObject Quest (harus sama persis, case-sensitive)")]
    public string questID;

    [Tooltip("Bobot persentase (0–100) yang ditambahkan saat quest ini selesai")]
    [Range(0f, 100f)]
    public float percentageWeight;
}

[Serializable]
public class IslandQuestProgressGroup
{
    public IslandName island;

    [Tooltip("Quest dan bobot progres untuk pulau ini.")]
    public List<QuestProgressEntry> quests = new List<QuestProgressEntry>();
}

/// <summary>
/// Mengelola progres keseluruhan game berdasarkan quest-quest yang sudah diselesaikan player.
/// Progress diperbarui otomatis setiap kali QuestController.OnQuestCompleted terpicu.
/// </summary>
public class GamePhaseManager : MonoBehaviour
{
    private static GamePhaseManager _instance;
    public static GamePhaseManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<GamePhaseManager>();
                if (_instance == null && Application.isPlaying)
                {
                    GameObject go = new GameObject("GamePhaseManager");
                    _instance = go.AddComponent<GamePhaseManager>();
                    DontDestroyOnLoad(go);
                }
            }
            return _instance;
        }
        set => _instance = value;
    }

    // =========================================================
    // KONFIGURASI QUEST → PERSENTASE (atur dari Inspector)
    // =========================================================

    [Header("Quest → Progress Mapping per Island")]
    [Tooltip("Atur quest dan bobot progres secara terpisah untuk setiap pulau.")]
    public List<IslandQuestProgressGroup> questProgressByIsland = new List<IslandQuestProgressGroup>();

    // =========================================================
    // STATE RUNTIME (Read Only di Inspector)
    // =========================================================

    [Header("Progress Info (Read Only)")]
    [SerializeField] private float currentCompletionPercentage = 0f;
    [SerializeField] private bool isGagUnlocked = false;

    /// <summary>Kumpulan QuestID yang sudah diselesaikan. Di-persist via PlayerPrefs.</summary>
    private HashSet<string> completedQuestIDs = new HashSet<string>();

    // =========================================================
    // TARGET PULAU (Sinkron dengan MapController & PlayerPrefs)
    // =========================================================

    [SerializeField] private string _targetIslandName;
    public string targetIslandName
    {
        get
        {
            if (string.IsNullOrEmpty(_targetIslandName))
            {
                _targetIslandName = PlayerPrefs.GetString(SaveKeyTargetIsland, "03_Pianemo");
            }
            return _targetIslandName;
        }
        set
        {
            _targetIslandName = value;
            PlayerPrefs.SetString(SaveKeyTargetIsland, value);
            PlayerPrefs.Save();
            MapController.targetIslandScene = value;
        }
    }

    // =========================================================
    // EVENTS
    // =========================================================

    [Header("Events")]
    public UnityEvent<float> OnPercentageUpdated = new UnityEvent<float>();
    public UnityEvent OnGagUnlocked = new UnityEvent();
    public UnityEvent<IslandName> OnIslandCompleted = new UnityEvent<IslandName>();
    public UnityEvent OnGameCompleted = new UnityEvent();

    // =========================================================
    // SAVE / LOAD KEYS
    // =========================================================

    private const string SaveKeyCompletedQuests = "GPM_CompletedQuests";
    private const string SaveKeyGagUnlocked = "GPM_GagUnlocked";
    private const string SaveKeyGagDebugUnlocked = "GPM_GagDebugUnlocked";
    private const string SaveKeyTargetIsland = "TargetIsland";

    // =========================================================
    // UNITY LIFECYCLE
    // =========================================================

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);
            EnsureDefaultQuestProgressMapping();
            LoadProgress();
            CalculatePercentage();
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
    }

    private void EnsureDefaultQuestProgressMapping()
    {
        if (questProgressByIsland != null && questProgressByIsland.Count > 0) return;

        questProgressByIsland = new List<IslandQuestProgressGroup>
        {
            BuildIslandGroup(IslandName.Airstrip, ("01_Airstrip", 1f)),
            BuildIslandGroup(IslandName.Waisai, ("01_Waisai", 3f), ("02_Waisai", 3f), ("03_Waisai", 3f)),
            BuildIslandGroup(IslandName.Piaynemo, ("01_Piaynemo", 2.5f), ("02_Piaynemo", 2.5f), ("03_Piaynemo", 2.5f), ("04_Piaynemo", 2.5f), ("05_Piaynemo", 2.5f), ("06_Piaynemo", 2.5f)),
            BuildIslandGroup(IslandName.Misool, ("01_Misool", 1.875f), ("02_Misool", 1.875f), ("03_Misool", 1.875f), ("04_Misool", 1.875f), ("05_Misool", 1.875f), ("06_Misool", 1.875f), ("07_Misool", 1.875f), ("08_Misool", 1.875f)),
            BuildIslandGroup(IslandName.Arborek, ("01_Arborek", 2.5f), ("02_Arborek", 2.5f), ("03_Arborek", 2.5f), ("04_Arborek", 2.5f), ("05_Arborek", 2.5f), ("06_Arborek", 2.5f)),
            BuildIslandGroup(IslandName.Sawinggrai, ("01_Sawinggrai", 3f), ("02_Sawinggrai", 3f), ("03_Sawinggrai", 3f), ("04_Sawinggrai", 3f), ("05_Sawinggrai", 3f)),
            BuildIslandGroup(IslandName.Friwen, ("01_Friwen", 3.75f), ("02_Friwen", 3.75f), ("03_Friwen", 3.75f), ("04_Friwen", 3.75f)),
            BuildIslandGroup(IslandName.Gag, ("01_Gag", 1.875f), ("02_Gag", 1.875f), ("03_Gag", 1.875f), ("04_Gag", 1.875f), ("05_Gag", 1.875f), ("06_Gag", 1.875f), ("07_Gag", 1.875f), ("08_Gag", 1.875f))
        };
    }

    private static IslandQuestProgressGroup BuildIslandGroup(IslandName island, params (string id, float weight)[] entries)
    {
        var group = new IslandQuestProgressGroup { island = island };
        for (int i = 0; i < entries.Length; i++)
        {
            group.quests.Add(new QuestProgressEntry { questID = entries[i].id, percentageWeight = entries[i].weight });
        }
        return group;
    }

    private void OnEnable()
    {
        QuestController.OnQuestCompleted += HandleQuestCompleted;
    }

    private void OnDisable()
    {
        QuestController.OnQuestCompleted -= HandleQuestCompleted;
    }

    // =========================================================
    // HANDLER: QUEST SELESAI
    // =========================================================

    public void HandleQuestCompleted(string questID)
    {
        if (string.IsNullOrEmpty(questID)) return;

        if (completedQuestIDs.Contains(questID))
        {
            Debug.Log($"[GamePhaseManager] Quest '{questID}' sudah tercatat sebelumnya, dilewati.");
            return;
        }

        completedQuestIDs.Add(questID);
        SaveProgress();

        Debug.Log($"[GamePhaseManager] Quest '{questID}' selesai — persentase diperbarui.");
        CalculatePercentage();
    }

    // =========================================================
    // HITUNG PERSENTASE
    // =========================================================

    public void CalculatePercentage()
    {
        float total = 0f;

        if (questProgressByIsland != null && questProgressByIsland.Count > 0)
        {
            foreach (var islandGroup in questProgressByIsland)
            {
                if (islandGroup == null || islandGroup.quests == null) continue;

                bool islandDone = QuestJournalManager.IsPrerequisiteIslandCompleted(islandGroup.island);
                foreach (var entry in islandGroup.quests)
                {
                    if (entry == null) continue;
                    if (islandDone || IsQuestDoneForProgress(entry.questID, islandGroup.island))
                    {
                        total += entry.percentageWeight;
                    }
                }
            }
        }
        else
        {
            // Fallback jika mapping inspector kosong: hitung dari StoryManager
            if (StoryManager.Instance != null)
            {
                total = StoryManager.Instance.CalculateProgressPercentage();
            }
        }

        total = Mathf.Clamp(total, 0f, 100f);

        bool wasUnlocked = isGagUnlocked;
        bool unlockedNow = IsGagUnlockedPersistent();
        isGagUnlocked = unlockedNow;
        PlayerPrefs.SetInt(SaveKeyGagUnlocked, unlockedNow ? 1 : 0);

        if (unlockedNow && !wasUnlocked)
        {
            PlayerPrefs.Save();
            Debug.Log("<color=green>[GamePhaseManager] Semua pulau prasyarat selesai! Pulau Gag Terbuka!</color>");
            OnGagUnlocked?.Invoke();
        }

        currentCompletionPercentage = total;
        OnPercentageUpdated?.Invoke(currentCompletionPercentage);

        if (currentCompletionPercentage >= 100f)
        {
            Debug.Log("<color=gold>[GamePhaseManager] 100% Tercapai! Permainan Tamat!</color>");
            OnGameCompleted?.Invoke();
        }
    }

    private bool IsQuestDoneForProgress(string questID, IslandName island)
    {
        if (string.IsNullOrEmpty(questID)) return false;
        if (completedQuestIDs.Contains(questID) || QuestJournalManager.HasCompletedQuestIDPersistent(questID))
        {
            return true;
        }

        if (questID == "06_Piaynemo")
        {
            return completedQuestIDs.Contains("06_Pianemo") || QuestJournalManager.HasCompletedQuestIDPersistent("06_Pianemo");
        }
        if (island == IslandName.Friwen)
        {
            if (questID == "01_Friwen") return completedQuestIDs.Contains("07") || QuestJournalManager.HasCompletedQuestIDPersistent("07");
            if (questID == "02_Friwen") return completedQuestIDs.Contains("08") || QuestJournalManager.HasCompletedQuestIDPersistent("08");
            if (questID == "03_Friwen") return completedQuestIDs.Contains("09") || QuestJournalManager.HasCompletedQuestIDPersistent("09");
            if (questID == "04_Friwen") return completedQuestIDs.Contains("10") || QuestJournalManager.HasCompletedQuestIDPersistent("10");
        }
        if (island == IslandName.Gag && questID.EndsWith("_Gag", StringComparison.Ordinal))
        {
            string shortId = questID.Substring(0, 2);
            return completedQuestIDs.Contains(shortId) || QuestJournalManager.HasCompletedQuestIDPersistent(shortId);
        }

        return false;
    }

    // =========================================================
    // SAVE / LOAD PROGRESS
    // =========================================================

    private void SaveProgress()
    {
        string joined = string.Join(",", completedQuestIDs);
        PlayerPrefs.SetString(SaveKeyCompletedQuests, joined);
        PlayerPrefs.Save();
        QuestJournalManager.InvalidatePersistentQuestCache();
    }

    private void LoadProgress()
    {
        completedQuestIDs.Clear();

        string saved = PlayerPrefs.GetString(SaveKeyCompletedQuests, "");
        if (!string.IsNullOrEmpty(saved))
        {
            foreach (string id in saved.Split(','))
            {
                if (!string.IsNullOrWhiteSpace(id))
                    completedQuestIDs.Add(id.Trim());
            }
        }

        isGagUnlocked = IsGagUnlockedPersistent();
        PlayerPrefs.SetInt(SaveKeyGagUnlocked, isGagUnlocked ? 1 : 0);
        Debug.Log($"[GamePhaseManager] Progress dimuat. Quest selesai: {completedQuestIDs.Count}, Gag Unlocked: {isGagUnlocked}");
    }

    // =========================================================
    // PUBLIC API
    // =========================================================

    public static bool IsGagUnlockedPersistent()
    {
        if (PlayerPrefs.GetInt("GameCompleted", 0) == 1)
        {
            return true;
        }

        if (PlayerPrefs.GetInt(SaveKeyGagDebugUnlocked, 0) == 1)
        {
            return true;
        }

        return QuestJournalManager.AreAllPrerequisiteIslandsCompleted();
    }

    public float GetCurrentPercentage() => currentCompletionPercentage;

    public bool GetIsGagUnlocked()
    {
        isGagUnlocked = IsGagUnlockedPersistent();
        return isGagUnlocked;
    }

    public bool IsQuestCompleted(string questID) => completedQuestIDs.Contains(questID);

    /// <summary>
    /// Mencatat penyelesaian sebuah pulau secara terintegrasi (dipanggil dari supir kapal / trigger).
    /// </summary>
    public void CompleteIsland(IslandName island)
    {
        Debug.Log($"[GamePhaseManager] CompleteIsland({island}) diproses.");
        PlayerPrefs.SetInt("GPM_Island_" + island.ToString(), 1);
        PlayerPrefs.SetInt("Island_" + (int)island, 1);
        PlayerPrefs.SetInt("BoatTriviaDone_" + island.ToString(), 1);
        PlayerPrefs.Save();

        if (StoryManager.Instance != null)
        {
            StoryManager.Instance.CompleteIsland(island);
        }

        if (QuestJournalManager.Instance != null)
        {
            QuestJournalManager.Instance.UnlockIslandAward(island);
        }

        OnIslandCompleted?.Invoke(island);
        CalculatePercentage();
    }

    public void UnlockGag(bool unlock = true)
    {
        UnlockGag(unlock, false);
    }

    public void UnlockGag(bool unlock, bool isDebugForce)
    {
        if (!unlock)
        {
            isGagUnlocked = false;
            PlayerPrefs.DeleteKey(SaveKeyGagUnlocked);
            PlayerPrefs.DeleteKey(SaveKeyGagDebugUnlocked);
            PlayerPrefs.DeleteKey("GagUnlocked");
            PlayerPrefs.Save();
            Debug.Log("[GamePhaseManager] Status Pulau Gag dikunci.");
            return;
        }

        if (isDebugForce)
        {
            PlayerPrefs.SetInt(SaveKeyGagDebugUnlocked, 1);
        }

        bool canUnlock = isDebugForce || IsGagUnlockedPersistent();
        isGagUnlocked = canUnlock;
        PlayerPrefs.SetInt(SaveKeyGagUnlocked, canUnlock ? 1 : 0);
        PlayerPrefs.Save();
        if (canUnlock) OnGagUnlocked?.Invoke();
        Debug.Log($"[GamePhaseManager] Status Pulau Gag diubah: {canUnlock} (debugForce={isDebugForce})");
    }

    public void SetPercentageDirectly(float pct)
    {
        currentCompletionPercentage = Mathf.Clamp(pct, 0f, 100f);
        bool unlockedNow = IsGagUnlockedPersistent();
        isGagUnlocked = unlockedNow;
        PlayerPrefs.SetInt(SaveKeyGagUnlocked, unlockedNow ? 1 : 0);
        if (unlockedNow)
        {
            OnGagUnlocked?.Invoke();
        }
        OnPercentageUpdated?.Invoke(currentCompletionPercentage);
    }

    /// <summary>
    /// Reset seluruh progress (untuk New Game).
    /// </summary>
    [ContextMenu("Reset All Progress")]
    public void ResetAllProgress()
    {
        completedQuestIDs.Clear();
        isGagUnlocked = false;
        currentCompletionPercentage = 0f;

        PlayerPrefs.DeleteKey(SaveKeyCompletedQuests);
        PlayerPrefs.DeleteKey(SaveKeyGagUnlocked);
        PlayerPrefs.DeleteKey(SaveKeyGagDebugUnlocked);
        PlayerPrefs.DeleteKey("GagUnlocked");
        PlayerPrefs.DeleteKey(SaveKeyTargetIsland);
        PlayerPrefs.Save();
        QuestJournalManager.InvalidatePersistentQuestCache();

        CalculatePercentage();
        Debug.LogWarning("[GamePhaseManager] Seluruh progress GamePhaseManager telah direset.");
    }
}
