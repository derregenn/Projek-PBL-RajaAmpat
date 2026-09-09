using UnityEngine;
using TMPro;
using System;

public class QuestUI : MonoBehaviour
{
    public static QuestUI Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private GameObject questHUDPanel;
    [SerializeField] private TextMeshProUGUI questTitleText;
    [SerializeField] private TextMeshProUGUI questDescriptionText;
    [SerializeField] private TextMeshProUGUI objectiveListText;

    private Quest activeQuest;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        UpdateQuestUI();
    }

    // ========================================
    // SET QUEST
    // ========================================

    public void SetQuest(Quest quest)
    {
        activeQuest = quest;

        UpdateQuestUI();
    }

    // ========================================
    // UPDATE UI
    // ========================================

    public void UpdateQuestUI()
    {
        // Tidak ada quest aktif
        if (activeQuest == null)
        {
            if (questHUDPanel != null)
                questHUDPanel.SetActive(false);

            return;
        }

        // Tampilkan Quest HUD
        if (questHUDPanel != null)
            questHUDPanel.SetActive(true);

        // ====================================
        // QUEST TITLE
        // ====================================

        if (questTitleText != null)
        {
            questTitleText.text = activeQuest.QuestTitle;
        }

        // ====================================
        // QUEST DESCRIPTION
        // ====================================

        if (questDescriptionText != null)
        {
            questDescriptionText.text = activeQuest.Description;
        }

        // ====================================
        // OBJECTIVES
        // ====================================

        if (objectiveListText != null)
        {
            objectiveListText.text = "";

            for (int i = 0; i < activeQuest.Objectives.Count; i++)
            {
                QuestObjective objective =
                    activeQuest.Objectives[i];

                char prefix = (char)('A' + i);

                string progress =
                    $"({objective.CurrentAmount}/{objective.RequiredAmount})";

                if (objective.IsComplete)
                {
                    objectiveListText.text +=
                        $"<color=#55FF55><s>[{prefix}] " +
                        $"{objective.Description} {progress}</s></color>\n";
                }
                else
                {
                    objectiveListText.text +=
                        $"[{prefix}] " +
                        $"{objective.Description} {progress}\n";
                }
            }
        }
    }

    internal void ClearQuest()
    {
        throw new NotImplementedException();
    }
}