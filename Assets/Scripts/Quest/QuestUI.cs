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

        // Nonaktifkan ContentSizeFitter dan VerticalLayoutGroup pada hudQuestTitle jika ada,
        // agar tidak terjadi layout fighting atau teks menyusut secara tidak sengaja
        UnityEngine.UI.ContentSizeFitter csf = hudQuestTitle.GetComponent<UnityEngine.UI.ContentSizeFitter>();
        if (csf != null) csf.enabled = false;

        UnityEngine.UI.VerticalLayoutGroup vlg = hudQuestTitle.GetComponent<UnityEngine.UI.VerticalLayoutGroup>();
        if (vlg != null) vlg.enabled = false;

        RectTransform titleRt = hudQuestTitle.rectTransform;
        if (titleRt != null)
        {
            titleRt.anchorMin = new Vector2(0.5f, 0.5f);
            titleRt.anchorMax = new Vector2(0.5f, 0.5f);
            titleRt.pivot = new Vector2(0.5f, 0.5f);
            titleRt.anchoredPosition = Vector2.zero;
            // Di dalam panel 600x130: 530 lebar memberi margin nyaman dari sudut membulat, 105 tinggi untuk teks 1-2 baris
            titleRt.sizeDelta = new Vector2(530f, 105f);
        }

        hudQuestTitle.margin = new Vector4(10f, 4f, 10f, 4f);
        hudQuestTitle.enableAutoSizing = true;
        hudQuestTitle.fontSizeMin = 18f;
        hudQuestTitle.fontSizeMax = 30f;
        hudQuestTitle.fontSize = 26f;
        hudQuestTitle.fontStyle = FontStyles.Bold;
        hudQuestTitle.horizontalAlignment = HorizontalAlignmentOptions.Center;
        hudQuestTitle.verticalAlignment = VerticalAlignmentOptions.Middle;
        hudQuestTitle.textWrappingMode = TextWrappingModes.Normal;
        hudQuestTitle.overflowMode = TextOverflowModes.Ellipsis;
        hudQuestTitle.color = Color.white;

        // Nonaktifkan elemen teks sekunder jika tidak digunakan agar tidak menutupi tampilan
        if (hudQuestDescription != null && hudQuestDescription.gameObject != null)
        {
            hudQuestDescription.gameObject.SetActive(false);
        }
        if (objectiveListText != null && objectiveListText.gameObject != null)
        {
            objectiveListText.gameObject.SetActive(false);
        }
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
        if (panelHUD != null)
        {
            panelHUD.SetActive(true);
            QuestSlider slider = panelHUD.GetComponent<QuestSlider>();
            if (slider != null)
            {
                slider.ShowQuestPopup();
            }
        }
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
                        activeTitle = $"{activeTitle?.Trim()} <color=#FFD700>({obj.CurrentAmount}/{obj.RequiredAmount})</color>";
                    }
                    break;
                }
            }
        }

        activeTitle = string.IsNullOrWhiteSpace(activeTitle) ? "" : activeTitle.Trim();

        if (hudQuestTitle != null) hudQuestTitle.text = activeTitle;
        if (hudQuestDescription != null) hudQuestDescription.text = "";
        if (objectiveListText != null) objectiveListText.text = "";

        // Bersihkan tag rich text untuk penyimpanan di catatan Journal
        string plainJournalTitle = System.Text.RegularExpressions.Regex.Replace(activeTitle, "<.*?>", string.Empty);
        SyncToJournal(plainJournalTitle);
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
