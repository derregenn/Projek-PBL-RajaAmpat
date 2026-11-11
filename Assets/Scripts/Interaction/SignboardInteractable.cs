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
    [SerializeField] private string objectiveID = "papan_informasi";
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
            if (QuestController.Instance != null)
            {
                if (QuestController.Instance.IsQuestActive("02_Friwen"))
                {
                    QuestController.Instance.ProgressObjective("papan_friwen", 1);
                }
                else if (!string.IsNullOrEmpty(objectiveID))
                {
                    QuestController.Instance.ProgressObjective(objectiveID, 1);
                }
            }

            onClose?.Invoke();
            if (triggerOnce) hasTriggered = true;
        }
    }

    public string GetPromptText() => isOpen ? "Tutup Info" : "Baca Informasi";

    private void AnimatePanel(bool opening)
    {
        if (infoPanel == null) return;

        if (animationCoroutine != null) StopCoroutine(animationCoroutine);

        if (opening)
        {
            EnsureDarkBackdrop();
            infoPanel.SetActive(true);
        }

        if (panelRect == null)
        {
            infoPanel.SetActive(opening);
            return;
        }

        animationCoroutine = StartCoroutine(AnimatePanelRoutine(opening));
    }

    private void EnsureDarkBackdrop()
    {
        if (infoPanel == null) return;

        // Cari apakah sudah ada backdrop hitam
        Transform existingBackdrop = infoPanel.transform.Find("BlackBackdrop");
        if (existingBackdrop == null && infoPanel.transform.parent != null)
        {
            existingBackdrop = infoPanel.transform.parent.Find("BlackBackdrop");
        }

        if (existingBackdrop == null)
        {
            // Periksa jika infoPanel sendiri sudah memiliki warna latar hitam
            UnityEngine.UI.Image img = infoPanel.GetComponent<UnityEngine.UI.Image>();
            if (img != null && img.color.a > 0.5f && img.color.r < 0.1f && img.color.g < 0.1f && img.color.b < 0.1f)
            {
                return;
            }

            GameObject backdrop = new GameObject("BlackBackdrop");
            backdrop.transform.SetParent(infoPanel.transform, false);
            backdrop.transform.SetAsFirstSibling();

            RectTransform rt = backdrop.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(-2f, -2f);
            rt.anchorMax = new Vector2(3f, 3f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            UnityEngine.UI.Image bgImg = backdrop.AddComponent<UnityEngine.UI.Image>();
            bgImg.color = new Color(0f, 0f, 0f, 0.7843f);
            bgImg.raycastTarget = false;
        }
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