using UnityEngine;
using UnityEngine.UI;

public class SasiHoldInteract : MonoBehaviour
{
    public float holdDuration = 2.0f;
    private float currentTimer = 0f;
    private bool isPlayerInArea = false;

    public GameObject sasiVisualObject;
    public Image progressBarUI; // UI lingkaran/bar hold (Opsional)

    private void Update()
    {
        if (isPlayerInArea && Input.GetKey(KeyCode.E))
        {
            currentTimer += Time.deltaTime;
            
            if (progressBarUI != null)
                progressBarUI.fillAmount = currentTimer / holdDuration;

            if (currentTimer >= holdDuration)
            {
                CompleteSasiInstallation();
            }
        }
        else if (Input.GetKeyUp(KeyCode.E))
        {
            currentTimer = 0f;
            if (progressBarUI != null) progressBarUI.fillAmount = 0;
        }
    }

    private void CompleteSasiInstallation()
    {
        Debug.Log("Sasi Laut Berhasil Dipasang!");
        if (sasiVisualObject != null) sasiVisualObject.SetActive(true);
        
        // Tambahkan progress quest jika selesai
        if (QuestJournalManager.Instance != null)
        {
            QuestJournalManager.Instance.AddCompletedQuest();
        }

        gameObject.SetActive(false); // Matikan area trigger
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player")) isPlayerInArea = true;
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player")) 
        {
            isPlayerInArea = false;
            currentTimer = 0f;
        }
    }
}