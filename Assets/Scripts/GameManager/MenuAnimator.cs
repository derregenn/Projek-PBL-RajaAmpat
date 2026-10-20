using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.Events;

public class MenuAnimator : MonoBehaviour
{
    [Header("Referensi UI")]
    public RectTransform logo;
    public RectTransform[] buttons; // 0=Play, 1=Continue, 2=Exit
    public CanvasGroup whiteFadePanel;

    [Header("Pengaturan Waktu & Jarak Meluncur")]
    public float animSpeed = 0.55f;
    public float delayBetweenButtons = 0.15f;
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

    private void Awake()
    {
        if (logo != null) logoTargetPos = logo.anchoredPosition;

        if (buttons != null)
        {
            buttonTargetPos = new Vector2[buttons.Length];
            for (int i = 0; i < buttons.Length; i++)
            {
                if (buttons[i] != null) buttonTargetPos[i] = buttons[i].anchoredPosition;
            }
        }
    }

    private void Start()
    {
        PrepareInitialPositions();
        StartCoroutine(PlayIntroSequenceSafe());
    }

    private void PrepareInitialPositions()
    {
        if (logo != null)
        {
            logo.anchoredPosition = logoTargetPos + new Vector2(0f, logoDropOffset);
            CanvasGroup cg = logo.GetComponent<CanvasGroup>();
            if (cg == null) cg = logo.gameObject.AddComponent<CanvasGroup>();
            cg.alpha = 0f;
        }

        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] != null)
            {
                float offset = (i == 1) ? buttonRightOffset : -buttonLeftOffset;
                buttons[i].anchoredPosition = buttonTargetPos[i] + new Vector2(offset, 0f);
                CanvasGroup cg = buttons[i].GetComponent<CanvasGroup>();
                if (cg == null) cg = buttons[i].gameObject.AddComponent<CanvasGroup>();
                cg.alpha = 0f;
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
        // Fade out panel putih dengan aman (jika ada)
        if (whiteFadePanel != null)
        {
            float fadeTime = 0f;
            float duration = 0.6f;
            while (fadeTime < duration)
            {
                fadeTime += Time.deltaTime;
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

        yield return new WaitForSeconds(0.1f);

        // Animasi Logo Masuk
        if (logo != null)
        {
            Vector2 startPos = logoTargetPos + new Vector2(0f, logoDropOffset);
            StartCoroutine(AnimateElementSafe(logo, startPos, logoTargetPos, true));
        }

        yield return new WaitForSeconds(0.2f);

        // Animasi Tombol Masuk Satu Persatu
        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] != null)
            {
                float offset = (i == 1) ? buttonRightOffset : -buttonLeftOffset;
                Vector2 startPos = buttonTargetPos[i] + new Vector2(offset, 0f);
                StartCoroutine(AnimateElementSafe(buttons[i], startPos, buttonTargetPos[i], false));
                yield return new WaitForSeconds(delayBetweenButtons);
            }
        }
    }

    private IEnumerator AnimateElementSafe(RectTransform rect, Vector2 startPos, Vector2 targetPos, bool isLogo)
    {
        if (rect == null) yield break;

        CanvasGroup cg = rect.GetComponent<CanvasGroup>();
        if (cg == null) cg = rect.gameObject.AddComponent<CanvasGroup>();

        float time = 0f;
        while (time < animSpeed)
        {
            if (rect == null || cg == null) yield break;

            time += Time.deltaTime;
            float progress = Mathf.Clamp01(time / animSpeed);
            
            float factor = isLogo ? logoPopInCurve.Evaluate(progress) : Mathf.SmoothStep(0f, 1f, progress);
            rect.anchoredPosition = isLogo ? Vector2.LerpUnclamped(startPos, targetPos, factor) : Vector2.Lerp(startPos, targetPos, factor);
            cg.alpha = Mathf.Lerp(0f, 1f, progress * 2f);

            yield return null;
        }

        if (rect != null) rect.anchoredPosition = targetPos;
        if (cg != null) cg.alpha = 1f;
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
                time += Time.deltaTime;
                if (whiteFadePanel != null) whiteFadePanel.alpha = time / 0.4f;
                yield return null;
            }
            if (whiteFadePanel != null) whiteFadePanel.alpha = 1f;
        }

        actionAfterFade?.Invoke();
    }
}