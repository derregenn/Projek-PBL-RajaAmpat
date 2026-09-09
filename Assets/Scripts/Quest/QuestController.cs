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

        // QuestController tetap ada saat pindah scene
        DontDestroyOnLoad(gameObject);

        // Cek QuestUI
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

        if (IsQuestActive(quest.QuestID))
        {
            Debug.Log(
                "Quest already active: "
                + quest.QuestID
            );
            return;
        }

        ActiveQuests.Add(quest);

        Debug.Log(
            "Quest Accepted: "
            + quest.QuestTitle
        );

        // Beritahu QuestUI
        if (questUI != null)
        {
            questUI.SetQuest(quest);
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
                "QuestController: Objective ID is empty."
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

        // Cari objective di semua quest aktif
        foreach (Quest quest in ActiveQuests)
        {
            if (quest == null)
                continue;

            QuestObjective objective =
                quest.GetObjective(objectiveID);

            // Objective ditemukan
            if (objective != null)
            {
                // Sudah selesai
                if (objective.IsComplete)
                {
                    Debug.Log(
                        "Objective already complete: "
                        + objectiveID
                    );

                    return;
                }

                // Tambahkan progress
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

                // Cek apakah seluruh objective selesai
                if (quest.IsComplete)
                {
                    CompleteQuest(quest);
                }

                return;
            }
        }

        Debug.LogWarning(
            "Objective not found in active quests: "
            + objectiveID
        );
    }

    // ========================================
    // COMPLETE QUEST
    // ========================================

    private void CompleteQuest(Quest quest)
    {
        if (quest == null)
            return;

        Debug.Log(
            "QUEST COMPLETE: " + quest.QuestTitle
        );

        // Hapus quest dari daftar quest aktif
        ActiveQuests.Remove(quest);

        // Bersihkan Quest UI
        if (questUI != null)
        {
            questUI.ClearQuest();
        }

        // ========================================
        // NANTI BISA DITAMBAHKAN
        // ========================================

        // UnlockNextQuest();
        // GiveReward();
        // SaveGame();
        // LoadNextScene();
    }
}