using System.Collections;
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
    [SerializeField] private float advanceDuration = 0.85f;

    [Header("Events Akhir QTE")]
    public UnityEvent onQTESuccess;
    public UnityEvent onQTEFail;

    [Header("Quest Progression Gate (Opsional)")]
    [SerializeField] private string requiredActiveObjectiveID = "";
    [SerializeField] private string requiredCompletedQuestID = "";

    private bool isDone = false;
    private bool qteRunning = false;
    private Transform playerTransform;
    private Rigidbody2D playerRigidbody;
    private RigidbodyConstraints2D originalConstraints;
    private bool hadRigidbody;
    private bool movementWasEnabled;
    private bool animatorWasEnabled;

    private Player playerScript;
    private SpriteRenderer playerSpriteRenderer;
    private Coroutine advanceCoroutine;

    public void Interact()
    {
        // QTE dimulai oleh collider, bukan oleh tombol interaksi.
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isDone || qteRunning || !other.CompareTag(playerTag)) return;
        if (!QuestProgressionGate.Passes(requiredActiveObjectiveID, string.Empty, requiredCompletedQuestID)) return;

        playerTransform = other.transform;
        playerRigidbody = other.attachedRigidbody;
        LockPlayerMovement();

        if (qteToTrigger != null)
        {
            // Membersihkan listener lama agar tidak dobel, lalu memasang listener baru
            qteToTrigger.onSuccess.RemoveAllListeners();
            qteToTrigger.onSuccess.AddListener(OnSuccess);

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

    private void LockPlayerMovement()
    {
        if (playerTransform != null)
        {
            playerScript = playerTransform.GetComponent<Player>();
            playerSpriteRenderer = playerTransform.GetComponentInChildren<SpriteRenderer>();
        }

        // Kunci input player dengan aman (tanpa mematikan komponen atau collider)
        if (playerScript != null)
        {
            playerScript.isMovementLocked = true;
        }

        // Jika movementToLock di-assign dan BUKAN Collider2D, baru nonaktifkan (mencegah collider player mati)
        if (movementToLock != null && !(movementToLock is Collider2D))
        {
            movementWasEnabled = movementToLock.enabled;
            movementToLock.enabled = false;
        }

        if (playerAnimator == null && playerTransform != null)
        {
            playerAnimator = playerTransform.GetComponentInChildren<Animator>();
        }

        // JANGAN mematikan Animator! Cukup set ke Idle agar tidak membeku kaku dalam pose aneh
        if (playerAnimator != null)
        {
            animatorWasEnabled = playerAnimator.enabled;
            playerAnimator.enabled = true; // Pastikan tetap aktif
            if (playerAnimator.HasState(0, Animator.StringToHash("Player_Idle")))
            {
                playerAnimator.Play("Player_Idle");
            }
        }

        hadRigidbody = playerRigidbody != null;
        if (hadRigidbody)
        {
            originalConstraints = playerRigidbody.constraints;
            playerRigidbody.linearVelocity = new Vector2(0f, playerRigidbody.linearVelocity.y);
            // Kunci rotasi saja, biarkan fisika & gravitasi tetap menempel di tanah
            playerRigidbody.constraints = RigidbodyConstraints2D.FreezeRotation;
        }
    }

    private void UnlockPlayerMovement()
    {
        if (hadRigidbody && playerRigidbody != null)
        {
            playerRigidbody.constraints = originalConstraints;
            playerRigidbody.linearVelocity = Vector2.zero;
        }

        if (movementToLock != null && !(movementToLock is Collider2D))
        {
            movementToLock.enabled = movementWasEnabled;
        }

        if (playerScript != null)
        {
            playerScript.isMovementLocked = false;
            playerScript.SetScriptedMovementActive(false);
        }

        if (playerAnimator != null && animatorWasEnabled)
        {
            playerAnimator.enabled = true;
            if (playerAnimator.HasState(0, Animator.StringToHash("Player_Idle")))
            {
                playerAnimator.Play("Player_Idle");
            }
        }

        qteRunning = false;
    }

    private void OnSuccess()
    {
        isDone = true;
        Debug.Log($"[StealthQTE] {gameObject.name} Berhasil dilewati!");

        if (playerTransform != null)
        {
            if (advanceCoroutine != null) StopCoroutine(advanceCoroutine);
            advanceCoroutine = StartCoroutine(AdvancePlayerSmoothly());
            return;
        }

        FinishSuccess();
    }

    private void AdvanceSuccessSetup()
    {
        if (playerScript != null)
        {
            playerScript.isMovementLocked = true;
            playerScript.SetScriptedMovementActive(true);
        }
        if (hadRigidbody && playerRigidbody != null)
        {
            // Pastikan FreezePositionX dari SequenceQTE dibersihkan agar MovePosition / langkah mengendap halus
            playerRigidbody.constraints = RigidbodyConstraints2D.FreezeRotation;
            playerRigidbody.linearVelocity = Vector2.zero;
        }
    }

    private IEnumerator AdvancePlayerSmoothly()
    {
        AdvanceSuccessSetup();
        Vector2 direction = advanceDirection.sqrMagnitude > 0f ? advanceDirection.normalized : Vector2.right;

        // Hadapkan sprite player ke arah langkah mengendap
        if (playerSpriteRenderer != null)
        {
            if (direction.x > 0.05f) playerSpriteRenderer.flipX = false;
            else if (direction.x < -0.05f) playerSpriteRenderer.flipX = true;
        }

        // Nyalakan animasi jalan/langkah mengendap agar tidak melayang kaku
        if (playerAnimator != null && playerAnimator.enabled)
        {
            if (playerAnimator.HasState(0, Animator.StringToHash("Player_Run")))
            {
                playerAnimator.Play("Player_Run");
            }
        }

        // Durasi melangkah yang smooth & organik (minimal 0.85s)
        float duration = Mathf.Max(advanceDuration, 0.85f);
        Vector3 startPosition = playerTransform.position;
        Vector3 targetPosition = startPosition + (Vector3)(direction * advanceDistance);
        float elapsed = 0f;
        WaitForFixedUpdate waitFixed = new WaitForFixedUpdate();

        while (elapsed < duration)
        {
            // Jaga lock selama langkah mengendap (mencegah SequenceQTE.FadeOutAndHide membuka lock lebih awal)
            if (playerScript != null) playerScript.isMovementLocked = true;
            if (playerRigidbody != null && (playerRigidbody.constraints & RigidbodyConstraints2D.FreezePositionX) != 0)
            {
                playerRigidbody.constraints = RigidbodyConstraints2D.FreezeRotation;
            }

            elapsed += Time.fixedDeltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);

            // Cosine Ease-in-Out S-curve: akselerasi halus di awal & deselerasi lembut di akhir
            float smoothT = 0.5f * (1f - Mathf.Cos(progress * Mathf.PI));

            Vector3 currentPos = Vector3.Lerp(startPosition, targetPosition, smoothT);

            if (playerRigidbody != null)
            {
                // Gunakan MovePosition di FixedUpdate agar fisik & kamera halus tanpa jitter/snapping
                playerRigidbody.MovePosition(new Vector2(currentPos.x, currentPos.y));
            }
            else
            {
                playerTransform.position = currentPos;
            }

            yield return waitFixed;
        }

        // Pastikan posisi akhir tercapai tepat pada target
        if (playerRigidbody != null)
        {
            playerRigidbody.MovePosition(new Vector2(targetPosition.x, targetPosition.y));
            playerRigidbody.linearVelocity = Vector2.zero;
        }
        else
        {
            playerTransform.position = targetPosition;
        }

        // Kembalikan ke animasi Idle secara mulus setelah langkah mengendap selesai
        if (playerAnimator != null && playerAnimator.enabled)
        {
            if (playerAnimator.HasState(0, Animator.StringToHash("Player_Idle")))
            {
                playerAnimator.Play("Player_Idle");
            }
        }

        // Beri jeda sepersekian detik agar animasi stabil sebelum kontrol dibuka
        yield return new WaitForSeconds(0.12f);

        advanceCoroutine = null;
        FinishSuccess();
    }

    private void FinishSuccess()
    {
        UnlockPlayerMovement();

        // Matikan collider area ini agar tidak tertrigger ulang
        Collider2D areaCollider = GetComponent<Collider2D>();
        if (areaCollider != null) areaCollider.enabled = false;

        // Beritahu Manager bahwa QTE ini berhasil
        onQTESuccess?.Invoke();
    }

    private void OnFail()
    {
        Debug.LogWarning($"[StealthQTE] {gameObject.name} Gagal!");
        if (advanceCoroutine != null)
        {
            StopCoroutine(advanceCoroutine);
            advanceCoroutine = null;
        }

        UnlockPlayerMovement();

        // Beritahu Manager bahwa QTE ini gagal
        onQTEFail?.Invoke();
    }

    public string GetPromptText()
    {
        if (isDone || !QuestProgressionGate.Passes(requiredActiveObjectiveID, string.Empty, requiredCompletedQuestID)) return "";
        return promptText;
    }

    /// <summary>
    /// Dipanggil oleh Manager jika player gagal total dan seluruh area harus diulang
    /// </summary>
    public void ResetArea()
    {
        isDone = false;
        if (advanceCoroutine != null)
        {
            StopCoroutine(advanceCoroutine);
            advanceCoroutine = null;
        }
        Collider2D areaCollider = GetComponent<Collider2D>();
        if (areaCollider != null) areaCollider.enabled = true;
    }
}
