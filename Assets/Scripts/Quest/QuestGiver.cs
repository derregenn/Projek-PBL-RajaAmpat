using UnityEngine;

public class QuestGiver : MonoBehaviour
{
    [Header("Quest")]
    [SerializeField] private Quest questToGive;

    [Header("Physical Collision (2D)")]
    [SerializeField] private bool giveQuestOnPlayerCollision = true;
    [SerializeField] private bool requireBoxCollider2D;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Pastikan objek yang masuk memiliki Tag "Player"
        if (collision.CompareTag("Player"))
        {
            // Panggil fungsi pemberian quest di sini
            // Misal: GiveQuest(); atau QuestController.Instance.GiveQuest(questToGive);
            Debug.Log("Pemain memasuki area quest!");
        }
    }

    public void GiveQuest()
    {
        if (questToGive == null)
        {
            Debug.LogWarning("QuestGiver: Quest To Give belum diisi.");
            return;
        }

        // Cari QuestController (menggunakan Instance atau cari di Scene)
        QuestController controller = QuestController.Instance;
        if (controller == null)
        {
            controller = FindFirstObjectByType<QuestController>();
        }

        if (controller == null)
        {
            Debug.LogError("QuestGiver: QuestController tidak ditemukan di scene!");
            return;
        }

        if (controller.IsQuestActive(questToGive.QuestID))
        {
            Debug.Log("Quest sudah aktif: " + questToGive.QuestID);
            return;
        }

        controller.AcceptQuest(questToGive);

        Debug.Log("NPC memberikan quest: " + questToGive.QuestTitle);
    }
}