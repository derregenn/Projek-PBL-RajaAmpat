using UnityEngine;

public class PhotoAreaTrigger : MonoBehaviour
{
    [Header("Quest & Journal Integration")]
    public string objectiveID = "Foto_alam"; // SAMA PERSIS dengan Objective ID di Inspector Quest 3
    public int photoID = 0;                  // Element 0 di All Photo Sprites Database
    public int pageIDToUnlock = 1;           // Page ID 1 di All Journal Database (Halaman Arborek)

    private bool isPlayerInside = false;

    private void Update()
    {
        if (isPlayerInside && Input.GetKeyDown(KeyCode.F))
        {
            // 1. Selesaikan Objective Quest 'Foto_alam'
            if (QuestController.Instance != null)
            {
                QuestController.Instance.ProgressObjective(objectiveID, 1);
            }

            // 2. Simpan Foto & Buka Halaman Jurnal Arborek
            if (QuestJournalManager.Instance != null)
            {
                QuestJournalManager.Instance.TakeSpecificPhoto(photoID);    // Pasang foto Arborek
                QuestJournalManager.Instance.UnlockNewPage(pageIDToUnlock); // Buka Halaman Arborek
                QuestJournalManager.Instance.AddCompletedQuest();        // Tambah % progress

                QuestJournalManager.Instance.currentOngoingQuest = "";
                QuestJournalManager.Instance.lastCompletedQuest = "Memotret Pemandangan Sasi Laut";
            }

            Debug.Log("Quest Foto Selesai & Foto masuk ke Jurnal Halaman 2!");
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player")) isPlayerInside = true;
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player")) isPlayerInside = false;
    }
}