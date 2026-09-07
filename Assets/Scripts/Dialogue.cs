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
    public CinemachineCamera dialogueCamera; // Hubungkan ke CinemachineCameraDialogue
    [SerializeField] private float npcLensSize = 4.5f; // Jarak zoom khusus NPC ini

    private int lineIndex = 0;
    private bool isInteracting = false;
    private bool canInteract = false;
    private bool isTyping = false;
    private bool hasBeenTriggered = false;

    private Coroutine typingCoroutine;

    private void Start()
    {
        // Pastikan prioritas kamera dialog mati saat awal level
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
        if (canInteract && !isInteracting && hasBeenTriggered && Input.GetKeyDown(KeyCode.E))
        {
            StartDialogue();
        }
        else if (isInteracting &&
                 NextPrompt != null &&
                 NextPrompt.activeInHierarchy &&
                 Input.GetKeyDown(KeyCode.E))
        {
            NextLine();
        }
    }

    private void StartDialogue()
    {
        canInteract = false;
        isInteracting = true;

        // --- PINDAHKAN KAMERA KE NPC INI & ZOOM ---
        if (dialogueCamera != null)
        {
            dialogueCamera.Target.TrackingTarget = this.transform; // Set target ke NPC ini
            dialogueCamera.Lens.OrthographicSize = npcLensSize;    // Set ukuran zoom
            dialogueCamera.Priority.Value = 20;                   // Naikkan priority agar aktif
        }

        if (InteractPrompt != null)
            InteractPrompt.SetActive(false);

        if (NextPrompt != null)
            NextPrompt.SetActive(false);

        if (DialogueBox != null)
            DialogueBox.SetActive(true);

        if (DialogueText != null)
            DialogueText.text = "";

        lineIndex = 0;
        StartTyping();
    }

    private void StartTyping()
    {
        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

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

        // Masukkan seluruh teks kalimat sekaligus agar kotak dialog langsung mengembang ke ukuran pasnya
        DialogueText.text = DialogueLines[lineIndex];
        DialogueText.maxVisibleCharacters = 0; // Sembunyikan karakter dulu

        int totalCharacters = DialogueLines[lineIndex].Length;

        for (int i = 0; i <= totalCharacters; i++)
        {
            DialogueText.maxVisibleCharacters = i; // Tampilkan satu per satu
            yield return new WaitForSeconds(TypeSpeed);
        }

        isTyping = false;

        if (NextPrompt != null)
            NextPrompt.SetActive(true);
    }

    private void NextLine()
    {
        if (isTyping)
            return;

        if (lineIndex < DialogueLines.Length - 1)
        {
            lineIndex++;

            if (NextPrompt != null)
                NextPrompt.SetActive(false);

            StartTyping();
        }
        else
        {
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

        if (DialogueBox != null)
            DialogueBox.SetActive(false);

        if (NextPrompt != null)
            NextPrompt.SetActive(false);

        // --- KEMBALIKAN KAMERA KE GAMEPLAY ---
        if (dialogueCamera != null)
        {
            dialogueCamera.Priority.Value = 0; // Kembalikan kontrol ke kamera utama
        }

        if (canInteract && InteractPrompt != null)
            InteractPrompt.SetActive(true);
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

            if (InteractPrompt != null)
                InteractPrompt.SetActive(false);

            if (isInteracting)
            {
                EndDialogue();
            }
        }
    }
}