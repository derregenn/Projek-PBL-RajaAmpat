using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class IslandAwardData
{
    public IslandName island;
    public string displayName;
    [Tooltip("UI Image slot di Travel Journal untuk award pulau ini")]
    public Image awardImage;
    [Tooltip("Sprite siluet/terkunci (gelap)")]
    public Sprite lockedSprite;
    [Tooltip("Sprite award berwarna/terbuka (terang)")]
    public Sprite unlockedSprite;
}

[System.Serializable]
public class JournalEntry
{
    public int pageID;
    public string title;
    [TextArea(2, 5)]
    public string triviaDescription;
    public Sprite pageBackgroundSprite;
    public bool requiresPhoto = false;
    public int photoIDToDisplay;

    [Tooltip("QuestID yang membuka halaman ini jika diselesaikan (opsional)")]
    public string unlockOnQuestID;
    [Tooltip("Atau Pulau yang membuka halaman ini jika pulau tuntas (opsional)")]
    public bool unlockOnIslandComplete = false;
    public IslandName unlockIsland;
}

public class QuestJournalManager : MonoBehaviour
{
    public static QuestJournalManager Instance { get; private set; }

    [Header("Camera & Polaroid Settings")]
    public Image cameraFlash;
    public GameObject polaroidPopup;
    public Image popupPhotoDisplay;

    [Header("Travel Journal Panel")]
    public GameObject questTabPanel;
    public Button btnCloseJournal;

    [Header("Progress Tracking UI (Kiri Atas Jurnal)")]
    [Tooltip("Teks persentase progres total di kiri atas jurnal")]
    public TextMeshProUGUI progressPercentageText;

    [Header("Quest Tabs (Ongoing & Completed)")]
    [Tooltip("Folder visual/tab sprite")]
    public Image folderImage;
    [Tooltip("Tombol tab untuk switch Ongoing/Completed")]
    public Button tabSwitchButton;
    [Tooltip("Teks quest di panel kiri (akan diisi quest ongoing atau list quest completed)")]
    public TextMeshProUGUI ongoingQuestText;
    public Image questIconImage;
    public Sprite tabOngoingSprite;
    public Sprite tabCompletedSprite;

    [Header("Quest Cards (Ongoing & Completed Multi-Panel)")]
    public GameObject questBoxPanel; // Card 1 (top)
    public GameObject completedCard2; // Card 2 (middle)
    public GameObject completedCard3; // Card 3 (bottom)
    public TextMeshProUGUI completedText2;
    public TextMeshProUGUI completedText3;

    private bool isOngoingTabActive = true;

    [Header("Island Awards Bar")]
    public List<IslandAwardData> islandAwards = new List<IslandAwardData>();
    public Image awardRajaAmpat;
    public Image awardSecretEnding;
    public TextMeshProUGUI trashStatusText;

    [Header("Halaman Jurnal (Kanan)")]
    public Image journalPageImage;
    public Image journalPhotoImage;
    public GameObject noPhotoWarning;
    public TextMeshProUGUI pageNumberText;
    public Button btnPrevPage;
    public Button btnNextPage;

    [Tooltip("Sprite halaman terkunci")]
    public Sprite lockedPageSprite;

    [Header("Database Jurnal & Foto")]
    public JournalEntry[] allJournalDatabase;
    public List<int> unlockedPages = new List<int>();

    public Sprite[] allPhotoSpritesDatabase;
    private Dictionary<int, Sprite> collectedPhotos = new Dictionary<int, Sprite>();

    private int currentJournalIndex = 0;
    private bool isFlashing = false;

    // Quest caching
    [HideInInspector] public string currentOngoingQuest = "Jelajahi keindahan Raja Ampat!";
    [HideInInspector] public string lastCompletedQuest = "Belum ada misi selesai.";
    private List<string> completedQuestTitles = new List<string>();
    private HashSet<string> completedQuestIDs = new HashSet<string>();
    private HashSet<IslandName> unlockedAwardIslands = new HashSet<IslandName>();

    private const string SaveKeyUnlockedPages = "QJM_UnlockedPages";
    private const string SaveKeyCollectedPhotos = "QJM_CollectedPhotos";
    private const string SaveKeyCompletedQuests = "QJM_CompletedQuests";
    private const string SaveKeyCompletedQuestIDs = "QJM_CompletedQuestIDs";
    private const string SaveKeyUnlockedIslands = "QJM_UnlockedIslands";

