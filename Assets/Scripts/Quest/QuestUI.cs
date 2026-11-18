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

    [Header("Layout Normalisasi")]
    [SerializeField] private bool normalizeTrackerTextLayout = true;

    public Quest currentActiveQuest;

    void Awake()
    {
        // QuestUI TIDAK boleh DontDestroyOnLoad — ia harus menjadi bagian dari
        // Canvas di masing-masing scene gameplay. QuestController akan
        // mencarinya secara otomatis setiap kali scene baru dimuat.
        EnsureStandardTrackerLayout();
    }

    void Start()
    {
        EnsureStandardTrackerLayout();
    }

    public void EnsureStandardTrackerLayout()
    {
        if (!normalizeTrackerTextLayout || hudQuestTitle == null) return;

        RectTransform titleRt = hudQuestTitle.rectTransform;
        if (titleRt != null)
        {
            titleRt.anchorMin = new Vector2(0.5f, 0.5f);
            titleRt.anchorMax = new Vector2(0.5f, 0.5f);
            titleRt.pivot = new Vector2(0.5f, 0.5f);
            titleRt.anchoredPosition = Vector2.zero;
            titleRt.sizeDelta = new Vector2(540f, 120f);
        }

        hudQuestTitle.margin = Vector4.zero;
        hudQuestTitle.enableAutoSizing = true;
        hudQuestTitle.fontSizeMin = 16f;
        hudQuestTitle.fontSizeMax = 26f;
        hudQuestTitle.fontSize = 24f;
        hudQuestTitle.horizontalAlignment = HorizontalAlignmentOptions.Center;
        hudQuestTitle.verticalAlignment = VerticalAlignmentOptions.Middle;
    }

    public void SetQuest(Quest quest)
    {
        currentActiveQuest = quest;
        UpdateQuestUI();
    }

    public void ShowStandalonePrompt(string title, string description = "")
    {
        EnsureStandardTrackerLayout();
        if (hudQuestTitle != null) hudQuestTitle.text = title;
        if (hudQuestDescription != null) hudQuestDescription.text = description;
        if (objectiveListText != null) objectiveListText.text = "";
        if (panelHUD != null) panelHUD.SetActive(true);
    }

    public void UpdateQuestUI()
    {
        if (currentActiveQuest == null) return;

        EnsureStandardTrackerLayout();

        string activeTitle = currentActiveQuest.QuestTitle;
        string activeDesc = currentActiveQuest.Description;

        if (currentActiveQuest.Objectives != null && currentActiveQuest.Objectives.Count > 0)
        {
            for (int i = 0; i < currentActiveQuest.Objectives.Count; i++)
            {
                var obj = currentActiveQuest.Objectives[i];
                if (obj != null && !obj.IsComplete)
                {
                    string objText = !string.IsNullOrWhiteSpace(obj.ObjectiveTitle) ? obj.ObjectiveTitle : obj.Description;

                    // Jika sudah di tahap berikutnya (misal setelah dapat barang, sekarang antar ke NPC),
                    // tampilkan judul/deskripsi tahap aktif di HUD dan Journal!
                    if (i > 0 && !string.IsNullOrWhiteSpace(objText))
                    {
                        activeTitle = objText;
                    }
                    else if (string.IsNullOrWhiteSpace(activeTitle) && !string.IsNullOrWhiteSpace(objText))
                    {
                        activeTitle = objText;
                    }

                    if (!string.IsNullOrWhiteSpace(obj.Description))
                    {
                        activeDesc = obj.Description;
                    }

                    if (obj.RequiredAmount > 1)
                    {
                        activeTitle = $"{activeTitle?.Trim()} ({obj.CurrentAmount}/{obj.RequiredAmount})";
                    }
                    break;
                }
            }
        }

        activeTitle = string.IsNullOrWhiteSpace(activeTitle) ? "" : activeTitle.Trim();

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
