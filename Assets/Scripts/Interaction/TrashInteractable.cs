using UnityEngine;
using TMPro;
using UnityEngine.Events;

public class TrashInteractable : MonoBehaviour, IInteractable
{
    [Header("Quest Integration")]
    [SerializeField] private string objectiveID = "collect_trash";
    [SerializeField] private int amountToAdd = 1;

    [Header("Audio & FX (Opsional)")]
    [SerializeField] private AudioClip collectSound;

    [Header("QTE Hold E")]
    [Tooltip("Hubungkan komponen QTE2 di Inspector. Sampah diambil setelah QTE berhasil.")]
    [SerializeField] private QTE2 holdQTE;

    [Header("UI Collectibles (Dari Kamu)")]
    [Tooltip("Tarik teks 'Progress' dari papan cokelat ke kolom ini")]
    public TextMeshProUGUI collectibleUIText;

    [Tooltip("Isi ID Jurnal jika sampah ini ngebuka cerita baru. Biarkan -1 jika tidak.")]
    public int unlockJournalID = -1;

    // Total sampah global yang sudah diambil pemain
    public static int totalCollected = 0;

    // Sesuaikan dengan total sampah mentok yang ada di game kalian
    private int maxTrash = 20;
    private bool collectionInProgress;
    public UnityEvent onInteractComplete; // Akan muncul di Inspector

    private void Awake()
    {
        totalCollected = PlayerPrefs.GetInt("TotalTrashCollected", 0);
    }

    private void Start()
    {
        if (QuestJournalManager.IsCurrentSceneIslandCompletedPersistent())
        {
            gameObject.SetActive(false);
        }
    }

    public void Interact()
    {
        if (collectionInProgress) return;

        if (holdQTE != null)
        {
            collectionInProgress = true;
            holdQTE.onSuccess.RemoveListener(CompleteCollection);
            holdQTE.onSuccess.AddListener(CompleteCollection);

            holdQTE.onFail.RemoveListener(CancelCollection);
            holdQTE.onFail.AddListener(CancelCollection);

            if (!holdQTE.gameObject.activeInHierarchy)
            {
                holdQTE.gameObject.SetActive(true);
            }

            holdQTE.BeginQTE();
            return;
        }

        CompleteCollection();
    }

    private void CancelCollection()
    {
        if (holdQTE != null)
        {
            holdQTE.onSuccess.RemoveListener(CompleteCollection);
            holdQTE.onFail.RemoveListener(CancelCollection);
        }
        collectionInProgress = false;
        Debug.Log("[TrashInteractable] QTE Gagal/Dibatalkan. Interaksi siap dicoba kembali.");
    }

    // Dipanggil otomatis saat QTE2 berhasil.
    private void CompleteCollection()
    {
        if (holdQTE != null)
        {
            holdQTE.onSuccess.RemoveListener(CompleteCollection);
            holdQTE.onFail.RemoveListener(CancelCollection);
        }

        collectionInProgress = false;

        // 1. Jalankan Logika Quest di QuestController jika ada
        if (QuestController.Instance != null && !string.IsNullOrEmpty(objectiveID))
        {
            QuestController.Instance.ProgressObjective(objectiveID, amountToAdd);
        }

        // 2. Integrasi MasterQuestManager (Bulu Seribu / Hama Misool)
        if (objectiveID == "collect_buluseribu")
        {
            if (MasterQuestManager.Instance != null)
            {
                MasterQuestManager.Instance.AddBuluSeribu();
            }
        }

        // 3. Putar Suara
        if (collectSound != null)
        {
            AudioSource.PlayClipAtPoint(collectSound, transform.position);
        }

        // 4. Update UI Progress Bar
        totalCollected += amountToAdd;
        PlayerPrefs.SetInt("TotalTrashCollected", totalCollected);
        PlayerPrefs.Save();

        if (QuestJournalManager.Instance != null)
        {
            QuestJournalManager.Instance.UpdateAwardsUI();
        }

        if (collectibleUIText != null)
        {
            float persentase = ((float)totalCollected / maxTrash) * 100f;
            collectibleUIText.text = "Progress: " + persentase.ToString("F0") + "%";
        }

        // 5. Buka halaman jurnal baru jika sampah ini punya cerita (Integrasi UI Kanan)
        if (unlockJournalID != -1 && QuestJournalManager.Instance != null)
        {
            QuestJournalManager.Instance.TakeSpecificPhoto(unlockJournalID);
        }

        // 6. Panggil Event Inspector jika ada
        onInteractComplete?.Invoke();

        // Panggil QuestObjectiveTrigger jika ada pada GameObject ini agar objective ter-update sebelum hancur
        QuestObjectiveTrigger objectiveTrigger = GetComponent<QuestObjectiveTrigger>();
        if (objectiveTrigger != null)
        {
            objectiveTrigger.ForceTrigger();
        }

        // 7. Hapus sampah/hama dari layar
        Destroy(gameObject);
    }

    public void SelesaiInteraksi()
    {
        Debug.Log("[TrashInteractable] Interaksi Selesai!");
        onInteractComplete?.Invoke();
    }

    [Header("Prompt Settings")]
    [Tooltip("Teks prompt saat didekati pemain. Biarkan kosong jika ingin default 'Ambil Sampah'")]
    [SerializeField] private string customPrompt = "";

    public string GetPromptText() => string.IsNullOrEmpty(customPrompt) ? "Ambil Sampah" : customPrompt;
}
