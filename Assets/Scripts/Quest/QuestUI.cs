using UnityEngine;
using TMPro;

public class QuestUI : MonoBehaviour
{
    public TextMeshProUGUI hudQuestTitle;
    public TextMeshProUGUI hudQuestDescription;
    public TextMeshProUGUI objectiveListText;

    public Quest currentActiveQuest; 

    public void SetQuest(Quest quest)
    {
        currentActiveQuest = quest;

        if (hudQuestTitle != null) hudQuestTitle.text = quest.QuestTitle;
        if (hudQuestDescription != null) hudQuestDescription.text = quest.Description;

        SyncToJournal(quest.QuestTitle);
    }

    public void UpdateQuestUI()
    {
        if (currentActiveQuest == null) return;
    }

    public void ClearQuest()
    {
        currentActiveQuest = null;

        if (hudQuestTitle != null) hudQuestTitle.text = "";
        if (hudQuestDescription != null) hudQuestDescription.text = "";
        if (objectiveListText != null) objectiveListText.text = "";

        if (QuestJournalManager.Instance != null)
        {
            QuestJournalManager.Instance.lastCompletedQuest = QuestJournalManager.Instance.currentOngoingQuest;
            QuestJournalManager.Instance.currentOngoingQuest = "Tidak ada misi aktif.";
            QuestJournalManager.Instance.RefreshQuestTabText();
            
            QuestJournalManager.Instance.AddCompletedQuest();
            
            QuestJournalManager.Instance.UnlockPianemoAward();
        }
    }

    private void SyncToJournal(string title)
    {
        if (QuestJournalManager.Instance != null)
        {
            QuestJournalManager.Instance.currentOngoingQuest = title;
            QuestJournalManager.Instance.RefreshQuestTabText();
        }
    }
}