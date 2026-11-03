using UnityEngine;
using UnityEngine.Events;
using System.Collections;

public class SignboardInteractable : MonoBehaviour, IInteractable
{
    [Header("UI Reference")]
    [SerializeField] private GameObject infoPanel;

    [Header("Animation")]
    [SerializeField] private float animationDuration = 0.3f;
    [SerializeField] private float pushDistance = 80f;

    [Header("Quest Options")]
    [Tooltip("Jika dicentang, event onClose hanya akan dipanggil 1 kali (memicu NPC Warlok 2)")]
    [SerializeField] private bool triggerOnce = true;
    private bool hasTriggered = false;

    [Header("Events")]
    [SerializeField] private UnityEvent onOpen;
    [SerializeField] private UnityEvent onClose;

    [Header("Popup SFX")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip popupSfx;

    private bool isOpen = false;
    private RectTransform panelRect;
    private Vector2 panelOpenPosition;
    private Coroutine animationCoroutine;

    private void Awake()
    {
        if (infoPanel != null)
        {
            panelRect = infoPanel.GetComponent<RectTransform>();
            if (panelRect != null)
            {
                panelOpenPosition = panelRect.anchoredPosition;
                panelRect.anchoredPosition = panelOpenPosition - Vector2.up * pushDistance;
            }

            infoPanel.SetActive(false);
        }
    }

    public void Interact()
    {
        isOpen = !isOpen;
        AnimatePanel(isOpen);

        if (isOpen)
        {
            if (audioSource != null && popupSfx != null)
            {
                audioSource.PlayOneShot(popupSfx);
            }

            onOpen?.Invoke();
        }
        else
        {
            CloseSignboard();
        }
    }

    private void CloseSignboard()
    {
        if (!hasTriggered || !triggerOnce)
        {
            onClose?.Invoke();
            if (triggerOnce) hasTriggered = true;
        }
    }

    public string GetPromptText() => isOpen ? "Tutup Info" : "Baca Informasi";

    private void AnimatePanel(bool opening)
    {
        if (infoPanel == null) return;

        if (animationCoroutine != null) StopCoroutine(animationCoroutine);

        if (opening) infoPanel.SetActive(true);

        if (panelRect == null)
        {
            infoPanel.SetActive(opening);
            return;
        }

        animationCoroutine = StartCoroutine(AnimatePanelRoutine(opening));
    }

    private IEnumerator AnimatePanelRoutine(bool opening)
    {
        Vector2 startPosition = panelRect.anchoredPosition;
        Vector2 targetPosition = opening
            ? panelOpenPosition
            : panelOpenPosition - Vector2.up * pushDistance;
        float elapsed = 0f;
        float duration = Mathf.Max(0.01f, animationDuration);

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            float easedProgress = progress * progress * (3f - 2f * progress);
            panelRect.anchoredPosition = Vector2.LerpUnclamped(startPosition, targetPosition, easedProgress);
            yield return null;
        }

        panelRect.anchoredPosition = targetPosition;
        if (!opening) infoPanel.SetActive(false);
        animationCoroutine = null;
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") && isOpen)
        {
            isOpen = false;
            AnimatePanel(false);
            CloseSignboard();
        }
    }
}