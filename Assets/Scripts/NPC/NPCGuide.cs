using UnityEngine;

public class NPCGuide : MonoBehaviour
{
    public GameObject mapUI; 
    public GameObject interactUI; 
    public GameObject warningUI; 

    private bool isPlayerNearby = false;
    private Player playerScript;

    void Update()
    {
        if (isPlayerNearby && Input.GetKeyDown(KeyCode.E))
        {
            if (playerScript != null)
            {
                if (playerScript.hasMap)
                {
                    if (mapUI != null)
                    {
                        mapUI.SetActive(true);
                        Time.timeScale = 0; // Menghentikan game saat peta terbuka
                    }
                }
                else
                {
                    if (warningUI != null)
                    {
                        warningUI.SetActive(true);
                        CancelInvoke("HideWarning"); // Mencegah bentrok timer
                        Invoke("HideWarning", 2.5f);
                    }
                }
            }
        }
    }

    // Tambahkan fungsi ini untuk memastikan waktu kembali normal saat peta ditutup via tombol X atau batal
    public void CloseMap()
    {
        if (mapUI != null)
        {
            mapUI.SetActive(false);
        }
        Time.timeScale = 1; // Mengembalikan waktu normal game
    }

    void HideWarning()
    {
        if (warningUI != null) warningUI.SetActive(false);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.tag == "Player")
        {
            isPlayerNearby = true;
            playerScript = collision.gameObject.GetComponent<Player>();
            if (interactUI != null) interactUI.SetActive(true);
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.gameObject.tag == "Player")
        {
            isPlayerNearby = false;
            playerScript = null;
            if (interactUI != null) interactUI.SetActive(false);
        }
    }
}