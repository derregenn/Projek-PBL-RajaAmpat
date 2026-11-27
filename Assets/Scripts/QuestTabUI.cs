using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Helper component yang ditaruh di GameObject root QuestTabUI.
/// Menghubungkan semua referensi visual di dalam prefab ke QuestJournalManager secara otomatis
/// saat prefab ini muncul di scene manapun, memastikan jurnal selalu bekerja tanpa perlu setup manual per scene!
/// </summary>
public class QuestTabUI : MonoBehaviour
{
    [Header("Panel Background & Close")]
    public Image backgroundImage;
    public Button closeButton;

    [Header("Progress Tracking (Kiri Atas)")]
    public TMPro.TextMeshProUGUI progressPercentageText;

    [Header("Quest Folder & Tabs")]
    public Image folderImage;
    public Button tabSwitchButton;
    public TMPro.TextMeshProUGUI ongoingQuestText;
    public Image questIconImage;
    public GameObject questBoxPanel;
    public GameObject completedCard2;
    public GameObject completedCard3;
    public TMPro.TextMeshProUGUI completedText2;
    public TMPro.TextMeshProUGUI completedText3;

    [Header("Halaman Jurnal")]
    public Image journalPageImage;
    public Image journalPhotoImage;
    public TMPro.TextMeshProUGUI pageNumberText;
    public Image pageStatusBg;
    public Image journalLabel;
    public Button prevPageButton;
    public Button nextPageButton;

    [Header("Island Awards")]
    public Image awardArborek;
    public Image awardFriwen;
    public Image awardGag;
    public Image awardMisool;
    public Image awardPiaynemo;
    public Image awardSawinggrai;
    public Image awardRajaAmpat;
    public Image awardSecretEnding;
    public Sprite awardRajaAmpatLockedSprite;
    public Sprite awardRajaAmpatUnlockedSprite;
    public Sprite awardSecretEndingLockedSprite;
    public Sprite awardSecretEndingUnlockedSprite;
    public TMPro.TextMeshProUGUI trashStatusText;

    [Header("Canvas Sorting")]
    [Tooltip("Sorting order to ensure QuestTabUI renders above Canvas_HUD (order 0)")]
    public int journalSortingOrder = 30;

    private void Awake()
    {
        EnsureDropShadow();
        EnsureStandardTransform();
        EnsureCanvasSortingOrder();
        EnsureStandardPageIndicator();
        RegisterToJournalManager();
    }

    private void Start()
    {
        EnsureDropShadow();
        EnsureStandardTransform();
        EnsureCanvasSortingOrder();
        EnsureStandardPageIndicator();
        RegisterToJournalManager();
        StartCoroutine(RefreshStandardTransformNextFrame());
    }

    private void OnEnable()
    {
        EnsureDropShadow();
        EnsureStandardTransform();
        EnsureCanvasSortingOrder();
        EnsureStandardPageIndicator();
        RegisterToJournalManager();
    }

    private void OnRectTransformDimensionsChange()
    {
        if (gameObject.activeInHierarchy)
        {
            EnsureStandardTransform();
        }
    }

    private System.Collections.IEnumerator RefreshStandardTransformNextFrame()
    {
        yield return null;
        EnsureStandardTransform();
    }

    private static void ClearPersistentListeners(UnityEngine.Events.UnityEvent evt)
    {
        if (evt == null) return;
        int count = evt.GetPersistentEventCount();
        for (int i = 0; i < count; i++)
        {
            evt.SetPersistentListenerState(i, UnityEngine.Events.UnityEventCallState.Off);
        }
    }

    public void EnsureDropShadow()
    {
        if (backgroundImage == null) backgroundImage = GetComponent<Image>();
        if (backgroundImage == null) return;

        Shadow shadow = backgroundImage.GetComponent<Shadow>();
        if (shadow == null)
        {
            shadow = backgroundImage.gameObject.AddComponent<Shadow>();
        }
        shadow.effectColor = new Color(0f, 0f, 0f, 0.45f);
        shadow.effectDistance = new Vector2(10f, -12f);
        shadow.useGraphicAlpha = true;
    }

    public void EnsureStandardTransform()
    {
        RectTransform rt = GetComponent<RectTransform>();
        if (rt == null) return;

        float factor = ComputeParentCanvasScaleFactor();

        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(1197.1f, 566.82f);
        rt.localScale = new Vector3(0.6356795f * factor, 0.6666441f * factor, 0.9768732f * factor);
        rt.anchoredPosition = new Vector2(0.45938f * factor, 0f);
    }

