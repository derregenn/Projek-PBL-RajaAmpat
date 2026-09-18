using UnityEngine;

public class StepClimbing : MonoBehaviour
{
    [Header("Climb Positions")]
    [SerializeField] private Transform[] climbSteps; // Titik-titik posisi pemanjatan
    [SerializeField] private Transform topPlatformPoint; // Titik akhir di atas tebing
    [SerializeField] private float moveSpeed = 5f;

    [Header("QTE Reference")]
    [SerializeField] private QTE1 qte1Script; // Drag GameObject QTE1 ke sini

    private int currentStepIndex = 0;
    private bool isClimbingMode = false;
    private bool isMovingToStep = false;
    private Vector3 targetPosition;
    private Rigidbody2D rb;
    private Collider2D playerCollider;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        playerCollider = GetComponent<Collider2D>();
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

                // Jika belum sampai puncak, tampilkan QTE1 sekali untuk step berikutnya
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

        // Matikan fisik & gravitasi
        if (rb != null) rb.bodyType = RigidbodyType2D.Kinematic;
        if (playerCollider != null) playerCollider.enabled = false;

        // Pindahkan player ke step pertama
        if (climbSteps != null && climbSteps.Length > 0)
        {
            targetPosition = climbSteps[0].position;
            isMovingToStep = true;
            HideQTE();
        }
        else
        {
            FinishClimbing();
        }
    }

    // Dipanggil saat Player BERHASIL menekan QTE 1x
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

    private void FinishClimbing()
    {
        isClimbingMode = false;
        HideQTE();

        // Kembalikan fisik & gravitasi ke normal
        if (rb != null) rb.bodyType = RigidbodyType2D.Dynamic;
        if (playerCollider != null) playerCollider.enabled = true;
    }

    private void ShowQTEOnce()
    {
        if (qte1Script != null)
        {
            qte1Script.ResetQTE(); // Aktifkan QTE1 sekali tekan
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