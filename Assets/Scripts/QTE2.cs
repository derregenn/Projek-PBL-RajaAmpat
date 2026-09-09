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
    public float holdDuration = 2.0f; // Durasi tahan sampai penuh (detik)
    public bool decayWhenReleased = true; // Apakah progress berkurang jika tombol dilepas?
    public float decaySpeed = 1.5f; // Kecepatan berkurang saat tombol dilepas

    [Header("Random Position Settings")]
    [SerializeField] private bool randomizePosition = true;
    [SerializeField] private Vector2 minPositionOffset = new Vector2(-400f, -200f); // Batas minimal X dan Y dari tengah layar
    [SerializeField] private Vector2 maxPositionOffset = new Vector2(400f, 200f);  // Batas maksimal X dan Y dari tengah layar

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

        // Acak posisi saat pertama kali aktif
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

    private void RandomizeUIPosition()
    {
        if (rectTransform == null) return;

        // Mengacak posisi lokal di dalam batas aman canvas/kamera
        float randomX = Random.Range(minPositionOffset.x, maxPositionOffset.x);
        float randomY = Random.Range(minPositionOffset.y, maxPositionOffset.y);

        rectTransform.anchoredPosition = new Vector2(randomX, randomY);
    }

    private void Success()
    {
        isCompleted = true;
        Debug.Log("Hold Selesai! Sukses!");

        if (QuestController.Instance != null)
        {
            QuestController.Instance.ProgressObjective(objectiveID, amountToAdd);
        }

        onSuccess?.Invoke();
        gameObject.SetActive(false);
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

        // Acak ulang posisi setiap kali QTE di-reset/dipanggil kembali
        if (randomizePosition)
        {
            RandomizeUIPosition();
        }

        gameObject.SetActive(true);
    }
}