    private float ComputeParentCanvasScaleFactor()
    {
        if (transform.parent == null) return 1f;

        Canvas rootParentCanvas = transform.parent.GetComponentInParent<Canvas>(true);
        if (rootParentCanvas == null) return 1f;

        RectTransform canvasRt = rootParentCanvas.GetComponent<RectTransform>();
        CanvasScaler scaler = rootParentCanvas.GetComponent<CanvasScaler>();

        float canvasWidth = canvasRt != null ? canvasRt.rect.width : 0f;
        float canvasHeight = canvasRt != null ? canvasRt.rect.height : 0f;

        if (canvasWidth <= 10f || canvasHeight <= 10f)
        {
            float sw = Screen.width > 0 ? Screen.width : 1920f;
            float sh = Screen.height > 0 ? Screen.height : 1080f;
            float aspect = sw / sh;

            if (scaler != null && scaler.uiScaleMode == CanvasScaler.ScaleMode.ScaleWithScreenSize &&
                scaler.referenceResolution.x > 0f && scaler.referenceResolution.y > 0f)
            {
                float wFromWidth = scaler.referenceResolution.x;
                float wFromHeight = scaler.referenceResolution.y * aspect;
                canvasWidth = Mathf.Lerp(wFromWidth, wFromHeight, scaler.matchWidthOrHeight);
                canvasHeight = canvasWidth / aspect;
            }
            else
            {
                canvasWidth = sw;
                canvasHeight = sh;
            }
        }

        if (canvasWidth > 10f && canvasHeight > 10f)
        {
            return Mathf.Clamp(Mathf.Min(canvasWidth / 800f, canvasHeight / 450f), 0.5f, 4f);
        }

        return 1f;
    }

    public void EnsureCanvasSortingOrder()
    {
        Canvas canvas = GetComponent<Canvas>();
        if (canvas == null)
        {
            canvas = gameObject.AddComponent<Canvas>();
        }
        canvas.overrideSorting = true;
        canvas.sortingOrder = journalSortingOrder;

        GraphicRaycaster raycaster = GetComponent<GraphicRaycaster>();
        if (raycaster == null)
        {
            raycaster = gameObject.AddComponent<GraphicRaycaster>();
        }
    }

