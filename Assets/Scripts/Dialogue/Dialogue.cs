using UnityEngine;
using TMPro;
using System.Collections;
using Unity.Cinemachine;

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

    [Header("Camera Zoom Settings")]
    public CinemachineCamera dialogueCamera;
    [SerializeField] private float npcLensSize = 4.5f;

    private int lineIndex = 0;
    private bool isInteracting = false;
    private bool canInteract = false;
    private bool isTyping = false;
    private bool hasBeenTriggered = false;

    private Coroutine typingCoroutine;

    // Reference QuestGiver
    private QuestGiver questGiver;

    // ========================================
    // UNITY
    // ========================================

    private void Awake()
    {
        // Cari QuestGiver di GameObject yang sama
        questGiver = GetComponent<QuestGiver>();
    }

    private void Start()
    {
        // Pastikan kamera dialog mati saat awal
        if (dialogueCamera != null)
        {
            dialogueCamera.Priority.Value = 0;
        }

        if (InteractPrompt != null)
            InteractPrompt.SetActive(false);

        if (DialogueBox != null)
            DialogueBox.SetActive(false);

        if (NextPrompt != null)
            NextPrompt.SetActive(false);
    }

    private void Update()
    {
        // ====================================
        // MULAI DIALOGUE
        // ====================================

        if (canInteract &&
            !isInteracting &&
            hasBeenTriggered &&
            Input.GetKeyDown(KeyCode.E))
        {
            StartDialogue();
        }

        // ====================================
        // NEXT DIALOGUE
        // ====================================

        else if (isInteracting &&
                 NextPrompt != null &&
                 NextPrompt.activeInHierarchy &&
                 Input.GetKeyDown(KeyCode.E))
        {
            NextLine();
        }
    }

    // ========================================
    // START DIALOGUE
    // ========================================

    private void StartDialogue()
    {
        canInteract = false;
        isInteracting = true;

        // Kamera dialog
        if (dialogueCamera != null)
        {
            dialogueCamera.Target.TrackingTarget = this.transform;
            dialogueCamera.Lens.OrthographicSize = npcLensSize;
            dialogueCamera.Priority.Value = 20;
        }

        if (InteractPrompt != null)
            InteractPrompt.SetActive(false);

        if (NextPrompt != null)
            NextPrompt.SetActive(false);

        if (DialogueBox != null)
            DialogueBox.SetActive(true);

        if (DialogueText != null)
        {
            DialogueText.text = "";
            DialogueText.maxVisibleCharacters = 0;
        }

        lineIndex = 0;

        StartTyping();
    }

    // ========================================
    // START TYPING
    // ========================================

    private void StartTyping()
    {
        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        typingCoroutine = StartCoroutine(WriteLine());
    }

    // ========================================
    // WRITE LINE
    // ========================================

    private IEnumerator WriteLine()
    {
        isTyping = true;

        if (DialogueLines == null ||
            DialogueLines.Length == 0)
        {
            EndDialogue();
            yield break;
        }

        DialogueText.text = DialogueLines[lineIndex];
        DialogueText.maxVisibleCharacters = 0;

        int totalCharacters =
            DialogueLines[lineIndex].Length;

        for (int i = 0; i <= totalCharacters; i++)
        {
            DialogueText.maxVisibleCharacters = i;

            yield return new WaitForSeconds(TypeSpeed);
        }

        isTyping = false;

        if (NextPrompt != null)
            NextPrompt.SetActive(true);
    }

    // ========================================
    // NEXT LINE
    // ========================================

    private void NextLine()
    {
        // Jangan lanjut kalau teks masih mengetik
        if (isTyping)
            return;

        // Masih ada dialogue berikutnya
        if (lineIndex < DialogueLines.Length - 1)
        {
            lineIndex++;

            if (NextPrompt != null)
                NextPrompt.SetActive(false);

            StartTyping();
        }
        else
        {
            // =================================
            // DIALOGUE BENAR-BENAR SELESAI
            // =================================

            // Kalau NPC memiliki QuestGiver,
            // berikan quest setelah dialogue selesai.
            if (questGiver != null)
            {
                questGiver.GiveQuest();
            }

            EndDialogue();
        }
    }

    // ========================================
    // END DIALOGUE
    // ========================================

    private void EndDialogue()
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }

        isTyping = false;
        isInteracting = false;

        if (DialogueBox != null)
            DialogueBox.SetActive(false);

        if (NextPrompt != null)
            NextPrompt.SetActive(false);

        // Kembalikan kamera
        if (dialogueCamera != null)
        {
            dialogueCamera.Priority.Value = 0;
        }

        if (canInteract &&
            InteractPrompt != null)
        {
            InteractPrompt.SetActive(true);
        }
    }

    // ========================================
    // PLAYER ENTER
    // ========================================

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player"))
            return;

        canInteract = true;

        if (!isInteracting)
        {
            if (!hasBeenTriggered)
            {
                hasBeenTriggered = true;

                // Dialogue pertama otomatis dimulai
                StartDialogue();
            }
            else if (InteractPrompt != null)
            {
                InteractPrompt.SetActive(true);
            }
        }
    }

    // ========================================
    // PLAYER EXIT
    // ========================================

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player"))
            return;

        canInteract = false;

        if (InteractPrompt != null)
            InteractPrompt.SetActive(false);

        if (isInteracting)
        {
            // Keluar dari trigger hanya menutup dialogue.
            // TIDAK memberikan quest.
            EndDialogue();
        }
    }
}