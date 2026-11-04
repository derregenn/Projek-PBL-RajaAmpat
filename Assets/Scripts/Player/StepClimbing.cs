using System.Collections;
using UnityEngine;

public class StepClimbing : MonoBehaviour
{
    [Header("Climb Positions")]
    [SerializeField] private Transform[] climbSteps; // Titik-titik posisi pemanjatan
    [SerializeField] private Transform[] climbDownSteps; // Titik-titik posisi turun
    [SerializeField] private Transform topPlatformPoint; // Titik akhir di atas tebing
    [SerializeField] private float moveSpeed = 2f;

    [Header("Climb Sprites")]
    [SerializeField] private Sprite climbSprite1; // Pose panjat 1
    [SerializeField] private Sprite climbSprite2; // Pose panjat 2

    [Header("QTE Reference")]
    [SerializeField] private QTE2 qte2Script; // Drag GameObject QTE2 ke sini

    [Header("Quest Settings")]
    [SerializeField] private string questObjectiveID = "piaynemo_hiking";

    private int currentStepIndex = 0;
    private bool isClimbingMode = false;
    private bool isClimbingSequenceActive = false;
    private bool isMovingToStep = false;
    private bool isClimbingDown = false;
    private Transform[] activeSteps;
    private Vector3 targetPosition;
    private Vector3 climbPosition;

    private Rigidbody2D rb;
    private Collider2D playerCollider;
    private SpriteRenderer spriteRenderer;
    private Animator anim;

    private Sprite originalSprite;
    private bool isUsingSprite1 = true;

    [Header("Platform Ground Settings")]
    [SerializeField] private Collider2D topPlatformCollider; // Drag BoxCollider2D dari TopPlatformGround

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        playerCollider = GetComponent<Collider2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        anim = GetComponent<Animator>();

        if (spriteRenderer != null)
        {
            originalSprite = spriteRenderer.sprite;
        }

        if (qte2Script != null) qte2Script.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (!isClimbingMode) return;

