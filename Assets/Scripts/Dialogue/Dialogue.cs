using UnityEngine;
using TMPro;
using System.Collections;
using Unity.Cinemachine;
using UnityEngine.Events;

public class Dialogue : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject InteractPrompt;
    [SerializeField] private GameObject DialogueBox;
    [SerializeField] private TMP_Text DialogueText;
    [SerializeField] private GameObject NextPrompt;

    [Header("Dialogue")]
    [SerializeField] private string[] DialogueLines;
    [SerializeField] private float TypeSpeed = 0.02f;

    [Header("NPC Sprite Settings")]
    [SerializeField] private SpriteRenderer npcSpriteRenderer;
    [SerializeField] private Sprite talkingSprite1; // Drag sprite talking1_V1 di Inspector
    [SerializeField] private Sprite talkingSprite2; // Drag sprite talking2_V1 di Inspector
    [SerializeField] private Sprite idleSprite;      // Sprite default saat NPC diam (opsional)
    [SerializeField] private int spriteToggleInterval = 3; // Berganti sprite tiap N karakter

    [Header("Camera Zoom Settings")]
    public CinemachineCamera dialogueCamera;
    [SerializeField] private float npcLensSize = 4.5f;

    [Header("Dialogue Events")]
    [SerializeField] private UnityEvent onDialogueEnd;

    private int lineIndex = 0;
    private bool isInteracting = false;
    private bool canInteract = false;
    private bool isTyping = false;
    private bool hasBeenTriggered = false;

    private QuestGiver questGiver;
    private Coroutine typingCoroutine;

    private void Awake()
    {
        questGiver = GetComponent<QuestGiver>();

        // Auto-assign SpriteRenderer jika belum diisi di Inspector
        if (npcSpriteRenderer == null)
        {
            npcSpriteRenderer = GetComponent<SpriteRenderer>();
        }

        // Simpan sprite default jika idleSprite belum di-set
        if (npcSpriteRenderer != null && idleSprite == null)
        {
            idleSprite = npcSpriteRenderer.sprite;
        }
    }

    private void Start()
    {
        if (dialogueCamera != null)
        {
            dialogueCamera.Priority.Value = 0;
        }

        if (InteractPrompt != null) InteractPrompt.SetActive(false);
        if (DialogueBox != null) DialogueBox.SetActive(false);
        if (NextPrompt != null) NextPrompt.SetActive(false);
    }

    private void Update()
    {
        if (canInteract && !isInteracting && hasBeenTriggered && Input.GetKeyDown(KeyCode.E))
        {
            StartDialogue();
        }
        else if (isInteracting && NextPrompt != null && NextPrompt.activeInHierarchy && Input.GetKeyDown(KeyCode.E))
        {
            NextLine();
        }
    }

    private void StartDialogue()
    {
        canInteract = false;
        isInteracting = true;

        if (dialogueCamera != null)
        {
            dialogueCamera.Target.TrackingTarget = this.transform;
            dialogueCamera.Lens.OrthographicSize = npcLensSize;
            dialogueCamera.Priority.Value = 20;
        }

        if (InteractPrompt != null) InteractPrompt.SetActive(false);
        if (NextPrompt != null) NextPrompt.SetActive(false);
        if (DialogueBox != null) DialogueBox.SetActive(true);
        if (DialogueText != null) DialogueText.text = "";

        lineIndex = 0;
        StartTyping();
    }

    private void StartTyping()
    {
        if (typingCoroutine != null) StopCoroutine(typingCoroutine);
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

        DialogueText.text = DialogueLines[lineIndex];
        DialogueText.maxVisibleCharacters = 0;

        int totalCharacters = DialogueLines[lineIndex].Length;
        bool toggleSprite = false;

        for (int i = 0; i <= totalCharacters; i++)
        {
            DialogueText.maxVisibleCharacters = i;

            // Animasi pergantian sprite bicaranya NPC saat teks diketik
            if (npcSpriteRenderer != null && talkingSprite1 != null && talkingSprite2 != null)
            {
                if (i % spriteToggleInterval == 0)
                {
                    toggleSprite = !toggleSprite;
                    npcSpriteRenderer.sprite = toggleSprite ? talkingSprite1 : talkingSprite2;
                }
            }

            yield return new WaitForSeconds(TypeSpeed);
        }

        isTyping = false;

        // Kembalikan ke sprite idle/talking1 saat pengetikan selesai
        ResetNPCSprite();

        if (NextPrompt != null) NextPrompt.SetActive(true);
    }

    private void NextLine()
    {
        if (isTyping) return;

        if (lineIndex < DialogueLines.Length - 1)
        {
            lineIndex++;
            if (NextPrompt != null) NextPrompt.SetActive(false);
            StartTyping();
        }
        else
        {
            if (questGiver != null)
            {
                questGiver.GiveQuest();
            }

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

        isTyping = false;
        isInteracting = false;

        ResetNPCSprite();

        if (DialogueBox != null) DialogueBox.SetActive(false);
        if (NextPrompt != null) NextPrompt.SetActive(false);

        if (dialogueCamera != null)
        {
            dialogueCamera.Priority.Value = 0;
        }

        if (canInteract && InteractPrompt != null) InteractPrompt.SetActive(true);

        onDialogueEnd?.Invoke();
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
            canInteract = true;

            if (!isInteracting)
            {
                if (!hasBeenTriggered)
                {
                    hasBeenTriggered = true;
                    StartDialogue();
                }
                else if (InteractPrompt != null)
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
            canInteract = false;

            if (InteractPrompt != null) InteractPrompt.SetActive(false);
            if (isInteracting) EndDialogue();
        }
    }

    public void ShowTriviaQuestion(string questionText)
    {
        if (DialogueBox != null) DialogueBox.SetActive(true);
        if (NextPrompt != null) NextPrompt.SetActive(false);

        if (typingCoroutine != null) StopCoroutine(typingCoroutine);

        DialogueText.text = questionText;
        DialogueText.maxVisibleCharacters = questionText.Length;

        // Gunakan sprite bicara saat pertanyaan trivia ditampilkan
        if (npcSpriteRenderer != null && talkingSprite1 != null)
        {
            npcSpriteRenderer.sprite = talkingSprite1;
        }
    }
}