using UnityEngine;

public class NPCQuestGiver : MonoBehaviour
{
    public GameObject interactUI; // Ikon 'E'
    private bool isPlayerNearby = false;
    private Player playerScript;

    void Update()
    {
        // Jika pemain dekat, menekan E, dan belum punya peta
        if (isPlayerNearby && Input.GetKeyDown(KeyCode.E))
        {
            if (playerScript != null && !playerScript.hasMap)
            {
                playerScript.hasMap = true;
                Debug.Log("Peta Waisai didapatkan!"); // Bisa diganti dengan memunculkan UI Peta
                
                if (interactUI != null) interactUI.SetActive(false); // Sembunyikan ikon E
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.tag == "Player") //[cite: 3, 5]
        {
            isPlayerNearby = true;
            playerScript = collision.gameObject.GetComponent<Player>(); //[cite: 5]
            
            // Munculkan ikon E hanya jika pemain belum punya peta
            if (interactUI != null && playerScript != null && !playerScript.hasMap)
            {
                interactUI.SetActive(true);
            }
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.gameObject.tag == "Player") //[cite: 3, 5]
        {
            isPlayerNearby = false;
            playerScript = null;
            if (interactUI != null) interactUI.SetActive(false);
        }
    }
}