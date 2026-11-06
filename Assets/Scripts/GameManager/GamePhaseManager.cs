using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

// Daftar pulau untuk mempermudah identifikasi (tetap ada agar kode lain yang mereferensikannya tidak error)
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
/// Progress diperbarui otomatis setiap kali QuestController.OnQuestCompleted terpicu —
/// tidak perlu memanggil CompleteIsland() secara manual lagi.
/// </summary>
public class GamePhaseManager : MonoBehaviour
{
    public static GamePhaseManager Instance;

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
    // EVENTS
    // =========================================================

    [Header("Events")]
    public UnityEvent<float> OnPercentageUpdated;
    public UnityEvent OnGagUnlocked; // Event untuk membuka tombol Gag di UI Peta
    internal string targetIslandName;

    // =========================================================
    // SAVE / LOAD KEYS
    // =========================================================


    private const string SaveKeyCompletedQuests = "GPM_CompletedQuests";
    private const string SaveKeyGagUnlocked = "GPM_GagUnlocked";

    // =========================================================
    // UNITY LIFECYCLE
    // =========================================================

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadProgress();
            CalculatePercentage();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnEnable()
    {
        // Dengarkan event dari QuestController setiap kali sebuah quest selesai
        QuestController.OnQuestCompleted += HandleQuestCompleted;
    }

    private void OnDisable()
    {
        QuestController.OnQuestCompleted -= HandleQuestCompleted;
    }

    // =========================================================
    // HANDLER: QUEST SELESAI
    // =========================================================

    /// <summary>
    /// Dipanggil otomatis oleh QuestController.OnQuestCompleted
    /// setiap kali player menyelesaikan sebuah quest.
    /// </summary>
    private void HandleQuestCompleted(string questID)
    {
        if (string.IsNullOrEmpty(questID)) return;

        // Jangan proses quest yang sama dua kali
        if (completedQuestIDs.Contains(questID))
        {
            Debug.Log($"[GamePhaseManager] Quest '{questID}' sudah tercatat sebelumnya, dilewati.");
            return;
        }

        // Periksa apakah quest ini ada di tabel mapping
        bool foundInTable = false;
        foreach (var islandGroup in questProgressByIsland)
        {
            if (islandGroup == null || islandGroup.quests == null) continue;

            foreach (var entry in islandGroup.quests)
            {
                if (entry != null && entry.questID == questID)
                {
                    foundInTable = true;
                    break;
                }
            }

            if (foundInTable) break;
        }

        if (!foundInTable)
        {
            // Quest ini selesai tapi tidak dikonfigurasi di tabel — tidak pengaruhi persentase
            Debug.Log($"[GamePhaseManager] Quest '{questID}' tidak ada di mapping quest per pulau, progres tidak diubah.");
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

    private void CalculatePercentage()
    {
        float total = 0f;

        foreach (var islandGroup in questProgressByIsland)
        {
            if (islandGroup == null || islandGroup.quests == null) continue;

            foreach (var entry in islandGroup.quests)
            {
                if (entry != null && completedQuestIDs.Contains(entry.questID))
                    total += entry.percentageWeight;
            }
        }

        // Clamp agar tidak melebihi 100%
        total = Mathf.Clamp(total, 0f, 100f);

        // Cek threshold 85% untuk membuka Pulau Gag
        if (total >= 85f && !isGagUnlocked)
        {
            isGagUnlocked = true;
            PlayerPrefs.SetInt(SaveKeyGagUnlocked, 1);
            PlayerPrefs.Save();
            Debug.Log("[GamePhaseManager] 85% Tercapai! Pulau Gag Terbuka!");
            OnGagUnlocked?.Invoke();
        }

        currentCompletionPercentage = total;
        OnPercentageUpdated?.Invoke(currentCompletionPercentage);

        if (currentCompletionPercentage >= 100f)
        {
            Debug.Log("[GamePhaseManager] 100% Tercapai! Mainkan Ending!");
        }
    }

    // =========================================================
    // SAVE / LOAD PROGRESS
    // =========================================================

    private void SaveProgress()
    {
        // Simpan semua completed questID sebagai satu string CSV
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

        Debug.Log($"[GamePhaseManager] Progress dimuat. Quest selesai: {completedQuestIDs.Count}");
    }

    // =========================================================
    // PUBLIC API (tetap kompatibel dengan kode lama)
    // =========================================================

    public float GetCurrentPercentage() => currentCompletionPercentage;

    public bool GetIsGagUnlocked() => isGagUnlocked;

    /// <summary>
    /// Cek apakah sebuah quest sudah diselesaikan.
    /// </summary>
    public bool IsQuestCompleted(string questID) => completedQuestIDs.Contains(questID);

    /// <summary>
    /// [LEGACY - Tidak digunakan lagi] Sebelumnya dipanggil manual saat naik perahu.
    /// Sekarang progress ditentukan otomatis dari quest yang diselesaikan.
    /// Fungsi ini dibiarkan agar tidak memutus referensi Inspector yang sudah ada.
    /// </summary>
    public void CompleteIsland(IslandName island)
    {
        Debug.LogWarning($"[GamePhaseManager] CompleteIsland({island}) dipanggil. " +
                         "Fungsi ini sudah tidak aktif. Gunakan sistem QuestID di questProgressTable.");
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
        PlayerPrefs.Save();

        CalculatePercentage();
        Debug.LogWarning("[GamePhaseManager] Seluruh progress telah direset.");
    }

#if UNITY_EDITOR
    /// <summary>Hanya untuk testing di Editor — simulasikan quest pertama di tabel selesai.</summary>
    [ContextMenu("Debug: Simulate First Quest Completed")]
    private void DebugSimulateFirstQuest()
    {
        if (questProgressByIsland != null)
        {
            foreach (var islandGroup in questProgressByIsland)
            {
                if (islandGroup == null || islandGroup.quests == null) continue;

                foreach (var entry in islandGroup.quests)
                {
                    if (entry == null || string.IsNullOrEmpty(entry.questID)) continue;
                    HandleQuestCompleted(entry.questID);
                    return;
                }
            }
        }
        else
            Debug.LogWarning("[GamePhaseManager] Mapping quest per pulau kosong, tidak ada yang disimulasikan.");
    }
#endif
}
