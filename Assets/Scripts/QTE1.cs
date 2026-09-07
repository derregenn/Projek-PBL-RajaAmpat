using UnityEngine;
using UnityEngine.UI;
using TMPro; // Gunakan UnityEngine.UI jika memakai Text standar
using UnityEngine.Events;

public class QuickTimeEvent : MonoBehaviour
{
    [Header("UI Elements")]
    public Image timerFillImage;
    public TMP_Text keyText;

    [Header("QTE Settings")]
    public KeyCode targetKey = KeyCode.E;
    public float timeLimit = 3.0f; // Durasi waktu untuk menekan tombol (detik)

    [Header("Events")]
    public UnityEvent onSuccess;
    public UnityEvent onFail;

    private float currentTimer;
    private bool isActive = false;

    void Start()
    {
        // Contoh: langsung memicu QTE saat game mulai (bisa dipanggil dari skrip lain)
        StartQTE(targetKey, timeLimit);
    }

    void Update()
    {
        if (!isActive) return;

        // Kurangi waktu
        currentTimer -= Time.deltaTime;
        timerFillImage.fillAmount = currentTimer / timeLimit;

        // Cek input tombol
        if (Input.GetKeyDown(targetKey))
        {
            Success();
        }
        else if (currentTimer <= 0f)
        {
            Fail();
        }
    }

    public void StartQTE(KeyCode key, float duration)
    {
        targetKey = key;
        timeLimit = duration;
        currentTimer = duration;

        if (keyText != null)
        {
            keyText.text = key.ToString();
        }

        if (timerFillImage != null)
        {
            timerFillImage.fillAmount = 1f;
        }

        gameObject.SetActive(true);
        isActive = true;
    }

    private void Success()
    {
        isActive = false;
        gameObject.SetActive(false);
        Debug.Log("QTE Berhasil!");
        onSuccess?.Invoke();
    }

    private void Fail()
    {
        isActive = false;
        gameObject.SetActive(false);
        Debug.Log("QTE Gagal!");
        onFail?.Invoke();
    }
}