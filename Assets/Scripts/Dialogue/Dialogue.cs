using UnityEngine;
using TMPro;
using System.Collections;
using Unity.Cinemachine;
using UnityEngine.Events;
using System;
using UnityEngine.UI;

public class Dialogue : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject InteractPrompt;
    [SerializeField] private GameObject DialogueBox;
    [SerializeField] private TMP_Text DialogueText;
    [SerializeField] private GameObject PlayerDialogueBox;
    [SerializeField] private TMP_Text PlayerDialogueText;
    [SerializeField] private GameObject NextPrompt;
    [SerializeField] private GameObject PlayerNextPrompt;

    [Header("Dialogue")]
    [SerializeField] private string[] DialogueLines;
    [SerializeField] private DialogueSpeaker[] DialogueLineSpeakers;
    [SerializeField] private string triviaQuestionText;
    [SerializeField] private float TypeSpeed = 0.02f;

    [Header("Typing Audio")]
    [SerializeField] private AudioSource typingAudioSource;
    [SerializeField] private AudioClip typingAudioClip;
    [SerializeField, Range(0f, 1f)] private float typingAudioVolume = 1f;
    [SerializeField, Min(0f)] private float typingAudioInterval = 0.06f;
    private float nextTypingAudioTime;

    private enum DialogueSpeaker
    {
        NPC,
        Player
    }

    [Header("NPC Sprite Settings")]
    [SerializeField] private SpriteRenderer npcSpriteRenderer;
    [SerializeField] private Sprite talkingSprite1; 
    [SerializeField] private Sprite talkingSprite2; 
    [SerializeField] private Sprite idleSprite;      
    [SerializeField] private int spriteToggleInterval = 3; 

    [Header("Camera Zoom Settings")]
    public CinemachineCamera dialogueCamera;
    [Tooltip("Ukuran zoom kamera dialog (Orthographic Size). Atur manual per NPC di Inspector.")]
    [SerializeField] private float npcLensSize = 2.0f;
    [Tooltip("Durasi transisi blend zoom kamera (dalam detik).")]
    [SerializeField] private float zoomTransitionDuration = 1.0f;

    [Header("Dialogue Events")]
    [SerializeField] private UnityEvent onDialogueEnd;

    [Header("Post Quest / Free Roam Loop Dialogue")]
    [Tooltip("Dialog berulang yang dimainkan terus saat berinteraksi setelah quest selesai atau di mode free roam")]
    [SerializeField] private string[] postQuestLoopLines;
    [SerializeField] private bool isPostQuestLoop = false;

    private int lineIndex = 0;
    private bool isInteracting = false;
    private bool canInteract = false;
    private bool isPlayerInCollider = false; // Penjaga status posisi player
    private bool isTyping = false;
    private bool hasBeenTriggered = false; 

    private QuestGiver questGiver;
    private Coroutine typingCoroutine;
    private Action onDialogueCompleteCallback;
    
    // Referensi ke Player Controller untuk dimatikan saat dialog
    private MonoBehaviour playerMovementScript;

    [Header("Interaction Settings")]
    [SerializeField] private float interactDistance = 1.8f;

    private GameObject cachedPlayerObj;

    private void EnsureInteractPrompt()
    {
        if (InteractPrompt == null)
        {
            Transform[] all = GetComponentsInChildren<Transform>(true);
            foreach (Transform t in all)
            {
                if (t.name == "InteractPrompt")
                {
                    InteractPrompt = t.gameObject;
                    break;
                }
            }
        }
    }

    private Canvas FindDialogueCanvas()
    {
        Transform t = transform.Find("DialogueCanvas");
        if (t != null)
        {
            Canvas c = t.GetComponent<Canvas>();
            if (c != null) return c;
        }
        if (DialogueBox != null && DialogueBox.transform.parent != null)
        {
            Canvas c = DialogueBox.transform.parent.GetComponent<Canvas>();
            if (c != null) return c;
        }
        Canvas[] all = GetComponentsInChildren<Canvas>(true);
        foreach (var c in all)
        {
            if (c.gameObject.name.IndexOf("DialogueCanvas", StringComparison.OrdinalIgnoreCase) >= 0)
                return c;
        }
        return all.Length > 0 ? all[0] : null;
    }

    private void RemoveRoguePromptCanvas()
    {
        if (InteractPrompt == null) return;
        Canvas promptCanvas = InteractPrompt.GetComponent<Canvas>();
        if (promptCanvas != null)
        {
            promptCanvas.enabled = false;
            if (Application.isPlaying)
            {
                Destroy(promptCanvas);
            }
#if UNITY_EDITOR
            else if (!UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
            {
                UnityEditor.EditorApplication.delayCall += () =>
                {
                    if (promptCanvas != null)
                    {
                        DestroyImmediate(promptCanvas, true);
                    }
                };
            }
#endif
        }
    }

    private void EnsureInteractPromptHierarchy()
    {
        EnsureInteractPrompt();

        if (InteractPrompt != null)
        {
            // Pastikan InteractPrompt DI DALAM DialogueCanvas, tapi DI LUAR DialogueBox
            Canvas dialogueCanvas = FindDialogueCanvas();
            Transform canvasTransform = dialogueCanvas != null ? dialogueCanvas.transform : null;

            if (DialogueBox != null)
            {
                if (canvasTransform == null) canvasTransform = DialogueBox.transform.parent;

                // Jika InteractPrompt berada di dalam DialogueBox, pindahkan keluar menjadi sibling
                if (InteractPrompt.transform.IsChildOf(DialogueBox.transform) && InteractPrompt.transform != DialogueBox.transform)
                {
                    InteractPrompt.transform.SetParent(canvasTransform, false);
                }
            }

            if (canvasTransform != null && InteractPrompt.transform.parent != canvasTransform)
            {
                InteractPrompt.transform.SetParent(canvasTransform, false);
            }

            RemoveRoguePromptCanvas();

            // Pastikan posisi & scale InteractPrompt sesuai standar Waisai (identik dengan TourGuide)
            RectTransform promptRt = InteractPrompt.GetComponent<RectTransform>();
            if (promptRt != null)
            {
                promptRt.anchorMin = new Vector2(0.5f, 0f);
                promptRt.anchorMax = new Vector2(0.5f, 0f);
                promptRt.pivot = new Vector2(0.5f, 0f);
                promptRt.sizeDelta = new Vector2(200f, 50f);
                promptRt.localScale = Vector3.one;
                promptRt.anchoredPosition = new Vector2(0f, 15f);
                promptRt.localPosition = new Vector3(0f, 15f, 0f);
            }

            TMP_Text promptText = InteractPrompt.GetComponent<TMP_Text>();
            if (promptText != null)
            {
                promptText.alignment = TextAlignmentOptions.Center;
                promptText.enableWordWrapping = false;
            }
        }
    }

    private float CalculateTargetWorldBottomY(GameObject characterObj, float pScaleY)
    {
        float headTopWorld = 0f;

        // Cari tinggi kepala dari Collider fisik non-trigger (badan karakter)
        Collider2D[] colliders = characterObj.GetComponentsInChildren<Collider2D>();
        foreach (var col in colliders)
        {
            if (col != null && !col.isTrigger)
            {
                float top = col.bounds.max.y - characterObj.transform.position.y;
                if (top > headTopWorld && top < 2.5f) headTopWorld = top;
            }
        }

        // Jika belum terdeteksi, gunakan SpriteRenderer
        if (headTopWorld < 0.2f)
        {
            SpriteRenderer sr = characterObj.GetComponent<SpriteRenderer>();
            if (sr == null) sr = characterObj.GetComponentInChildren<SpriteRenderer>();
            if (sr != null && sr.bounds.size.y > 0.1f)
            {
                float srTop = sr.bounds.max.y - characterObj.transform.position.y;
                if (srTop > 0.2f && srTop < 2.5f) headTopWorld = srTop;
            }
        }

        // Fallback standar proporsional jika bounds tidak terdeteksi
        if (headTopWorld < 0.2f)
        {
            headTopWorld = 1.05f * (pScaleY / 0.26f);
        }

        // Jarak celah pas 0.10 unit (~12px) tepat di atas kepala/rambut karakter untuk dasar (bottom) kotak dialog
        return headTopWorld + 0.10f;
    }

    private bool CheckIsCutscene()
    {
        string currentSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        return currentSceneName.IndexOf("cutscene", StringComparison.OrdinalIgnoreCase) >= 0
            || currentSceneName.IndexOf("boat", StringComparison.OrdinalIgnoreCase) >= 0
            || gameObject.name.IndexOf("cutscene", StringComparison.OrdinalIgnoreCase) >= 0
            || (transform.parent != null && transform.parent.name.IndexOf("cutscene", StringComparison.OrdinalIgnoreCase) >= 0);
    }

    private void EnsureWaisaiDialogueStandard()
    {
        // 1. Standar Canvas (Normalisasi World Scale & Offset agar identik di SEMUA NPC di SEMUA SCENE)
        Canvas dialogueCanvas = FindDialogueCanvas();
        if (dialogueCanvas != null)
        {
            RectTransform cRt = dialogueCanvas.GetComponent<RectTransform>();
            if (cRt != null)
            {
                // Canvas dibuat presisi 300 x 102.69 dengan pivot di bawah (0.5, 0)
                cRt.sizeDelta = new Vector2(300f, 102.69f);
                cRt.pivot = new Vector2(0.5f, 0f);
                cRt.anchorMin = new Vector2(0.5f, 0f);
                cRt.anchorMax = new Vector2(0.5f, 0f);

                float pScaleX = Mathf.Abs(transform.lossyScale.x);
                float pScaleY = Mathf.Abs(transform.lossyScale.y);
                if (pScaleX <= 0.0001f) pScaleX = 1f;
                if (pScaleY <= 0.0001f) pScaleY = 1f;

                const float targetWorldScale = 0.00818f;

                // Hitung posisi pas tepat di atas kepala karakter
                float targetBottomWorldY = CalculateTargetWorldBottomY(gameObject, pScaleY);

                float signX = Mathf.Sign(transform.lossyScale.x);
                cRt.localScale = new Vector3((targetWorldScale / pScaleX) * signX, targetWorldScale / pScaleY, 1f);
                cRt.anchoredPosition = new Vector2(0f, targetBottomWorldY / pScaleY);
            }
        }

        // 2. Hubungkan NextPrompt jika belum terhubung
        if (DialogueBox != null && NextPrompt == null)
        {
            Transform btnNext = DialogueBox.transform.Find("NextButton");
            if (btnNext == null) btnNext = DialogueBox.transform.Find("Btn_Next");
            if (btnNext == null) btnNext = DialogueBox.transform.Find("NextPrompt");
            if (btnNext != null)
            {
                NextPrompt = btnNext.gameObject;
            }
        }

        // 3. Konfigurasi DialogueBox, Text, dan NextPrompt dengan standar Waisai (Auto ContentSizeFitter & No-Overlap)
        ConfigureDialogueBoxLayout(DialogueBox, DialogueText, NextPrompt);

        // 3. Standar Audio Typing
        if (typingAudioSource == null)
        {
            typingAudioSource = GetComponent<AudioSource>();
            if (typingAudioSource == null)
            {
                typingAudioSource = gameObject.AddComponent<AudioSource>();
                typingAudioSource.playOnAwake = false;
                typingAudioSource.spatialBlend = 0f;
            }
        }

        if (typingAudioClip == null)
        {
            if (typingAudioSource != null && typingAudioSource.clip != null)
            {
                typingAudioClip = typingAudioSource.clip;
            }
            else
            {
                typingAudioClip = Resources.Load<AudioClip>("Dialog_TypingSlow");
            }
        }

        TypeSpeed = 0.04f;

        // 4. Standar Dialogue Player & Auto Binding Inspector
        EnsurePlayerDialogueReferencesAndStandard();
    }

    public void EnsurePlayerDialogueReferencesAndStandard()
    {
        if (cachedPlayerObj == null)
        {
            cachedPlayerObj = GameObject.FindGameObjectWithTag("Player");
            if (cachedPlayerObj == null) cachedPlayerObj = GameObject.Find("Player");
        }
        if (cachedPlayerObj == null) return;

        // Cari DialogueCanvas pada objek Player
        Transform pCanvasTr = cachedPlayerObj.transform.Find("DialogueCanvas");
        if (pCanvasTr == null)
        {
            Canvas[] pCanvases = cachedPlayerObj.GetComponentsInChildren<Canvas>(true);
            foreach (var c in pCanvases)
            {
                if (c.gameObject.name.IndexOf("DialogueCanvas", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    pCanvasTr = c.transform;
                    break;
                }
            }
        }

        if (pCanvasTr != null)
        {
            // Pastikan Canvas Renderer mode World Space dan Sorting Layer Foreground
            Canvas pCanvas = pCanvasTr.GetComponent<Canvas>();
            if (pCanvas != null && pCanvas.renderMode == RenderMode.WorldSpace)
            {
                pCanvas.overrideSorting = true;
                pCanvas.sortingLayerName = "Foreground";
                pCanvas.sortingOrder = 100;
                if (pCanvas.worldCamera == null) pCanvas.worldCamera = Camera.main;
            }

            // Cari DialogueBox pada Player
            Transform pBoxTr = pCanvasTr.Find("DialogueBox");
            if (pBoxTr == null)
            {
                foreach (Transform child in pCanvasTr)
                {
                    if (child.name.IndexOf("DialogueBox", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        pBoxTr = child;
                        break;
                    }
                }
            }

            if (pBoxTr != null)
            {
                // Sambungkan inspector references jika null atau salah referensi
                if (PlayerDialogueBox == null || PlayerDialogueBox.transform.root != cachedPlayerObj.transform.root)
                {
                    PlayerDialogueBox = pBoxTr.gameObject;
                }

                Transform pTextTr = pBoxTr.Find("Dialogue");
                if (pTextTr == null) pTextTr = pBoxTr.Find("Text (TMP)");
                if (pTextTr == null)
                {
                    TMP_Text[] texts = pBoxTr.GetComponentsInChildren<TMP_Text>(true);
                    foreach (var txt in texts)
                    {
                        if (txt.gameObject.name != "NextButton" && txt.transform.parent.name != "NextButton")
                        {
                            pTextTr = txt.transform;
                            break;
                        }
                    }
                }

                if (pTextTr != null && (PlayerDialogueText == null || PlayerDialogueText.transform.root != cachedPlayerObj.transform.root))
                {
                    PlayerDialogueText = pTextTr.GetComponent<TMP_Text>();
                }

                Transform pNextTr = pBoxTr.Find("NextButton");
                if (pNextTr == null) pNextTr = pBoxTr.Find("Btn_Next");
                if (pNextTr == null) pNextTr = pBoxTr.Find("NextPrompt");
                if (pNextTr != null && (PlayerNextPrompt == null || PlayerNextPrompt.transform.root != cachedPlayerObj.transform.root))
                {
                    PlayerNextPrompt = pNextTr.gameObject;
                }

                // Normalisasi Player Dialogue Canvas & Box
                NormalizePlayerDialogueUI(cachedPlayerObj, pCanvasTr, pBoxTr, PlayerDialogueText, PlayerNextPrompt);
            }
        }
    }

    private void ConfigureDialogueBoxLayout(GameObject boxObj, TMP_Text textObj, GameObject nextObj)
    {
        if (boxObj == null) return;

        // 1. DialogueBox RectTransform: Pivot (0.5, 0)
        RectTransform boxRt = boxObj.GetComponent<RectTransform>();
        if (boxRt != null)
        {
            boxRt.anchorMin = new Vector2(0.5f, 0f);
            boxRt.anchorMax = new Vector2(0.5f, 0f);
            boxRt.pivot = new Vector2(0.5f, 0f);
            boxRt.sizeDelta = new Vector2(300f, 102.69f);
            boxRt.anchoredPosition = Vector2.zero;
            boxRt.localScale = Vector3.one;
        }

        // 2. ContentSizeFitter: Vertical = PreferredSize (melebar ke atas secara dinamis)
        ContentSizeFitter csf = boxObj.GetComponent<ContentSizeFitter>();
        if (csf == null) csf = boxObj.AddComponent<ContentSizeFitter>();
        csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // 3. VerticalLayoutGroup: Spacing & child control
        VerticalLayoutGroup vlg = boxObj.GetComponent<VerticalLayoutGroup>();
        if (vlg != null)
        {
            vlg.padding = new RectOffset(12, 12, 12, 12);
            vlg.spacing = 6f;
            vlg.childAlignment = TextAnchor.MiddleCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false; // Kritis: jangan paksakan stretch height pada tombol next
        }

        // 4. Text Formatting
        if (textObj != null)
        {
            RectTransform textRt = textObj.GetComponent<RectTransform>();
            if (textRt != null)
            {
                textRt.sizeDelta = new Vector2(280f, textRt.sizeDelta.y);
            }
            textObj.fontSize = 18f;
            textObj.fontSizeMin = 14f;
            textObj.fontSizeMax = 18f;
            textObj.enableWordWrapping = true;
            textObj.alignment = TextAlignmentOptions.Center;
            textObj.fontStyle = FontStyles.Normal;
        }

        // 5. NextPrompt Layout & Text
        if (nextObj != null)
        {
            RectTransform nextRt = nextObj.GetComponent<RectTransform>();
            if (nextRt != null)
            {
                nextRt.localScale = Vector3.one;
            }

            LayoutElement le = nextObj.GetComponent<LayoutElement>();
            if (le == null) le = nextObj.AddComponent<LayoutElement>();
            le.minHeight = 24f;
            le.preferredHeight = 28f;
            le.flexibleHeight = 0f;

            ConfigureNextPromptVisual(nextObj);
        }
    }

    private void ConfigureNextPromptVisual(GameObject nextObj)
    {
        if (nextObj == null) return;

        // 1. Matikan seluruh Image background agar tidak ada kotak warna hijau atau apapun di Next button
        Image[] allImgs = nextObj.GetComponentsInChildren<Image>(true);
        foreach (var img in allImgs)
        {
            if (img != null)
            {
                img.enabled = false;
                img.color = Color.clear;
            }
        }

        // 2. Set semua Button colors menjadi transparan
        Button[] allBtns = nextObj.GetComponentsInChildren<Button>(true);
        foreach (var btn in allBtns)
        {
            if (btn != null)
            {
                ColorBlock cb = btn.colors;
                cb.normalColor = Color.clear;
                cb.highlightedColor = Color.clear;
                cb.pressedColor = Color.clear;
                cb.selectedColor = Color.clear;
                cb.disabledColor = Color.clear;
                btn.colors = cb;
            }
        }

        // 3. Pastikan teks "Next [E]" putih bersih dan berada di tengah tanpa background box
        TMP_Text nextText = nextObj.GetComponentInChildren<TMP_Text>();
        if (nextText != null)
        {
            nextText.fontSize = 14f;
            nextText.alignment = TextAlignmentOptions.Center;
            nextText.color = Color.white;
        }
    }

    private void NormalizePlayerDialogueUI(GameObject playerObj, Transform pCanvasTr, Transform pBoxTr, TMP_Text pText, GameObject pNext)
    {
        float plScaleX = Mathf.Abs(playerObj.transform.lossyScale.x);
        float plScaleY = Mathf.Abs(playerObj.transform.lossyScale.y);
        if (plScaleX <= 0.0001f) plScaleX = 1f;
        if (plScaleY <= 0.0001f) plScaleY = 1f;

        const float targetWorldScale = 0.00818f;

        // Hitung posisi pas tepat di atas kepala Player
        float targetBottomWorldY = CalculateTargetWorldBottomY(playerObj, plScaleY);

        RectTransform pcRt = pCanvasTr.GetComponent<RectTransform>();
        if (pcRt != null)
        {
            // Canvas dibuat presisi 300 x 102.69 dengan pivot di bawah (0.5, 0)
            pcRt.sizeDelta = new Vector2(300f, 102.69f);
            pcRt.pivot = new Vector2(0.5f, 0f);
            pcRt.anchorMin = new Vector2(0.5f, 0f);
            pcRt.anchorMax = new Vector2(0.5f, 0f);
            float signX = Mathf.Sign(playerObj.transform.lossyScale.x);
            pcRt.localScale = new Vector3((targetWorldScale / plScaleX) * signX, targetWorldScale / plScaleY, 1f);
            pcRt.anchoredPosition = new Vector2(0f, targetBottomWorldY / plScaleY);
        }

        if (pBoxTr != null)
        {
            ConfigureDialogueBoxLayout(pBoxTr.gameObject, pText, pNext);
        }
    }

    private void EnsureDialogueCanvasSorting()
    {
        Canvas[] canvases = GetComponentsInChildren<Canvas>(true);
        foreach (Canvas c in canvases)
        {
            if (c.renderMode == RenderMode.WorldSpace)
            {
                c.overrideSorting = true;
                c.sortingLayerName = "Foreground";
                c.sortingOrder = 100;
                if (c.worldCamera == null)
                {
                    c.worldCamera = Camera.main;
                }
            }
        }

        RemoveRoguePromptCanvas();
    }private void CheckProximity()
    {
        if (isInteracting) return;

        if (cachedPlayerObj == null)
        {
            cachedPlayerObj = GameObject.FindGameObjectWithTag("Player");
            if (cachedPlayerObj == null) cachedPlayerObj = GameObject.Find("Player");
        }

        if (cachedPlayerObj != null)
        {
            bool inProximity = false;
            Collider2D col = GetComponent<Collider2D>();
            if (col == null) col = GetComponentInChildren<Collider2D>();
            if (col != null)
            {
                Vector2 closestPoint = col.ClosestPoint(cachedPlayerObj.transform.position);
                float distToCol = Vector2.Distance(closestPoint, cachedPlayerObj.transform.position);
                inProximity = distToCol <= 0.8f; // Mengikuti presisi batas collider NPC
            }
            else
            {
                float dist = Vector2.Distance(transform.position, cachedPlayerObj.transform.position);
                inProximity = dist <= interactDistance;
            }

            if ((inProximity || isPlayerInCollider) && !CheckIsCutscene())
            {
                canInteract = true;
                if (InteractPrompt != null && !InteractPrompt.activeSelf)
                {
                    EnsureInteractPromptHierarchy();
                    InteractPrompt.SetActive(true);
                }
            }
            else if (!isPlayerInCollider && !inProximity)
            {
                if (canInteract)
                {
                    canInteract = false;
                    if (InteractPrompt != null && InteractPrompt.activeSelf)
                    {
                        InteractPrompt.SetActive(false);
                    }
                }
            }
        }
    }

    private void SetPlayerMovementLock(bool isLocked)
    {
        GameObject pObj = cachedPlayerObj != null ? cachedPlayerObj : GameObject.FindGameObjectWithTag("Player");
        if (pObj == null) pObj = GameObject.Find("Player");
        if (pObj != null)
        {
            cachedPlayerObj = pObj;

            Player player = pObj.GetComponent<Player>();
            if (player != null)
            {
                player.isMovementLocked = isLocked;
                player.enabled = true; // Tetap aktif agar Update() menahan velocity di 0 dan animasi Idle
            }

            PlayerSwim swim = pObj.GetComponent<PlayerSwim>();
            if (swim != null)
            {
                swim.isMovementLocked = isLocked;
            }

            Rigidbody2D rb = pObj.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
            }

            Animator anim = pObj.GetComponent<Animator>();
            if (anim != null && isLocked)
            {
                if (player != null && player.isSwimming)
                    anim.Play("Player_swim");
                else
                    anim.Play("Player_Idle");
            }
        }
    }

    private void OnValidate()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.delayCall += () =>
        {
            if (this == null) return;
            EnsureInteractPromptHierarchy();
            EnsureDialogueCanvasSorting();
            EnsureWaisaiDialogueStandard();
        };
#endif
    }

    private void Awake()
    {
        questGiver = GetComponent<QuestGiver>();

        if (npcSpriteRenderer == null)
        {
            npcSpriteRenderer = GetComponent<SpriteRenderer>();
        }

        if (npcSpriteRenderer != null && idleSprite == null)
        {
            idleSprite = npcSpriteRenderer.sprite;
        }
        
        EnsureInteractPromptHierarchy();
        EnsureDialogueCanvasSorting();
        EnsureWaisaiDialogueStandard();

        // Cari skrip Player pada objek ber-tag Player
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            cachedPlayerObj = playerObj;
            playerMovementScript = playerObj.GetComponent<Player>(); 
        }

        // Pastikan dialogue camera terhubung
        EnsureDialogueCamera();
    }

    private void OnEnable()
    {
        EnsureInteractPromptHierarchy();
        EnsureDialogueCanvasSorting();
        EnsureWaisaiDialogueStandard();
    }

    private void Start()
    {
        EnsureInteractPromptHierarchy();
        EnsureDialogueCanvasSorting();
        EnsureWaisaiDialogueStandard();
        EnsureDialogueCamera();
        if (dialogueCamera != null)
        {
            dialogueCamera.Priority.Value = 0;
        }

        bool isMapNPC = GetComponent<NPCMapTrigger>() != null || gameObject.name.IndexOf("Map", StringComparison.OrdinalIgnoreCase) >= 0;
        if (!isMapNPC && postQuestLoopLines != null && postQuestLoopLines.Length > 0)
        {
            bool isFreeRoam = PlayerPrefs.GetInt("GameCompleted", 0) == 1;
            if (isFreeRoam)
            {
                SwitchToLoopDialogue();
            }
        }

        if (!isPlayerInCollider && InteractPrompt != null) InteractPrompt.SetActive(false);
        if (DialogueBox != null) DialogueBox.SetActive(false);
        if (PlayerDialogueBox != null) PlayerDialogueBox.SetActive(false);
        if (NextPrompt != null) NextPrompt.SetActive(false);
        if (PlayerNextPrompt != null) PlayerNextPrompt.SetActive(false);
    }

    private void Update()
    {
        CheckProximity();

        // 1. Memulai dialog manual dengan memencet E
        if (canInteract && !isInteracting && Input.GetKeyDown(KeyCode.E))
        {
            StartDialogue();
        }
        // 2. Lanjut dialog
        else if (isInteracting && Input.GetKeyDown(KeyCode.E)
            && ((NextPrompt != null && NextPrompt.activeInHierarchy)
                || (PlayerNextPrompt != null && PlayerNextPrompt.activeInHierarchy)))
        {
            NextLine();
        }
    }

    public void StartDialogue()
    {
        if (isInteracting) return;

        EnsureWaisaiDialogueStandard();

        if (InteractPrompt != null) InteractPrompt.SetActive(false);

        canInteract = false; 
        isInteracting = true;
        lineIndex = 0;

        // --- KUNCI GERAKAN PLAYER & NOLKAN VELOCITY ---
        SetPlayerMovementLock(true);

        ApplyDialogueCameraZoom();

        if (PlayerDialogueBox != null) PlayerDialogueBox.SetActive(false);
        if (NextPrompt != null) NextPrompt.SetActive(false);
        if (PlayerNextPrompt != null) PlayerNextPrompt.SetActive(false);

        StartTyping();
    }

    public void StartDialogue(string[] npcDialogueLines, Action onComplete)
    {
        onDialogueCompleteCallback = onComplete;
        DialogueLines = npcDialogueLines;

        DialogueLineSpeakers = new DialogueSpeaker[npcDialogueLines.Length];
        for (int i = 0; i < DialogueLineSpeakers.Length; i++)
        {
            DialogueLineSpeakers[i] = DialogueSpeaker.NPC;
        }

        lineIndex = 0;
        StartDialogue();
    }

    private void StartTyping()
    {
        if (typingCoroutine != null) StopCoroutine(typingCoroutine);
        if (typingAudioSource != null) typingAudioSource.Stop();
        typingCoroutine = StartCoroutine(WriteLine());
    }

    private IEnumerator WriteLine()
    {
        isTyping = true;

        if (DialogueLines == null || DialogueLines.Length == 0)
        {
            EndDialogue();
            yield break;
        }

        bool isPlayerLine = DialogueLineSpeakers != null
            && lineIndex < DialogueLineSpeakers.Length
            && DialogueLineSpeakers[lineIndex] == DialogueSpeaker.Player;
        TMP_Text activeText = isPlayerLine ? PlayerDialogueText : DialogueText;

        if (DialogueBox != null) DialogueBox.SetActive(!isPlayerLine);
        if (PlayerDialogueBox != null) PlayerDialogueBox.SetActive(isPlayerLine);
        if (activeText == null)
        {
            isTyping = false;
            EndDialogue();
            yield break;
        }

        activeText.text = DialogueLines[lineIndex];
        activeText.maxVisibleCharacters = 0;

        Canvas.ForceUpdateCanvases();
        if (DialogueBox != null && DialogueBox.activeSelf)
        {
            RectTransform bRt = DialogueBox.GetComponent<RectTransform>();
            if (bRt != null) UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(bRt);
        }
        if (PlayerDialogueBox != null && PlayerDialogueBox.activeSelf)
        {
            RectTransform pbRt = PlayerDialogueBox.GetComponent<RectTransform>();
            if (pbRt != null) UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(pbRt);
        }

        int totalCharacters = DialogueLines[lineIndex].Length;
        bool toggleSprite = false;

        for (int i = 0; i <= totalCharacters; i++)
        {
            activeText.maxVisibleCharacters = i;

            if (i < totalCharacters && typingAudioSource != null && typingAudioClip != null
                && Time.time >= nextTypingAudioTime && !typingAudioSource.isPlaying)
            {
                typingAudioSource.PlayOneShot(typingAudioClip, typingAudioVolume);
                nextTypingAudioTime = Time.time + typingAudioInterval;
            }

            if (!isPlayerLine && npcSpriteRenderer != null && talkingSprite1 != null && talkingSprite2 != null)
            {
                if (i % spriteToggleInterval == 0)
                {
                    toggleSprite = !toggleSprite;
                    npcSpriteRenderer.sprite = toggleSprite ? talkingSprite1 : talkingSprite2;
                }
            }

            if (i < totalCharacters)
            {
                yield return new WaitForSeconds(TypeSpeed);
            }
        }

        isTyping = false;
        if (typingAudioSource != null) typingAudioSource.Stop();

        ResetNPCSprite();

        if (isPlayerLine)
        {
            if (PlayerNextPrompt != null)
            {
                ConfigureNextPromptVisual(PlayerNextPrompt);
                PlayerNextPrompt.SetActive(true);
            }
        }
        else if (NextPrompt != null)
        {
            ConfigureNextPromptVisual(NextPrompt);
            NextPrompt.SetActive(true);
        }
    }

    private void NextLine()
    {
        if (isTyping) return;

        if (lineIndex < DialogueLines.Length - 1)
        {
            lineIndex++;
            if (NextPrompt != null) NextPrompt.SetActive(false);
            if (PlayerNextPrompt != null) PlayerNextPrompt.SetActive(false);
            StartTyping();
        }
        else
        {
            lineIndex = 0;
            EndDialogue();
        }
    }

    private void EndDialogue()
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }
        if (typingAudioSource != null) typingAudioSource.Stop();

        isTyping = false;
        isInteracting = false;

        // --- BUKA KUNCI GERAKAN PLAYER ---
        SetPlayerMovementLock(false);

        ResetNPCSprite();

        if (DialogueBox != null) DialogueBox.SetActive(false);
        if (PlayerDialogueBox != null) PlayerDialogueBox.SetActive(false);
        if (NextPrompt != null) NextPrompt.SetActive(false);
        if (PlayerNextPrompt != null) PlayerNextPrompt.SetActive(false);

        ResetDialogueCamera();

        // Aktifkan lagi izin interaksi & prompt jika masih dekat
        CheckProximity();

        // Selesaikan QuestObjectiveTrigger yang menempel pada NPC ini (misal serahkan barang / bicara dengan NPC)
        // SEBELUM onDialogueEnd memicu QuestGiver untuk quest berikutnya!
        QuestObjectiveTrigger[] triggers = GetComponents<QuestObjectiveTrigger>();
        if (triggers != null)
        {
            foreach (var trig in triggers)
            {
                if (trig != null && trig.enabled)
                {
                    trig.CompleteFromDialogue();
                }
            }
        }

        bool isMapNPC = GetComponent<NPCMapTrigger>() != null || gameObject.name.IndexOf("Map", StringComparison.OrdinalIgnoreCase) >= 0;
        if (isMapNPC)
        {
            // NPC Peta / Sopir Kapal selalu memanggil event untuk membuka Map dan tidak boleh terkunci ke dialog loop
            lineIndex = 0;
            isPostQuestLoop = false;
            onDialogueEnd?.Invoke();
        }
        else if (!isPostQuestLoop)
        {
            onDialogueEnd?.Invoke();
            SwitchToLoopDialogue();
        }

        onDialogueCompleteCallback?.Invoke();
        onDialogueCompleteCallback = null;
    }

    public void SwitchToLoopDialogue()
    {
        if (postQuestLoopLines == null || postQuestLoopLines.Length == 0)
        {
            postQuestLoopLines = GetDefaultPostQuestLoopLines();
        }

        if (postQuestLoopLines != null && postQuestLoopLines.Length > 0)
        {
            DialogueLines = postQuestLoopLines;
            DialogueLineSpeakers = new DialogueSpeaker[postQuestLoopLines.Length];
            for (int i = 0; i < DialogueLineSpeakers.Length; i++)
            {
                DialogueLineSpeakers[i] = DialogueSpeaker.NPC;
            }
            isPostQuestLoop = true;
        }
    }

    private string[] GetDefaultPostQuestLoopLines()
    {
        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name.ToLower();
        string goName = gameObject.name.ToLower();
        if (transform.parent != null) goName += " " + transform.parent.name.ToLower();

        if (sceneName.Contains("waisai"))
        {
            if (goName.Contains("petugas"))
                return new string[] { "Selamat menjelajahi pulau-pulau Raja Ampat! Dan ingat untuk selalu jaga kebersihan laut kita ya." };
            if (goName.Contains("warlok"))
                return new string[] { "Kalau lihat sampah di pasir atau dermaga, bantu dipungut dan dibuang ke tempatnya ya!" };
            if (goName.Contains("bule"))
                return new string[] { "Pemandangan di sini benar-benar unbelievable! Selamat berlibur ya!" };
            return new string[] { "Selamat menikmati keindahan alam Raja Ampat!" };
        }
        else if (sceneName.Contains("pianemo") || sceneName.Contains("piaynemo"))
        {
            return new string[] { "Tetap berhati-hati saat melangkah. Dan nikmati pemandangan indah dari atas puncak!" };
        }
        else if (sceneName.Contains("arborek"))
        {
            if (goName.Contains("tetua"))
                return new string[] { "Terima kasih sudah menghormati tradisi Sasi kami, Nak. Ceritakan kearifan lokal desa kami ini kepada teman-temanmu di luar sana ya." };
            return new string[] { "Desa kami selalu bersih karena kami saling menjaga tradisi leluhur." };
        }
        else if (sceneName.Contains("misool"))
        {
            if (goName.Contains("ibu"))
                return new string[] { "Pari Mantanya sudah berenang bebas lagi di laut dalam! Terima kasih banyak ya, Nak..." };
            return new string[] { "Terima kasih banyak sudah bantu membersihkan hama Bulu Seribu tadi. Terumbu karang Misool jadi bisa bernapas lega berkat mu!" };
        }
        else if (sceneName.Contains("sawinggrai"))
        {
            if (goName.Contains("foto") || goName.Contains("penjual"))
                return new string[] { "Foto Cenderawasih Merah nya jernih sekali! Hasil cetakannya pasti bakal sangat membantu usaha kecil saya di sini." };
            if (goName.Contains("2") || goName.Contains("panggung"))
                return new string[] { "Tiang-tiang rumah panggung kami memang selalu jadi rumah favorit buat ikan-ikan kecil." };
            return new string[] { "Akses jalan menuju hutan sudah bersih dan aman dilewati. Terima kasih bantuannya ya!" };
        }
        else if (sceneName.Contains("friwen"))
        {
            if (goName.Contains("trivia") || goName.Contains("2"))
                return new string[] { "Ingat terus pesan tadi ya, Kawan. Menjaga jarak aman dan tidak membuang sampah adalah cara terbaik menikmati keindahan Pasir Timbul." };
            return new string[] { "Silakan bersantai menikmati pantai Friwen yang teduh." };
        }
        else if (sceneName.Contains("gag"))
        {
            if (goName.Contains("tetua"))
                return new string[] { "Bawa pulang kisah tentang tanah dan laut kami ini Nak.. Agar dunia tahu bahwa surga yang terluka ini butuh kita jaga bersama." };
            return new string[] { "Data penelitian ini akan kami gunakan untuk terus mengadvokasi pemulihan ekosistem Pulau Gag." };
        }

        return null;
    }

    public void SetPostQuestLoopLines(string[] loopLines)
    {
        postQuestLoopLines = loopLines;
        SwitchToLoopDialogue();
    }

    private void ResetNPCSprite()
    {
        if (npcSpriteRenderer != null && idleSprite != null)
        {
            npcSpriteRenderer.sprite = idleSprite;
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (CheckIsCutscene()) return;
        if (collision.CompareTag("Player"))
        {
            isPlayerInCollider = true;
            if (!isInteracting)
            {
                canInteract = true;
                if (InteractPrompt != null && !InteractPrompt.activeSelf)
                {
                    InteractPrompt.SetActive(true);
                }
            }
        }
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (CheckIsCutscene()) return;
        if (collision.CompareTag("Player"))
        {
            isPlayerInCollider = true;
            if (!isInteracting)
            {
                canInteract = true;
                if (InteractPrompt != null && !InteractPrompt.activeSelf)
                {
                    InteractPrompt.SetActive(true);
                }
            }
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            isPlayerInCollider = false;
            if (cachedPlayerObj != null)
            {
                float dist = Vector2.Distance(transform.position, cachedPlayerObj.transform.position);
                if (dist > interactDistance)
                {
                    canInteract = false;
                    if (InteractPrompt != null) InteractPrompt.SetActive(false);
                }
            }
            else
            {
                canInteract = false;
                if (InteractPrompt != null) InteractPrompt.SetActive(false);
            }

            if (isInteracting) EndDialogue();
        }
    }

    public void ShowTriviaQuestion(string questionText)
    {
        if (string.IsNullOrEmpty(questionText)) return;

        EnsureWaisaiDialogueStandard();

        if (InteractPrompt != null) InteractPrompt.SetActive(false);
        if (DialogueBox != null) DialogueBox.SetActive(true);
        if (PlayerDialogueBox != null) PlayerDialogueBox.SetActive(false);
        if (NextPrompt != null) NextPrompt.SetActive(false);
        if (PlayerNextPrompt != null) PlayerNextPrompt.SetActive(false);

        if (typingCoroutine != null) StopCoroutine(typingCoroutine);
        if (typingAudioSource != null) typingAudioSource.Stop();

        if (DialogueText != null)
        {
            DialogueText.text = questionText;
            DialogueText.maxVisibleCharacters = questionText.Length;

            Canvas.ForceUpdateCanvases();
            if (DialogueBox != null)
            {
                RectTransform bRt = DialogueBox.GetComponent<RectTransform>();
                if (bRt != null) UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(bRt);
            }
        }

        if (npcSpriteRenderer != null && talkingSprite1 != null)
        {
            npcSpriteRenderer.sprite = talkingSprite1;
        }
    }

    public void StartCustomDialogue(string[] customLines, Action onComplete)
    {
        DialogueLines = customLines;
        onDialogueCompleteCallback = onComplete;

        DialogueLineSpeakers = new DialogueSpeaker[customLines.Length];
        for (int i = 0; i < DialogueLineSpeakers.Length; i++)
        {
            DialogueLineSpeakers[i] = DialogueSpeaker.NPC;
        }

        StartDialogue();
    }

    public void StartDialogueForced()
    {
        if (isInteracting) return;

        if (InteractPrompt != null) InteractPrompt.SetActive(false);

        isInteracting = true;
        lineIndex = 0;

        // --- KUNCI GERAKAN PLAYER & NOLKAN VELOCITY ---
        SetPlayerMovementLock(true);

        ApplyDialogueCameraZoom();

        if (PlayerDialogueBox != null) PlayerDialogueBox.SetActive(false);
        if (NextPrompt != null) NextPrompt.SetActive(false);
        if (PlayerNextPrompt != null) PlayerNextPrompt.SetActive(false);

        StartTyping();
    }

    #region Camera Zoom Management
    private void EnsureDialogueCamera()
    {
        if (dialogueCamera == null)
        {
            GameObject camObj = GameObject.Find("CinemachineCameraDialogue");
            if (camObj != null)
            {
                dialogueCamera = camObj.GetComponent<CinemachineCamera>();
            }

            if (dialogueCamera == null)
            {
                CinemachineCamera[] cams = FindObjectsByType<CinemachineCamera>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                foreach (var c in cams)
                {
                    if (c != null && c.name.IndexOf("Dialogue", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        dialogueCamera = c;
                        break;
                    }
                }
            }
        }
    }

    private void ApplyDialogueCameraZoom()
    {
        EnsureDialogueCamera();

        if (dialogueCamera != null)
        {
            if (CheckIsCutscene())
            {
                // Pada cutscene perahu, jaga agar kamera tetap menyorot perahu dengan framing luas
                BoatBobbing boat = FindFirstObjectByType<BoatBobbing>();
                if (boat != null)
                {
                    dialogueCamera.Target.TrackingTarget = boat.transform;
                }
                var boatLens = dialogueCamera.Lens;
                boatLens.OrthographicSize = npcLensSize > 0f ? npcLensSize : 3.6f;
                dialogueCamera.Lens = boatLens;

                var follow = dialogueCamera.GetComponent<Unity.Cinemachine.CinemachineFollow>();
                if (follow != null)
                {
                    follow.FollowOffset = new Vector3(0f, 1.4f, -10f);
                }

                dialogueCamera.Priority.Value = 20;
                return;
            }

            // Arahkan kamera dialog ke NPC ini
            dialogueCamera.Target.TrackingTarget = this.transform;

            // Terapkan ukuran lensa yang ditentukan langsung di Inspector tanpa dipaksa override
            if (npcLensSize > 0f)
            {
                var lens = dialogueCamera.Lens;
                lens.OrthographicSize = npcLensSize;
                dialogueCamera.Lens = lens;
            }

            // Atur durasi blend zoom kamera pada CinemachineBrain agar halus dan santai
            if (Camera.main != null)
            {
                CinemachineBrain brain = Camera.main.GetComponent<CinemachineBrain>();
                if (brain != null)
                {
                    brain.DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.EaseInOut, zoomTransitionDuration);
                }
            }

            // Naikkan prioritas agar Cinemachine melakukan blend transisi ke kamera dialog
            dialogueCamera.Priority.Value = 20;

            CinemachineConfiner2D confiner = dialogueCamera.GetComponent<CinemachineConfiner2D>();
            if (confiner != null)
            {
                confiner.InvalidateBoundingShapeCache();
            }
        }
    }

    private void ResetDialogueCamera()
    {
        if (dialogueCamera != null)
        {
            if (CheckIsCutscene())
            {
                dialogueCamera.Priority.Value = 0;
                return;
            }

            // Pastikan blend saat kembali ke player tetap menggunakan transisi yang santai
            if (Camera.main != null)
            {
                CinemachineBrain brain = Camera.main.GetComponent<CinemachineBrain>();
                if (brain != null)
                {
                    brain.DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.EaseInOut, zoomTransitionDuration);
                }
            }

            dialogueCamera.Priority.Value = 0;

            // Beri jeda secukupnya agar blend transisi Cinemachine dari NPC ke Player selesai
            // sebelum target kamera dikembalikan ke player
            StartCoroutine(RoutineResetStandbyTarget(zoomTransitionDuration + 0.1f));
        }
    }

    private IEnumerator RoutineResetStandbyTarget(float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        if (dialogueCamera != null && dialogueCamera.Priority.Value == 0)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj == null) playerObj = GameObject.Find("Player");
            if (playerObj != null)
            {
                dialogueCamera.Target.TrackingTarget = playerObj.transform;
            }
        }
    }
    #endregion
}
