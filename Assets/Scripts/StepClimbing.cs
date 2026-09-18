using UnityEngine;
using TMPro;

public class StepClimbing : MonoBehaviour
{
    [Header("Climb Positions")]
    [SerializeField] private Transform[] climbSteps; // Titik-titik posisi pemanjatan (Waypoint)
    [SerializeField] private Transform topPlatformPoint; // Titik akhir di atas tebing
    [SerializeField] private float moveSpeed = 5f;

    [Header("UI Prompt")]
    [SerializeField] private GameObject promptPanel;
    [SerializeField] private TextMeshProUGUI promptText;

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
        if (promptPanel != null) promptPanel.SetActive(false);
    }

    private void Update()
    {
        // Mode uji sementara: mulai pemanjatan langsung tanpa menunggu NPC/dialog.
        if (!isClimbingMode && Input.GetKeyDown(KeyCode.E))
        {
            StartClimbSequence();
            return;
        }

        if (!isClimbingMode) return;

        // Player menekan tombol E satu per satu saat sudah berhenti di step
        if (!isMovingToStep && Input.GetKeyDown(KeyCode.E))
        {
            AdvanceClimbStep();
        }

        // Pergerakan mulus antar step
        if (isMovingToStep)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);

            if (Vector3.Distance(transform.position, targetPosition) < 0.05f)
            {
                transform.position = targetPosition;
                isMovingToStep = false;

                // Tampilkan kembali prompt [E] untuk step berikutnya
                if (currentStepIndex < climbSteps.Length)
                {
                    ShowPrompt("E");
                }
                else
                {
                    FinishClimbing();
                }
            }
        }
    }

    // Dipanggil saat interaksi NPC/Dialog selesai
    public void StartClimbSequence()
    {
        isClimbingMode = true;
        currentStepIndex = 0;

        // Matikan fisik & gravitasi agar player melayang mengikuti waypoint
        if (rb != null) rb.bodyType = RigidbodyType2D.Kinematic;
        if (playerCollider != null) playerCollider.enabled = false;

        // Pindahkan player ke step pertama
        if (climbSteps.Length > 0)
        {
            targetPosition = climbSteps[0].position;
            isMovingToStep = true;
            HidePrompt();
        }
    }

    private void AdvanceClimbStep()
    {
        currentStepIndex++;
        HidePrompt();

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
    }

    private void FinishClimbing()
    {
        isClimbingMode = false;
        HidePrompt();

        // Kembalikan fisik & gravitasi ke normal
        if (rb != null) rb.bodyType = RigidbodyType2D.Dynamic;
        if (playerCollider != null) playerCollider.enabled = true;
    }

    private void ShowPrompt(string msg)
    {
        if (promptPanel != null) promptPanel.SetActive(true);
        if (promptText != null) promptText.text = msg;
    }

    private void HidePrompt()
    {
        if (promptPanel != null) promptPanel.SetActive(false);
    }
}