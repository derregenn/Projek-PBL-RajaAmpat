using UnityEngine;
using UnityEngine.UI;

public class SasiInteractable : MonoBehaviour
{
    [Header("Pengaturan Tombol")]
    public KeyCode interactKey = KeyCode.E;

    [Header("Referensi UI & QTE")]
    public GameObject uiPromptGroup;
    [SerializeField] private QTE2 qteToTrigger;

    [Header("Referensi Objek Patok Sasi")]
    public GameObject patokSasiObject;

    [Header("Quest Objective ID")]
    public string objectiveID = "sasi_arborek";

    [Header("Animasi (Opsional)")]
    public Animator playerAnimator;
    public string crouchAnimParameter = "isCrouching";

    private bool isPlayerInRange = false;
    private bool isCompleted = false;
    private bool qteStarted = false;

    private void Start()
    {
        if (uiPromptGroup != null) uiPromptGroup.SetActive(false);
        if (patokSasiObject != null) patokSasiObject.SetActive(false);
    }

    private void Update()
    {
        if (!isPlayerInRange || isCompleted || qteStarted) return;

        // Pemain harus menekan tombol E untuk memulai QTE
        if (Input.GetKeyDown(interactKey))
        {
            StartQTE();
        }
    }

    private void StartQTE()
    {
        if (qteToTrigger == null)
        {
            Debug.LogError("[SasiInteractable] Skrip QTE2 belum dimasukkan ke slot Inspector!");
            return;
        }

        qteStarted = true;
        if (uiPromptGroup != null) uiPromptGroup.SetActive(false); // Sembunyikan prompt E saat QTE jalan

        if (playerAnimator != null) playerAnimator.SetBool(crouchAnimParameter, true);

        // Amankan listener jika onSuccess terdeteksi
        if (qteToTrigger.onSuccess != null)
        {
            qteToTrigger.onSuccess.RemoveAllListeners();
            qteToTrigger.onSuccess.AddListener(CompletePlanting);
        }

        qteToTrigger.StartQTE();
    }

    private void CompletePlanting()
    {
        isCompleted = true;

        if (playerAnimator != null) playerAnimator.SetBool(crouchAnimParameter, false);
        if (patokSasiObject != null) patokSasiObject.SetActive(true);

        if (QuestController.Instance != null && !string.IsNullOrEmpty(objectiveID))
        {
            QuestController.Instance.ProgressObjective(objectiveID, 1);
        }

        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;
    }

    private void ResetInteraction()
    {
        qteStarted = false;
        if (playerAnimator != null) playerAnimator.SetBool(crouchAnimParameter, false);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (isCompleted) return;

        if (collision.CompareTag("Player"))
        {
            isPlayerInRange = true;
            if (uiPromptGroup != null && !qteStarted) uiPromptGroup.SetActive(true);
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            isPlayerInRange = false;
            if (uiPromptGroup != null) uiPromptGroup.SetActive(false);

            ResetInteraction();
        }
    }
}