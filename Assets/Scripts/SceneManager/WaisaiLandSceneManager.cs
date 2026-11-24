using UnityEngine;

public class WaisaiLandSceneManager : MonoBehaviour
{
    public static WaisaiLandSceneManager Instance { get; private set; }
    private int lastObservedStage = -1;

    [Header("Referensi NPC & Objek Waisai")]
    [Tooltip("Petugas loket informasi / Hub awal")]
    [SerializeField] private GameObject petugasHub;

    [Tooltip("Tour Guide untuk Quest 2 / Trivia Waisai")]
    [SerializeField] private GameObject npcQuest2;

    [Tooltip("Warlok penduduk lokal Waisai")]
    [SerializeField] private GameObject warlok;

    [Tooltip("Dialog / Canvas setelah menyelesaikan Trivia")]
    [SerializeField] private GameObject dialogueAfterTrivia;

    [Tooltip("Trigger atau Teleporter di dermaga menuju pulau berikutnya")]
    [SerializeField] private GameObject dermagaTrigger;

    [Tooltip("Tour Guide sambutan awal di pelabuhan (memberi Quest1_Waisai)")]
    [SerializeField] private GameObject tourGuideIntro;

    [Tooltip("Tour Guide setelah Petugas Hub (Trivia Waisai)")]
    [SerializeField] private GameObject tourGuideTrivia;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        AutoFindReferencesIfNull();
    }

    private void AutoFindReferencesIfNull()
    {
        if (petugasHub == null) petugasHub = GameObject.Find("PetugasHub");
        if (npcQuest2 == null) npcQuest2 = GameObject.Find("NPCQuest2");
        if (warlok == null) warlok = GameObject.Find("Warlok");
        if (dialogueAfterTrivia == null) dialogueAfterTrivia = GameObject.Find("DialogueAfterTrivia");
        if (dermagaTrigger == null) dermagaTrigger = GameObject.Find("Dermaga_Waisai");
        if (tourGuideIntro == null) tourGuideIntro = GameObject.Find("TourGuide");
        if (tourGuideTrivia == null) tourGuideTrivia = GameObject.Find("TourGuide_Trivia");
    }

    private void Start()
    {
        if (MasterQuestManager.Instance != null)
        {
            MasterQuestManager.Instance.SetActiveIsland(IslandType.Waisai);
            MasterQuestManager.Instance.EnsureStageInitialized();
            Debug.Log($"<color=cyan>[WaisaiLandSceneManager] STAGE WAISAI SAAT INI: {MasterQuestManager.Instance.CurrentStage}</color>");
        }

        RefreshLandState(isInitialStart: true);
    }

    private bool lastObservedHubUnlocked = false;

    private static bool IsHubUnlocked(int stage)
    {
        if (stage >= 1) return true;
        if (QuestJournalManager.HasCompletedQuestIDPersistent("01_Waisai")) return true;
        return QuestController.Instance != null && QuestController.Instance.IsQuestActive("01_Waisai");
    }

    private void Update()
    {
        if (MasterQuestManager.Instance == null) return;

        int stage = MasterQuestManager.Instance.CurrentStage;
        bool hubUnlocked = IsHubUnlocked(stage);
        if (stage != lastObservedStage || hubUnlocked != lastObservedHubUnlocked)
        {
            RefreshLandState(isInitialStart: false);
        }
    }

    public void RefreshLandState()
    {
        RefreshLandState(isInitialStart: false);
    }

    private void RefreshLandState(bool isInitialStart)
    {
        if (MasterQuestManager.Instance == null) return;

        int stage = MasterQuestManager.Instance.CurrentStage;
        lastObservedStage = stage;

        Debug.Log($"<color=cyan>[WaisaiLandSceneManager] Refresh State Waisai. Stage Aktif: {stage}</color>");

        // Stage 0: Bertemu Petugas Hub di loket
        if (petugasHub != null)
        {
            petugasHub.SetActive(stage == 0 || stage == 1);
        }

        // Stage 1: Bertemu Tour Guide / Menjawab Trivia
        if (npcQuest2 != null)
        {
            npcQuest2.SetActive(stage >= 1);
        }

        // Warlok aktif untuk eksplorasi dan interaksi
        if (warlok != null)
        {
            warlok.SetActive(true);
        }

        // Stage 2+: Selesai Waisai, dermaga terbuka untuk melanjutkan ke pulau berikutnya
        if (dermagaTrigger != null)
        {
            dermagaTrigger.SetActive(stage >= 2);
        }

        if (tourGuideIntro == null) tourGuideIntro = GameObject.Find("TourGuide");
        if (tourGuideTrivia == null) tourGuideTrivia = GameObject.Find("TourGuide_Trivia");

        bool hubUnlocked = IsHubUnlocked(stage);
        bool introQuestCompleted = QuestJournalManager.HasCompletedQuestIDPersistent("01_Waisai");
        lastObservedHubUnlocked = hubUnlocked;
        bool showIntro = (stage == 0) && !hubUnlocked;

        if (petugasHub != null)
        {
            Collider2D hubCol = petugasHub.GetComponent<Collider2D>();
            if (hubCol != null) hubCol.enabled = hubUnlocked;

            Dialogue hubDlg = petugasHub.GetComponent<Dialogue>();
            if (hubDlg != null) hubDlg.enabled = hubUnlocked;

            if (!hubUnlocked)
            {
                Transform promptTr = petugasHub.transform.Find("DialogueCanvas/InteractPrompt");
                if (promptTr != null) promptTr.gameObject.SetActive(false);
            }
        }

        if (tourGuideIntro != null)
        {
            if (showIntro)
            {
                tourGuideIntro.SetActive(true);
            }
            else if (stage >= 1 || introQuestCompleted || isInitialStart)
            {
                // Saat 01_Waisai baru diterima di scene yang sama, biarkan FaderTrigger.onMidFade menonaktifkan TourGuide setelah fade hitam.
                tourGuideIntro.SetActive(false);
            }
        }

        if (tourGuideTrivia != null)
        {
            tourGuideTrivia.SetActive(stage >= 1);
        }

        if (showIntro && QuestController.Instance != null && !QuestController.Instance.HasActiveQuest())
        {
            QuestUI ui = FindFirstObjectByType<QuestUI>();
            if (ui != null)
            {
                ui.ShowStandalonePrompt("Temui Tour Guide di pelabuhan Waisai");
            }
        }
    }

    public void OnPetugasHubCompleted()
    {
        if (tourGuideIntro != null)
        {
            tourGuideIntro.SetActive(false);
        }

        if (MasterQuestManager.Instance != null && MasterQuestManager.Instance.CurrentStage < 1)
        {
            AdvanceStageTo(1);
        }
        else
        {
            RefreshLandState(isInitialStart: false);
        }
    }

    public void AdvanceStageTo(int newStage)
    {
        if (MasterQuestManager.Instance != null)
        {
            MasterQuestManager.Instance.AdvanceStageTo(newStage);
        }
        else
        {
            RefreshLandState();
        }
    }
}
