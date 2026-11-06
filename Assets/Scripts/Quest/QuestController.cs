using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using System;

public class QuestController : MonoBehaviour
{
    public static QuestController Instance { get; private set; }

    // ========================================
    // EVENT FOR PROGRESS & BARRIER UNLOCK
    // ========================================
    // Event ini disiarkan saat quest selesai (mengirimkan questID)
    public static event Action<string> OnQuestCompleted;

    [Header("Quest UI Reference")]
    [Tooltip("Opsional: assign manual jika QuestUI ada di scene yang sama dengan QuestController. " +
             "Jika kosong, akan dicari otomatis setiap kali scene baru dimuat.")]
    [SerializeField] private QuestUI questUI;

    [Header("Active Quests")]
    public List<Quest> ActiveQuests { get; private set; }
        = new List<Quest>();

    // Referensi untuk animasi panel
    [SerializeField] private QuestSlider questSliderAnim;

    // ========================================
    // UNITY LIFECYCLE
    // ========================================

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

    private void OnEnable()
    {
        // Daftar ke event perpindahan scene agar referensi UI bisa diperbarui
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    /// <summary>
    /// Dipanggil otomatis setiap kali scene baru selesai dimuat.
    /// Mencari ulang QuestUI dan QuestSlider di scene aktif agar tidak null
    /// ketika game dijalankan dari Main Menu.
    /// </summary>
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Tunda satu frame agar semua objek di scene sudah selesai Awake/Start
        StartCoroutine(ReconnectUINextFrame());
    }

    private IEnumerator ReconnectUINextFrame()
    {
        yield return null; // tunggu satu frame

        // Cari QuestUI di scene aktif jika referensi saat ini sudah null atau berasal dari scene yang berbeda
        if (questUI == null)
        {
            questUI = FindFirstObjectByType<QuestUI>();
            if (questUI != null)
                Debug.Log($"[QuestController] QuestUI ditemukan di scene '{SceneManager.GetActiveScene().name}'.");
            else
                Debug.Log($"[QuestController] Tidak ada QuestUI di scene '{SceneManager.GetActiveScene().name}' — quest berjalan tanpa HUD.");
        }

        // Cari QuestSlider di scene aktif jika referensi saat ini sudah null
        if (questSliderAnim == null)
        {
            questSliderAnim = FindFirstObjectByType<QuestSlider>();
        }

        // Jika ada quest aktif saat scene dimuat (misal Continue Game), perbarui HUD
        if (questUI != null && ActiveQuests.Count > 0)
        {
            Quest lastActive = ActiveQuests[ActiveQuests.Count - 1];
            questUI.SetQuest(lastActive);
            questUI.UpdateQuestUI();
            Debug.Log($"[QuestController] HUD di-refresh untuk quest aktif: '{lastActive.QuestTitle}'.");
        }
    }

    // ========================================
    // ACCEPT QUEST
    // ========================================

    public void AcceptQuest(Quest quest)
    {
        if (quest == null)
        {
            Debug.LogWarning("QuestController: Quest is null.");
            return;
        }

        if (string.IsNullOrEmpty(quest.QuestID))
        {
            Debug.LogWarning("QuestController: QuestID kosong.");
            return;
        }

        if (IsQuestActive(quest.QuestID))
        {
            Debug.Log("Quest already active: " + quest.QuestID);
            return;
        }

        ActiveQuests.Add(quest);

        Debug.Log("QUEST ACCEPTED: " + quest.QuestTitle);

        // Pastikan referensi UI masih valid; cari ulang jika perlu
        if (questUI == null)
            questUI = FindFirstObjectByType<QuestUI>();

        if (questSliderAnim == null)
            questSliderAnim = FindFirstObjectByType<QuestSlider>();

        // Tampilkan quest ke UI
        if (questUI != null)
        {
            questUI.SetQuest(quest);
            questUI.UpdateQuestUI();

            if (questSliderAnim != null)
            {
                questSliderAnim.ShowQuestPopup();
            }
        }
        else
        {
            Debug.LogWarning("QuestController: QuestUI tidak ditemukan di scene ini. " +
                             "Pastikan ada GameObject dengan komponen QuestUI di scene gameplay.");
        }
    }

