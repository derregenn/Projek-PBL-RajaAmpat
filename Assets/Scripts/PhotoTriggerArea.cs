using UnityEngine;

public class PhotoAreaTrigger : MonoBehaviour
{
    public int photoID = 0; // Sesuaikan dengan indeks di All Photo Sprites Database
    private bool isPlayerInside = false;

    private void Update()
    {
        // Langsung foto jika player menekan F di dalam area tanpa mengecek status quest
        if (isPlayerInside && Input.GetKeyDown(KeyCode.F))
        {
            if (QuestJournalManager.Instance != null)
            {
                QuestJournalManager.Instance.TakeSpecificPhoto(photoID);
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            isPlayerInside = true;
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            isPlayerInside = false;
        }
    }
}