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
    [SerializeField] private string requiredCompletedQuestID = "";
    private bool hasTriggered = false;

    [Header("Events")]
    [SerializeField] private UnityEvent onOpen;
    [SerializeField] private UnityEvent onClose;

    [Header("Popup SFX")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip popupSfx;

    [Header("Voice Over (VO) Settings")]
    [Tooltip("Slot Voice Over (VO) per poster/scene. Durasi auto-close mengikuti panjang klip VO.")]
    [SerializeField] private AudioClip voiceOverClip;
    [SerializeField] private AudioSource voiceOverAudioSource;
    [SerializeField] private bool autoCloseAfterVoiceOver = true;
    [SerializeField, Min(0f)] private float extraDelayAfterVoiceOver = 0.25f;

    private bool isOpen = false;
    private static SignboardInteractable activeOpenSignboard;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticState()
    {
        activeOpenSignboard = null;
    }

    public static SignboardInteractable ActiveOpenSignboard =>
        (activeOpenSignboard != null
         && activeOpenSignboard.isOpen
         && activeOpenSignboard.gameObject.activeInHierarchy
         && activeOpenSignboard.infoPanel != null
         && activeOpenSignboard.infoPanel.activeInHierarchy)
        ? activeOpenSignboard
        : null;
    public static bool IsAnySignboardOpen => ActiveOpenSignboard != null;
    public bool IsOpen => isOpen;
    private RectTransform panelRect;
    private Vector2 panelOpenPosition;
    private bool hasInitializedPanel = false;
    private Coroutine animationCoroutine;
    private Coroutine voAutoCloseCoroutine;

    private bool PassesQuestGate()
    {
        if (QuestProgressionGate.Passes(string.Empty, string.Empty, requiredCompletedQuestID))
        {
            return true;
        }

        if (requiredCompletedQuestID == "01_Friwen")
        {
            if (QuestController.Instance != null &&
                (QuestController.Instance.IsQuestActive("04_Friwen")
                 || QuestController.Instance.IsQuestActive("02_Friwen")
                 || QuestController.Instance.IsQuestCompleted("04_Friwen")
                 || QuestController.Instance.IsQuestCompleted("02_Friwen")))
            {
                return true;
            }

            if (MasterQuestManager.Instance != null && MasterQuestManager.Instance.CurrentStage >= 2)
            {
                return true;
            }
        }

        return false;
    }

    private void EnsurePanelInitialized()
    {
        if (hasInitializedPanel || infoPanel == null) return;
        hasInitializedPanel = true;

        panelRect = infoPanel.GetComponent<RectTransform>();
        if (panelRect != null)
        {
            panelOpenPosition = panelRect.anchoredPosition;
            panelRect.anchoredPosition = panelOpenPosition - Vector2.up * pushDistance;
        }

        infoPanel.SetActive(false);
    }

    private void Awake()
    {
        EnsurePanelInitialized();
    }

    public void Interact()
    {
        EnsurePanelInitialized();
        if (!isOpen)
        {
            if (InteractionInputLock.IsAnyModalBlockingWorldInteract || InteractionInputLock.WasInteractKeyConsumedThisFrame) return;
            if (!PassesQuestGate()) return;

            if (infoPanel == null)
            {
                Debug.LogWarning($"[SignboardInteractable] '{name}' belum memiliki referensi infoPanel.", this);
                InteractionInputLock.ConsumeInteractKeyThisFrame();
                onOpen?.Invoke();
                CloseSignboard();
                return;
            }
        }
        isOpen = !isOpen;
        InteractionInputLock.ConsumeInteractKeyThisFrame();
        AnimatePanel(isOpen);

        if (isOpen)
        {
            activeOpenSignboard = this;
            if (audioSource != null && popupSfx != null)
            {
                audioSource.PlayOneShot(popupSfx);
            }

            StartVoiceOverAndAutoClose();
            onOpen?.Invoke();
        }
        else
        {
            if (activeOpenSignboard == this) activeOpenSignboard = null;
            StopVoiceOverAndAutoClose();
            CloseSignboard();
        }
    }

    private void StartVoiceOverAndAutoClose()
    {
        StopVoiceOverAndAutoClose();
        if (voiceOverClip == null) return;

        if (voiceOverAudioSource == null)
        {
            voiceOverAudioSource = audioSource != null ? audioSource : GetComponent<AudioSource>();
            if (voiceOverAudioSource == null)
            {
                voiceOverAudioSource = gameObject.AddComponent<AudioSource>();
                voiceOverAudioSource.playOnAwake = false;
                voiceOverAudioSource.spatialBlend = 0f;
            }
        }

        voiceOverAudioSource.Stop();
        voiceOverAudioSource.clip = voiceOverClip;
        voiceOverAudioSource.Play();

        if (autoCloseAfterVoiceOver)
        {
            float delay = voiceOverClip.length + Mathf.Max(0f, extraDelayAfterVoiceOver);
            voAutoCloseCoroutine = StartCoroutine(AutoCloseAfterVoiceOverRoutine(delay));
        }
    }

    private void StopVoiceOverAndAutoClose()
    {
        if (voAutoCloseCoroutine != null)
        {
            StopCoroutine(voAutoCloseCoroutine);
            voAutoCloseCoroutine = null;
        }
        if (voiceOverAudioSource != null && voiceOverAudioSource.isPlaying)
        {
            voiceOverAudioSource.Stop();
        }
    }

    private IEnumerator AutoCloseAfterVoiceOverRoutine(float delay)
    {
        yield return new WaitForSecondsRealtime(Mathf.Max(0f, delay));
        while (voiceOverAudioSource != null && voiceOverAudioSource.isPlaying)
        {
            yield return null;
        }

        voAutoCloseCoroutine = null;
        if (isOpen)
        {
            isOpen = false;
            if (activeOpenSignboard == this) activeOpenSignboard = null;
            AnimatePanel(false);
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

    public string GetPromptText()
    {
        if (!isOpen && (InteractionInputLock.IsAnyModalBlockingWorldInteract || !PassesQuestGate())) return "";
        return isOpen ? "Tutup Info" : "Baca Informasi";
    }

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

    private void OnDisable()
    {
        isOpen = false;
        if (activeOpenSignboard == this) activeOpenSignboard = null;
        StopVoiceOverAndAutoClose();
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") && isOpen)
        {
            isOpen = false;
            if (activeOpenSignboard == this) activeOpenSignboard = null;
            StopVoiceOverAndAutoClose();
            AnimatePanel(false);
            CloseSignboard();
        }
    }
}