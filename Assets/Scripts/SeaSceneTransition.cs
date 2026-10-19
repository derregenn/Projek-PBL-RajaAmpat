using UnityEngine;
using UnityEngine.SceneManagement;

public class SeaSceneTransition : MonoBehaviour
{
    [Header("Pengaturan Scene")]
    public string targetSceneName = "07_Laut";
    
    [Header("UI Feedback")]
    public GameObject interactPromptUI;

    private bool isPlayerInArea = false;

    private void Start()
    {
        // Otomatis matikan teks "Tekan E" saat game baru di-Play
        if (interactPromptUI != null)
        {
            interactPromptUI.SetActive(false);
        }
    }

    private void Update()
    {
        if (isPlayerInArea)
        {
            if (Input.GetKeyDown(KeyCode.E))
            {
                Debug.Log("--> Tombol E Ditekan! Memuat Scene: " + targetSceneName);
                SceneManager.LoadScene(targetSceneName);
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        Debug.Log("Pemicu disentuh oleh: " + collision.gameObject.name + " (Tag: " + collision.tag + ")");

        if (collision.CompareTag("Player") || collision.gameObject.name.Contains("Player"))
        {
            isPlayerInArea = true;
            if (interactPromptUI != null)
            {
                interactPromptUI.SetActive(true);
            }
            Debug.Log("--> Player Masuk Area Pindah Scene. Tekan E!");
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") || collision.gameObject.name.Contains("Player"))
        {
            isPlayerInArea = false;
            if (interactPromptUI != null)
            {
                interactPromptUI.SetActive(false);
            }
            Debug.Log("--> Player Keluar Area Pindah Scene.");
        }
    }
}