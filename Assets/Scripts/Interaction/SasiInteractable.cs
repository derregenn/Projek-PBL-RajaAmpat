using UnityEngine;

public class SasiInteractable : MonoBehaviour
{
    [Header("Referensi QTE & UI")]
    [SerializeField] private QTE2 qteToTrigger;
    public GameObject uiPromptGroup;

    [Header("Referensi Objek Patok Sasi")]
    public GameObject patokSasiObject;

    [Header("Animasi Karakter (Opsional)")]
    public Animator playerAnimator;
    public string crouchAnimParameter = "isCrouching";

    private bool isPlayerInRange = false;
    private bool isCompleted = false;

    private void Start()
    {
        if (uiPromptGroup != null) uiPromptGroup.SetActive(false);
        if (patokSasiObject != null) patokSasiObject.SetActive(false);
        if (qteToTrigger != null) qteToTrigger.gameObject.SetActive(false);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (isCompleted) return;

        if (collision.CompareTag("Player"))
        {
            isPlayerInRange = true;

            if (playerAnimator == null)
            {
                playerAnimator = collision.GetComponent<Animator>();
            }

            if (uiPromptGroup != null) uiPromptGroup.SetActive(true);

            if (qteToTrigger != null)
            {
                // Hubungkan listener sukses dan gagal
                qteToTrigger.onSuccess.RemoveAllListeners();
                qteToTrigger.onSuccess.AddListener(OnQTESuccess);

                // Tambahkan listener gagal jika script QTE2 memiliki event onFailure
                // qteToTrigger.onFailure.RemoveAllListeners();
                // qteToTrigger.onFailure.AddListener(OnQTEFailed);

                // Jalankan animasi jongkok sekali saja di awal QTE
                SetCrouchAnimation(true);
                qteToTrigger.BeginQTE();
            }
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            isPlayerInRange = false;

            if (uiPromptGroup != null) uiPromptGroup.SetActive(false);
            if (qteToTrigger != null) qteToTrigger.gameObject.SetActive(false);

            SetCrouchAnimation(false);
        }
    }

    private void OnQTESuccess()
    {
        isCompleted = true;

        SetCrouchAnimation(false);

        if (qteToTrigger != null) qteToTrigger.gameObject.SetActive(false);
        if (uiPromptGroup != null) uiPromptGroup.SetActive(false);
        if (patokSasiObject != null) patokSasiObject.SetActive(true);

        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;
    }

    private void OnQTEFailed()
    {
        SetCrouchAnimation(false);
        if (uiPromptGroup != null) uiPromptGroup.SetActive(true);
    }

    private void SetCrouchAnimation(bool isCrouching)
    {
        if (playerAnimator != null && !string.IsNullOrEmpty(crouchAnimParameter))
        {
            playerAnimator.SetBool(crouchAnimParameter, isCrouching);
        }
    }
}