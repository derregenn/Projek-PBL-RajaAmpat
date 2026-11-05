using UnityEngine;
using UnityEngine.Events;

public class StealthQTEInteractable : MonoBehaviour, IInteractable
{
    [Header("Behavior")]
    [SerializeField] private string promptText = "Maju Perlahan";
    [SerializeField] private SequenceQTE qteToTrigger; // Referensi QTE sama seperti di BiotaInteractable
    [SerializeField] private string playerTag = "Player";

    [Header("Gerakan Player")]
    [SerializeField] private Behaviour movementToLock;
    [SerializeField] private Animator playerAnimator;
    [SerializeField] private Vector2 advanceDirection = Vector2.right;
    [SerializeField] private float advanceDistance = 1f;
    [SerializeField] private float advanceDuration = 0.5f;

    [Header("Events Akhir QTE")]
    public UnityEvent onQTESuccess;
    public UnityEvent onQTEFail;

    private bool isDone = false;
    private bool qteRunning = false;
    private Transform playerTransform;
    private Rigidbody2D playerRigidbody;
    private RigidbodyConstraints2D originalConstraints;
    private bool hadRigidbody;
    private bool movementWasEnabled;
    private bool animatorWasEnabled;

    public void Interact()
    {
        // QTE dimulai oleh collider, bukan oleh tombol interaksi.
    }

    [System.Obsolete]
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isDone || qteRunning || !other.CompareTag(playerTag)) return;

        playerTransform = other.transform;
        playerRigidbody = other.attachedRigidbody;
        LockPlayerMovement();

        if (qteToTrigger != null)
        {
            // Membersihkan listener lama agar tidak dobel, lalu memasang listener baru
            qteToTrigger.onSuccess.RemoveAllListeners();
            qteToTrigger.onSuccess.AddListener(OnSuccess);

            // Menambahkan listener untuk kegagalan (Sesuaikan 'onFail' dengan variabel di SequenceQTE kamu)
            // Jika variabelnya bernama lain seperti 'onFailed', ubah kodenya di bawah ini:
            if (qteToTrigger.onFail != null)
            {
                qteToTrigger.onFail.RemoveAllListeners();
                qteToTrigger.onFail.AddListener(OnFail);
            }

            qteRunning = true;
            qteToTrigger.StartQTE();
            return;
        }

        // Jika tidak ada QTE terpasang, langsung anggap sukses (untuk testing)
        OnSuccess();
    }

    [System.Obsolete]
    private void LockPlayerMovement()
    {
        if (movementToLock != null)
        {
            movementWasEnabled = movementToLock.enabled;
            movementToLock.enabled = false;
        }

        if (playerAnimator == null && playerTransform != null)
            playerAnimator = playerTransform.GetComponentInChildren<Animator>();

        if (playerAnimator != null)
        {
            animatorWasEnabled = playerAnimator.enabled;
            playerAnimator.enabled = false;
        }

        hadRigidbody = playerRigidbody != null;
        if (hadRigidbody)
        {
            originalConstraints = playerRigidbody.constraints;
            playerRigidbody.velocity = Vector2.zero;
            playerRigidbody.constraints = RigidbodyConstraints2D.FreezeAll;
        }
    }

    private void UnlockPlayerMovement()
    {
        if (hadRigidbody && playerRigidbody != null)
            playerRigidbody.constraints = originalConstraints;

        if (movementToLock != null)
            movementToLock.enabled = movementWasEnabled;

        if (playerAnimator != null)
            playerAnimator.enabled = animatorWasEnabled;

        qteRunning = false;
    }

    private void OnSuccess()
    {
        isDone = true;
        Debug.Log($"[StealthQTE] {gameObject.name} Berhasil dilewati!");

        if (playerTransform != null && advanceDuration > 0f)
        {
            StartCoroutine(AdvancePlayerSmoothly());
            return;
        }

        if (playerTransform != null)
        {
            Vector2 direction = advanceDirection.sqrMagnitude > 0f ? advanceDirection.normalized : Vector2.right;
            playerTransform.position += (Vector3)(direction * advanceDistance);
        }

        FinishSuccess();
    }

    private System.Collections.IEnumerator AdvancePlayerSmoothly()
    {
        Vector2 direction = advanceDirection.sqrMagnitude > 0f ? advanceDirection.normalized : Vector2.right;
        Vector3 startPosition = playerTransform.position;
        Vector3 targetPosition = startPosition + (Vector3)(direction * advanceDistance);
        float elapsed = 0f;

        while (elapsed < advanceDuration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / advanceDuration);
            playerTransform.position = Vector3.Lerp(startPosition, targetPosition, Mathf.SmoothStep(0f, 1f, progress));
            yield return null;
        }

        playerTransform.position = targetPosition;
        FinishSuccess();
    }

    private void FinishSuccess()
    {
        UnlockPlayerMovement();

        // Matikan collider area ini agar prompt "E" hilang setelah sukses
        Collider2D areaCollider = GetComponent<Collider2D>();
        if (areaCollider != null) areaCollider.enabled = false;

        // Beritahu Manager bahwa QTE ini berhasil
        onQTESuccess?.Invoke();
    }

    private void OnFail()
    {
        Debug.LogWarning($"[StealthQTE] {gameObject.name} Gagal!");
        UnlockPlayerMovement();

        // Beritahu Manager bahwa QTE ini gagal
        onQTEFail?.Invoke();
    }

    public string GetPromptText() => isDone ? "" : promptText;

    /// <summary>
    /// Dipanggil oleh Manager jika player gagal total dan seluruh area harus diulang
    /// </summary>
    public void ResetArea()
    {
        isDone = false;
        Collider2D areaCollider = GetComponent<Collider2D>();
        if (areaCollider != null) areaCollider.enabled = true;
    }
}