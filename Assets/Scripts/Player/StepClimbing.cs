using UnityEngine;

public class StepClimbing : MonoBehaviour
{
    [Header("Climb Positions")]
    [SerializeField] private Transform[] climbSteps; // Titik-titik posisi pemanjatan
    [SerializeField] private Transform topPlatformPoint; // Titik akhir di atas tebing
    [SerializeField] private float moveSpeed = 5f;

    [Header("Climb Sprites")]
    [SerializeField] private Sprite climbSprite1; // Drag pose panjat 1 (misal: tangan kanan atas)
    [SerializeField] private Sprite climbSprite2; // Drag pose panjat 2 (misal: tangan kiri atas)

    [Header("QTE Reference")]
    [SerializeField] private QTE1 qte1Script; // Drag GameObject QTE1 ke sini

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

        if (qte1Script != null) qte1Script.gameObject.SetActive(false);
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
                    FinishClimbing();
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
            FinishClimbing();
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
            FinishClimbing();
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

    private void FinishClimbing()
    {
        isClimbingMode = false;
        HideQTE();

        // Kembalikan fisik & gravitasi ke normal
        if (rb != null) rb.bodyType = RigidbodyType2D.Dynamic;
        if (playerCollider != null) playerCollider.enabled = true;

        // Kembalikan Sprite asli dan aktifkan Animator kembali
        if (spriteRenderer != null && originalSprite != null)
        {
            spriteRenderer.sprite = originalSprite;
        }

        if (anim != null)
        {
            anim.enabled = true;
            anim.Play("Idle"); // Sesuaikan nama state Idle kamu
        }
    }

    private void ShowQTEOnce()
    {
        if (qte1Script != null)
        {
            qte1Script.ResetQTE();
        }
    }

    private void HideQTE()
    {
        if (qte1Script != null)
        {
            qte1Script.gameObject.SetActive(false);
        }
    }
}