    // ========================================
    // CHECK QUEST ACTIVE & HELPERS
    // ========================================

    public bool IsQuestActive(string questID)
    {
        if (string.IsNullOrEmpty(questID))
            return false;

        return ActiveQuests.Exists(q => q != null && q.QuestID == questID);
    }

    /// <summary>
    /// Mengetahui apakah sedang ada minimal satu quest aktif yang belum selesai.
    /// </summary>
    public bool HasActiveQuest()
    {
        return ActiveQuests != null && ActiveQuests.Count > 0;
    }

    /// <summary>
    /// Mengambil quest aktif terakhir di daftar antrean.
    /// </summary>
    public Quest GetCurrentActiveQuest()
    {
        if (ActiveQuests != null && ActiveQuests.Count > 0)
            return ActiveQuests[ActiveQuests.Count - 1];
        return null;
    }

    // ========================================
    // PROGRESS OBJECTIVE
    // ========================================

    public void ProgressObjective(string objectiveID, int amount = 1)
    {
        if (string.IsNullOrEmpty(objectiveID))
        {
            Debug.LogWarning("QuestController: Objective ID kosong.");
            return;
        }

        if (amount <= 0)
        {
            Debug.LogWarning("QuestController: Amount harus lebih dari 0.");
            return;
        }

        // Cari objective di quest aktif
        foreach (Quest quest in ActiveQuests)
        {
            if (quest == null)
                continue;

            QuestObjective objective = quest.GetObjective(objectiveID);

            if (objective == null)
                continue;

            // Jangan tambah progress jika sudah selesai
            if (objective.IsComplete)
            {
                Debug.Log("Objective already complete: " + objectiveID);
                return;
            }

            // Tambahkan progress
            objective.AddProgress(amount);

            Debug.Log(
                "Objective Progress: " + objectiveID
                + " " + objective.CurrentAmount
                + "/" + objective.RequiredAmount
            );

            // Update UI
            if (questUI != null)
                questUI.UpdateQuestUI();

            // Cek quest selesai
            if (IsQuestFinished(quest))
                CompleteQuest(quest);

            return;
        }

        Debug.LogWarning("Objective not found in active quests: " + objectiveID);
    }

    // ========================================
    // CHECK QUEST FINISHED
    // ========================================

    private bool IsQuestFinished(Quest quest)
    {
        if (quest == null)
            return false;

        if (quest.Objectives == null || quest.Objectives.Count == 0)
        {
            Debug.LogWarning("Quest tidak memiliki objective: " + quest.QuestTitle);
            return false;
        }

        // Semua objective harus selesai
        foreach (QuestObjective objective in quest.Objectives)
        {
            if (objective == null)
                continue;

            if (!objective.IsComplete)
                return false;
        }

        return true;
    }

    // ========================================
    // COMPLETE QUEST
    // ========================================

    private void CompleteQuest(Quest quest)
    {
        if (quest == null)
            return;

        if (!ActiveQuests.Contains(quest))
            return;

        string completedQuestID = quest.QuestID;

        Debug.Log("================================");
        Debug.Log("QUEST COMPLETE: " + quest.QuestTitle);
        Debug.Log("================================");

        // Hapus quest dari ActiveQuests
        ActiveQuests.Remove(quest);

        // Jika masih ada quest aktif lain yang tersisa (misal quest lama yang sempat tertimpa),
        // pulihkan tampilannya di UI bukannya mengosongkan HUD!
        if (ActiveQuests.Count > 0)
        {
            Quest remainingQuest = ActiveQuests[ActiveQuests.Count - 1];
            if (questUI != null)
            {
                questUI.SetQuest(remainingQuest);
                questUI.UpdateQuestUI();
                Debug.Log($"[QuestController] UI dipulihkan ke quest sebelumnya yang belum tuntas: '{remainingQuest.QuestTitle}'");
            }
        }
        else
        {
            // Bersihkan UI jika semua quest benar-benar telah tuntas
            if (questUI != null)
                questUI.ClearQuest();
        }

        // ====================================
        // SIARKAN EVENT KE BARRIER & SYSTEM PROGRESS
        // ====================================
        OnQuestCompleted?.Invoke(completedQuestID);
    }
}
