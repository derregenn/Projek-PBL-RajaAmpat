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
    public Image questIconImage;
    public GameObject questBoxPanel;
    public GameObject completedCard2;
    public GameObject completedCard3;
    public TMPro.TextMeshProUGUI completedText2;
    public TMPro.TextMeshProUGUI completedText3;

    [Header("Halaman Jurnal")]
    public Image journalPageImage;
    public Image journalPhotoImage;
    public TMPro.TextMeshProUGUI pageNumberText;
    public Image pageStatusBg;
    public Image journalLabel;
    public Button prevPageButton;
    public Button nextPageButton;

    [Header("Island Awards")]
    public Image awardArborek;
    public Image awardFriwen;
    public Image awardGag;
    public Image awardMisool;
    public Image awardPiaynemo;
    public Image awardSawinggrai;
    public Image awardRajaAmpat;
    public Image awardSecretEnding;
    public TMPro.TextMeshProUGUI trashStatusText;

    [Header("Canvas Sorting")]
    [Tooltip("Sorting order to ensure QuestTabUI renders above Canvas_HUD (order 0)")]
    public int journalSortingOrder = 30;

    private void Awake()
    {
        EnsureCanvasSortingOrder();
        RegisterToJournalManager();
    }

    private void Start()
    {
        EnsureCanvasSortingOrder();
        RegisterToJournalManager();
    }

    private void OnEnable()
    {
        EnsureCanvasSortingOrder();
        RegisterToJournalManager();
    }

    public void EnsureCanvasSortingOrder()
    {
        Canvas canvas = GetComponent<Canvas>();
        if (canvas == null)
        {
            canvas = gameObject.AddComponent<Canvas>();
        }
        canvas.overrideSorting = true;
        canvas.sortingOrder = journalSortingOrder;

        GraphicRaycaster raycaster = GetComponent<GraphicRaycaster>();
        if (raycaster == null)
        {
            raycaster = gameObject.AddComponent<GraphicRaycaster>();
        }
    }

    public void RegisterToJournalManager()
    {
        EnsureCanvasSortingOrder();
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
            if (questIconImage != null) manager.questIconImage = questIconImage;
            if (questBoxPanel != null) manager.questBoxPanel = questBoxPanel;
            if (completedCard2 != null) manager.completedCard2 = completedCard2;
            if (completedCard3 != null) manager.completedCard3 = completedCard3;
            if (completedText2 != null) manager.completedText2 = completedText2;
            if (completedText3 != null) manager.completedText3 = completedText3;
            if (awardRajaAmpat != null) manager.awardRajaAmpat = awardRajaAmpat;
            if (awardSecretEnding != null) manager.awardSecretEnding = awardSecretEnding;
            if (trashStatusText != null) manager.trashStatusText = trashStatusText;
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

