using UnityEngine;
using TMPro;

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
        // Ambil quest aktif jika pemain berpindah scene atau me-reload level
        if (QuestController.Instance != null && QuestController.Instance.ActiveQuests.Count > 0)
        {
            activeQuest = QuestController.Instance.ActiveQuests[0];
        }

        UpdateQuestUI();
    }

    public void SetQuest(Quest quest)
    {
        activeQuest = quest;
        UpdateQuestUI();
    }

    public void UpdateQuestUI()
    {
        if (activeQuest == null)
        {
            if (questHUDPanel != null)
            {
                questHUDPanel.SetActive(false);
            }
            return;
        }

        if (questHUDPanel != null)
            questHUDPanel.SetActive(true);

        if (questTitleText != null)
            questTitleText.text = activeQuest.QuestTitle;

        if (questDescriptionText != null)
            questDescriptionText.text = activeQuest.Description;

        if (objectiveListText != null)
        {
            objectiveListText.text = "";

            for (int i = 0; i < activeQuest.Objectives.Count; i++)
            {
                QuestObjective objective = activeQuest.Objectives[i];
                char prefix = (char)('A' + i);
                string progress = $"({objective.CurrentAmount}/{objective.RequiredAmount})";

                if (objective.IsComplete)
                {
                    objectiveListText.text +=
                        $"<color=#55FF55><s>[{prefix}] {objective.Description} {progress}</s></color>\n";
                }
                else
                {
                    objectiveListText.text +=
                        $"[{prefix}] {objective.Description} {progress}\n";
                }
            }
        }
    }

    public void ClearQuest()
    {
        activeQuest = null;

        if (questHUDPanel != null)
        {
            questHUDPanel.SetActive(false);
        }
    }
}