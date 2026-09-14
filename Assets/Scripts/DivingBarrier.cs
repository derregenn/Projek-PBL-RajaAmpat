using UnityEngine;
using UnityEngine.UI; // Jika nanti ingin memakai teks UI

public class DivingBarrier : MonoBehaviour
{
    [Header("UI Feedback")]
    [Tooltip("Masukkan objek teks peringatan dari Canvas (Opsional)")]
    public GameObject warningPanel;

    private void Start()
    {
        // Pastikan panel peringatan mati saat game dimulai
        if (warningPanel != null)
        {
            warningPanel.SetActive(false);
        }
    }

    // Terpanggil ketika pemain menabrak tembok barrier ini
    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Pastikan objek karaktermu memiliki Tag "Player" di Inspector
        if (collision.gameObject.CompareTag("Player"))
        {
            Debug.Log("Pemain mencoba ke air. Memicu peringatan!");
            
            // Tampilkan UI Peringatan (misal: "Tekan E untuk Menyelam")
            if (warningPanel != null)
            {
                warningPanel.SetActive(true);
            }
        }
    }

    // Terpanggil ketika pemain menjauh dari tembok barrier
    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            // Sembunyikan UI Peringatan saat pemain menjauh
            if (warningPanel != null)
            {
                warningPanel.SetActive(false);
            }
        }
    }
}