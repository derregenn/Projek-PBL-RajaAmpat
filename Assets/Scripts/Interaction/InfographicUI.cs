using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class InfographicUI : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Masukkan GameObject Panel/Canvas Infografis ke sini")]
    public GameObject infographicPanel;

    [Header("Popup Settings")]
    [Tooltip("Durasi ease-in popup")]
    public float popupDuration = 0.35f;

    [Tooltip("Durasi player kontrol dinonaktifkan setelah infografis muncul")]
    public float playerControlLockDuration = 5f;

    [Tooltip("Script player yang akan dinonaktifkan sementara. Kosongkan jika ingin otomatis mencari script player umum.")]
    public MonoBehaviour[] playerControlsToDisable;

    [Header("Events")]
    [Tooltip("Event yang berjalan saat infografis ditutup (Bisa dipakai untuk memberi Quest)")]
    public UnityEvent onInfographicClosed;

    private CanvasGroup infographicCanvasGroup;
    private RectTransform infographicRectTransform;
    private Coroutine popupCoroutine;
    private Coroutine autoCloseCoroutine;
    private Coroutine enablePlayerControlCoroutine;
    private readonly List<MonoBehaviour> toggledPlayerScripts = new List<MonoBehaviour>();
    private Vector3 originalPanelScale = Vector3.one;
    private bool hasOriginalPanelScale;

    private void Awake()
    {
        CachePanelComponents();

        // Pastikan panel infografis mati saat game baru dimulai
        if (infographicPanel != null)
        {
            infographicPanel.SetActive(false);
        }
    }

    private void Start()
    {
        // Jangan tampilkan panel otomatis di Start agar ukuran/posisi panel tetap sesuai setup editor.
        // Panggil ShowPanel() hanya saat event yang benar-benar memicu infografis muncul.
        if (infographicPanel != null)
        {
            infographicPanel.SetActive(false);
        }
    }

    private void OnDisable()
    {
        RestorePlayerControl();

        if (popupCoroutine != null)
        {
            StopCoroutine(popupCoroutine);
            popupCoroutine = null;
        }

        if (autoCloseCoroutine != null)
        {
            StopCoroutine(autoCloseCoroutine);
            autoCloseCoroutine = null;
        }

        if (enablePlayerControlCoroutine != null)
        {
            StopCoroutine(enablePlayerControlCoroutine);
            enablePlayerControlCoroutine = null;
        }
    }

    private void CachePanelComponents()
    {
        if (infographicPanel == null)
        {
            return;
        }

        infographicRectTransform = infographicPanel.GetComponent<RectTransform>();
        infographicCanvasGroup = infographicPanel.GetComponent<CanvasGroup>();

        if (infographicCanvasGroup == null)
        {
            infographicCanvasGroup = infographicPanel.AddComponent<CanvasGroup>();
        }

        if (infographicRectTransform != null && !hasOriginalPanelScale)
        {
            originalPanelScale = infographicRectTransform.localScale;
            hasOriginalPanelScale = true;
        }

        if (infographicPanel.activeSelf)
        {
            infographicCanvasGroup.alpha = 1f;
        }
        else
        {
            infographicCanvasGroup.alpha = 0f;
        }

        if (infographicRectTransform != null)
        {
            infographicRectTransform.localScale = originalPanelScale;
        }
    }

    /// <summary>
    /// Panggil fungsi ini dari On Dialogue End () Petugas Hub
    /// </summary>
    public void ShowPanel()
    {
        if (infographicPanel == null)
        {
            return;
        }

        CachePanelComponents();
        infographicPanel.SetActive(true);

        if (popupCoroutine != null)
        {
            StopCoroutine(popupCoroutine);
        }

        if (autoCloseCoroutine != null)
        {
            StopCoroutine(autoCloseCoroutine);
        }

        if (enablePlayerControlCoroutine != null)
        {
            StopCoroutine(enablePlayerControlCoroutine);
        }

        popupCoroutine = StartCoroutine(EaseInPopup());
        DisablePlayerControlForDuration(playerControlLockDuration);
        autoCloseCoroutine = StartCoroutine(CloseAfterDelay(playerControlLockDuration));
    }

    private IEnumerator EaseInPopup()
    {
        if (infographicCanvasGroup == null)
        {
            yield break;
        }

        Vector3 startScale = originalPanelScale * 0.85f;
        Vector3 targetScale = originalPanelScale;

        if (infographicRectTransform != null)
        {
            infographicRectTransform.localScale = startScale;
        }

        infographicCanvasGroup.alpha = 0f;
        float elapsed = 0f;

        while (elapsed < popupDuration)
        {
            float t = Mathf.Clamp01(elapsed / popupDuration);
            float eased = Mathf.SmoothStep(0f, 1f, t);

            infographicCanvasGroup.alpha = eased;

            if (infographicRectTransform != null)
            {
                infographicRectTransform.localScale = Vector3.Lerp(startScale, targetScale, eased);
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        infographicCanvasGroup.alpha = 1f;

        if (infographicRectTransform != null)
        {
            infographicRectTransform.localScale = targetScale;
        }
    }

    private IEnumerator CloseAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        HidePanel();
    }

    private void DisablePlayerControlForDuration(float duration)
    {
        RestorePlayerControl();

        toggledPlayerScripts.Clear();

        if (playerControlsToDisable != null)
        {
            foreach (MonoBehaviour playerScript in playerControlsToDisable)
            {
                if (playerScript != null && playerScript.enabled)
                {
                    toggledPlayerScripts.Add(playerScript);
                    playerScript.enabled = false;
                }
            }
        }

        foreach (MonoBehaviour playerScript in FindPlayerControlScripts())
        {
            if (playerScript != null && !toggledPlayerScripts.Contains(playerScript))
            {
                toggledPlayerScripts.Add(playerScript);
                playerScript.enabled = false;
            }
        }

        enablePlayerControlCoroutine = StartCoroutine(EnablePlayerControlAfterDelay(duration));
    }

    private IEnumerator EnablePlayerControlAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        RestorePlayerControl();
    }

    private void RestorePlayerControl()
    {
        foreach (MonoBehaviour playerScript in toggledPlayerScripts)
        {
            if (playerScript != null)
            {
                playerScript.enabled = true;
            }
        }

        toggledPlayerScripts.Clear();
    }

    private List<MonoBehaviour> FindPlayerControlScripts()
    {
        List<MonoBehaviour> foundScripts = new List<MonoBehaviour>();
#pragma warning disable CS0618 // Type or member is obsolete
        MonoBehaviour[] allScripts = FindObjectsOfType<MonoBehaviour>(true);
#pragma warning restore CS0618 // Type or member is obsolete

        foreach (MonoBehaviour script in allScripts)
        {
            if (script == null)
            {
                continue;
            }

            string typeName = script.GetType().Name;
            bool isLikelyPlayerControl = typeName.Contains("Player", System.StringComparison.OrdinalIgnoreCase)
                || typeName.Contains("Movement", System.StringComparison.OrdinalIgnoreCase)
                || typeName.Contains("Controller", System.StringComparison.OrdinalIgnoreCase)
                || typeName.Contains("Input", System.StringComparison.OrdinalIgnoreCase);

            if (isLikelyPlayerControl)
            {
                foundScripts.Add(script);
            }
        }

        return foundScripts;
    }

    /// <summary>
    /// Panggil fungsi ini dari tombol 'Close' / 'X' / 'Mengerti' di UI Infografis
    /// </summary>
    public void HidePanel()
    {
        if (autoCloseCoroutine != null)
        {
            StopCoroutine(autoCloseCoroutine);
            autoCloseCoroutine = null;
        }

        if (enablePlayerControlCoroutine != null)
        {
            StopCoroutine(enablePlayerControlCoroutine);
            enablePlayerControlCoroutine = null;
        }

        RestorePlayerControl();

        if (infographicPanel != null)
        {
            infographicPanel.SetActive(false);
        }

        if (infographicRectTransform != null)
        {
            infographicRectTransform.localScale = originalPanelScale;
        }

        // Jalankan event penutup (misal: memicu Quest Baru)
        onInfographicClosed?.Invoke();
    }
}