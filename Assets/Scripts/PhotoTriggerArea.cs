using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public class PhotoTriggerArea : MonoBehaviour
{
    [Header("UI Reference")]
    [Tooltip("Drag GameObject PromptPanelFoto ke sini")]
    public GameObject photoPromptPanel; // Slot UI Prompt Tombol F

    [Header("Quest Integration")]
    public string objectiveID = "foto_sawinggrai";

    [Header("Journal & Photo Settings")]
    [Tooltip("ID Foto di array All Photo Sprites Database pada QuestJournalManager")]
    public int photoID = 0;

    [Tooltip("ID Halaman Jurnal yang ingin dibuka saat foto ini diambil")]
    public int pageIDToUnlock = 1;

    [Header("Input Settings")]
    public KeyCode interactKey = KeyCode.F; // Tombol F sesuai permintaan

    [Header("Events")]
    [Tooltip("Jika dicentang, onObjectiveEnd menunggu hingga popup Polaroid selesai tampil sebelum dipanggil.")]
    [SerializeField] private bool waitForPolaroidBeforeEnd = false;
    [Tooltip("Dipanggil tepat setelah foto berhasil diambil & objective quest diproses")]
    public UnityEvent onObjectiveEnd;

    private bool isPlayerInRange = false;
    private bool isPhotoTaken = false;
    private bool isShowingPrompt = false;

    private void EnsurePromptReference()
    {
        if (photoPromptPanel != null) return;
        Scene activeScene = SceneManager.GetActiveScene();
        if (!activeScene.isLoaded) return;
        GameObject[] roots = activeScene.GetRootGameObjects();
        GameObject fallback = null;
        for (int r = 0; r < roots.Length; r++)
        {
            if (roots[r] == null) continue;
            Transform[] all = roots[r].GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i] == null) continue;
                if (all[i].name == "PhotoPromptPanel")
                {
                    photoPromptPanel = all[i].gameObject;
                    return;
                }
                if (fallback == null && all[i].name == "PromptPanelFoto")
                {
                    fallback = all[i].gameObject;
                }
            }
        }
        if (fallback != null) photoPromptPanel = fallback;
    }

    private void SetPromptVisible(bool visible)
    {
        if (visible)
        {
            EnsurePromptReference();
            if (photoPromptPanel != null && !photoPromptPanel.activeSelf) photoPromptPanel.SetActive(true);
            isShowingPrompt = photoPromptPanel != null;
        }
        else
        {
            if (isShowingPrompt && photoPromptPanel != null && photoPromptPanel.activeSelf) photoPromptPanel.SetActive(false);
            isShowingPrompt = false;
        }
    }

    private void OnEnable()
    {
        EnsurePromptReference();
        if (!QuestJournalManager.IsCurrentSceneIslandCompletedPersistent())
        {
            isPhotoTaken = false;
        }
    }

    private void Start()
    {
        EnsurePromptReference();
        if (photoPromptPanel != null && !isPlayerInRange)
        {
            photoPromptPanel.SetActive(false);
        }

        if (QuestJournalManager.IsCurrentSceneIslandCompletedPersistent())
        {
            isPhotoTaken = true;
            gameObject.SetActive(false);
        }
    }

    private void Update()
    {
        // Menekan tombol F saat berada di area foto
        if (isPlayerInRange && !isPhotoTaken)
        {
            if (InteractionInputLock.IsAnyModalBlockingWorldInteract || InteractionInputLock.IsSignboardOpen)
            {
                SetPromptVisible(false);
                return;
            }
            SetPromptVisible(true);
            if (Input.GetKeyDown(interactKey))
            {
                TakeAreaPhoto();
            }
        }
        else if (isShowingPrompt)
        {
            SetPromptVisible(false);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") || (other.transform.root != null && other.transform.root.CompareTag("Player")))
        {
            isPlayerInRange = true;

            // Munculkan UI Prompt Foto saat player mendekat ke area
            if (!isPhotoTaken && !InteractionInputLock.IsAnyModalBlockingWorldInteract && !InteractionInputLock.IsSignboardOpen)
            {
                SetPromptVisible(true);
            }
        }
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (other.CompareTag("Player") || (other.transform.root != null && other.transform.root.CompareTag("Player")))
        {
            isPlayerInRange = true;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player") || (other.transform.root != null && other.transform.root.CompareTag("Player")))
        {
            isPlayerInRange = false;

            // Sembunyikan UI Prompt Foto saat player menjauh
            SetPromptVisible(false);
        }
    }

    private void OnDisable()
    {
        isPlayerInRange = false;
        SetPromptVisible(false);
    }

    private void TakeAreaPhoto()
    {
        isPhotoTaken = true;

        // Langsung matikan UI Prompt Foto saat tombol F ditekan
        SetPromptVisible(false);

        // 2. Progress Objective Quest
        if (QuestController.Instance != null && !string.IsNullOrEmpty(objectiveID))
        {
            QuestController.Instance.ProgressObjective(objectiveID, 1);
            if (objectiveID == "foto_friwen")
            {
                QuestController.Instance.MarkQuestCompleted("03_Friwen");
                if (QuestJournalManager.Instance != null)
                {
                    QuestJournalManager.Instance.RecordCompletedQuest("03_Friwen", "Foto Friwen");
                }
            }
            else if (objectiveID == "foto_arborek")
            {
                QuestController.Instance.MarkQuestCompleted("05_Arborek");
                if (QuestJournalManager.Instance != null)
                {
                    QuestJournalManager.Instance.RecordCompletedQuest("05_Arborek", "Ambil foto tradisi Sasi Arborek");
                }
            }
            Debug.Log($"[PhotoTrigger] Quest Objective '{objectiveID}' Berhasil!");
        }

        // 1 & 3. Panggil QuestJournalManager + Event Transisi Layar (tunggu Polaroid jika diaktifkan di Inspector)
        if (QuestJournalManager.Instance != null)
        {
            QuestJournalManager.Instance.UnlockNewPage(pageIDToUnlock);
            if (waitForPolaroidBeforeEnd)
            {
                QuestJournalManager.Instance.TakeSpecificPhoto(photoID, () => onObjectiveEnd?.Invoke());
            }
            else
            {
                QuestJournalManager.Instance.TakeSpecificPhoto(photoID);
                onObjectiveEnd?.Invoke();
            }
            Debug.Log($"[PhotoTrigger] Foto ID {photoID} diambil & Halaman Jurnal ID {pageIDToUnlock} dibuka!");
        }
        else
        {
            Debug.LogWarning("[PhotoTrigger] QuestJournalManager.Instance tidak ditemukan di scene!");
            onObjectiveEnd?.Invoke();
        }
    }
}