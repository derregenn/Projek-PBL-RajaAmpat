using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class CanvasHUDAutoBinder : MonoBehaviour
{
    private static CanvasHUDAutoBinder instance;

    [Header("Sprite References")]
    public Sprite journalButtonSprite;
    public Sprite pauseButtonSprite;
    public Sprite controlHudLandSprite;
    public Sprite controlHudUnderwaterSprite;

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
        if (journalButtonSprite == null || pauseButtonSprite == null || controlHudLandSprite == null || controlHudUnderwaterSprite == null)
        {
            Sprite[] allSprites = Resources.FindObjectsOfTypeAll<Sprite>();
            foreach (var s in allSprites)
            {
                if (s == null) continue;
                if (journalButtonSprite == null && s.name.Equals("Journal Button", System.StringComparison.OrdinalIgnoreCase))
                    journalButtonSprite = s;
                else if (pauseButtonSprite == null && s.name.Equals("Pause Button", System.StringComparison.OrdinalIgnoreCase))
                    pauseButtonSprite = s;
                else if (controlHudLandSprite == null && s.name.Equals("Control Hud_All Pulau", System.StringComparison.OrdinalIgnoreCase))
                    controlHudLandSprite = s;
                else if (controlHudUnderwaterSprite == null && (s.name.Equals("Control Hud_Underwater", System.StringComparison.OrdinalIgnoreCase) || s.name.StartsWith("Control Hud_Underwater_")))
                    controlHudUnderwaterSprite = s;
            }
        }
    }

    public void EnsureHUDInCurrentScene()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        // Jangan pasang di MainMenu atau perahu transisi
        if (sceneName.Contains("MainMenu") || sceneName.Contains("BoatToIsland")) return;

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
        }

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

        // 5. Atur Control HUD (Land vs Underwater) persis acuan 10_Gag (519, -490)
        Transform ctrlTr = hudCanvas.transform.Find("Control Hud_All Pulau");
        if (ctrlTr == null) ctrlTr = hudCanvas.transform.Find("Control Hud_Underwater");

        if (ctrlTr != null)
        {
            RectTransform rt = ctrlTr.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = new Vector2(519f, -490f);
            }

            Image img = ctrlTr.GetComponent<Image>();
            if (img != null)
            {
                if (isUnderwater && controlHudUnderwaterSprite != null)
                {
                    img.sprite = controlHudUnderwaterSprite;
                    ctrlTr.name = "Control Hud_Underwater";
                }
                else if (!isUnderwater && controlHudLandSprite != null)
                {
                    img.sprite = controlHudLandSprite;
                    ctrlTr.name = "Control Hud_All Pulau";
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
        GameObject ctrlGo = new GameObject(isUnderwater ? "Control Hud_Underwater" : "Control Hud_All Pulau");
        ctrlGo.transform.SetParent(canvasGo.transform, false);
        RectTransform rtCtrl = ctrlGo.AddComponent<RectTransform>();
        rtCtrl.anchorMin = new Vector2(0.5f, 0.5f);
        rtCtrl.anchorMax = new Vector2(0.5f, 0.5f);
        rtCtrl.pivot = new Vector2(0.5f, 0.5f);
        rtCtrl.anchoredPosition = new Vector2(519f, -490f);
        rtCtrl.sizeDelta = new Vector2(803f, 54f);

        Image imgCtrl = ctrlGo.AddComponent<Image>();
        imgCtrl.sprite = (isUnderwater && controlHudUnderwaterSprite != null) ? controlHudUnderwaterSprite : controlHudLandSprite;

        Debug.Log($"[CanvasHUDAutoBinder] Berhasil membuat Canvas_HUD otomatis untuk scene '{sceneName}'.");
        return canvasGo;
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
        PauseManager pm = FindFirstObjectByType<PauseManager>();
        if (pm != null)
        {
            pm.PauseGame();
        }
    }
}

