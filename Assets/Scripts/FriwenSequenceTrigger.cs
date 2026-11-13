using UnityEngine;

public class FriwenSequenceTrigger : MonoBehaviour
{
    [Header("References")]
    public FriwenEventManager eventManager;
    public Quest questToGive;

    [Header("Settings")]
    public bool autoAcceptQuestOnSceneStart = true; // Terima quest otomatis di awal scene
    public bool isQTETriggerZone = false;           // Centang KHUSUS untuk Trigger_QTE_Perahu di sebelah kanan

    private bool hasTriggered = false;

    private void Start()
    {
        // 1. Terima Quest 1 otomatis saat scene dimuat agar langsung muncul di HUD
        if (autoAcceptQuestOnSceneStart && questToGive != null && QuestController.Instance != null)
        {
            QuestController.Instance.AcceptQuest(questToGive);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasTriggered) return;

        if (other.CompareTag("Player"))
        {
            hasTriggered = true;

            // 2. Hanya jalankan sequence perahu jika player menyentuh trigger di sebelah kanan dekat perahu
            if (isQTETriggerZone && eventManager != null)
            {
                eventManager.StartSequencePerahu(this);
            }
        }
    }

    public void ResetTrigger()
    {
        hasTriggered = false;
    }
}