        // Pergerakan mulus antar step
        if (isMovingToStep)
        {
            climbPosition = Vector3.MoveTowards(climbPosition, targetPosition, moveSpeed * Time.deltaTime);
            transform.position = climbPosition;

            if (Vector3.Distance(climbPosition, targetPosition) < 0.05f)
            {
                climbPosition = targetPosition;
                transform.position = climbPosition;
                isMovingToStep = false;

                if (currentStepIndex < activeSteps.Length)
                {
                    ShowQTEOnce();
                }
                else
                {
                    // Jalankan sekuens penutup (Teleport & ScreenFader) saat semua step selesai
                    StartCoroutine(FinishClimbingRoutine());
                }
            }
        }
    }

    private void LateUpdate()
    {
        // Kunci sumbu horizontal setelah semua input gerak diproses.
        if (isClimbingSequenceActive)
        {
            Vector3 position = transform.position;
            position.x = climbPosition.x;
            transform.position = position;
        }
    }

    public void StartClimbSequence()
    {
        StartClimb(climbSteps, false);
    }

    public void StartClimbDownSequence()
    {
        StartClimb(climbDownSteps, true);
    }

    private void StartClimb(Transform[] steps, bool climbingDown)
    {
        isClimbingMode = true;
        isClimbingSequenceActive = true;
        climbPosition = transform.position;
        isClimbingDown = climbingDown;
        activeSteps = steps;
        currentStepIndex = 0;
        isUsingSprite1 = true;

        // Matikan komponen Animator agar tidak menimpa penggantian Sprite manual
        if (anim != null) anim.enabled = false;

        // Matikan fisik & gravitasi
        if (rb != null) rb.bodyType = RigidbodyType2D.Kinematic;
        if (playerCollider != null) playerCollider.enabled = false;

        if (activeSteps != null && activeSteps.Length > 0)
        {
            UpdateClimbSprite();
            targetPosition = activeSteps[0].position;
            isMovingToStep = true;
            HideQTE();
        }
        else
        {
            StartCoroutine(FinishClimbingRoutine());
        }

        // Matikan collider platform atas agar tidak menabrak player saat naik
        if (topPlatformCollider != null)
        {
            topPlatformCollider.enabled = false;
        }
    }

    public void OnQTESuccess()
    {
        if (isClimbingMode && !isMovingToStep)
        {
            AdvanceClimbStep();
        }
    }

    private void AdvanceClimbStep()
    {
        currentStepIndex++;
        HideQTE();

        // Berganti pose sprite tiap step
        isUsingSprite1 = !isUsingSprite1;
        UpdateClimbSprite();

        if (currentStepIndex < activeSteps.Length)
        {
            targetPosition = activeSteps[currentStepIndex].position;
            isMovingToStep = true;
        }
        else if (!isClimbingDown && topPlatformPoint != null)
        {
            targetPosition = topPlatformPoint.position;
            isMovingToStep = true;
        }
        else
        {
            StartCoroutine(FinishClimbingRoutine());
        }
    }

    private void UpdateClimbSprite()
    {
        if (spriteRenderer == null) return;

        if (isUsingSprite1 && climbSprite1 != null)
        {
            spriteRenderer.sprite = climbSprite1;
        }
        else if (!isUsingSprite1 && climbSprite2 != null)
        {
            spriteRenderer.sprite = climbSprite2;
        }
    }

    private IEnumerator FinishClimbingRoutine()
    {
        isClimbingMode = false;
        HideQTE();

        // 1. Transisi Layar Hitam (Fade Out)
        if (ScreenFader.Instance != null)
        {
            yield return ScreenFader.Instance.FadeOutRoutine();
        }

        // 2. Pindahkan Posisi ke Puncak Bukit jika ada
        if (!isClimbingDown && topPlatformPoint != null)
        {
            climbPosition = topPlatformPoint.position;
            transform.position = climbPosition;
        }

        // TAMBAHKAN BARIS INI: Nyalakan collider lantai agar player bisa berdiri
        if (topPlatformCollider != null)
        {
            topPlatformCollider.enabled = true;
        }

        // 3. Laporkan Progres Quest 2 (Cliff Hiking)
        if (!isClimbingDown && QuestController.Instance != null && !string.IsNullOrEmpty(questObjectiveID))
        {
            QuestController.Instance.ProgressObjective(questObjectiveID);
        }

        // 4. Kembalikan fisik & gravitasi ke normal
        if (rb != null) rb.bodyType = RigidbodyType2D.Dynamic;
        if (playerCollider != null) playerCollider.enabled = true;

        // 5. Kembalikan Sprite asli dan aktifkan Animator
        if (spriteRenderer != null && originalSprite != null)
        {
            spriteRenderer.sprite = originalSprite;
        }

        if (anim != null)
        {
            anim.enabled = true;
            int idleStateHash = Animator.StringToHash("Idle");
            if (anim.runtimeAnimatorController != null && anim.layerCount > 0 && anim.HasState(0, idleStateHash))
            {
                anim.Play(idleStateHash, 0);
            }
        }

        yield return new WaitForSeconds(0.2f);

        // 6. Layar Terang Kembali (Fade In)
        if (ScreenFader.Instance != null)
        {
            yield return ScreenFader.Instance.FadeInRoutine();
        }

        isClimbingSequenceActive = false;
    }

    private void ShowQTEOnce()
    {
        if (qte2Script != null)
        {
            // TAMBAHKAN BARIS INI: Nyalakan dulu GameObject-nya sebelum di-reset
            qte2Script.gameObject.SetActive(true);

            qte2Script.ResetQTE();
        }
    }

    private void HideQTE()
    {
        if (qte2Script != null)
        {
            qte2Script.gameObject.SetActive(false);
        }
    }
}