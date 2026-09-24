using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;
using System.Collections;
using System.Collections.Generic;

public class SequenceQTE : MonoBehaviour
{
    [Header("Player Control Integration")]
    [SerializeField] private MonoBehaviour playerMovementScript; // Drag skrip Player / PlayerController ke sini

    [Header("UI References")]
    [SerializeField] private RectTransform qtePanelTransform;
    [SerializeField] private Slider timerSlider;
    [SerializeField] private TextMeshProUGUI[] keyTexts;
    [SerializeField] private Image[] keyBoxes;

    [Header("QTE Settings")]
    [SerializeField] private float timeLimit = 5f;
    [SerializeField] private float timePenalty = 1f;
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color activeColor = Color.yellow;
    [SerializeField] private Color successColor = Color.green;
    [SerializeField] private Color failColor = Color.red;

    [Header("UI Shake Settings")]
    [SerializeField] private float shakeDuration = 0.2f;
    [SerializeField] private float shakeMagnitude = 10f;

    [Header("Possible Keys Pool")]
    [SerializeField]
    private KeyCode[] possibleKeys = new KeyCode[]
    {
        KeyCode.A, KeyCode.W, KeyCode.S, KeyCode.D, KeyCode.Z, KeyCode.X, KeyCode.E, KeyCode.F
    };

    [Header("Events")]
    public UnityEvent onSuccess;
    public UnityEvent onFail;

    private List<KeyCode> currentSequence = new List<KeyCode>();
    private int currentIndex = 0;
    private float timer = 0f;
    private bool isActive = false;
    private Vector3 originalPanelPosition;

    private void Awake()
    {
        if (qtePanelTransform != null)
        {
            originalPanelPosition = qtePanelTransform.anchoredPosition;
            qtePanelTransform.gameObject.SetActive(false);
        }
    }

    private void Update()
    {
        if (!isActive) return;

        timer -= Time.deltaTime;
        if (timerSlider != null) timerSlider.value = timer / timeLimit;

        if (timer <= 0f)
        {
            FailQTE();
            return;
        }

        if (Input.anyKeyDown)
        {
            KeyCode expectedKey = currentSequence[currentIndex];

            if (Input.GetKeyDown(expectedKey))
            {
                HighlightBox(currentIndex, successColor);
                currentIndex++;

                if (currentIndex >= currentSequence.Count)
                {
                    SuccessQTE();
                }
                else
                {
                    HighlightBox(currentIndex, activeColor);
                }
            }
            else
            {
                foreach (KeyCode key in possibleKeys)
                {
                    if (Input.GetKeyDown(key) && key != expectedKey)
                    {
                        ApplyPenalty();
                        break;
                    }
                }
            }
        }
    }

    private void ApplyPenalty()
    {
        timer -= timePenalty;
        if (timerSlider != null) timerSlider.value = Mathf.Max(0, timer / timeLimit);

        HighlightBox(currentIndex, failColor);

        if (qtePanelTransform != null)
        {
            StopAllCoroutines();
            StartCoroutine(ShakePanelRoutine());
        }

        if (timer <= 0f)
        {
            FailQTE();
        }
    }

    private IEnumerator ShakePanelRoutine()
    {
        float elapsed = 0f;

        while (elapsed < shakeDuration)
        {
            float x = Random.Range(-1f, 1f) * shakeMagnitude;
            float y = Random.Range(-1f, 1f) * shakeMagnitude;

            qtePanelTransform.anchoredPosition = new Vector2(originalPanelPosition.x + x, originalPanelPosition.y + y);

            elapsed += Time.deltaTime;
            yield return null;
        }

        qtePanelTransform.anchoredPosition = originalPanelPosition;

        if (isActive && currentIndex < keyBoxes.Length)
        {
            HighlightBox(currentIndex, activeColor);
        }
    }

    [ContextMenu("Start QTE Test")]
    public void StartQTE()
    {
        // 1. Matikan kontrol gerak Player
        SetPlayerControl(false);

        currentSequence.Clear();
        currentIndex = 0;
        timer = timeLimit;
        isActive = true;

        if (qtePanelTransform != null)
        {
            qtePanelTransform.anchoredPosition = originalPanelPosition;
            qtePanelTransform.gameObject.SetActive(true);
        }

        for (int i = 0; i < 4; i++)
        {
            KeyCode randomKey = possibleKeys[Random.Range(0, possibleKeys.Length)];
            currentSequence.Add(randomKey);

            if (i < keyTexts.Length && keyTexts[i] != null)
            {
                keyTexts[i].text = randomKey.ToString().ToUpper();
            }

            HighlightBox(i, normalColor);
        }

        HighlightBox(0, activeColor);
    }

    private void HighlightBox(int index, Color color)
    {
        if (keyBoxes != null && index < keyBoxes.Length && keyBoxes[index] != null)
        {
            keyBoxes[index].color = color;
        }
    }

    private void SuccessQTE()
    {
        isActive = false;
        Debug.Log("QTE BERHASIL!");
        onSuccess?.Invoke();
        Invoke(nameof(HidePanel), 0.4f);
    }

    private void FailQTE()
    {
        isActive = false;
        Debug.Log("QTE GAGAL!");
        onFail?.Invoke();
        Invoke(nameof(HidePanel), 0.4f);
    }

    private void HidePanel()
    {
        if (qtePanelTransform != null) qtePanelTransform.gameObject.SetActive(false);

        // 2. Aktifkan kembali kontrol gerak Player
        SetPlayerControl(true);
    }

    private void SetPlayerControl(bool enable)
    {
        // Jika skrip di-drag manual di Inspector
        if (playerMovementScript != null)
        {
            playerMovementScript.enabled = enable;
        }
        else
        {
            // Mencari skrip Player (Player.cs) di scene jika belum di-drag
            Player player = FindFirstObjectByType<Player>();
            if (player != null)
            {
                player.enabled = enable;
            }
        }
    }
}