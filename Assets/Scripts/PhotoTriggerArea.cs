using UnityEngine;
using UnityEngine.Events;

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
    [Tooltip("Dipanggil tepat setelah foto berhasil diambil & objective quest diproses")]
    public UnityEvent onObjectiveEnd;

    private bool isPlayerInRange = false;
    private bool isPhotoTaken = false;

    private void Start()
    {
        // Sembunyikan UI Prompt Foto di awal game
        if (photoPromptPanel != null)
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
        if (isPlayerInRange && !isPhotoTaken && Input.GetKeyDown(interactKey))
        {
            TakeAreaPhoto();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") || (other.transform.root != null && other.transform.root.CompareTag("Player")))
        {
            isPlayerInRange = true;

            // Munculkan UI Prompt Foto saat player mendekat ke area
            if (!isPhotoTaken && photoPromptPanel != null)
            {
                photoPromptPanel.SetActive(true);
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player") || (other.transform.root != null && other.transform.root.CompareTag("Player")))
        {
            isPlayerInRange = false;

            // Sembunyikan UI Prompt Foto saat player menjauh
            if (photoPromptPanel != null)
            {
                photoPromptPanel.SetActive(false);
            }
        }
    }

    private void TakeAreaPhoto()
    {
        isPhotoTaken = true;

        // Langsung matikan UI Prompt Foto saat tombol F ditekan
        if (photoPromptPanel != null)
        {
            photoPromptPanel.SetActive(false);
        }

        // 1. Panggil QuestJournalManager untuk mengurus Flash, Polaroid Popup, dan Jurnal secara terpusat
        if (QuestJournalManager.Instance != null)
        {
            QuestJournalManager.Instance.TakeSpecificPhoto(photoID);
            QuestJournalManager.Instance.UnlockNewPage(pageIDToUnlock);
            Debug.Log($"[PhotoTrigger] Foto ID {photoID} diambil & Halaman Jurnal ID {pageIDToUnlock} dibuka!");
        }
        else
        {
            Debug.LogWarning("[PhotoTrigger] QuestJournalManager.Instance tidak ditemukan di scene!");
        }

        // 2. Progress Objective Quest
        if (QuestController.Instance != null && !string.IsNullOrEmpty(objectiveID))
        {
            QuestController.Instance.ProgressObjective(objectiveID, 1);
            Debug.Log($"[PhotoTrigger] Quest Objective '{objectiveID}' Berhasil!");
        }

        // 3. Panggil Event Transisi Layar
        onObjectiveEnd?.Invoke();
    }
}