using UnityEngine;
using System.Collections.Generic;

public class QuestController : MonoBehaviour
{
    public static QuestController Instance { get; private set; }

    [SerializeField] private QuestUI questUI;

    public List<Quest> ActiveQuests { get; private set; }
        = new List<Quest>();

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

    // =========================
    // ACCEPT QUEST
    // =========================

    public void AcceptQuest(Quest quest)
    {
        if (quest == null)
        {
            Debug.LogWarning("Quest is null.");
            return;
        }

        if (ActiveQuests.Contains(quest))
        {
            Debug.Log("Quest already active.");
            return;
        }

        ActiveQuests.Add(quest);

        Debug.Log("Quest Accepted: " + quest.QuestTitle);

        if (questUI != null)
        {
            questUI.SetQuest(quest);
        }
    }

    // =========================
    // CHECK QUEST
    // =========================

    public bool IsQuestActive(string questID)
    {
        return ActiveQuests.Exists(
            q => q != null && q.QuestID == questID
        );
    }

    // =========================
    // PROGRESS OBJECTIVE
    // =========================

    public void ProgressObjective(string objectiveID, int amount = 1)
    {
        Debug.Log("Mencari Objective: " + objectiveID);

        foreach (Quest quest in ActiveQuests)
        {
            if (quest == null)
            {
                continue;
            }

            Debug.Log("Quest aktif: " + quest.QuestTitle);

            foreach (QuestObjective objective in quest.Objectives)
            {
                Debug.Log(
                    "Objective tersedia: "
                    + objective.ObjectiveID
                );
            }

            QuestObjective target = null;

            // Match IDs safely so values such as "collect_coins" still work
            // when the asset contains different casing or extra whitespace.
            string requestedID = objectiveID?.Trim();
            foreach (QuestObjective objective in quest.Objectives)
            {
                if (objective != null &&
                    string.Equals(
                        objective.ObjectiveID?.Trim(),
                        requestedID,
                        System.StringComparison.OrdinalIgnoreCase))
                {
                    target = objective;
                    break;
                }
            }

            if (target != null && !target.IsComplete)
            {
                target.AddProgress(amount);

                Debug.Log(
                    "Objective Progress: "
                    + objectiveID
                    + " "
                    + target.CurrentAmount
                    + "/"
                    + target.RequiredAmount
                );

                if (quest.IsComplete)
                {
                    CompleteQuest(quest);
                }

                questUI?.UpdateQuestUI();
                return;
            }
        }

        Debug.LogWarning(
            "Objective not found: " + objectiveID
        );
    }

    // =========================
    // COMPLETE QUEST
    // =========================

    private void CompleteQuest(Quest quest)
    {
        Debug.Log(
            "QUEST COMPLETE: "
            + quest.QuestTitle
        );

        ActiveQuests.Remove(quest);

        questUI?.UpdateQuestUI();

        // Nanti bisa ditambah:
        // UnlockNextQuest()
        // GiveReward()
        // SaveGame()
    }
}