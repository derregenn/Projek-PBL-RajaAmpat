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

    private void Update()
    {
        if (isMapOpen && Time.frameCount > mapOpenedFrame && Input.GetKeyDown(KeyCode.E))
        {
            CloseWorldMap();
        }
    }

    // Hanya dipanggil melalui UnityEvent atau script event lain.
    public void OpenWorldMap()
    {
        if (mapController == null || isMapOpen) return;

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