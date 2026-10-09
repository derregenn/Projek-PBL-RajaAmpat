using System.Collections;
using UnityEngine;

public class StepClimbing : MonoBehaviour
{
    [Header("Climb Positions")]
    [SerializeField] private Transform[] climbSteps; // Titik-titik posisi pemanjatan
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
    private bool isMovingToStep = false;
    private Vector3 targetPosition;

    private Rigidbody2D rb;
    private Collider2D playerCollider;
    private SpriteRenderer spriteRenderer;
    private Animator anim;

    private Sprite originalSprite;
    private bool isUsingSprite1 = true;

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
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);

            if (Vector3.Distance(transform.position, targetPosition) < 0.05f)
            {
                transform.position = targetPosition;
                isMovingToStep = false;

                if (currentStepIndex < climbSteps.Length)
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

    public void StartClimbSequence()
    {
        isClimbingMode = true;
        currentStepIndex = 0;
        isUsingSprite1 = true;

        // Matikan komponen Animator agar tidak menimpa penggantian Sprite manual
        if (anim != null) anim.enabled = false;

        // Matikan fisik & gravitasi
        if (rb != null) rb.bodyType = RigidbodyType2D.Kinematic;
        if (playerCollider != null) playerCollider.enabled = false;

        if (climbSteps != null && climbSteps.Length > 0)
        {
            UpdateClimbSprite();
            targetPosition = climbSteps[0].position;
            isMovingToStep = true;
            HideQTE();
        }
        else
        {
            StartCoroutine(FinishClimbingRoutine());
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

        if (currentStepIndex < climbSteps.Length)
        {
            targetPosition = climbSteps[currentStepIndex].position;
            isMovingToStep = true;
        }
        else if (topPlatformPoint != null)
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
        if (topPlatformPoint != null)
        {
            transform.position = topPlatformPoint.position;
        }

        // 3. Laporkan Progres Quest 2 (Cliff Hiking)
        if (QuestController.Instance != null && !string.IsNullOrEmpty(questObjectiveID))
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
            anim.Play("Idle");
        }

        yield return new WaitForSeconds(0.2f);

        // 6. Layar Terang Kembali (Fade In)
        if (ScreenFader.Instance != null)
        {
            yield return ScreenFader.Instance.FadeInRoutine();
        }
    }

    private void ShowQTEOnce()
    {
        if (qte2Script != null)
        {
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