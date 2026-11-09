using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Helper component yang ditaruh di GameObject root QuestTabUI.
/// Menghubungkan semua referensi visual di dalam prefab ke QuestJournalManager secara otomatis
/// saat prefab ini muncul di scene manapun, memastikan jurnal selalu bekerja tanpa perlu setup manual per scene!
/// </summary>
public class QuestTabUI : MonoBehaviour
{
    [Header("Panel Background & Close")]
    public Image backgroundImage;
    public Button closeButton;

    [Header("Progress Tracking (Kiri Atas)")]
    public TMPro.TextMeshProUGUI progressPercentageText;

    [Header("Quest Folder & Tabs")]
    public Image folderImage;
    public Button tabSwitchButton;
    public TMPro.TextMeshProUGUI ongoingQuestText;

    [Header("Halaman Jurnal")]
    public Image journalPageImage;
    public Image journalPhotoImage;
    public TMPro.TextMeshProUGUI pageNumberText;
    public Button prevPageButton;
    public Button nextPageButton;

    [Header("Island Awards")]
    public Image awardArborek;
    public Image awardFriwen;
    public Image awardGag;
    public Image awardMisool;
    public Image awardPiaynemo;
    public Image awardSawinggrai;

    private void Awake()
    {
        RegisterToJournalManager();
    }

    private void Start()
    {
        RegisterToJournalManager();
    }

    private void OnEnable()
    {
        RegisterToJournalManager();
    }

    public void RegisterToJournalManager()
    {
        QuestJournalManager manager = QuestJournalManager.Instance;
        if (manager == null)
        {
            manager = FindFirstObjectByType<QuestJournalManager>();
        }

        if (manager != null)
        {
            manager.questTabPanel = this.gameObject;
            if (progressPercentageText != null) manager.progressPercentageText = progressPercentageText;
            if (folderImage != null) manager.folderImage = folderImage;
            if (tabSwitchButton != null) manager.tabSwitchButton = tabSwitchButton;
            if (ongoingQuestText != null) manager.ongoingQuestText = ongoingQuestText;
            if (journalPageImage != null) manager.journalPageImage = journalPageImage;
            if (journalPhotoImage != null) manager.journalPhotoImage = journalPhotoImage;
            if (pageNumberText != null) manager.pageNumberText = pageNumberText;
            if (prevPageButton != null) manager.btnPrevPage = prevPageButton;
            if (nextPageButton != null) manager.btnNextPage = nextPageButton;
            if (closeButton != null) manager.btnCloseJournal = closeButton;

            // Sambungkan button OnClick
            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(manager.CloseJournal);
                closeButton.onClick.AddListener(manager.CloseJournal);
            }
            if (prevPageButton != null)
            {
                prevPageButton.onClick.RemoveListener(manager.PrevPage);
                prevPageButton.onClick.AddListener(manager.PrevPage);
            }
            if (nextPageButton != null)
            {
                nextPageButton.onClick.RemoveListener(manager.NextPage);
                nextPageButton.onClick.AddListener(manager.NextPage);
            }
            if (tabSwitchButton != null)
            {
                tabSwitchButton.onClick.RemoveListener(manager.ToggleQuestTab);
                tabSwitchButton.onClick.AddListener(manager.ToggleQuestTab);
            }

            // Sambungkan Award Images ke data manager
            AssignAwardImage(manager, IslandName.Arborek, awardArborek);
            AssignAwardImage(manager, IslandName.Friwen, awardFriwen);
            AssignAwardImage(manager, IslandName.Gag, awardGag);
            AssignAwardImage(manager, IslandName.Misool, awardMisool);
            AssignAwardImage(manager, IslandName.Piaynemo, awardPiaynemo);
            AssignAwardImage(manager, IslandName.Sawinggrai, awardSawinggrai);

            manager.UpdateAllJournalSystems();
        }
    }

    private void AssignAwardImage(QuestJournalManager manager, IslandName island, Image img)
    {
        if (img == null) return;
        img.preserveAspect = true;
        if (img.rectTransform != null)
        {
            img.rectTransform.localScale = new Vector3(1f, 4.65f, 1f);
        }

        if (manager.islandAwards == null) manager.islandAwards = new System.Collections.Generic.List<IslandAwardData>();
        var found = manager.islandAwards.Find(a => a.island == island);
        if (found != null)
        {
            found.awardImage = img;
        }
        else
        {
            manager.islandAwards.Add(new IslandAwardData
            {
                island = island,
                displayName = island.ToString(),
                awardImage = img
            });
        }
    }
}

