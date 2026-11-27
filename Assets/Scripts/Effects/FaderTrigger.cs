using UnityEngine;
using UnityEngine.Events;

public class FaderTrigger : MonoBehaviour
{
    [Header("Apa yang terjadi saat layar GELAP TOTAL?")]
    public UnityEvent onMidFade;

    [Header("Trigger Collider (opsional)")]
    [SerializeField] private bool triggerOnEnter = true;
    [SerializeField] private string activatorTag = "Player";

    /// <summary>
    /// Panggil fungsi ini dari event OnDialogueEnd () NPC
    /// </summary>
    public void TriggerFadeAndDoAction()
    {
        if (UniversalFader.Instance != null)
        {
            // Menjalankan fade, lalu memanggil event onMidFade saat layar hitam
            UniversalFader.Instance.FadeTransition(() =>
            {
                onMidFade?.Invoke();
            });
        }
        else
        {
            ScreenFader fader = ScreenFader.Instance != null
                ? ScreenFader.Instance
                : FindFirstObjectByType<ScreenFader>();

            if (fader != null)
            {
                fader.StartCoroutine(ScreenFaderTransitionRoutine(fader));
            }
            else
            {
                Debug.LogWarning("[FaderTrigger] UniversalFader dan ScreenFader tidak ditemukan di scene!");
                onMidFade?.Invoke(); // Jalankan langsung jika tidak ada fader
            }
        }
    }

    private System.Collections.IEnumerator ScreenFaderTransitionRoutine(ScreenFader fader)
    {
        yield return fader.StartCoroutine(fader.FadeOutRoutine());
        onMidFade?.Invoke();
        yield return fader.StartCoroutine(fader.FadeInRoutine());
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (triggerOnEnter && other.CompareTag(activatorTag))
        {
            TriggerFadeAndDoAction();
        }
    }
}