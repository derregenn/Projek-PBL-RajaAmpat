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
    public UnityEvent<float> OnPercentageUpdated;
    public UnityEvent OnGagUnlocked; // Event untuk membuka tombol Gag di UI Peta
    public UnityEvent<IslandName> OnIslandCompleted;
    public UnityEvent OnGameCompleted;

    // =========================================================
    // SAVE / LOAD KEYS
    // =========================================================

    private const string SaveKeyCompletedQuests = "GPM_CompletedQuests";
    private const string SaveKeyGagUnlocked = "GPM_GagUnlocked";
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
            LoadProgress();
            CalculatePercentage();
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
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

                foreach (var entry in islandGroup.quests)
                {
                    if (entry != null && completedQuestIDs.Contains(entry.questID))
                        total += entry.percentageWeight;
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

        // Cek threshold 85% untuk membuka Pulau Gag
        if (total >= 85f && !isGagUnlocked)
        {
            isGagUnlocked = true;
            PlayerPrefs.SetInt(SaveKeyGagUnlocked, 1);
            PlayerPrefs.Save();
            Debug.Log("<color=green>[GamePhaseManager] 85% Tercapai! Pulau Gag Terbuka!</color>");
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

    // =========================================================
    // SAVE / LOAD PROGRESS
    // =========================================================

    private void SaveProgress()
    {
        string joined = string.Join(",", completedQuestIDs);
        PlayerPrefs.SetString(SaveKeyCompletedQuests, joined);
        PlayerPrefs.Save();
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

        isGagUnlocked = PlayerPrefs.GetInt(SaveKeyGagUnlocked, 0) == 1;
        Debug.Log($"[GamePhaseManager] Progress dimuat. Quest selesai: {completedQuestIDs.Count}, Gag Unlocked: {isGagUnlocked}");
    }

    // =========================================================
    // PUBLIC API
    // =========================================================

    public float GetCurrentPercentage() => currentCompletionPercentage;

    public bool GetIsGagUnlocked() => isGagUnlocked;

    public bool IsQuestCompleted(string questID) => completedQuestIDs.Contains(questID);

    /// <summary>
    /// Mencatat penyelesaian sebuah pulau secara terintegrasi (dipanggil dari supir kapal / trigger).
    /// </summary>
    public void CompleteIsland(IslandName island)
    {
        Debug.Log($"[GamePhaseManager] CompleteIsland({island}) diproses.");
        PlayerPrefs.SetInt("Island_" + (int)island, 1);
        PlayerPrefs.Save();

        if (StoryManager.Instance != null)
        {
            StoryManager.Instance.CompleteIsland((int)island);
        }

        OnIslandCompleted?.Invoke(island);
        CalculatePercentage();
    }

    public void UnlockGag(bool unlock = true)
    {
        isGagUnlocked = unlock;
        PlayerPrefs.SetInt(SaveKeyGagUnlocked, unlock ? 1 : 0);
        PlayerPrefs.Save();
        if (unlock) OnGagUnlocked?.Invoke();
        Debug.Log($"[GamePhaseManager] Status Pulau Gag diubah: {unlock}");
    }

    public void SetPercentageDirectly(float pct)
    {
        currentCompletionPercentage = Mathf.Clamp(pct, 0f, 100f);
        if (currentCompletionPercentage >= 85f && !isGagUnlocked)
        {
            UnlockGag(true);
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
        PlayerPrefs.DeleteKey(SaveKeyTargetIsland);
        PlayerPrefs.Save();

        CalculatePercentage();
        Debug.LogWarning("[GamePhaseManager] Seluruh progress GamePhaseManager telah direset.");
    }
}