    public void EnsureStandardPageIndicator()
    {
        Transform rightPanel = transform.Find("RightPanel");
        if (rightPanel == null && journalPageImage != null)
        {
            rightPanel = journalPageImage.transform.parent;
        }
        if (rightPanel == null) return;

        if (pageStatusBg == null)
        {
            Transform pillTr = rightPanel.Find("PageStatusPill");
            if (pillTr != null) pageStatusBg = pillTr.GetComponent<Image>();
        }

        if (pageStatusBg != null)
        {
            if (!pageStatusBg.gameObject.activeSelf) pageStatusBg.gameObject.SetActive(true);
            pageStatusBg.enabled = true;
            pageStatusBg.color = Color.white;
            pageStatusBg.raycastTarget = false;

            RectTransform pillRt = pageStatusBg.rectTransform;
            if (pillRt != null)
            {
                pillRt.anchorMin = new Vector2(0.5f, 0.5f);
                pillRt.anchorMax = new Vector2(0.5f, 0.5f);
                pillRt.pivot = new Vector2(0.5f, 0.5f);
                pillRt.anchoredPosition = new Vector2(-40f, -141.46f);
                pillRt.sizeDelta = new Vector2(88f, 24f);
                pillRt.localScale = Vector3.one;
            }
        }

        if (pageNumberText == null)
        {
            Transform pageTextTr = rightPanel.Find("PageNumberText");
            if (pageTextTr != null)
            {
                pageNumberText = pageTextTr.GetComponent<TMPro.TextMeshProUGUI>();
            }
        }

        if (pageNumberText == null)
        {
            GameObject pageGo = new GameObject("PageNumberText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TMPro.TextMeshProUGUI));
            pageGo.layer = rightPanel.gameObject.layer;
            pageGo.transform.SetParent(rightPanel, false);
            pageNumberText = pageGo.GetComponent<TMPro.TextMeshProUGUI>();

            TMPro.TextMeshProUGUI fontRef = progressPercentageText != null
                ? progressPercentageText
                : (ongoingQuestText != null ? ongoingQuestText : trashStatusText);
            if (fontRef != null)
            {
                if (fontRef.font != null) pageNumberText.font = fontRef.font;
                if (fontRef.fontSharedMaterial != null) pageNumberText.fontSharedMaterial = fontRef.fontSharedMaterial;
            }
            pageNumberText.text = "Page 1";
        }

        if (pageNumberText != null)
        {
            if (!pageNumberText.gameObject.activeSelf) pageNumberText.gameObject.SetActive(true);
            pageNumberText.enabled = true;

            if (pageStatusBg != null && pageNumberText.transform.parent == pageStatusBg.transform.parent)
            {
                int pillIndex = pageStatusBg.transform.GetSiblingIndex();
                if (pageNumberText.transform.GetSiblingIndex() <= pillIndex)
                {
                    pageNumberText.transform.SetSiblingIndex(pillIndex + 1);
                }
            }

            RectTransform textRt = pageNumberText.rectTransform;
            if (textRt != null)
            {
                textRt.anchorMin = new Vector2(0.5f, 0.5f);
                textRt.anchorMax = new Vector2(0.5f, 0.5f);
                textRt.pivot = new Vector2(0.5f, 0.5f);
                textRt.anchoredPosition = new Vector2(-40f, -141.46f);
                textRt.sizeDelta = new Vector2(84f, 22f);
                textRt.localScale = Vector3.one;
            }

            pageNumberText.color = Color.white;
            pageNumberText.fontStyle = TMPro.FontStyles.Bold;
            pageNumberText.alignment = TMPro.TextAlignmentOptions.Center;
            pageNumberText.textWrappingMode = TMPro.TextWrappingModes.NoWrap;
            pageNumberText.overflowMode = TMPro.TextOverflowModes.Overflow;
            pageNumberText.enableAutoSizing = true;
            pageNumberText.fontSizeMin = 10f;
            pageNumberText.fontSizeMax = 13.5f;
            pageNumberText.fontSize = 13f;
            pageNumberText.margin = Vector4.zero;
            pageNumberText.raycastTarget = false;

            if (string.IsNullOrWhiteSpace(pageNumberText.text))
            {
                pageNumberText.text = "Page 1";
            }
        }
    }

    public void RegisterToJournalManager()
    {
        EnsureCanvasSortingOrder();
        EnsureStandardPageIndicator();
        EnsureSpecialAwardsInCollectibles();

        QuestJournalManager manager = QuestJournalManager.Instance;
        if (manager == null)
        {
            manager = FindFirstObjectByType<QuestJournalManager>(FindObjectsInactive.Include);
            if (manager != null && !manager.gameObject.activeInHierarchy)
            {
                manager.gameObject.SetActive(true);
            }
        }

        if (manager != null)
        {
            manager.questTabPanel = this.gameObject;
            if (progressPercentageText != null) manager.progressPercentageText = progressPercentageText;
            if (folderImage != null) manager.folderImage = folderImage;
            if (tabSwitchButton != null) manager.tabSwitchButton = tabSwitchButton;
            if (ongoingQuestText != null) manager.ongoingQuestText = ongoingQuestText;
            if (questIconImage != null) manager.questIconImage = questIconImage;
            if (questBoxPanel != null) manager.questBoxPanel = questBoxPanel;
            if (completedCard2 != null) manager.completedCard2 = completedCard2;
            if (completedCard3 != null) manager.completedCard3 = completedCard3;
            if (completedText2 != null) manager.completedText2 = completedText2;
            if (completedText3 != null) manager.completedText3 = completedText3;
            if (awardRajaAmpat != null) manager.awardRajaAmpat = awardRajaAmpat;
            if (awardSecretEnding != null) manager.awardSecretEnding = awardSecretEnding;
            if (awardRajaAmpatLockedSprite != null) manager.awardRajaAmpatLockedSprite = awardRajaAmpatLockedSprite;
            if (awardRajaAmpatUnlockedSprite != null) manager.awardRajaAmpatUnlockedSprite = awardRajaAmpatUnlockedSprite;
            if (awardSecretEndingLockedSprite != null) manager.awardSecretEndingLockedSprite = awardSecretEndingLockedSprite;
            if (awardSecretEndingUnlockedSprite != null) manager.awardSecretEndingUnlockedSprite = awardSecretEndingUnlockedSprite;
            if (trashStatusText != null) manager.trashStatusText = trashStatusText;
            if (journalPageImage != null) manager.journalPageImage = journalPageImage;
            if (journalPhotoImage != null) manager.journalPhotoImage = journalPhotoImage;
            if (pageNumberText != null) manager.pageNumberText = pageNumberText;
            if (prevPageButton != null) manager.btnPrevPage = prevPageButton;
            if (nextPageButton != null) manager.btnNextPage = nextPageButton;
            if (closeButton != null) manager.btnCloseJournal = closeButton;

            // Sambungkan button OnClick (matikan persistent scene listeners lama agar tidak double/stale)
            if (closeButton != null)
            {
                ClearPersistentListeners(closeButton.onClick);
                closeButton.onClick.RemoveListener(manager.CloseJournal);
                closeButton.onClick.AddListener(manager.CloseJournal);
            }
            if (prevPageButton != null)
            {
                ClearPersistentListeners(prevPageButton.onClick);
                prevPageButton.onClick.RemoveListener(manager.PrevPage);
                prevPageButton.onClick.AddListener(manager.PrevPage);
            }
            if (nextPageButton != null)
            {
                ClearPersistentListeners(nextPageButton.onClick);
                nextPageButton.onClick.RemoveListener(manager.NextPage);
                nextPageButton.onClick.AddListener(manager.NextPage);
            }
            if (tabSwitchButton != null)
            {
                ClearPersistentListeners(tabSwitchButton.onClick);
                tabSwitchButton.onClick.RemoveListener(manager.ToggleQuestTab);
                tabSwitchButton.onClick.AddListener(manager.ToggleQuestTab);
            }

            // Sambungkan Award Images ke data manager
            AssignAwardImage(manager, IslandName.Arborek, awardArborek);
            AssignAwardImage(manager, IslandName.Friwen, awardFriwen);
            AssignAwardImage(manager, IslandName.Gag, awardGag);
            AssignAwardImage(manager, IslandName.Misool, awardMisool);
            AssignAwardImage(manager, IslandName.Piaynemo, awardPiaynemo);
            AssignAwardImage(manager, IslandName.Sawinggrai, awardSawinggrai);

            manager.UpdateAllJournalSystems();
        }
    }

    private void EnsureSpecialAwardsInCollectibles()
    {
        Transform collectiblesTransform = transform.Find("Collectibles");
        if (collectiblesTransform == null && awardGag != null)
        {
            collectiblesTransform = awardGag.transform.parent;
        }
        if (collectiblesTransform == null && awardPiaynemo != null)
        {
            collectiblesTransform = awardPiaynemo.transform.parent;
        }

        if (awardSecretEnding == null && collectiblesTransform != null)
        {
            awardSecretEnding = FindOrCreateSpecialAward(
                collectiblesTransform,
                new[] { "Award_SecretEnding", "Award_Secret Ending" },
                "Award_SecretEnding",
                new Vector2(290f, 0f)
            );
        }

        if (awardRajaAmpat == null && collectiblesTransform != null)
        {
            awardRajaAmpat = FindOrCreateSpecialAward(
                collectiblesTransform,
                new[] { "Award_Tampat", "Award_RajaAmpat" },
                "Award_Tampat",
                new Vector2(405f, 0f)
            );
        }

        NormalizeSpecialAwardTransform(awardSecretEnding);
        NormalizeSpecialAwardTransform(awardRajaAmpat);
    }

    private Image FindOrCreateSpecialAward(Transform parent, string[] candidateNames, string createName, Vector2 anchoredPosition)
    {
        for (int i = 0; i < candidateNames.Length; i++)
        {
            Transform existing = parent.Find(candidateNames[i]);
            if (existing != null)
            {
                Image existingImage = existing.GetComponent<Image>();
                if (existingImage != null) return existingImage;
            }
        }

        GameObject awardObj = new GameObject(createName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        awardObj.layer = parent.gameObject.layer;
        awardObj.transform.SetParent(parent, false);

        RectTransform rt = awardObj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPosition;
        rt.sizeDelta = new Vector2(87f, 76f);
        rt.localScale = new Vector3(1f, 4.65f, 1f);

        Image img = awardObj.GetComponent<Image>();
        img.preserveAspect = true;
        return img;
    }

    private void NormalizeSpecialAwardTransform(Image img)
    {
        if (img == null) return;
        img.preserveAspect = true;
        if (img.rectTransform != null)
        {
            img.rectTransform.localScale = new Vector3(1f, 4.65f, 1f);
        }
    }

    private void AssignAwardImage(QuestJournalManager manager, IslandName island, Image img)
    {
        if (img == null) return;
        img.preserveAspect = true;
        if (img.rectTransform != null)
        {
            img.rectTransform.localScale = new Vector3(1f, 4.65f, 1f);
        }

        if (manager.islandAwards == null) manager.islandAwards = new System.Collections.Generic.List<IslandAwardData>();
        var found = manager.islandAwards.Find(a => a.island == island);
        if (found != null)
        {
            found.awardImage = img;
        }
        else
        {
            manager.islandAwards.Add(new IslandAwardData
            {
                island = island,
                displayName = island.ToString(),
                awardImage = img
            });
        }
    }
}

