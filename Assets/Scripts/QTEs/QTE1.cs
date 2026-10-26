using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;
using System.Collections;

public class QTE1 : MonoBehaviour
{
    [Header("UI Elements")]
    public Image timerFillImage;
    public TMP_Text keyText;

    [Header("QTE Settings")]
    public KeyCode targetKey = KeyCode.E;
    public float timeLimit = 3.0f; // Batas waktu PER pencetan E (atur di Inspector)
    public int totalPressesRequired = 3; // Target jumlah total pencetan "E" sampai sukses penuh

    [Header("Random Position Settings")]
    [SerializeField] private bool randomizePosition = true;
    [SerializeField] private Vector2 minPositionOffset = new Vector2(-400f, -200f);
    [SerializeField] private Vector2 maxPositionOffset = new Vector2(400f, 200f);

    [Header("Quest Objective")]
    [SerializeField] private string objectiveID = "press_interact";
    [SerializeField] private int amountToAddPerPress = 1;

    [Header("Feedback Visual Settings")]
    [SerializeField] private Graphic keyGraphic;
    [SerializeField] private Color successColor = Color.cyan;
    [SerializeField] private float flashDuration = 0.15f;

    [Header("Events")]
    public UnityEvent onSuccess; // Dipanggil 1x HANYA SETELAH target pencetan terpenuhi semua
    public UnityEvent onFail;    // Dipanggil kalau waktu habis di salah satu pencetan

    private float currentTimer;
    private int currentPressCount = 0;
    private bool isActive = false;
    private bool isCompleted = false;
    private RectTransform rectTransform;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    void OnEnable()
    {
        InitQTE();
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
            OnSinglePressSuccess();
        }
        else if (currentTimer <= 0f)
        {
            Fail();
        }
    }

    public void InitQTE()
    {
        currentPressCount = 0;
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

    private void OnSinglePressSuccess()
    {
        isActive = false;
        StartCoroutine(ProcessPressRoutine());
    }

    private IEnumerator ProcessPressRoutine()
    {
        yield return StartCoroutine(FlashSuccessRoutine());

        currentPressCount++;

        // Update progress quest di tiap pencetan
        if (QuestController.Instance != null && !string.IsNullOrEmpty(objectiveID))
        {
            QuestController.Instance.ProgressObjective(objectiveID, amountToAddPerPress);
        }

        // CEK: Apakah jumlah pencetan sudah mencapai target total?
        if (currentPressCount >= totalPressesRequired)
        {
            isCompleted = true;
            Debug.Log($"[QTE1] Sukses Penuh! Terpencet {currentPressCount}/{totalPressesRequired} kali.");
            
            onSuccess?.Invoke(); // Memanggil FaderTrigger.PlaySuccessSequence()
            gameObject.SetActive(false);
        }
        else
        {
            Debug.Log($"[QTE1] Pencetan ke-{currentPressCount}/{totalPressesRequired} Berhasil. Acak posisi & reset timer!");
            ResetForNextPress();
        }
    }

    private void ResetForNextPress()
    {
        currentTimer = timeLimit;

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

    private void Fail()
    {
        isActive = false;
        currentPressCount = 0;
        Debug.Log("[QTE1] Gagal (Waktu Habis)!");

        onFail?.Invoke(); // Memanggil FaderTrigger.PlayFailSequence()
    }

    private IEnumerator FlashSuccessRoutine()
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