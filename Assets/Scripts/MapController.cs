using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

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

    [Header("Standard MapMenuPanel Layout (Waisai Reference)")]
    [SerializeField] private bool normalizeMapPanelToWaisaiLayout = true;
    [SerializeField] private Vector3 standardMapPanelScale = new Vector3(0.83738f, 0.83738f, 0.83738f);
    [SerializeField] private float standardIslandsScaleOn800x600 = 0.7f;
    [SerializeField] private bool autoBindMissingIslandButtons = true;

    [Header("Current Island Marker (KAMU DI SINI)")]
    [SerializeField] private RectTransform currentLocationMarker;
    [SerializeField] private TMP_Text currentLocationText;
    [SerializeField] private string currentLocationLabel = "KAMU DI SINI";
    [SerializeField] private Vector2 markerOffset = new Vector2(0f, 52f);
    [SerializeField] private Vector2 markerSize = new Vector2(154f, 36f);
    [SerializeField] private Color markerBackgroundColor = new Color(0.86f, 0.22f, 0.16f, 0.96f);
    [SerializeField] private Color markerTextColor = Color.white;
    [SerializeField] private bool animateLocationMarker = true;

    private bool isMinimapOpen = true; // Default tampil di layar
    private Coroutine minimapFadeRoutine;
    private Coroutine worldMapFadeRoutine;
    private Vector2 markerBaseAnchoredPos;

    void Start()
    {
        EnsureStandardMapPanelSetup();
        UpdateCurrentLocationMarker();

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

        if (animateLocationMarker && currentLocationMarker != null &&
            worldMapCanvasGroup != null && worldMapCanvasGroup.gameObject.activeInHierarchy &&
            currentLocationMarker.gameObject.activeInHierarchy)
        {
            float pulse = Mathf.Sin(Time.unscaledTime * 3.5f);
            currentLocationMarker.anchoredPosition = markerBaseAnchoredPos + new Vector2(0f, pulse * 4f);
            float scale = 1f + pulse * 0.04f;
            currentLocationMarker.localScale = new Vector3(scale, scale, 1f);
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

        EnsureStandardMapPanelSetup();
        UpdateCurrentLocationMarker();
        if (worldMapFadeRoutine != null) StopCoroutine(worldMapFadeRoutine);
        worldMapFadeRoutine = StartCoroutine(FadeGroup(worldMapCanvasGroup, true, true));
    }

    private static readonly (string buttonName, string sceneName)[] StandardIslandButtonBindings =
    {
        ("Btn_Waisai", "02_Waisai"),
        ("Btn_Piaynemo", "03_Pianemo"),
        ("Btn_Pianemo", "03_Pianemo"),
        ("Btn_Misool", "04_Misool"),
        ("Btn_Arborek", "05_Arborek"),
        ("Btn_Sawinggrai", "06_Sawinggrai"),
        ("Btn_Friwen", "07_Friwen"),
        ("Btn_Gag", "10_Gag")
    };

    private bool buttonsConfiguredOnce = false;

    public void EnsureStandardMapPanelSetup()
    {
        if (worldMapCanvasGroup == null) return;

        if (normalizeMapPanelToWaisaiLayout)
        {
            RectTransform panelRt = worldMapCanvasGroup.GetComponent<RectTransform>();
            if (panelRt != null)
            {
                panelRt.anchorMin = Vector2.zero;
                panelRt.anchorMax = Vector2.one;
                panelRt.pivot = new Vector2(0.5f, 0.5f);
                panelRt.sizeDelta = Vector2.zero;
                panelRt.anchoredPosition = Vector2.zero;
                panelRt.localScale = standardMapPanelScale;
            }

            Transform islandsContainer = FindChildByName(worldMapCanvasGroup.transform, "IslandsContainer");
            if (islandsContainer != null)
            {
                float refWidth = 800f;
                Canvas parentCanvas = worldMapCanvasGroup.GetComponentInParent<Canvas>(true);
                if (parentCanvas != null)
                {
                    CanvasScaler scaler = parentCanvas.GetComponent<CanvasScaler>();
                    if (scaler != null && scaler.uiScaleMode == CanvasScaler.ScaleMode.ScaleWithScreenSize && scaler.referenceResolution.x > 1f)
                    {
                        refWidth = scaler.referenceResolution.x;
                    }
                }

                float targetScale = standardIslandsScaleOn800x600 * (refWidth / 800f);
                islandsContainer.localScale = new Vector3(targetScale, targetScale, 1f);
            }
        }

        // Nonaktifkan tombol duplikat legacy (Btn_Pulau5 / Btn_Pulau6) agar tidak menelan klik Btn_Gag / Btn_Friwen
        DisableBlockingDuplicateButton("Btn_Pulau5");
        DisableBlockingDuplicateButton("Btn_Pulau6");

        if (autoBindMissingIslandButtons && !buttonsConfiguredOnce)
        {
            buttonsConfiguredOnce = true;
            for (int i = 0; i < StandardIslandButtonBindings.Length; i++)
            {
                var entry = StandardIslandButtonBindings[i];
                Transform btnTr = FindChildByName(worldMapCanvasGroup.transform, entry.buttonName);
                if (btnTr == null) continue;

                Button btn = btnTr.GetComponent<Button>();
                if (btn == null) continue;

                Image img = btnTr.GetComponent<Image>();
                if (img != null) img.raycastTarget = true;

                if (!HasValidPersistentIslandClick(btn, entry.sceneName))
                {
                    string sceneTarget = entry.sceneName;
                    btn.onClick.AddListener(() => LoadIslandScene(sceneTarget));
                }
            }
        }
    }

    private void DisableBlockingDuplicateButton(string duplicateName)
    {
        Transform dupTr = FindChildByName(worldMapCanvasGroup.transform, duplicateName);
        if (dupTr == null) return;

        Image dupImg = dupTr.GetComponent<Image>();
        if (dupImg != null) dupImg.raycastTarget = false;
        dupTr.gameObject.SetActive(false);
    }

    private static bool HasValidPersistentIslandClick(Button btn, string expectedScene)
    {
        int count = btn.onClick.GetPersistentEventCount();
        if (count <= 0) return false;

        for (int i = 0; i < count; i++)
        {
            UnityEngine.Object target = btn.onClick.GetPersistentTarget(i);
            string method = btn.onClick.GetPersistentMethodName(i);
            if (target != null && (method == "PindahKePulau" || method == "LoadIslandScene"))
            {
                // Jangan anggap valid bila Btn_Gag masih mengarah ke 06_Sawinggrai atau null target
                if (btn.name == "Btn_Gag" && target is MapTeleport)
                {
                    // Biarkan MapGagUnlocker tetap bekerja atau fallback jika masih kosong
                    return true;
                }
                if (!string.IsNullOrEmpty(expectedScene))
                {
                    return true;
                }
            }
        }
        return false;
    }

    public void UpdateCurrentLocationMarker()
    {
        if (worldMapCanvasGroup == null) return;

        string targetButtonName = ResolveCurrentIslandButtonName(SceneManager.GetActiveScene().name);
        Transform targetButton = !string.IsNullOrEmpty(targetButtonName)
            ? FindChildByName(worldMapCanvasGroup.transform, targetButtonName)
            : null;

        if (targetButton == null && targetButtonName == "Btn_Piaynemo")
        {
            targetButton = FindChildByName(worldMapCanvasGroup.transform, "Btn_Pianemo");
        }

        if (targetButton == null)
        {
            if (currentLocationMarker != null)
            {
                currentLocationMarker.gameObject.SetActive(false);
            }
            return;
        }

        EnsureLocationMarker();
        if (currentLocationMarker == null) return;

        currentLocationMarker.SetParent(targetButton, false);
        currentLocationMarker.SetAsLastSibling();
        currentLocationMarker.anchorMin = new Vector2(0.5f, 0.5f);
        currentLocationMarker.anchorMax = new Vector2(0.5f, 0.5f);
        currentLocationMarker.pivot = new Vector2(0.5f, 0.5f);
        currentLocationMarker.localScale = Vector3.one;
        currentLocationMarker.sizeDelta = markerSize;
        markerBaseAnchoredPos = markerOffset;
        currentLocationMarker.anchoredPosition = markerBaseAnchoredPos;

        Image bg = currentLocationMarker.GetComponent<Image>();
        if (bg != null)
        {
            bg.color = markerBackgroundColor;
        }

        if (currentLocationText == null)
        {
            currentLocationText = currentLocationMarker.GetComponentInChildren<TMP_Text>(true);
        }

        if (currentLocationText != null)
        {
            currentLocationText.text = currentLocationLabel;
            currentLocationText.color = markerTextColor;
        }

        currentLocationMarker.gameObject.SetActive(true);
    }

    private void EnsureLocationMarker()
    {
        if (currentLocationMarker != null) return;

        Transform existing = FindChildByName(worldMapCanvasGroup.transform, "CurrentLocationMarker");
        if (existing != null)
        {
            currentLocationMarker = existing.GetComponent<RectTransform>();
            if (currentLocationMarker != null)
            {
                currentLocationText = existing.GetComponentInChildren<TMP_Text>(true);
                return;
            }
        }

        GameObject markerGo = new GameObject("CurrentLocationMarker", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Outline));
        currentLocationMarker = markerGo.GetComponent<RectTransform>();
        currentLocationMarker.SetParent(worldMapCanvasGroup.transform, false);
        currentLocationMarker.sizeDelta = markerSize;

        Image bg = markerGo.GetComponent<Image>();
        bg.color = markerBackgroundColor;
        bg.raycastTarget = false;

        Outline outline = markerGo.GetComponent<Outline>();
        outline.effectColor = new Color(0.18f, 0.06f, 0.04f, 0.95f);
        outline.effectDistance = new Vector2(1.5f, -1.5f);

        GameObject textGo = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        RectTransform textRt = textGo.GetComponent<RectTransform>();
        textRt.SetParent(currentLocationMarker, false);
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = new Vector2(8f, 2f);
        textRt.offsetMax = new Vector2(-8f, -2f);

        TextMeshProUGUI tmp = textGo.GetComponent<TextMeshProUGUI>();
        TMP_Text refTmp = worldMapCanvasGroup.GetComponentInChildren<TMP_Text>(true);
        if (refTmp != null && refTmp.font != null)
        {
            tmp.font = refTmp.font;
        }
        tmp.text = currentLocationLabel;
        tmp.color = markerTextColor;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.enableAutoSizing = true;
        tmp.fontSizeMin = 11f;
        tmp.fontSizeMax = 18f;
        tmp.raycastTarget = false;
        currentLocationText = tmp;
    }

    private static string ResolveCurrentIslandButtonName(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName)) return null;
        string lower = sceneName.ToLowerInvariant();
        if (lower.Contains("waisai")) return "Btn_Waisai";
        if (lower.Contains("pianemo") || lower.Contains("piaynemo")) return "Btn_Piaynemo";
        if (lower.Contains("arborek")) return "Btn_Arborek";
        if (lower.Contains("misool")) return "Btn_Misool";
        if (lower.Contains("sawinggrai")) return "Btn_Sawinggrai";
        if (lower.Contains("friwen")) return "Btn_Friwen";
        if (lower.Contains("gag")) return "Btn_Gag";
        return null;
    }

    private static Transform FindChildByName(Transform root, string targetName)
    {
        if (root == null || string.IsNullOrEmpty(targetName)) return null;
        if (root.name == targetName) return root;
        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindChildByName(root.GetChild(i), targetName);
            if (found != null) return found;
        }
        return null;
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
        if (worldMapFadeRoutine != null) StopCoroutine(worldMapFadeRoutine);
        worldMapFadeRoutine = StartCoroutine(FadeGroup(worldMapCanvasGroup, false, true));
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
