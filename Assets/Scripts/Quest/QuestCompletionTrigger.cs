using UnityEngine;
using UnityEngine.Events;

public class QuestCompletionTrigger : MonoBehaviour
{
    [Header("Quest Target")]
    [Tooltip("ID Quest yang harus diselesaikan untuk memicu event ini")]
    [SerializeField] private string requiredQuestID = "collect_trash";

    [Header("Barrier / Jalan Terkunci")]
    [Tooltip("GameObject rintangan/pembatas jalan yang akan dihilangkan")]
    [SerializeField] private GameObject barrierObject;
    [Tooltip("Bocorkan jalan secara instan dengan Destroy, atau hanya menonaktifkan (SetActive false)")]
    [SerializeField] private bool destroyBarrier = false;

    [Header("Audio & FX (Opsional)")]
    [SerializeField] private AudioClip unlockSound;
    [SerializeField] private GameObject unlockParticleFX;

    [Header("Event Lanjutan (Dialog, Trivia, Quest Baru, dll)")]
    public UnityEvent onQuestCompletedEvents;

    private bool isTriggered = false;

    private void OnEnable()
    {
        SubscribeEvent();
    }

    private void Start()
    {
        // Panggil ulang untuk memastikan tidak terlewat jika QuestController baru siap di Start
        SubscribeEvent();
    }

    private void SubscribeEvent()
    {
        if (QuestController.Instance != null)
        {
            // Lepas dulu agar tidak terdaftar ganda
            QuestController.OnQuestCompleted -= CheckQuestCompletion;
            QuestController.OnQuestCompleted += CheckQuestCompletion;
        }
    }

    private void OnDisable()
    {
        if (QuestController.Instance != null)
        {
            QuestController.OnQuestCompleted -= CheckQuestCompletion;
        }
    }

    private void CheckQuestCompletion(string questID)
    {
        Debug.Log($"[Trigger Check] Received Quest: {questID} | Required: {requiredQuestID}");

        if (isTriggered) return;

        // Periksa apakah ID cocok
        if (questID.Equals(requiredQuestID, System.StringComparison.OrdinalIgnoreCase))
        {
            ExecuteUnlockSequence();
        }
    }

    public void ExecuteUnlockSequence()
    {
        isTriggered = true;

        if (barrierObject != null)
        {
            if (unlockParticleFX != null)
            {
                Instantiate(unlockParticleFX, barrierObject.transform.position, Quaternion.identity);
            }

            if (destroyBarrier)
            {
                Destroy(barrierObject);
            }
            else
            {
                // Matikan seluruh objek barrier agar Collider dan Prompt IInteractable ikut mati
                barrierObject.SetActive(false);
            }
        }

        if (unlockSound != null)
        {
            AudioSource.PlayClipAtPoint(unlockSound, transform.position);
        }

        onQuestCompletedEvents?.Invoke();
    }
}