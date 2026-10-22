using System.Collections;
using UnityEngine;
using Unity.Cinemachine; // Namespace Cinemachine untuk Unity 6

public class BiotaInteractable : MonoBehaviour, IInteractable
{
    [Header("Behavior")]
    [SerializeField] private string biotaName = "Penyu Laut";
    [SerializeField] private SequenceQTE qteToTrigger;
    [SerializeField] private string objectiveID = "inspect_turtle";

    [Header("Animation & Camera")]
    [SerializeField] private Animator animator;
    [SerializeField] private string animationTrigger = "SelesaiInteraksi";

    [Header("Cinemachine Settings")]
    [SerializeField] private CinemachineCamera dialogueCamera; // Komponen Cinemachine Camera di Unity 6
    [SerializeField] private int activePriority = 20;   // Priority saat Zoom In
    [SerializeField] private int defaultPriority = 0;   // Priority normal (Standby)
    [SerializeField] private float delayBeforeAnim = 1f;
    [SerializeField] private float delayAfterAnim = 2f;

    private bool isDone = false;

    private void Start()
    {
        if (animator != null)
        {
            animator.enabled = false;
        }

        // Pastikan priority awal berada di posisi standby
        if (dialogueCamera != null)
        {
            dialogueCamera.Priority.Value = defaultPriority;
        }
    }

    public void Interact()
    {
        if (isDone) return;

        if (qteToTrigger != null)
        {
            qteToTrigger.StartQTE();
            qteToTrigger.onSuccess.RemoveAllListeners();
            qteToTrigger.onSuccess.AddListener(OnQTESuccess);
            return;
        }

        OnQTESuccess();
    }

    private void OnQTESuccess()
    {
        StartCoroutine(CinematicSequence());
    }

    private IEnumerator CinematicSequence()
    {
        // 1. Zoom In: Naikkan Priority agar Cinemachine beralih ke kamera ini
        if (dialogueCamera != null)
        {
            dialogueCamera.Priority.Value = activePriority;
        }

        // 2. Jeda sebelum animasi berjalan
        yield return new WaitForSeconds(delayBeforeAnim);

        // 3. Mainkan Animasi Penyu
        if (animator != null && !string.IsNullOrEmpty(animationTrigger))
        {
            animator.enabled = true;
            animator.SetTrigger(animationTrigger);
        }

        // 4. Jeda menikmati animasi
        yield return new WaitForSeconds(delayAfterAnim);

        // 5. Zoom Out: Kembalikan Priority ke semula
        if (dialogueCamera != null)
        {
            dialogueCamera.Priority.Value = defaultPriority;
        }

        // 6. Update Quest
        if (QuestController.Instance != null && !string.IsNullOrEmpty(objectiveID))
        {
            QuestController.Instance.ProgressObjective(objectiveID, 1);
        }

        isDone = true;
        Debug.Log($"Interaksi dengan {biotaName} selesai!");
    }

    public string GetPromptText() => isDone ? biotaName : $"Bantu {biotaName}";
}