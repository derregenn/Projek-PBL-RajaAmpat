using UnityEngine;
using UnityEngine.Events;
using System.Collections;

public class FaderTrigger : MonoBehaviour
{
    [Header("Apa yang terjadi saat layar GELAP TOTAL?")]
    public UnityEvent onMidFade;

    [Header("Trigger Collider (opsional)")]
    [SerializeField] private bool triggerOnEnter = true;
    [SerializeField] private string activatorTag = "Player";

    private bool isTransitioning = false;

    /// <summary>
    /// Panggil fungsi ini dari event OnDialogueEnd () NPC
    /// </summary>
    public void TriggerFadeAndDoAction()
    {
        if (isTransitioning) return;
        isTransitioning = true;

        if (UniversalFader.Instance != null)
        {
            // Menjalankan fade, lalu memanggil event onMidFade saat layar hitam
            UniversalFader.Instance.FadeTransition(() =>
            {
                onMidFade?.Invoke();
            });
            StartCoroutine(ResetTransitionFlag(1.5f));
        }
        else
        {
            Debug.LogWarning("[FaderTrigger] UniversalFader tidak ditemukan di scene!");
            onMidFade?.Invoke(); // Jalankan langsung jika tidak ada fader
            isTransitioning = false;
        }
    }

    private IEnumerator ResetTransitionFlag(float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        isTransitioning = false;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (triggerOnEnter && other.CompareTag(activatorTag))
        {
            TriggerFadeAndDoAction();
        }
    }
}
