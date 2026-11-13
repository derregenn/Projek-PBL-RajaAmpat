using System.Collections;
using UnityEngine;

public class NPCMapTrigger : MonoBehaviour
{
    public MapController mapController;
    private bool isMapOpen = false;
    private int mapOpenedFrame = -1;
    private Coroutine popupRoutine;

    [SerializeField] private float popupDuration = 0.35f;
    [SerializeField] private Vector3 popupScaleMin = new Vector3(0.75f, 0.75f, 1f);
    [SerializeField] private AudioClip openMapSfx;
    [SerializeField] private string[] requiredQuestIDs;
    [SerializeField] private bool showWhenRequiredQuestsComplete = true;
    [SerializeField] private GameObject alternateNPC;

    private bool HasQuestRequirement => requiredQuestIDs != null && requiredQuestIDs.Length > 0;

    private void OnEnable()
    {
        if (HasQuestRequirement)
        {
            QuestController.OnQuestCompleted += HandleQuestCompleted;
        }
    }

    private void OnDisable()
    {
        QuestController.OnQuestCompleted -= HandleQuestCompleted;
    }

    private void Start()
    {
        if (HasQuestRequirement)
        {
            UpdateNPCVisibility(null);
        }
    }

    private void Update()
    {
        if (isMapOpen && Time.frameCount > mapOpenedFrame && Input.GetKeyDown(KeyCode.E))
        {
            CloseWorldMap();
        }
    }

    private void HandleQuestCompleted(string questID)
    {
        if (!HasQuestRequirement)
        {
            return;
        }

        foreach (string requiredQuestID in requiredQuestIDs)
        {
            if (requiredQuestID == questID)
            {
                UpdateNPCVisibility(questID);
                return;
            }
        }
    }

    private void UpdateNPCVisibility(string justCompletedQuestID)
    {
        if (alternateNPC == null)
        {
            Debug.LogWarning($"[{nameof(NPCMapTrigger)}] Alternate NPC belum di-assign pada {name}.");
            return;
        }

        bool shouldBeActive = AreRequiredQuestsComplete(justCompletedQuestID) == showWhenRequiredQuestsComplete;
        alternateNPC.SetActive(!shouldBeActive);
        if (gameObject.activeSelf != shouldBeActive)
        {
            gameObject.SetActive(shouldBeActive);
        }
    }

    private bool AreRequiredQuestsComplete(string justCompletedQuestID)
    {
        GamePhaseManager gamePhaseManager = GamePhaseManager.Instance;
        if (gamePhaseManager == null)
        {
            return false;
        }

        foreach (string requiredQuestID in requiredQuestIDs)
        {
            if (string.IsNullOrWhiteSpace(requiredQuestID))
            {
                Debug.LogWarning($"[{nameof(NPCMapTrigger)}] Required quest ID kosong pada {name}.");
                return false;
            }

            if (!gamePhaseManager.IsQuestCompleted(requiredQuestID) && requiredQuestID != justCompletedQuestID)
            {
                return false;
            }
        }

        return true;
    }

    // Hanya dipanggil melalui UnityEvent atau script event lain.
    public void OpenWorldMap()
    {
        if (HasQuestRequirement
            && (!showWhenRequiredQuestsComplete || !AreRequiredQuestsComplete(null)))
        {
            return;
        }

        if (mapController == null || isMapOpen) return;

        // Buka peta dunia tanpa menyelesaikan quest atau membuka award secara prematur
        mapController.OpenWorldMap();
        if (openMapSfx != null)
        {
            AudioSource.PlayClipAtPoint(openMapSfx, transform.position);
        }
        isMapOpen = true;
        mapOpenedFrame = Time.frameCount;
        PlayPopupAnimation(true);
    }

    public void CloseWorldMap()
    {
        if (mapController == null || !isMapOpen) return;

        isMapOpen = false;
        PlayPopupAnimation(false);
    }

    private void PlayPopupAnimation(bool opening)
    {
        if (mapController == null) return;

        Transform target = mapController.transform;
        if (target == null) return;

        if (popupRoutine != null)
        {
            StopCoroutine(popupRoutine);
        }

        Vector3 startScale = opening ? popupScaleMin : Vector3.one;
        Vector3 endScale = opening ? Vector3.one : popupScaleMin;
        popupRoutine = StartCoroutine(PopUpAnimation(target, startScale, endScale, popupDuration, opening));
    }

    private IEnumerator PopUpAnimation(Transform target, Vector3 startScale, Vector3 endScale, float duration, bool opening)
    {
        float elapsed = 0f;
        target.localScale = startScale;

        while (elapsed < duration)
        {
            float t = elapsed / duration;
            float eased = EaseOutBack(t);
            target.localScale = Vector3.LerpUnclamped(startScale, endScale, eased);
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        target.localScale = endScale;

        if (!opening && mapController != null)
        {
            mapController.CloseWorldMap();
        }
    }

    private static float EaseOutBack(float t)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;

        return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
    }
}