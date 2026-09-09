using UnityEngine;
using System.Collections.Generic;

public class QuestController : MonoBehaviour
{
    public static QuestController Instance { get; private set; }

    [Header("Quest UI Reference")]
    [SerializeField] private QuestUI questUI;

    [Header("Active Quests")]
    public List<Quest> ActiveQuests { get; private set; }
        = new List<Quest>();

    // ========================================
    // UNITY
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

        if (questUI == null)
        {
            Debug.LogWarning(
                "QuestController: QuestUI belum di-assign di Inspector."
            );
        }
    }

    // ========================================
    // ACCEPT QUEST
    // ========================================

    public void AcceptQuest(Quest quest)
    {
        if (quest == null)
        {
            Debug.LogWarning(
                "QuestController: Quest is null."
            );
            return;
        }

        if (string.IsNullOrEmpty(quest.QuestID))
        {
            Debug.LogWarning(
                "QuestController: QuestID kosong."
            );
            return;
        }

        if (IsQuestActive(quest.QuestID))
        {
            Debug.Log(
                "Quest already active: "
                + quest.QuestID
            );
            return;
        }

        // ====================================
        // PENTING
        // Quest baru diterima di sini.
        // BELUM mengecek quest.IsComplete.
        // ====================================

        ActiveQuests.Add(quest);

        Debug.Log(
            "QUEST ACCEPTED: "
            + quest.QuestTitle
        );

        // Tampilkan quest ke UI
        if (questUI != null)
        {
            questUI.SetQuest(quest);
            questUI.UpdateQuestUI();
        }
        else
        {
            Debug.LogWarning(
                "QuestController: QuestUI belum di-assign."
            );
        }
    }

    // ========================================
    // CHECK QUEST ACTIVE
    // ========================================

    public bool IsQuestActive(string questID)
    {
        if (string.IsNullOrEmpty(questID))
            return false;

        return ActiveQuests.Exists(
            q => q != null && q.QuestID == questID
        );
    }

    // ========================================
    // PROGRESS OBJECTIVE
    // ========================================

    public void ProgressObjective(
        string objectiveID,
        int amount = 1)
    {
        if (string.IsNullOrEmpty(objectiveID))
        {
            Debug.LogWarning(
                "QuestController: Objective ID kosong."
            );
            return;
        }

        if (amount <= 0)
        {
            Debug.LogWarning(
                "QuestController: Amount harus lebih dari 0."
            );
            return;
        }

        // Cari objective di quest aktif
        foreach (Quest quest in ActiveQuests)
        {
            if (quest == null)
                continue;

            QuestObjective objective =
                quest.GetObjective(objectiveID);

            // ====================================
            // OBJECTIVE DITEMUKAN
            // ====================================

            if (objective == null)
                continue;

            // Jangan tambah progress jika sudah selesai
            if (objective.IsComplete)
            {
                Debug.Log(
                    "Objective already complete: "
                    + objectiveID
                );

                return;
            }

            // ====================================
            // TAMBAHKAN PROGRESS
            // ====================================

            objective.AddProgress(amount);

            Debug.Log(
                "Objective Progress: "
                + objectiveID
                + " "
                + objective.CurrentAmount
                + "/"
                + objective.RequiredAmount
            );

            // Update UI
            if (questUI != null)
            {
                questUI.UpdateQuestUI();
            }

            // ====================================
            // CEK QUEST SELESAI
            // ====================================

            if (IsQuestFinished(quest))
            {
                CompleteQuest(quest);
            }

            return;
        }

        Debug.LogWarning(
            "Objective not found in active quests: "
            + objectiveID
        );
    }

    // ========================================
    // CHECK QUEST FINISHED
    // ========================================

    private bool IsQuestFinished(Quest quest)
    {
        if (quest == null)
            return false;

        if (quest.Objectives == null ||
            quest.Objectives.Count == 0)
        {
            Debug.LogWarning(
                "Quest tidak memiliki objective: "
                + quest.QuestTitle
            );

            return false;
        }

        // Semua objective harus selesai
        foreach (QuestObjective objective in quest.Objectives)
        {
            if (objective == null)
                continue;

            if (!objective.IsComplete)
            {
                return false;
            }
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

        // Pastikan quest memang aktif
        if (!ActiveQuests.Contains(quest))
            return;

        Debug.Log(
            "================================"
        );

        Debug.Log(
            "QUEST COMPLETE: "
            + quest.QuestTitle
        );

        Debug.Log(
            "================================"
        );

        // Hapus quest dari ActiveQuests
        ActiveQuests.Remove(quest);

        // Bersihkan UI
        if (questUI != null)
        {
            questUI.ClearQuest();
        }

        // ====================================
        // FITUR NANTI
        // ====================================

        // UnlockNextQuest();
        // GiveReward();
        // SaveGame();
        // LoadNextScene();
    }
}