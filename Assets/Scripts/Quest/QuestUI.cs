using UnityEngine;
using TMPro;

public class QuestUI : MonoBehaviour
{
    [Header("Referensi HUD Asli Regen")]
    [SerializeField] private TMP_Text questTitleText;
    [SerializeField] private TMP_Text questDescriptionText;
    [SerializeField] private TMP_Text objectiveListText;

    private Quest currentActiveQuest;

    // Dipanggil otomatis oleh sistem Regen saat quest diterima
    public void SetQuest(Quest quest)
    {
        if (quest == null)
        {
            Debug.LogWarning("QuestUI: Quest yang diterima bernilai NULL!");
            return;
        }

        currentActiveQuest = quest;
        Debug.Log("QuestUI: Menerima Quest -> " + quest.QuestTitle);

        // 1. Paksa Panel HUD Aktif
        gameObject.SetActive(true);

        // 2. Masukkan Judul ke TMP_Text
        if (questTitleText != null)
        {
            questTitleText.gameObject.SetActive(true);
            questTitleText.text = quest.QuestTitle;
            Debug.Log("QuestUI: Berhasil set Judul ke HUD -> " + quest.QuestTitle);
        }
        else
        {
            Debug.LogError("QuestUI ERROR: questTitleText di Inspector belum ditarik!");
        }

        // 3. Masukkan Deskripsi ke TMP_Text
        if (questDescriptionText != null)
        {
            questDescriptionText.gameObject.SetActive(true);
            questDescriptionText.text = quest.Description;
        }

        // 4. Update Angka Objective
        UpdateQuestUI();

        // 5. Sinkronkan ke Buku Jurnal Cokelat
        if (QuestJournalManager.Instance != null)
        {
            QuestJournalManager.Instance.currentOngoingQuest = quest.QuestTitle;
            QuestJournalManager.Instance.RefreshQuestTabText();
        }
    }

    // Dipanggil otomatis oleh sistem Regen saat progress bertambah
    public void UpdateQuestUI()
    {
        if (currentActiveQuest == null) return;

        if (objectiveListText != null)
        {
            objectiveListText.gameObject.SetActive(true);
            string objectiveText = "";
            
            if (currentActiveQuest.Objectives != null)
            {
                foreach (var obj in currentActiveQuest.Objectives)
                {
                    if (obj != null)
                    {
                        objectiveText += $"{obj.CurrentAmount} / {obj.RequiredAmount}\n";
                    }
                }
            }
            
            objectiveListText.text = objectiveText;
            Debug.Log("QuestUI: Berhasil set Objective ke HUD -> " + objectiveText);
        }
        else
        {
            Debug.LogError("QuestUI ERROR: objectiveListText di Inspector belum ditarik!");
        }
    }

    // Dipanggil otomatis oleh sistem Regen saat quest selesai
public void ClearQuest()
{
    currentActiveQuest = null;
    Debug.Log("QuestUI: ClearQuest dipanggil.");

    if (questTitleText != null) questTitleText.text = "";
    if (questDescriptionText != null) questDescriptionText.text = "";
    if (objectiveListText != null) objectiveListText.text = "";

    if (QuestJournalManager.Instance != null)
    {
        QuestJournalManager.Instance.lastCompletedQuest = QuestJournalManager.Instance.currentOngoingQuest;
        QuestJournalManager.Instance.currentOngoingQuest = "Tidak ada misi aktif.";
        QuestJournalManager.Instance.RefreshQuestTabText();

        // --- INI YANG WAJIB ADA AGAR AWARDNYA MENYALA ---
        QuestJournalManager.Instance.UnlockPianemoAward();
    }
}
}