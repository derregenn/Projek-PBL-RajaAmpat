using UnityEngine;
using TMPro;
using System.Collections;
using Unity.Cinemachine;
using UnityEngine.Events;
using System;

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

    private void EnsureInteractPromptHierarchy()
    {
        EnsureInteractPrompt();

        if (InteractPrompt != null)
        {
            // Pastikan InteractPrompt DI DALAM DialogueCanvas, tapi DI LUAR DialogueBox
            Canvas dialogueCanvas = GetComponentInChildren<Canvas>(true);
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

            // Pastikan posisi & scale InteractPrompt sesuai standar Waisai (identik dengan TourGuide)
            RectTransform promptRt = InteractPrompt.GetComponent<RectTransform>();
            if (promptRt != null)
            {
                promptRt.anchorMin = new Vector2(0.5f, 0.5f);
                promptRt.anchorMax = new Vector2(0.5f, 0.5f);
                promptRt.pivot = new Vector2(0.5f, 0.5f);
                promptRt.sizeDelta = new Vector2(200f, 50f);
                promptRt.localScale = Vector3.one;
                promptRt.anchoredPosition = new Vector2(0f, -73.5f);
            }
        }
    }

    private void EnsureWaisaiDialogueStandard()
    {
        // 1. Standar Canvas (Normalisasi World Scale & Offset agar identik di SEMUA NPC di SEMUA SCENE)
        // Standar golden: TourGuide scale = 0.2434f, canvas localScale = 0.05109428f -> worldScale = 0.01243635f
        // Offset Y golden: 11.06f * 0.2434f = 2.692f world units di atas posisi pivot NPC
        Canvas dialogueCanvas = GetComponentInChildren<Canvas>(true);
        if (dialogueCanvas != null)
        {
            RectTransform cRt = dialogueCanvas.GetComponent<RectTransform>();
            if (cRt != null)
            {
                cRt.sizeDelta = new Vector2(1000f, 600f);

                float pScaleX = Mathf.Abs(transform.lossyScale.x);
                float pScaleY = Mathf.Abs(transform.lossyScale.y);

                const float targetWorldScale = 0.01243635f;
                const float targetWorldOffsetY = 2.692f;

                if (pScaleX > 0.0001f && pScaleY > 0.0001f)
                {
                    float sX = targetWorldScale / pScaleX;
                    float sY = targetWorldScale / pScaleY;
                    cRt.localScale = new Vector3(sX, sY, 1f);
                    cRt.anchoredPosition = new Vector2(0f, targetWorldOffsetY / pScaleY);
                }
                else
                {
                    cRt.localScale = new Vector3(0.05109428f, 0.05109428f, 1f);
                    cRt.anchoredPosition = new Vector2(0f, 11.06f);
                }
            }
        }

        // 2. Standar DialogueBox (Ukuran 300 x 112.69)
        if (DialogueBox != null)
        {
            RectTransform boxRt = DialogueBox.GetComponent<RectTransform>();
            if (boxRt != null)
            {
                boxRt.anchorMin = new Vector2(0.5f, 0.5f);
                boxRt.anchorMax = new Vector2(0.5f, 0.5f);
                boxRt.pivot = new Vector2(0.5f, 0.5f);
                boxRt.sizeDelta = new Vector2(300f, 112.69f);
                boxRt.localScale = Vector3.one;
            }

            // Pastikan NextPrompt terhubung jika ada child button / NextButton
            if (NextPrompt == null)
            {
                Transform btnNext = DialogueBox.transform.Find("NextButton");
                if (btnNext == null) btnNext = DialogueBox.transform.Find("Btn_Next");
                if (btnNext == null) btnNext = DialogueBox.transform.Find("NextPrompt");
                if (btnNext != null)
                {
                    NextPrompt = btnNext.gameObject;
                }
            }
        }

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

        if (InteractPrompt != null)
        {
            Canvas promptCanvas = InteractPrompt.GetComponent<Canvas>();
            if (promptCanvas == null)
            {
                promptCanvas = InteractPrompt.AddComponent<Canvas>();
            }
            promptCanvas.overrideSorting = true;
            promptCanvas.sortingLayerName = "Foreground";
            promptCanvas.sortingOrder = 110;
        }
    }

    private void CheckProximity()
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

            if (inProximity || isPlayerInCollider)
            {
                canInteract = true;
                if (InteractPrompt != null && !InteractPrompt.activeSelf)
                {
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
            if (PlayerNextPrompt != null) PlayerNextPrompt.SetActive(true);
        }
        else if (NextPrompt != null)
        {
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
