using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;

public class QTE2 : MonoBehaviour
{
    [Header("UI Elements")]
    public Image holdFillImage;
    public TMP_Text keyText;
    public TMP_Text countText;

    [Header("QTE Settings")]
    public KeyCode targetKey = KeyCode.E;
    public float holdDuration = 1.5f;
    public int requiredHolds = 3;

    [Header("Failure Settings")]
    [Tooltip("Batas waktu total (detik) untuk menyelesaikan seluruh QTE. Set 0 jika tidak ingin pakai batas waktu.")]
    public float timeLimit = 7f;
    [Tooltip("Jika DICENTANG, melepas tombol E sebelum melingkar penuh akan LANGSUNG memicu OnFail. Jika UNCHECK, bar akan berkurang perlahan.")]
    public bool failOnRelease = false;

    [Header("Quest Objective")]
    [SerializeField] private string objectiveID = "perahu_friwen";

    [Header("Events")]
    public UnityEvent onSuccess;
    public UnityEvent onFail;

    private float currentHoldTimer = 0f;
    private int currentHoldCount = 0;
    private float timeRemaining = 0f;
    private bool isActive = false;
    private bool isCompleted = false;

    // Otomatis running saat GameObject di-SetActive(true)
    private void OnEnable()
    {
        InitQTE();
    }

    private void Update()
    {
        if (!isActive || isCompleted) return;

        // 1. Cek Batas Waktu Total QTE
        if (timeLimit > 0f)
        {
            timeRemaining -= Time.deltaTime;
            if (timeRemaining <= 0f)
            {
                FailQTE("Batas waktu QTE habis!");
                return;
            }
        }

        // 2. Logika Menahan Tombol
        if (Input.GetKey(targetKey))
        {
            currentHoldTimer += Time.deltaTime;

            if (holdFillImage != null)
            {
                holdFillImage.fillAmount = Mathf.Clamp01(currentHoldTimer / holdDuration);
            }

            if (currentHoldTimer >= holdDuration)
            {
                CompleteOneHold();
            }
        }
        else
        {
            // Jika tombol dilepas dan opsi failOnRelease diaktifkan
            if (failOnRelease && currentHoldTimer > 0f)
            {
                FailQTE("Tombol dilepas sebelum lingkaran penuh!");
                return;
            }

            // Jika failOnRelease mati, bar progress berkurang pelan-pelan
            if (currentHoldTimer > 0f)
            {
                currentHoldTimer -= Time.deltaTime * 2f;
                currentHoldTimer = Mathf.Max(currentHoldTimer, 0f);

                if (holdFillImage != null)
                {
                    holdFillImage.fillAmount = Mathf.Clamp01(currentHoldTimer / holdDuration);
                }
            }
        }
    }

    public void BeginQTE() => InitQTE();

    public void ResetQTE()
    {
        currentHoldCount = 0;
        currentHoldTimer = 0f;
        timeRemaining = timeLimit;
        isCompleted = false;

        if (keyText != null) keyText.text = targetKey.ToString();
        if (holdFillImage != null) holdFillImage.fillAmount = 0f;

        UpdateCountText();
    }

    public void InitQTE()
    {
        ResetQTE();
        isActive = true;
    }

    private void CompleteOneHold()
    {
        currentHoldCount++;
        currentHoldTimer = 0f;

        if (holdFillImage != null) holdFillImage.fillAmount = 0f;

        UpdateCountText();
        Debug.Log($"Hold {currentHoldCount}/{requiredHolds} Selesai!");

        if (currentHoldCount >= requiredHolds)
        {
            FinishQTE();
        }
    }

    private void UpdateCountText()
    {
        if (countText != null)
        {
            countText.text = $"{currentHoldCount}/{requiredHolds}";
        }
    }

    private void FinishQTE()
    {
        isActive = false;
        isCompleted = true;

        if (QuestController.Instance != null && !string.IsNullOrEmpty(objectiveID))
        {
            QuestController.Instance.ProgressObjective(objectiveID, 1);
        }

        onSuccess?.Invoke();
        gameObject.SetActive(false);
    }

    public void FailQTE(string reason = "")
    {
        if (!isActive || isCompleted) return;

        isActive = false;
        isCompleted = false;

        Debug.Log($"[QTE2] GAGAL! Alasan: {reason}");

        onFail?.Invoke();
        gameObject.SetActive(false);
    }
}