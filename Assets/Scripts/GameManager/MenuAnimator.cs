using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.Events;

public class MenuAnimator : MonoBehaviour
{
    [Header("Referensi UI")]
    public RectTransform logo;
    public RectTransform[] buttons; // Play, Continue, Exit, Credit, FreeRoam, TravelJournal
    public CanvasGroup whiteFadePanel;

    [Header("Credits Panel Slide")]
    public GameObject creditCanvas;
    public RectTransform creditPanel;
    public float creditSlideOffsetY = 1000f;
    public float creditSlideDuration = 0.45f;

    [Header("Pengaturan Waktu & Jarak Meluncur")]
    public float introStartDelay = 0.35f;
    public float animSpeed = 0.55f;
    public float delayBetweenButtons = 0.12f;
    public float logoDropOffset = 180f;     
    public float buttonLeftOffset = 800f;   
    public float buttonRightOffset = 800f;  

    [Header("Kurva Animasi Logo (Bouncy)")]
    public AnimationCurve logoPopInCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Event Setelah Tombol Diklik")]
    public UnityEvent onOutroFinished_Play;     
    public UnityEvent onOutroFinished_Continue; 

    private Vector2 logoTargetPos;
    private Vector2[] buttonTargetPos;
    private Vector2 creditTargetPos;
    private bool isShowingCredits = false;
    private bool isTransitioningCredits = false;
    private Coroutine creditCoroutine;

    private void Awake()
    {
        Time.timeScale = 1f;
        if (logo != null) logoTargetPos = logo.anchoredPosition;

        if (buttons != null)
        {
            buttonTargetPos = new Vector2[buttons.Length];
            for (int i = 0; i < buttons.Length; i++)
            {
                if (buttons[i] != null) buttonTargetPos[i] = buttons[i].anchoredPosition;
            }
        }

        if (creditPanel != null)
        {
            creditTargetPos = creditPanel.anchoredPosition;
        }

        if (creditCanvas != null)
        {
            creditCanvas.SetActive(false);
        }
    }

    private void Start()
    {
        Time.timeScale = 1f;
        PrepareInitialPositions();
        StartCoroutine(PlayIntroSequenceSafe());
    }

    private void Update()
    {
        if (isShowingCredits && !isTransitioningCredits && Input.GetKeyDown(KeyCode.Escape))
        {
            CloseCredits();
        }
    }

    private float GetSlideOffsetX(int index)
    {
        if (buttonTargetPos != null && index < buttonTargetPos.Length)
        {
            float targetX = buttonTargetPos[index].x;
            if (targetX < -80f) return -buttonLeftOffset;
            if (targetX > 80f) return buttonRightOffset;
        }
        return (index % 2 == 1) ? buttonRightOffset : -buttonLeftOffset;
    }

    private void PrepareInitialPositions()
    {
        if (logo != null)
        {
            logo.anchoredPosition = logoTargetPos + new Vector2(0f, logoDropOffset);
            CanvasGroup cg = logo.GetComponent<CanvasGroup>();
            if (cg == null) cg = logo.gameObject.AddComponent<CanvasGroup>();
            cg.alpha = 0f;
            cg.blocksRaycasts = true;
        }

        if (buttons != null)
        {
            for (int i = 0; i < buttons.Length; i++)
            {
                if (buttons[i] != null)
                {
                    buttons[i].anchoredPosition = buttonTargetPos[i] + new Vector2(GetSlideOffsetX(i), 0f);
                    CanvasGroup cg = buttons[i].GetComponent<CanvasGroup>();
                    if (cg == null) cg = buttons[i].gameObject.AddComponent<CanvasGroup>();
                    cg.alpha = 0f;
                    cg.blocksRaycasts = true;
                }
            }
        }

        if (whiteFadePanel != null)
        {
            whiteFadePanel.gameObject.SetActive(true);
            whiteFadePanel.alpha = 1f;
            whiteFadePanel.blocksRaycasts = true;
        }
    }

