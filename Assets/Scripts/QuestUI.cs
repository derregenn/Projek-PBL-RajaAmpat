using UnityEngine;
using TMPro;

public class QuestUI : MonoBehaviour
{
    public static QuestUI Instance;

    [Header("Quest Data")]
    public Quest activeQuest; // Masukkan ScriptableObject Quest di sini
    private QuestProgress currentProgress;

    [Header("UI Direct References")]
    public GameObject questHUDPanel;
    public TextMeshProUGUI questTitleText;
    public TextMeshProUGUI questDescriptionText;
    public TextMeshProUGUI objectiveListText;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        if (activeQuest != null)
        {
            SetQuest(activeQuest);
        }
    }

    public void SetQuest(Quest newQuest)
    {
        activeQuest = newQuest;
        currentProgress = new QuestProgress(newQuest);
        UpdateQuestUI();
    }

    // Panggil dari skrip koin/musuh: QuestController.Instance.AddObjectiveProgress("objID", 1);
    public void AddObjectiveProgress(string objectiveID, int amount = 1)
    {
        if (currentProgress == null) return;

        bool updated = false;
        foreach (var objective in currentProgress.objectives)
        {
            if (objective.objectiveID == objectiveID && !objective.IsCompleted())
            {
                objective.currentAmount = Mathf.Clamp(objective.currentAmount + amount, 0, objective.requiredAmount);
                updated = true;
                break;
            }
        }

        if (updated)
        {
            UpdateQuestUI();
        }
    }

    public void UpdateQuestUI()
    {
        if (currentProgress == null || currentProgress.quest == null)
        {
            if (questHUDPanel != null) questHUDPanel.SetActive(false);
            return;
        }

        if (questHUDPanel != null) questHUDPanel.SetActive(true);

        // Update Judul & Deskripsi
        if (questTitleText != null)
            questTitleText.text = currentProgress.quest.questName;

        if (questDescriptionText != null)
            questDescriptionText.text = currentProgress.quest.questDescription;

        // Susun daftar objektif (A, B, C) dalam satu blok teks
        if (objectiveListText != null)
        {
            objectiveListText.text = "";
            for (int i = 0; i < currentProgress.objectives.Count; i++)
            {
                var obj = currentProgress.objectives[i];
                char prefix = (char)('A' + i);

                if (obj.IsCompleted())
                {
                    objectiveListText.text += $"<color=#55FF55><s>[{prefix}] {obj.description} ({obj.currentAmount}/{obj.requiredAmount})</s></color>\n";
                }
                else
                {
                    objectiveListText.text += $"[{prefix}] {obj.description} ({obj.currentAmount}/{obj.requiredAmount})\n";
                }
            }
        }
    }
}