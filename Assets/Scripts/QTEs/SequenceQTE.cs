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
    [SerializeField] private CanvasGroup qteCanvasGroup;
    [SerializeField] private Slider timerSlider;
    [SerializeField] private TextMeshProUGUI[] keyTexts;
    [SerializeField] private Image[] keyBoxes;
    [SerializeField] private Sprite[] neutralKeySprites;
    [SerializeField] private Sprite[] correctKeySprites;
    [SerializeField] private Sprite[] incorrectKeySprites;

    [Header("QTE SFX")]
    [SerializeField] private AudioSource qteAudioSource;
    [SerializeField] private AudioClip tappingSfx;
    [SerializeField] private AudioClip timerSfx;
    [SerializeField] private AudioClip successSfx;

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

    [Header("UI Fade Settings")]
    [SerializeField] private float fadeDuration = 0.25f;

    [Header("Possible Keys Pool")]
    [SerializeField]
    private KeyCode[] possibleKeys = new KeyCode[]
    {
        KeyCode.A, KeyCode.W, KeyCode.S, KeyCode.D, KeyCode.Z, KeyCode.X, KeyCode.Q
    };

    [Header("Events")]
    public UnityEvent onSuccess;
    public UnityEvent onFail;

    private List<KeyCode> currentSequence = new List<KeyCode>();
    private int currentIndex = 0;
    private float timer = 0f;
    private int lastTimerSecond = 0;
    private bool isActive = false;
    private Vector3 originalPanelPosition;
    private Coroutine shakeCoroutine;
    private Coroutine fadeCoroutine;
    private Sprite[] originalKeyBoxSprites;

    private void Awake()
    {
        if (keyBoxes != null)
        {
            originalKeyBoxSprites = new Sprite[keyBoxes.Length];
            for (int i = 0; i < keyBoxes.Length; i++)
            {
                if (keyBoxes[i] != null) originalKeyBoxSprites[i] = keyBoxes[i].sprite;
            }
        }

        if (qtePanelTransform != null)
        {
            originalPanelPosition = qtePanelTransform.anchoredPosition;
            if (qteCanvasGroup == null)
            {
                qteCanvasGroup = qtePanelTransform.GetComponent<CanvasGroup>();
                if (qteCanvasGroup == null) qteCanvasGroup = qtePanelTransform.gameObject.AddComponent<CanvasGroup>();
            }
            qteCanvasGroup.alpha = 0f;
            qtePanelTransform.gameObject.SetActive(false);
        }
    }

    private void Update()
    {
        if (!isActive) return;

        if (currentSequence.Count == 0 || currentIndex < 0 || currentIndex >= currentSequence.Count)
        {
            FailQTE();
            return;
        }

        timer -= Time.deltaTime;
        if (timerSlider != null) timerSlider.value = timeLimit > 0f ? Mathf.Clamp01(timer / timeLimit) : 0f;

        int currentTimerSecond = Mathf.CeilToInt(timer);
        if (currentTimerSecond < lastTimerSecond)
        {
            PlayQTESfx(timerSfx);
            lastTimerSecond = currentTimerSecond;
        }

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
                PlayQTESfx(tappingSfx);
                SetBoxSprite(currentIndex, expectedKey, correctKeySprites, successColor);
                currentIndex++;

                if (currentIndex >= currentSequence.Count)
                {
                    SuccessQTE();
                }
                else
                {
                    SetBoxSprite(currentIndex, currentSequence[currentIndex], neutralKeySprites, activeColor);
                }
            }
            else
            {
                if (possibleKeys == null) return;

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

        SetBoxSprite(currentIndex, currentSequence[currentIndex], incorrectKeySprites, failColor);

        if (qtePanelTransform != null)
        {
            if (shakeCoroutine != null) StopCoroutine(shakeCoroutine);
            shakeCoroutine = StartCoroutine(ShakePanelRoutine());
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

        if (isActive && keyBoxes != null && currentIndex < keyBoxes.Length)
        {
            SetBoxSprite(currentIndex, currentSequence[currentIndex], neutralKeySprites, activeColor);
        }
        shakeCoroutine = null;
    }

    [ContextMenu("Start QTE Test")]
    public void StartQTE()
    {
        if (possibleKeys == null || possibleKeys.Length == 0)
        {
            Debug.LogWarning("QTE tidak dapat dimulai karena daftar tombol kosong.");
            return;
        }

        CancelInvoke(nameof(HidePanel));
        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        if (shakeCoroutine != null) StopCoroutine(shakeCoroutine);

        // 1. Matikan kontrol gerak Player
        SetPlayerControl(false);

        currentSequence.Clear();
        currentIndex = 0;
        timer = timeLimit;
        lastTimerSecond = Mathf.CeilToInt(timer);
        isActive = true;

        if (qtePanelTransform != null)
        {
            qtePanelTransform.anchoredPosition = originalPanelPosition;
            qtePanelTransform.gameObject.SetActive(true);
            fadeCoroutine = StartCoroutine(FadeCanvasGroup(0f, 1f));
        }

        for (int i = 0; i < 4; i++)
        {
            KeyCode randomKey = possibleKeys[Random.Range(0, possibleKeys.Length)];
            currentSequence.Add(randomKey);

            if (keyTexts != null && i < keyTexts.Length && keyTexts[i] != null)
            {
                keyTexts[i].text = randomKey.ToString().ToUpper();
            }

            SetBoxSprite(i, randomKey, neutralKeySprites, normalColor);
        }

        SetBoxSprite(0, currentSequence[0], neutralKeySprites, activeColor);
    }

    private void SetBoxSprite(int index, KeyCode key, Sprite[] stateSprites, Color fallbackColor)
    {
        if (keyBoxes == null || index < 0 || index >= keyBoxes.Length || keyBoxes[index] == null) return;

        int spriteIndex = GetKeySpriteIndex(key);
        Sprite sprite = stateSprites != null && spriteIndex >= 0 && spriteIndex < stateSprites.Length
            ? stateSprites[spriteIndex]
            : null;

        keyBoxes[index].sprite = sprite != null
            ? sprite
            : originalKeyBoxSprites != null && index < originalKeyBoxSprites.Length
                ? originalKeyBoxSprites[index]
                : null;
        keyBoxes[index].color = sprite != null ? Color.white : fallbackColor;

        if (keyTexts != null && index < keyTexts.Length && keyTexts[index] != null)
        {
            keyTexts[index].enabled = sprite == null;
        }
    }

    private int GetKeySpriteIndex(KeyCode key)
    {
        switch (key)
        {
            case KeyCode.A: return 0;
            case KeyCode.D: return 1;
            case KeyCode.Q: return 2;
            case KeyCode.S: return 3;
            case KeyCode.W: return 4;
            case KeyCode.X: return 5;
            case KeyCode.Z: return 6;
            default: return -1;
        }
    }

    private void SuccessQTE()
    {
        isActive = false;
        PlayQTESfx(successSfx);
        Debug.Log("QTE BERHASIL!");
        onSuccess?.Invoke();
        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(FadeOutAndHide());
    }

    private void FailQTE()
    {
        if (currentIndex >= 0 && currentIndex < currentSequence.Count)
        {
            SetBoxSprite(currentIndex, currentSequence[currentIndex], incorrectKeySprites, failColor);
        }

        isActive = false;
        Debug.Log("QTE GAGAL!");
        onFail?.Invoke();
        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(FadeOutAndHide());
    }

    private IEnumerator FadeOutAndHide()
    {
        yield return new WaitForSeconds(0.4f);
        yield return FadeCanvasGroup(1f, 0f);
        HidePanel();
        fadeCoroutine = null;
    }

    private IEnumerator FadeCanvasGroup(float startAlpha, float targetAlpha)
    {
        if (qteCanvasGroup == null) yield break;

        if (fadeDuration <= 0f)
        {
            qteCanvasGroup.alpha = targetAlpha;
            yield break;
        }

        float elapsed = 0f;
        qteCanvasGroup.alpha = startAlpha;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            qteCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / fadeDuration);
            yield return null;
        }
        qteCanvasGroup.alpha = targetAlpha;
    }

    private void HidePanel()
    {
        if (qtePanelTransform != null) qtePanelTransform.gameObject.SetActive(false);

        // 2. Aktifkan kembali kontrol gerak Player
        SetPlayerControl(true);
    }

    private void PlayQTESfx(AudioClip clip)
    {
        if (clip == null) return;

        if (qteAudioSource != null)
        {
            qteAudioSource.PlayOneShot(clip);
        }
        else
        {
            AudioSource.PlayClipAtPoint(clip, transform.position);
        }
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