    private IEnumerator PlayIntroSequenceSafe()
    {
        if (introStartDelay > 0f)
        {
            yield return new WaitForSecondsRealtime(introStartDelay);
        }

        // Fade out panel putih dengan aman (jika ada)
        if (whiteFadePanel != null)
        {
            float fadeTime = 0f;
            float duration = 0.6f;
            while (fadeTime < duration)
            {
                fadeTime += Time.unscaledDeltaTime;
                if (whiteFadePanel != null)
                    whiteFadePanel.alpha = Mathf.Lerp(1f, 0f, fadeTime / duration);
                yield return null;
            }
            if (whiteFadePanel != null)
            {
                whiteFadePanel.alpha = 0f;
                whiteFadePanel.blocksRaycasts = false;
            }
        }

        yield return new WaitForSecondsRealtime(0.1f);

        // Animasi Logo Masuk
        if (logo != null)
        {
            Vector2 startPos = logoTargetPos + new Vector2(0f, logoDropOffset);
            StartCoroutine(AnimateElementSafe(logo, startPos, logoTargetPos, true));
        }

        yield return new WaitForSecondsRealtime(0.2f);

        // Animasi Semua Tombol Masuk Satu Persatu
        if (buttons != null)
        {
            for (int i = 0; i < buttons.Length; i++)
            {
                if (buttons[i] != null)
                {
                    Vector2 startPos = buttonTargetPos[i] + new Vector2(GetSlideOffsetX(i), 0f);
                    StartCoroutine(AnimateElementSafe(buttons[i], startPos, buttonTargetPos[i], false));
                    yield return new WaitForSecondsRealtime(delayBetweenButtons);
                }
            }
        }
    }

    private IEnumerator AnimateElementSafe(RectTransform rect, Vector2 startPos, Vector2 targetPos, bool isLogo)
    {
        if (rect == null) yield break;

        CanvasGroup cg = rect.GetComponent<CanvasGroup>();
        if (cg == null) cg = rect.gameObject.AddComponent<CanvasGroup>();
        cg.blocksRaycasts = true;

        float time = 0f;
        while (time < animSpeed)
        {
            if (rect == null || cg == null) yield break;

            time += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(time / animSpeed);
            
            float factor = isLogo ? logoPopInCurve.Evaluate(progress) : Mathf.SmoothStep(0f, 1f, progress);
            rect.anchoredPosition = isLogo ? Vector2.LerpUnclamped(startPos, targetPos, factor) : Vector2.Lerp(startPos, targetPos, factor);
            cg.alpha = Mathf.Lerp(0f, 1f, progress * 2f);

            yield return null;
        }

        if (rect != null) rect.anchoredPosition = targetPos;
        if (cg != null) cg.alpha = 1f;
    }

    public void OpenCredits()
    {
        if (isShowingCredits || isTransitioningCredits || creditPanel == null) return;
        if (creditCoroutine != null) StopCoroutine(creditCoroutine);
        creditCoroutine = StartCoroutine(OpenCreditsRoutine());
    }

    public void CloseCredits()
    {
        if (!isShowingCredits || isTransitioningCredits || creditPanel == null) return;
        if (creditCoroutine != null) StopCoroutine(creditCoroutine);
        creditCoroutine = StartCoroutine(CloseCreditsRoutine());
    }

