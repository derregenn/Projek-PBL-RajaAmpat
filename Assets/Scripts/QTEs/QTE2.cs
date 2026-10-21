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

    [Header("Quest Objective")]
    [SerializeField] private string objectiveID = "press_interact";

    [Header("Events")]
    public UnityEvent onSuccess;
    public UnityEvent onFail;

    private float currentHoldTimer = 0f;
    private int currentHoldCount = 0;
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
}