using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;

public class QTE1 : MonoBehaviour
{
    [Header("UI Elements")]
    public Image timerFillImage;
    public TMP_Text keyText;

    [Header("QTE Settings")]
    public KeyCode targetKey = KeyCode.E;
    public float timeLimit = 3.0f;

    [Header("Random Position Settings")]
    [SerializeField] private bool randomizePosition = true;
    [SerializeField] private Vector2 minPositionOffset = new Vector2(-400f, -200f);
    [SerializeField] private Vector2 maxPositionOffset = new Vector2(400f, 200f);

    [Header("Quest Objective")]
    [SerializeField] private string objectiveID = "press_interact";
    [SerializeField] private int amountToAdd = 1;

    [Header("Feedback Visual Settings")]
    [SerializeField] private Graphic keyGraphic;
    [SerializeField] private Color successColor = Color.cyan;
    [SerializeField] private float flashDuration = 0.15f;

    [Header("Events")]
    public UnityEvent onSuccess;
    public UnityEvent onFail;

    private float currentTimer;
    private bool isActive = false;
    private bool isCompleted = false;
    private RectTransform rectTransform;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    void Update()
    {
        if (!isActive || isCompleted) return;

        currentTimer -= Time.deltaTime;

        if (timerFillImage != null)
        {
            timerFillImage.fillAmount = Mathf.Clamp01(currentTimer / timeLimit);
        }

        if (Input.GetKeyDown(targetKey))
        {
            Success();
        }
        else if (currentTimer <= 0f)
        {
            Fail();
        }
    }

    public void InitQTE()
    {
        currentTimer = timeLimit;
        isCompleted = false;

        if (keyText != null)
        {
            keyText.text = targetKey.ToString();
        }

        if (timerFillImage != null)
        {
            timerFillImage.fillAmount = 1f;
        }

        if (randomizePosition)
        {
            RandomizeUIPosition();
        }

        isActive = true;
    }

    public void RandomizeUIPosition()
    {
        if (rectTransform == null) return;

        float randomX = Random.Range(minPositionOffset.x, maxPositionOffset.x);
        float randomY = Random.Range(minPositionOffset.y, maxPositionOffset.y);

        rectTransform.anchoredPosition = new Vector2(randomX, randomY);
    }

    private void Success()
    {
        isActive = false;
        Debug.Log("QTE Berhasil!");

        StartCoroutine(FlashSuccessAndProceed());
    }

    private System.Collections.IEnumerator FlashSuccessAndProceed()
    {
        yield return StartCoroutine(FlashSuccessRoutine());

        if (QuestController.Instance != null && !string.IsNullOrEmpty(objectiveID))
        {
            QuestController.Instance.ProgressObjective(objectiveID, amountToAdd);
        }

        onSuccess?.Invoke();

        bool needMoreProgress = false;
        if (QuestController.Instance != null && !string.IsNullOrEmpty(objectiveID))
        {
            foreach (var quest in QuestController.Instance.ActiveQuests)
            {
                if (quest == null) continue;

                var obj = quest.GetObjective(objectiveID);
                if (obj != null && !obj.IsComplete)
                {
                    needMoreProgress = true;
                    break;
                }
            }
        }

        if (needMoreProgress)
        {
            ResetQTE();
        }
        else
        {
            isCompleted = true;
            gameObject.SetActive(false);
        }
    }

    private void Fail()
    {
        isActive = false;
        Debug.Log("QTE Press Gagal!");

        onFail?.Invoke();
        ResetQTE();
    }

    public void ResetQTE()
    {
        currentTimer = timeLimit;
        isCompleted = false;

        if (timerFillImage != null)
        {
            timerFillImage.fillAmount = 1f;
        }

        if (randomizePosition)
        {
            RandomizeUIPosition();
        }

        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }

        isActive = true;
    }

    private System.Collections.IEnumerator FlashSuccessRoutine()
    {
        if (keyGraphic != null)
        {
            Color originalColor = keyGraphic.color;
            Vector3 originalScale = transform.localScale;

            keyGraphic.color = successColor;
            transform.localScale = originalScale * 1.2f;

            yield return new WaitForSeconds(flashDuration);

            keyGraphic.color = originalColor;
            transform.localScale = originalScale;
        }
    }
}