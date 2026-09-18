using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;

public class QTE2 : MonoBehaviour
{
    [Header("UI Elements")]
    public Image timerFillImage;
    public TMP_Text keyText;

    [Header("Hold Settings")]
    public KeyCode targetKey = KeyCode.E;
    public float holdDuration = 2.0f;
    public bool decayWhenReleased = true;
    public float decaySpeed = 1.5f;

    [Header("Random Position Settings")]
    [SerializeField] private bool randomizePosition = true;
    [SerializeField] private Vector2 minPositionOffset = new Vector2(-400f, -200f);
    [SerializeField] private Vector2 maxPositionOffset = new Vector2(400f, 200f);

    [Header("Quest Objective")]
    [SerializeField] private string objectiveID = "hold_interact";
    [SerializeField] private int amountToAdd = 1;

    [Header("Events")]
    public UnityEvent onSuccess;
    public UnityEvent onFail;

    private float currentHoldTime = 0f;
    private bool isHolding = false;
    private bool isCompleted = false;
    private RectTransform rectTransform;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    void Start()
    {
        if (keyText != null)
        {
            keyText.text = targetKey.ToString();
        }

        if (timerFillImage != null)
        {
            timerFillImage.fillAmount = 0f;
        }

        if (randomizePosition)
        {
            RandomizeUIPosition();
        }
    }

    void Update()
    {
        if (isCompleted) return;

        if (Input.GetKey(targetKey))
        {
            isHolding = true;
            currentHoldTime += Time.deltaTime;
        }
        else
        {
            isHolding = false;

            if (decayWhenReleased)
            {
                currentHoldTime -= Time.deltaTime * decaySpeed;
            }
            else
            {
                currentHoldTime = 0f;
            }
        }

        currentHoldTime = Mathf.Clamp(currentHoldTime, 0f, holdDuration);

        if (timerFillImage != null)
        {
            timerFillImage.fillAmount = currentHoldTime / holdDuration;
        }

        if (currentHoldTime >= holdDuration)
        {
            Success();
        }
    }

    public void RandomizeUIPosition()
    {
        if (rectTransform == null) return;

        // Menghitung posisi acak baru di dalam area batas offset
        float randomX = Random.Range(minPositionOffset.x, maxPositionOffset.x);
        float randomY = Random.Range(minPositionOffset.y, maxPositionOffset.y);

        rectTransform.anchoredPosition = new Vector2(randomX, randomY);
    }

    private void Success()
    {
        Debug.Log("Hold 1x Selesai!");

        // Tambah progres quest (biasanya bernilai 1)
        if (QuestController.Instance != null)
        {
            QuestController.Instance.ProgressObjective(objectiveID, amountToAdd);
        }

        onSuccess?.Invoke();

        // Cek apakah objective quest masih butuh diselesaikan lagi
        bool needMoreProgress = false;

        if (QuestController.Instance != null)
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
            // Reset timer dan acak ulang koordinat posisi untuk giliran berikutnya
            ResetQTE();
        }
        else
        {
            // Jika seluruh target jumlah sudah terpenuhi (misal 3/3), matikan total UI QTE
            isCompleted = true;
            gameObject.SetActive(false);
        }
    }

    public void ResetQTE()
    {
        currentHoldTime = 0f;
        isHolding = false;
        isCompleted = false;

        if (timerFillImage != null)
        {
            timerFillImage.fillAmount = 0f;
        }

        // Pindah posisi ke titik acak baru tiap kali di-reset/berhasil 1x
        if (randomizePosition)
        {
            RandomizeUIPosition();
        }

        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }
    }
}