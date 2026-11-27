using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class CanvasHUDAutoBinder : MonoBehaviour
{
    private static CanvasHUDAutoBinder instance;
    public static CanvasHUDAutoBinder Instance => instance;

    private GameObject currentJournalButtonGo;

    [Header("Sprite References")]
    public Sprite journalButtonSprite;
    public Sprite pauseButtonSprite;
    public Sprite controlHudLandSprite;
    public Sprite controlHudUnderwaterSprite;
    public Sprite controlHudIslandSprite;
    public Sprite controlHudAirstripSprite;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoInitialize()
    {
        if (instance == null)
        {
            GameObject go = new GameObject("CanvasHUDAutoBinder");
            instance = go.AddComponent<CanvasHUDAutoBinder>();
            DontDestroyOnLoad(go);
        }
        instance.EnsureHUDInCurrentScene();
    }

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);
        }
        else if (instance != this)
        {
            Destroy(gameObject);
            return;
        }

        SceneManager.sceneLoaded += OnSceneLoaded;
        LoadDefaultSprites();
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        StartCoroutine(EnsureHUDNextFrame());
    }

    private IEnumerator EnsureHUDNextFrame()
    {
        yield return null;
        EnsureHUDInCurrentScene();
    }

    private void LoadDefaultSprites()
    {
        // Cari sprite dari seluruh sprite yang telah termuat jika belum di-assign
        if (journalButtonSprite == null
            || pauseButtonSprite == null
            || controlHudLandSprite == null
            || controlHudUnderwaterSprite == null
            || controlHudIslandSprite == null
            || controlHudAirstripSprite == null)
        {
            Sprite[] allSprites = Resources.FindObjectsOfTypeAll<Sprite>();
            foreach (var s in allSprites)
            {
                if (s == null) continue;
                if (journalButtonSprite == null && s.name.Equals("Journal Button", System.StringComparison.OrdinalIgnoreCase))
                    journalButtonSprite = s;
                else if (pauseButtonSprite == null && s.name.Equals("Pause Button", System.StringComparison.OrdinalIgnoreCase))
                    pauseButtonSprite = s;
                else if (controlHudIslandSprite == null && s.name.Equals("Control Hud_6 Pulau", System.StringComparison.OrdinalIgnoreCase))
                    controlHudIslandSprite = s;
                else if (controlHudLandSprite == null && s.name.Equals("Control Hud_All Pulau", System.StringComparison.OrdinalIgnoreCase))
                    controlHudLandSprite = s;
                else if (controlHudAirstripSprite == null && s.name.Equals("Control Hud_Airstrip_Prolog", System.StringComparison.OrdinalIgnoreCase))
                    controlHudAirstripSprite = s;
                else if (controlHudUnderwaterSprite == null && s.name.Equals("Control Hud_Underwater", System.StringComparison.OrdinalIgnoreCase))
                    controlHudUnderwaterSprite = s;
            }
        }
    }

    private Sprite GetFallbackControlHudSprite(string sceneName)
    {
        if (sceneName.Contains("Laut"))
        {
            return controlHudUnderwaterSprite != null ? controlHudUnderwaterSprite : controlHudLandSprite;
        }
        if (sceneName.Contains("Airstrip"))
        {
            return controlHudAirstripSprite != null ? controlHudAirstripSprite : controlHudLandSprite;
        }
        if (sceneName.Contains("Waisai"))
        {
            return controlHudLandSprite != null ? controlHudLandSprite : controlHudIslandSprite;
        }
        return controlHudIslandSprite != null ? controlHudIslandSprite : controlHudLandSprite;
    }

    private static void ApplyControlHudLayout(RectTransform rt, Sprite sprite)
    {
        if (rt == null) return;
        float w = sprite != null ? sprite.rect.width : 1067f;
        float h = sprite != null ? sprite.rect.height : 54f;
        float sx = Mathf.Abs(rt.localScale.x) > 0.001f ? Mathf.Abs(rt.localScale.x) : 1f;
        float sy = Mathf.Abs(rt.localScale.y) > 0.001f ? Mathf.Abs(rt.localScale.y) : 1f;
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(w / sx, h / sy);
        rt.anchoredPosition = new Vector2(920.5f - w * 0.5f, -517f + h * 0.5f);
    }

    public void EnsureHUDInCurrentScene()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        string sceneName = activeScene.name;
        StandardizePromptPanelAndQTE2(activeScene);
        // Jangan pasang di MainMenu atau perahu transisi
        if (sceneName.Contains("MainMenu") || sceneName.Contains("BoatToIsland")) return;

        EnsureCameraConfinerBound();
        LoadDefaultSprites();

        GameObject hudCanvas = GameObject.Find("Canvas_HUD");
        if (hudCanvas == null)
        {
            hudCanvas = CreateHUDCanvas(sceneName);
        }
        else
        {
            ConfigureExistingHUD(hudCanvas, sceneName);
        }
    }

    private void EnsureCameraConfinerBound()
    {
        GameObject boundsObj = GameObject.Find("CameraBounds");
        if (boundsObj == null) boundsObj = GameObject.Find("CameraBounds Variant");
        if (boundsObj == null) return;

        PolygonCollider2D boundsPoly = boundsObj.GetComponent<PolygonCollider2D>();
        if (boundsPoly == null) return;

        var confiners = FindObjectsByType<Unity.Cinemachine.CinemachineConfiner2D>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var confiner in confiners)
        {
            if (confiner != null && confiner.BoundingShape2D == null)
            {
                confiner.BoundingShape2D = boundsPoly;
                confiner.InvalidateBoundingShapeCache();
            }
        }
    }

    private void ConfigureExistingHUD(GameObject hudCanvas, string sceneName)
    {
        bool isUnderwater = sceneName.Contains("Laut");

        // 1. Pastikan Canvas_HUD menggunakan Screen Space - Overlay persis acuan 10_Gag
        Canvas canvas = hudCanvas.GetComponent<Canvas>();
        if (canvas != null)
        {
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 0;
        }

        // 2. Pastikan CanvasScaler Scale With Screen Size 1920x1080 (Match 0.5) persis acuan 10_Gag
        CanvasScaler scaler = hudCanvas.GetComponent<CanvasScaler>();
        if (scaler != null)
        {
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            scaler.referencePixelsPerUnit = 100f;
        }

        // 3. Hubungkan Journal Button & tata letak persis acuan 10_Gag (633, 405, size 126x139)
        Transform btnJournalTr = hudCanvas.transform.Find("Button");
        if (btnJournalTr != null)
        {
            RectTransform rt = btnJournalTr.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = new Vector2(633f, 405f);
                rt.sizeDelta = new Vector2(126f, 139f);
                rt.localScale = Vector3.one;
            }

            Button btn = btnJournalTr.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.RemoveListener(OnJournalButtonClicked);
                btn.onClick.AddListener(OnJournalButtonClicked);
            }
            if (journalButtonSprite != null)
            {
                Image img = btnJournalTr.GetComponent<Image>();
                if (img != null && img.sprite == null) img.sprite = journalButtonSprite;
            }
            currentJournalButtonGo = btnJournalTr.gameObject;
        }
        RefreshJournalButtonState();

        // 4. Hubungkan Pause Button & tata letak persis acuan 10_Gag (841.4, 405, size 127x139)
        Transform btnPauseTr = hudCanvas.transform.Find("Button (1)");
        if (btnPauseTr != null)
        {
            RectTransform rt = btnPauseTr.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = new Vector2(841.4f, 405f);
                rt.sizeDelta = new Vector2(127f, 139f);
                rt.localScale = Vector3.one;
            }

            Button btn = btnPauseTr.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.RemoveListener(OnPauseButtonClicked);
                btn.onClick.AddListener(OnPauseButtonClicked);
            }
            if (pauseButtonSprite != null)
            {
                Image img = btnPauseTr.GetComponent<Image>();
                if (img != null && img.sprite == null) img.sprite = pauseButtonSprite;
            }
        }

        // 5. Atur Control HUD tanpa menimpa sprite/posisi yang sudah dikonfigurasi di scene
        Transform ctrlTr = hudCanvas.transform.Find("Control Hud_All Pulau");
        if (ctrlTr == null) ctrlTr = hudCanvas.transform.Find("Control Hud_Underwater");
        if (ctrlTr == null) ctrlTr = hudCanvas.transform.Find("Control Hud_6 Pulau");
        if (ctrlTr == null)
        {
            for (int i = 0; i < hudCanvas.transform.childCount; i++)
            {
                Transform child = hudCanvas.transform.GetChild(i);
                if (child != null && child.name.StartsWith("Control Hud", System.StringComparison.OrdinalIgnoreCase))
                {
                    ctrlTr = child;
                    break;
                }
            }
        }

        if (ctrlTr != null)
        {
            RectTransform rt = ctrlTr.GetComponent<RectTransform>();
            Image img = ctrlTr.GetComponent<Image>();
            if (img != null)
            {
                img.preserveAspect = true;
                if (img.sprite == null)
                {
                    Sprite fallbackSprite = GetFallbackControlHudSprite(sceneName);
                    if (fallbackSprite != null)
                    {
                        img.sprite = fallbackSprite;
                        ApplyControlHudLayout(rt, fallbackSprite);
                    }
                }
                else if (isUnderwater && controlHudUnderwaterSprite != null && img.sprite != controlHudUnderwaterSprite)
                {
                    img.sprite = controlHudUnderwaterSprite;
                    ApplyControlHudLayout(rt, controlHudUnderwaterSprite);
                    ctrlTr.name = "Control Hud_Underwater";
                }
            }
        }
    }

    private GameObject CreateHUDCanvas(string sceneName)
    {
        bool isUnderwater = sceneName.Contains("Laut");

        GameObject canvasGo = new GameObject("Canvas_HUD");
        Canvas canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;

        CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        canvasGo.AddComponent<GraphicRaycaster>();

        // 1. Journal Button
        GameObject journalBtnGo = new GameObject("Button");
        journalBtnGo.transform.SetParent(canvasGo.transform, false);
        RectTransform rtJournal = journalBtnGo.AddComponent<RectTransform>();
        rtJournal.anchorMin = new Vector2(0.5f, 0.5f);
        rtJournal.anchorMax = new Vector2(0.5f, 0.5f);
        rtJournal.pivot = new Vector2(0.5f, 0.5f);
        rtJournal.anchoredPosition = new Vector2(633f, 405f);
        rtJournal.sizeDelta = new Vector2(126f, 139f);

        Image imgJournal = journalBtnGo.AddComponent<Image>();
        if (journalButtonSprite != null) imgJournal.sprite = journalButtonSprite;
        Button btnJournal = journalBtnGo.AddComponent<Button>();
        btnJournal.onClick.AddListener(OnJournalButtonClicked);
        currentJournalButtonGo = journalBtnGo;
        RefreshJournalButtonState();

        // 2. Pause Button
        GameObject pauseBtnGo = new GameObject("Button (1)");
        pauseBtnGo.transform.SetParent(canvasGo.transform, false);
        RectTransform rtPause = pauseBtnGo.AddComponent<RectTransform>();
        rtPause.anchorMin = new Vector2(0.5f, 0.5f);
        rtPause.anchorMax = new Vector2(0.5f, 0.5f);
        rtPause.pivot = new Vector2(0.5f, 0.5f);
        rtPause.anchoredPosition = new Vector2(841.4f, 405f);
        rtPause.sizeDelta = new Vector2(127f, 139f);

        Image imgPause = pauseBtnGo.AddComponent<Image>();
        if (pauseButtonSprite != null) imgPause.sprite = pauseButtonSprite;
        Button btnPause = pauseBtnGo.AddComponent<Button>();
        btnPause.onClick.AddListener(OnPauseButtonClicked);

        // 3. Control Hud
        Sprite fallbackCtrlSprite = GetFallbackControlHudSprite(sceneName);
        GameObject ctrlGo = new GameObject(isUnderwater ? "Control Hud_Underwater" : "Control Hud_All Pulau");
        ctrlGo.transform.SetParent(canvasGo.transform, false);
        RectTransform rtCtrl = ctrlGo.AddComponent<RectTransform>();
        rtCtrl.localScale = Vector3.one;
        ApplyControlHudLayout(rtCtrl, fallbackCtrlSprite);

        Image imgCtrl = ctrlGo.AddComponent<Image>();
        imgCtrl.sprite = fallbackCtrlSprite;
        imgCtrl.preserveAspect = true;

        Debug.Log($"[CanvasHUDAutoBinder] Berhasil membuat Canvas_HUD otomatis untuk scene '{sceneName}'.");
        return canvasGo;
    }

    public void RefreshJournalButtonState()
    {
        bool unlocked = QuestJournalManager.IsJournalUnlockedPersistent();
        if (currentJournalButtonGo == null)
        {
            GameObject hudCanvas = GameObject.Find("Canvas_HUD");
            if (hudCanvas != null)
            {
                Transform btnTr = hudCanvas.transform.Find("Button");
                if (btnTr != null) currentJournalButtonGo = btnTr.gameObject;
            }
        }
        if (currentJournalButtonGo != null)
        {
            currentJournalButtonGo.SetActive(unlocked);
        }
    }

    private static void StandardizePromptPanelAndQTE2(Scene scene)
    {
        if (!scene.IsValid() || scene.name == "10_Gag" || scene.name == "01_MainMenu") return;

        GameObject[] roots = scene.GetRootGameObjects();
        for (int r = 0; r < roots.Length; r++)
        {
            if (roots[r] == null) continue;
            Transform[] descendants = roots[r].GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < descendants.Length; i++)
            {
                Transform tr = descendants[i];
                if (tr == null) continue;

                if (tr.name == "PromptPanel")
                {
                    RectTransform rt = tr as RectTransform;
                    if (rt != null)
                    {
                        float ratio = GetCanvasReferenceWidthRatio(tr);
                        rt.anchorMin = new Vector2(0.5f, 0.5f);
                        rt.anchorMax = new Vector2(0.5f, 0.5f);
                        rt.pivot = new Vector2(0.5f, 0.5f);
                        rt.sizeDelta = new Vector2(1152f, 558f);
                        rt.anchoredPosition = new Vector2(122.3f * ratio, 21.3f * ratio);
                        rt.localScale = new Vector3(0.20485361f * ratio, 0.20485361f * ratio, 1f);
                    }
                }
            }

            QTE2[] qtes = roots[r].GetComponentsInChildren<QTE2>(true);
            for (int i = 0; i < qtes.Length; i++)
            {
                if (qtes[i] == null) continue;
                Transform parent = qtes[i].transform.parent;
                if (parent == null || parent.GetComponent<Canvas>() == null) continue;

                RectTransform rt = qtes[i].transform as RectTransform;
                if (rt != null)
                {
                    float ratio = GetCanvasReferenceWidthRatio(qtes[i].transform);
                    rt.anchorMin = new Vector2(0.5f, 0.5f);
                    rt.anchorMax = new Vector2(0.5f, 0.5f);
                    rt.pivot = new Vector2(0.5f, 0.5f);
                    rt.anchoredPosition = new Vector2(107f * ratio, -41f * ratio);
                    rt.localScale = new Vector3(1.0731555f * ratio, 1.0731555f * ratio, 1f);
                }
            }
        }
    }

    private static float GetCanvasReferenceWidthRatio(Transform target)
    {
        CanvasScaler scaler = target != null ? target.GetComponentInParent<CanvasScaler>(true) : null;
        if (scaler != null && scaler.uiScaleMode == CanvasScaler.ScaleMode.ScaleWithScreenSize && scaler.referenceResolution.x > 0f)
        {
            return scaler.referenceResolution.x / 800f;
        }
        return 1f;
    }

    private void OnJournalButtonClicked()
    {
        if (QuestJournalManager.Instance != null)
        {
            QuestJournalManager.Instance.ToggleQuestTab();
        }
        else
        {
            QuestJournalManager qjm = FindFirstObjectByType<QuestJournalManager>();
            if (qjm != null) qjm.ToggleQuestTab();
        }
    }

    private void OnPauseButtonClicked()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.TogglePause();
            return;
        }

        PauseManager pm = FindFirstObjectByType<PauseManager>(FindObjectsInactive.Include);
        if (pm != null)
        {
            if (pm.IsPaused) pm.ResumeGame();
            else pm.PauseGame();
        }
    }
}