    private IEnumerator OpenCreditsRoutine()
    {
        isTransitioningCredits = true;
        isShowingCredits = true;

        if (creditCanvas != null) creditCanvas.SetActive(true);
        creditPanel.gameObject.SetActive(true);

        CanvasGroup creditCg = creditPanel.GetComponent<CanvasGroup>();
        if (creditCg == null) creditCg = creditPanel.gameObject.AddComponent<CanvasGroup>();
        creditCg.alpha = 0f;
        creditCg.blocksRaycasts = false;

        Vector2 creditStart = creditTargetPos + new Vector2(0f, -creditSlideOffsetY);
        creditPanel.anchoredPosition = creditStart;

        Vector2 logoStart = logo != null ? logo.anchoredPosition : Vector2.zero;
        Vector2 logoOut = logoTargetPos + new Vector2(0f, logoDropOffset);
        CanvasGroup logoCg = logo != null ? GetOrAddCanvasGroup(logo.gameObject) : null;
        if (logoCg != null) logoCg.blocksRaycasts = false;

        int btnCount = buttons != null ? buttons.Length : 0;
        Vector2[] btnStarts = new Vector2[btnCount];
        Vector2[] btnOuts = new Vector2[btnCount];
        CanvasGroup[] btnCgs = new CanvasGroup[btnCount];
        for (int i = 0; i < btnCount; i++)
        {
            if (buttons[i] != null)
            {
                btnStarts[i] = buttons[i].anchoredPosition;
                btnOuts[i] = buttonTargetPos[i] + new Vector2(GetSlideOffsetX(i), 0f);
                btnCgs[i] = GetOrAddCanvasGroup(buttons[i].gameObject);
                btnCgs[i].blocksRaycasts = false;
            }
        }

        float elapsed = 0f;
        float duration = Mathf.Max(0.15f, creditSlideDuration);
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));

            if (logo != null)
            {
                logo.anchoredPosition = Vector2.Lerp(logoStart, logoOut, t);
                if (logoCg != null) logoCg.alpha = 1f - t;
            }

            for (int i = 0; i < btnCount; i++)
            {
                if (buttons[i] != null)
                {
                    buttons[i].anchoredPosition = Vector2.Lerp(btnStarts[i], btnOuts[i], t);
                    if (btnCgs[i] != null) btnCgs[i].alpha = 1f - t;
                }
            }

            creditPanel.anchoredPosition = Vector2.Lerp(creditStart, creditTargetPos, t);
            creditCg.alpha = t;
            yield return null;
        }

        if (logo != null) { logo.anchoredPosition = logoOut; if (logoCg != null) logoCg.alpha = 0f; }
        for (int i = 0; i < btnCount; i++)
        {
            if (buttons[i] != null)
            {
                buttons[i].anchoredPosition = btnOuts[i];
                if (btnCgs[i] != null) btnCgs[i].alpha = 0f;
            }
        }

        creditPanel.anchoredPosition = creditTargetPos;
        creditCg.alpha = 1f;
        creditCg.blocksRaycasts = true;
        isTransitioningCredits = false;
    }

    private IEnumerator CloseCreditsRoutine()
    {
        isTransitioningCredits = true;

        CanvasGroup creditCg = GetOrAddCanvasGroup(creditPanel.gameObject);
        creditCg.blocksRaycasts = false;

        Vector2 creditStart = creditPanel.anchoredPosition;
        Vector2 creditOut = creditTargetPos + new Vector2(0f, -creditSlideOffsetY);

        Vector2 logoStart = logo != null ? logo.anchoredPosition : Vector2.zero;
        CanvasGroup logoCg = logo != null ? GetOrAddCanvasGroup(logo.gameObject) : null;

        int btnCount = buttons != null ? buttons.Length : 0;
        Vector2[] btnStarts = new Vector2[btnCount];
        CanvasGroup[] btnCgs = new CanvasGroup[btnCount];
        for (int i = 0; i < btnCount; i++)
        {
            if (buttons[i] != null)
            {
                btnStarts[i] = buttons[i].anchoredPosition;
                btnCgs[i] = GetOrAddCanvasGroup(buttons[i].gameObject);
            }
        }

        float elapsed = 0f;
        float duration = Mathf.Max(0.15f, creditSlideDuration);
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));

            creditPanel.anchoredPosition = Vector2.Lerp(creditStart, creditOut, t);
            creditCg.alpha = 1f - t;

            if (logo != null)
            {
                logo.anchoredPosition = Vector2.Lerp(logoStart, logoTargetPos, t);
                if (logoCg != null) logoCg.alpha = t;
            }

            for (int i = 0; i < btnCount; i++)
            {
                if (buttons[i] != null)
                {
                    buttons[i].anchoredPosition = Vector2.Lerp(btnStarts[i], buttonTargetPos[i], t);
                    if (btnCgs[i] != null) btnCgs[i].alpha = t;
                }
            }

            yield return null;
        }

        creditPanel.anchoredPosition = creditOut;
        creditCg.alpha = 0f;
        if (creditCanvas != null) creditCanvas.SetActive(false);

        if (logo != null) { logo.anchoredPosition = logoTargetPos; if (logoCg != null) { logoCg.alpha = 1f; logoCg.blocksRaycasts = true; } }
        for (int i = 0; i < btnCount; i++)
        {
            if (buttons[i] != null)
            {
                buttons[i].anchoredPosition = buttonTargetPos[i];
                if (btnCgs[i] != null)
                {
                    btnCgs[i].alpha = 1f;
                    btnCgs[i].blocksRaycasts = true;
                }
            }
        }

        isShowingCredits = false;
        isTransitioningCredits = false;
    }

    private CanvasGroup GetOrAddCanvasGroup(GameObject target)
    {
        CanvasGroup cg = target.GetComponent<CanvasGroup>();
        if (cg == null) cg = target.AddComponent<CanvasGroup>();
        return cg;
    }

    public void ClickPlayButton()
    {
        StartCoroutine(PlayDirectFadeSequence(onOutroFinished_Play));
    }

    public void ClickContinueButton()
    {
        StartCoroutine(PlayDirectFadeSequence(onOutroFinished_Continue));
    }

    private IEnumerator PlayDirectFadeSequence(UnityEvent actionAfterFade)
    {
        if (whiteFadePanel != null)
        {
            whiteFadePanel.gameObject.SetActive(true);
            whiteFadePanel.blocksRaycasts = true;
            
            float time = 0f;
            while (time < 0.4f)
            {
                time += Time.unscaledDeltaTime;
                if (whiteFadePanel != null) whiteFadePanel.alpha = time / 0.4f;
                yield return null;
            }
            if (whiteFadePanel != null) whiteFadePanel.alpha = 1f;
        }

        actionAfterFade?.Invoke();
    }
}