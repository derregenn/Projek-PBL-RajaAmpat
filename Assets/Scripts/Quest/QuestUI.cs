using UnityEngine;
using TMPro;

public class QuestUI : MonoBehaviour
{
    [Header("Pengaturan Panel Visual")]
    public GameObject panelHUD; // Tempat memasukkan objek QuestHUDPanel

    [Header("Referensi Teks HUD")]
    public TextMeshProUGUI hudQuestTitle;
    public TextMeshProUGUI hudQuestDescription;
    public TextMeshProUGUI objectiveListText;

    public Quest currentActiveQuest; 

    void Start()
    {
        // Pastikan panel visual mati saat game pertama kali dijalankan
        if (panelHUD != null) panelHUD.SetActive(false);
    }

    public void SetQuest(Quest quest)
    {
        currentActiveQuest = quest;

        // Nyalakan panel visual saat mendapat misi
        if (panelHUD != null) panelHUD.SetActive(true);

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

        // Matikan panel visual kembali setelah misi selesai
        if (panelHUD != null) panelHUD.SetActive(false);
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