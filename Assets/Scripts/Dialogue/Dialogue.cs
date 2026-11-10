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
    [SerializeField] private float npcLensSize = 4.5f;

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
        
        // Cari skrip Player pada objek ber-tag Player
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerMovementScript = playerObj.GetComponent<Player>(); 
        }
    }

    private void Start()
    {
        if (dialogueCamera != null)
        {
            dialogueCamera.Priority.Value = 0;
        }

        if (postQuestLoopLines != null && postQuestLoopLines.Length > 0)
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

        // --- KUNCI GERAKAN PLAYER ---
        if (playerMovementScript != null)
        {
            playerMovementScript.enabled = false;
        }

        if (dialogueCamera != null)
        {
            dialogueCamera.Target.TrackingTarget = this.transform;
            dialogueCamera.Lens.OrthographicSize = npcLensSize;
            dialogueCamera.Priority.Value = 20;

            CinemachineConfiner2D confiner = dialogueCamera.GetComponent<CinemachineConfiner2D>();
            if (confiner != null) confiner.InvalidateBoundingShapeCache();
        }

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
        if (playerMovementScript != null)
        {
            playerMovementScript.enabled = true;
        }

        ResetNPCSprite();

        if (DialogueBox != null) DialogueBox.SetActive(false);
        if (PlayerDialogueBox != null) PlayerDialogueBox.SetActive(false);
        if (NextPrompt != null) NextPrompt.SetActive(false);
        if (PlayerNextPrompt != null) PlayerNextPrompt.SetActive(false);

        if (dialogueCamera != null)
        {
            dialogueCamera.Priority.Value = 0;

            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                dialogueCamera.Target.TrackingTarget = playerObj.transform;
            }
        }

        // PERBAIKAN: Jika player masih di dalam area collider, aktifkan lagi izin interaksi & prompt
        if (isPlayerInCollider)
        {
            canInteract = true;
            if (InteractPrompt != null) InteractPrompt.SetActive(true);
        }

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

        if (!isPostQuestLoop)
        {
            onDialogueEnd?.Invoke();
            if (postQuestLoopLines != null && postQuestLoopLines.Length > 0)
            {
                SwitchToLoopDialogue();
            }
        }

        onDialogueCompleteCallback?.Invoke();
        onDialogueCompleteCallback = null;
    }

    public void SwitchToLoopDialogue()
    {
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
            canInteract = true;

            if (!isInteracting && InteractPrompt != null)
            {
                InteractPrompt.SetActive(true);
            }
        }
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (!isPlayerInCollider && collision.CompareTag("Player"))
        {
            isPlayerInCollider = true;
            canInteract = true;

            if (!isInteracting && InteractPrompt != null)
            {
                InteractPrompt.SetActive(true);
            }
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            isPlayerInCollider = false;
            canInteract = false;

            if (InteractPrompt != null) InteractPrompt.SetActive(false);
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

        if (playerMovementScript != null)
        {
            playerMovementScript.enabled = false;
        }

        if (dialogueCamera != null)
        {
            dialogueCamera.Target.TrackingTarget = this.transform;
            dialogueCamera.Lens.OrthographicSize = npcLensSize;
            dialogueCamera.Priority.Value = 20;

            CinemachineConfiner2D confiner = dialogueCamera.GetComponent<CinemachineConfiner2D>();
            if (confiner != null) confiner.InvalidateBoundingShapeCache();
        }

        if (PlayerDialogueBox != null) PlayerDialogueBox.SetActive(false);
        if (NextPrompt != null) NextPrompt.SetActive(false);
        if (PlayerNextPrompt != null) PlayerNextPrompt.SetActive(false);

        StartTyping();
    }
}