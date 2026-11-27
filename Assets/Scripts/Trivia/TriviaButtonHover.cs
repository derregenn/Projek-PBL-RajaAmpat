using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Komponen efek hover untuk tombol jawaban Trivia.
/// Menangani animasi zoom/scale dan pemutaran SFX saat mouse mengarah ke tombol (atau navigasi keyboard/gamepad).
/// </summary>
[RequireComponent(typeof(Button))]
public class TriviaButtonHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler, IPointerClickHandler
{
    [Header("Scale Animation")]
    [Tooltip("Faktor perbesaran saat tombol di-hover (misal 1.06 = 106%)")]
    [SerializeField] private float hoverScale = 1.06f;

    [Tooltip("Kecepatan transisi scale lerp")]
    [SerializeField] private float transitionSpeed = 14f;

    [Header("Audio")]
    [Tooltip("SFX yang diputar saat tombol di-hover")]
    [SerializeField] private AudioClip hoverSFX;
    [SerializeField] private AudioSource audioSource;
    [Range(0f, 1f)] [SerializeField] private float sfxVolume = 1f;

    private Vector3 originalScale;
    private Vector3 targetScale;
    private Coroutine scaleCoroutine;
    private Button button;
    private bool isInitialized = false;

    private void Awake()
    {
        button = GetComponent<Button>();
        if (!isInitialized)
        {
            originalScale = transform.localScale;
            if (originalScale == Vector3.zero) originalScale = Vector3.one;
            targetScale = originalScale;
            isInitialized = true;
        }
    }

    private void OnEnable()
    {
        ResetScaleInstant();
    }

    private void OnDisable()
    {
        ResetScaleInstant();
    }

    /// <summary>
    /// Inisialisasi properti hover secara dinamis dari TriviaQuizUI.
    /// </summary>
    public void Initialize(float targetHoverScale, float speed, AudioClip sfx, AudioSource source, float volume = 1f)
    {
        hoverScale = targetHoverScale;
        transitionSpeed = speed;
        hoverSFX = sfx;
        audioSource = source;
        sfxVolume = volume;

        if (!isInitialized)
        {
            originalScale = transform.localScale;
            if (originalScale == Vector3.zero) originalScale = Vector3.one;
            targetScale = originalScale;
            isInitialized = true;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (button != null && !button.interactable) return;
        TriggerHover();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        EndHover();
    }

    public void OnSelect(BaseEventData eventData)
    {
        if (button != null && !button.interactable) return;
        TriggerHover();
    }

    public void OnDeselect(BaseEventData eventData)
    {
        EndHover();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // Reset scale saat diklik agar tidak stuck membesar
        ResetScaleInstant();
    }

    private void TriggerHover()
    {
        targetScale = originalScale * hoverScale;
        PlayHoverSound();
        StartScaleAnimation();
    }

    private void EndHover()
    {
        targetScale = originalScale;
        StartScaleAnimation();
    }

    public void ResetScaleInstant()
    {
        if (scaleCoroutine != null)
        {
            StopCoroutine(scaleCoroutine);
            scaleCoroutine = null;
        }

        if (isInitialized)
        {
            targetScale = originalScale;
            transform.localScale = originalScale;
        }
    }

    private void PlayHoverSound()
    {
        if (hoverSFX == null) return;

        if (audioSource != null)
        {
            audioSource.PlayOneShot(hoverSFX, sfxVolume);
        }
        else
        {
            AudioSource.PlayClipAtPoint(hoverSFX, Camera.main != null ? Camera.main.transform.position : Vector3.zero, sfxVolume);
        }
    }

    private void StartScaleAnimation()
    {
        if (!gameObject.activeInHierarchy)
        {
            transform.localScale = targetScale;
            return;
        }

        if (scaleCoroutine != null) StopCoroutine(scaleCoroutine);
        scaleCoroutine = StartCoroutine(AnimateScaleRoutine());
    }

    private IEnumerator AnimateScaleRoutine()
    {
        while (Vector3.Distance(transform.localScale, targetScale) > 0.001f)
        {
            transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.unscaledDeltaTime * transitionSpeed);
            yield return null;
        }

        transform.localScale = targetScale;
        scaleCoroutine = null;
    }
}
