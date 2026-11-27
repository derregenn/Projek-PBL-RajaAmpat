using System.Collections;
using UnityEngine;

public class NPCMapTrigger : MonoBehaviour
{
    private static NPCMapTrigger activeMapTrigger;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticState()
    {
        activeMapTrigger = null;
    }

    public static bool IsAnyMapOpen =>
        activeMapTrigger != null
        && activeMapTrigger.isMapOpen
        && activeMapTrigger.gameObject.activeInHierarchy
        && (activeMapTrigger.mapController == null
            || activeMapTrigger.mapController.worldMapCanvasGroup == null
            || (activeMapTrigger.mapController.worldMapCanvasGroup.gameObject.activeInHierarchy
                && activeMapTrigger.mapController.worldMapCanvasGroup.alpha > 0.01f));

    public MapController mapController;
    private bool isMapOpen = false;
    private int mapOpenedFrame = -1;
    private Coroutine popupRoutine;

    [SerializeField] private float popupDuration = 0.35f;
    [SerializeField] private Vector3 popupScaleMin = new Vector3(0.75f, 0.75f, 1f);
    [SerializeField] private AudioClip openMapSfx;
    [SerializeField] private string objectiveIDOnMapOpen;
    private void Update()
    {
        if (isMapOpen && Time.frameCount > mapOpenedFrame && Input.GetKeyDown(KeyCode.E))
        {
            InteractionInputLock.ConsumeInteractKeyThisFrame();
            CloseWorldMap();
        }
    }

    private void OnDisable()
    {
        isMapOpen = false;
        if (activeMapTrigger == this) activeMapTrigger = null;
    }

    // Hanya dipanggil melalui UnityEvent atau script event lain.
    public void OpenWorldMap()
    {
        if (mapController == null)
        {
            mapController = FindFirstObjectByType<MapController>(FindObjectsInactive.Include);
        }

        if (mapController == null)
        {
            Debug.LogWarning($"[{nameof(NPCMapTrigger)}] MapController tidak ditemukan di scene pada {name}.");
            return;
        }

        // Jika map sedang tertutup di hierarchy, reset status isMapOpen
        if (mapController.worldMapCanvasGroup != null && !mapController.worldMapCanvasGroup.gameObject.activeInHierarchy)
        {
            isMapOpen = false;
        }

        if (!QuestJournalManager.CanLeaveCurrentIslandForMap())
        {
            isMapOpen = false;
            MapController.HideCanvasGroupImmediate(mapController.worldMapCanvasGroup);
            Time.timeScale = 1f;
            Debug.Log("[NPCMapTrigger] Dialog NPCMap selesai, namun MapMenuPanel disembunyikan karena semua quest pulau ini belum selesai.");
            return;
        }

        if (isMapOpen) return;

        mapController.OpenWorldMap();
        if (openMapSfx != null)
        {
            AudioSource.PlayClipAtPoint(openMapSfx, transform.position);
        }
        isMapOpen = true;
        activeMapTrigger = this;
        mapOpenedFrame = Time.frameCount;
        CompleteMapObjective();
        PlayPopupAnimation(true);
    }

    public void ResetMapState()
    {
        isMapOpen = false;
    }

    private void CompleteMapObjective()
    {
        if (string.IsNullOrWhiteSpace(objectiveIDOnMapOpen))
        {
            return;
        }

        QuestController questController = QuestController.Instance;
        if (questController == null || questController.ActiveQuests == null)
        {
            return;
        }

        foreach (Quest quest in questController.ActiveQuests)
        {
            QuestObjective objective = quest != null ? quest.GetObjective(objectiveIDOnMapOpen) : null;
            if (objective == null || objective.IsComplete)
            {
                continue;
            }

            questController.ProgressObjective(objectiveIDOnMapOpen);
            return;
        }
    }

    public void CloseWorldMap()
    {
        if (mapController == null)
        {
            mapController = FindFirstObjectByType<MapController>(FindObjectsInactive.Include);
        }

        isMapOpen = false;
        if (activeMapTrigger == this) activeMapTrigger = null;
        if (mapController != null)
        {
            PlayPopupAnimation(false);
        }
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