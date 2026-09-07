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

    [Header("Events")]
    public UnityEvent onSuccess;
    public UnityEvent onFail;

    private float currentHoldTime = 0f;
    private bool isHolding = false;
    private bool isCompleted = false;

    void Start()
    {
        // Inisialisasi awal
        if (keyText != null)
        {
            keyText.text = targetKey.ToString();
        }

        if (timerFillImage != null)
        {
            timerFillImage.fillAmount = 0f;
        }
    }

    void Update()
    {
        if (isCompleted) return;

        // Cek penekanan tombol
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
                // Progress berkurang bertahap jika tombol dilepas
                currentHoldTime -= Time.deltaTime * decaySpeed;
            }
            else
            {
                // Langsung reset ke nol jika tombol dilepas
                currentHoldTime = 0f;
            }
        }

        // Batasi nilai agar tetap di rentang 0 sampai holdDuration
        currentHoldTime = Mathf.Clamp(currentHoldTime, 0f, holdDuration);

        // Perbarui UI Fill melingkar
        if (timerFillImage != null)
        {
            timerFillImage.fillAmount = currentHoldTime / holdDuration;
        }

        // Cek kondisi sukses jika sudah terisi 100%
        if (currentHoldTime >= holdDuration)
        {
            Success();
        }
    }

    private void Success()
    {
        isCompleted = true;
        Debug.Log("Hold Selesai! Sukses!");
        onSuccess?.Invoke();
        gameObject.SetActive(false);
    }

    // Panggil fungsi ini jika ingin memunculkan dan mereset QTE dari awal
    public void ResetQTE()
    {
        currentHoldTime = 0f;
        isHolding = false;
        isCompleted = false;

        if (timerFillImage != null)
        {
            timerFillImage.fillAmount = 0f;
        }

        gameObject.SetActive(true);
    }
}