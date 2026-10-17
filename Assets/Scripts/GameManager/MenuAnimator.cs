using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.Events;

public class MenuAnimator : MonoBehaviour
{
    [Header("Referensi UI")]
    public RectTransform logo;
    public RectTransform[] buttons; // Masukkan berurutan: Play, Continue, Exit
    public CanvasGroup whiteFadePanel;

    [Header("Pengaturan Waktu")]
    public float animSpeed = 0.5f;
    public float delayBetweenButtons = 0.15f;

    [Header("Kurva Animasi (Bouncy)")]
    public AnimationCurve popInCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Event Setelah Animasi Outro")]
    public UnityEvent onOutroFinished_Play;     // Event untuk tombol Play
    public UnityEvent onOutroFinished_Continue; // Event untuk tombol Continue

    private void Start()
    {
        // 1. Sembunyikan semua UI di awal (Skala = 0)
        if (logo != null) logo.localScale = Vector3.zero;
        foreach (var btn in buttons)
        {
            if (btn != null) btn.localScale = Vector3.zero;
        }

        // 2. Pastikan panel putih transparan di awal
        if (whiteFadePanel != null)
        {
            whiteFadePanel.alpha = 0f;
            whiteFadePanel.blocksRaycasts = false;
        }

        // 3. Mulai animasi intro
        StartCoroutine(PlayIntroSequence());
    }

    private IEnumerator PlayIntroSequence()
    {
        yield return new WaitForSeconds(0.5f); // Jeda sebelum animasi mulai

        // Animasikan Logo
        if (logo != null) StartCoroutine(ScaleUI(logo, Vector3.zero, Vector3.one));
        yield return new WaitForSeconds(0.3f);

        // Animasikan Tombol satu per satu
        foreach (var btn in buttons)
        {
            if (btn != null)
            {
                StartCoroutine(ScaleUI(btn, Vector3.zero, Vector3.one));
                yield return new WaitForSeconds(delayBetweenButtons);
            }
        }
    }

    private IEnumerator ScaleUI(RectTransform uiElement, Vector3 startScale, Vector3 endScale)
    {
        float time = 0f;
        while (time < animSpeed)
        {
            time += Time.deltaTime;
            // Gunakan LerpUnclamped agar kurva bisa melampaui 1 (efek membal/bouncy)
            float curvePercent = popInCurve.Evaluate(time / animSpeed);
            uiElement.localScale = Vector3.LerpUnclamped(startScale, endScale, curvePercent);
            yield return null;
        }
        uiElement.localScale = endScale;
    }

    // 🔴 PANGGIL INI DARI TOMBOL "PLAY / NEW GAME"
    public void ClickPlayButton()
    {
        StartCoroutine(PlayOutroSequence(onOutroFinished_Play));
    }

    // 🟢 PANGGIL INI DARI TOMBOL "CONTINUE"
    public void ClickContinueButton()
    {
        StartCoroutine(PlayOutroSequence(onOutroFinished_Continue));
    }

    private IEnumerator PlayOutroSequence(UnityEvent actionAfterFade)
    {
        // 1. Hilangkan tombol bersaman
        foreach (var btn in buttons)
        {
            if (btn != null) StartCoroutine(ScaleUI(btn, btn.localScale, Vector3.zero));
        }
        yield return new WaitForSeconds(0.2f);

        // 2. Hilangkan logo
        if (logo != null) StartCoroutine(ScaleUI(logo, logo.localScale, Vector3.zero));
        yield return new WaitForSeconds(0.4f);

        // 3. Fade ke Putih
        if (whiteFadePanel != null)
        {
            whiteFadePanel.blocksRaycasts = true;
            float time = 0;
            while (time < 1f) // 1 detik durasi fade
            {
                time += Time.deltaTime;
                whiteFadePanel.alpha = time;
                yield return null;
            }
        }

        // 4. Jalankan fungsi sistem game aslimu (pindah scene / reset data)
        actionAfterFade?.Invoke();

        // Bersihkan fade setelah aksi pindah scene; panel persisten tidak boleh menutupi scene baru.
        if (whiteFadePanel != null)
        {
            whiteFadePanel.alpha = 0f;
            whiteFadePanel.blocksRaycasts = false;
        }
    }
}