using System.Collections;
using UnityEngine;

public class MapController : MonoBehaviour
{
    // Variabel static untuk menyimpan scene pulau tujuan (misal: "03_Pianemo")
    public static string targetIslandScene = "03_Pianemo";

    [Header("Linear Minimap (Key Q)")]
    public CanvasGroup minimapCanvasGroup; // Masukkan MinimapTrack ke sini
    public KeyCode toggleKey = KeyCode.Q;
    public float fadeDuration = 0.2f;

    [Header("World Map Panel (NPC Only)")]
    public CanvasGroup worldMapCanvasGroup; // Masukkan MapMenuPanel ke sini

    private bool isMinimapOpen = true; // Default tampil di layar
    private Coroutine minimapFadeRoutine;

    void Start()
    {
        // Pastikan World Map tertutup di awal game
        if (worldMapCanvasGroup != null)
        {
            worldMapCanvasGroup.alpha = 0f;
            worldMapCanvasGroup.interactable = false;
            worldMapCanvasGroup.blocksRaycasts = false;
            worldMapCanvasGroup.gameObject.SetActive(false);
        }
    }

    void Update()
    {
        // Tekan Q hanya untuk toggle MinimapTrack (jika peta NPC sedang tidak terbuka)
        if (Input.GetKeyDown(toggleKey))
        {
            if (worldMapCanvasGroup != null && worldMapCanvasGroup.gameObject.activeSelf)
            {
                CloseWorldMap(); // Jika world map lagi aktif, tombol Q bisa dipakai menutupnya
            }
            else
            {
                ToggleMinimap();
            }
        }
    }

    // --- LOGIKA MINIMAP LINEAR (Q KEY) ---
    public void ToggleMinimap()
    {
        isMinimapOpen = !isMinimapOpen;

        if (minimapFadeRoutine != null) StopCoroutine(minimapFadeRoutine);
        minimapFadeRoutine = StartCoroutine(FadeGroup(minimapCanvasGroup, isMinimapOpen, false));
    }

    // --- LOGIKA WORLD MAP PULAU (DIPANGGIL NPC) ---
    public void OpenWorldMap()
    {
        if (!QuestJournalManager.CanLeaveCurrentIslandForMap())
        {
            HideCanvasGroupImmediate(worldMapCanvasGroup);
            Time.timeScale = 1f;
            Debug.Log("[MapController] MapMenuPanel ditahan karena quest di pulau ini belum semua selesai.");
            return;
        }

        StartCoroutine(FadeGroup(worldMapCanvasGroup, true, true));
    }

    public static void HideCanvasGroupImmediate(CanvasGroup cg)
    {
        if (cg == null) return;
        cg.alpha = 0f;
        cg.interactable = false;
        cg.blocksRaycasts = false;
        cg.gameObject.SetActive(false);
    }

    public void CloseWorldMap()
    {
        NPCMapTrigger[] triggers = FindObjectsByType<NPCMapTrigger>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var trig in triggers)
        {
            if (trig != null) trig.ResetMapState();
        }

        StartCoroutine(FadeGroup(worldMapCanvasGroup, false, true));
    }

    private IEnumerator FadeGroup(CanvasGroup cg, bool open, bool affectTimeScale)
    {
        if (cg == null) yield break;

        if (open) cg.gameObject.SetActive(true);

        float startAlpha = cg.alpha;
        float targetAlpha = open ? 1f : 0f;
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            cg.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / fadeDuration);
            yield return null;
        }

        cg.alpha = targetAlpha;
        cg.interactable = open;
        cg.blocksRaycasts = open;

        if (!open)
        {
            cg.gameObject.SetActive(false);
            if (affectTimeScale) Time.timeScale = 1f;
        }
        else
        {
            if (affectTimeScale) Time.timeScale = 0f; // Jeda game saat World Map dibuka lewat NPC
        }
    }

    // --- PANGGIL FUNGSI INI DARI TOMBOL PETA ---
    public void LoadIslandScene(string sceneName)
    {
        BoatTransitionController.NavigateToIsland(sceneName);
    }
}
