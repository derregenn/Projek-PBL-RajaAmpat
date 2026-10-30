using UnityEngine;
using UnityEngine.Events;

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

    [Header("Event Transisi Layar")]
    [Tooltip("Dipanggil tepat setelah QTE tancap sasi sukses dilakukan")]
    public UnityEvent onSasiCompleted;

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
                qteToTrigger.gameObject.SetActive(true);

                qteToTrigger.onSuccess.RemoveAllListeners();
                qteToTrigger.onSuccess.AddListener(OnQTESuccess);

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

        // JALANKAN TRANSISI LAYAR DI SINI (SETELAH QTE TANCAP SASI SUKSES)
        onSasiCompleted?.Invoke();
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
            if (playerAnimator.GetBool(crouchAnimParameter) != isCrouching)
            {
                playerAnimator.SetBool(crouchAnimParameter, isCrouching);
            }
        }
    }
}