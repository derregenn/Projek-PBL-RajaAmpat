using UnityEngine;

public class PhotoTriggerArea : MonoBehaviour
{
    [Header("ID Foto Jurnal")]
    [Tooltip("Isi dengan ID yang ada di database QuestJournalManager (misal: 1 untuk Sasi)")]
    public int photoIDToTrigger; 

    private bool isPlayerInZone = false;
    public GameObject interactPromptUI; // Ikon 'F' yang muncul di atas kepala (Opsional)

    void Update()
    {
        if (isPlayerInZone && Input.GetKeyDown(KeyCode.F))
        {
            // Suruh Manager menjepret foto dengan ID ini
            QuestJournalManager.Instance.TakeSpecificPhoto(photoIDToTrigger);
            
            // Hapus area ini agar tidak bisa difoto 2 kali
            gameObject.SetActive(false); 
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            isPlayerInZone = true;
            if (interactPromptUI != null) interactPromptUI.SetActive(true);
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            isPlayerInZone = false;
            if (interactPromptUI != null) interactPromptUI.SetActive(false);
        }
    }
}