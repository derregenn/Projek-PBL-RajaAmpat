using UnityEngine;

public class NPCMapTrigger : MonoBehaviour
{
    public MapController mapController;
    public GameObject interactPromptUI; // Opsional: Teks UI "Press E to talk/open map"
    public KeyCode interactKey = KeyCode.E;

    private bool isPlayerNearby = false;

    void Start()
    {
        if (interactPromptUI != null) interactPromptUI.SetActive(false);
    }

    void Update()
    {
        if (isPlayerNearby && Input.GetKeyDown(interactKey))
        {
            if (mapController != null)
            {
                mapController.OpenWorldMap();
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            isPlayerNearby = true;
            if (interactPromptUI != null) interactPromptUI.SetActive(true);
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            isPlayerNearby = false;
            if (interactPromptUI != null) interactPromptUI.SetActive(false);
        }
    }
}