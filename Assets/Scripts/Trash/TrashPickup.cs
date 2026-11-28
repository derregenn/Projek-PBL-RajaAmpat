using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.Events;

/// <summary>
/// Komponen untuk setiap objek sampah individual di scene.
///
/// CARA SETUP DI UNITY INSPECTOR:
///   1. Pasang script ini ke GameObject sampah (ganti / tambah bersama TrashInteractable jika sudah ada).
///   2. Isi "Trash ID" — format: "NamaScene_NamaObjek" misal "04_Misool_SampahBot01".
///      ID harus UNIK di seluruh game, tidak boleh ada dua sampah dengan ID yang sama.
///   3. Opsional: isi "Collect Sound", "Objective ID" (integrasi quest), dan events.
///
/// CARA KERJA:
///   - Di Awake, daftar ke GlobalTrashManager.
///   - Jika sudah diambil (save lama), langsung nonaktifkan diri sendiri.
///   - Saat Interact(), catat ke GlobalTrashManager, update quest, lalu Destroy.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class TrashPickup : MonoBehaviour, IInteractable
{
    // =========================================================
    // INSPECTOR FIELDS
    // =========================================================

    [Header("Identitas Sampah (WAJIB UNIK)")]
    [Tooltip("Format: SceneName_ObjectName. Contoh: 04_Misool_SampahBot01")]
    [SerializeField] private string trashID = "";

    [Header("Integrasi Quest (Opsional)")]
    [Tooltip("Objective ID di QuestController. Kosongkan jika tidak perlu.")]
    [SerializeField] private string objectiveID = "";
    [SerializeField] private int amountToAdd = 1;

    [Header("Integrasi MasterQuestManager (Bulu Seribu)")]
    [Tooltip("Centang jika sampah ini adalah Bulu Seribu Misool.")]
    [SerializeField] private bool isBuluSeribu = false;

    [Header("QTE Hold E (Opsional)")]
    [Tooltip("Hubungkan QTE2 jika sampah ini perlu Hold E.")]
    [SerializeField] private QTE2 holdQTE;

    [Header("Audio & FX")]
    [SerializeField] private AudioClip collectSound;

    [Header("UI Counter (Opsional)")]
    [Tooltip("Teks UI progress sampah. Diisi otomatis.")]
    public TextMeshProUGUI collectibleUIText;

    [Header("Popup UI")]
    [Tooltip("Referensi GameObject popup 'Trash Collected' pada Canvas.")]
    [SerializeField] private GameObject trashCollectedPopupUI;
    [SerializeField, Min(0f)] private float popupFadeOutDuration = 1.5f;

    [Header("Prompt")]
    [SerializeField] private string customPrompt = "";

    [Header("Events")]
    public UnityEvent onCollected;

    // =========================================================
    // STATE
    // =========================================================

    private bool collectionInProgress;
    private bool collected;

    // =========================================================
    // UNITY LIFECYCLE
    // =========================================================

    private void Awake()
    {
        if (string.IsNullOrWhiteSpace(trashID))
        {
            Debug.LogWarning($"[TrashPickup] '{gameObject.name}' tidak memiliki TrashID! Harap isi di Inspector.", this);
            return;
        }

        bool shouldBeActive = GlobalTrashManager.Instance.RegisterTrash(trashID);

        if (!shouldBeActive)
        {
            // Sudah diambil di sesi sebelumnya — nonaktifkan tanpa animasi
            gameObject.SetActive(false);
        }
    }

    private void Start()
    {
        // Nonaktifkan jika pulau sudah selesai DAN tidak dalam Free Roam
        if (!GlobalTrashManager.Instance.IsFreeRoamMode &&
            QuestJournalManager.IsCurrentSceneIslandCompletedPersistent())
        {
            gameObject.SetActive(false);
        }
    }

    // =========================================================
    // INTERACTABLE INTERFACE
    // =========================================================

    public string GetPromptText() =>
        string.IsNullOrEmpty(customPrompt) ? "Ambil Sampah" : customPrompt;

    public void Interact()
    {
        if (collected || collectionInProgress) return;

        if (holdQTE != null)
        {
            collectionInProgress = true;
            holdQTE.onSuccess.RemoveListener(CompleteCollection);
            holdQTE.onSuccess.AddListener(CompleteCollection);
            holdQTE.onFail.RemoveListener(CancelCollection);
            holdQTE.onFail.AddListener(CancelCollection);

            if (!holdQTE.gameObject.activeInHierarchy)
                holdQTE.gameObject.SetActive(true);

            holdQTE.BeginQTE();
            return;
        }

        CompleteCollection();
    }

    // =========================================================
    // COLLECTION LOGIC
    // =========================================================

    private void CancelCollection()
    {
        if (holdQTE != null)
        {
            holdQTE.onSuccess.RemoveListener(CompleteCollection);
            holdQTE.onFail.RemoveListener(CancelCollection);
        }
        collectionInProgress = false;
    }

    private void CompleteCollection()
    {
        if (collected) return;
        collected = true;

        if (holdQTE != null)
        {
            holdQTE.onSuccess.RemoveListener(CompleteCollection);
            holdQTE.onFail.RemoveListener(CancelCollection);
        }
        collectionInProgress = false;

        // 1. Catat ke GlobalTrashManager
        bool isNew = GlobalTrashManager.Instance.CollectTrash(trashID);
        if (isNew)
        {
            TrashCollectedHUD.Show();
            if (trashCollectedPopupUI != null)
            {
                trashCollectedPopupUI.SetActive(true);
                StartCoroutine(FadeOutPopup());
            }
        }

        // 2. Update Quest Objective
        if (!string.IsNullOrEmpty(objectiveID) && QuestController.Instance != null)
        {
            QuestController.Instance.ProgressObjective(objectiveID, amountToAdd);
        }

        // 3. Bulu Seribu (Misool)
        if (isBuluSeribu && MasterQuestManager.Instance != null)
        {
            MasterQuestManager.Instance.AddBuluSeribu();
        }

        // 4. Audio
        if (collectSound != null)
            AudioSource.PlayClipAtPoint(collectSound, transform.position);

        // 5. Update UI Counter
        if (collectibleUIText != null)
        {
            int total = GlobalTrashManager.Instance.TotalInGame;
            int col = GlobalTrashManager.Instance.TotalCollected;
            float pct = total > 0 ? (float)col / total * 100f : 0f;
            collectibleUIText.text = $"Progress: {pct:F0}%";
        }

        // 6. Update Journal / Awards UI
        QuestJournalManager.Instance?.UpdateAwardsUI();

        // 7. QuestObjectiveTrigger lama (kompatibilitas)
        GetComponent<QuestObjectiveTrigger>()?.ForceTrigger();

        // 8. Event Inspector
        onCollected?.Invoke();

        // 9. Hapus sampah dari scene
        Destroy(gameObject);
    }

    private IEnumerator FadeOutPopup()
    {
        CanvasGroup canvasGroup = trashCollectedPopupUI.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = trashCollectedPopupUI.AddComponent<CanvasGroup>();

        canvasGroup.alpha = 1f;
        float elapsed = 0f;

        while (elapsed < popupFadeOutDuration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = 1f - Mathf.Clamp01(elapsed / popupFadeOutDuration);
            yield return null;
        }

        canvasGroup.alpha = 0f;
        trashCollectedPopupUI.SetActive(false);
        canvasGroup.alpha = 1f;
    }
}
