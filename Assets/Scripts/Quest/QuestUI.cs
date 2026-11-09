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

    void Awake()
    {
        // QuestUI TIDAK boleh DontDestroyOnLoad — ia harus menjadi bagian dari
        // Canvas di masing-masing scene gameplay. QuestController akan
        // mencarinya secara otomatis setiap kali scene baru dimuat.
    }

    void Start()
    {

    }

    public void SetQuest(Quest quest)
    {
        currentActiveQuest = quest;
        UpdateQuestUI();
    }

    public void UpdateQuestUI()
    {
        if (currentActiveQuest == null) return;

        string activeTitle = currentActiveQuest.QuestTitle;
        string activeDesc = currentActiveQuest.Description;

        if (currentActiveQuest.Objectives != null && currentActiveQuest.Objectives.Count > 0)
        {
            for (int i = 0; i < currentActiveQuest.Objectives.Count; i++)
            {
                var obj = currentActiveQuest.Objectives[i];
                if (obj != null && !obj.IsComplete)
                {
                    string objText = !string.IsNullOrEmpty(obj.ObjectiveTitle) ? obj.ObjectiveTitle : obj.Description;

                    // Jika sudah di tahap berikutnya (misal setelah dapat barang, sekarang antar ke NPC),
                    // tampilkan judul/deskripsi tahap aktif di HUD dan Journal!
                    if (i > 0 && !string.IsNullOrEmpty(objText))
                    {
                        activeTitle = objText;
                    }
                    else if (string.IsNullOrEmpty(activeTitle) && !string.IsNullOrEmpty(objText))
                    {
                        activeTitle = objText;
                    }

                    if (!string.IsNullOrEmpty(obj.Description))
                    {
                        activeDesc = obj.Description;
                    }

                    if (obj.RequiredAmount > 1)
                    {
                        activeTitle = $"{activeTitle} ({obj.CurrentAmount}/{obj.RequiredAmount})";
                    }
                    break;
                }
            }
        }

        if (hudQuestTitle != null) hudQuestTitle.text = activeTitle;
        if (hudQuestDescription != null) hudQuestDescription.text = "";
        if (objectiveListText != null) objectiveListText.text = "";

        SyncToJournal(activeTitle);
    }

    public void ClearQuest()
    {
        currentActiveQuest = null;

        if (hudQuestTitle != null) hudQuestTitle.text = "";
        if (hudQuestDescription != null) hudQuestDescription.text = "";
        if (objectiveListText != null) objectiveListText.text = "";

        if (QuestJournalManager.Instance != null)
        {
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