    private static readonly Dictionary<string, string> KnownQuestTitles = new Dictionary<string, string>
    {
        { "01_Airstrip", "Keluar dari Bandara" },
        { "01_Waisai", "Baca Papan Informasi di Sekitar Pulau" },
        { "02_Waisai", "Temui Tour Guide di Dekat Pantai" },
        { "03_Waisai", "Menuju Petugas Hub" },
        { "01_Piaynemo", "Temui Tour Guide di Piaynemo" },
        { "02_Piaynemo", "Naik ke Puncak Bukit" },
        { "03_Piaynemo", "Potret Keindahan Piaynemo" },
        { "04_Piaynemo", "Turun dari Puncak Bukit" },
        { "05_Piaynemo", "Bersihkan Sampah di Pesisir Pantai" },
        { "06_Piaynemo", "Kembali ke Dermaga" },
        { "01_Misool", "Temui Tour Guide di Misool" },
        { "02_Misool", "Baca Papan Informasi di Misool" },
        { "03_Misool", "Temui Warga Lokal Misool" },
        { "04_Misool", "Menyelam ke Bawah Laut Misool" },
        { "05_Misool", "Selamatkan Biota Laut dari Sampah" },
        { "06_Misool", "Foto Biota Laut Misool" },
        { "07_Misool", "Kembali ke Daratan Misool" },
        { "08_Misool", "Kembali ke Dermaga Misool" },
        { "01_Arborek", "Bantu Tetua Adat Mencari Kotak Umpan" },
        { "02_Arborek", "Bantu Tetua Adat Mencari Kerang" },
        { "03_Arborek", "Lakukan Ritual Sasi di Pantai" },
        { "04_Arborek", "Jawab Pertanyaan Tetua Adat" },
        { "05_Arborek", "Potret Desa Wisata Arborek" },
        { "06_Arborek", "Kembali ke Dermaga Arborek" },
        { "01_Sawinggrai", "Beri Makan Ikan di Dermaga" },
        { "02_Sawinggrai", "Menyelam ke Laut Sawinggrai" },
        { "03_Sawinggrai", "Potret Ikan di Laut Sawinggrai" },
        { "04_Sawinggrai", "Serahkan Foto ke Pembisnis" },
        { "01_Friwen", "Ikat Perahu yang Lepas di Pantai" },
        { "02_Friwen", "Baca Papan Informasi Friwen" },
        { "03_Friwen", "Jawab Kuis Tour Guide Friwen" },
        { "04_Friwen", "Potret Keindahan Pasir Timbul" },
        { "10", "Kembali ke Tour Guide Friwen" },
        { "01", "Bantu Peneliti Mengambil Sampel Air" },
        { "02", "Minta Air Bersih ke Peneliti" },
        { "03", "Berikan Air Bersih ke Tetua Adat" },
        { "04", "Cari Sagu untuk Tetua Adat" },
        { "05", "Berikan Sagu ke Tetua Adat" },
        { "06", "Bicara dengan Tetua Adat Gag" },
        { "07", "Lihat Kondisi Tambang dari Bukit" },
        { "08", "Kembali ke Dermaga Pulau Gag" }
    };

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);
            LoadJournalSaveData();
        }
        else if (Instance != this)
        {
            // Jika ada instance baru di scene baru, transfer referensi UI scene ke Instance persistent
            Instance.AdoptUIReferences(this);
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        InitializeUI();
        ListenToGameEvents();
        UpdateAllJournalSystems();
    }

    private void OnDestroy()
    {
        UnlistenGameEvents();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab) && !isFlashing)
        {
            ToggleJournalPanel();
        }
    }

    // =========================================================
    // INITIALIZATION & UI ADOPTION
    // =========================================================

    private void InitializeUI()
    {
        if (questTabPanel == null)
        {
            QuestTabUI tabUI = FindFirstObjectByType<QuestTabUI>(FindObjectsInactive.Include);
            if (tabUI != null)
            {
                tabUI.RegisterToJournalManager();
                questTabPanel = tabUI.gameObject;
            }
        }

        if (cameraFlash != null) cameraFlash.color = new Color(1, 1, 1, 0);
        if (polaroidPopup != null) polaroidPopup.SetActive(false);
        if (questTabPanel != null) questTabPanel.SetActive(false);

        // Pasang listener tombol UI jika belum terhubung
        if (btnPrevPage != null)
        {
            btnPrevPage.onClick.RemoveListener(PrevPage);
            btnPrevPage.onClick.AddListener(PrevPage);
        }
        if (btnNextPage != null)
        {
            btnNextPage.onClick.RemoveListener(NextPage);
            btnNextPage.onClick.AddListener(NextPage);
        }
        if (btnCloseJournal != null)
        {
            btnCloseJournal.onClick.RemoveListener(CloseJournal);
            btnCloseJournal.onClick.AddListener(CloseJournal);
        }
        if (tabSwitchButton != null)
        {
            tabSwitchButton.onClick.RemoveListener(ToggleQuestTab);
            tabSwitchButton.onClick.AddListener(ToggleQuestTab);
        }

        // Halaman awal default selalu terbuka (Page 1)
        if (allJournalDatabase != null && allJournalDatabase.Length > 0)
        {
            // Pastikan seluruh halaman tidak memblokir render karena foto terpisah (semua gambar halaman sudah pre-rendered)
            foreach (var entry in allJournalDatabase)
            {
                if (entry != null)
                {
                    entry.requiresPhoto = false;
                }
            }

            if (!unlockedPages.Contains(allJournalDatabase[0].pageID))
            {
                unlockedPages.Add(allJournalDatabase[0].pageID);
                SaveJournalSaveData();
            }
        }
    }

    public void AdoptUIReferences(QuestJournalManager sceneManager)
    {
        if (sceneManager == null) return;

        if (sceneManager.cameraFlash != null) this.cameraFlash = sceneManager.cameraFlash;
        if (sceneManager.polaroidPopup != null) this.polaroidPopup = sceneManager.polaroidPopup;
        if (sceneManager.popupPhotoDisplay != null) this.popupPhotoDisplay = sceneManager.popupPhotoDisplay;

        if (sceneManager.questTabPanel != null) this.questTabPanel = sceneManager.questTabPanel;
        if (sceneManager.btnCloseJournal != null) this.btnCloseJournal = sceneManager.btnCloseJournal;
        if (sceneManager.progressPercentageText != null) this.progressPercentageText = sceneManager.progressPercentageText;

        if (sceneManager.folderImage != null) this.folderImage = sceneManager.folderImage;
        if (sceneManager.tabSwitchButton != null) this.tabSwitchButton = sceneManager.tabSwitchButton;
        if (sceneManager.ongoingQuestText != null) this.ongoingQuestText = sceneManager.ongoingQuestText;
        if (sceneManager.questIconImage != null) this.questIconImage = sceneManager.questIconImage;
        if (sceneManager.questBoxPanel != null) this.questBoxPanel = sceneManager.questBoxPanel;
        if (sceneManager.completedCard2 != null) this.completedCard2 = sceneManager.completedCard2;
        if (sceneManager.completedCard3 != null) this.completedCard3 = sceneManager.completedCard3;
        if (sceneManager.completedText2 != null) this.completedText2 = sceneManager.completedText2;
        if (sceneManager.completedText3 != null) this.completedText3 = sceneManager.completedText3;
        if (sceneManager.awardRajaAmpat != null) this.awardRajaAmpat = sceneManager.awardRajaAmpat;
        if (sceneManager.awardSecretEnding != null) this.awardSecretEnding = sceneManager.awardSecretEnding;
        if (sceneManager.trashStatusText != null) this.trashStatusText = sceneManager.trashStatusText;
        if (sceneManager.tabOngoingSprite != null) this.tabOngoingSprite = sceneManager.tabOngoingSprite;
        if (sceneManager.tabCompletedSprite != null) this.tabCompletedSprite = sceneManager.tabCompletedSprite;

        if (sceneManager.journalPageImage != null) this.journalPageImage = sceneManager.journalPageImage;
        if (sceneManager.journalPhotoImage != null) this.journalPhotoImage = sceneManager.journalPhotoImage;
        if (sceneManager.noPhotoWarning != null) this.noPhotoWarning = sceneManager.noPhotoWarning;
        if (sceneManager.pageNumberText != null) this.pageNumberText = sceneManager.pageNumberText;
        if (sceneManager.btnPrevPage != null) this.btnPrevPage = sceneManager.btnPrevPage;
        if (sceneManager.btnNextPage != null) this.btnNextPage = sceneManager.btnNextPage;

        if (this.islandAwards == null || this.islandAwards.Count == 0)
        {
            if (sceneManager.islandAwards != null && sceneManager.islandAwards.Count > 0)
            {
                this.islandAwards = sceneManager.islandAwards;
            }
        }
        else if (sceneManager.islandAwards != null && sceneManager.islandAwards.Count > 0)
        {
            foreach (var sceneAward in sceneManager.islandAwards)
            {
                if (sceneAward == null) continue;
                var existing = this.islandAwards.Find(a => a.island == sceneAward.island);
                if (existing != null)
                {
                    if (sceneAward.awardImage != null) existing.awardImage = sceneAward.awardImage;
                    if (existing.lockedSprite == null && sceneAward.lockedSprite != null) existing.lockedSprite = sceneAward.lockedSprite;
                    if (existing.unlockedSprite == null && sceneAward.unlockedSprite != null) existing.unlockedSprite = sceneAward.unlockedSprite;
                }
            }
        }

        if (sceneManager.allJournalDatabase != null && sceneManager.allJournalDatabase.Length >= 17)
        {
            this.allJournalDatabase = sceneManager.allJournalDatabase;
        }

        QuestTabUI tabUI = FindFirstObjectByType<QuestTabUI>(FindObjectsInactive.Include);
        if (tabUI != null)
        {
            tabUI.RegisterToJournalManager();
        }

        InitializeUI();
        UpdateAllJournalSystems();
    }

    private void ListenToGameEvents()
    {
        QuestController.OnQuestCompleted += HandleQuestCompleted;

        if (GamePhaseManager.Instance != null)
        {
            GamePhaseManager.Instance.OnPercentageUpdated.AddListener(OnGameProgressUpdated);
            GamePhaseManager.Instance.OnIslandCompleted.AddListener(OnIslandCompletedHandler);
        }
    }

    private void UnlistenGameEvents()
    {
        QuestController.OnQuestCompleted -= HandleQuestCompleted;

        if (GamePhaseManager.Instance != null)
        {
            GamePhaseManager.Instance.OnPercentageUpdated.RemoveListener(OnGameProgressUpdated);
            GamePhaseManager.Instance.OnIslandCompleted.RemoveListener(OnIslandCompletedHandler);
        }
    }

    // =========================================================
    // EVENT HANDLERS
    // =========================================================

    public void RecordCompletedQuest(string questID, string questTitle = null)
    {
        if (!string.IsNullOrEmpty(questID) && completedQuestIDs.Contains(questID))
        {
            RefreshQuestTabText();
            return;
        }

        string resolvedTitle = questTitle;

        if (!string.IsNullOrEmpty(questID) && KnownQuestTitles.TryGetValue(questID, out string canonicalTitle))
        {
            if (string.IsNullOrEmpty(resolvedTitle) ||
                resolvedTitle == "Tidak ada misi aktif." ||
                resolvedTitle == "Jelajahi keindahan Raja Ampat!")
            {
                resolvedTitle = canonicalTitle;
            }
        }

        if (!string.IsNullOrEmpty(resolvedTitle) &&
            resolvedTitle != "Tidak ada misi aktif." &&
            resolvedTitle != "Jelajahi keindahan Raja Ampat!")
        {
            if (!string.IsNullOrEmpty(questID))
            {
                completedQuestIDs.Add(questID);
            }

            lastCompletedQuest = resolvedTitle;
            if (!completedQuestTitles.Contains(resolvedTitle))
            {
                completedQuestTitles.Add(resolvedTitle);
                SaveJournalSaveData();
            }
        }

        RefreshQuestTabText();
    }

    private void HandleQuestCompleted(string questID)
    {
        RecordCompletedQuest(questID, null);

        // Cek unlock halaman berdasarkan QuestID yang selesai
        CheckJournalPageUnlocks(questID);

        // Cek apakah quest ini adalah quest terakhir pulau yang membuka Award
        CheckFinalQuestIslandUnlock(questID);

        // Update UI
        UpdateAllJournalSystems();
    }

    private void CheckFinalQuestIslandUnlock(string questID)
    {
        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        IslandName? completedIsland = null;

        switch (questID)
        {
            case "01_Airstrip":
                completedIsland = IslandName.Airstrip;
                break;
            case "03_Waisai":
                completedIsland = IslandName.Waisai;
                break;
            case "06_Piaynemo":
                completedIsland = IslandName.Piaynemo;
                break;
            case "07_Misool":
            case "08_Misool":
                if (IsFinalQuestOfIslandDone(IslandName.Misool))
                    completedIsland = IslandName.Misool;
                break;
            case "05_Arborek":
                completedIsland = IslandName.Arborek;
                break;
            case "04_Sawinggrai":
            case "03_Sawinggrai":
                if (IsFinalQuestOfIslandDone(IslandName.Sawinggrai))
                    completedIsland = IslandName.Sawinggrai;
                break;
            case "10":
            case "04_Friwen":
                if (IsFinalQuestOfIslandDone(IslandName.Friwen))
                    completedIsland = IslandName.Friwen;
                break;
            case "08":
                // Quest ID 08 adalah Gag HANYA jika berada di Pulau Gag atau alur Gag aktif!
                // Jika sedang di Friwen, 08 adalah Trivia Friwen, jangan buka award Gag!
                if (sceneName.Contains("Gag") || HasCompletedQuestID("01"))
                {
                    completedIsland = IslandName.Gag;
                }
                break;
        }

        if (completedIsland.HasValue)
        {
            UnlockIslandAward(completedIsland.Value);
            Debug.Log($"<color=green>[QuestJournalManager] Quest '{questID}' menuntaskan pulau {completedIsland.Value}! Award & Halaman Pulau Terbuka!</color>");
        }
    }

    public void UnlockIslandAward(IslandName island)
    {
        unlockedAwardIslands.Add(island);
        PlayerPrefs.SetInt("QJM_Award_" + island.ToString(), 1);
        PlayerPrefs.SetInt("GPM_Island_" + island.ToString(), 1);
        PlayerPrefs.SetInt("Island_" + (int)island, 1);
        SaveJournalSaveData();
        CheckIslandPageUnlocks(island);
        UpdateAwardsUI();
    }

    public void UnlockIslandAwardForCurrentScene()
    {
        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        IslandName? island = null;
        if (sceneName.Contains("Waisai")) island = IslandName.Waisai;
        else if (sceneName.Contains("Pianemo") || sceneName.Contains("Piaynemo")) island = IslandName.Piaynemo;
        else if (sceneName.Contains("Misool")) island = IslandName.Misool;
        else if (sceneName.Contains("Arborek")) island = IslandName.Arborek;
        else if (sceneName.Contains("Sawinggrai")) island = IslandName.Sawinggrai;
        else if (sceneName.Contains("Friwen")) island = IslandName.Friwen;
        else if (sceneName.Contains("Gag")) island = IslandName.Gag;

        if (island.HasValue && IsFinalQuestOfIslandDone(island.Value))
        {
            UnlockIslandAward(island.Value);
        }
    }

    private void OnGameProgressUpdated(float percentage)
    {
        UpdateProgressUI(percentage);
        if (percentage >= 100f)
        {
            UnlockNewPage(12);
            UnlockNewPage(17);
        }
    }

    private void OnIslandCompletedHandler(IslandName island)
    {
        UnlockIslandAward(island);
    }

    // =========================================================
    // JOURNAL VISIBILITY & CONTROLS
    // =========================================================

    public void ToggleJournalPanel()
    {
        if (questTabPanel == null)
        {
            // Coba auto-find QuestTabUI jika belum terhubung
            QuestTabUI tabUI = FindFirstObjectByType<QuestTabUI>(FindObjectsInactive.Include);
            if (tabUI != null)
            {
                tabUI.RegisterToJournalManager();
                questTabPanel = tabUI.gameObject;
            }
            else
            {
                GameObject found = GameObject.Find("QuestTabUI");
                if (found != null) questTabPanel = found;
            }

            if (questTabPanel == null)
            {
                Debug.LogWarning("[QuestJournalManager] questTabPanel belum ter-assign.");
                return;
            }
        }

        bool willOpen = !questTabPanel.activeSelf;
        questTabPanel.SetActive(willOpen);
        Time.timeScale = willOpen ? 0f : 1f;

        if (willOpen)
        {
            UpdateAllJournalSystems();
        }
    }

    public void CloseJournal()
    {
        if (questTabPanel != null) questTabPanel.SetActive(false);
        Time.timeScale = 1f;
    }

    // =========================================================
    // PROGRESS TRACKING (KIRI ATAS)
    // =========================================================

    private void UpdateProgressUI(float percentage = -1f)
    {
        if (percentage < 0f && GamePhaseManager.Instance != null)
        {
            percentage = GamePhaseManager.Instance.GetCurrentPercentage();
        }

        if (percentage < 0f) percentage = 0f;

        int displayPct = Mathf.FloorToInt(percentage);

        if (progressPercentageText != null)
        {
            progressPercentageText.text = $"Progress: {displayPct}%";
        }

        if (trashStatusText != null)
        {
            int trashCount = PlayerPrefs.GetInt("TotalTrashCollected", TrashInteractable.totalCollected);
            trashStatusText.text = $"{trashCount}/20";
        }
    }

    public string GetCurrentIslandDisplayName()
    {
        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        if (sceneName.Contains("Waisai") || sceneName.Contains("Airstrip")) return "Pulau Waisai";
        if (sceneName.Contains("Pianemo")) return "Pulau Piaynemo";
        if (sceneName.Contains("Misool")) return "Pulau Misool";
        if (sceneName.Contains("Arborek")) return "Pulau Arborek";
        if (sceneName.Contains("Sawinggrai")) return "Pulau Sawinggrai";
        if (sceneName.Contains("Friwen")) return "Pulau Friwen";
        if (sceneName.Contains("Gag")) return "Pulau Gag";
        return "Raja Ampat";
    }

    // =========================================================
    // QUEST TAB (ONGOING & COMPLETED)
    // =========================================================

    public void ToggleQuestTab()
    {
        if (questTabPanel == null)
        {
            QuestTabUI tabUI = FindFirstObjectByType<QuestTabUI>(FindObjectsInactive.Include);
            if (tabUI != null)
            {
                tabUI.RegisterToJournalManager();
                questTabPanel = tabUI.gameObject;
            }
        }

        if (questTabPanel != null && !questTabPanel.activeSelf)
        {
            ToggleJournalPanel();
            return;
        }

        isOngoingTabActive = !isOngoingTabActive;
        RefreshTabVisual();
        RefreshQuestTabText();
    }

    public void SetTab(bool ongoing)
    {
        isOngoingTabActive = ongoing;
        RefreshTabVisual();
        RefreshQuestTabText();
    }

    private void RefreshTabVisual()
    {
        if (folderImage != null)
        {
            folderImage.sprite = isOngoingTabActive ? tabOngoingSprite : tabCompletedSprite;
        }
    }

    public void RefreshQuestTabText()
    {
        EnsureQuestCardsInitialized();

        if (isOngoingTabActive)
        {
            // === ONGOING TAB ===
            if (questBoxPanel != null) questBoxPanel.SetActive(true);
            if (completedCard2 != null) completedCard2.SetActive(false);
            if (completedCard3 != null) completedCard3.SetActive(false);

            if (questIconImage != null) questIconImage.gameObject.SetActive(true);

            if (ongoingQuestText != null)
            {
                ongoingQuestText.gameObject.SetActive(true);
                RectTransform rtText = ongoingQuestText.GetComponent<RectTransform>();
                if (rtText != null)
                {
                    rtText.anchoredPosition = new Vector2(-28f, 70f);
                    rtText.sizeDelta = new Vector2(285f, 58f);
                }
                ongoingQuestText.alignment = TextAlignmentOptions.MidlineLeft;
                ongoingQuestText.enableAutoSizing = true;
                ongoingQuestText.fontSizeMin = 12f;
                ongoingQuestText.fontSizeMax = 20f;
                ongoingQuestText.fontSize = 20f;
                ongoingQuestText.overflowMode = TextOverflowModes.Ellipsis;

                string islandTitle = GetCurrentIslandDisplayName();
                ongoingQuestText.text = $"<size=20><color=#555555>{islandTitle}</color></size>\n<size=16><color=#111111><b>{currentOngoingQuest}</b></color></size>";
            }
        }
        else
        {
            // === COMPLETED TAB ===
            if (questIconImage != null) questIconImage.gameObject.SetActive(false);

            if (completedQuestTitles == null || completedQuestTitles.Count == 0)
            {
                // Belum ada misi selesai -> Tampilkan 1 card pesan kosong
                if (questBoxPanel != null) questBoxPanel.SetActive(true);
                if (ongoingQuestText != null)
                {
                    ongoingQuestText.gameObject.SetActive(true);
                    RectTransform rtText = ongoingQuestText.GetComponent<RectTransform>();
                    if (rtText != null)
                    {
                        rtText.anchoredPosition = new Vector2(-10f, 70f);
                        rtText.sizeDelta = new Vector2(314f, 53f);
                    }
                    ongoingQuestText.alignment = TextAlignmentOptions.MidlineLeft;
                    ongoingQuestText.enableAutoSizing = true;
                    ongoingQuestText.fontSizeMin = 12f;
                    ongoingQuestText.fontSizeMax = 18f;
                    ongoingQuestText.fontSize = 16f;
                    ongoingQuestText.text = "<size=16><color=#555555><i>Belum ada misi selesai.</i></color></size>";
                }
                if (completedCard2 != null) completedCard2.SetActive(false);
                if (completedCard3 != null) completedCard3.SetActive(false);
            }
            else
            {
                int count = completedQuestTitles.Count;
                int startIdx = Mathf.Max(0, count - 3);

                // Card 1
                if (questBoxPanel != null) questBoxPanel.SetActive(true);
                if (ongoingQuestText != null)
                {
                    ongoingQuestText.gameObject.SetActive(true);
                    RectTransform rtText = ongoingQuestText.GetComponent<RectTransform>();
                    if (rtText != null)
                    {
                        rtText.anchoredPosition = new Vector2(-10f, 70f);
                        rtText.sizeDelta = new Vector2(314f, 53f);
                    }
                    ongoingQuestText.alignment = TextAlignmentOptions.MidlineLeft;
                    ongoingQuestText.enableAutoSizing = true;
                    ongoingQuestText.fontSizeMin = 12f;
                    ongoingQuestText.fontSizeMax = 20f;
                    ongoingQuestText.fontSize = 18f;
                    ongoingQuestText.text = $"<size=18><color=#111111><b>{completedQuestTitles[startIdx]}</b></color></size>";
                }

                // Card 2
                if (count >= 2)
                {
                    if (completedCard2 != null)
                    {
                        completedCard2.SetActive(true);
                        if (completedText2 != null)
                        {
                            completedText2.enableAutoSizing = true;
                            completedText2.fontSizeMin = 12f;
                            completedText2.fontSizeMax = 20f;
                            completedText2.fontSize = 18f;
                            completedText2.text = $"<size=18><color=#111111><b>{completedQuestTitles[startIdx + 1]}</b></color></size>";
                        }
                    }
                }
                else
                {
                    if (completedCard2 != null) completedCard2.SetActive(false);
                }

                // Card 3
                if (count >= 3)
                {
                    if (completedCard3 != null)
                    {
                        completedCard3.SetActive(true);
                        if (completedText3 != null)
                        {
                            completedText3.enableAutoSizing = true;
                            completedText3.fontSizeMin = 12f;
                            completedText3.fontSizeMax = 20f;
                            completedText3.fontSize = 18f;
                            completedText3.text = $"<size=18><color=#111111><b>{completedQuestTitles[startIdx + 2]}</b></color></size>";
                        }
                    }
                }
                else
                {
                    if (completedCard3 != null) completedCard3.SetActive(false);
                }
            }
        }
    }

    private void EnsureQuestCardsInitialized()
    {
        if (folderImage == null) return;
        Transform leftPanel = folderImage.transform; // LeftPanel

        // Find or setup questBoxPanel (Card 1)
        if (questBoxPanel == null)
        {
            Transform panelT = leftPanel.Find("Panel");
            if (panelT != null) questBoxPanel = panelT.gameObject;
        }

        if (questBoxPanel != null)
        {
            RectTransform rt1 = questBoxPanel.GetComponent<RectTransform>();
            if (rt1 != null)
            {
                rt1.anchorMin = new Vector2(0.5f, 0.5f);
                rt1.anchorMax = new Vector2(0.5f, 0.5f);
                rt1.pivot = new Vector2(0.5f, 0.5f);
                rt1.anchoredPosition = new Vector2(-10f, 70f);
                rt1.sizeDelta = new Vector2(350f, 65f);
                rt1.localScale = Vector3.one;
            }
        }

        // Setup ongoingQuestText inside Card 1 / LeftPanel
        if (ongoingQuestText != null)
        {
            RectTransform rtText = ongoingQuestText.GetComponent<RectTransform>();
            if (rtText != null)
            {
                if (rtText.parent != leftPanel && rtText.parent != questBoxPanel.transform)
                {
                    rtText.SetParent(leftPanel, false);
                }
                rtText.anchorMin = new Vector2(0.5f, 0.5f);
                rtText.anchorMax = new Vector2(0.5f, 0.5f);
                rtText.pivot = new Vector2(0.5f, 0.5f);
                rtText.anchoredPosition = new Vector2(-28f, 70f);
                rtText.sizeDelta = new Vector2(285f, 58f);
                rtText.localScale = Vector3.one;
            }
            ongoingQuestText.margin = Vector4.zero;
        }

        // Setup questIconImage
        if (questIconImage != null)
        {
            RectTransform rtIcon = questIconImage.GetComponent<RectTransform>();
            if (rtIcon != null)
            {
                if (rtIcon.parent != leftPanel) rtIcon.SetParent(leftPanel, false);
                rtIcon.anchorMin = new Vector2(0.5f, 0.5f);
                rtIcon.anchorMax = new Vector2(0.5f, 0.5f);
                rtIcon.pivot = new Vector2(0.5f, 0.5f);
                rtIcon.anchoredPosition = new Vector2(138f, 70f);
                rtIcon.sizeDelta = new Vector2(36f, 36f);
                rtIcon.localScale = Vector3.one;
            }
        }

        // Setup Card 2 (Completed Card 2)
        if (completedCard2 == null || completedCard2.transform.parent != leftPanel)
        {
            Transform t2 = leftPanel.Find("CompletedCard2");
            if (t2 != null) completedCard2 = t2.gameObject;
        }
        if (completedCard2 == null && questBoxPanel != null)
        {
            completedCard2 = Instantiate(questBoxPanel, leftPanel);
            completedCard2.name = "CompletedCard2";
            // Hapus QuestIcon jika terbawa clone
            Transform iconClone = completedCard2.transform.Find("QuestIcon");
            if (iconClone != null) Destroy(iconClone.gameObject);
        }
        if (completedCard2 != null)
        {
            RectTransform rt2 = completedCard2.GetComponent<RectTransform>();
            if (rt2 != null)
            {
                rt2.anchorMin = new Vector2(0.5f, 0.5f);
                rt2.anchorMax = new Vector2(0.5f, 0.5f);
                rt2.pivot = new Vector2(0.5f, 0.5f);
                rt2.anchoredPosition = new Vector2(-10f, -2f);
                rt2.sizeDelta = new Vector2(350f, 65f);
                rt2.localScale = Vector3.one;
            }
            if (completedText2 == null || completedText2.transform.parent != completedCard2.transform)
            {
                completedText2 = completedCard2.GetComponentInChildren<TextMeshProUGUI>();
            }
            if (completedText2 == null)
            {
                GameObject tObj = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
                tObj.transform.SetParent(completedCard2.transform, false);
                completedText2 = tObj.GetComponent<TextMeshProUGUI>();
                if (ongoingQuestText != null)
                {
                    completedText2.font = ongoingQuestText.font;
                    completedText2.fontSharedMaterial = ongoingQuestText.fontSharedMaterial;
                }
            }
            if (completedText2 != null)
            {
                RectTransform rtT2 = completedText2.GetComponent<RectTransform>();
                rtT2.anchorMin = new Vector2(0f, 0f);
                rtT2.anchorMax = new Vector2(1f, 1f);
                rtT2.pivot = new Vector2(0.5f, 0.5f);
                rtT2.offsetMin = new Vector2(18f, 6f);
                rtT2.offsetMax = new Vector2(-18f, -6f);
                rtT2.localScale = Vector3.one;
                completedText2.margin = Vector4.zero;
                completedText2.alignment = TextAlignmentOptions.MidlineLeft;
                completedText2.enableAutoSizing = true;
                completedText2.fontSizeMin = 12f;
                completedText2.fontSizeMax = 20f;
                completedText2.fontSize = 18f;
            }
        }

        // Setup Card 3 (Completed Card 3)
        if (completedCard3 == null || completedCard3.transform.parent != leftPanel)
        {
            Transform t3 = leftPanel.Find("CompletedCard3");
            if (t3 != null) completedCard3 = t3.gameObject;
        }
        if (completedCard3 == null && questBoxPanel != null)
        {
            completedCard3 = Instantiate(questBoxPanel, leftPanel);
            completedCard3.name = "CompletedCard3";
            Transform iconClone = completedCard3.transform.Find("QuestIcon");
            if (iconClone != null) Destroy(iconClone.gameObject);
        }
        if (completedCard3 != null)
        {
            RectTransform rt3 = completedCard3.GetComponent<RectTransform>();
            if (rt3 != null)
            {
                rt3.anchorMin = new Vector2(0.5f, 0.5f);
                rt3.anchorMax = new Vector2(0.5f, 0.5f);
                rt3.pivot = new Vector2(0.5f, 0.5f);
                rt3.anchoredPosition = new Vector2(-10f, -74f);
                rt3.sizeDelta = new Vector2(350f, 65f);
                rt3.localScale = Vector3.one;
            }
            if (completedText3 == null || completedText3.transform.parent != completedCard3.transform)
            {
                completedText3 = completedCard3.GetComponentInChildren<TextMeshProUGUI>();
            }
            if (completedText3 == null)
            {
                GameObject tObj = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
                tObj.transform.SetParent(completedCard3.transform, false);
                completedText3 = tObj.GetComponent<TextMeshProUGUI>();
                if (ongoingQuestText != null)
                {
                    completedText3.font = ongoingQuestText.font;
                    completedText3.fontSharedMaterial = ongoingQuestText.fontSharedMaterial;
                }
            }
            if (completedText3 != null)
            {
                RectTransform rtT3 = completedText3.GetComponent<RectTransform>();
                rtT3.anchorMin = new Vector2(0f, 0f);
                rtT3.anchorMax = new Vector2(1f, 1f);
                rtT3.pivot = new Vector2(0.5f, 0.5f);
                rtT3.offsetMin = new Vector2(18f, 6f);
                rtT3.offsetMax = new Vector2(-18f, -6f);
                rtT3.localScale = Vector3.one;
                completedText3.margin = Vector4.zero;
                completedText3.alignment = TextAlignmentOptions.MidlineLeft;
                completedText3.enableAutoSizing = true;
                completedText3.fontSizeMin = 12f;
                completedText3.fontSizeMax = 20f;
                completedText3.fontSize = 18f;
            }
        }
    }

    // =========================================================
    // AWARDS SYSTEM (PER PULAU DARI GAMEPHASEMANAGER)
    // =========================================================

    public void UpdateAwardsUI()
    {
        if (islandAwards != null)
        {
            foreach (var award in islandAwards)
            {
                if (award == null || award.awardImage == null) continue;

                bool isUnlocked = IsIslandUnlocked(award.island);

                if (isUnlocked)
                {
                    if (award.unlockedSprite != null)
                    {
                        award.awardImage.sprite = award.unlockedSprite;
                        award.awardImage.color = Color.white;
                    }
                }
                else
                {
                    if (award.lockedSprite != null)
                    {
                        award.awardImage.sprite = award.lockedSprite;
                        award.awardImage.color = Color.white;
                    }
                    else
                    {
                        if (award.unlockedSprite != null)
                            award.awardImage.sprite = award.unlockedSprite;
                        award.awardImage.color = new Color(0.25f, 0.25f, 0.25f, 0.85f);
                    }
                }
            }
        }

        // Update Award Raja Ampat & Secret Ending
        bool allIslandsDone = false;
        if (GamePhaseManager.Instance != null)
        {
            allIslandsDone = GamePhaseManager.Instance.GetCurrentPercentage() >= 99f;
        }

        if (awardRajaAmpat != null)
        {
            awardRajaAmpat.color = allIslandsDone ? Color.white : new Color(0.25f, 0.25f, 0.25f, 0.85f);
        }

        if (awardSecretEnding != null)
        {
            int sasiCount = PlayerPrefs.GetInt("Sasi_Count", 0);
            awardSecretEnding.color = (allIslandsDone && sasiCount >= 2) ? Color.white : new Color(0.25f, 0.25f, 0.25f, 0.85f);
        }
    }

    public bool IsIslandUnlocked(IslandName island)
    {
        if (unlockedAwardIslands.Contains(island))
        {
            return true;
        }

        if (PlayerPrefs.GetInt("QJM_Award_" + island.ToString(), 0) == 1)
        {
            unlockedAwardIslands.Add(island);
            return true;
        }

        if (PlayerPrefs.GetInt("GPM_Island_" + island.ToString(), 0) == 1)
        {
            unlockedAwardIslands.Add(island);
            return true;
        }

        // Cek PlayerPrefs dari IslandCompletionTrigger ("Island_X")
        if (PlayerPrefs.GetInt("Island_" + (int)island, 0) == 1)
        {
            unlockedAwardIslands.Add(island);
            return true;
        }

        // Cek apakah quest terakhir pulau sudah selesai di completedQuestIDs / GamePhaseManager
        if (IsFinalQuestOfIslandDone(island))
        {
            unlockedAwardIslands.Add(island);
            return true;
        }

        return false;
    }

    public bool IsFinalQuestOfIslandDone(IslandName island)
    {
        switch (island)
        {
            case IslandName.Airstrip:
                return HasCompletedQuestID("01_Airstrip");
            case IslandName.Waisai:
                return HasCompletedQuestID("03_Waisai");
            case IslandName.Piaynemo:
                return HasCompletedQuestID("06_Piaynemo") || HasCompletedQuestID("05_Piaynemo");
            case IslandName.Misool:
                return (HasCompletedQuestID("07_Misool") || HasCompletedQuestID("08_Misool")) &&
                       (PlayerPrefs.GetInt("Misool_QuestStage", 0) >= 8 || MasterQuestManager.Instance?.CurrentStage >= 8);
            case IslandName.Arborek:
                return HasCompletedQuestID("05_Arborek");
            case IslandName.Sawinggrai:
                return HasCompletedQuestID("04_Sawinggrai") && HasCompletedQuestID("02_Sawinggrai");
            case IslandName.Friwen:
                return (HasCompletedQuestID("10") || HasCompletedQuestID("04_Friwen")) && HasCompletedQuestID("07");
            case IslandName.Gag:
                return HasCompletedQuestID("08") && 
                       (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name.Contains("Gag") || 
                        PlayerPrefs.GetInt("Visited_10_Gag", 0) == 1 || 
                        HasCompletedQuestID("01"));
            default:
                return false;
        }
    }

    private bool HasCompletedQuestID(string qid)
    {
        if (completedQuestIDs.Contains(qid)) return true;
        if (GamePhaseManager.Instance != null && GamePhaseManager.Instance.IsQuestCompleted(qid)) return true;
        return false;
    }

    // =========================================================
    // JOURNAL PAGE NAVIGATION & CONTENT UNLOCK
    // =========================================================

    public void CheckJournalPageUnlocks(string questID)
    {
        if (string.IsNullOrEmpty(questID)) return;

        bool newlyUnlocked = false;

        // Custom direct mappings according to story progression
        string activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;

        if (questID == "01_Waisai" || questID == "02_Waisai" || questID == "03_Waisai")
        {
            newlyUnlocked |= UnlockPageInternal(1);
            newlyUnlocked |= UnlockPageInternal(2);
        }
        else if (questID == "01_Piaynemo" || questID == "02_Piaynemo")
        {
            newlyUnlocked |= UnlockPageInternal(3);
        }
        else if (questID == "03_Piaynemo" || questID == "04_Piaynemo" || questID == "05_Piaynemo" || questID == "06_Piaynemo")
        {
            newlyUnlocked |= UnlockPageInternal(4);
        }
        else if (questID == "01_Arborek" || questID == "02_Arborek" || questID == "03_Arborek")
        {
            newlyUnlocked |= UnlockPageInternal(5);
        }
        else if (questID == "05_Arborek" || questID == "06_Arborek")
        {
            newlyUnlocked |= UnlockPageInternal(13); // Page 13: Selesai Arborek
        }
        else if (questID == "01_Misool" || questID == "02_Misool")
        {
            newlyUnlocked |= UnlockPageInternal(16); // Page 16: Poster Edukasi Goa Keramat Misool
        }
        else if (questID == "03_Misool" || questID == "04_Misool" || questID == "05_Misool" || questID == "06_Misool")
        {
            newlyUnlocked |= UnlockPageInternal(15); // Page 15: Laut Misool
        }
        else if (questID == "07_Misool" || questID == "08_Misool")
        {
            newlyUnlocked |= UnlockPageInternal(6); // Page 6: Manta Misool
        }
        else if (questID == "01_Sawinggrai" || questID == "04_Sawinggrai")
        {
            newlyUnlocked |= UnlockPageInternal(14); // Page 14: Sawinggrai Burung Cendrawasih
        }
        else if (questID == "02_Sawinggrai" || questID == "03_Sawinggrai")
        {
            if (HasCompletedQuestID("04_Sawinggrai"))
            {
                newlyUnlocked |= UnlockPageInternal(7); // Page 7: Selesai Sawinggrai
            }
        }
        else if (questID == "07" && activeScene.Contains("Friwen"))
        {
            newlyUnlocked |= UnlockPageInternal(8); // Page 8: Friwen Navigasi
        }
        else if (questID == "08" && activeScene.Contains("Friwen"))
        {
            newlyUnlocked |= UnlockPageInternal(8);
        }
        else if ((questID == "09" || questID == "03_Friwen") && activeScene.Contains("Friwen"))
        {
            newlyUnlocked |= UnlockPageInternal(9); // Page 9: Pasir Timbul Friwen
        }
        else if ((questID == "10" || questID == "04_Friwen") && activeScene.Contains("Friwen"))
        {
            newlyUnlocked |= UnlockPageInternal(8);
        }
        else if (questID == "01" && (activeScene.Contains("Gag") || !activeScene.Contains("Friwen")))
        {
            newlyUnlocked |= UnlockPageInternal(10); // Page 10: Tiba di Gag / Sampel
        }
        else if ((questID == "06" || questID == "07") && activeScene.Contains("Gag"))
        {
            newlyUnlocked |= UnlockPageInternal(11); // Page 11: Jeritan Tanah Asli Gag
        }
        else if (questID == "08" && activeScene.Contains("Gag"))
        {
            newlyUnlocked |= UnlockPageInternal(12); // Page 12: 100% Progress / Setelah Gag
            newlyUnlocked |= UnlockPageInternal(17); // Page 17: Sertifikat ECO-HERO 100%
        }

        if (allJournalDatabase != null)
        {
            foreach (var entry in allJournalDatabase)
            {
                if (entry == null) continue;

                if (!string.IsNullOrEmpty(entry.unlockOnQuestID) && entry.unlockOnQuestID == questID)
                {
                    newlyUnlocked |= UnlockPageInternal(entry.pageID);
                }
            }
        }

        if (newlyUnlocked) SaveJournalSaveData();
    }

    public void CheckIslandPageUnlocks(IslandName island)
    {
        bool newlyUnlocked = false;

        switch (island)
        {
            case IslandName.Waisai:
                newlyUnlocked |= UnlockPageInternal(1);
                newlyUnlocked |= UnlockPageInternal(2);
                break;
            case IslandName.Piaynemo:
                newlyUnlocked |= UnlockPageInternal(3);
                newlyUnlocked |= UnlockPageInternal(4);
                break;
            case IslandName.Arborek:
                newlyUnlocked |= UnlockPageInternal(5);
                newlyUnlocked |= UnlockPageInternal(13); // Page 13: Selesai Arborek
                break;
            case IslandName.Misool:
                newlyUnlocked |= UnlockPageInternal(6);  // Page 6: Manta Misool
                newlyUnlocked |= UnlockPageInternal(15); // Page 15: Laut Misool
                newlyUnlocked |= UnlockPageInternal(16); // Page 16: Poster Edukasi Goa Keramat
                break;
            case IslandName.Sawinggrai:
                newlyUnlocked |= UnlockPageInternal(7);  // Page 7: Selesai Sawinggrai
                newlyUnlocked |= UnlockPageInternal(14); // Page 14: Sawinggrai Burung Cendrawasih
                break;
            case IslandName.Friwen:
                newlyUnlocked |= UnlockPageInternal(8);  // Page 8: Friwen Navigasi
                newlyUnlocked |= UnlockPageInternal(9);  // Page 9: Pasir Timbul Friwen
                break;
            case IslandName.Gag:
                newlyUnlocked |= UnlockPageInternal(10); // Page 10: Tiba di Gag
                newlyUnlocked |= UnlockPageInternal(11); // Page 11: Jeritan Tanah Asli Gag
                newlyUnlocked |= UnlockPageInternal(12); // Page 12: 100% Progress / Setelah Gag
                newlyUnlocked |= UnlockPageInternal(17); // Page 17: Sertifikat ECO-HERO 100%
                break;
        }

        if (allJournalDatabase != null)
        {
            foreach (var entry in allJournalDatabase)
            {
                if (entry == null) continue;

                if (entry.unlockOnIslandComplete && entry.unlockIsland == island)
                {
                    newlyUnlocked |= UnlockPageInternal(entry.pageID);
                }
            }
        }

        if (newlyUnlocked) SaveJournalSaveData();
    }

    private bool UnlockPageInternal(int pageID)
    {
        if (!unlockedPages.Contains(pageID))
        {
            unlockedPages.Add(pageID);
            Debug.Log($"<color=cyan>[QuestJournalManager] Halaman Jurnal ID {pageID} Terbuka!</color>");
            return true;
        }
        return false;
    }

    public void UnlockNewPage(int pageID)
    {
        if (!unlockedPages.Contains(pageID))
        {
            unlockedPages.Add(pageID);
            SaveJournalSaveData();
            UpdateJournalPageUI();
        }
    }

    public void NextPage()
    {
        if (allJournalDatabase == null || allJournalDatabase.Length == 0) return;
        currentJournalIndex = (currentJournalIndex + 1) % allJournalDatabase.Length;
        UpdateJournalPageUI();
    }

    public void PrevPage()
    {
        if (allJournalDatabase == null || allJournalDatabase.Length == 0) return;
        currentJournalIndex--;
        if (currentJournalIndex < 0) currentJournalIndex = allJournalDatabase.Length - 1;
        UpdateJournalPageUI();
    }

    private void UpdateJournalPageUI()
    {
        if (allJournalDatabase == null || allJournalDatabase.Length == 0)
        {
            if (journalPageImage != null) journalPageImage.gameObject.SetActive(false);
            if (journalPhotoImage != null) journalPhotoImage.gameObject.SetActive(false);
            if (noPhotoWarning != null) noPhotoWarning.SetActive(false);
            if (pageNumberText != null) pageNumberText.text = "Page 0";
            return;
        }

        if (currentJournalIndex < 0 || currentJournalIndex >= allJournalDatabase.Length)
        {
            currentJournalIndex = 0;
        }

        JournalEntry entry = allJournalDatabase[currentJournalIndex];
        bool isUnlocked = unlockedPages.Contains(entry.pageID);

        if (pageNumberText != null)
        {
            pageNumberText.text = $"Page {currentJournalIndex + 1}";
        }

        if (isUnlocked)
        {
            if (journalPageImage != null)
            {
                journalPageImage.gameObject.SetActive(true);
                journalPageImage.sprite = entry.pageBackgroundSprite;
            }

            if (entry.requiresPhoto)
            {
                if (collectedPhotos.ContainsKey(entry.photoIDToDisplay))
                {
                    if (journalPhotoImage != null)
                    {
                        journalPhotoImage.gameObject.SetActive(true);
                        journalPhotoImage.sprite = collectedPhotos[entry.photoIDToDisplay];
                    }
                    if (noPhotoWarning != null) noPhotoWarning.SetActive(false);
                }
                else
                {
                    if (journalPhotoImage != null) journalPhotoImage.gameObject.SetActive(false);
                    if (noPhotoWarning != null) noPhotoWarning.SetActive(true);
                }
            }
            else
            {
                if (journalPhotoImage != null) journalPhotoImage.gameObject.SetActive(false);
                if (noPhotoWarning != null) noPhotoWarning.SetActive(false);
            }
        }
        else
        {
            // Halaman Terkunci
            if (journalPageImage != null)
            {
                journalPageImage.gameObject.SetActive(true);
                journalPageImage.sprite = lockedPageSprite != null ? lockedPageSprite : entry.pageBackgroundSprite;
            }
            if (journalPhotoImage != null) journalPhotoImage.gameObject.SetActive(false);
            if (noPhotoWarning != null) noPhotoWarning.SetActive(false);
        }
    }

    // =========================================================
    // REFRESH ALL
    // =========================================================

    public void UpdateAllJournalSystems()
    {
        UpdateProgressUI();
        RefreshTabVisual();
        RefreshQuestTabText();
        UpdateAwardsUI();
        UpdateJournalPageUI();
    }

    // =========================================================
    // PHOTO POPUP SEQUENCING
    // =========================================================

    public void TakeSpecificPhoto(int photoID)
    {
        if (isFlashing) return;

        Sprite photoSprite = GetPhotoSpriteByID(photoID);
        if (photoSprite != null && !collectedPhotos.ContainsKey(photoID))
        {
            collectedPhotos.Add(photoID, photoSprite);
            SaveJournalSaveData();
        }

        StartCoroutine(PhotoSequence());
    }

    private IEnumerator PhotoSequence()
    {
        isFlashing = true;

        if (cameraFlash != null) cameraFlash.color = new Color(1, 1, 1, 1);
        if (polaroidPopup != null) polaroidPopup.SetActive(true);

        float alpha = 1f;
        while (alpha > 0)
        {
            alpha -= Time.unscaledDeltaTime * 2.5f;
            if (cameraFlash != null) cameraFlash.color = new Color(1, 1, 1, alpha);
            yield return null;
        }

        yield return new WaitForSecondsRealtime(2f);

        if (polaroidPopup != null) polaroidPopup.SetActive(false);
        if (cameraFlash != null) cameraFlash.color = new Color(1, 1, 1, 0);
        isFlashing = false;
    }

    private Sprite GetPhotoSpriteByID(int id)
    {
        if (allPhotoSpritesDatabase != null && id >= 0 && id < allPhotoSpritesDatabase.Length)
        {
            return allPhotoSpritesDatabase[id];
        }
        return null;
    }

    // =========================================================
    // COMPATIBILITY API
    // =========================================================

    public void AddCompletedQuest()
    {
        // Legacy stub for QuestUI
        UpdateAllJournalSystems();
    }

    public void UnlockPianemoAward()
    {
        // Legacy stub for QuestUI
        UpdateAwardsUI();
    }

    // =========================================================
    // SAVE / LOAD
    // =========================================================

    private void SaveJournalSaveData()
    {
        string pagesStr = string.Join(",", unlockedPages);
        PlayerPrefs.SetString(SaveKeyUnlockedPages, pagesStr);

        string photoKeysStr = string.Join(",", collectedPhotos.Keys);
        PlayerPrefs.SetString(SaveKeyCollectedPhotos, photoKeysStr);

        string completedStr = string.Join("|", completedQuestTitles);
        PlayerPrefs.SetString(SaveKeyCompletedQuests, completedStr);

        string completedIDsStr = string.Join(",", completedQuestIDs);
        PlayerPrefs.SetString(SaveKeyCompletedQuestIDs, completedIDsStr);

        List<string> islandList = new List<string>();
        foreach (var isl in unlockedAwardIslands)
        {
            islandList.Add(isl.ToString());
        }
        PlayerPrefs.SetString(SaveKeyUnlockedIslands, string.Join(",", islandList));

        PlayerPrefs.Save();
    }

    private void LoadJournalSaveData()
    {
        unlockedPages.Clear();
        string pagesStr = PlayerPrefs.GetString(SaveKeyUnlockedPages, "1");
        if (!unlockedPages.Contains(1)) unlockedPages.Add(1);
        if (!string.IsNullOrEmpty(pagesStr))
        {
            foreach (var p in pagesStr.Split(','))
            {
                if (int.TryParse(p.Trim(), out int id) && !unlockedPages.Contains(id))
                {
                    unlockedPages.Add(id);
                }
            }
        }

        collectedPhotos.Clear();
        string photoKeysStr = PlayerPrefs.GetString(SaveKeyCollectedPhotos, "");
        if (!string.IsNullOrEmpty(photoKeysStr))
        {
            foreach (var k in photoKeysStr.Split(','))
            {
                if (int.TryParse(k.Trim(), out int id))
                {
                    Sprite s = GetPhotoSpriteByID(id);
                    if (s != null && !collectedPhotos.ContainsKey(id))
                    {
                        collectedPhotos.Add(id, s);
                    }
                }
            }
        }

        completedQuestIDs.Clear();
        string completedIDsStr = PlayerPrefs.GetString(SaveKeyCompletedQuestIDs, "");
        if (!string.IsNullOrEmpty(completedIDsStr))
        {
            foreach (var qid in completedIDsStr.Split(','))
            {
                string trimmed = qid.Trim();
                if (!string.IsNullOrEmpty(trimmed))
                {
                    completedQuestIDs.Add(trimmed);
                }
            }
        }

        unlockedAwardIslands.Clear();
        string unlockedIslandsStr = PlayerPrefs.GetString(SaveKeyUnlockedIslands, "");
        if (!string.IsNullOrEmpty(unlockedIslandsStr))
        {
            foreach (var islStr in unlockedIslandsStr.Split(','))
            {
                if (Enum.TryParse(islStr.Trim(), out IslandName parsedIsland))
                {
                    unlockedAwardIslands.Add(parsedIsland);
                }
            }
        }

        completedQuestTitles.Clear();
        string completedStr = PlayerPrefs.GetString(SaveKeyCompletedQuests, "");
        if (!string.IsNullOrEmpty(completedStr))
        {
            foreach (var title in completedStr.Split('|'))
            {
                string trimmed = title.Trim();
                if (!string.IsNullOrEmpty(trimmed) && !completedQuestTitles.Contains(trimmed))
                {
                    completedQuestTitles.Add(trimmed);
                }
            }
            if (completedQuestTitles.Count > 0)
            {
                lastCompletedQuest = completedQuestTitles[completedQuestTitles.Count - 1];
            }
        }
    }
}
