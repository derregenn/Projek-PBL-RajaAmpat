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

    [Header("Secondary NPC (3rd Speaker / NPC-to-NPC)")]
    [Tooltip("Centang jika Speaker 'Player' (index 1) pada DialogueLineSpeakers diarahkan ke DialogueBox NPC lain (tanpa Player).")]
    [SerializeField] private bool speaker2IsOtherNpc = false;
    [Tooltip("Opsional: Box untuk speaker ke-3 (OtherNPC = index 2) jika percakapan melibatkan 2 NPC dan Player sekaligus.")]
    [SerializeField] private GameObject SecondaryNPCDialogueBox;
    [SerializeField] private TMP_Text SecondaryNPCDialogueText;
    [SerializeField] private GameObject SecondaryNPCNextPrompt;

    [Header("Dialogue")]
    [SerializeField] private string[] DialogueLines;
    [SerializeField] private DialogueSpeaker[] DialogueLineSpeakers;
    [SerializeField] private Sprite[] DialogueLineSprites;
    [SerializeField] private string triviaQuestionText;
    [SerializeField] private float TypeSpeed = 0.04f;

    [Header("Typing Audio")]
    [SerializeField] private AudioSource typingAudioSource;
    [SerializeField] private AudioClip typingAudioClip;
    [SerializeField, Range(0f, 1f)] private float typingAudioVolume = 0.3f;
    [SerializeField, Min(0f)] private float typingAudioInterval = 0.06f;
    private float nextTypingAudioTime;

    private enum DialogueSpeaker
    {
        NPC,
        Player,
        OtherNPC
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
    [Tooltip("Samakan ketinggian vertikal (Y) kamera dialog dengan kamera gameplay aktif.")]
    [SerializeField] private bool matchGameplayCameraHeight = true;
    [Tooltip("Cegah DialogueBox NPC maupun Player terpotong di luar batas atas/bawah kamera.")]
    [SerializeField] private bool preventDialogueBoxCameraOverlap = true;

    [Header("Quest Progression Gate (Opsional)")]
    [Tooltip("Kosongkan untuk NPC opsional/selalu aktif. Jika diisi, dialog hanya aktif setelah salah satu Quest ID ini aktif atau selesai (pisahkan koma jika lebih dari satu).")]
    [SerializeField] private string requiredActiveOrCompletedQuestID = "";
    [Tooltip("Kosongkan untuk NPC opsional/selalu aktif. Jika diisi, dialog hanya aktif setelah seluruh Quest ID ini selesai (pisahkan koma jika lebih dari satu).")]
    [SerializeField] private string requiredCompletedQuestID = "";

    private Vector3 savedDialogueFollowOffset;
    private bool hasSavedDialogueFollowOffset = false;

    [Header("Dialogue Events")]
    [SerializeField] private UnityEvent onDialogueEnd;

    [Header("Post Quest / Free Roam Loop Dialogue")]
    [Tooltip("Dialog berulang yang dimainkan terus saat berinteraksi setelah quest selesai atau di mode free roam")]
    [SerializeField] private string[] postQuestLoopLines;
    [Tooltip("Opsional: Speaker per baris pada postQuestLoopLines (kosong = semua NPC).")]
    [SerializeField] private DialogueSpeaker[] postQuestLoopSpeakers;
    [Tooltip("Opsional: Quest ID yang jika selesai otomatis memicu dialog loop NPC ini.")]
    [SerializeField] private string loopTriggerQuestID;
    [Tooltip("Opsional: Stage minimum MasterQuestManager yang memicu dialog loop NPC ini (-1 = nonaktif).")]
    [SerializeField] private int loopTriggerMinStage = -1;
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
    [SerializeField] private bool allowManualInteract = true;
    [SerializeField] private bool autoTriggerOnPlayerEnter = false;

    private static Dialogue activeDialogueInstance;
    public static bool IsAnyDialogueActive =>
        activeDialogueInstance != null
        && activeDialogueInstance.isInteracting
        && activeDialogueInstance.gameObject.activeInHierarchy;
    private GameObject cachedPlayerObj;
    private bool isBoatCutsceneScene;
    private Canvas cachedOwnCanvas;
    private Canvas cachedDialogueCanvas;
    private Canvas cachedPlayerDialogueCanvas;
    private Collider2D cachedOwnerCollider;
    private bool hasSearchedOwnerCollider;
    private bool cachedIsCutscene;
    private bool hasCachedCutsceneFlag;
    private Player cachedPlayerComp;
    private PlayerSwim cachedPlayerSwim;
    private Rigidbody2D cachedPlayerRb;
    private Animator cachedPlayerAnim;
    private SpriteRenderer cachedPlayerSr;
    private Sprite playerDefaultIdleSprite;
    private bool isPlayerSpriteOverriddenByLine;
    private Sprite currentNpcPoseSprite;
    private Vector3 initialNpcLocalScale;
    private bool hasCachedInitialNpcScale;
    private string[] initialDialogueLines;
    private DialogueSpeaker[] initialDialogueLineSpeakers;
    private Sprite[] initialDialogueLineSprites;
    private bool hasCachedInitialDialogueData;
    private bool hasCompletedInitialDialogue;
    private Unity.Cinemachine.CinemachineFollow cachedDialogueFollow;
    private CinemachineCamera cachedFollowCamSource;
    private float lastAppliedCanvasOrtho = -1f;
    private Vector3 lastAppliedOwnerScale = Vector3.zero;
    private static readonly Vector3[] sharedWorldCorners = new Vector3[4];
    private static readonly System.Collections.Generic.List<Collider2D> sharedCollidersBuffer = new System.Collections.Generic.List<Collider2D>(8);

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

    private GameObject GetCharacterOwnerObject()
    {
        if (cachedOwnCanvas == null) cachedOwnCanvas = GetComponent<Canvas>();
        if (cachedOwnCanvas != null && transform.parent != null)
        {
            return transform.parent.gameObject;
        }
        return gameObject;
    }

    private Transform GetCharacterTransform()
    {
        return GetCharacterOwnerObject().transform;
    }

    private Canvas FindDialogueCanvas()
    {
        if (Application.isPlaying && cachedDialogueCanvas != null)
        {
            return cachedDialogueCanvas;
        }

        Canvas selfCanvas = GetComponent<Canvas>();
        if (selfCanvas != null)
        {
            cachedDialogueCanvas = selfCanvas;
            return selfCanvas;
        }

        Transform t = transform.Find("DialogueCanvas");
        if (t != null)
        {
            Canvas c = t.GetComponent<Canvas>();
            if (c != null)
            {
                cachedDialogueCanvas = c;
                return c;
            }
        }
        if (DialogueBox != null && DialogueBox.transform.parent != null)
        {
            Canvas c = DialogueBox.transform.parent.GetComponent<Canvas>();
            if (c != null)
            {
                cachedDialogueCanvas = c;
                return c;
            }
        }
        Canvas[] all = GetComponentsInChildren<Canvas>(true);
        foreach (var c in all)
        {
            if (c.gameObject.name.IndexOf("DialogueCanvas", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                cachedDialogueCanvas = c;
                return c;
            }
        }
        cachedDialogueCanvas = all.Length > 0 ? all[0] : null;
        return cachedDialogueCanvas;
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
            else if (!UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode
                     && !UnityEditor.PrefabUtility.IsPartOfPrefabInstance(InteractPrompt))
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
            bool canReparentPrompt = true;
#if UNITY_EDITOR
            if (!Application.isPlaying && UnityEditor.PrefabUtility.IsPartOfPrefabInstance(InteractPrompt))
            {
                canReparentPrompt = false;
            }
#endif

            // Pastikan InteractPrompt DI DALAM DialogueCanvas, tapi DI LUAR DialogueBox
            Canvas dialogueCanvas = FindDialogueCanvas();
            Transform canvasTransform = dialogueCanvas != null ? dialogueCanvas.transform : null;

            if (DialogueBox != null)
            {
                if (canvasTransform == null) canvasTransform = DialogueBox.transform.parent;

                // Jika InteractPrompt berada di dalam DialogueBox, pindahkan keluar menjadi sibling
                if (canReparentPrompt && InteractPrompt.transform.IsChildOf(DialogueBox.transform) && InteractPrompt.transform != DialogueBox.transform)
                {
                    InteractPrompt.transform.SetParent(canvasTransform, false);
                }
            }

            if (canReparentPrompt && canvasTransform != null && InteractPrompt.transform.parent != canvasTransform)
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

    public static float GetCameraCompensatedWorldScale(CinemachineCamera dialogueCam, float npcLensSize, bool isInteracting)
    {
        float ortho = 2.0f;
        if (isInteracting && Camera.main != null && Camera.main.orthographic && Camera.main.orthographicSize > 0.1f)
        {
            ortho = Camera.main.orthographicSize;
        }
        else if (dialogueCam != null && npcLensSize > 0.1f)
        {
            ortho = npcLensSize;
        }
        else if (Camera.main != null && Camera.main.orthographic && Camera.main.orthographicSize > 0.1f)
        {
            ortho = Camera.main.orthographicSize;
        }
        else if (dialogueCam != null && dialogueCam.Lens.OrthographicSize > 0.1f)
        {
            ortho = dialogueCam.Lens.OrthographicSize;
        }

        // Referensi 02_CutsceneAirstrip: ortho = 2.0 -> targetWorldScale = 0.00818f (~34.5% lebar layar)
        return 0.00818f * (ortho / 2.0f);
    }

    public static float CalculateCharacterHeadOffsetY(GameObject characterObj, float pScaleY, float orthoSize = 2.0f)
    {
        float headTopWorld = 0f;

        sharedCollidersBuffer.Clear();
        characterObj.GetComponentsInChildren<Collider2D>(false, sharedCollidersBuffer);
        for (int i = 0; i < sharedCollidersBuffer.Count; i++)
        {
            Collider2D col = sharedCollidersBuffer[i];
            if (col != null && !col.isTrigger)
            {
                float top = col.bounds.max.y - characterObj.transform.position.y;
                if (top > headTopWorld && top < 2.5f) headTopWorld = top;
            }
        }
        sharedCollidersBuffer.Clear();

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

        if (headTopWorld < 0.2f)
        {
            headTopWorld = 1.05f * (pScaleY / 0.26f);
        }

        float gap = Mathf.Clamp(0.05f * orthoSize, 0.10f, 0.45f);
        return headTopWorld + gap;
    }

    public static void ApplyStandardCanvasTransform(GameObject ownerObj, Canvas dialogueCanvas, float targetWorldScale, float orthoSize)
    {
        if (ownerObj == null || dialogueCanvas == null) return;

        RectTransform cRt = dialogueCanvas.GetComponent<RectTransform>();
        if (cRt == null) return;

        Vector2 targetSizeDelta = new Vector2(300f, 102.69f);
        Vector2 targetAnchor = new Vector2(0.5f, 0f);
        if ((cRt.sizeDelta - targetSizeDelta).sqrMagnitude > 0.0001f) cRt.sizeDelta = targetSizeDelta;
        if ((cRt.pivot - targetAnchor).sqrMagnitude > 0.0001f) cRt.pivot = targetAnchor;
        if ((cRt.anchorMin - targetAnchor).sqrMagnitude > 0.0001f) cRt.anchorMin = targetAnchor;
        if ((cRt.anchorMax - targetAnchor).sqrMagnitude > 0.0001f) cRt.anchorMax = targetAnchor;

        Transform parentTr = dialogueCanvas.transform.parent;
        GameObject characterOwner = (ownerObj == dialogueCanvas.gameObject && parentTr != null) ? parentTr.gameObject : ownerObj;

        Vector3 parentLossyScale = parentTr != null ? parentTr.lossyScale : Vector3.one;
        float pScaleX = Mathf.Abs(parentLossyScale.x);
        float pScaleY = Mathf.Abs(parentLossyScale.y);
        if (pScaleX <= 0.0001f) pScaleX = 1f;
        if (pScaleY <= 0.0001f) pScaleY = 1f;

        float targetBottomWorldY = CalculateCharacterHeadOffsetY(characterOwner, pScaleY, orthoSize);
        float signX = Mathf.Sign(parentLossyScale.x);
        if (signX == 0f) signX = 1f;

        Vector3 targetScale = new Vector3((targetWorldScale / pScaleX) * signX, targetWorldScale / pScaleY, 1f);
        if ((cRt.localScale - targetScale).sqrMagnitude > 1e-9f)
        {
            cRt.localScale = targetScale;
        }

        Vector2 targetAnchoredPos = new Vector2(0f, targetBottomWorldY / pScaleY);
        if ((cRt.anchoredPosition - targetAnchoredPos).sqrMagnitude > 0.0001f)
        {
            cRt.anchoredPosition = targetAnchoredPos;
        }
    }

    private float CalculateTargetWorldBottomY(GameObject characterObj, float pScaleY)
    {
        return CalculateCharacterHeadOffsetY(characterObj, pScaleY, 2.0f);
    }

    private bool CheckIsCutscene()
    {
        if (Application.isPlaying && hasCachedCutsceneFlag)
        {
            return cachedIsCutscene;
        }

        bool result = isBoatCutsceneScene
            || gameObject.name.IndexOf("cutscene", StringComparison.OrdinalIgnoreCase) >= 0
            || (transform.parent != null && transform.parent.name.IndexOf("cutscene", StringComparison.OrdinalIgnoreCase) >= 0);

        if (Application.isPlaying)
        {
            cachedIsCutscene = result;
            hasCachedCutsceneFlag = true;
        }

        return result;
    }

    private static GameObject ResolveCanvasOwner(Canvas c)
    {
        if (c == null) return null;
        return c.transform.parent != null ? c.transform.parent.gameObject : c.gameObject;
    }

    private bool IsUsingExternalSpeaker2()
    {
        if (speaker2IsOtherNpc && PlayerDialogueBox != null) return true;
        if (PlayerDialogueBox != null && PlayerDialogueBox.scene.IsValid() && cachedPlayerObj != null)
        {
            return PlayerDialogueBox.transform.root != cachedPlayerObj.transform.root;
        }
        return false;
    }

    private static void ScaleExternalBoxCanvas(GameObject boxObj, float targetWorldScale, float activeOrtho)
    {
        if (boxObj == null) return;
        Canvas extCanvas = boxObj.GetComponentInParent<Canvas>(true);
        if (extCanvas != null && extCanvas.renderMode == RenderMode.WorldSpace)
        {
            ApplyStandardCanvasTransform(ResolveCanvasOwner(extCanvas), extCanvas, targetWorldScale, activeOrtho);
        }
    }

    private void UpdateActiveDialogueCanvasScale()
    {
        float targetWorldScale = GetCameraCompensatedWorldScale(dialogueCamera, npcLensSize, isInteracting);
        float activeOrtho = (targetWorldScale / 0.00818f) * 2.0f;

        GameObject charOwner = GetCharacterOwnerObject();
        Vector3 ownerScale = charOwner != null ? charOwner.transform.lossyScale : Vector3.one;
        bool needsScaleUpdate = Mathf.Abs(activeOrtho - lastAppliedCanvasOrtho) > 0.001f
            || (ownerScale - lastAppliedOwnerScale).sqrMagnitude > 1e-6f;

        if (needsScaleUpdate)
        {
            lastAppliedCanvasOrtho = activeOrtho;
            lastAppliedOwnerScale = ownerScale;

            Canvas dialogueCanvas = FindDialogueCanvas();
            if (dialogueCanvas != null)
            {
                ApplyStandardCanvasTransform(charOwner, dialogueCanvas, targetWorldScale, activeOrtho);
            }

            if (IsUsingExternalSpeaker2())
            {
                ScaleExternalBoxCanvas(PlayerDialogueBox, targetWorldScale, activeOrtho);
            }
            else if (cachedPlayerObj != null)
            {
                if (cachedPlayerDialogueCanvas == null)
                {
                    Transform pCanvasTr = cachedPlayerObj.transform.Find("DialogueCanvas");
                    if (pCanvasTr != null) cachedPlayerDialogueCanvas = pCanvasTr.GetComponent<Canvas>();
                }
                if (cachedPlayerDialogueCanvas != null)
                {
                    ApplyStandardCanvasTransform(cachedPlayerObj, cachedPlayerDialogueCanvas, targetWorldScale, activeOrtho);
                }
            }

            ScaleExternalBoxCanvas(SecondaryNPCDialogueBox, targetWorldScale, activeOrtho);
        }

        ClampDialogueCameraFollowToVisibleBoxes(activeOrtho);
    }

    public bool PassesQuestPrerequisite()
    {
        return QuestProgressionGate.Passes(string.Empty, requiredActiveOrCompletedQuestID, requiredCompletedQuestID);
    }

    private void EnsureWaisaiDialogueStandard()
    {
        float targetWorldScale = GetCameraCompensatedWorldScale(dialogueCamera, npcLensSize, isInteracting);
        float activeOrtho = (targetWorldScale / 0.00818f) * 2.0f;

        // 1. Standar Canvas (Normalisasi World Scale & Offset agar identik di SEMUA NPC di SEMUA SCENE)
        Canvas dialogueCanvas = FindDialogueCanvas();
        if (dialogueCanvas != null)
        {
            ApplyStandardCanvasTransform(GetCharacterOwnerObject(), dialogueCanvas, targetWorldScale, activeOrtho);
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

        if (TypeSpeed <= 0f)
        {
            TypeSpeed = 0.04f;
        }

        // 4. Standar Dialogue Player & Auto Binding Inspector
        EnsurePlayerDialogueReferencesAndStandard();
    }

    private static TMP_Text FindBoxDialogueText(Transform boxTr)
    {
        if (boxTr == null) return null;
        Transform tTr = boxTr.Find("Dialogue");
        if (tTr == null) tTr = boxTr.Find("Text (TMP)");
        if (tTr == null)
        {
            TMP_Text[] texts = boxTr.GetComponentsInChildren<TMP_Text>(true);
            foreach (var txt in texts)
            {
                if (txt.gameObject.name != "NextButton" && (txt.transform.parent == null || txt.transform.parent.name != "NextButton"))
                {
                    tTr = txt.transform;
                    break;
                }
            }
        }
        return tTr != null ? tTr.GetComponent<TMP_Text>() : null;
    }

    private static GameObject FindBoxNextPrompt(Transform boxTr)
    {
        if (boxTr == null) return null;
        Transform nextTr = boxTr.Find("NextButton");
        if (nextTr == null) nextTr = boxTr.Find("Btn_Next");
        if (nextTr == null) nextTr = boxTr.Find("NextPrompt");
        return nextTr != null ? nextTr.gameObject : null;
    }

    private void EnsureExternalSpeakerBox(GameObject boxObj, ref TMP_Text textField, ref GameObject nextPromptField, float targetWorldScale, float activeOrtho)
    {
        if (boxObj == null) return;

        Canvas extCanvas = boxObj.GetComponentInParent<Canvas>(true);
        if (extCanvas != null && extCanvas.renderMode == RenderMode.WorldSpace)
        {
            extCanvas.overrideSorting = true;
            extCanvas.sortingLayerName = "Foreground";
            extCanvas.sortingOrder = 100;
            if (extCanvas.worldCamera == null) extCanvas.worldCamera = Camera.main;
            ApplyStandardCanvasTransform(ResolveCanvasOwner(extCanvas), extCanvas, targetWorldScale, activeOrtho);
        }

        if (textField == null || textField.transform.root != boxObj.transform.root)
        {
            TMP_Text foundText = FindBoxDialogueText(boxObj.transform);
            if (foundText != null) textField = foundText;
        }

        if (nextPromptField == null || nextPromptField.transform.root != boxObj.transform.root)
        {
            GameObject foundNext = FindBoxNextPrompt(boxObj.transform);
            if (foundNext != null) nextPromptField = foundNext;
        }

        ConfigureDialogueBoxLayout(boxObj, textField, nextPromptField);
    }

    public void EnsurePlayerDialogueReferencesAndStandard()
    {
        if (cachedPlayerObj == null)
        {
            cachedPlayerObj = GameObject.FindGameObjectWithTag("Player");
            if (cachedPlayerObj == null) cachedPlayerObj = GameObject.Find("Player");
        }

        // Cegah interaksi jika skrip Dialogue berada pada objek Player (misal saat berenang / PlayerDiving)
        if (cachedPlayerObj != null && (gameObject == cachedPlayerObj || transform.IsChildOf(cachedPlayerObj.transform)))
        {
            canInteract = false;
            if (InteractPrompt != null && InteractPrompt.activeSelf) InteractPrompt.SetActive(false);
            return;
        }

        if (GetComponent<Player>() != null || GetComponent<PlayerSwim>() != null || GetComponent<PlayerDiving>() != null)
        {
            canInteract = false;
            if (InteractPrompt != null && InteractPrompt.activeSelf) InteractPrompt.SetActive(false);
            return;
        }

        // Cegah interaksi manual jika tidak ada baris dialog sama sekali
        if ((DialogueLines == null || DialogueLines.Length == 0) && (!isPostQuestLoop || postQuestLoopLines == null || postQuestLoopLines.Length == 0))
        {
            canInteract = false;
            if (InteractPrompt != null && InteractPrompt.activeSelf) InteractPrompt.SetActive(false);
            return;
        }

        float targetWorldScale = GetCameraCompensatedWorldScale(dialogueCamera, npcLensSize, isInteracting);
        float activeOrtho = (targetWorldScale / 0.00818f) * 2.0f;

        if (IsUsingExternalSpeaker2())
        {
            EnsureExternalSpeakerBox(PlayerDialogueBox, ref PlayerDialogueText, ref PlayerNextPrompt, targetWorldScale, activeOrtho);
        }
        else if (cachedPlayerObj != null)
        {
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

                    if (PlayerDialogueText == null || PlayerDialogueText.transform.root != cachedPlayerObj.transform.root)
                    {
                        TMP_Text foundText = FindBoxDialogueText(pBoxTr);
                        if (foundText != null) PlayerDialogueText = foundText;
                    }

                    if (PlayerNextPrompt == null || PlayerNextPrompt.transform.root != cachedPlayerObj.transform.root)
                    {
                        GameObject foundNext = FindBoxNextPrompt(pBoxTr);
                        if (foundNext != null) PlayerNextPrompt = foundNext;
                    }

                    // Normalisasi Player Dialogue Canvas & Box
                    NormalizePlayerDialogueUI(cachedPlayerObj, pCanvasTr, pBoxTr, PlayerDialogueText, PlayerNextPrompt);
                }
            }
        }

        if (SecondaryNPCDialogueBox != null)
        {
            EnsureExternalSpeakerBox(SecondaryNPCDialogueBox, ref SecondaryNPCDialogueText, ref SecondaryNPCNextPrompt, targetWorldScale, activeOrtho);
        }
    }

    public static void ConfigureDialogueBoxLayout(GameObject boxObj, TMP_Text textObj, GameObject nextObj)
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
                textRt.localScale = Vector3.one;
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

    public static void ConfigureNextPromptVisual(GameObject nextObj)
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
        float targetWorldScale = GetCameraCompensatedWorldScale(dialogueCamera, npcLensSize, isInteracting);
        float activeOrtho = (targetWorldScale / 0.00818f) * 2.0f;

        Canvas pCanvas = pCanvasTr != null ? pCanvasTr.GetComponent<Canvas>() : null;
        if (pCanvas != null)
        {
            ApplyStandardCanvasTransform(playerObj, pCanvas, targetWorldScale, activeOrtho);
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
    }

    private void CheckProximity()
    {
        bool blockedByActiveState = isInteracting
            || !allowManualInteract
            || !PassesQuestPrerequisite()
            || (activeDialogueInstance != null && activeDialogueInstance.isInteracting)
            || InteractionInputLock.IsAnyOverlayBlockingDialogue
            || CheckIsCutscene();

        if (blockedByActiveState)
        {
            canInteract = false;
            if (InteractPrompt != null && InteractPrompt.activeSelf)
            {
                InteractPrompt.SetActive(false);
            }
            return;
        }

        if (cachedPlayerObj == null)
        {
            cachedPlayerObj = GameObject.FindGameObjectWithTag("Player");
            if (cachedPlayerObj == null) cachedPlayerObj = GameObject.Find("Player");
        }

        if (cachedPlayerObj != null)
        {
            bool inProximity = false;
            GameObject charOwner = GetCharacterOwnerObject();
            if (!hasSearchedOwnerCollider)
            {
                hasSearchedOwnerCollider = true;
                cachedOwnerCollider = GetComponent<Collider2D>();
                if (cachedOwnerCollider == null) cachedOwnerCollider = GetComponentInChildren<Collider2D>();
                if (cachedOwnerCollider == null && charOwner != gameObject)
                {
                    cachedOwnerCollider = charOwner.GetComponent<Collider2D>();
                }
            }

            Collider2D col = cachedOwnerCollider;
            if (col != null)
            {
                if (speaker2IsOtherNpc)
                {
                    float distToCenter = Vector2.Distance((Vector2)col.bounds.center, (Vector2)cachedPlayerObj.transform.position);
                    inProximity = distToCenter <= interactDistance;
                }
                else
                {
                    Vector2 closestPoint = col.ClosestPoint(cachedPlayerObj.transform.position);
                    float distToCol = Vector2.Distance(closestPoint, cachedPlayerObj.transform.position);
                    inProximity = distToCol <= 0.8f; // Mengikuti presisi batas collider NPC
                }
            }
            else
            {
                float dist = Vector2.Distance(charOwner.transform.position, cachedPlayerObj.transform.position);
                inProximity = dist <= interactDistance;
            }

            if (inProximity || isPlayerInCollider)
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

    private void CachePlayerComponents(GameObject pObj)
    {
        if (pObj == null) return;
        if (cachedPlayerObj != pObj || cachedPlayerComp == null)
        {
            cachedPlayerObj = pObj;
            cachedPlayerComp = pObj.GetComponent<Player>();
            cachedPlayerSwim = pObj.GetComponent<PlayerSwim>();
            cachedPlayerRb = pObj.GetComponent<Rigidbody2D>();
            cachedPlayerAnim = pObj.GetComponent<Animator>();
            cachedPlayerSr = pObj.GetComponent<SpriteRenderer>();
            if (cachedPlayerSr == null) cachedPlayerSr = pObj.GetComponentInChildren<SpriteRenderer>();
            if (cachedPlayerSr != null && playerDefaultIdleSprite == null)
            {
                playerDefaultIdleSprite = cachedPlayerSr.sprite;
            }
        }
    }

    private void SetPlayerMovementLock(bool isLocked)
    {
        GameObject pObj = cachedPlayerObj != null ? cachedPlayerObj : GameObject.FindGameObjectWithTag("Player");
        if (pObj == null) pObj = GameObject.Find("Player");
        if (pObj != null)
        {
            CachePlayerComponents(pObj);

            if (cachedPlayerComp != null)
            {
                cachedPlayerComp.isMovementLocked = isLocked;
                cachedPlayerComp.enabled = true; // Tetap aktif agar Update() menahan velocity di 0 dan animasi Idle
            }

            if (cachedPlayerSwim != null)
            {
                cachedPlayerSwim.isMovementLocked = isLocked;
            }

            if (cachedPlayerRb != null)
            {
                cachedPlayerRb.linearVelocity = Vector2.zero;
            }

            if (cachedPlayerAnim != null && isLocked && !isPlayerSpriteOverriddenByLine)
            {
                if (!cachedPlayerAnim.enabled) cachedPlayerAnim.enabled = true;
                AnimatorStateInfo st = cachedPlayerAnim.GetCurrentAnimatorStateInfo(0);
                if (cachedPlayerComp != null && cachedPlayerComp.isSwimming)
                {
                    if (!st.IsName("Player_swim")) cachedPlayerAnim.Play("Player_swim");
                }
                else
                {
                    if (!st.IsName("Player_Idle")) cachedPlayerAnim.Play("Player_Idle");
                }
            }
        }
    }

    private void OnValidate()
    {
#if UNITY_EDITOR
        if (UnityEditor.PrefabUtility.IsPartOfPrefabAsset(gameObject)) return;
        UnityEditor.EditorApplication.delayCall += () =>
        {
            if (this == null || UnityEditor.PrefabUtility.IsPartOfPrefabAsset(gameObject)) return;
            EnsureInteractPromptHierarchy();
            EnsureDialogueCanvasSorting();
            EnsureWaisaiDialogueStandard();
        };
#endif
    }

    private void Awake()
    {
        string activeSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        isBoatCutsceneScene = activeSceneName.IndexOf("boat", StringComparison.OrdinalIgnoreCase) >= 0
            || FindFirstObjectByType<BoatTransitionController>() != null;
        hasCachedCutsceneFlag = false;

        questGiver = GetComponent<QuestGiver>();

        if (!hasCachedInitialDialogueData && !isPostQuestLoop && DialogueLines != null && DialogueLines.Length > 0)
        {
            initialDialogueLines = (string[])DialogueLines.Clone();
            initialDialogueLineSpeakers = DialogueLineSpeakers != null ? (DialogueSpeaker[])DialogueLineSpeakers.Clone() : null;
            initialDialogueLineSprites = DialogueLineSprites != null ? (Sprite[])DialogueLineSprites.Clone() : null;
            hasCachedInitialDialogueData = true;
        }

        if (npcSpriteRenderer == null)
        {
            npcSpriteRenderer = GetComponent<SpriteRenderer>();
        }

        if (npcSpriteRenderer != null)
        {
            if (!hasCachedInitialNpcScale)
            {
                initialNpcLocalScale = npcSpriteRenderer.transform.localScale;
                hasCachedInitialNpcScale = true;
            }
            if (idleSprite == null)
            {
                idleSprite = npcSpriteRenderer.sprite;
            }
        }
        
        EnsureInteractPromptHierarchy();
        EnsureDialogueCanvasSorting();
        EnsureWaisaiDialogueStandard();
        EnsureTypingAudioSource();

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
        lastAppliedCanvasOrtho = -1f;
        EnsureInteractPromptHierarchy();
        EnsureDialogueCanvasSorting();
        EnsureWaisaiDialogueStandard();
        EnsureTypingAudioSource();
    }

    private void OnDisable()
    {
        if (activeDialogueInstance == this)
        {
            activeDialogueInstance = null;
        }
        isInteracting = false;
        isTyping = false;
    }

    private void Start()
    {
        EnsureInteractPromptHierarchy();
        EnsureDialogueCanvasSorting();
        EnsureWaisaiDialogueStandard();
        EnsureDialogueCamera();

        if (!isInteracting)
        {
            if (dialogueCamera != null)
            {
                dialogueCamera.Priority.Value = 0;
            }

            RefreshPostQuestLoopState();

            if (!isPlayerInCollider && InteractPrompt != null) InteractPrompt.SetActive(false);
            if (DialogueBox != null) DialogueBox.SetActive(false);
            if (SecondaryNPCDialogueBox != null) SecondaryNPCDialogueBox.SetActive(false);
            if (NextPrompt != null) NextPrompt.SetActive(false);
            if (SecondaryNPCNextPrompt != null) SecondaryNPCNextPrompt.SetActive(false);
            if (!IsAnyDialogueActive)
            {
                if (PlayerDialogueBox != null) PlayerDialogueBox.SetActive(false);
                if (PlayerNextPrompt != null) PlayerNextPrompt.SetActive(false);
            }
        }
    }

    private void Update()
    {
        if (isInteracting)
        {
            SetPlayerMovementLock(true);
            UpdateActiveDialogueCanvasScale();
        }

        CheckProximity();

        bool anotherDialogueActive = activeDialogueInstance != null
            && activeDialogueInstance != this
            && activeDialogueInstance.isInteracting
            && activeDialogueInstance.gameObject.activeInHierarchy;

        // 1. Memulai dialog manual dengan memencet E
        if (allowManualInteract && !anotherDialogueActive && canInteract && !isInteracting
            && !InteractionInputLock.IsAnyOverlayBlockingDialogue
            && !InteractionInputLock.WasInteractKeyConsumedThisFrame
            && Input.GetKeyDown(KeyCode.E))
        {
            InteractionInputLock.ConsumeInteractKeyThisFrame();
            StartDialogue();
        }
        // 2. Lanjut dialog
        else if (isInteracting && !InteractionInputLock.WasInteractKeyConsumedThisFrame && Input.GetKeyDown(KeyCode.E)
            && ((NextPrompt != null && NextPrompt.activeInHierarchy)
                || (PlayerNextPrompt != null && PlayerNextPrompt.activeInHierarchy)
                || (SecondaryNPCNextPrompt != null && SecondaryNPCNextPrompt.activeInHierarchy)))
        {
            InteractionInputLock.ConsumeInteractKeyThisFrame();
            NextLine();
        }
    }

    public void StartDialogue()
    {
        if (isInteracting) return;
        if (cachedPlayerObj != null && (gameObject == cachedPlayerObj || transform.IsChildOf(cachedPlayerObj.transform))) return;
        if (GetComponent<Player>() != null || GetComponent<PlayerSwim>() != null || GetComponent<PlayerDiving>() != null) return;
        if ((DialogueLines == null || DialogueLines.Length == 0) && (!isPostQuestLoop || postQuestLoopLines == null || postQuestLoopLines.Length == 0)) return;
        if (!PassesQuestPrerequisite()) return;
        if (InteractionInputLock.IsAnyOverlayBlockingDialogue) return;
        if (activeDialogueInstance != null && activeDialogueInstance != this
            && activeDialogueInstance.isInteracting && activeDialogueInstance.gameObject.activeInHierarchy)
        {
            return;
        }

        EnsureWaisaiDialogueStandard();
        RefreshPostQuestLoopState();

        if (InteractPrompt != null) InteractPrompt.SetActive(false);

        canInteract = false;
        isInteracting = true;
        activeDialogueInstance = this;
        lineIndex = 0;
        currentNpcPoseSprite = null;
        isPlayerSpriteOverriddenByLine = false;
        lastAppliedCanvasOrtho = -1f;
        InteractionInputLock.ConsumeInteractKeyThisFrame();

        // --- KUNCI GERAKAN PLAYER & NOLKAN VELOCITY ---
        SetPlayerMovementLock(true);

        ApplyDialogueCameraZoom();

        if (PlayerDialogueBox != null) PlayerDialogueBox.SetActive(false);
        if (SecondaryNPCDialogueBox != null) SecondaryNPCDialogueBox.SetActive(false);
        if (NextPrompt != null) NextPrompt.SetActive(false);
        if (PlayerNextPrompt != null) PlayerNextPrompt.SetActive(false);
        if (SecondaryNPCNextPrompt != null) SecondaryNPCNextPrompt.SetActive(false);

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

    private void EnsureHierarchyActiveForCoroutine()
    {
        Transform p = transform;
        while (p != null)
        {
            if (!p.gameObject.activeSelf)
            {
                p.gameObject.SetActive(true);
            }
            p = p.parent;
        }
        if (!enabled)
        {
            enabled = true;
        }
    }

    private void EnsureTypingAudioSource()
    {
        if (typingAudioSource == null || !typingAudioSource.gameObject.activeInHierarchy)
        {
            AudioSource localSource = GetComponent<AudioSource>();
            if (localSource == null)
            {
                localSource = gameObject.AddComponent<AudioSource>();
                localSource.playOnAwake = false;
                localSource.loop = false;
                localSource.spatialBlend = 0f;
            }
            typingAudioSource = localSource;
        }

        if (typingAudioClip != null && typingAudioClip.loadState == AudioDataLoadState.Unloaded)
        {
            typingAudioClip.LoadAudioData();
        }
    }

    private void StartTyping()
    {
        EnsureHierarchyActiveForCoroutine();
        EnsureTypingAudioSource();
        if (typingCoroutine != null) StopCoroutine(typingCoroutine);
        if (typingAudioSource != null) typingAudioSource.Stop();
        typingCoroutine = StartCoroutine(WriteLine());
    }

    private IEnumerator WriteLine()
    {
        isTyping = true;
        lastAppliedCanvasOrtho = -1f;

        if (DialogueLines == null || DialogueLines.Length == 0)
        {
            EndDialogue();
            yield break;
        }

        DialogueSpeaker speaker = (DialogueLineSpeakers != null && lineIndex < DialogueLineSpeakers.Length)
            ? DialogueLineSpeakers[lineIndex]
            : DialogueSpeaker.NPC;
        bool isNpcPrimary = speaker == DialogueSpeaker.NPC;
        bool isOtherNpc3rd = speaker == DialogueSpeaker.OtherNPC;

        Sprite lineSprite = DialogueLineSprites != null && lineIndex < DialogueLineSprites.Length
            ? DialogueLineSprites[lineIndex]
            : null;

        GameObject activeBox;
        TMP_Text activeText;
        GameObject activeNextPrompt;

        if (isOtherNpc3rd && SecondaryNPCDialogueBox != null)
        {
            activeBox = SecondaryNPCDialogueBox;
            activeText = SecondaryNPCDialogueText;
            activeNextPrompt = SecondaryNPCNextPrompt;
        }
        else if (speaker == DialogueSpeaker.Player || isOtherNpc3rd)
        {
            activeBox = PlayerDialogueBox;
            activeText = PlayerDialogueText;
            activeNextPrompt = PlayerNextPrompt;
        }
        else
        {
            activeBox = DialogueBox;
            activeText = DialogueText;
            activeNextPrompt = NextPrompt;
        }

        // Nonaktifkan semua box terlebih dahulu lalu nyalakan box aktif (aman walau ada box yang direuse)
        if (DialogueBox != null) DialogueBox.SetActive(false);
        if (PlayerDialogueBox != null) PlayerDialogueBox.SetActive(false);
        if (SecondaryNPCDialogueBox != null) SecondaryNPCDialogueBox.SetActive(false);
        if (activeBox != null) activeBox.SetActive(true);

        if (activeText == null)
        {
            isTyping = false;
            EndDialogue();
            yield break;
        }

        activeText.text = DialogueLines[lineIndex];
        activeText.maxVisibleCharacters = 0;

        bool lineSpriteIsPlayer = IsPlayerCharacterSprite(lineSprite);
        if (lineSprite != null)
        {
            if (lineSpriteIsPlayer)
            {
                ApplyPlayerLineSprite(lineSprite);
                ApplyNPCSpriteWithScale(currentNpcPoseSprite != null ? currentNpcPoseSprite : idleSprite);
            }
            else
            {
                if (!isNpcPrimary)
                {
                    RestorePlayerDefaultSprite();
                }
                currentNpcPoseSprite = lineSprite;
                ApplyNPCSpriteWithScale(lineSprite);
            }
        }
        else
        {
            if (!isNpcPrimary)
            {
                RestorePlayerDefaultSprite();
                ApplyNPCSpriteWithScale(currentNpcPoseSprite != null ? currentNpcPoseSprite : idleSprite);
            }
            else if (currentNpcPoseSprite != null)
            {
                ApplyNPCSpriteWithScale(currentNpcPoseSprite);
            }
        }

        Canvas.ForceUpdateCanvases();
        if (activeBox != null && activeBox.activeSelf)
        {
            RectTransform bRt = activeBox.GetComponent<RectTransform>();
            if (bRt != null) UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(bRt);
        }

        int totalCharacters = DialogueLines[lineIndex].Length;
        bool toggleSprite = false;

        for (int i = 0; i <= totalCharacters; i++)
        {
            activeText.maxVisibleCharacters = i;

            if (i < totalCharacters && typingAudioClip != null)
            {
                if (typingAudioSource == null || !typingAudioSource.gameObject.activeInHierarchy)
                {
                    EnsureTypingAudioSource();
                }
                if (typingAudioSource != null && Time.time >= nextTypingAudioTime && !typingAudioSource.isPlaying)
                {
                    typingAudioSource.PlayOneShot(typingAudioClip, typingAudioVolume);
                    nextTypingAudioTime = Time.time + typingAudioInterval;
                }
            }

            if (isNpcPrimary && npcSpriteRenderer != null && talkingSprite1 != null && talkingSprite2 != null && (lineSprite == null || lineSprite == idleSprite || lineSprite == talkingSprite1 || lineSprite == talkingSprite2))
            {
                if (i % spriteToggleInterval == 0)
                {
                    toggleSprite = !toggleSprite;
                    ApplyNPCSpriteWithScale(toggleSprite ? talkingSprite1 : talkingSprite2);
                }
            }

            if (i < totalCharacters)
            {
                yield return new WaitForSeconds(TypeSpeed);
            }
        }

        isTyping = false;
        if (typingAudioSource != null) typingAudioSource.Stop();

        if (isNpcPrimary)
        {
            if (lineSprite != null && !lineSpriteIsPlayer)
            {
                ApplyNPCSpriteWithScale(lineSprite);
            }
            else
            {
                ApplyNPCSpriteWithScale(currentNpcPoseSprite != null ? currentNpcPoseSprite : idleSprite);
            }
        }

        if (activeNextPrompt != null)
        {
            ConfigureNextPromptVisual(activeNextPrompt);
            activeNextPrompt.SetActive(true);
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
            if (SecondaryNPCNextPrompt != null) SecondaryNPCNextPrompt.SetActive(false);
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
        InteractionInputLock.ConsumeInteractKeyThisFrame();
        if (activeDialogueInstance == this)
        {
            activeDialogueInstance = null;
        }

        // --- BUKA KUNCI GERAKAN PLAYER ---
        RestorePlayerDefaultSprite();
        SetPlayerMovementLock(false);

        if (currentNpcPoseSprite != null && idleSprite != null
            && idleSprite.name.IndexOf("Tiduran_Gag", StringComparison.OrdinalIgnoreCase) >= 0
            && currentNpcPoseSprite.name.IndexOf("Tiduran_Gag", StringComparison.OrdinalIgnoreCase) < 0)
        {
            ApplyNPCSpriteWithScale(currentNpcPoseSprite);
            idleSprite = currentNpcPoseSprite;
            if (npcSpriteRenderer != null)
            {
                initialNpcLocalScale = npcSpriteRenderer.transform.localScale;
                hasCachedInitialNpcScale = true;
            }
        }
        else
        {
            ResetNPCSprite();
        }

        if (DialogueBox != null) DialogueBox.SetActive(false);
        if (PlayerDialogueBox != null) PlayerDialogueBox.SetActive(false);
        if (SecondaryNPCDialogueBox != null) SecondaryNPCDialogueBox.SetActive(false);
        if (NextPrompt != null) NextPrompt.SetActive(false);
        if (PlayerNextPrompt != null) PlayerNextPrompt.SetActive(false);
        if (SecondaryNPCNextPrompt != null) SecondaryNPCNextPrompt.SetActive(false);

        ResetDialogueCamera();

        // Aktifkan lagi izin interaksi & prompt jika masih dekat
        CheckProximity();

        // Selesaikan QuestObjectiveTrigger yang menempel pada NPC ini (misal serahkan barang / bicara dengan NPC)
        // SEBELUM onDialogueEnd memicu QuestGiver untuk quest berikutnya!
        if (!isPostQuestLoop)
        {
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
        }

        if (IsMapNPC())
        {
            // NPC Peta / Sopir Kapal selalu memanggil event untuk membuka Map dan tidak boleh terkunci ke dialog loop
            lineIndex = 0;
            isPostQuestLoop = false;
            onDialogueEnd?.Invoke();
        }
        else if (!isPostQuestLoop)
        {
            hasCompletedInitialDialogue = true;
            onDialogueEnd?.Invoke();
            SwitchToLoopDialogue();
        }

        onDialogueCompleteCallback?.Invoke();
        onDialogueCompleteCallback = null;
    }

    private bool IsMapNPC()
    {
        return GetComponent<NPCMapTrigger>() != null ||
               gameObject.name.IndexOf("Map", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    public void RefreshPostQuestLoopState()
    {
        if (IsMapNPC()) return;

        bool shouldLoop = QuestJournalManager.IsCurrentSceneIslandCompletedPersistent();

        if (!shouldLoop && !string.IsNullOrEmpty(loopTriggerQuestID))
        {
            shouldLoop = QuestJournalManager.HasCompletedQuestIDPersistent(loopTriggerQuestID);
        }

        if (!shouldLoop && loopTriggerMinStage >= 0 && MasterQuestManager.Instance != null)
        {
            shouldLoop = MasterQuestManager.Instance.CurrentStage >= loopTriggerMinStage;
        }

        if (shouldLoop || hasCompletedInitialDialogue)
        {
            SwitchToLoopDialogue();
            lineIndex = 0;
        }
        else if (isPostQuestLoop && hasCachedInitialDialogueData && initialDialogueLines != null && initialDialogueLines.Length > 0)
        {
            DialogueLines = (string[])initialDialogueLines.Clone();
            DialogueLineSpeakers = initialDialogueLineSpeakers != null ? (DialogueSpeaker[])initialDialogueLineSpeakers.Clone() : null;
            DialogueLineSprites = initialDialogueLineSprites != null ? (Sprite[])initialDialogueLineSprites.Clone() : null;
            isPostQuestLoop = false;
            lineIndex = 0;
        }
    }

    public void SwitchToLoopDialogue()
    {
        if (postQuestLoopLines != null && postQuestLoopLines.Length > 0)
        {
            DialogueLines = postQuestLoopLines;
            DialogueLineSpeakers = new DialogueSpeaker[postQuestLoopLines.Length];
            for (int i = 0; i < DialogueLineSpeakers.Length; i++)
            {
                if (postQuestLoopSpeakers != null && i < postQuestLoopSpeakers.Length)
                {
                    DialogueLineSpeakers[i] = postQuestLoopSpeakers[i];
                }
                else
                {
                    DialogueLineSpeakers[i] = DialogueSpeaker.NPC;
                }
            }
            DialogueLineSprites = null;
            currentNpcPoseSprite = null;
            isPostQuestLoop = true;
        }
    }

    public void SetPostQuestLoopLines(string[] loopLines)
    {
        postQuestLoopLines = loopLines;
        SwitchToLoopDialogue();
    }

    private static bool IsPlayerCharacterSprite(Sprite s)
    {
        if (s == null) return false;
        string n = s.name;
        return n.StartsWith("idle_happy", StringComparison.OrdinalIgnoreCase)
            || n.StartsWith("idle_sad", StringComparison.OrdinalIgnoreCase)
            || n.StartsWith("idle1_v1.5", StringComparison.OrdinalIgnoreCase)
            || n.StartsWith("crouch_", StringComparison.OrdinalIgnoreCase)
            || n.StartsWith("Male_", StringComparison.OrdinalIgnoreCase);
    }

    private void ApplyPlayerLineSprite(Sprite s)
    {
        if (s == null) return;
        GameObject pObj = cachedPlayerObj != null ? cachedPlayerObj : GameObject.FindGameObjectWithTag("Player");
        if (pObj == null) pObj = GameObject.Find("Player");
        if (pObj == null) return;
        CachePlayerComponents(pObj);

        if (cachedPlayerSr != null)
        {
            if (playerDefaultIdleSprite == null)
            {
                playerDefaultIdleSprite = cachedPlayerSr.sprite;
            }
            isPlayerSpriteOverriddenByLine = true;
            if (cachedPlayerAnim != null && cachedPlayerAnim.enabled)
            {
                cachedPlayerAnim.enabled = false;
            }
            cachedPlayerSr.sprite = s;
        }
    }

    private void RestorePlayerDefaultSprite()
    {
        if (!isPlayerSpriteOverriddenByLine) return;
        isPlayerSpriteOverriddenByLine = false;
        if (cachedPlayerSr != null && playerDefaultIdleSprite != null)
        {
            cachedPlayerSr.sprite = playerDefaultIdleSprite;
        }
        if (cachedPlayerAnim != null && !cachedPlayerAnim.enabled)
        {
            cachedPlayerAnim.enabled = true;
            if (cachedPlayerComp != null && cachedPlayerComp.isSwimming)
            {
                cachedPlayerAnim.Play("Player_swim");
            }
            else
            {
                cachedPlayerAnim.Play("Player_Idle");
            }
        }
    }

    private void ApplyNPCSpriteWithScale(Sprite targetSprite)
    {
        if (npcSpriteRenderer == null || targetSprite == null) return;

        if (!hasCachedInitialNpcScale)
        {
            initialNpcLocalScale = npcSpriteRenderer.transform.localScale;
            hasCachedInitialNpcScale = true;
        }

        npcSpriteRenderer.sprite = targetSprite;

        // Penyesuaian proporsi skala otomatis ketika Tetua Adat Gag beralih dari Tiduran (skala 0.27) ke Duduk/Idle (skala 0.476458)
        if (idleSprite != null && idleSprite.name.IndexOf("Tiduran_Gag", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            bool targetIsTiduran = targetSprite.name.IndexOf("Tiduran_Gag", StringComparison.OrdinalIgnoreCase) >= 0;
            if (!targetIsTiduran && Mathf.Abs(initialNpcLocalScale.y) < 0.35f)
            {
                const float ratio = 1.76466f; // 0.476458f / 0.27f
                npcSpriteRenderer.transform.localScale = new Vector3(
                    initialNpcLocalScale.x * ratio,
                    initialNpcLocalScale.y * ratio,
                    initialNpcLocalScale.z);
            }
            else
            {
                npcSpriteRenderer.transform.localScale = initialNpcLocalScale;
            }
            return;
        }

        // Samakan ukuran visual sprite DialogueLineSprites dengan scale awal di Hierarchy
        // (mengatasi sprite ekspresi tambahan yang resolusi/PPU-nya berbeda dengan sprite idle)
        if (idleSprite != null && targetSprite != idleSprite)
        {
            float baseHeight = idleSprite.bounds.size.y;
            float targetHeight = targetSprite.bounds.size.y;
            if (baseHeight > 0.0001f && targetHeight > 0.0001f)
            {
                float ratio = baseHeight / targetHeight;
                npcSpriteRenderer.transform.localScale = new Vector3(
                    initialNpcLocalScale.x * ratio,
                    initialNpcLocalScale.y * ratio,
                    initialNpcLocalScale.z);
                return;
            }
        }

        npcSpriteRenderer.transform.localScale = initialNpcLocalScale;
    }

    private void ResetNPCSprite()
    {
        if (npcSpriteRenderer != null && idleSprite != null)
        {
            ApplyNPCSpriteWithScale(idleSprite);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (CheckIsCutscene()) return;
        if (!PassesQuestPrerequisite()) return;
        if (collision.CompareTag("Player"))
        {
            isPlayerInCollider = true;
            if (InteractionInputLock.IsAnyOverlayBlockingDialogue)
            {
                canInteract = false;
                if (InteractPrompt != null && InteractPrompt.activeSelf)
                {
                    InteractPrompt.SetActive(false);
                }
                return;
            }

            if (autoTriggerOnPlayerEnter && !hasBeenTriggered && !isInteracting &&
                (activeDialogueInstance == null || !activeDialogueInstance.isInteracting))
            {
                hasBeenTriggered = true;
                canInteract = false;
                if (InteractPrompt != null && InteractPrompt.activeSelf)
                {
                    InteractPrompt.SetActive(false);
                }
                StartDialogue();
                return;
            }

            if (!allowManualInteract) return;
            if (!isInteracting && (activeDialogueInstance == null || !activeDialogueInstance.isInteracting))
            {
                canInteract = true;
                if (InteractPrompt != null && !InteractPrompt.activeSelf)
                {
                    EnsureInteractPromptHierarchy();
                    InteractPrompt.SetActive(true);
                }
            }
        }
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (CheckIsCutscene()) return;
        if (!PassesQuestPrerequisite() || InteractionInputLock.IsAnyOverlayBlockingDialogue)
        {
            canInteract = false;
            if (InteractPrompt != null && InteractPrompt.activeSelf)
            {
                InteractPrompt.SetActive(false);
            }
            return;
        }
        if (collision.CompareTag("Player"))
        {
            isPlayerInCollider = true;
            if (!allowManualInteract || isInteracting ||
                (activeDialogueInstance != null && activeDialogueInstance.isInteracting))
            {
                if (InteractPrompt != null && InteractPrompt.activeSelf)
                {
                    InteractPrompt.SetActive(false);
                }
                return;
            }

            canInteract = true;
            if (InteractPrompt != null && !InteractPrompt.activeSelf)
            {
                EnsureInteractPromptHierarchy();
                InteractPrompt.SetActive(true);
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
        EnsureHierarchyActiveForCoroutine();

        if (isInteracting) return;
        if (activeDialogueInstance != null && activeDialogueInstance != this
            && activeDialogueInstance.isInteracting && activeDialogueInstance.gameObject.activeInHierarchy)
        {
            return;
        }

        EnsureWaisaiDialogueStandard();
        RefreshPostQuestLoopState();

        if (InteractPrompt != null) InteractPrompt.SetActive(false);

        canInteract = false;
        isInteracting = true;
        activeDialogueInstance = this;
        lineIndex = 0;
        currentNpcPoseSprite = null;
        isPlayerSpriteOverriddenByLine = false;
        lastAppliedCanvasOrtho = -1f;
        InteractionInputLock.ConsumeInteractKeyThisFrame();

        // --- KUNCI GERAKAN PLAYER & NOLKAN VELOCITY ---
        SetPlayerMovementLock(true);

        ApplyDialogueCameraZoom();

        if (PlayerDialogueBox != null) PlayerDialogueBox.SetActive(false);
        if (SecondaryNPCDialogueBox != null) SecondaryNPCDialogueBox.SetActive(false);
        if (NextPrompt != null) NextPrompt.SetActive(false);
        if (PlayerNextPrompt != null) PlayerNextPrompt.SetActive(false);
        if (SecondaryNPCNextPrompt != null) SecondaryNPCNextPrompt.SetActive(false);

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
            Transform charTr = GetCharacterTransform();
            dialogueCamera.Target.TrackingTarget = charTr;

            var dialogueFollow = dialogueCamera.GetComponent<Unity.Cinemachine.CinemachineFollow>();
            if (dialogueFollow != null)
            {
                if (!hasSavedDialogueFollowOffset)
                {
                    savedDialogueFollowOffset = dialogueFollow.FollowOffset;
                    hasSavedDialogueFollowOffset = true;
                }

                if (matchGameplayCameraHeight)
                {
                    CinemachineCamera gameplayCam = FindPrimaryGameplayCamera();
                    float activeOrtho = npcLensSize > 0f
                        ? npcLensSize
                        : (gameplayCam != null && gameplayCam.Lens.OrthographicSize > 0.1f ? gameplayCam.Lens.OrthographicSize : 2.5f);

                    float targetWorldY = charTr.position.y + savedDialogueFollowOffset.y;
                    if (gameplayCam != null)
                    {
                        targetWorldY = gameplayCam.transform.position.y;
                        Transform gameplayTarget = gameplayCam.Target.TrackingTarget;
                        var gameplayFollow = gameplayCam.GetComponent<Unity.Cinemachine.CinemachineFollow>();
                        if (gameplayTarget != null && gameplayFollow != null)
                        {
                            targetWorldY = gameplayTarget.position.y + gameplayFollow.FollowOffset.y;
                        }
                    }

                    if (preventDialogueBoxCameraOverlap)
                    {
                        targetWorldY = EnsureTargetWorldYContainsDialogueBox(targetWorldY, activeOrtho);
                    }

                    dialogueFollow.FollowOffset = new Vector3(
                        savedDialogueFollowOffset.x,
                        targetWorldY - charTr.position.y,
                        savedDialogueFollowOffset.z
                    );
                }
            }

            // Terapkan ukuran lensa yang ditentukan di Inspector (atau ikuti kamera non-dialog bila tidak diset)
            if (npcLensSize > 0f)
            {
                var lens = dialogueCamera.Lens;
                lens.OrthographicSize = npcLensSize;
                dialogueCamera.Lens = lens;
            }
            else
            {
                CinemachineCamera gameplayCam = FindPrimaryGameplayCamera();
                if (gameplayCam != null && gameplayCam.Lens.OrthographicSize > 0.1f)
                {
                    var lens = dialogueCamera.Lens;
                    lens.OrthographicSize = gameplayCam.Lens.OrthographicSize;
                    dialogueCamera.Lens = lens;
                }
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

    private CinemachineCamera FindPrimaryGameplayCamera()
    {
        CinemachineCamera gameplayCam = null;
        CinemachineCamera[] allCams = FindObjectsByType<CinemachineCamera>(FindObjectsSortMode.None);
        foreach (CinemachineCamera cam in allCams)
        {
            if (cam == null || cam == dialogueCamera) continue;
            if (cam.name.Equals("CinemachineCamera", StringComparison.OrdinalIgnoreCase))
            {
                return cam;
            }
            if (gameplayCam == null && cam.name.IndexOf("Dialogue", StringComparison.OrdinalIgnoreCase) < 0)
            {
                gameplayCam = cam;
            }
        }
        return gameplayCam;
    }

    private float EnsureTargetWorldYContainsDialogueBox(float targetWorldY, float activeOrtho)
    {
        Transform charTr = GetCharacterTransform();
        float pScaleY = Mathf.Abs(charTr.lossyScale.y);
        if (pScaleY <= 0.0001f) pScaleY = 1f;

        float boxBottomWorldY = charTr.position.y + CalculateCharacterHeadOffsetY(charTr.gameObject, pScaleY, activeOrtho);
        float worldScale = GetCameraCompensatedWorldScale(dialogueCamera, activeOrtho, isInteracting);
        float estimatedBoxHeightPx = 168f;
        if (DialogueBox != null)
        {
            RectTransform rt = DialogueBox.GetComponent<RectTransform>();
            if (rt != null && rt.rect.height > 80f)
            {
                estimatedBoxHeightPx = Mathf.Max(estimatedBoxHeightPx, rt.rect.height);
            }
        }

        float expectedTopWorldY = boxBottomWorldY + estimatedBoxHeightPx * worldScale;
        float topMargin = Mathf.Clamp(0.22f * (activeOrtho / 2.5f), 0.16f, 0.45f);
        float minCamYForTop = (expectedTopWorldY + topMargin) - activeOrtho;

        // Pastikan kaki NPC tetap masuk di batas bawah layar
        float maxCamYForBottom = (charTr.position.y - 0.15f) + activeOrtho;
        if (targetWorldY < minCamYForTop && minCamYForTop <= maxCamYForBottom)
        {
            targetWorldY = minCamYForTop;
        }
        return targetWorldY;
    }

    private void ClampDialogueCameraFollowToVisibleBoxes(float activeOrtho)
    {
        if (!preventDialogueBoxCameraOverlap || dialogueCamera == null || CheckIsCutscene()) return;
        if (cachedFollowCamSource != dialogueCamera || cachedDialogueFollow == null)
        {
            cachedFollowCamSource = dialogueCamera;
            cachedDialogueFollow = dialogueCamera.GetComponent<Unity.Cinemachine.CinemachineFollow>();
        }
        var dialogueFollow = cachedDialogueFollow;
        if (dialogueFollow == null) return;

        float highestBoxTop = float.MinValue;
        highestBoxTop = Mathf.Max(highestBoxTop, GetActiveBoxTopWorldY(DialogueBox));
        highestBoxTop = Mathf.Max(highestBoxTop, GetActiveBoxTopWorldY(PlayerDialogueBox));
        highestBoxTop = Mathf.Max(highestBoxTop, GetActiveBoxTopWorldY(SecondaryNPCDialogueBox));
        if (highestBoxTop <= float.MinValue * 0.5f) return;

        Transform charTr = GetCharacterTransform();
        float currentCamY = charTr.position.y + dialogueFollow.FollowOffset.y;
        float topMargin = Mathf.Clamp(0.18f * (activeOrtho / 2.5f), 0.12f, 0.35f);
        float neededCamY = (highestBoxTop + topMargin) - activeOrtho;
        float maxCamYForBottom = (charTr.position.y - 0.15f) + activeOrtho;
        if (neededCamY > currentCamY && neededCamY <= maxCamYForBottom)
        {
            Vector3 off = dialogueFollow.FollowOffset;
            off.y = neededCamY - charTr.position.y;
            dialogueFollow.FollowOffset = off;
        }
    }

    private static float GetActiveBoxTopWorldY(GameObject boxObj)
    {
        if (boxObj == null || !boxObj.activeInHierarchy) return float.MinValue;
        RectTransform rt = boxObj.GetComponent<RectTransform>();
        if (rt == null) return float.MinValue;
        rt.GetWorldCorners(sharedWorldCorners);
        return Mathf.Max(sharedWorldCorners[1].y, sharedWorldCorners[2].y);
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
            if (hasSavedDialogueFollowOffset)
            {
                var dialogueFollow = dialogueCamera.GetComponent<Unity.Cinemachine.CinemachineFollow>();
                if (dialogueFollow != null)
                {
                    dialogueFollow.FollowOffset = savedDialogueFollowOffset;
                }
                hasSavedDialogueFollowOffset = false;
            }
        }
    }
    #endregion
}
